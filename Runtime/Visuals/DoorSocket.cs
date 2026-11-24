using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// A doorway. It shows the door visual when connected to another room and the wall visual otherwise.
    /// transform.forward must point out of the room.
    /// </summary>
    public class DoorSocket : WallSurface
    {
        [SerializeField] private GameObject _doorVisual;
        [SerializeField] private GameObject _wallVisual;

        public bool IsUsed { get; private set; }

        public void SetConnected(bool connected)
        {
            IsUsed = connected;

            if (_doorVisual != null)
                _doorVisual.SetActive(connected && !IsSuppressed);

            if (_wallVisual != null)
                _wallVisual.SetActive(!connected && !IsSuppressed);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var from = transform.position;
            var to = from + OutwardDirection * 0.75f;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(to, 0.1f);
        }
#endif

        public GameObject GetVisual(bool connected) => connected ? _doorVisual : _wallVisual;
    }
}
