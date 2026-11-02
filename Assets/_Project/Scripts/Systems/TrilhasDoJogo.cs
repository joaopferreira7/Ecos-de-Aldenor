using System.Collections.Generic;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// A MIXAGEM DAS TRILHAS, num lugar so.
    ///
    /// As faixas vem de pacotes diferentes e nao foram masterizadas juntas. Medido
    /// no Unity (RMS da faixa inteira):
    ///
    ///   Lobby Music  -15,5 dBFS   menu e prologo
    ///   Music 1      -25,2 dBFS   tutorial e Fase 1
    ///   Music 2      -21,4 dBFS   Fase 2
    ///   Music 3      -19,7 dBFS   Fases 3 e 4
    ///   Boss Battle   -8,5 dBFS   chefe
    ///   rpg_village  -15,6 dBFS   epilogo e Vitoria
    ///
    /// Sem correcao, a musica caia uns 10 dB do prologo para o tutorial - o jogo
    /// parecia ficar mudo justo quando comecava - e subia fase a fase. O ganho
    /// abaixo leva todas para perto de -20 dBFS; o chefe fica ~2 dB acima, de
    /// proposito. O ganho de uma faixa daqui vale mais que o da cena.
    ///
    /// Algumas faixas tambem nao emendam no loop: o Music 1 termina num fade de
    /// ~10 s e mais 2 s de silencio (a cada 2 min 15 s a musica sumia e voltava
    /// de supetao), o Music 2 acaba em silencio e recomeca alto, e a da vila tem
    /// quase 2 s de silencio no fim e entra em fade. Para elas a janela de loop
    /// corta o fade final e o AudioManager emenda o fim no comeco com crossfade.
    /// </summary>
    public static class TrilhasDoJogo
    {
        public struct Trilha
        {
            public float ganho;
            /// <summary>Onde o loop recomeca (s).</summary>
            public float inicioDoLoop;
            /// <summary>Onde o loop termina (s); 0 = fim do arquivo, sem janela.</summary>
            public float fimDoLoop;
        }

        private static readonly Dictionary<string, Trilha> trilhas = new Dictionary<string, Trilha>
        {
            { "Lobby Music",        new Trilha { ganho = 0.60f } },
            { "Music 1",            new Trilha { ganho = 1.70f, fimDoLoop = 120.5f } },
            { "Music 2",            new Trilha { ganho = 1.10f, fimDoLoop = 118.5f } },
            { "Music 3",            new Trilha { ganho = 0.92f } },
            { "BossTheme_Junkala",  new Trilha { ganho = 0.36f } },
            { "rpg_village02_loop", new Trilha { ganho = 0.60f, inicioDoLoop = 3f, fimDoLoop = 100.5f } },
        };

        public static bool Para(string nomeDoClipe, out Trilha trilha) => trilhas.TryGetValue(nomeDoClipe, out trilha);
    }
}
