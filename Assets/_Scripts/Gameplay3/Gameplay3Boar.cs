using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay3
{
    public class Gameplay3Boar : MonoBehaviour
    {
        [Header("Patrol & Speeds")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 5.2f;
        [SerializeField] private float detectionRadius = 8.0f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float patrolRadius = 12.0f;

        [Header("Stats")]
        [SerializeField] private int maxHP = 120;
        [SerializeField] private int currentHP = 120;
        [SerializeField] private int attackDamage = 30;

        private Vector3 startSpawnPos;
        private Vector3 currentPatrolTarget;
        private Transform currentChaseTarget;
        private float lastAttackTime = 0f;
        private float lastHitByPlayerTime = -999f;
        private bool isDead = false;
        private Terrain cachedTerrain;

        private void Start()
        {
            currentHP = maxHP;
            startSpawnPos = transform.position;
            SnapToGround();
            PickNewPatrolTarget();
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
        }

        private void Update()
        {
            if (isDead) return;

            CheckPlayerAttackInput();
            EvaluateTargets();

            if (currentChaseTarget != null)
            {
                ChaseAndAttack();
            }
            else
            {
                Patrol();
            }
        }

        private void PickNewPatrolTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            currentPatrolTarget = startSpawnPos + new Vector3(randomCircle.x, 0, randomCircle.y);

            Terrain terrain = GetTerrain();
            if (terrain != null)
            {
                currentPatrolTarget.y = terrain.SampleHeight(currentPatrolTarget) + terrain.transform.position.y + 0.5f;
            }
        }

        private void Patrol()
        {
            Vector3 diff = currentPatrolTarget - transform.position;
            diff.y = 0;

            if (diff.magnitude <= 1.0f)
            {
                PickNewPatrolTarget();
                return;
            }

            Quaternion targetRot = Quaternion.LookRotation(diff.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);

            Vector3 flatMove = new Vector3(currentPatrolTarget.x, transform.position.y, currentPatrolTarget.z);
            transform.position = Vector3.MoveTowards(transform.position, flatMove, patrolSpeed * Time.deltaTime);
            SnapToGround();
        }

        private void EvaluateTargets()
        {
            if (Gameplay3Manager.Instance == null) return;

            Transform bestTarget = null;
            float closestDist = detectionRadius;

            if (!Gameplay3Manager.Instance.IsPlayerInCheckpoint && Gameplay3Manager.Instance.playerTransform != null)
            {
                float d = Vector3.Distance(transform.position, Gameplay3Manager.Instance.playerTransform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = Gameplay3Manager.Instance.playerTransform;
                }
            }

            var warga = Gameplay3Manager.Instance.wargaNPC;
            if (warga != null && !warga.isCollapsed && !warga.isPermanentlyDead && !Gameplay3Manager.Instance.IsPlayerInCheckpoint)
            {
                float d = Vector3.Distance(transform.position, warga.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = warga.transform;
                }
            }

            var mhs = Gameplay3Manager.Instance.mahasiswaNPC;
            if (mhs != null && !mhs.isCollapsed && !mhs.isPermanentlyDead && !Gameplay3Manager.Instance.IsPlayerInCheckpoint)
            {
                float d = Vector3.Distance(transform.position, mhs.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = mhs.transform;
                }
            }

            currentChaseTarget = bestTarget;
        }

        private void ChaseAndAttack()
        {
            Vector3 dir = currentChaseTarget.position - transform.position;
            dir.y = 0;

            if (dir.magnitude > 0.2f)
            {
                Quaternion rot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 8f * Time.deltaTime);

                Vector3 target = new Vector3(currentChaseTarget.position.x, transform.position.y, currentChaseTarget.position.z);
                transform.position = Vector3.MoveTowards(transform.position, target, chaseSpeed * Time.deltaTime);
                SnapToGround();
            }

            if (Vector3.Distance(transform.position, currentChaseTarget.position) <= attackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    PerformAttack(currentChaseTarget);
                }
            }
        }

        private void PerformAttack(Transform target)
        {
            if (Gameplay3Manager.Instance != null && Gameplay3Manager.Instance.IsPlayerInCheckpoint) return;

            lastAttackTime = Time.time;

            if (target == Gameplay3Manager.Instance.playerTransform)
            {
                Gameplay3Manager.Instance.DamageMC(attackDamage);
            }
            else
            {
                Gameplay3NPC npc = target.GetComponent<Gameplay3NPC>();
                if (npc != null)
                {
                    npc.TakeDamage(attackDamage);
                }
            }
        }

        private void CheckPlayerAttackInput()
        {
            if (Gameplay3Manager.Instance == null || Gameplay3Manager.Instance.playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, Gameplay3Manager.Instance.playerTransform.position);
            if (dist > 2.5f) return;

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
            StartCoroutine(FlashRedRoutine());

            if (currentHP <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Color orig = rend.material.color;
                rend.material.color = Color.red;
                yield return new WaitForSeconds(0.12f);
                rend.material.color = orig;
            }
        }

        private void Die()
        {
            isDead = true;
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = false;

            yield return new WaitForSeconds(5.0f);

            transform.position = startSpawnPos;
            SnapToGround();
            currentHP = maxHP;
            isDead = false;
            PickNewPatrolTarget();

            if (rend != null) rend.enabled = true;
            if (col != null) col.enabled = true;
        }

        public void ResetBoar()
        {
            isDead = false;
            currentHP = maxHP;
            transform.position = startSpawnPos;
            SnapToGround();
            PickNewPatrolTarget();

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = true;
        }
    }
}
