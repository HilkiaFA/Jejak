using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay1
{
    public enum TreeSpecies
    {
        Ulin,
        Merbau,
        Jati,
        Bambu,
        Beringin,
        Gaharu,
        Mahoni
    }

    public class TreeChop : MonoBehaviour
    {
        [Header("Species Configuration")]
        [SerializeField] private TreeSpecies species = TreeSpecies.Jati;

        [Header("Interact Settings")]
        [SerializeField] private float chopCooldown = 0.4f;
        [SerializeField] private float interactDistance = 2.5f;

        [Header("Visuals")]
        [SerializeField] private GameObject treeModel;
        [SerializeField] private GameObject stumpModel;

        private int maxHP = 100;
        private int currentHP = 100;
        private int woodReward = 10;
        private int currentChops = 0;
        private int maxChops = 5;
        private bool isFelled = false;
        private bool isPlayerNearby = false;
        private float lastChopTime = 0f;
        private Transform playerTransform;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public TreeSpecies Species => species;
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public bool IsFelled => isFelled;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            ConfigureSpeciesStats();
        }

        private void Start()
        {
            FindPlayer();
            ResetTree();
        }

        private void ConfigureSpeciesStats()
        {
            switch (species)
            {
                case TreeSpecies.Ulin:
                    maxHP = 150;
                    woodReward = 15;
                    maxChops = 8;
                    break;
                case TreeSpecies.Merbau:
                    maxHP = 130;
                    woodReward = 13;
                    maxChops = 7;
                    break;
                case TreeSpecies.Jati:
                    maxHP = 100;
                    woodReward = 10;
                    maxChops = 5;
                    break;
                case TreeSpecies.Bambu:
                    maxHP = 70;
                    woodReward = 9;
                    maxChops = 4;
                    break;
                case TreeSpecies.Beringin:
                    maxHP = 170;
                    woodReward = 20;
                    maxChops = 9;
                    break;
                case TreeSpecies.Gaharu:
                    maxHP = 136;
                    woodReward = 19;
                    maxChops = 7;
                    break;
                case TreeSpecies.Mahoni:
                    maxHP = 120;
                    woodReward = 16;
                    maxChops = 6;
                    break;
            }
            currentHP = maxHP;
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
                var mouse = Mouse.current;

                bool attackTriggered = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    || Input.GetKeyDown(KeyCode.E)
                    || Input.GetMouseButtonDown(0);

                if (attackTriggered)
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
            currentHP -= 20;
            if (currentHP < 0) currentHP = 0;

            StartCoroutine(ShakeTreeRoutine());

            Debug.Log($"<color=yellow>[Tebang Pohon] {species} ({gameObject.name}): Pukulan {currentChops}/{maxChops} - Sisa HP: {currentHP}/{maxHP}</color>");

            if (currentHP <= 0 || currentChops >= maxChops)
            {
                FellTree();
            }
        }

        private IEnumerator ShakeTreeRoutine()
        {
            Vector3 originalPos = transform.position;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float xOffset = Random.Range(-0.06f, 0.06f);
                float zOffset = Random.Range(-0.06f, 0.06f);
                transform.position = originalPos + new Vector3(xOffset, 0f, zOffset);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = originalPos;
        }

        private void FellTree()
        {
            isFelled = true;
            Debug.Log($"<color=green>[Pohon Tumbang] {species} tumbang! Mendapatkan {woodReward} Balok.</color>");

            if (Gameplay1Manager.Instance != null)
            {
                Gameplay1Manager.Instance.AddWood(woodReward);
            }

            StartCoroutine(FallAnimationRoutine());
        }

        private IEnumerator FallAnimationRoutine()
        {
            float fallDuration = 1.0f;
            float elapsed = 0f;
            Quaternion initialRot = transform.rotation;
            Quaternion targetRot = initialRot * Quaternion.Euler(75f, 0f, 0f);

            while (elapsed < fallDuration)
            {
                transform.rotation = Quaternion.Slerp(initialRot, targetRot, elapsed / fallDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (treeModel != null) treeModel.SetActive(false);
            if (stumpModel != null) stumpModel.SetActive(true);

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        public void ResetTree()
        {
            isFelled = false;
            currentChops = 0;
            ConfigureSpeciesStats();

            transform.position = initialPosition;
            transform.rotation = initialRotation;

            if (treeModel != null) treeModel.SetActive(true);
            if (stumpModel != null) stumpModel.SetActive(false);

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        private void OnGUI()
        {
            if (isPlayerNearby && !isFelled)
            {
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f) : Vector3.zero;

                if (screenPos.z > 0)
                {
                    GUIStyle style = new GUIStyle(GUI.skin.box);
                    style.fontSize = 13;
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.yellow;
                    style.alignment = TextAnchor.MiddleCenter;

                    string promptText = $"[Klik Kiri / E] Tebang Pohon {species}\nHP: {currentHP}/{maxHP} (+{woodReward} Balok)";
                    GUI.Box(new Rect(screenPos.x - 120, Screen.height - screenPos.y - 45, 240, 50), promptText, style);
                }
            }
        }
    }
}
