using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jejak.Core;

namespace Jejak.Gameplay1
{
    public enum GameplayPhase
    {
        VideoIntro,
        Phase1_SafeForest,
        Phase2_Invasion,
        CycleTransition,
        GameplayFinished
    }

    public class Gameplay1Manager : MonoBehaviour
    {
        public static Gameplay1Manager Instance { get; private set; }

        [Header("Target & Wood Settings")]
        [SerializeField] private int targetWood = 100;
        [SerializeField] private int currentWood = 0;
        public int CurrentWood => currentWood;
        public int TargetWood => targetWood;

        [Header("MC Health Stats")]
        [SerializeField] private int mcMaxHP = 100;
        [SerializeField] private int mcCurrentHP = 100;
        public int MCCurrentHP => mcCurrentHP;
        public int MCMaxHP => mcMaxHP;

        [Header("Timer Settings")]
        [SerializeField] private float phase1Duration = 180.0f;
        [SerializeField] private float phase2Duration = 30.0f;
        private float remainingTime = 0f;
        public float RemainingTime => remainingTime;

        [Header("Cycle Status")]
        private int currentCycle = 1;
        public int CurrentCycle => currentCycle;

        [Header("Current Phase")]
        [SerializeField] private GameplayPhase currentPhase = GameplayPhase.VideoIntro;
        public GameplayPhase CurrentPhase => currentPhase;

        [Header("Player Zone States")]
        public bool IsPlayerInUnsafeZone { get; private set; } = false;
        public bool IsPlayerInCheckpoint { get; private set; } = false;

        [Header("Scene Object References")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform checkpointPoint;

        [Header("Lists of Entities")]
        [SerializeField] private List<BoarAI> boars = new List<BoarAI>();
        [SerializeField] private List<TreeChop> trees = new List<TreeChop>();

        private Coroutine timerRoutine;
        private bool isResetting = false;

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
            FindReferences();

            if (CutsceneVideoManager.Instance == null)
            {
                StartGameplay1();
            }
        }

        private void FindReferences()
        {
            if (checkpointPoint == null)
            {
                GameObject cp = GameObject.Find("Checkpoint");
                if (cp != null) checkpointPoint = cp.transform;
            }
            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
            }
            if (boars.Count == 0)
            {
                boars.AddRange(FindObjectsByType<BoarAI>(FindObjectsSortMode.None));
            }
            if (trees.Count == 0)
            {
                trees.AddRange(FindObjectsByType<TreeChop>(FindObjectsSortMode.None));
            }
        }

        private void Update()
        {
            if (playerObject == null || checkpointPoint == null)
            {
                FindReferences();
            }

            if (checkpointPoint != null && playerObject != null)
            {
                Vector3 diff = playerObject.transform.position - checkpointPoint.position;
                diff.y = 0f;
                IsPlayerInCheckpoint = (diff.magnitude <= 3.5f);
            }

            if (currentWood >= targetWood && IsPlayerInCheckpoint && currentPhase != GameplayPhase.GameplayFinished && !isResetting)
            {
                FinishGameplay1();
            }
        }

        public void StartGameplay1()
        {
            Debug.Log("<color=green>[Gameplay 1] Dimulai! Tujuan: Kumpulkan 100 Balok Kayu.</color>");

            mcCurrentHP = mcMaxHP;
            currentWood = 0;
            isResetting = false;

            if (playerObject != null)
            {
                playerObject.SetActive(true);
                TeleportPlayerToSpawn();
            }

            StartPhase1();
        }

        private void TeleportPlayerToSpawn()
        {
            if (playerObject == null) return;

            CharacterController cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (playerSpawnPoint != null)
            {
                playerObject.transform.position = playerSpawnPoint.position + Vector3.up * 1.0f;
                playerObject.transform.rotation = playerSpawnPoint.rotation;
            }
            else if (checkpointPoint != null)
            {
                playerObject.transform.position = checkpointPoint.position + Vector3.up * 1.5f;
            }

            if (cc != null) cc.enabled = true;
        }

