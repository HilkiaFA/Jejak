using UnityEngine;

namespace Jejak.Player
{
    public class SimpleFollowCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);
        [SerializeField] private float smoothSpeed = 8.0f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;

            transform.LookAt(target.position + Vector3.up * 1.2f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
