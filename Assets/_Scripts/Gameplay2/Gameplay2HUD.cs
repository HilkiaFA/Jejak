using UnityEngine;
using UnityEngine.InputSystem;

namespace Jejak.Gameplay2
{
    public class Gameplay2HUD : MonoBehaviour
    {
        public static Gameplay2HUD Instance { get; private set; }

        private float redBorderAlpha = 0f;
        private string bannerMessage = "";
        private float bannerTimer = 0f;

        private Gameplay2NPC currentInteractingNPC = null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
                if (bannerTimer <= 0f) bannerMessage = "";
            }

            if (redBorderAlpha > 0f)
            {
                redBorderAlpha = Mathf.MoveTowards(redBorderAlpha, 0f, 1.5f * Time.deltaTime);
            }

            CheckNPCProximity();

            if (currentInteractingNPC != null)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                    {
                        SelectOption(1);
                    }
                    else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                    {
                        SelectOption(2);
                    }
                }
            }

            if (Gameplay2Manager.Instance != null &&
                Gameplay2Manager.Instance.currentWood >= Gameplay2Manager.Instance.targetWood &&
                Gameplay2Manager.Instance.isPlayerInCheckpoint &&
                Gameplay2Manager.Instance.currentPhase != G2Phase.Finished)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame)
                {
                    Gameplay2Manager.Instance.SubmitWoodAndBuildBridge();
                }
            }
        }

        private void CheckNPCProximity()
        {
            if (Gameplay2Manager.Instance == null || Gameplay2Manager.Instance.playerTransform == null) return;

            Transform pt = Gameplay2Manager.Instance.playerTransform;
            currentInteractingNPC = null;

            var warga = Gameplay2Manager.Instance.wargaNPC;
            if (warga != null && !warga.isRecruited && !warga.isDead)
            {
                if (Vector3.Distance(pt.position, warga.transform.position) <= 2.5f)
                {
                    currentInteractingNPC = warga;
                    return;
                }
            }

            var mhs = Gameplay2Manager.Instance.mahasiswaNPC;
            if (mhs != null && !mhs.isRecruited && !mhs.isDead)
            {
                if (Vector3.Distance(pt.position, mhs.transform.position) <= 2.5f)
                {
                    currentInteractingNPC = mhs;
                    return;
                }
            }
        }

        public void SelectOption(int option)
        {
            if (currentInteractingNPC == null) return;

            if (option == 1)
            {
                currentInteractingNPC.Recruit();
                ShowBanner($" {currentInteractingNPC.npcName} Bergabung!", 3f);
            }
            else
            {
                ShowBanner("Dialog ditutup.", 1.5f);
            }
            currentInteractingNPC = null;
        }

        public void ShowBanner(string msg, float duration = 3.5f)
        {
            bannerMessage = msg;
            bannerTimer = duration;
        }

        public void TriggerHitFlash()
        {
            redBorderAlpha = 0.8f;
        }

        private void OnGUI()
        {
            if (Gameplay2Manager.Instance == null) return;
            if (!Gameplay2Manager.Instance.gameObject.activeInHierarchy) return;

            if (redBorderAlpha > 0.05f)
            {
                Color oldC = GUI.color;
                GUI.color = new Color(1f, 0f, 0f, redBorderAlpha);
                int bw = 24;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, bw), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, Screen.height - bw, Screen.width, bw), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, 0, bw, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - bw, 0, bw, Screen.height), Texture2D.whiteTexture);
                GUI.color = oldC;
            }

            int wood = Gameplay2Manager.Instance.currentWood;
            int targetWood = Gameplay2Manager.Instance.targetWood;
            int seedlings = Gameplay2Manager.Instance.currentSeedlings;
            float timer = Gameplay2Manager.Instance.remainingTime;

            int min = Mathf.FloorToInt(timer / 60f);
            int sec = Mathf.FloorToInt(timer % 60f);
            string timeStr = string.Format("{0:00}:{1:00}", min, sec);

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 14;
            boxStyle.fontStyle = FontStyle.Bold;
            boxStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginArea(new Rect(20, 20, 280, 160), boxStyle);
            GUILayout.Space(4);
            GUILayout.Label($"Balok : <b>{wood} / {targetWood}</b> | Bibit : <b>{seedlings}</b>");

            if (Gameplay2Manager.Instance.currentPhase == G2Phase.InvasionPeriod || Gameplay2Manager.Instance.currentPhase == G2Phase.EnragedInvasion)
            {
                GUI.color = Color.red;
                GUILayout.Label($"INVASI : <b>{timeStr}</b>");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label($"Waktu Aman : <b>{timeStr}</b>");
            }

            GUILayout.Label($"HP MC : <b>{Gameplay2Manager.Instance.mcCurrentHP}/{Gameplay2Manager.Instance.mcMaxHP}</b>");

            var w = Gameplay2Manager.Instance.wargaNPC;
            string wStatus = (w == null) ? "Tidak Ada" : w.isDead ? "<color=red>Gugur</color>" : w.isRecruited ? $"<color=green>{w.currentHP}/{w.maxHP}</color>" : "<color=yellow>Belum Rekrut</color>";
            GUILayout.Label($"Warga : <b>{wStatus}</b> (Damage +3)");

            var m = Gameplay2Manager.Instance.mahasiswaNPC;
            string mStatus = (m == null) ? "Tidak Ada" : m.isDead ? "<color=red>Gugur</color>" : m.isRecruited ? $"<color=green>{m.currentHP}/{m.maxHP}</color>" : "<color=yellow>Belum Rekrut</color>";
            GUILayout.Label($"Mahasiswa : <b>{mStatus}</b> (Damage +10)");
            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(bannerMessage))
            {
                GUIStyle bStyle = new GUIStyle(GUI.skin.box);
                bStyle.fontSize = 18;
                bStyle.fontStyle = FontStyle.Bold;
                bStyle.alignment = TextAnchor.MiddleCenter;

                GUI.color = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2 - 270, 30, 540, 60), bannerMessage, bStyle);
                GUI.color = Color.white;
            }

            if (currentInteractingNPC != null)
            {
                GUIStyle dBox = new GUIStyle(GUI.skin.box);
                dBox.fontSize = 15;
                dBox.fontStyle = FontStyle.Bold;

                GUILayout.BeginArea(new Rect(Screen.width / 2 - 250, Screen.height - 210, 500, 180), dBox);
                GUILayout.Space(8);

                if (currentInteractingNPC.npcType == NPCType.Warga)
                {
                    GUILayout.Label("Warga Desa:");
                    GUILayout.Label("\"Mau menebang pohon lagi, Nak? Hutan ini sudah banyak yang rusak.\"");
                    GUILayout.Space(10);
                    if (GUILayout.Button("1. [Ajak Kerja Sama] \"Saya mau perbaiki jembatan tanpa merusak hutan. Mau bantu?\"", GUILayout.Height(36)))
                    {
                        SelectOption(1);
                    }
                    if (GUILayout.Button("2. [Tidak Jadi] \"Saya jalan sendiri saja, Pak.\"", GUILayout.Height(32)))
                    {
                        SelectOption(2);
                    }
                }
                else
                {
                    GUILayout.Label("Mahasiswa:");
                    GUILayout.Label("\"Kak, mau bangun jembatan? Aku bisa bantu nebang.\"");
                    GUILayout.Space(10);
                    if (GUILayout.Button("1. [Ajak Kerja Sama] \"Boleh. Tapi kita hanya tebang pohon yang diperbolehkan ya.\"", GUILayout.Height(36)))
                    {
                        SelectOption(1);
                    }
                    if (GUILayout.Button("2. [Tidak Jadi] \"Tidak usah, aku bisa sendiri.\"", GUILayout.Height(32)))
                    {
                        SelectOption(2);
                    }
                }
                GUILayout.EndArea();
            }

            if (Gameplay2Manager.Instance.currentWood >= Gameplay2Manager.Instance.targetWood &&
                Gameplay2Manager.Instance.isPlayerInCheckpoint &&
                Gameplay2Manager.Instance.currentPhase != G2Phase.Finished)
            {
                GUIStyle pStyle = new GUIStyle(GUI.skin.box);
                pStyle.fontSize = 16;
                pStyle.fontStyle = FontStyle.Bold;
                pStyle.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.green;
                GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height - 90, 500, 45), "[TEKAN E] Serahkan 100 Balok & Pasang Jembatan Gotong Royong!", pStyle);
                GUI.color = Color.white;
            }
        }
    }
}
