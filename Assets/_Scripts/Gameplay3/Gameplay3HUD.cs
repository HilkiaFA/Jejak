using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jejak.Gameplay3
{
    public class Gameplay3HUD : MonoBehaviour
    {
        public static Gameplay3HUD Instance { get; private set; }

        private float redBorderAlpha = 0f;
        private string bannerMessage = "";
        private float bannerTimer = 0f;

        private bool isEnding = false;
        private float endingFadeAlpha = 0f;

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

            if (isEnding)
            {
                endingFadeAlpha = Mathf.MoveTowards(endingFadeAlpha, 1f, 0.4f * Time.deltaTime);

                var kb = Keyboard.current;
                bool restartPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

                if (restartPressed && endingFadeAlpha >= 0.95f)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
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

        public void TriggerEnding()
        {
            isEnding = true;
            ShowBanner("SELURUH BIBIT BERHASIL DITANAM!", 6f);
        }

        private void OnGUI()
        {
            if (Gameplay3Manager.Instance == null) return;
            if (!Gameplay3Manager.Instance.gameObject.activeInHierarchy) return;

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

            if (!isEnding)
            {
                DrawMainHUD();
            }

            if (!string.IsNullOrEmpty(bannerMessage) && !isEnding)
            {
                GUIStyle bStyle = new GUIStyle(GUI.skin.box);
                bStyle.fontSize = 15;
                bStyle.fontStyle = FontStyle.Bold;
                bStyle.alignment = TextAnchor.MiddleCenter;

                GUI.color = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2 - 320, 25, 640, 65), bannerMessage, bStyle);
                GUI.color = Color.white;
            }

            if (isEnding)
            {
                DrawEndingScreen();
            }
        }

        private void DrawMainHUD()
        {
            int planted = Gameplay3Manager.Instance.plantedCount;
            int target = Gameplay3Manager.Instance.targetPlantings;
            int seeds = Gameplay3Manager.Instance.seedlingsInventory;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 13;
            boxStyle.fontStyle = FontStyle.Bold;
            boxStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginArea(new Rect(20, 20, 310, 160), boxStyle);
            GUILayout.Space(4);
            GUILayout.Label($"Lokasi Ditanam : <b><color=green>{planted} / {target}</color></b>");
            GUILayout.Label($"Sisa Bibit : <b>{seeds}</b>");
            GUILayout.Label($"HP MC : <b>{Gameplay3Manager.Instance.mcCurrentHP}/{Gameplay3Manager.Instance.mcMaxHP}</b>");

            var w = Gameplay3Manager.Instance.wargaNPC;
            string wText = "Tidak Ada";
            if (w != null)
            {
                if (w.isPermanentlyDead) wText = "<color=gray>Gugur Permanen</color>";
                else if (w.isCollapsed) wText = $"<color=red>TUMBANG ({Mathf.CeilToInt(w.reviveTimer)}s)!</color>";
                else wText = $"<color=green>{w.currentHP}/{w.maxHP}</color>";
            }
            GUILayout.Label($"Warga : <b>{wText}</b>");

            var m = Gameplay3Manager.Instance.mahasiswaNPC;
            string mText = "Tidak Ada";
            if (m != null)
            {
                if (m.isPermanentlyDead) mText = "<color=gray>Gugur Permanen</color>";
                else if (m.isCollapsed) mText = $"<color=red>TUMBANG ({Mathf.CeilToInt(m.reviveTimer)}s)!</color>";
                else mText = $"<color=green>{m.currentHP}/{m.maxHP}</color>";
            }
            GUILayout.Label($"Mahasiswa : <b>{mText}</b>");

            if (Gameplay3Manager.Instance.IsPlayerInCheckpoint)
            {
                GUI.color = Color.green;
                GUILayout.Label("Status: <b>DI CHECKPOINT (AMAN)</b>");
                GUI.color = Color.white;
            }
            GUILayout.EndArea();
        }

        private void DrawEndingScreen()
        {
            Color oldC = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, endingFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldC;

            if (endingFadeAlpha > 0.4f)
            {
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 38;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.normal.textColor = Color.black;

                GUIStyle subStyle = new GUIStyle(GUI.skin.label);
                subStyle.fontSize = 20;
                subStyle.fontStyle = FontStyle.Italic;
                subStyle.alignment = TextAnchor.MiddleCenter;
                subStyle.normal.textColor = new Color(0.2f, 0.4f, 0.2f);

                GUIStyle storyStyle = new GUIStyle(GUI.skin.label);
                storyStyle.fontSize = 15;
                storyStyle.alignment = TextAnchor.MiddleCenter;
                storyStyle.normal.textColor = Color.black;

                float midX = Screen.width / 2;
                float midY = Screen.height / 2;

                GUI.Label(new Rect(midX - 300, midY - 140, 600, 60), "JEJAK", titleStyle);
                GUI.Label(new Rect(midX - 350, midY - 80, 700, 40), "\"Setiap Langkah Meninggalkan Jejak\"", subStyle);

                string narrative = "Melalui gotong royong dan pelestarian alam, hutan kembali hijau subur.\nSungai mengalir jernih, dan jembatan masa depan berdiri kokoh.\n\nTHE END";
                GUI.Label(new Rect(midX - 350, midY - 20, 700, 100), narrative, storyStyle);

                if (endingFadeAlpha >= 0.95f)
                {
                    GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
                    hintStyle.fontSize = 13;
                    hintStyle.alignment = TextAnchor.MiddleCenter;
                    hintStyle.normal.textColor = Color.gray;
                    GUI.Label(new Rect(midX - 250, midY + 110, 500, 30), "[Tekan SPASI / ENTER untuk Mengulang]", hintStyle);
                }
            }
        }
    }
}
