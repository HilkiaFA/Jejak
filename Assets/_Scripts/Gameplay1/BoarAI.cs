using UnityEngine;

namespace Jejak.Gameplay1
{
    public enum BoarState
    {
        Guarding,
        Chasing,
        Invading,
        Returning
    }

    public class BoarAI : MonoBehaviour
    {
        [Header("State & Speeds")]
        public BoarState currentState = BoarState.Guarding;
        [SerializeField] private float walkSpeed = 3.0f;
        [SerializeField] private float chaseSpeed = 5.5f;
        [SerializeField] private float rotationSpeed = 8.0f;
        [SerializeField] private float attackRange = 1.2f;

        [Header("Positions")]
        [SerializeField] private Transform nestTransform;
        [SerializeField] private Transform invasionTargetPoint;

        private Transform playerTransform;
        private Vector3 startNestPosition;
        private Quaternion startNestRotation;

        private void Start()
        {
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

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                return;
            }

            EvaluateState();
            ExecuteMovement();
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
                            AttackPlayer();
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
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPos.x, transform.position.y, targetPos.z), speed * Time.deltaTime);
            }
        }

        private void AttackPlayer()
        {
            if (Gameplay1Manager.Instance != null && Gameplay1Manager.Instance.IsPlayerInCheckpoint)
            {
                return;
            }

            Debug.Log("<color=red>[Babi Hutan] Menyeruduk MC!</color>");
            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.TriggerHitFlash();
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
            transform.position = startNestPosition;
            transform.rotation = startNestRotation;
            currentState = BoarState.Guarding;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
