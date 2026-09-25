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

        /// <summary>
        /// "Tentar novamente", na tela de Game Over: repete a fase em que o
        /// jogador morreu, em vez de recomecar o jogo do Tutorial.
        ///
        /// A contagem de almas volta ao que era quando a fase comecou (ver
        /// GameManager.PrepareRetry): a cena recarregada traz as almas dela de
        /// volta, e sem desfazer a contagem o mesmo fragmento contaria duas
        /// vezes. Sem fase registrada - alguem abriu esta cena direto no editor,
        /// por exemplo - cai no comeco do jogo, que e o comportamento antigo.
        /// </summary>
        public void OnRetryClicked()
        {
            GameManager gerente = GameManager.Instance;
            if (gerente == null || !gerente.CanRetryPhase)
            {
                OnPlayClicked();
                return;
            }

            gerente.PrepareRetry();
            SceneController.Instance?.LoadScene(gerente.LastGameplayScene);
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
