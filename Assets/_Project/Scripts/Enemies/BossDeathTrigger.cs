using UnityEngine;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Escuta o evento de morte do HealthSystem do chefe e carrega a tela de
    /// vitoria automaticamente. Mantido separado de AVigilia para nao misturar
    /// a logica de combate do chefe com a logica de progressao de cena.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class BossDeathTrigger : MonoBehaviour
    {
        [SerializeField] private float victoryDelay = 1.6f;

        private HealthSystem healthSystem;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            healthSystem.OnDeath += HandleBossDeath;
        }

        private void OnDestroy()
        {
            healthSystem.OnDeath -= HandleBossDeath;
        }

        private void HandleBossDeath()
        {
            // Atraso para a explosao de morte e o tremor de camera serem vistos
            // antes de trocar para a tela de vitoria.
            Invoke(nameof(GoToVictory), victoryDelay);
        }

        private void GoToVictory()
        {
            SceneController.Instance?.LoadVictoryScreen();
        }
    }
}
