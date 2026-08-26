using System;
using UnityEngine;

namespace EcosDeAldenor.Core
{
    /// <summary>
    /// Gerenciador central do jogo. Persiste entre cenas (DontDestroyOnLoad) e
    /// mantem o estado global: fragmentos coletados, checkpoint atual, contagem
    /// de mortes e estado de pausa. Implementado como Singleton simples, padrao
    /// comum em jogos Unity para acesso global controlado a um unico ponto de dados.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Progresso do Jogador")]
        [SerializeField] private int totalFragmentsCollected;
        [SerializeField] private int fragmentsRequiredForFinalPhase = 6;

        [Header("Checkpoint")]
        private Vector3 lastCheckpointPosition;
        private string lastCheckpointScene;

        [Header("Estado do Jogo")]
        private int deathCount;
        private bool isPaused;

        public event Action<int> OnFragmentsChanged;
        public event Action OnGamePaused;
        public event Action OnGameResumed;

        public int TotalFragments => totalFragmentsCollected;
        public int FragmentsRequired => fragmentsRequiredForFinalPhase;
        public bool CanAccessFinalPhase => totalFragmentsCollected >= fragmentsRequiredForFinalPhase;
        public bool IsPaused => isPaused;

        private void Awake()
        {
            // Garante que existe apenas uma instancia do GameManager na aplicacao.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddFragment(int amount = 1)
        {
            totalFragmentsCollected += amount;
            OnFragmentsChanged?.Invoke(totalFragmentsCollected);
        }

        public void SetCheckpoint(Vector3 position, string sceneName)
        {
            lastCheckpointPosition = position;
            lastCheckpointScene = sceneName;
        }

        public Vector3 GetCheckpointPosition() => lastCheckpointPosition;
        public string GetCheckpointScene() => lastCheckpointScene;

        public void RegisterDeath()
        {
            deathCount++;
        }

        public int GetDeathCount() => deathCount;

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (isPaused) OnGamePaused?.Invoke();
            else OnGameResumed?.Invoke();
        }

        /// <summary>
        /// Reinicia o progresso do jogador. Usado ao comecar um novo jogo
        /// a partir do menu principal.
        /// </summary>
        public void ResetProgress()
        {
            totalFragmentsCollected = 0;
            deathCount = 0;
            lastCheckpointPosition = Vector3.zero;
            lastCheckpointScene = string.Empty;
            OnFragmentsChanged?.Invoke(totalFragmentsCollected);
        }
    }
}
