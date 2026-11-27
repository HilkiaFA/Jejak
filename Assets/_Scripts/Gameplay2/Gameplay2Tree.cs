using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay2
{
    public enum TreeCategory
    {
        Legal,
        Protected
    }

    public class Gameplay2Tree : MonoBehaviour
    {
        [Header("Tree Info")]
        public string treeName = "Pohon Ulin";
        public TreeCategory category = TreeCategory.Legal;
        public int maxHP = 150;
        public int currentHP = 150;
        public int woodReward = 15;
        public int seedlingReward = 1;

        [Header("State")]
        public bool isFelled = false;
        public bool isClassificationRevealed = false;
        [SerializeField] private float interactDistance = 2.5f;

        private Transform playerTransform;
        private bool isPlayerNearby = false;
        private float lastHitTime = 0f;
        private float hitCooldown = 0.5f;
        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
            ResetTree();

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            if (isFelled) return;

            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                return;
            }

            Vector3 diff = playerTransform.position - transform.position;
            diff.y = 0;
            isPlayerNearby = (diff.magnitude <= interactDistance);

            if (isPlayerNearby)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    if (Time.time - lastHitTime >= hitCooldown)
                    {
                        HitTree();
                    }
                }
            }
        }

        public void RevealClassification()
        {
            isClassificationRevealed = true;
        }

        private void HitTree()
        {
            lastHitTime = Time.time;

            if (category == TreeCategory.Protected)
            {
                StartCoroutine(ShakeRoutine());
                Debug.LogWarning($"<color=red>[Pohon Dilindungi] Kamu memukul {treeName}! Babi hutan seketika mengamuk!</color>");
                if (Gameplay2Manager.Instance != null)
                {
                    Gameplay2Manager.Instance.TriggerProtectedTreeAggro();
                }
                return;
            }

            int totalDamage = 20;

            if (Gameplay2Manager.Instance != null)
            {
                if (Gameplay2Manager.Instance.wargaNPC != null &&
                    Gameplay2Manager.Instance.wargaNPC.isRecruited &&
                    !Gameplay2Manager.Instance.wargaNPC.isDead)
                {
                    totalDamage += Gameplay2Manager.Instance.wargaNPC.attackDamage;
                }

                if (Gameplay2Manager.Instance.mahasiswaNPC != null &&
                    Gameplay2Manager.Instance.mahasiswaNPC.isRecruited &&
                    !Gameplay2Manager.Instance.mahasiswaNPC.isDead)
                {
                    totalDamage += Gameplay2Manager.Instance.mahasiswaNPC.attackDamage;
                }
            }

            currentHP -= totalDamage;
            StartCoroutine(ShakeRoutine());

            Debug.Log($"<color=green>[Tebang Bersama] {treeName} terkena {totalDamage} damage! Sisa HP: {currentHP}/{maxHP}</color>");

            if (currentHP <= 0)
            {
                FellTree();
            }
        }

        private void FellTree()
        {
            isFelled = true;
            isPlayerNearby = false;

            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (Gameplay2Manager.Instance != null)
            {
                Gameplay2Manager.Instance.AddWoodAndSeedling(woodReward, seedlingReward);
            }
        }

        private IEnumerator ShakeRoutine()
        {
            Vector3 orig = transform.position;
            for (int i = 0; i < 5; i++)
            {
                transform.position = orig + new Vector3(Random.Range(-0.08f, 0.08f), 0, Random.Range(-0.08f, 0.08f));
                yield return new WaitForSeconds(0.02f);
            }
            transform.position = orig;
        }

        public void ResetTree()
        {
            isFelled = false;
            currentHP = maxHP;
            isClassificationRevealed = false;
            transform.position = startPosition;

            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = true;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        private void OnGUI()
        {
            if (isFelled) return;

            if (isPlayerNearby)
            {
                string info = "";
                if (category == TreeCategory.Protected)
                {
                    info = isClassificationRevealed
                        ? $"DILINDUNGI: {treeName} (JANGAN DITEBANG!)"
                        : $"{treeName} [Tekan E]";
                }
                else
                {
                    info = isClassificationRevealed
                        ? $"BOLEH: {treeName} (HP: {currentHP}/{maxHP}) [Tekan E]"
                        : $"{treeName} (HP: {currentHP}/{maxHP}) [Tekan E]";
                }

                GUI.Box(new Rect(Screen.width / 2 - 170, Screen.height - 80, 340, 35), info);
            }
        }

        private void OnDrawGizmos()
        {
            if (category == TreeCategory.Protected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.6f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.6f);
            }
        }
    }
}
