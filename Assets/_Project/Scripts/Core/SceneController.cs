using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcosDeAldenor.Core
{
    /// <summary>
    /// Centraliza o carregamento de cenas do jogo, com suporte a fade simples
    /// (evita cortes bruscos na troca de cena). Implementado como Singleton
    /// persistente, assim como o GameManager, pois precisa sobreviver as
    /// trocas de cena para poder controlar a proxima cena a ser carregada.
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        public static SceneController Instance { get; private set; }

        [Header("Nomes das Cenas")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string tutorialScene = "Tutorial";
        [SerializeField] private string phase1Scene = "Phase1";
        [SerializeField] private string phase2Scene = "Phase2";
        [SerializeField] private string finalPhaseScene = "FinalPhase";
        [SerializeField] private string victoryScene = "VictoryScreen";
        [SerializeField] private string gameOverScene = "GameOverScreen";

        [Header("Fade")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Garante um overlay de fade persistente (sobrevive as trocas de cena)
            // mesmo que nenhum CanvasGroup tenha sido atribuido no Inspector.
            EnsureFadeOverlay();

            // Revela a primeira cena a partir do preto, evitando um flash inicial.
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
                fadeCanvasGroup.blocksRaycasts = true;
                StartCoroutine(Fade(0f));
            }
        }

        /// <summary>
        /// Cria em runtime um Canvas de tela cheia com uma imagem preta e um
        /// CanvasGroup, como filho deste objeto persistente. Assim o fade funciona
        /// em todas as transicoes sem depender de fiacao manual por cena (e sem
        /// ser destruido junto da cena antiga durante o carregamento).
        /// </summary>
        private void EnsureFadeOverlay()
        {
            if (fadeCanvasGroup != null) return;

            var overlay = new GameObject("FadeOverlay");
            overlay.transform.SetParent(transform, false);

            var canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000; // acima de todo o HUD/menus
            overlay.AddComponent<GraphicRaycaster>();

            var imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(overlay.transform, false);
            var image = imageObj.AddComponent<Image>();
            image.color = Color.black;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            fadeCanvasGroup = overlay.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        public void LoadMainMenu() => LoadScene(mainMenuScene);
        public void LoadTutorial() => LoadScene(tutorialScene);
        public void LoadPhase1() => LoadScene(phase1Scene);
        public void LoadPhase2() => LoadScene(phase2Scene);
        public void LoadVictoryScreen() => LoadScene(victoryScene);
        public void LoadGameOverScreen() => LoadScene(gameOverScene);

        /// <summary>
        /// Carrega a fase final apenas se o jogador tiver fragmentos suficientes
        /// (regra definida no GameManager). Caso contrario, ignora a chamada -
        /// a UI deve usar GameManager.CanAccessFinalPhase para habilitar/desabilitar
        /// o acesso antes mesmo de chamar este metodo.
        /// </summary>
        public void LoadFinalPhase()
        {
            if (GameManager.Instance != null && !GameManager.Instance.CanAccessFinalPhase)
            {
                Debug.LogWarning("Fragmentos insuficientes para acessar a fase final.");
                return;
            }

            LoadScene(finalPhaseScene);
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Carrega uma cena depois de um tempo, contando esse tempo AQUI.
        ///
        /// Existe porque quem pede uma troca de cena com atraso costuma ser
        /// justamente um objeto que esta morrendo - o chefe, por exemplo, e
        /// removido 0,6 s depois de cair. Um Invoke ou uma corrotina iniciada
        /// nele morrem junto com o objeto, em silencio, e a cena nunca troca.
        /// Este controlador e persistente e nao vai a lugar nenhum.
        ///
        /// A contagem e em tempo REAL: o hit stop zera o timeScale por instantes
        /// exatamente no golpe que mata, e uma pausa no meio da comemoracao nao
        /// pode engolir a troca de cena.
        /// </summary>
        public void LoadSceneAfter(string sceneName, float delay)
        {
            StartCoroutine(LoadSceneAfterRoutine(sceneName, delay));
        }

        public void LoadVictoryScreenAfter(float delay) => LoadSceneAfter(victoryScene, delay);
        public void LoadGameOverScreenAfter(float delay) => LoadSceneAfter(gameOverScene, delay);

        private IEnumerator LoadSceneAfterRoutine(string sceneName, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            yield return StartCoroutine(LoadSceneRoutine(sceneName));
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            OnSceneLoadStarted?.Invoke(sceneName);

            yield return StartCoroutine(Fade(1f));

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return StartCoroutine(Fade(0f));

            OnSceneLoadCompleted?.Invoke(sceneName);
        }

        /// <summary>
        /// Anima o CanvasGroup de fade entre 0 (transparente) e 1 (opaco).
        /// Se nenhum CanvasGroup for atribuido, pula a animacao sem erro.
        /// </summary>
        private IEnumerator Fade(float targetAlpha)
        {
            if (fadeCanvasGroup == null)
            {
                yield break;
            }

            // Bloqueia interacao enquanto a tela nao esta totalmente visivel.
            fadeCanvasGroup.blocksRaycasts = true;

            float startAlpha = fadeCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
            // Libera a interacao somente quando o fade termina transparente.
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;
        }
    }
}
