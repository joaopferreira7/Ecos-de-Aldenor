using EcosDeAldenor.Enemies;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Inimigo introdutorio da Fase 1. Apenas patrulha entre dois pontos e
    /// nao persegue o jogador - serve para ensinar o padrao de dano por
    /// contato sem exigir reacao rapida. Morre com 1 hit (definido no
    /// HealthSystem via Inspector, maxHealth = 1).
    /// </summary>
    public class SombraRastejante : EnemyBase
    {
        // Sobrescreve para ignorar deteccao/perseguicao: sempre patrulha.
        protected override void ChasePlayer()
        {
            Patrol();
        }
    }
}
