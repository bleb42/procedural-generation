using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonGen
{
    /// <summary>Instantiates the rooms of a layout under a parent transform.</summary>
    public sealed class RoomSpawner
    {
        private readonly Transform _parent;

        public RoomSpawner(Transform parent) => _parent = parent;

        public IEnumerator ClearRoutine(FrameBudget budget)
        {
            budget.Begin();

            for (int i = _parent.childCount - 1; i >= 0; i--)
            {
                var child = _parent.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }

                if (budget.Exceeded)
                {
                    yield return null;
                    budget.Begin();
                }
            }
        }

        /// <summary>Spawns every room. <paramref name="onDone"/> receives null when spawning failed.</summary>
        public IEnumerator SpawnRoutine(DungeonLayout layout, FrameBudget budget, Action<List<SpawnedRoom>> onDone)
        {
            var spawned = new List<SpawnedRoom>(layout.Nodes.Count);

            budget.Begin();

            foreach (var node in layout.Nodes)
            {
                if (!SpawnOne(node, spawned))
                {
                    yield return ClearRoutine(budget);
                    onDone(null);
                    yield break;
                }

                if (budget.Exceeded)
                {
                    yield return null;
                    budget.Begin();
                }
            }

            onDone(spawned);
        }

        private bool SpawnOne(PlacementNode node, List<SpawnedRoom> spawned)
        {
            var definition = node.Blueprint as RoomDefinition;

            if (definition == null || definition.Prefab == null)
            {
                Debug.LogError($"{node.Blueprint?.Name}: the room has no prefab, it cannot be spawned.");
                return false;
            }

            var instance = Object.Instantiate(definition.Prefab, _parent).GetComponent<RoomInstance>();

            if (instance == null)
            {
                Debug.LogError($"{definition.Prefab.name}: the prefab has no RoomInstance component.");
                return false;
            }

            var t = instance.transform;
            t.localPosition = new Vector3(node.Origin.x, 0f, node.Origin.y);
            t.localRotation = Quaternion.Euler(0f, DirectionUtil.ToYaw(node.RotationSteps), 0f);

            if (instance.DoorSockets.Count != node.Doors.Count)
            {
                Debug.LogError($"{instance.name}: the prefab has {instance.DoorSockets.Count} door sockets but " +
                               $"the definition has {node.Doors.Count} doors. Bake the room again.");
                return false;
            }

            WarnIfStale(instance, definition);

            spawned.Add(new SpawnedRoom(node, instance, definition.Weight));
            return true;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void WarnIfStale(RoomInstance instance, RoomDefinition definition)
        {
            var floors = new List<FloorRect>();
            var doors = new List<DoorDef>();
            var problems = new List<string>();

            RoomMeasure.Measure(instance, floors, doors, problems);

            if (RoomMeasure.Matches(floors, doors, definition))
                return;

            Debug.LogWarning($"{definition.name}: the prefab does not match its baked data. " +
                             "Rooms may overlap or not line up. Bake the room again.", definition);
        }
    }
}
