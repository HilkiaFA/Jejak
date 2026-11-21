using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using Jejak.Gameplay1;

namespace Jejak.Core
{
    public class CutsceneVideoManager : MonoBehaviour
    {
        public static CutsceneVideoManager Instance { get; private set; }

        [Header("Video Player & UI")]
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("UI Canvas atau Panel tempat Video ditampilkan (RawImage)")]
        [SerializeField] private GameObject videoScreenUI;

        [Header("Video Clips")]
        [Tooltip("Video 1: Dosen bangun, kerja ke kampus, pulang lihat jembatan rusak, lalu tidur")]
        [SerializeField] private VideoClip introVideoClip;

        [Tooltip("Video 2: Bangun di masa kini, jembatan sudah kokoh, tapi melihat hutan gundul")]
        [SerializeField] private VideoClip outroVideoClip;

        [Header("Gameplay Objects")]
        [Tooltip("GameObject Player di masa lalu")]
        [SerializeField] private GameObject playerObject;

        [Tooltip("Lingkungan Masa Lalu (Desa kuno, sungai, jembatan, NPC)")]
        [SerializeField] private GameObject pastWorldGroup;

        [Header("Skip Settings")]
        [SerializeField] private bool allowSkip = true;

        private bool isVideoPlaying = false;

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

        private void Start()
        {
            PlayIntroCutscene();
        }

        private void Update()
        {
            if (isVideoPlaying && allowSkip)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                {
                    Debug.Log("<color=yellow>[Cutscene] Video dilewati (Skip)!</color>");
                    FinishIntroVideo();
                }
            }
        }

        
        public void PlayIntroCutscene()
        {
            if (playerObject != null) playerObject.SetActive(false);
            if (pastWorldGroup != null) pastWorldGroup.SetActive(false);

            if (videoPlayer != null && introVideoClip != null)
            {
                isVideoPlaying = true;
                if (videoScreenUI != null) videoScreenUI.SetActive(true);

                videoPlayer.clip = introVideoClip;
                videoPlayer.loopPointReached += OnIntroVideoFinished;
                videoPlayer.Play();
                Debug.Log("<color=cyan>[Cutscene] Memutar Video Intro Era Modern...</color>");
            }
            else
            {
                Debug.LogWarning("[Cutscene] File Video Intro belum dipasang! Langsung memulai gameplay Masa Lalu.");
                StartPastGameplay();
            }
        }

        private void OnIntroVideoFinished(VideoPlayer source)
        {
            videoPlayer.loopPointReached -= OnIntroVideoFinished;
            FinishIntroVideo();
        }

        private void FinishIntroVideo()
        {
            isVideoPlaying = false;
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            if (videoScreenUI != null)
            {
                videoScreenUI.SetActive(false);
            }

            StartPastGameplay();
        }


        public void StartPastGameplay()
        {
            if (pastWorldGroup != null) pastWorldGroup.SetActive(true);
            if (playerObject != null) playerObject.SetActive(true);

            Debug.Log("<color=green>[Gameplay] Karakter terbangun di MASA LALU! Memulai Gameplay 1.</color>");

            if (Gameplay1Manager.Instance != null)
            {
                Gameplay1Manager.Instance.StartGameplay1();
            }
        }


        public void PlayOutroCutscene()
        {
            if (playerObject != null) playerObject.SetActive(false);
            if (pastWorldGroup != null) pastWorldGroup.SetActive(false);

            if (videoPlayer != null && outroVideoClip != null)
            {
                isVideoPlaying = true;
                if (videoScreenUI != null) videoScreenUI.SetActive(true);

                videoPlayer.clip = outroVideoClip;
                videoPlayer.loopPointReached += OnOutroFinished;
                videoPlayer.Play();
                Debug.Log("<color=cyan>[Cutscene] Memutar Video Bagian D: The Consequence (Dampak di Masa Depan)...</color>");
            }
            else
            {
                Debug.Log("<color=green>[Gameplay 1 SELESAI] File Video Bagian D (The Consequence) belum dipasang di Inspector. Target 100 Balok Berhasil Tercapai!</color>");
            }
        }

        private void OnOutroFinished(VideoPlayer source)
        {
            videoPlayer.loopPointReached -= OnOutroFinished;
            isVideoPlaying = false;
            Debug.Log("<color=yellow>[Cutscene] Video Outro Selesai. Babak 1 Selesai!</color>");
        }

        private void OnGUI()
        {
            if (isVideoPlaying && allowSkip)
            {
                GUI.Label(new Rect(Screen.width - 220, Screen.height - 40, 200, 30), "Tekan [SPACE] untuk Skip Video");
            }
        }
    }
}
