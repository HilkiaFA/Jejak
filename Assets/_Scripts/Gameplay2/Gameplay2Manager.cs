using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jejak.Core;
using Jejak.Gameplay3;

namespace Jejak.Gameplay2
{
    public enum G2Phase
    {
        SafePeriod,
        InvasionPeriod,
        EnragedInvasion,
        BridgeSubmission,
        Finished
    }

    public class Gameplay2Manager : MonoBehaviour
    {
        public static Gameplay2Manager Instance { get; private set; }

        [Header("Phase & Timers")]
        public G2Phase currentPhase = G2Phase.SafePeriod;
        [SerializeField] private float safeDuration = 150.0f;
        [SerializeField] private float invasionDuration = 30.0f;
        public float remainingTime = 150.0f;

        [Header("Stats MC")]
        public int mcMaxHP = 100;
        public int mcCurrentHP = 100;
        public int currentWood = 0;
        public int targetWood = 100;
        public int currentSeedlings = 0;

        [Header("NPC References")]
        public Gameplay2NPC wargaNPC;
        public Gameplay2NPC mahasiswaNPC;

        [Header("Boars & Trees")]
        public List<Gameplay2Boar> boars = new List<Gameplay2Boar>();
        public List<Gameplay2Tree> trees = new List<Gameplay2Tree>();

        [Header("Zones & Points")]
        public Transform checkpointTransform;
        public GameObject bridgePlaceholder;
        public Transform playerTransform;

        public bool isPlayerInCheckpoint = false;
        private Coroutine timerRoutine;
        private bool isResetting = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            FindSceneReferences();

            if (CutsceneVideoManager.Instance == null)
            {
                StartGameplay2();
            }
        }

        private void FindSceneReferences()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (checkpointTransform == null)
            {
                GameObject cp = GameObject.Find("Checkpoint");
                if (cp != null) checkpointTransform = cp.transform;
            }

            if (bridgePlaceholder == null)
            {
                bridgePlaceholder = GameObject.Find("SM_Bridge");
            }

            if (wargaNPC == null)
            {
                Gameplay2NPC[] npcs = FindObjectsByType<Gameplay2NPC>(FindObjectsSortMode.None);
                foreach (var npc in npcs)
                {
                    if (npc.npcType == NPCType.Warga) wargaNPC = npc;
                    else if (npc.npcType == NPCType.Mahasiswa) mahasiswaNPC = npc;
                }
            }

