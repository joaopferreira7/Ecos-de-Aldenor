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
            //
            // O atraso e contado pelo SceneController, e nao por um Invoke aqui,
            // porque ESTE objeto nao chega la: EnemyBase.HandleDeath agenda
            // Destroy(gameObject, 0.6f) para o corpo sumir depois da animacao de
            // morte, e destruir o objeto cancela os Invoke pendentes dele. Com
            // victoryDelay de 1,6 s, a chamada era descartada aos 0,6 s sem erro
            // nenhum no console - matava-se o chefe e ficava-se preso na arena
            // para sempre. O SceneController e persistente e nao morre junto.
            SceneController.Instance?.LoadVictoryScreenAfter(victoryDelay);
        }
    }
}
