using UnityEngine;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Conecta os botoes do Menu Principal as acoes correspondentes no
    /// SceneController. Mantido separado do SceneController para que a
    /// logica de navegacao de cenas nao dependa de detalhes especificos
    /// da UI do menu (Single Responsibility).
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public void OnPlayClicked()
        {
            GameManager.Instance?.ResetProgress();
            SceneController.Instance?.LoadTutorial();
        }

        public void OnCreditsClicked()
        {
            SceneController.Instance?.LoadScene("Credits");
        }

        /// <summary>
        /// Volta ao Menu Principal (usado pelo botao "Voltar" da tela de Creditos).
        /// Recorre ao SceneManager diretamente caso o SceneController persistente
        /// ainda nao exista (ex.: cena de Creditos aberta isoladamente).
        /// </summary>
        public void OnBackToMenuClicked()
        {
            if (SceneController.Instance != null)
                SceneController.Instance.LoadMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void OnQuitClicked()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