            if (boars.Count == 0) boars.AddRange(FindObjectsByType<Gameplay2Boar>(FindObjectsSortMode.None));
            if (trees.Count == 0) trees.AddRange(FindObjectsByType<Gameplay2Tree>(FindObjectsSortMode.None));
        }

        private void Update()
        {
            if (checkpointTransform != null && playerTransform != null)
            {
                Vector3 diff = playerTransform.position - checkpointTransform.position;
                diff.y = 0f;

                isPlayerInCheckpoint = (diff.magnitude <= 3.5f);
            }
        }

        public void StartGameplay2()
        {
            Debug.Log("<color=green>[Gameplay 2] Dimulai: Gotong Royong dan Pelestarian!</color>");
            gameObject.SetActive(true);

            FindSceneReferences();

            if (checkpointTransform != null && playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                playerTransform.position = checkpointTransform.position + Vector3.up * 1.5f;
                if (cc != null) cc.enabled = true;

                Player.PlayerController pc = playerTransform.GetComponent<Player.PlayerController>();
                if (pc != null) pc.ResetVelocity();
            }

            ResetToSafePeriod();
        }

        public void ResetToSafePeriod()
        {
            currentPhase = G2Phase.SafePeriod;
            remainingTime = safeDuration;

            foreach (var b in boars)
            {
                if (b != null) b.ResetToNest();
            }

            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(TimerCountdownRoutine());

            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.ShowBanner("HUTAN AMAN (02:30) - Rekrut Warga & Mahasiswa!", 4f);
            }
        }

        private IEnumerator TimerCountdownRoutine()
        {
            while (remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }
            remainingTime = 0f;

            if (currentPhase == G2Phase.SafePeriod)
            {
                StartInvasion();
            }
            else if (currentPhase == G2Phase.InvasionPeriod || currentPhase == G2Phase.EnragedInvasion)
            {
                EndInvasion();
            }
        }

        public void StartInvasion()
        {
            currentPhase = G2Phase.InvasionPeriod;
            remainingTime = invasionDuration;

            Debug.Log("<color=red>[Gameplay 2] WAKTU AMAN HABIS! Babi hutan menyerbu Safe Zone!</color>");

            foreach (var b in boars)
            {
                if (b != null) b.StartInvasion();
            }

            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.ShowBanner("PERINGATAN INVASI (00:30)!\nBabi hutan masuk Safe Zone! Berlindung di Checkpoint!", 5f);
            }

            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(TimerCountdownRoutine());
        }

        public void TriggerProtectedTreeAggro()
        {
            currentPhase = G2Phase.EnragedInvasion;
            remainingTime = 30f;

            Debug.Log("<color=red>[Gameplay 2] POHON DILINDUNGI DISERANG! Babi hutan mengamuk seketika!</color>");

            if (boars.Count == 0)
            {
                boars.AddRange(FindObjectsByType<Gameplay2Boar>(FindObjectsSortMode.None));
            }

            foreach (var b in boars)
            {
                if (b != null)
                {
                    b.StartInvasion();
                }
            }

            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.TriggerHitFlash();
                Gameplay2HUD.Instance.ShowBanner("POHON TERLARANG DIPUKUL!\nSemua babi mengamuk seketika! Lari ke Checkpoint!", 5f);
            }

            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(TimerCountdownRoutine());
        }

        private void EndInvasion()
        {
            Debug.Log("<color=cyan>[Gameplay 2] Waktu invasi berakhir.</color>");

            if (currentWood >= targetWood)
            {
                currentPhase = G2Phase.BridgeSubmission;
                if (Gameplay2HUD.Instance != null)
                {
                    Gameplay2HUD.Instance.ShowBanner("TARGET 100 BALOK TERCAPAI!\nKembalilah ke Checkpoint dan tekan [E] untuk membangun jembatan!", 6f);
                }
            }
            else
            {
                ResetToSafePeriod();
            }
        }

        public void DamageMC(int damage)
        {
            if (isPlayerInCheckpoint || isResetting) return;

            mcCurrentHP -= damage;
            Debug.Log($"<color=red>[Gameplay 2] MC terkena serudukan! HP: {mcCurrentHP}/{mcMaxHP}</color>");

            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.TriggerHitFlash();
            }

            if (mcCurrentHP <= 0)
            {
                mcCurrentHP = 0;
                StartCoroutine(MCDeathRoutine());
            }
        }

        private IEnumerator MCDeathRoutine()
        {
            isResetting = true;
            Debug.Log("<color=red>[Gameplay 2] MC MATI! Reset seluruh babak Gameplay 2...</color>");
            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.ShowBanner("MC PINGSAN! Mengulang dari awal...", 4f);
            }

            yield return new WaitForSeconds(2.5f);

            mcCurrentHP = mcMaxHP;
            currentWood = 0;
            currentSeedlings = 0;

            if (wargaNPC != null) wargaNPC.ResetNPC();
            if (mahasiswaNPC != null) mahasiswaNPC.ResetNPC();

            foreach (var t in trees)
            {
                if (t != null) t.ResetTree();
            }

            if (checkpointTransform != null && playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerTransform.position = checkpointTransform.position + Vector3.up * 1.5f;
                if (cc != null) cc.enabled = true;

                Player.PlayerController pc = playerTransform.GetComponent<Player.PlayerController>();
                if (pc != null) pc.ResetVelocity();
            }

            isResetting = false;
            ResetToSafePeriod();
        }

        public void AddWoodAndSeedling(int wood, int seedling)
        {
            currentWood += wood;
            currentSeedlings += seedling;
            Debug.Log($"<color=yellow>[Gameplay 2] Balok: {currentWood}/{targetWood} | Bibit: {currentSeedlings}</color>");

            if (currentWood >= targetWood && currentPhase != G2Phase.BridgeSubmission && currentPhase != G2Phase.Finished)
            {
                currentPhase = G2Phase.BridgeSubmission;
                if (Gameplay2HUD.Instance != null)
                {
                    Gameplay2HUD.Instance.ShowBanner("100 Balok Terkumpul!\nPergilah ke Checkpoint dan tekan [E] untuk memasang jembatan!", 5f);
                }
            }
        }

        public void SubmitWoodAndBuildBridge()
        {
            currentPhase = G2Phase.Finished;
            Debug.Log("<color=green>[Gameplay 2 SELESAI] Jembatan berhasil dibangun dengan gotong royong!</color>");

            if (bridgePlaceholder != null)
            {
                bridgePlaceholder.SetActive(true);
            }

            if (Gameplay2HUD.Instance != null)
            {
                Gameplay2HUD.Instance.ShowBanner("GAMEPLAY 2 SELESAI!\nJembatan Berhasil Dibangun Bersama Warga & Mahasiswa!", 5f);
            }

            StartCoroutine(TransitionToGameplay3Routine());
        }

        private IEnumerator TransitionToGameplay3Routine()
        {
            yield return new WaitForSeconds(4.5f);

            GameObject gp2Group = GameObject.Find("Gameplay2_Group");
            GameObject gp3Group = GameObject.Find("Gameplay3_Group");

            if (gp3Group != null)
            {
                if (gp2Group != null) gp2Group.SetActive(false);
                gp3Group.SetActive(true);

                var gp3Manager = gp3Group.GetComponentInChildren<Gameplay3Manager>();
                if (gp3Manager != null)
                {
                    gp3Manager.StartGameplay3(currentSeedlings);
                }
            }
            else
            {
                var gp3Manager = Object.FindFirstObjectByType<Gameplay3Manager>(FindObjectsInactive.Include);
                if (gp3Manager != null)
                {
                    if (gp2Group != null) gp2Group.SetActive(false);
                    gp3Manager.gameObject.SetActive(true);
                    gp3Manager.StartGameplay3(currentSeedlings);
                }
            }
        }
    }
}
