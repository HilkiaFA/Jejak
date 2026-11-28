using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jejak.Gameplay3
{
    public class Gameplay3Manager : MonoBehaviour
    {
        public static Gameplay3Manager Instance { get; private set; }

        [Header("Seedlings & Progress")]
        public int seedlingsInventory = 5;
        public int plantedCount = 0;
        public int targetPlantings = 5;

        [Header("Stats MC")]
        public int mcMaxHP = 100;
        public int mcCurrentHP = 100;

        [Header("Entities & Points")]
        public Transform checkpointTransform;
        public Transform playerTransform;
        public Gameplay3NPC wargaNPC;
        public Gameplay3NPC mahasiswaNPC;
        public List<PlantingSpot> plantingSpots = new List<PlantingSpot>();
        public List<Gameplay3Boar> enemies = new List<Gameplay3Boar>();

        [Header("States")]
        public bool IsPlayerInCheckpoint { get; private set; } = false;
        public bool isGameplayFinished = false;
        private bool isHealingAtCheckpoint = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            FindSceneReferences();
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

            if (wargaNPC == null || mahasiswaNPC == null)
            {
                Gameplay3NPC[] npcs = FindObjectsByType<Gameplay3NPC>(FindObjectsSortMode.None);
                foreach (var npc in npcs)
                {
                    if (npc.npcType == G3NPCType.Warga) wargaNPC = npc;
                    else if (npc.npcType == G3NPCType.Mahasiswa) mahasiswaNPC = npc;
                }
            }

            if (plantingSpots.Count == 0)
            {
                plantingSpots.AddRange(FindObjectsByType<PlantingSpot>(FindObjectsSortMode.None));
            }

            if (enemies.Count == 0)
            {
                enemies.AddRange(FindObjectsByType<Gameplay3Boar>(FindObjectsSortMode.None));
            }
        }

        public void StartGameplay3(int carriedSeedlings = 5)
        {
            gameObject.SetActive(true);
            seedlingsInventory = Mathf.Max(targetPlantings, carriedSeedlings);
            plantedCount = 0;
            mcCurrentHP = mcMaxHP;
            isGameplayFinished = false;

            FindSceneReferences();

            if (checkpointTransform != null && playerTransform != null)
            {
                var cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerTransform.position = checkpointTransform.position + Vector3.up * 1.5f;
                if (cc != null) cc.enabled = true;

                var pc = playerTransform.GetComponent<Player.PlayerController>();
                if (pc != null) pc.ResetVelocity();
            }

            if (wargaNPC != null) wargaNPC.ResetNPC();
            if (mahasiswaNPC != null) mahasiswaNPC.ResetNPC();

            foreach (var spot in plantingSpots)
            {
                if (spot != null) spot.ResetSpot();
            }

            foreach (var enemy in enemies)
            {
                if (enemy != null) enemy.ResetBoar();
            }

            Debug.Log("<color=green>[Gameplay 3] Dimulai: Pemulihan Hutan!</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.ShowBanner("GAMEPLAY 3: PEMULIHAN HUTAN!\nTanam seluruh bibit di 5 lokasi bersama Warga & Mahasiswa!", 6f);
            }
        }

        private void Update()
        {
            if (isGameplayFinished) return;

            CheckCheckpointStatus();
        }

        private void CheckCheckpointStatus()
        {
            if (checkpointTransform == null || playerTransform == null) return;

            Vector3 diff = playerTransform.position - checkpointTransform.position;
            diff.y = 0;
            bool wasInCheckpoint = IsPlayerInCheckpoint;
            IsPlayerInCheckpoint = (diff.magnitude <= 3.8f);

            if (IsPlayerInCheckpoint)
            {
                ApplyCheckpointHealing();
            }
            else
            {
                isHealingAtCheckpoint = false;
            }
        }

        private void ApplyCheckpointHealing()
        {
            bool healedSomeone = false;

            if (mcCurrentHP < mcMaxHP)
            {
                mcCurrentHP = mcMaxHP;
                healedSomeone = true;
            }

            if (wargaNPC != null && wargaNPC.isCollapsed && !wargaNPC.isPermanentlyDead)
            {
                wargaNPC.ReviveAtCheckpoint();
                healedSomeone = true;
            }

            if (mahasiswaNPC != null && mahasiswaNPC.isCollapsed && !mahasiswaNPC.isPermanentlyDead)
            {
                mahasiswaNPC.ReviveAtCheckpoint();
                healedSomeone = true;
            }

            if (healedSomeone && !isHealingAtCheckpoint)
            {
                isHealingAtCheckpoint = true;
                if (Gameplay3HUD.Instance != null)
                {
                    Gameplay3HUD.Instance.ShowBanner("Checkpoint Aman: Kondisi Dipulihkan & NPC Bangkit Kembali!", 3.5f);
                }
            }
        }

        public bool CanPlant()
        {
            return (seedlingsInventory > 0 && plantedCount < targetPlantings && !isGameplayFinished);
        }

        public void OnSeedlingPlanted(PlantingSpot spot)
        {
            seedlingsInventory = Mathf.Max(0, seedlingsInventory - 1);
            plantedCount++;

            Debug.Log($"<color=green>[GP3 Tanam] Bibit berhasil ditanam! Progres: {plantedCount}/{targetPlantings}</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.ShowBanner($"Bibit Berhasil Ditanam! Lokasi ({plantedCount}/{targetPlantings})", 3.5f);
            }

            if (plantedCount >= targetPlantings)
            {
                StartCoroutine(EndingRoutine());
            }
        }

        public void DamageMC(int damage)
        {
            if (IsPlayerInCheckpoint || isGameplayFinished) return;

            mcCurrentHP -= damage;
            Debug.Log($"<color=red>[GP3 MC Rusak] Diseruduk babi! HP: {mcCurrentHP}/{mcMaxHP}</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.TriggerHitFlash();
            }

            if (mcCurrentHP <= 0)
            {
                mcCurrentHP = 0;
                StartCoroutine(MCDeathRoutine());
            }
        }

        private IEnumerator MCDeathRoutine()
        {
            Debug.Log("<color=red>[GP3 MC Pingsan] Kembali ke Checkpoint...</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.ShowBanner("MC Pingsan! Dievakuasi ke Checkpoint...", 3.5f);
            }

            yield return new WaitForSeconds(2.0f);

            mcCurrentHP = mcMaxHP;

            if (checkpointTransform != null && playerTransform != null)
            {
                var cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerTransform.position = checkpointTransform.position + Vector3.up * 1.5f;
                if (cc != null) cc.enabled = true;

                var pc = playerTransform.GetComponent<Player.PlayerController>();
                if (pc != null) pc.ResetVelocity();
            }
        }

        private IEnumerator EndingRoutine()
        {
            isGameplayFinished = true;
            Debug.Log("<color=green>[GAMEPLAY 3 SELESAI] Seluruh 5 bibit berhasil ditanam! Menuju Ending JEJAK.</color>");

            if (Gameplay3HUD.Instance != null)
            {
                Gameplay3HUD.Instance.TriggerEnding();
            }

            yield return null;
        }
    }
}
