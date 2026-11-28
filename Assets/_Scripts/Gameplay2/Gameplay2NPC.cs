using UnityEngine;

namespace Jejak.Gameplay2
{
    public enum NPCType
    {
        Warga,
        Mahasiswa
    }

    public class Gameplay2NPC : MonoBehaviour
    {
        [Header("NPC Profile")]
        public NPCType npcType = NPCType.Warga;
        public string npcName = "Warga Desa";
        public int maxHP = 50;
        public int currentHP = 50;
        public int attackDamage = 3;

        [Header("Status")]
        public bool isRecruited = false;
        public bool isDead = false;

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
            if (npcType == NPCType.Warga)
            {
                npcName = "Warga Desa";
                maxHP = 50;
                attackDamage = 3;
                followOffset = new Vector3(-1.2f, 0f, -1.5f);
            }
            else
            {
                npcName = "Mahasiswa";
                maxHP = 50;
                attackDamage = 10;
                followOffset = new Vector3(1.2f, 0f, -1.5f);
            }

            currentHP = maxHP;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            ResetNPC();

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
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
            if (isDead) return;

            if (isRecruited && playerTransform != null)
            {
                FollowPlayer();
            }
            else
            {
                StickToGround();
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

        public void TakeDamage(int dmg)
        {
            if (isDead) return;

            currentHP -= dmg;
            Debug.Log($"<color=orange>[NPC] {npcName} terkena serudukan babi! HP: {currentHP}/{maxHP}</color>");

            if (currentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            isRecruited = false;
            currentHP = 0;
            Debug.Log($"<color=red>[NPC Mati] {npcName} tewas diseruduk babi hutan!</color>");

            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.ShowBanner($"{npcName} gugur diserang babi!", 3.5f);
            }

            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        public void Recruit()
        {
            isRecruited = true;
            Debug.Log($"<color=green>[Rekrut Sukses] {npcName} bergabung membantu MC!</color>");

            if (npcType == NPCType.Warga)
            {
                if (Gameplay2Manager.Instance != null)
                {
                    foreach (var tree in Gameplay2Manager.Instance.trees)
                    {
                        if (tree != null) tree.RevealClassification();
                    }
                }
            }
        }

        public void ResetNPC()
        {
            isRecruited = false;
            isDead = false;
            currentHP = maxHP;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            StickToGround();

            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = true;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }
    }
}
