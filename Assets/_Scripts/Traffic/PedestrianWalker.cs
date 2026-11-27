using UnityEngine;

namespace Jejak.Traffic
{
    public class PedestrianWalker : MonoBehaviour
    {
        [Header("Waypoints")]
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;

        [Header("Walking Settings")]
        [SerializeField] private float speed = 2.0f;
        [SerializeField] private float arriveThreshold = 0.5f;

        private Transform targetPoint;

        private void Start()
        {
            if (pointA != null)
            {
                targetPoint = pointA;
            }
        }

        private void Update()
        {
            if (targetPoint == null) return;

            Vector3 direction = (targetPoint.position - transform.position);
            direction.y = 0;

            if (direction.magnitude <= arriveThreshold)
            {
                targetPoint = (targetPoint == pointA) ? pointB : pointA;
            }
            else
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);
            }
        }

        public void SetPoints(Transform a, Transform b)
        {
            pointA = a;
            pointB = b;
            targetPoint = a;
        }
    }
}
