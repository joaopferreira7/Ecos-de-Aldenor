using EcosDeAldenor.Enemies;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Inimigo da Fase 2. Usa o comportamento padrao de EnemyBase (patrulha +
    /// perseguicao ao detectar o jogador), configurado via Inspector.
    /// Nao precisa sobrescrever nada - a classe base ja cobre o comportamento.
    ///
    /// O que o distingue do espectro da Fase 1 e a RESISTENCIA (3 pontos de
    /// vida, tres golpes para cair, contra um golpe so do espectro), e nao mais
    /// o dano. Ele chegou a causar 2 de dano: com os 3 coracoes do Ren e o altar
    /// curando uma unica vez, dois encostoes acabavam a partida - e acabar a
    /// partida, ate aqui, custava o jogo inteiro. A dificuldade Dificil devolve
    /// o golpe pesado (ver DifficultySettings.DanoDeInimigo).
    /// </summary>
    public class GuardiaoDePedra : EnemyBase
    {
    }
}
