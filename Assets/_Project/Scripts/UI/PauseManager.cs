using UnityEngine;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Controla a exibicao do menu de pause. Delega a logica de tempo (Time.timeScale)
    /// ao GameManager, mantendo responsabilidade unica: este script so cuida da UI.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        [Tooltip("Toques de pausa e retomada (pacote Leohpaz, 10_UI_Menu_SFX).")]
        [SerializeField] private AudioClip pausarSfx;
        [SerializeField] private AudioClip retomarSfx;

        private void Awake()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused += ShowPausePanel;
                GameManager.Instance.OnGameResumed += HidePausePanel;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused -= ShowPausePanel;
                GameManager.Instance.OnGameResumed -= HidePausePanel;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        private void ShowPausePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            // O jogo congela ao pausar, e um jogo congelado sem som nenhum deixa
            // duvida se pausou ou travou. O toque responde na hora.
            AudioManager.Instance?.PlaySfx(pausarSfx);
        }

        private void HidePausePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            AudioManager.Instance?.PlaySfx(retomarSfx);
        }

        public void OnResumeButtonClicked()
        {
            GameManager.Instance?.TogglePause();
        }

        public void OnMainMenuButtonClicked()
        {
            Time.timeScale = 1f;
            SceneController.Instance?.LoadMainMenu();
        }
    }
}
