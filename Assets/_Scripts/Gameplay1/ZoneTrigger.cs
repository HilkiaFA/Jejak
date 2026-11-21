using UnityEngine;

namespace Jejak.Gameplay1
{
    public enum ZoneType
    {
        SafeZone,
        UnsafeZone,
        Checkpoint
    }

    [RequireComponent(typeof(Collider))]
    public class ZoneTrigger : MonoBehaviour
    {
        [SerializeField] private ZoneType zoneType = ZoneType.SafeZone;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && Gameplay1Manager.Instance != null)
            {
                switch (zoneType)
                {
                    case ZoneType.UnsafeZone:
                        Gameplay1Manager.Instance.SetPlayerInUnsafeZone(true);
                        break;
                    case ZoneType.Checkpoint:
                        Gameplay1Manager.Instance.SetPlayerInCheckpoint(true);
                        break;
                    case ZoneType.SafeZone:
                        break;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && Gameplay1Manager.Instance != null)
            {
                switch (zoneType)
                {
                    case ZoneType.UnsafeZone:
                        Gameplay1Manager.Instance.SetPlayerInUnsafeZone(false);
                        break;
                    case ZoneType.Checkpoint:
                        Gameplay1Manager.Instance.SetPlayerInCheckpoint(false);
                        break;
                }
            }
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            switch (zoneType)
            {
                case ZoneType.SafeZone:
                    Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
                    break;
                case ZoneType.UnsafeZone:
                    Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
                    break;
                case ZoneType.Checkpoint:
                    Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f);
                    break;
            }
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
