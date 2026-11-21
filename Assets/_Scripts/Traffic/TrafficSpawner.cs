using System.Collections;
using UnityEngine;

namespace Jejak.Traffic
{
    public class TrafficSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private float minInterval = 2.0f;
        [SerializeField] private float maxInterval = 4.5f;
        [SerializeField] private float minSpeed = 6.0f;
        [SerializeField] private float maxSpeed = 10.0f;

        [Header("Colors (Optional)")]
        [SerializeField] private Color[] carColors = new Color[]
        {
            Color.white, Color.red, Color.blue, Color.yellow, Color.gray, Color.black
        };

        private Coroutine spawnRoutine;

        private void OnEnable()
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        private void OnDisable()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

                if (carPrefab != null)
                {
                    GameObject car = Instantiate(carPrefab, transform.position, transform.rotation, transform);
                    TrafficCar trafficCar = car.GetComponent<TrafficCar>();
                    if (trafficCar != null)
                    {
                        trafficCar.speed = Random.Range(minSpeed, maxSpeed);
                    }

                    if (carColors.Length > 0)
                    {
                        Renderer rend = car.GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            rend.material.color = carColors[Random.Range(0, carColors.Length)];
                        }
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
    }
}
