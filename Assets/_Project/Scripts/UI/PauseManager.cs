using UnityEngine;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Controla a exibicao do menu de pause. Delega a logica de tempo (Time.timeScale)
    /// ao GameManager, mantendo responsabilidade unica: este script so cuida da UI.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

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
        }

        private void HidePausePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
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
