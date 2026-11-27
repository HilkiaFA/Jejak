using UnityEngine;

namespace Jejak.Gameplay2
{
    public class Gameplay2Boar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 5.0f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private int attackDamage = 30;
        [SerializeField] private float attackCooldown = 1.2f;

        private Vector3 startNestPos;
        private Quaternion startNestRot;
        private bool isInvading = false;
        private float lastAttackTime = 0f;

        private void Start()
        {
            startNestPos = transform.position;
            startNestRot = transform.rotation;
        }

        private void Update()
        {
            if (Gameplay2Manager.Instance == null) return;

            if (isInvading)
            {
                ChaseAndAttackTargets();
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, startNestPos, 3f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, startNestRot, 5f * Time.deltaTime);
            }
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
            transform.position = startNestPos;
            transform.rotation = startNestRot;
        }
    }
}
