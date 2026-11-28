using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using Jejak.Player;
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
        [SerializeField] private GameObject gameplay3Group;

        [Header("Skip Settings")]
        [SerializeField] private bool allowSkip = true;

        private ActiveVideo currentActiveVideo = ActiveVideo.None;
        private float videoStartTime = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            FindVideoPlayerComponent();
            FindSceneReferences();
        }

        private void FindVideoPlayerComponent()
        {
            if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();

            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        }

        private void FindSceneReferences()
        {
            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
            }
            if (gameplay1Group == null)
            {
                gameplay1Group = GameObject.Find("Gameplay1_Group");
            }
            if (gameplay2Group == null)
            {
                gameplay2Group = GameObject.Find("Gameplay2_Group");
            }
            if (gameplay3Group == null)
            {
                gameplay3Group = GameObject.Find("Gameplay3_Group");
            }
        }

        private void Start()
        {
            PlayIntroCutscene();
        }

        private void Update()
        {
            if (currentActiveVideo != ActiveVideo.None && allowSkip)
            {
                if (Time.time - videoStartTime > 1.2f)
                {
                    var keyboard = Keyboard.current;
                    bool skipPressed = (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                        || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape);

                    if (skipPressed)
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
        }

        public void PlayIntroCutscene()
        {
            currentActiveVideo = ActiveVideo.IntroVideo;
            videoStartTime = Time.time;

            ConfigureCameraAndPlayer(false);

            if (gameplay1Group != null) gameplay1Group.SetActive(false);
            if (gameplay2Group != null) gameplay2Group.SetActive(false);
            if (gameplay3Group != null) gameplay3Group.SetActive(false);

            bool videoReady = SetupVideoSource(introVideoClip, "Closing PKKMB.mp4");

            if (videoReady)
            {
                if (videoScreenUI != null) videoScreenUI.SetActive(true);

                videoPlayer.loopPointReached += OnIntroVideoFinished;
                videoPlayer.Play();
                Debug.Log("<color=cyan>[Cutscene] Memutar Video 1 (Intro Bagian B)...</color>");
            }
            else
            {
                Debug.LogWarning("[Cutscene] File Video 1 tidak ditemukan. Langsung memulai Gameplay 1.");
                FinishIntroVideo();
            }
        }

        private bool SetupVideoSource(VideoClip clip, string fallbackFileName)
        {
            Camera targetCam = Camera.main;
            if (targetCam == null) targetCam = FindFirstObjectByType<Camera>();

            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            videoPlayer.targetCamera = targetCam;

            if (clip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = clip;
                return true;
            }

            string filePath = Path.Combine(Application.dataPath, "Color", fallbackFileName);
            if (File.Exists(filePath))
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = filePath;
                return true;
            }

            return false;
        }

        private void ConfigureCameraAndPlayer(bool enablePlayerControls)
        {
            if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;

            var cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = enablePlayerControls;

            var pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = enablePlayerControls;
        }

        private void OnIntroVideoFinished(VideoPlayer source)
        {
            videoPlayer.loopPointReached -= OnIntroVideoFinished;
            FinishIntroVideo();
        }

        private void FinishIntroVideo()
        {
            currentActiveVideo = ActiveVideo.None;

            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            if (videoScreenUI != null) videoScreenUI.SetActive(false);

            StartPastGameplay1();
        }

        public void StartPastGameplay1()
        {
            ConfigureCameraAndPlayer(true);

            if (pastWorldGroup != null) pastWorldGroup.SetActive(true);
            if (gameplay1Group != null) gameplay1Group.SetActive(true);
            if (gameplay2Group != null) gameplay2Group.SetActive(false);
            if (gameplay3Group != null) gameplay3Group.SetActive(false);

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
            videoStartTime = Time.time;

            ConfigureCameraAndPlayer(false);

            if (pastWorldGroup != null) pastWorldGroup.SetActive(false);
            if (gameplay1Group != null) gameplay1Group.SetActive(false);
            if (gameplay2Group != null) gameplay2Group.SetActive(false);
            if (gameplay3Group != null) gameplay3Group.SetActive(false);

            bool videoReady = SetupVideoSource(outroVideoClip, "Closing PKKMB 2.mp4");

            if (videoReady)
            {
                if (videoScreenUI != null) videoScreenUI.SetActive(true);

                videoPlayer.loopPointReached += OnOutroFinished;
                videoPlayer.Play();
                Debug.Log("<color=cyan>[Cutscene] Memutar Video 2 (The Consequence - Bagian D)...</color>");
            }
            else
            {
                Debug.LogWarning("[Cutscene] File Video 2 tidak ditemukan. Langsung memulai Gameplay 2.");
                FinishOutroVideo();
            }
        }

        private void OnOutroFinished(VideoPlayer source)
        {
            videoPlayer.loopPointReached -= OnOutroFinished;
            FinishOutroVideo();
        }

        private void FinishOutroVideo()
        {
            currentActiveVideo = ActiveVideo.None;

            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            if (videoScreenUI != null) videoScreenUI.SetActive(false);

            StartGameplay2();
        }

        public void StartGameplay2()
        {
            ConfigureCameraAndPlayer(true);

            if (pastWorldGroup != null) pastWorldGroup.SetActive(true);
            if (gameplay1Group != null) gameplay1Group.SetActive(false);
            if (gameplay2Group != null) gameplay2Group.SetActive(true);
            if (gameplay3Group != null) gameplay3Group.SetActive(false);

            Debug.Log("<color=green>[Gameplay] Bangun kembali di 2011 untuk KESEMPATAN KEDUA! Memulai Gameplay 2.</color>");

            if (Gameplay2Manager.Instance != null)
            {
                Gameplay2Manager.Instance.gameObject.SetActive(true);
                Gameplay2Manager.Instance.StartGameplay2();
            }
        }
    }
}
