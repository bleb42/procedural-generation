namespace DungeonGen
{
    /// <summary>A wall piece. It is turned off when another room's wall covers it.</summary>
    public class WallSocket : WallSurface
    {
        public void ApplyVisual() => gameObject.SetActive(!IsSuppressed);
    }
}
