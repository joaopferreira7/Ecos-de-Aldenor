using UnityEngine;

namespace EcosDeAldenor.Core
{
    public enum Dificuldade
    {
        Facil = 0,
        Medio = 1,
        Dificil = 2
    }

    /// <summary>
    /// Ponto unico onde a dificuldade e escolhida, guardada e traduzida em
    /// numeros de jogo.
    ///
    /// E uma classe estatica com PlayerPrefs, e nao mais um Singleton de cena,
    /// porque a escolha precisa sobreviver a duas coisas: a troca de cena
    /// (Menu -> Tutorial -> fases) e o fechamento do jogo. O GameManager ja
    /// persiste entre cenas, mas ele e recriado a cada execucao - a preferencia
    /// do jogador nao deveria voltar ao padrao toda vez que o jogo abre.
    ///
    /// Ninguem le "qual e a dificuldade" para fazer um if: quem precisa pede o
    /// valor ja convertido (vida, dano, tempo de aviso). Assim as regras de
    /// balanceamento ficam todas aqui, num lugar so, em vez de espalhadas em
    /// cada inimigo.
    ///
    /// MEDIO e, por definicao, o jogo como ele sempre foi: todos os
    /// multiplicadores desse nivel valem 1 e todos os valores dos prefabs
    /// passam intactos.
    /// </summary>
    public static class DifficultySettings
    {
        private const string ChavePrefs = "EcosDeAldenor.Dificuldade";

        private static Dificuldade atual = (Dificuldade)(-1); // ainda nao lida do disco

        public static Dificuldade Atual
        {
            get
            {
                if ((int)atual < 0)
                {
                    int salva = PlayerPrefs.GetInt(ChavePrefs, (int)Dificuldade.Medio);
                    atual = (Dificuldade)Mathf.Clamp(salva, 0, 2);
                }
                return atual;
            }
            set
            {
                atual = value;
                PlayerPrefs.SetInt(ChavePrefs, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static string Nome => Atual switch
        {
            Dificuldade.Facil => "Fácil",
            Dificuldade.Dificil => "Difícil",
            _ => "Médio"
        };

        public static string NomeDe(Dificuldade d) => d switch
        {
            Dificuldade.Facil => "Fácil",
            Dificuldade.Dificil => "Difícil",
            _ => "Médio"
        };

        public static string DescricaoDe(Dificuldade d) => d switch
        {
            Dificuldade.Facil => "Um coração a mais, inimigos mais fracos e avisos do chefe bem longos.",
            Dificuldade.Dificil => "Inimigos mais duros, rápidos e violentos, e o chefe quase não avisa.",
            _ => "A experiência original de Ecos de Aldenor."
        };

        /// <summary>Escolhe um valor por dificuldade, na ordem Facil / Medio / Dificil.</summary>
        private static float Por(float facil, float medio, float dificil) => Atual switch
        {
            Dificuldade.Facil => facil,
            Dificuldade.Dificil => dificil,
            _ => medio
        };

        // --- Jogador -------------------------------------------------------

        /// <summary>Coracoes do Ren. O HUD monta a fileira a partir deste valor.</summary>
        public static int VidaDoJogador => Atual switch
        {
            Dificuldade.Facil => 4,
            Dificuldade.Dificil => 3,
            _ => 3
        };

        /// <summary>Tempo de piscada invulneravel depois de levar dano.</summary>
        public static float InvulnerabilidadeDoJogador(float baseVal) => baseVal * Por(1.5f, 1f, 0.8f);

        // --- Inimigos comuns ----------------------------------------------

        public static int VidaDeInimigo(int baseVal) =>
            Mathf.Max(1, Mathf.RoundToInt(baseVal * Por(0.7f, 1f, 1.35f)));

        public static int DanoDeInimigo(int baseVal) =>
            Mathf.Max(1, Mathf.RoundToInt(baseVal * Por(0.6f, 1f, 1.5f)));

        public static float VelocidadeDeInimigo(float baseVal) => baseVal * Por(0.85f, 1f, 1.2f);

        /// <summary>Intervalo minimo entre dois danos de contato do mesmo inimigo.</summary>
        public static float CooldownDeContato(float baseVal) => baseVal * Por(1.6f, 1f, 0.8f);

        // --- Chefe ---------------------------------------------------------

        /// <summary>
        /// Duracao do aviso (telegrafo) antes de um golpe. No Facil o aviso dura
        /// bem mais - e o que torna o chefe legivel para quem nunca jogou.
        /// </summary>
        public static float TelegrafoDoChefe(float baseVal) => baseVal * Por(1.6f, 1f, 0.7f);

        /// <summary>Respiro entre os golpes do chefe (recuo e recarga).</summary>
        public static float RespiroDoChefe(float baseVal) => baseVal * Por(1.35f, 1f, 0.75f);

        /// <summary>Ajuste na chance de o chefe partir para o ataque especial.</summary>
        public static float AjusteDeChanceEspecial => Por(-0.08f, 0f, 0.2f);
    }
}
