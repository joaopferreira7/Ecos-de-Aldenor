using UnityEngine;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Marca o ponto de saida de uma fase. Ao ser tocado pelo jogador, carrega
    /// a proxima cena definida no Inspector. Usado no fim de cada fase (Tutorial,
    /// Fase 1, Fase 2) para dar progressao continua sem precisar de logica
    /// hardcoded de qual e a 'proxima fase' dentro do SceneController.
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

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
    }
}
