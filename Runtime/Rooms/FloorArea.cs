using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Marks a walkable rectangle of a room. Add one or several (for L-shaped or T-shaped rooms).
    /// The baker measures them; their edges can be anywhere, there is no grid.
    /// </summary>
    public class FloorArea : MonoBehaviour
    {
        [SerializeField] private Vector3 _size = new Vector3(4f, 0.1f, 4f);

        public Vector3 Size
        {
            get => _size;
            set => _size = value;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0.2f, 0.85f, 0.2f, 0.35f);
            Gizmos.DrawCube(Vector3.zero, _size);

            Gizmos.color = new Color(0.2f, 0.85f, 0.2f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, _size);
        }
#endif
    }
}
