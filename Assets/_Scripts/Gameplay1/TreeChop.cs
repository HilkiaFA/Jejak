using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay1
{
    public class TreeChop : MonoBehaviour
    {
        [Header("Chop Settings")]
        [SerializeField] private int maxChops = 3;
        [SerializeField] private int woodReward = 10;
        [SerializeField] private float chopCooldown = 0.4f;
        [Tooltip("Jarak deteksi pemain ke pohon (dalam meter)")]
        [SerializeField] private float interactDistance = 2.5f;

        [Header("Visuals (Optional)")]
        [SerializeField] private GameObject treeModel;
        [SerializeField] private GameObject stumpModel;

        private int currentChops = 0;
        private bool isFelled = false;
        private bool isPlayerNearby = false;
        private float lastChopTime = 0f;
        private Transform playerTransform;

        private void Start()
        {
            FindPlayer();
            ResetTree();
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            if (isFelled) return;

            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            Vector3 diff = playerTransform.position - transform.position;
            diff.y = 0f;
            float horizontalDistance = diff.magnitude;

            isPlayerNearby = (horizontalDistance <= interactDistance);

            if (isPlayerNearby)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    if (Time.time - lastChopTime >= chopCooldown)
                    {
                        Chop();
                    }
                }
            }
        }

        private void Chop()
        {
            lastChopTime = Time.time;
            currentChops++;
            StartCoroutine(ShakeTreeRoutine());

            Debug.Log($"<color=yellow>[Tebang Pohon] {gameObject.name}: Tebasan {currentChops}/{maxChops}</color>");

            if (currentChops >= maxChops)
            {
                FellTree();
            }
        }

        private void FellTree()
        {
            isFelled = true;
            isPlayerNearby = false;

            if (treeModel != null)
            {
                treeModel.SetActive(false);
                if (stumpModel != null) stumpModel.SetActive(true);
            }
            else
            {
                Renderer rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;

                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }

            if (Gameplay1Manager.Instance != null)
            {
                Gameplay1Manager.Instance.AddWood(woodReward);
            }

            Debug.Log($"<color=green>[Pohon Tumbang] Mendapatkan {woodReward} Balok Kayu!</color>");
        }

        private IEnumerator ShakeTreeRoutine()
        {
            Vector3 originalPos = transform.position;
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.08f, 0.08f), 0, Random.Range(-0.08f, 0.08f));
                transform.position = originalPos + offset;
                yield return new WaitForSeconds(0.02f);
            }
            transform.position = originalPos;
        }

        public void ResetTree()
        {
            currentChops = 0;
            isFelled = false;
            isPlayerNearby = false;

            if (treeModel != null)
            {
                treeModel.SetActive(true);
                if (stumpModel != null) stumpModel.SetActive(false);
            }
            else
            {
                Renderer rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = true;

                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }
        }

        private void OnGUI()
        {
            if (isPlayerNearby && !isFelled)
            {
                GUI.Box(new Rect(Screen.width / 2 - 120, Screen.height - 80, 240, 35), $"[Tekan E] Tebang ({currentChops}/{maxChops})");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactDistance);
        }
    }
}
