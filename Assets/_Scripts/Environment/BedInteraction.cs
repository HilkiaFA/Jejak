using UnityEngine;
using UnityEngine.InputSystem;
using Jejak.Core;

namespace Jejak.Environment
{
    [RequireComponent(typeof(Collider))]
    public class BedInteraction : MonoBehaviour
    {
        [Header("Syarat Tidur")]
        [Tooltip("Jembatan masa lalu harus selesai sebelum karakter bisa tidur untuk bangun di masa depan")]
        [SerializeField] private BridgeConstruction bridgeConstruction;

        private bool isPlayerNearby = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNearby = true;
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
            if (isPlayerNearby)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    bool isBridgeDone = (bridgeConstruction != null && bridgeConstruction.currentPhase >= 3);

                    if (isBridgeDone)
                    {
                        Debug.Log("<color=yellow>[Kasur Masa Lalu] Karakter tertidur pulas setelah jembatan selesai...</color>");
                        if (CutsceneVideoManager.Instance != null)
                        {
                            CutsceneVideoManager.Instance.PlayOutroCutscene();
                        }
                    }
                    else
                    {
                        Debug.Log("<color=orange>[Kasur Masa Lalu] Kamu belum bisa tidur! Selesaikan pembangunan jembatan dulu.</color>");
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (isPlayerNearby)
            {
                bool isBridgeDone = (bridgeConstruction != null && bridgeConstruction.currentPhase >= 3);
                string message = isBridgeDone
                    ? "[Tekan E] Tidur untuk Kembali ke Masa Depan (Lihat Hasil Jembatan)"
                    : "Belum bisa tidur! Selesaikan pembangunan jembatan bersama warga desa dulu.";

                GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height - 100, 500, 45), message);
            }
        }
    }
}
