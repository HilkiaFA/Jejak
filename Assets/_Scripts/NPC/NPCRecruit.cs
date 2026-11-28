using UnityEngine;
using UnityEngine.InputSystem;
using Jejak.Environment;

namespace Jejak.NPC
{
    public enum NPCSkillType
    {
        WaterControl,
        HeavyLifting,
        Architecture
    }

    [RequireComponent(typeof(Collider))]
    public class NPCRecruit : MonoBehaviour
    {
        [Header("NPC Profile")]
        public string npcName = "Ki Tirta";
        public NPCSkillType skillType = NPCSkillType.WaterControl;
        [TextArea(2, 4)]
        public string dialogueBeforeRecruit = "Arus sungai ini terlalu ganas, tiang pancang tidak akan bisa ditancapkan. Aku bisa menghentikan arusnya jika kau butuh!";
        [TextArea(2, 4)]
        public string dialogueAfterRecruit = "Aku sudah siap membantu di lokasi jembatan!";

        [Header("Status & Work Location")]
        public bool isRecruited = false;
        [SerializeField] private Transform constructionWorkSpot;

        [Header("Bridge Reference")]
        [SerializeField] private BridgeConstruction bridgeConstruction;

        private bool isPlayerNearby = false;

        private void Awake()
        {
            if (bridgeConstruction == null)
            {
                bridgeConstruction = FindFirstObjectByType<BridgeConstruction>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNearby = true;
                Debug.Log($"<color=cyan>[NPC] Mendekati {npcName}. Tekan [E] untuk bicara.</color>");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNearby = false;
            }
        }

        private void Update()
        {
            if (isPlayerNearby && !isRecruited)
            {
                var keyboard = Keyboard.current;
                bool ePressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.E);
                if (ePressed)
                {
                    RecruitThisNPC();
                }
            }
        }

        private void RecruitThisNPC()
        {
            isRecruited = true;
            Debug.Log($"<color=green>[NPC Sukses] {npcName} ({skillType}) berhasil direkrut!</color>");

            if (bridgeConstruction == null)
            {
                bridgeConstruction = FindFirstObjectByType<BridgeConstruction>();
            }

            if (bridgeConstruction != null)
            {
                bridgeConstruction.NotifyNPCRecruited(this);
                Debug.Log($"<color=green>[NPC] Laporan bantuan {npcName} sudah dikirim ke Jembatan!</color>");
            }
            else
            {
                Debug.LogError("[NPC Error] Tidak menemukan objek BridgeConstruction di Scene! Pastikan skrip BridgeConstruction terpasang pada jembatan.");
            }

            if (constructionWorkSpot != null)
            {
                transform.position = constructionWorkSpot.position;
                transform.rotation = constructionWorkSpot.rotation;
            }
        }

        private void OnGUI()
        {
            if (isPlayerNearby)
            {
                string text = isRecruited
                    ? $"{npcName}: \"{dialogueAfterRecruit}\""
                    : $"{npcName}: \"{dialogueBeforeRecruit}\"\n[Tekan E untuk Minta Tolong]";

                GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height - 130, 500, 65), text);
            }
        }
    }
}