        private void StartPhase1()
        {
            currentPhase = GameplayPhase.Phase1_SafeForest;
            remainingTime = phase1Duration;

            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.ShowBanner($"SIKLUS {currentCycle} - HUTAN AMAN (Target: 100 Balok)", 4f);
            }

            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(TimerCountdownRoutine());
        }

        private IEnumerator TimerCountdownRoutine()
        {
            while (remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }

            remainingTime = 0f;

            if (currentPhase == GameplayPhase.Phase1_SafeForest)
            {
                StartPhase2Invasion();
            }
            else if (currentPhase == GameplayPhase.Phase2_Invasion)
            {
                EvaluateCycleEnd();
            }
        }

        private void StartPhase2Invasion()
        {
            currentPhase = GameplayPhase.Phase2_Invasion;
            remainingTime = phase2Duration;

            Debug.Log("<color=red>[Gameplay 1] PERINGATAN: INVASI DIMULAI! Babi hutan menyerbu Safe Zone!</color>");

            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.ShowBanner("PERINGATAN: INVASI BABI HUTAN (00:30)\nLARI DAN BERLINDUNG DI CHECKPOINT!", 5f);
            }

            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(TimerCountdownRoutine());
        }

        private void EvaluateCycleEnd()
        {
            currentPhase = GameplayPhase.CycleTransition;

            Debug.Log($"<color=cyan>[Gameplay 1] Evaluasi Siklus {currentCycle}: Balok terkumpul = {currentWood}/{targetWood}</color>");

            if (currentWood >= targetWood)
            {
                FinishGameplay1();
            }
            else
            {
                StartCoroutine(RestartCycleRoutine());
            }
        }

        private IEnumerator RestartCycleRoutine()
        {
            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.ShowBanner($"Balok belum mencapai 100 ({currentWood}/{targetWood}).\nBabi hutan kembali ke sarang... Bersiap Siklus Berikutnya!", 4f);
            }

            foreach (var boar in boars)
            {
                if (boar != null) boar.ResetToNest();
            }

            yield return new WaitForSeconds(3.0f);

            currentCycle++;
            StartPhase1();
        }

        public void AddWood(int amount)
        {
            currentWood += amount;
            Debug.Log($"<color=yellow>[Gameplay 1] Balok bertambah: {currentWood}/{targetWood}</color>");

            if (currentWood >= targetWood)
            {
                Debug.Log("<color=green>[Gameplay 1] Target 100 Balok tercapai!</color>");

                if (IsPlayerInCheckpoint)
                {
                    FinishGameplay1();
                }
                else if (Gameplay1HUD.Instance != null)
                {
                    Gameplay1HUD.Instance.ShowBanner("TARGET 100 BALOK TERCAPAI!\nSegera lari masuk ke Checkpoint (Kotak Kuning)!", 5f);
                }
            }
        }

        public void TakeMCDamage(int damage)
        {
            if (isResetting || currentPhase == GameplayPhase.GameplayFinished) return;

            mcCurrentHP -= damage;
            Debug.Log($"<color=red>[MC Rusak] Diseruduk babi (-{damage} HP)! Sisa HP: {mcCurrentHP}/{mcMaxHP}</color>");

            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.TriggerHitFlash();
            }

            if (mcCurrentHP <= 0)
            {
                mcCurrentHP = 0;
                StartCoroutine(MCDeathResetRoutine());
            }
        }

        private IEnumerator MCDeathResetRoutine()
        {
            isResetting = true;
            Debug.Log("<color=red>[MC GUGUR] MC kehabisan HP! Seluruh progres kayu direset.</color>");

            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.ShowBanner("MC MATI DISERUDUK BABI!\nBalok kayu direset ke 0 dan hutan kembali ke awal.", 4.5f);
            }

            yield return new WaitForSeconds(2.0f);

            mcCurrentHP = mcMaxHP;
            currentWood = 0;

            TeleportPlayerToSpawn();

            foreach (var boar in boars)
            {
                if (boar != null) boar.ResetToNest();
            }

            foreach (var tree in trees)
            {
                if (tree != null) tree.ResetTree();
            }

            isResetting = false;
            StartPhase1();
        }

        public void FinishGameplay1()
        {
            currentPhase = GameplayPhase.GameplayFinished;
            Debug.Log("<color=green>[Gameplay 1 SELESAI] Sukses mengumpulkan 100 balok! Beralih ke Bagian D (The Consequence).</color>");

            if (Gameplay1HUD.Instance != null)
            {
                Gameplay1HUD.Instance.ShowBanner("GAMEPLAY 1 SELESAI!\n100 Balok Berhasil Terkumpul!", 4f);
            }

            if (CutsceneVideoManager.Instance != null)
            {
                CutsceneVideoManager.Instance.PlayOutroCutscene();
            }
        }

        public void SetPlayerInUnsafeZone(bool inUnsafe)
        {
            IsPlayerInUnsafeZone = inUnsafe;
            if (inUnsafe)
            {
                Debug.Log("<color=red>[Zona] Pemain MASUK Unsafe Zone! Babi hutan mulai mengejar!</color>");
            }
            else
            {
                Debug.Log("<color=green>[Zona] Pemain KEMBALI ke Safe Zone. Babi hutan mundur.</color>");
            }
        }

        public void SetPlayerInCheckpoint(bool inCheckpoint)
        {
            IsPlayerInCheckpoint = inCheckpoint;
            if (inCheckpoint)
            {
                Debug.Log("<color=green>[Zona] Pemain AMAN di Checkpoint!</color>");

                if (currentWood >= targetWood && currentPhase != GameplayPhase.GameplayFinished && !isResetting)
                {
                    FinishGameplay1();
                }
            }
            else
            {
                Debug.Log("<color=orange>[Zona] Pemain KELUAR dari Checkpoint.</color>");
            }
        }
    }
}
