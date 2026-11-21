using System.Collections;
using UnityEngine;

namespace Jejak.Gameplay1
{
    public class Gameplay1HUD : MonoBehaviour
    {
        public static Gameplay1HUD Instance { get; private set; }

        private float redBorderAlpha = 0f;
        private string bannerMessage = "";
        private float bannerTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
                if (bannerTimer <= 0f) bannerMessage = "";
            }

            if (Gameplay1Manager.Instance != null)
            {
                bool isBeingChased = Gameplay1Manager.Instance.IsPlayerInUnsafeZone &&
                                     Gameplay1Manager.Instance.CurrentPhase == GameplayPhase.Phase1_SafeForest;

                bool isInvadingAndExposed = Gameplay1Manager.Instance.CurrentPhase == GameplayPhase.Phase2_Invasion &&
                                            !Gameplay1Manager.Instance.IsPlayerInCheckpoint;

                if (isBeingChased || isInvadingAndExposed)
                {
                    redBorderAlpha = Mathf.PingPong(Time.time * 2.5f, 0.45f) + 0.1f;
                }
                else
                {
                    redBorderAlpha = Mathf.MoveTowards(redBorderAlpha, 0f, 2f * Time.deltaTime);
                }
            }
        }

        public void ShowBanner(string message, float duration = 3.5f)
        {
            bannerMessage = message;
            bannerTimer = duration;
        }

        public void TriggerHitFlash()
        {
            redBorderAlpha = 0.8f;
        }

        private void OnGUI()
        {
            if (Gameplay1Manager.Instance == null) return;
            if (Gameplay1Manager.Instance.CurrentPhase == GameplayPhase.VideoIntro ||
                Gameplay1Manager.Instance.CurrentPhase == GameplayPhase.GameplayFinished) return;

            if (redBorderAlpha > 0.05f)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 0f, 0f, redBorderAlpha);
                int borderWidth = 24;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, borderWidth), Texture2D.whiteTexture); 
                GUI.DrawTexture(new Rect(0, Screen.height - borderWidth, Screen.width, borderWidth), Texture2D.whiteTexture); 
                GUI.DrawTexture(new Rect(0, 0, borderWidth, Screen.height), Texture2D.whiteTexture); 
                GUI.DrawTexture(new Rect(Screen.width - borderWidth, 0, borderWidth, Screen.height), Texture2D.whiteTexture); 
                GUI.color = oldColor;
            }

            int wood = Gameplay1Manager.Instance.CurrentWood;
            int targetWood = Gameplay1Manager.Instance.TargetWood;
            float timer = Gameplay1Manager.Instance.RemainingTime;
            int cycle = Gameplay1Manager.Instance.CurrentCycle;

            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            GUIStyle hudBoxStyle = new GUIStyle(GUI.skin.box);
            hudBoxStyle.fontSize = 16;
            hudBoxStyle.fontStyle = FontStyle.Bold;
            hudBoxStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginArea(new Rect(20, 20, 260, 110), hudBoxStyle);
            GUILayout.Space(6);
            GUILayout.Label($"   Balok Kayu : <b>{wood} / {targetWood}</b>");
            
            if (Gameplay1Manager.Instance.CurrentPhase == GameplayPhase.Phase2_Invasion)
            {
                GUI.color = Color.red;
                GUILayout.Label($"   INVASI : <b>{timeString}</b>");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label($"   Waktu Aman : <b>{timeString}</b>");
            }

            GUILayout.Label($"   Siklus Ke : <b>{cycle}</b>");
            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(bannerMessage))
            {
                GUIStyle bannerStyle = new GUIStyle(GUI.skin.box);
                bannerStyle.fontSize = 20;
                bannerStyle.fontStyle = FontStyle.Bold;
                bannerStyle.alignment = TextAnchor.MiddleCenter;

                GUI.color = (Gameplay1Manager.Instance.CurrentPhase == GameplayPhase.Phase2_Invasion) ? Color.red : Color.yellow;
                GUI.Box(new Rect(Screen.width / 2 - 250, 40, 500, 60), bannerMessage, bannerStyle);
                GUI.color = Color.white;
            }

            string zoneStatus = "";
            if (Gameplay1Manager.Instance.IsPlayerInCheckpoint)
            {
                GUI.color = Color.green;
                zoneStatus = " Posisi: DI DALAM CHECKPOINT (AMAN)";
            }
            else if (Gameplay1Manager.Instance.IsPlayerInUnsafeZone)
            {
                GUI.color = Color.red;
                zoneStatus = " Posisi: UNSAFE ZONE (BAHAYA!)";
            }
            else
            {
                GUI.color = Color.white;
                zoneStatus = " Posisi: SAFE ZONE (HUTAN AMAN)";
            }

            GUI.Label(new Rect(Screen.width - 320, 20, 300, 30), zoneStatus);
            GUI.color = Color.white;
        }
    }
}
