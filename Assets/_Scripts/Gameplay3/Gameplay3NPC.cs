using UnityEngine;

namespace Jejak.Gameplay3
{
    public enum G3NPCType
    {
        Warga,
        Mahasiswa
    }

    public class Gameplay3NPC : MonoBehaviour
    {
        [Header("NPC Profile")]
        public G3NPCType npcType = G3NPCType.Warga;
        public string npcName = "Warga Desa";
        public int maxHP = 50;
        public int currentHP = 50;

        [Header("State")]
        public bool isCollapsed = false;
        public bool isPermanentlyDead = false;
        public float reviveTimer = 15.0f;
        private const float MaxReviveTime = 15.0f;

        [Header("Follow Settings")]
        [SerializeField] private float followSpeed = 4.0f;
        [SerializeField] private float stopDistance = 2.0f;
        [SerializeField] private Vector3 followOffset = new Vector3(-1.2f, 0f, -1.5f);

        private Transform playerTransform;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Terrain cachedTerrain;

        private void Start()
        {
            if (npcType == G3NPCType.Warga)
            {
                npcName = "Warga Desa";
                followOffset = new Vector3(-1.2f, 0f, -1.5f);
            }
            else
            {
                npcName = "Mahasiswa";
                followOffset = new Vector3(1.2f, 0f, -1.5f);
            }

            maxHP = 50;
            currentHP = maxHP;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;

            StickToGround();
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

        private void StickToGround()
        {
            Terrain terrain = GetTerrain();
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                transform.position = new Vector3(transform.position.x, terrainY + 1.0f, transform.position.z);
            }
        }

        private void Update()
        {
            if (isPermanentlyDead) return;

            if (isCollapsed)
            {
                HandleReviveTimer();
                return;
            }

            if (playerTransform != null)
            {
                FollowPlayer();
            }
        }

        private void FollowPlayer()
        {
            Vector3 targetPos = playerTransform.position + playerTransform.TransformDirection(followOffset);

            Terrain terrain = GetTerrain();
            if (terrain != null)
            {
                targetPos.y = terrain.SampleHeight(targetPos) + terrain.transform.position.y + 1.0f;
            }

            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist > stopDistance)
            {
                Vector3 moveDir = (targetPos - transform.position).normalized;
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
                }
                transform.position = Vector3.MoveTowards(transform.position, targetPos, followSpeed * Time.deltaTime);
            }
            StickToGround();
        }

        public void TakeDamage(int damage)
        {
            if (isCollapsed || isPermanentlyDead) return;

            currentHP -= damage;
            Debug.Log($"<color=orange>[GP3 NPC] {npcName} terkena serudukan! HP: {currentHP}/{maxHP}</color>");

            if (currentHP <= 0)
            {
                Collapse();
            }
        }

        private void Collapse()
        {
            isCollapsed = true;
            currentHP = 0;
            reviveTimer = MaxReviveTime;

            Debug.Log($"<color=red>[GP3 NPC Tumbang] {npcName} tumbang! Waktu revive di Checkpoint: 15 detik!</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.ShowBanner($"{npcName} tumbang! Segera lari ke Checkpoint untuk memulihkannya (15s)!", 4f);
            }

            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.gray;
            }
        }

        private void HandleReviveTimer()
        {
            reviveTimer -= Time.deltaTime;

            if (reviveTimer <= 0f)
            {
                DiePermanently();
            }
        }

        private void DiePermanently()
        {
            isPermanentlyDead = true;
            isCollapsed = false;
            reviveTimer = 0f;

            Debug.Log($"<color=red>[GP3 NPC Mati Permanen] Waktu revive habis! {npcName} gugur permanen.</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.ShowBanner($"Waktu habis! {npcName} gugur permanen.", 4f);
            }

            var rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        public void ReviveAtCheckpoint()
        {
            if (isPermanentlyDead || !isCollapsed) return;

            isCollapsed = false;
            currentHP = maxHP;
            reviveTimer = MaxReviveTime;

            Debug.Log($"<color=green>[GP3 NPC Revive] {npcName} berhasil diselamatkan dan pulih kembali!</color>");

            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = true;
                rend.material.color = Color.white;
            }

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (playerTransform != null)
            {
                transform.position = playerTransform.position + followOffset;
                StickToGround();
            }
        }

        public void ResetNPC()
        {
            isCollapsed = false;
            isPermanentlyDead = false;
            currentHP = maxHP;
            reviveTimer = MaxReviveTime;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            StickToGround();

            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = true;
                rend.material.color = Color.white;
            }

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }
    }
}
