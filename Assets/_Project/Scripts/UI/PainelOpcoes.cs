using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Janela "Opcoes": volume da musica e dos efeitos.
    ///
    /// O AudioManager ja sabia mudar os dois volumes desde a mixagem do jogo;
    /// faltava so a tela. Os valores mudam ao arrastar (da para ouvir na hora,
    /// porque a trilha da cena continua tocando por baixo) e ficam gravados ao
    /// fechar, valendo tambem na proxima vez que o jogo abrir.
    /// </summary>
    public static class PainelOpcoes
    {
        public static void Abrir()
        {
            var audio = AudioManager.Instance;
            var painel = UIFactory.AbrirModal("Opções", new Vector2(900f, 460f),
                                              () => audio?.SalvarVolumes());

            float musica = audio != null ? audio.MusicVolume : 0.55f;
            float efeitos = audio != null ? audio.SfxVolume : 0.9f;

            Linha(painel, "Música", musica, 60f, v => audio?.SetMusicVolume(v));
            Linha(painel, "Efeitos", efeitos, -30f, v => audio?.SetSfxVolume(v));
        }

        static void Linha(Transform painel, string rotulo, float valor, float y,
                          UnityEngine.Events.UnityAction<float> aoMudar)
        {
            var texto = UIFactory.Texto(painel, "Rotulo_" + rotulo, rotulo, 28, UIFactory.CorTextoFraco,
                                        TextAnchor.MiddleRight, new Vector2(200f, 40f), new Vector2(-300f, y));
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;

            var porcento = UIFactory.Texto(painel, "Valor_" + rotulo, Porcento(valor), 26, UIFactory.CorTexto,
                                           TextAnchor.MiddleLeft, new Vector2(100f, 40f), new Vector2(320f, y));

            UIFactory.Deslizante(painel, "Deslizante_" + rotulo, new Vector2(440f, 34f), new Vector2(30f, y), valor,
                                 v => { aoMudar(v); porcento.text = Porcento(v); });
        }

        static string Porcento(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
