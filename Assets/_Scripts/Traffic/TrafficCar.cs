using UnityEngine;

namespace Jejak.Traffic
{
    public class TrafficCar : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 8.0f;
        [SerializeField] private float rayDistance = 4.0f;
        [SerializeField] private LayerMask obstacleLayer = ~0; 

        [Header("Lifetime")]
        [SerializeField] private float lifeTime = 25.0f;

        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; 
            }
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            bool isBlocked = Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, rayDistance, obstacleLayer);

            if (!isBlocked)
            {
                transform.Translate(Vector3.forward * (speed * Time.deltaTime));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * rayDistance);
        }
    }
}
