using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EcosDeAldenor.Core;
using EcosDeAldenor.Player;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Escuta o evento de morte do HealthSystem do jogador. Perder toda a vida
    /// (0 coracoes) leva a tela de Game Over. Cair para fora do mapa (ver
    /// FallDeathZone) e tratado a parte por HandleFallOutOfBounds: se ja existe
    /// um checkpoint na fase atual, o jogador volta para la em vez de ir para
    /// Game Over; sem checkpoint na fase, cai para o mesmo fluxo de Game Over.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class RespawnHandler : MonoBehaviour
    {
        [SerializeField] private float deathDelay = 1.2f;
        [SerializeField] private int fallDamage = 1;

        private HealthSystem healthSystem;
        private PlayerController playerController;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            playerController = GetComponent<PlayerController>();
            healthSystem.OnDeath += HandleDeath;
            healthSystem.OnDamaged += HandleDamagedFeedback;
        }

        private void OnDestroy()
        {
            healthSystem.OnDeath -= HandleDeath;
            healthSystem.OnDamaged -= HandleDamagedFeedback;
        }

        /// <summary>Tremor curto de camera quando o jogador leva dano - reforca o impacto.</summary>
        private void HandleDamagedFeedback()
        {
            CameraShake.Shake(0.18f, 0.18f);
        }

        /// <summary>
        /// Chamado pelo FallDeathZone quando o jogador cai para fora do mapa.
        /// </summary>
        public void HandleFallOutOfBounds()
        {
            GameManager manager = GameManager.Instance;
            bool hasCheckpointHere = manager != null
                && !string.IsNullOrEmpty(manager.GetCheckpointScene())
                && manager.GetCheckpointScene() == SceneManager.GetActiveScene().name;

            if (!hasCheckpointHere)
            {
                healthSystem.Kill();
                return;
            }

            transform.position = manager.GetCheckpointPosition();
            playerController?.ResetForRespawn();
            healthSystem.TakeDamage(fallDamage);
        }

        private void HandleDeath()
        {
            // PlayerController ja bloqueia input sozinho quando HealthSystem.IsDead e true.
            GameManager.Instance?.RegisterDeath();
            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            // Aguarda a animacao de morte ser vista antes de trocar de tela.
            yield return new WaitForSeconds(deathDelay);
            SceneController.Instance?.LoadGameOverScreen();
        }
    }
}
