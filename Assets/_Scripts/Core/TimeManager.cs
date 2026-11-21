using System;
using System.Collections;
using UnityEngine;

namespace Jejak.Core
{
    public enum GameEra
    {
        Modern,
        Past
    }

    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Current Era")]
        [SerializeField] private GameEra currentEra = GameEra.Modern;
        public GameEra CurrentEra => currentEra;

        [Header("World Group GameObjects")]
        [Tooltip("Parent GameObject yang berisi semua elemen Era Modern (Jalan raya, mobil, lampu kota)")]
        [SerializeField] private GameObject modernWorldGroup;

        [Tooltip("Parent GameObject yang berisi semua elemen Era Masa Lalu (Desa kuno, pepohonan lebat, warga masa lalu)")]
        [SerializeField] private GameObject pastWorldGroup;

        [Header("Bridge States")]
        [SerializeField] private GameObject modernBrokenBridge;
        [SerializeField] private GameObject modernFixedBridge;

        [Header("Environment Consequence")]
        [SerializeField] private GameObject modernLushForest;
        [SerializeField] private GameObject modernBarrenForest;

        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform modernBedSpawnPoint;
        [SerializeField] private Transform pastBedSpawnPoint;

        public bool isBridgeBuiltInPast = false;
        public bool isForestDeforested = false;

        public event Action<GameEra> OnEraChanged;

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
            ApplyEraState();
        }

        public void SleepAndTransitionEra()
        {
            StartCoroutine(SleepRoutine());
        }

        private IEnumerator SleepRoutine()
        {
            Debug.Log("<color=yellow>[TimeManager] Karakter mulai tertidur...</color>");

            yield return new WaitForSeconds(1.0f);

            currentEra = (currentEra == GameEra.Modern) ? GameEra.Past : GameEra.Modern;

            ApplyEraState();

            if (playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                if (currentEra == GameEra.Modern && modernBedSpawnPoint != null)
                {
                    playerTransform.position = modernBedSpawnPoint.position;
                    playerTransform.rotation = modernBedSpawnPoint.rotation;
                }
                else if (currentEra == GameEra.Past && pastBedSpawnPoint != null)
                {
                    playerTransform.position = pastBedSpawnPoint.position;
                    playerTransform.rotation = pastBedSpawnPoint.rotation;
                }

                if (cc != null) cc.enabled = true;
            }

            Debug.Log($"<color=green>[TimeManager] Terbangun di era: {currentEra}!</color>");
            OnEraChanged?.Invoke(currentEra);
        }

        public void ApplyEraState()
        {
            bool isModern = (currentEra == GameEra.Modern);

            if (modernWorldGroup != null) modernWorldGroup.SetActive(isModern);
            if (pastWorldGroup != null) pastWorldGroup.SetActive(!isModern);

            if (isModern)
            {
                if (modernBrokenBridge != null) modernBrokenBridge.SetActive(!isBridgeBuiltInPast);
                if (modernFixedBridge != null) modernFixedBridge.SetActive(isBridgeBuiltInPast);

                if (modernLushForest != null) modernLushForest.SetActive(!isForestDeforested);
                if (modernBarrenForest != null) modernBarrenForest.SetActive(isForestDeforested);
            }
        }

        public void SetBridgeCompletedInPast()
        {
            isBridgeBuiltInPast = true;
            isForestDeforested = true; 
            Debug.Log("<color=cyan>[TimeManager] Jembatan masa lalu selesai! Hutan menjadi gundul untuk masa depan.</color>");
        }
    }
}
