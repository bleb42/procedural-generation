using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Connects a <see cref="DungeonBuilder"/>, the player and the map views.
    /// It also tracks which room the player is in. Keep this object active while the game runs.
    /// </summary>
    public class MapPanel : MonoBehaviour
    {
        [SerializeField] private MiniMapView _mini;
        [SerializeField] private FullMapView _full;

        [Tooltip("Optional. When both are set, the panel initializes itself on Start.")]
        [SerializeField] private DungeonBuilder _builder;
        [SerializeField] private Transform _player;

        public MapModel Model { get; } = new MapModel();

        private void Start()
        {
            if (_builder != null && _player != null)
                Initialize(_builder, _player);
        }

        private void Update()
        {
            if (_player != null)
                Model.Track(_player.position);
        }

        private void OnDestroy() => Unsubscribe();

        public void Initialize(DungeonBuilder builder, Transform player)
        {
            Unsubscribe();

            _builder = builder;
            _player = player;

            if (_mini != null)
                _mini.Bind(Model, player);

            if (_full != null)
                _full.Bind(Model, player);

            _builder.Generated += Model.Build;
            _builder.Cleared += Model.Clear;

            if (_builder.Current != null)
                Model.Build(_builder.Current);
        }

        /// <summary>Fits the full map to its viewport. Call it when the full map is opened.</summary>
        public void Refit()
        {
            if (_full != null)
                _full.Refit();
        }

        private void Unsubscribe()
        {
            if (_builder == null)
                return;

            _builder.Generated -= Model.Build;
            _builder.Cleared -= Model.Clear;
        }
    }
}
