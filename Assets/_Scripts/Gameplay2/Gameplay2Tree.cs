using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay2
{
    public enum TreeType
    {
        Ulin,
        Merbau,
        Jati,
        Bambu,
        Beringin,
        Gaharu,
        Mahoni
    }

    public enum TreeCategory
    {
        Legal,
        Protected
    }

    public class Gameplay2Tree : MonoBehaviour
    {
        [Header("Tree Type Configuration")]
        public TreeType treeType = TreeType.Ulin;
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

        private void OnValidate()
        {
            ApplyTreeTypeDefaults();
        }

        private void ApplyTreeTypeDefaults()
        {
            switch (treeType)
            {
                case TreeType.Ulin:
                    treeName = "Pohon Ulin";
                    category = TreeCategory.Legal;
                    maxHP = 150;
                    woodReward = 15;
                    seedlingReward = 1;
                    break;

                case TreeType.Merbau:
                    treeName = "Pohon Merbau";
                    category = TreeCategory.Legal;
                    maxHP = 130;
                    woodReward = 13;
                    seedlingReward = 1;
                    break;

                case TreeType.Jati:
                    treeName = "Pohon Jati";
                    category = TreeCategory.Legal;
                    maxHP = 100;
                    woodReward = 10;
                    seedlingReward = 1;
                    break;

                case TreeType.Bambu:
                    treeName = "Pohon Bambu";
                    category = TreeCategory.Legal;
                    maxHP = 70;
                    woodReward = 9;
                    seedlingReward = 1;
                    break;

                case TreeType.Beringin:
                    treeName = "Pohon Beringin";
                    category = TreeCategory.Protected;
                    maxHP = 170;
                    woodReward = 0;
                    seedlingReward = 0;
                    break;

                case TreeType.Gaharu:
                    treeName = "Pohon Gaharu";
                    category = TreeCategory.Protected;
                    maxHP = 136;
                    woodReward = 0;
                    seedlingReward = 0;
                    break;

                case TreeType.Mahoni:
                    treeName = "Pohon Mahoni";
                    category = TreeCategory.Protected;
                    maxHP = 120;
                    woodReward = 0;
                    seedlingReward = 0;
                    break;
            }
            currentHP = maxHP;
        }

        private void Start()
        {
            ApplyTreeTypeDefaults();
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
                var mouse = Mouse.current;

                bool attackTriggered = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    || Input.GetKeyDown(KeyCode.E)
                    || Input.GetMouseButtonDown(0);

                if (attackTriggered && Time.time - lastHitTime >= hitCooldown)
                {
                    HitTree();
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
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f) : Vector3.zero;

                string info = "";
                if (category == TreeCategory.Protected)
                {
                    info = isClassificationRevealed
                        ? $"<color=red><b>[DILINDUNGI]</b> {treeName}\n(JANGAN DITEBANG! Babi Hutan Mengamuk)</color>"
                        : $"{treeName}\n[Klik Kiri / E] Tebang Pohon";
                }
                else
                {
                    info = isClassificationRevealed
                        ? $"<color=green><b>[BOLEH]</b> {treeName} (HP: {currentHP}/{maxHP})\nBalok: +{woodReward} | Bibit: +{seedlingReward}</color>\n[Klik Kiri / E] Tebang Pohon"
                        : $"{treeName} (HP: {currentHP}/{maxHP})\n[Klik Kiri / E] Tebang Pohon";
                }

                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;

                float boxW = 320;
                float boxH = 55;
                float x = (screenPos.z > 0) ? screenPos.x - boxW / 2 : Screen.width / 2 - boxW / 2;
                float y = (screenPos.z > 0) ? Screen.height - screenPos.y - boxH : Screen.height - 100;

                x = Mathf.Clamp(x, 20, Screen.width - boxW - 20);
                y = Mathf.Clamp(y, 20, Screen.height - boxH - 20);

                GUI.Box(new Rect(x, y, boxW, boxH), info, style);
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
