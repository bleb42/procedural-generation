using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Entry point of the package. Put it in a scene, assign a profile and call <see cref="Generate()"/>.
    /// Subscribe to <see cref="Generated"/> to place enemies, items and so on.
    /// </summary>
    public class DungeonBuilder : MonoBehaviour
    {
        [SerializeField] private DungeonProfile _profile;

        [Tooltip("Parent of the spawned rooms. This transform is used when empty.")]
        [SerializeField] private Transform _roomsRoot;

        [Header("Seed")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _fixedSeed;

        [Header("Performance")]
        [Tooltip("How many milliseconds per frame generation may use. The rest is spread over the next frames.")]
        [SerializeField, Range(1f, 16f)] private float _msPerFrame = 6f;

        [Header("Startup")]
        [SerializeField] private bool _generateOnStart;
        [SerializeField] private bool _logResult = true;

        private bool _isGenerating;

        /// <summary>Raised after the rooms are spawned and their walls are set up.</summary>
        public event Action<DungeonResult> Generated;

        /// <summary>Raised when the current dungeon is removed.</summary>
        public event Action Cleared;

        public DungeonProfile Profile
        {
            get => _profile;
            set => _profile = value;
        }

        /// <summary>The dungeon that is currently in the scene, or null.</summary>
        public DungeonResult Current { get; private set; }

        public bool IsGenerating => _isGenerating;

        private Transform Root => _roomsRoot != null ? _roomsRoot : transform;

        private void Start()
        {
            if (_generateOnStart)
                Generate();
        }

        private void OnDisable() => _isGenerating = false;

        /// <summary>Starts generation over several frames. Returns null when one is already running.</summary>
        public Coroutine Generate() => _isGenerating ? null : StartCoroutine(GenerateRoutine());

        /// <summary>Same as <see cref="Generate()"/> with a specific seed.</summary>
        public Coroutine Generate(int seed) => _isGenerating ? null : StartCoroutine(GenerateRoutine(seed));

        /// <summary>Generation as a coroutine. <paramref name="onDone"/> receives null on failure.</summary>
        public IEnumerator GenerateRoutine(int? seed = null, Action<DungeonResult> onDone = null)
        {
            if (_isGenerating)
            {
                onDone?.Invoke(null);
                yield break;
            }

            _isGenerating = true;

            yield return GenerateInternal(ResolveSeed(seed), new FrameBudget(_msPerFrame), onDone);

            _isGenerating = false;
        }

        /// <summary>Generates everything in one go. Works in the editor too. Returns null on failure.</summary>
        public DungeonResult GenerateImmediate(int? seed = null)
        {
            if (_isGenerating)
                return null;

            DungeonResult result = null;

            CoroutineUtility.RunImmediately(
                GenerateInternal(ResolveSeed(seed), FrameBudget.Unlimited, r => result = r));

            return result;
        }

        /// <summary>Removes the current dungeon.</summary>
        public void Clear()
        {
            CoroutineUtility.RunImmediately(new RoomSpawner(Root).ClearRoutine(FrameBudget.Unlimited));
            ReleaseCurrent();
        }

        [ContextMenu("Generate (Editor Preview)")]
        private void GeneratePreview() => GenerateImmediate();

        [ContextMenu("Clear")]
        private void ClearFromMenu() => Clear();

        private int ResolveSeed(int? seed)
        {
            if (seed.HasValue)
                return seed.Value;

            return _useFixedSeed ? _fixedSeed : Guid.NewGuid().GetHashCode();
        }

        private IEnumerator GenerateInternal(int seed, FrameBudget budget, Action<DungeonResult> onDone)
        {
            var problems = new List<string>();

            if (_profile == null)
                problems.Add("No DungeonProfile is assigned.");
            else
                _profile.Validate(problems);

            if (problems.Count > 0)
            {
                foreach (var problem in problems)
                    Debug.LogError($"DungeonBuilder: {problem}", this);

                onDone?.Invoke(null);
                yield break;
            }

            ReleaseCurrent();

            var spawner = new RoomSpawner(Root);

            yield return spawner.ClearRoutine(budget);
            yield return null;

            var layout = BuildLayout(seed);

            if (layout == null)
            {
                onDone?.Invoke(null);
                yield break;
            }

            List<SpawnedRoom> rooms = null;
            yield return spawner.SpawnRoutine(layout, budget, spawned => rooms = spawned);

            if (rooms == null)
            {
                Debug.LogError("DungeonBuilder: generation failed, see the errors above.", this);
                onDone?.Invoke(null);
                yield break;
            }

            yield return RoomVisualCoordinator.ApplyVisualsRoutine(rooms, budget);

            Current = new DungeonResult(layout, rooms, Root);
            Report(layout);

            Generated?.Invoke(Current);
            onDone?.Invoke(Current);
        }

        private DungeonLayout BuildLayout(int seed)
        {
            try
            {
                return new DungeonLayoutGenerator(seed).Generate(_profile.CreateRequest());
            }
            catch (Exception e)
            {
                Debug.LogError($"DungeonBuilder: layout failed. {e.Message}", this);
                return null;
            }
        }

        private void Report(DungeonLayout layout)
        {
            foreach (var warning in layout.Warnings)
                Debug.LogWarning($"DungeonBuilder: {warning}", this);

            if (layout.ReachedTarget)
            {
                if (_logResult)
                    Debug.Log($"DungeonBuilder: {layout.PlacedRoomCount}/{layout.RequestedRoomCount} rooms (seed {layout.Seed}).", this);

                return;
            }

            Debug.LogWarning($"DungeonBuilder: only {layout.PlacedRoomCount}/{layout.RequestedRoomCount} rooms fit " +
                             $"(seed {layout.Seed}). Add more branch rooms or doors.", this);
        }

        private void ReleaseCurrent()
        {
            if (Current == null)
                return;

            Current = null;
            Cleared?.Invoke();
        }
    }
}
