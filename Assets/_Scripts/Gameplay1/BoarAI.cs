using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay1
{
    public enum BoarState
    {
        Guarding,
        Chasing,
        Invading,
        Returning,
        Dead
    }

    public class BoarAI : MonoBehaviour
    {
        [Header("State & Speeds")]
        public BoarState currentState = BoarState.Guarding;
        [SerializeField] private float walkSpeed = 3.0f;
        [SerializeField] private float chaseSpeed = 5.5f;
        [SerializeField] private float rotationSpeed = 8.0f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Stats")]
        [SerializeField] private int maxHP = 120;
        [SerializeField] private int currentHP = 120;
        [SerializeField] private int attackDamage = 30;

        [Header("Positions")]
        [SerializeField] private Transform nestTransform;
        [SerializeField] private Transform invasionTargetPoint;

        private Transform playerTransform;
        private Vector3 startNestPosition;
        private Quaternion startNestRotation;
        private float lastAttackTime = -999f;
        private float lastHitByPlayerTime = -999f;
        private bool isPlayerNearby = false;
        private Terrain cachedTerrain;

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;

        private void Start()
        {
            currentHP = maxHP;

            SpreadOverlappingBoars();
            StickToGround();

            if (nestTransform != null)
            {
                startNestPosition = nestTransform.position;
                startNestRotation = nestTransform.rotation;
            }
            else
            {
                startNestPosition = transform.position;
                startNestRotation = transform.rotation;
            }

            FindPlayer();
        }

        private void SpreadOverlappingBoars()
        {
            Collider[] overlaps = Physics.OverlapSphere(transform.position, 0.5f);
            foreach (var col in overlaps)
            {
                if (col.gameObject != gameObject && col.GetComponent<BoarAI>() != null)
                {
                    int index = transform.GetSiblingIndex();
                    float angle = index * 72f * Mathf.Deg2Rad;
                    transform.position += new Vector3(Mathf.Cos(angle) * 1.5f, 0f, Mathf.Sin(angle) * 1.5f);
                    break;
                }
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

        private void StickToGround()
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
                    if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform) && !hit.collider.CompareTag("Player") && hit.collider.GetComponent<BoarAI>() == null)
                    {
                        transform.position = new Vector3(transform.position.x, hit.point.y + 0.5f, transform.position.z);
                    }
                }
            }
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
            if (currentState == BoarState.Dead) return;

            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float distToPlayer = Vector3.Distance(playerTransform.position, transform.position);
            isPlayerNearby = (distToPlayer <= 2.5f);

            CheckPlayerAttackInput();
            EvaluateState();
            ExecuteMovement();

            if (currentState == BoarState.Guarding)
            {
                StickToGround();
            }
        }

        private void CheckPlayerAttackInput()
        {
            if (!isPlayerNearby) return;

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
            currentHP -= damage;
            Debug.Log($"<color=orange>[Serang Babi] Babi hutan terkena kapak (-{damage} HP)! Sisa HP: {currentHP}/{maxHP}</color>");

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
            currentState = BoarState.Dead;
            Debug.Log("<color=green>[Babi Hutan Mati] Babi hutan tumbang! Akan respawn dalam 5 detik.</color>");

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = false;

            yield return new WaitForSeconds(5.0f);

            transform.position = startNestPosition;
            transform.rotation = startNestRotation;
            StickToGround();
            currentHP = maxHP;
            currentState = BoarState.Guarding;

            if (rend != null) rend.enabled = true;
            if (col != null) col.enabled = true;

            Debug.Log("<color=cyan>[Babi Hutan Respawn] Babi hutan telah muncul kembali di sarang.</color>");
        }

        private void EvaluateState()
        {
            if (Gameplay1Manager.Instance == null) return;

            var currentPhase = Gameplay1Manager.Instance.CurrentPhase;
            bool isPlayerInUnsafe = Gameplay1Manager.Instance.IsPlayerInUnsafeZone;
            bool isPlayerInCheckpoint = Gameplay1Manager.Instance.IsPlayerInCheckpoint;

            if (currentPhase == GameplayPhase.Phase1_SafeForest)
            {
                if (isPlayerInUnsafe)
                {
                    currentState = BoarState.Chasing;
                }
                else
                {
                    if (currentState == BoarState.Chasing)
                    {
                        currentState = BoarState.Returning;
                    }
                    else if (currentState == BoarState.Returning)
                    {
                        if (Vector3.Distance(transform.position, startNestPosition) < 0.5f)
                        {
                            currentState = BoarState.Guarding;
                        }
                    }
                }
            }
            else if (currentPhase == GameplayPhase.Phase2_Invasion)
            {
                if (isPlayerInCheckpoint)
                {
                    currentState = BoarState.Invading;
                }
                else
                {
                    currentState = BoarState.Chasing;
                }
            }
            else if (currentPhase == GameplayPhase.CycleTransition)
            {
                currentState = BoarState.Returning;
            }
        }

        private void ExecuteMovement()
        {
            switch (currentState)
            {
                case BoarState.Guarding:
                    transform.rotation = Quaternion.Slerp(transform.rotation, startNestRotation, rotationSpeed * Time.deltaTime);
                    break;

                case BoarState.Chasing:
                    if (playerTransform != null)
                    {
                        MoveTowardsTarget(playerTransform.position, chaseSpeed);

                        if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange)
                        {
                            if (Time.time - lastAttackTime >= attackCooldown)
                            {
                                AttackPlayer();
                            }
                        }
                    }
                    break;

                case BoarState.Invading:
                    Vector3 targetPos = (invasionTargetPoint != null) ? invasionTargetPoint.position : Vector3.zero;
                    MoveTowardsTarget(targetPos, walkSpeed);
                    break;

                case BoarState.Returning:
                    MoveTowardsTarget(startNestPosition, walkSpeed);
                    if (Vector3.Distance(transform.position, startNestPosition) < 0.5f)
                    {
                        currentState = BoarState.Guarding;
                    }
                    break;
            }
        }

        private void MoveTowardsTarget(Vector3 targetPos, float speed)
        {
            Vector3 direction = (targetPos - transform.position);
            direction.y = 0;

            if (direction.magnitude > 0.2f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

                Vector3 target = new Vector3(targetPos.x, transform.position.y, targetPos.z);
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                StickToGround();
            }
        }

        private void AttackPlayer()
        {
            if (Gameplay1Manager.Instance != null && Gameplay1Manager.Instance.IsPlayerInCheckpoint)
            {
                return;
            }

            lastAttackTime = Time.time;
            Debug.Log($"<color=red>[Babi Hutan] Menyeruduk MC (-{attackDamage} HP)!</color>");

            if (Gameplay1Manager.Instance != null)
            {
                Gameplay1Manager.Instance.TakeMCDamage(attackDamage);
            }

            if (playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null)
                {
                    Vector3 pushDir = (playerTransform.position - transform.position).normalized;
                    pushDir.y = 0;
                    cc.Move(pushDir * 2.5f);
                }
            }
        }

        public void ResetToNest()
        {
            StopAllCoroutines();
            transform.position = startNestPosition;
            transform.rotation = startNestRotation;
            StickToGround();
            currentHP = maxHP;
            currentState = BoarState.Guarding;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = true;
        }

        private void OnGUI()
        {
            if (isPlayerNearby && currentState != BoarState.Dead)
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
