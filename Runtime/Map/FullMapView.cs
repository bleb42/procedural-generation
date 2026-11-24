using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonGen
{
    /// <summary>A large map with zoom and drag.</summary>
    public class FullMapView : MapView, IScrollHandler, IDragHandler
    {
        private const float AbsoluteMinZoom = 0.01f;

        [Header("Settings")]
        [SerializeField] private MapZoomData _zoomAsset;

        [Tooltip("Show every room, even unvisited ones. Useful for debugging.")]
        [SerializeField] private bool _showAllRooms;

        private MapZoomData _zoomSettings;
        private RectTransform _viewport;
        private float _zoom = 1f;

        private MapZoomData Zoom
        {
            get
            {
                if (_zoomSettings == null)
                    _zoomSettings = _zoomAsset != null ? _zoomAsset : ScriptableObject.CreateInstance<MapZoomData>();

                return _zoomSettings;
            }
        }

        protected override bool IsShown(SpawnedRoom room) => _showAllRooms || base.IsShown(room);

        protected override void OnRebuilt() => ResetView();

        protected override void OnStatesRefreshed()
        {
            if (_content != null)
                ClampPosition();
        }

        public void Refit() => ResetView();

        public void ZoomIn() => ZoomAround(Vector2.zero, Zoom.ZoomFactor);

        public void ZoomOut() => ZoomAround(Vector2.zero, 1f / Zoom.ZoomFactor);

        public void ResetView()
        {
            if (_content == null)
                return;

            _zoom = Mathf.Clamp(Zoom.StartZoom, MinZoom(), Zoom.MaxZoom);
            _content.localScale = new Vector3(_zoom, _zoom, 1f);
            CenterOnPlayer();
        }

        public void CenterOnPlayer()
        {
            if (_content == null || Player == null)
                return;

            _content.anchoredPosition = -PlayerMapPosition * _zoom;
            ClampPosition();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData.scrollDelta.y == 0f || GetViewport() == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, eventData.position, eventData.pressEventCamera, out Vector2 cursor);

            float factor = eventData.scrollDelta.y > 0f ? Zoom.ZoomFactor : 1f / Zoom.ZoomFactor;
            ZoomAround(cursor, factor);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_content == null || GetViewport() == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, eventData.position, eventData.pressEventCamera, out Vector2 now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 previous);

            _content.anchoredPosition += now - previous;
            ClampPosition();
        }

        protected override void Tick()
        {
            if (_playerMarker == null || _content == null)
                return;

            _playerMarker.anchoredPosition = _content.anchoredPosition + PlayerMapPosition * _zoom;

            RotateMarker();
        }

        private void ZoomAround(Vector2 cursor, float factor)
        {
            if (_content == null)
                return;

            float newZoom = Mathf.Clamp(_zoom * factor, MinZoom(), Zoom.MaxZoom);

            if (Mathf.Approximately(newZoom, _zoom))
                return;

            Vector2 mapPoint = (cursor - _content.anchoredPosition) / _zoom;

            _zoom = newZoom;
            _content.localScale = new Vector3(_zoom, _zoom, 1f);
            _content.anchoredPosition = cursor - mapPoint * _zoom;

            ClampPosition();
        }

        private float MinZoom()
        {
            if (!_hasBounds || GetViewport() == null)
                return Zoom.MinZoom;

            Vector2 map = _boundsMax - _boundsMin;
            Vector2 view = _viewport.rect.size;

            if (map.x <= 0f || map.y <= 0f)
                return Zoom.MinZoom;

            float fit = Mathf.Min(view.x / map.x, view.y / map.y) * Zoom.FitMargin;

            return Mathf.Clamp(Mathf.Min(fit, Zoom.StartZoom), AbsoluteMinZoom, Zoom.MaxZoom);
        }

        private void ClampPosition()
        {
            if (!_hasBounds || GetViewport() == null)
                return;

            Vector2 center = (_boundsMin + _boundsMax) * 0.5f * _zoom;
            Vector2 halfMap = (_boundsMax - _boundsMin) * 0.5f * _zoom;

            Vector2 offset = _content.anchoredPosition + center;
            offset.x = Mathf.Clamp(offset.x, -halfMap.x, halfMap.x);
            offset.y = Mathf.Clamp(offset.y, -halfMap.y, halfMap.y);

            _content.anchoredPosition = offset - center;
        }

        private RectTransform GetViewport()
        {
            if (_viewport == null && _content != null)
                _viewport = _content.parent as RectTransform;

            return _viewport;
        }
    }
}
