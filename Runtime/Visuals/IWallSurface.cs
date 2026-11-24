using UnityEngine;

namespace DungeonGen
{
    public interface IWallSurface
    {
        Vector3 Position { get; }
        Vector3 OutwardDirection { get; }

        void Hide();
    }
}
