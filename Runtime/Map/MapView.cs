using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGen
{
    /// <summary>Base class of the map views. Draws every room as UI rectangles under <c>_content</c>.</summary>
    public abstract class MapView : MonoBehaviour
    {
        private const float SeamOverlap = 0.5f;
        private const float EdgeTolerance = 0.05f;
        private const float DoorProbeDistance = 0.25f;

        private static readonly FloorRect[] NoSiblings = new FloorRect[0];

        [Header("UI")]
        [Tooltip("Empty RectTransform inside the map panel. Anchors and pivot at center, position 0. " +
                 "Its parent gets a RectMask2D automatically.")]
        [SerializeField] protected RectTransform _content;

        [Tooltip("Optional. Sibling of Content (NOT a child), pivot at center.")]
        [SerializeField] protected RectTransform _playerMarker;

        [Header("Settings")]
        [Tooltip("Defaults are used when empty.")]
        [SerializeField] private MapStyleData _styleAsset;

        private enum Side
        {
            Left,
            Right,
            Down,
            Up
        }

        private sealed class RectView
        {
            public Image Image;
            public FloorRect Rect;
        }

        private sealed class UnknownView
        {
            public Image Image;
            public FloorRect Rect;
            public SpawnedRoom Neighbor;
        }

        private sealed class RoomView
        {
            public readonly List<RectView> Rects = new();
            public readonly List<Image> Doors = new();
            public readonly List<UnknownView> Unknowns = new();
        }

        private readonly Dictionary<SpawnedRoom, RoomView> _views = new();

        private MapStyleData _style;

        protected Vector2 _boundsMin, _boundsMax;
        protected bool _hasBounds;

        protected MapModel Model { get; private set; }
        protected Transform Player { get; private set; }

        protected MapStyleData Style
        {
            get
            {
                if (_style == null)
                    _style = _styleAsset != null ? _styleAsset : ScriptableObject.CreateInstance<MapStyleData>();

                return _style;
            }
        }

        /// <summary>Position of the player in map pixels, relative to the map origin.</summary>
        protected Vector2 PlayerMapPosition => Model.ToLocal(Player.position) * Style.PixelsPerUnit;

        public void Bind(MapModel model, Transform player)
        {
            Unbind();

            Model = model;
            Player = player;

            Model.Rebuilt += Rebuild;
            Model.Entered += RefreshStates;

            Rebuild();
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (Model == null)
                return;

            Model.Rebuilt -= Rebuild;
            Model.Entered -= RefreshStates;
            Model = null;
        }

        private void Update()
        {
            if (Model == null || Player == null || _content == null)
                return;

            Tick();
        }

        protected abstract void Tick();

        protected virtual void OnRebuilt() { }

        protected virtual void OnStatesRefreshed() { }

        protected virtual bool IsShown(SpawnedRoom room) => Model.IsVisited(room);

        protected void RotateMarker()
        {
            if (_playerMarker != null)
                _playerMarker.localEulerAngles = new Vector3(0f, 0f, -Player.eulerAngles.y);
        }

        private void Rebuild()
        {
            ClearContent();

            if (_content == null)
                return;

            EnsureMask();

            foreach (var room in Model.Rooms)
            {
                var view = new RoomView();

                foreach (var rect in room.Rects)
                    view.Rects.Add(new RectView { Image = CreateRoomRect(rect, room.Rects), Rect = rect });

                _views[room] = view;
            }

            foreach (var room in Model.Rooms)
            {
                var view = _views[room];

                foreach (var door in room.Node.Doors)
                {
                    if (!door.IsConnected)
                        continue;

                    var probe = door.Position + DirectionUtil.ToVector(door.Direction) * DoorProbeDistance;
                    var neighbor = Model.RoomAt(probe);

                    if (neighbor != null && neighbor != room && TryCreateUnknown(door, probe, neighbor, out var unknown))
                        view.Unknowns.Add(unknown);

                    view.Doors.Add(CreateDoorMarker(door));
                }
            }

            RefreshStates();
            OnRebuilt();
        }

        protected void RefreshStates()
        {
            if (Model == null)
                return;

            _hasBounds = false;

            foreach (var pair in _views)
            {
                bool shown = IsShown(pair.Key);
                var color = GetRoomColor(pair.Key);

                foreach (var rect in pair.Value.Rects)
                {
                    rect.Image.gameObject.SetActive(shown);
                    rect.Image.color = color;

                    if (shown)
                        GrowBounds(rect.Rect);
                }

                foreach (var door in pair.Value.Doors)
                    door.gameObject.SetActive(shown);

                foreach (var unknown in pair.Value.Unknowns)
                {
                    bool show = shown && !IsShown(unknown.Neighbor);

                    unknown.Image.gameObject.SetActive(show);
                    unknown.Image.color = Style.UnknownColor;

                    if (show)
                        GrowBounds(unknown.Rect);
                }
            }

            OnStatesRefreshed();
        }

        private void ClearContent()
        {
            if (_content != null)
            {
                for (int i = _content.childCount - 1; i >= 0; i--)
                {
                    var child = _content.GetChild(i).gameObject;

                    if (Application.isPlaying)
                        Destroy(child);
                    else
                        DestroyImmediate(child);
                }
            }

            _views.Clear();
            _hasBounds = false;
        }

        private void GrowBounds(FloorRect rect)
        {
            float scale = Style.PixelsPerUnit;
            Vector2 min = rect.Min * scale;
            Vector2 max = rect.Max * scale;

            if (!_hasBounds)
            {
                _boundsMin = min;
                _boundsMax = max;
                _hasBounds = true;
                return;
            }

            _boundsMin = Vector2.Min(_boundsMin, min);
            _boundsMax = Vector2.Max(_boundsMax, max);
        }

        /// <summary>The part of a not yet visited room that is revealed behind a door.</summary>
        private bool TryCreateUnknown(PlacementDoor door, Vector2 probe, SpawnedRoom neighbor, out UnknownView view)
        {
            view = null;

            foreach (var neighborRect in neighbor.Rects)
            {
                if (!neighborRect.Contains(probe, EdgeTolerance))
                    continue;

                var forward = DirectionUtil.ToVector(door.Direction);
                bool alongX = door.Direction == Direction.East || door.Direction == Direction.West;
                float depth = Style.UnknownDepth;
                float width = Style.UnknownWidth;

                var patch = new FloorRect(
                    door.Position + forward * (depth * 0.5f),
                    alongX ? new Vector2(depth, width) : new Vector2(width, depth));

                if (!patch.TryIntersect(neighborRect, out var visible))
                    return false;

                view = new UnknownView
                {
                    Image = CreateRoomRect(visible, NoSiblings),
                    Rect = visible,
                    Neighbor = neighbor
                };

                return true;
            }

            return false;
        }

        private Image CreateRoomRect(FloorRect rect, IReadOnlyList<FloorRect> siblings)
        {
            float half = Style.RoomGap * 0.5f;

            float left = Touches(rect, siblings, Side.Left) ? -SeamOverlap : half;
            float right = Touches(rect, siblings, Side.Right) ? -SeamOverlap : half;
            float down = Touches(rect, siblings, Side.Down) ? -SeamOverlap : half;
            float up = Touches(rect, siblings, Side.Up) ? -SeamOverlap : half;

            Vector2 min = rect.Min * Style.PixelsPerUnit;
            Vector2 max = rect.Max * Style.PixelsPerUnit;

            var size = new Vector2(max.x - min.x - left - right, max.y - min.y - down - up);
            var center = (min + max) * 0.5f + new Vector2((left - right) * 0.5f, (down - up) * 0.5f);

            return CreateRect(center, size, Style.VisitedColor);
        }

        /// <summary>True when another rectangle of the same room continues past the given side.</summary>
        private static bool Touches(FloorRect rect, IReadOnlyList<FloorRect> siblings, Side side)
        {
            Vector2 min = rect.Min, max = rect.Max;

            foreach (var other in siblings)
            {
                if (other.Center == rect.Center && other.Size == rect.Size)
                    continue;

                Vector2 oMin = other.Min, oMax = other.Max;

                switch (side)
                {
                    case Side.Left:
                        if (Mathf.Abs(oMax.x - min.x) <= EdgeTolerance && OverlapLength(min.y, max.y, oMin.y, oMax.y) > EdgeTolerance)
                            return true;
                        break;

                    case Side.Right:
                        if (Mathf.Abs(oMin.x - max.x) <= EdgeTolerance && OverlapLength(min.y, max.y, oMin.y, oMax.y) > EdgeTolerance)
                            return true;
                        break;

                    case Side.Down:
                        if (Mathf.Abs(oMax.y - min.y) <= EdgeTolerance && OverlapLength(min.x, max.x, oMin.x, oMax.x) > EdgeTolerance)
                            return true;
                        break;

                    default:
                        if (Mathf.Abs(oMin.y - max.y) <= EdgeTolerance && OverlapLength(min.x, max.x, oMin.x, oMax.x) > EdgeTolerance)
                            return true;
                        break;
                }
            }

            return false;
        }

        private static float OverlapLength(float aMin, float aMax, float bMin, float bMax) =>
            Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin);

        private Image CreateDoorMarker(PlacementDoor door)
        {
            float scale = Style.PixelsPerUnit;
            float across = Style.DoorMarkerLength * scale;
            float along = Style.RoomGap + 2f;

            bool horizontalMove = door.Direction == Direction.East || door.Direction == Direction.West;
            var size = horizontalMove ? new Vector2(along, across) : new Vector2(across, along);

            return CreateRect(door.Position * scale, size, Style.DoorColor);
        }

        private Image CreateRect(Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = new GameObject("MapRect", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;

            rect.SetParent(_content, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private void EnsureMask()
        {
            var viewport = _content.parent;

            if (viewport == null || viewport.GetComponent<Canvas>() != null)
                return;

            if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }

        private Color GetRoomColor(SpawnedRoom room)
        {
            if (room == Model.Start)
                return Style.StartColor;

            if (room == Model.Current)
                return Style.CurrentColor;

            return Style.VisitedColor;
        }
    }
}
