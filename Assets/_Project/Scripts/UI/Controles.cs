using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// A lista de controles do jogo, num lugar so, e a janela que a mostra.
    ///
    /// A mesma tabela aparece em tres lugares - a tela de tutorial, o menu
    /// principal e a pausa. Escrita uma vez aqui, um controle novo (como foi a
    /// esquiva) entra nas tres de uma vez, em vez de alguem lembrar de cada
    /// uma.
    /// </summary>
    public static class Controles
    {
        public static readonly (string acao, string tecla)[] Lista =
        {
            ("Mover", "A / D  ou  setas"),
            ("Pular", "Espaço"),
            ("Atacar", "Botão esquerdo do mouse"),
            ("Esquivar", "Shift  ou  botão direito"),
            ("Pausar", "Esc"),
        };

        /// <summary>Uma linha da tabela: acao a esquerda, tecla numa caixa a direita.</summary>
        public static void Linha(Transform pai, string acao, string tecla, float y)
        {
            var rotulo = UIFactory.Texto(pai, "Acao_" + acao, acao, 28, UIFactory.CorTextoFraco,
                                         TextAnchor.MiddleRight, new Vector2(300f, 44f),
                                         new Vector2(-280f, y));
            rotulo.horizontalOverflow = HorizontalWrapMode.Overflow;

            var caixa = UIFactory.Caixa(pai, "Tecla_" + acao, new Vector2(470f, 46f), new Vector2(110f, y));
            var img = caixa.gameObject.AddComponent<Image>();
            img.sprite = RuntimeSprites.Solido();
            img.color = new Color(0.18f, 0.14f, 0.24f, 0.9f);

            var texto = UIFactory.Texto(caixa, "Valor", tecla, 26, UIFactory.CorTexto,
                                        TextAnchor.MiddleCenter, new Vector2(470f, 46f), Vector2.zero);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        /// <summary>Janela "Controles" (menu principal e pausa).</summary>
        public static void AbrirPainel()
        {
            float altura = 260f + Lista.Length * 56f;
            var painel = UIFactory.AbrirModal("Controles", new Vector2(1000f, altura));

            float y = altura * 0.5f - 160f;
            foreach (var (acao, tecla) in Lista)
            {
                Linha(painel, acao, tecla, y);
                y -= 56f;
            }
        }
    }
}
