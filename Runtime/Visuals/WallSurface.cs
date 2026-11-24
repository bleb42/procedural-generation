using UnityEngine;

namespace DungeonGen
{
    /// <summary>Base of everything that can be a wall piece. Overlapping walls of neighbouring rooms get hidden.</summary>
    public abstract class WallSurface : MonoBehaviour, IWallSurface
    {
        [Tooltip("Euler offset applied to transform.forward to get the direction the wall faces outward.")]
        [SerializeField] private Vector3 _outwardDirectionOffset = Vector3.zero;

        public Vector3 Position => transform.position;
        public Vector3 OutwardDirection => Quaternion.Euler(_outwardDirectionOffset) * transform.forward;

        protected bool IsSuppressed { get; private set; }

        public void Hide() => IsSuppressed = true;
    }
}
