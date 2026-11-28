using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay2
{
    public class Gameplay2Boar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 5.0f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private int attackDamage = 30;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Stats")]
        [SerializeField] private int maxHP = 120;
        [SerializeField] private int currentHP = 120;

        private Vector3 startNestPos;
        private Quaternion startNestRot;
        private bool isInvading = false;
        private float lastAttackTime = 0f;
        private float lastHitByPlayerTime = -999f;
        private bool isDead = false;
        private Terrain cachedTerrain;

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;

        private void Start()
        {
            currentHP = maxHP;
            SnapToGround();
            startNestPos = transform.position;
            startNestRot = transform.rotation;
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
                transform.position = new Vector3(transform.position.x, terrainY + 0.5f, transform.position.z);

                if (terrain.terrainData != null)
                {
                    Vector3 tPos = terrain.transform.position;
                    Vector3 tSize = terrain.terrainData.size;
                    float normX = (transform.position.x - tPos.x) / tSize.x;
                    float normZ = (transform.position.z - tPos.z) / tSize.z;

                    if (normX >= 0f && normX <= 1f && normZ >= 0f && normZ <= 1f)
                    {
                        Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(normX, normZ);
                        Vector3 forward = transform.forward;
                        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, terrainNormal);
                        if (projectedForward.sqrMagnitude > 0.001f)
                        {
                            transform.rotation = Quaternion.LookRotation(projectedForward.normalized, terrainNormal);
                        }
                    }
                }
            }
            else
            {
                RaycastHit hit;
                int layerMask = LayerMask.GetMask("Default", "Terrain");
                if (Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 5f, transform.position.z), Vector3.down, out hit, 50f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform) && !hit.collider.CompareTag("Player") && hit.collider.GetComponent<Gameplay2Boar>() == null)
                    {
                        transform.position = new Vector3(transform.position.x, hit.point.y + 0.5f, transform.position.z);
                    }
                }
            }
        }

        private void Update()
        {
            if (isDead) return;
            if (Gameplay2Manager.Instance == null) return;

            CheckPlayerAttackInput();

            if (isInvading)
            {
                ChaseAndAttackTargets();
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, startNestPos, 3f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, startNestRot, 5f * Time.deltaTime);
                SnapToGround();
            }
        }

        private void CheckPlayerAttackInput()
        {
            if (Gameplay2Manager.Instance == null || Gameplay2Manager.Instance.playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, Gameplay2Manager.Instance.playerTransform.position);
            if (distToPlayer > 2.5f) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            bool attackTriggered = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || Input.GetKeyDown(KeyCode.E)
                || Input.GetMouseButtonDown(0);

            if (attackTriggered && Time.time - lastHitByPlayerTime >= 0.4f)
            {
                lastHitByPlayerTime = Time.time;
                TakeDamageFromPlayer(20);
            }
        }

        public void TakeDamageFromPlayer(int damage)
        {
            if (isDead) return;

            currentHP -= damage;
            Debug.Log($"<color=orange>[Serang Babi G2] Babi terkena kapak (-{damage} HP)! Sisa: {currentHP}/{maxHP}</color>");

            StartCoroutine(FlashRedRoutine());

            if (currentHP <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.red;
                yield return new WaitForSeconds(0.12f);
                renderer.material.color = originalColor;
            }
        }

        private void Die()
        {
            isDead = true;
            Debug.Log("<color=green>[Babi Mati G2] Babi hutan tumbang! Respawn dalam 5 detik.</color>");

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = false;

            yield return new WaitForSeconds(5.0f);

            transform.position = startNestPos;
            transform.rotation = startNestRot;
            SnapToGround();
            currentHP = maxHP;
            isDead = false;

            if (rend != null) rend.enabled = true;
            if (col != null) col.enabled = true;

            Debug.Log("<color=cyan>[Babi Respawn G2] Babi hutan telah muncul kembali di sarang.</color>");
        }

        private void ChaseAndAttackTargets()
        {
            Transform bestTarget = null;
            float closestDist = 999f;

            if (!Gameplay2Manager.Instance.isPlayerInCheckpoint && Gameplay2Manager.Instance.playerTransform != null)
            {
                float d = Vector3.Distance(transform.position, Gameplay2Manager.Instance.playerTransform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = Gameplay2Manager.Instance.playerTransform;
                }
            }

            var warga = Gameplay2Manager.Instance.wargaNPC;
            if (warga != null && warga.isRecruited && !warga.isDead && !Gameplay2Manager.Instance.isPlayerInCheckpoint)
            {
                float d = Vector3.Distance(transform.position, warga.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = warga.transform;
                }
            }

            var mhs = Gameplay2Manager.Instance.mahasiswaNPC;
            if (mhs != null && mhs.isRecruited && !mhs.isDead && !Gameplay2Manager.Instance.isPlayerInCheckpoint)
            {
                float d = Vector3.Distance(transform.position, mhs.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = mhs.transform;
                }
            }

            if (bestTarget != null)
            {
                Vector3 dir = (bestTarget.position - transform.position);
                dir.y = 0;

                if (dir.magnitude > 0.2f)
                {
                    Quaternion rot = Quaternion.LookRotation(dir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot, 8f * Time.deltaTime);
                    Vector3 flatTarget = new Vector3(bestTarget.position.x, transform.position.y, bestTarget.position.z);
                    transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);
                    SnapToGround();
                }

                if (Vector3.Distance(transform.position, bestTarget.position) <= attackRange)
                {
                    if (Time.time - lastAttackTime >= attackCooldown)
                    {
                        PerformAttack(bestTarget);
                    }
                }
            }
        }

        private void PerformAttack(Transform target)
        {
            if (Gameplay2Manager.Instance != null && Gameplay2Manager.Instance.isPlayerInCheckpoint)
            {
                return;
            }

            lastAttackTime = Time.time;

            if (target == Gameplay2Manager.Instance.playerTransform)
            {
                Gameplay2Manager.Instance.DamageMC(attackDamage);
            }
            else
            {
                Gameplay2NPC npc = target.GetComponent<Gameplay2NPC>();
                if (npc != null)
                {
                    npc.TakeDamage(attackDamage);
                }
            }
        }

        public void StartInvasion()
        {
            isInvading = true;
        }

        public void ResetToNest()
        {
            isInvading = false;
            isDead = false;
            currentHP = maxHP;
            transform.position = startNestPos;
            transform.rotation = startNestRot;
            SnapToGround();

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = true;
        }

        private void OnGUI()
        {
            if (isDead) return;
            if (Gameplay2Manager.Instance == null || Gameplay2Manager.Instance.playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, Gameplay2Manager.Instance.playerTransform.position);
            if (dist <= 2.5f)
            {
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.2f) : Vector3.zero;

                if (screenPos.z > 0)
                {
                    GUIStyle style = new GUIStyle(GUI.skin.box);
                    style.fontSize = 12;
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.red;
                    style.alignment = TextAnchor.MiddleCenter;

                    string promptText = $"Babi Hutan\nHP: {currentHP}/{maxHP}\n[Klik Kiri / E] Pukul Kapak (-20)";
                    GUI.Box(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 45, 200, 50), promptText, style);
                }
            }
        }
    }
}
