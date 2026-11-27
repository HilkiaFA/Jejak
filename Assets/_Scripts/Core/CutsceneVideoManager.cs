using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using Jejak.Gameplay1;
using Jejak.Gameplay2;

namespace Jejak.Core
{
    public class CutsceneVideoManager : MonoBehaviour
    {
        public static CutsceneVideoManager Instance { get; private set; }

        public enum ActiveVideo
        {
            None,
            IntroVideo,
            OutroVideo
        }

        [Header("Video Player & UI")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private GameObject videoScreenUI;

        [Header("Video Clips")]
        [SerializeField] private VideoClip introVideoClip;

        [SerializeField] private VideoClip outroVideoClip;

        [Header("Gameplay Objects & Groups")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject pastWorldGroup;
        [SerializeField] private GameObject gameplay1Group;
        [SerializeField] private GameObject gameplay2Group;

        [Header("Skip Settings")]
        [SerializeField] private bool allowSkip = true;

        private ActiveVideo currentActiveVideo = ActiveVideo.None;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            PlayIntroCutscene();
        }

        private void Update()
        {
            if (currentActiveVideo != ActiveVideo.None && allowSkip)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                {
                    Debug.Log("<color=yellow>[Cutscene] Video dilewati (Skip)!</color>");
                    if (currentActiveVideo == ActiveVideo.IntroVideo)
                    {
                        FinishIntroVideo();
                    }
                    else if (currentActiveVideo == ActiveVideo.OutroVideo)
                    {
                        FinishOutroVideo();
                    }
                }
            }
        }

        public void PlayIntroCutscene()
        {
            currentActiveVideo = ActiveVideo.IntroVideo;

            if (playerObject != null) playerObject.SetActive(false);
            if (pastWorldGroup != null) pastWorldGroup.SetActive(false);

            if (videoPlayer != null && introVideoClip != null)
            {
                if (videoScreenUI != null) videoScreenUI.SetActive(true);

                videoPlayer.clip = introVideoClip;
                videoPlayer.loopPointReached += OnIntroVideoFinished;
                videoPlayer.Play();
                Debug.Log("<color=cyan>[Cutscene] Memutar Video 1 (Intro Bagian B)...</color>");
            }
            else
            {
                Debug.LogWarning("[Cutscene] File Video 1 belum dipasang. Langsung memulai GAMEPLAY 1.");
                FinishIntroVideo();
            }
        }

        private void OnIntroVideoFinished(VideoPlayer source)
        {
            videoPlayer.loopPointReached -= OnIntroVideoFinished;
            FinishIntroVideo();
        }

        private void FinishIntroVideo()
        {
            currentActiveVideo = ActiveVideo.None;
            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
            if (videoScreenUI != null) videoScreenUI.SetActive(false);

            StartPastGameplay1();
        }

        public void StartPastGameplay1()
        {
            if (pastWorldGroup != null) pastWorldGroup.SetActive(true);
            if (playerObject != null) playerObject.SetActive(true);
            if (gameplay1Group != null) gameplay1Group.SetActive(true);
            if (gameplay2Group != null) gameplay2Group.SetActive(false);

            Debug.Log("<color=green>[Gameplay] Karakter terbangun di MASA LALU! Memulai Gameplay 1.</color>");

            if (Gameplay1Manager.Instance != null)
            {
                Gameplay1Manager.Instance.gameObject.SetActive(true);
                Gameplay1Manager.Instance.StartGameplay1();
            }
        }

        public void PlayOutroCutscene()
        {
            currentActiveVideo = ActiveVideo.OutroVideo;

            if (playerObject != null) playerObject.SetActive(false);
            if (pastWorldGroup != null) pastWorldGroup.SetActive(false);

            if (videoPlayer != null && outroVideoClip != null)
            {
                if (videoScreenUI != null) videoScreenUI.SetActive(true);

                videoPlayer.clip = outroVideoClip;
                videoPlayer.loopPointReached += OnOutroFinished;
                videoPlayer.Play();
                Debug.Log("<color=cyan>[Cutscene] Memutar Video 2: Bagian D (The Consequence)...</color>");
            }
            else
            {
                Debug.LogWarning("[Cutscene] File Video 2 belum dipasang. Langsung beralih ke GAMEPLAY 2.");
                FinishOutroVideo();
            }
        }

        private void OnOutroFinished(VideoPlayer source)
        {
            videoPlayer.loopPointReached -= OnOutroFinished;
            FinishOutroVideo();
        }

        public void FinishOutroVideo()
        {
            currentActiveVideo = ActiveVideo.None;
            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
            if (videoScreenUI != null) videoScreenUI.SetActive(false);

            Debug.Log("<color=yellow>[Cutscene] Video 2 Selesai. Memulai GAMEPLAY 2 (Gotong Royong & Pelestarian)!</color>");

            if (Gameplay1Manager.Instance != null)
            {
                Gameplay1Manager.Instance.gameObject.SetActive(false);
            }
            if (gameplay1Group != null) gameplay1Group.SetActive(false);
            if (gameplay2Group != null) gameplay2Group.SetActive(true);

            if (playerObject != null) playerObject.SetActive(true);
            if (pastWorldGroup != null) pastWorldGroup.SetActive(true);

            if (Gameplay2Manager.Instance != null)
            {
                Gameplay2Manager.Instance.StartGameplay2();
            }
        }

        private void OnGUI()
        {
            if (currentActiveVideo != ActiveVideo.None && allowSkip)
            {
                GUI.Label(new Rect(Screen.width - 220, Screen.height - 40, 200, 30), "Tekan [SPACE] untuk Skip Video");
            }
        }
    }
}
