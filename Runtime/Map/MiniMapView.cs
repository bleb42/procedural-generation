namespace DungeonGen
{
    /// <summary>A small map that stays centered on the player.</summary>
    public class MiniMapView : MapView
    {
        protected override void Tick()
        {
            _content.anchoredPosition = -PlayerMapPosition;

            RotateMarker();
        }
    }
}
