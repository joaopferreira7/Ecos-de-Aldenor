using EcosDeAldenor.Enemies;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Inimigo da Fase 2. Usa o comportamento padrao de EnemyBase (patrulha +
    /// perseguicao ao detectar o jogador). Mais resistente e causa mais dano,
    /// configurado via Inspector (maxHealth = 2-3, contactDamage maior).
    /// Nao precisa sobrescrever nada - a classe base ja cobre o comportamento.
    /// </summary>
    public class GuardiaoDePedra : EnemyBase
    {
    }
}
