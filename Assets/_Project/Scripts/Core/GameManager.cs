using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // REINICIAR A FASE DEPOIS DE MORRER
        //
        // A tela de Game Over so oferecia "Menu Principal", e o menu recomeca o
        // jogo do Tutorial: morrer no chefe custava refazer o jogo inteiro.
        //
        // Para poder repetir apenas a fase, e preciso guardar quantas almas o
        // jogador tinha AO ENTRAR nela. Recarregar a cena traz de volta as almas
        // que estavam nela (elas sao destruidas ao ser coletadas, mas voltam com
        // a cena) - sem desfazer a contagem, o mesmo fragmento seria somado duas
        // vezes e o HUD chegaria a marcar "8 / 6".
        private string lastGameplayScene;
        private int fragmentsAtPhaseStart;

        public int TotalFragments => totalFragmentsCollected;
        /// <summary>Ha uma fase de jogo para repetir depois de um Game Over?</summary>
        public bool CanRetryPhase => !string.IsNullOrEmpty(lastGameplayScene);
        public string LastGameplayScene => lastGameplayScene;
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

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// Guarda o ponto de partida de cada fase jogavel: qual e a cena e
        /// quantas almas o jogador ja trazia ao chegar nela.
        ///
        /// Uma cena e "jogavel" quando tem um objeto com a tag Player - assim
        /// nao ha uma lista de nomes de cena para manter em dia, e as telas de
        /// menu, vitoria, derrota e creditos ficam de fora sozinhas. O evento
        /// sceneLoaded dispara depois do Awake dos objetos da cena, entao o
        /// jogador ja existe quando esta busca acontece.
        /// </summary>
        private void HandleSceneLoaded(Scene cena, LoadSceneMode modo)
        {
            if (GameObject.FindGameObjectWithTag("Player") == null) return;

            lastGameplayScene = cena.name;
            fragmentsAtPhaseStart = totalFragmentsCollected;
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
        /// Prepara a repeticao da fase em que o jogador morreu: devolve a
        /// contagem de almas ao valor que ela tinha quando a fase comecou e
        /// esquece o checkpoint.
        ///
        /// O checkpoint precisa ser esquecido porque a cena vai ser recarregada
        /// do zero: o altar volta apagado e a fase recomeca do inicio, mas a
        /// posicao gravada continuaria valendo para a proxima queda - o jogador
        /// cairia no abismo e reapareceria num altar que, na tela, nunca foi
        /// aceso.
        /// </summary>
        public void PrepareRetry()
        {
            ResetPauseState();
            lastCheckpointPosition = Vector3.zero;
            lastCheckpointScene = string.Empty;

            totalFragmentsCollected = fragmentsAtPhaseStart;
            OnFragmentsChanged?.Invoke(totalFragmentsCollected);
        }

        /// <summary>
        /// Desfaz a pausa sem avisar ninguem: o tempo volta a correr e o jogo
        /// deixa de se considerar pausado.
        ///
        /// Existe porque este objeto sobrevive a troca de cena e o estado de
        /// pausa sobrevivia junto. Sair para o Menu Principal pelo menu de pausa
        /// restaurava o Time.timeScale, mas deixava isPaused ligado - e na
        /// partida seguinte o PRIMEIRO ESC chamava TogglePause para "despausar"
        /// algo que nao estava pausado: tocava o som de retomada e nao abria
        /// nada. So o segundo ESC pausava de verdade, o que parece tecla travada.
        ///
        /// Nao dispara OnGameResumed de proposito: quem chama esta saindo da
        /// cena, e nao ha painel de pausa a esconder nem retomada a anunciar.
        /// </summary>
        public void ResetPauseState()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Reinicia o progresso do jogador. Usado ao comecar um novo jogo
        /// a partir do menu principal.
        /// </summary>
        public void ResetProgress()
        {
            ResetPauseState();
            totalFragmentsCollected = 0;
            deathCount = 0;
            lastCheckpointPosition = Vector3.zero;
            lastCheckpointScene = string.Empty;
            lastGameplayScene = string.Empty;
            fragmentsAtPhaseStart = 0;
            OnFragmentsChanged?.Invoke(totalFragmentsCollected);
        }
    }
}
