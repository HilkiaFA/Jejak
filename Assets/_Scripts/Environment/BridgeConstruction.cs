using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Jejak.Core;
using Jejak.NPC;

namespace Jejak.Environment
{
    [RequireComponent(typeof(Collider))]
    public class BridgeConstruction : MonoBehaviour
    {
        [Header("Building Phase Visuals (GameObjects)")]
        [Tooltip("Visual Fase 1: Tiang Pancang Dasar")]
        [SerializeField] private GameObject phase1Foundation;

        [Tooltip("Visual Fase 2: Rangka Balok Kayu Utama")]
        [SerializeField] private GameObject phase2Framework;

        [Tooltip("Visual Fase 3: Jembatan Lengkap (Lantai & Pagar)")]
        [SerializeField] private GameObject phase3Completed;

        [Header("Current Progress")]
        [Range(0, 3)]
        public int currentPhase = 0;
        public bool isBuildingInProgress = false;

        [Header("Recruited NPCs Checklist")]
        public bool hasWaterController = false;
        public bool hasLumberjack = false;
        public bool hasArchitect = false;

        private bool isPlayerNearby = false;
        private Collider col;

        private void Awake()
        {
            col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning("[Jembatan Warning] Collider pada jembatan belum 'Is Trigger'! Mengubah otomatis menjadi Is Trigger.");
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            SyncWithRecruitedNPCs();
            UpdateVisuals();
        }

      
        public void SyncWithRecruitedNPCs()
        {
            NPCRecruit[] npcs = FindObjectsByType<NPCRecruit>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc.isRecruited)
                {
                    NotifyNPCRecruited(npc);
                }
            }
        }

        public void NotifyNPCRecruited(NPCRecruit npc)
        {
            switch (npc.skillType)
            {
                case NPCSkillType.WaterControl:
                    hasWaterController = true;
                    Debug.Log("<color=green>[Jembatan] Syarat Fase 1 (Air Tenang) TERPENUHI!</color>");
                    break;
                case NPCSkillType.HeavyLifting:
                    hasLumberjack = true;
                    Debug.Log("<color=green>[Jembatan] Syarat Fase 2 (Kayu Jati) TERPENUHI!</color>");
                    break;
                case NPCSkillType.Architecture:
                    hasArchitect = true;
                    Debug.Log("<color=green>[Jembatan] Syarat Fase 3 (Pasak Abadi) TERPENUHI!</color>");
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNearby = true;
                SyncWithRecruitedNPCs();
                Debug.Log($"<color=cyan>[Jembatan] Pemain masuk area konstruksi! Fase: {currentPhase}, Air: {hasWaterController}, Kayu: {hasLumberjack}, Arsitek: {hasArchitect}</color>");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNearby = false;
                Debug.Log("<color=grey>[Jembatan] Pemain keluar dari area konstruksi.</color>");
            }
        }

        private void Update()
        {
            if (isPlayerNearby && !isBuildingInProgress)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    TryAdvanceBuilding();
                }
            }
        }

        private void TryAdvanceBuilding()
        {
            SyncWithRecruitedNPCs();

            if (currentPhase == 0)
            {
                if (!hasWaterController)
                {
                    Debug.LogWarning("[Jembatan] Tidak bisa membangun: Ki Tirta (Pengendali Air) belum direkrut!");
                    return;
                }
                StartCoroutine(BuildRoutine(1));
            }
            else if (currentPhase == 1)
            {
                if (!hasLumberjack)
                {
                    Debug.LogWarning("[Jembatan] Tidak bisa membangun: Bima (Penebang Kayu) belum direkrut!");
                    return;
                }
                StartCoroutine(BuildRoutine(2));
            }
            else if (currentPhase == 2)
            {
                if (!hasArchitect)
                {
                    Debug.LogWarning("[Jembatan] Tidak bisa membangun: Empu Rancang (Arsitek) belum direkrut!");
                    return;
                }
                StartCoroutine(BuildRoutine(3));
            }
            else
            {
                Debug.Log("<color=green>[Jembatan] Jembatan sudah selesai 100%! Tidurlah di kasur desa.</color>");
            }
        }

        private IEnumerator BuildRoutine(int nextPhase)
        {
            isBuildingInProgress = true;
            Debug.Log($"<color=yellow>[Jembatan] Memulai pembangunan Fase {nextPhase}... Tunggu 2 detik.</color>");

            yield return new WaitForSeconds(2.0f);

            currentPhase = nextPhase;
            UpdateVisuals();
            isBuildingInProgress = false;

            Debug.Log($"<color=green>[Jembatan Sukses] Fase {currentPhase} berhasil dibangun!</color>");

            if (currentPhase >= 3)
            {
                Debug.Log("<color=green>[Jembatan SELESAI] Jembatan kokoh selesai dibangun! Silakan tidur di kasur desa.</color>");
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.SetBridgeCompletedInPast();
                }
            }
        }

        private void UpdateVisuals()
        {
            if (phase1Foundation != null) phase1Foundation.SetActive(currentPhase >= 1);
            else if (currentPhase >= 1) Debug.LogWarning("[Jembatan Inspector] Slot 'Phase 1 Foundation' masih kosong di Inspector!");

            if (phase2Framework != null) phase2Framework.SetActive(currentPhase >= 2);
            else if (currentPhase >= 2) Debug.LogWarning("[Jembatan Inspector] Slot 'Phase 2 Framework' masih kosong di Inspector!");

            if (phase3Completed != null) phase3Completed.SetActive(currentPhase >= 3);
            else if (currentPhase >= 3) Debug.LogWarning("[Jembatan Inspector] Slot 'Phase 3 Completed' masih kosong di Inspector!");
        }

        private void OnGUI()
        {
            if (!isPlayerNearby) return;

            string statusText = "";
            if (isBuildingInProgress)
            {
                statusText = "🔨 Sedang Membangun... Harap tunggu...";
            }
            else if (currentPhase == 0)
            {
                statusText = hasWaterController 
                    ? "[Tekan E] Tanam Tiang Pancang (Air sudah ditenangkan Ki Tirta)" 
                    : " Arus terlalu deras! Cari & rekrut Ki Tirta dulu.";
            }
            else if (currentPhase == 1)
            {
                statusText = hasLumberjack 
                    ? "[Tekan E] Pasang Rangka Balok Kayu (Dibantu Bima)" 
                    : " Butuh balok kayu kokoh! Cari & rekrut Bima si Penebang.";
            }
            else if (currentPhase == 2)
            {
                statusText = hasArchitect 
                    ? "[Tekan E] Kunci Pasak Abadi (Arahan Empu Rancang)" 
                    : " Butuh teknik pasak kuno! Cari & rekrut Empu Rancang.";
            }
            else
            {
                statusText = " Jembatan sudah selesai kokoh! Pergilah ke kasur desa dan tekan [E] untuk tidur.";
            }

            GUI.Box(new Rect(Screen.width / 2 - 260, Screen.height - 110, 520, 50), statusText);
        }

        private void OnDrawGizmos()
        {
            Collider c = GetComponent<Collider>();
            if (c != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
            }
        }
    }
}
