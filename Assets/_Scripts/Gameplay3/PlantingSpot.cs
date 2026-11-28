using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay3
{
    public class PlantingSpot : MonoBehaviour
    {
        [Header("Planting State")]
        public bool isPlanted = false;
        [SerializeField] private float interactRange = 2.5f;
        [SerializeField] private GameObject saplingVisual;

        private Transform playerTransform;
        private bool isPlayerNearby = false;
        private Terrain cachedTerrain;

        private void Start()
        {
            SnapToGround();

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;

            if (saplingVisual != null)
            {
                saplingVisual.SetActive(isPlanted);
            }
        }

        private Terrain GetTerrain()
        {
            if (cachedTerrain == null)
            {
                cachedTerrain = Terrain.activeTerrain;
                if (cachedTerrain == null)
                {
                    cachedTerrain = Object.FindFirstObjectByType<Terrain>();
                }
            }
            return cachedTerrain;
        }

        private void SnapToGround()
        {
            Terrain terrain = GetTerrain();
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                transform.position = new Vector3(transform.position.x, terrainY + 0.1f, transform.position.z);
            }
        }

        private void Update()
        {
            if (isPlanted) return;

            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                return;
            }

            Vector3 diff = playerTransform.position - transform.position;
            diff.y = 0;
            isPlayerNearby = (diff.magnitude <= interactRange);

            if (isPlayerNearby)
            {
                var kb = Keyboard.current;
                bool ePressed = (kb != null && kb.eKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.E);

                if (ePressed)
                {
                    TryPlant();
                }
            }
        }

        private void TryPlant()
        {
            if (Gameplay3Manager.Instance == null) return;

            if (Gameplay3Manager.Instance.CanPlant())
            {
                isPlanted = true;
                isPlayerNearby = false;

                if (saplingVisual != null)
                {
                    saplingVisual.SetActive(true);
                }
                else
                {
                    CreateDefaultSapling();
                }

                Gameplay3Manager.Instance.OnSeedlingPlanted(this);
            }
        }

        private void CreateDefaultSapling()
        {
            GameObject sapling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sapling.name = "Sapling_Trunk";
            sapling.transform.SetParent(transform);
            sapling.transform.localPosition = new Vector3(0, 0.4f, 0);
            sapling.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);

            var trunkRend = sapling.GetComponent<Renderer>();
            if (trunkRend != null) trunkRend.material.color = new Color(0.45f, 0.25f, 0.1f);

            Collider col = sapling.GetComponent<Collider>();
            if (col != null) Destroy(col);

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Sapling_Leaves";
            leaves.transform.SetParent(transform);
            leaves.transform.localPosition = new Vector3(0, 0.9f, 0);
            leaves.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var leavesRend = leaves.GetComponent<Renderer>();
            if (leavesRend != null) leavesRend.material.color = new Color(0.15f, 0.75f, 0.2f);

            Collider col2 = leaves.GetComponent<Collider>();
            if (col2 != null) Destroy(col2);

            saplingVisual = sapling;
        }

        public void ResetSpot()
        {
            isPlanted = false;
            if (saplingVisual != null)
            {
                saplingVisual.SetActive(false);
            }
        }

        private void OnGUI()
        {
            if (isPlayerNearby && !isPlanted)
            {
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f) : Vector3.zero;

                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 13;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                style.alignment = TextAnchor.MiddleCenter;

                float boxW = 240;
                float boxH = 40;
                float px = (screenPos.z > 0) ? screenPos.x - boxW / 2 : Screen.width / 2 - boxW / 2;
                float py = (screenPos.z > 0) ? Screen.height - screenPos.y - boxH : Screen.height - 120;

                px = Mathf.Clamp(px, 20, Screen.width - boxW - 20);
                py = Mathf.Clamp(py, 20, Screen.height - boxH - 20);

                GUI.Box(new Rect(px, py, boxW, boxH), "[Tekan E] Tanam Bibit Pohon", style);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isPlanted ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.3f, 0.8f);
        }
    }
}
