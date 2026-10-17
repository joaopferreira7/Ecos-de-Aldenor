using UnityEngine;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Resumo da partida nas telas de Vitoria e Derrota: almas reunidas,
    /// mortes, tempo de jogo e dificuldade.
    ///
    /// As duas telas terminavam numa frase e num botao. O jogo ja contava
    /// almas e mortes (o GameManager guardava tudo isso) e nao mostrava a
    /// ninguem; um numero no fim e o que da peso ao que acabou de acontecer, e
    /// um motivo para jogar de novo no Dificil.
    /// </summary>
    public class ResumoDaPartida : MonoBehaviour
    {
        private void Start()
        {
            var gm = GameManager.Instance;
            int almas = gm != null ? gm.TotalFragments : 0;
            int meta = gm != null ? gm.FragmentsRequired : 6;
            int mortes = gm != null ? gm.GetDeathCount() : 0;
            float segundos = gm != null ? gm.TempoDeJogo : 0f;

            var itens = new (string valor, string rotulo)[]
            {
                (almas + " / " + meta, "almas"),
                (mortes.ToString(), mortes == 1 ? "morte" : "mortes"),
                (Tempo(segundos), "tempo"),
                (DifficultySettings.Nome, "dificuldade"),
            };

            const float largura = 220f, folga = 24f;
            float x = -(itens.Length * largura + (itens.Length - 1) * folga) * 0.5f + largura * 0.5f;
            foreach (var (valor, rotulo) in itens)
            {
                UIFactory.Titulo(transform, "Valor_" + rotulo, valor, 48, UIFactory.CorAcento,
                                 new Vector2(largura, 60f), new Vector2(x, 14f));
                UIFactory.Texto(transform, "Rotulo_" + rotulo, rotulo, 22, UIFactory.CorTextoFraco,
                                TextAnchor.MiddleCenter, new Vector2(largura, 30f), new Vector2(x, -32f));
                x += largura + folga;
            }
        }

        static string Tempo(float segundos)
        {
            int s = Mathf.FloorToInt(segundos);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
