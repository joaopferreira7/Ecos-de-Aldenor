using UnityEngine;
using EcosDeAldenor.Core;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Marca o ponto de saida de uma fase. Ao ser tocado pelo jogador, carrega
    /// a proxima cena definida no Inspector. Usado no fim de cada fase (Tutorial,
    /// Fase 1, Fase 2) para dar progressao continua sem precisar de logica
    /// hardcoded de qual e a 'proxima fase' dentro do SceneController.
    ///
    /// A PORTA GUARDA AS ALMAS DA PROPRIA REGIAO
    ///
    /// As seis almas do jogo ficam sobre plataformas (y entre 2,3 e 3,6) e todas
    /// as saidas ficam no chao (y 0,5), no fim do percurso. Ou seja: o caminho
    /// natural - correr para a direita - nao passa por alma nenhuma. E a fase
    /// final exige as seis, sem folga.
    ///
    /// Antes desta checagem, quem pulasse uma alma so descobria no fim da Fase 2:
    /// a porta de la chama LoadFinalPhase, que recusa em silencio quando faltam
    /// almas. Nao havia mensagem, nem som, nem caminho de volta - o jogo ficava
    /// invencivel sem avisar, e nada na tela explicava por que a porta parara de
    /// funcionar.
    ///
    /// Com a regra aqui, cada fase cobra as suas almas na hora de sair. O
    /// jogador descobre no lugar onde ainda da para resolver, a regra vira a
    /// mesma em toda fase, e e impossivel chegar a fase final com menos de seis.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PhaseExit : MonoBehaviour
    {
        private enum NextScene
        {
            Phase1,
            Phase2,
            FinalPhase,
            VictoryScreen
        }

        [SerializeField] private NextScene nextScene;

        [Tooltip("A porta so abre quando todas as almas desta fase forem coletadas.")]
        [SerializeField] private bool exigirAlmasDaFase = true;

        [Tooltip("Intervalo minimo entre dois avisos, para nao repetir a mensagem a cada quadro.")]
        [SerializeField] private float intervaloDoAviso = 2.5f;

        [Tooltip("Toque de porta trancada (opcional).")]
        [SerializeField] private AudioClip portaTrancadaSfx;

        private float proximoAviso;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (exigirAlmasDaFase && !PodePassar()) return;

            switch (nextScene)
            {
                case NextScene.Phase1:
                    SceneController.Instance?.LoadPhase1();
                    break;
                case NextScene.Phase2:
                    SceneController.Instance?.LoadPhase2();
                    break;
                case NextScene.FinalPhase:
                    SceneController.Instance?.LoadFinalPhase();
                    break;
                case NextScene.VictoryScreen:
                    SceneController.Instance?.LoadVictoryScreen();
                    break;
            }
        }

        /// <summary>
        /// A fase so libera a saida quando nao resta nenhuma alma nela. Conta os
        /// Fragment ainda vivos na cena, e nao o total do GameManager: coletar
        /// destroi o objeto, entao o que sobrou na cena E o que falta - sem
        /// precisar registrar em lugar nenhum quantas almas cada fase tinha.
        /// </summary>
        private bool PodePassar()
        {
            int faltam = Object.FindObjectsByType<Fragment>(FindObjectsSortMode.None).Length;
            if (faltam <= 0) return true;

            if (Time.unscaledTime >= proximoAviso)
            {
                proximoAviso = Time.unscaledTime + intervaloDoAviso;
                Avisar(faltam);
            }
            return false;
        }

        /// <summary>
        /// Diz o que falta e onde procurar. A segunda linha existe porque as
        /// almas ficam todas sobre plataformas, fora do caminho reto ate a
        /// porta: sem ela o jogador sabe que faltou algo, mas nao onde olhar.
        /// </summary>
        private void Avisar(int faltam)
        {
            string quantas = faltam == 1
                ? "AINDA HÁ UMA ALMA NESTA REGIÃO"
                : "AINDA HÁ " + faltam + " ALMAS NESTA REGIÃO";

            var ui = Object.FindAnyObjectByType<UIManager>();
            if (ui != null)
            {
                ui.MostrarAviso(quantas +
                    "\n<size=20><color=#B9A7D6>Procure nas plataformas mais altas</color></size>", 2.2f);
            }

            AudioManager.Instance?.PlaySfx(portaTrancadaSfx);
        }
    }
}
