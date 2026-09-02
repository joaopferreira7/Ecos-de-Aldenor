using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Tela inicial de tutorial: o primeiro que o jogador ve depois de apertar
    /// "Jogar". Explica os controles e o objetivo, e so libera a fase quando ele
    /// escolhe comecar.
    ///
    /// Por que uma tela, e nao apenas as placas espalhadas pelo cenario: as
    /// placas continuam la e continuam sendo o melhor jeito de ensinar (ensinam
    /// no lugar onde o comando e usado), mas elas so aparecem depois que o
    /// jogador ja esta andando. Quem nunca jogou precisa saber ANTES qual tecla
    /// pula e qual botao ataca - senao o primeiro minuto e gasto tentando
    /// descobrir isso. A tela cobre esse primeiro minuto; as placas seguem
    /// reforcando durante a caminhada.
    ///
    /// A tela e curta de proposito: quatro controles, uma linha de objetivo e
    /// tres avisos. Tudo o que passa disso ninguem le.
    ///
    /// O tempo fica congelado (timeScale = 0) enquanto ela esta aberta, entao o
    /// jogador nao leva dano nem cai enquanto le.
    /// </summary>
    public class TutorialScreen : MonoBehaviour
    {
        [Tooltip("Deixe ligado para a tela aparecer assim que a cena abrir.")]
        [SerializeField] private bool mostrarAoIniciar = true;

        private Canvas canvas;
        private bool aberta;

        private void Start()
        {
            if (mostrarAoIniciar) Abrir();
        }

        public void Abrir()
        {
            if (aberta) return;
            aberta = true;

            UIFactory.GarantirEventSystem();
            Construir();

            // Congela o jogo enquanto a tela esta aberta. E o mesmo recurso do
            // menu de pausa, entao nada precisa saber que esta "no tutorial".
            Time.timeScale = 0f;
        }

        public void Continuar()
        {
            if (!aberta) return;
            aberta = false;

            Time.timeScale = 1f;

            // Se o jogador tiver apertado ESC com a tela aberta, o GameManager
            // ficou marcado como pausado; sem isto o proximo ESC re-pausaria o
            // jogo em vez de despausar.
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
                GameManager.Instance.TogglePause();

            if (canvas != null) Destroy(canvas.gameObject);
        }

        private void Update()
        {
            if (!aberta) return;

            // Alem do botao: Espaco ou Enter tambem comecam. O jogador ja esta
            // com a mao no teclado, e Espaco e a tecla de pulo que ele acabou de
            // aprender na propria tela.
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Continuar();
            }
        }

        private void OnDestroy()
        {
            // Trocar de cena com a tela aberta nao pode deixar o jogo congelado.
            if (aberta) Time.timeScale = 1f;
        }

        private void Construir()
        {
            canvas = UIFactory.CriarCanvas("TutorialScreenCanvas", 4000);

            // Escurecimento cobrindo a cena inteira: a fase continua visivel ao
            // fundo, mas apagada, entao fica claro que o jogo ainda nao comecou.
            UIFactory.Painel(canvas.transform, "Escurecer", new Color(0.02f, 0.01f, 0.04f, 0.88f),
                             Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var painel = UIFactory.Painel(canvas.transform, "Painel", UIFactory.CorFundoPainel,
                                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          Vector2.zero, Vector2.zero);
            painel.sizeDelta = new Vector2(1080f, 720f);
            painel.anchoredPosition = Vector2.zero;

            // Fio dourado no topo e na base, como moldura discreta.
            UIFactory.Linha(painel, new Vector2(1080f, 3f), new Vector2(0f, 360f), UIFactory.CorAcento);
            UIFactory.Linha(painel, new Vector2(1080f, 3f), new Vector2(0f, -360f), UIFactory.CorAcento);

            UIFactory.Texto(painel, "Titulo", "COMO JOGAR", 52, UIFactory.CorTitulo,
                            TextAnchor.MiddleCenter, new Vector2(1000f, 70f), new Vector2(0f, 288f),
                            FontStyle.Bold);

            UIFactory.Texto(painel, "Subtitulo", "Ren desperta no cemitério de Aldenor",
                            22, UIFactory.CorTextoFraco, TextAnchor.MiddleCenter,
                            new Vector2(1000f, 34f), new Vector2(0f, 244f));

            // --- Controles: acao a esquerda, tecla a direita ------------------
            float y = 190f;
            LinhaDeControle(painel, "Mover", "A  /  D     ou     ←  →", ref y);
            LinhaDeControle(painel, "Pular", "Espaço", ref y);
            LinhaDeControle(painel, "Atacar", "Botão esquerdo do mouse", ref y);
            LinhaDeControle(painel, "Esquivar", "Shift  ou  botão direito", ref y);
            LinhaDeControle(painel, "Pausar", "ESC", ref y);

            UIFactory.Linha(painel, new Vector2(880f, 1f), new Vector2(0f, -46f),
                            new Color(0.5f, 0.42f, 0.62f, 0.45f));

            // --- Objetivo e avisos --------------------------------------------
            UIFactory.Texto(painel, "Objetivo",
                            "Reúna as 6 almas espalhadas pelas fases para abrir a igreja — e derrote A Vigília.",
                            26, UIFactory.CorAcento, TextAnchor.MiddleCenter,
                            new Vector2(940f, 40f), new Vector2(0f, -80f), FontStyle.Bold);

            UIFactory.Texto(painel, "Avisos",
                            "Os altares acesos são checkpoints.\n" +
                            "Cair no abismo custa um coração e devolve você ao último altar.\n" +
                            "Na luta final, a marca no chão mostra onde o golpe vai cair: role para fora dela.",
                            22, UIFactory.CorTexto, TextAnchor.UpperCenter,
                            new Vector2(940f, 110f), new Vector2(0f, -160f));

            UIFactory.Texto(painel, "Dificuldade",
                            "Dificuldade: " + DifficultySettings.Nome + "  (altere no Menu Principal)",
                            20, UIFactory.CorTextoFraco, TextAnchor.MiddleCenter,
                            new Vector2(940f, 30f), new Vector2(0f, -232f));

            // --- Comecar -------------------------------------------------------
            UIFactory.Botao(painel, "BotaoComecar", "COMEÇAR", 30,
                            new Vector2(340f, 66f), new Vector2(0f, -288f), Continuar);

            UIFactory.Texto(painel, "Atalho", "ou pressione Espaço", 18, UIFactory.CorTextoFraco,
                            TextAnchor.MiddleCenter, new Vector2(600f, 26f), new Vector2(0f, -338f));
        }

        /// <summary>Uma linha da tabela de controles, e desce o cursor vertical.</summary>
        private void LinhaDeControle(Transform pai, string acao, string tecla, ref float y)
        {
            var rotulo = UIFactory.Texto(pai, "Acao_" + acao, acao, 28, UIFactory.CorTextoFraco,
                                         TextAnchor.MiddleRight, new Vector2(300f, 44f),
                                         new Vector2(-230f, y));
            rotulo.horizontalOverflow = HorizontalWrapMode.Overflow;

            var caixa = UIFactory.Caixa(pai, "Tecla_" + acao, new Vector2(470f, 46f), new Vector2(160f, y));
            var img = caixa.gameObject.AddComponent<Image>();
            img.sprite = Systems.RuntimeSprites.Solido();
            img.color = new Color(0.18f, 0.14f, 0.24f, 0.9f);

            var texto = UIFactory.Texto(caixa, "Valor", tecla, 26, UIFactory.CorTexto,
                                        TextAnchor.MiddleCenter, new Vector2(470f, 46f), Vector2.zero,
                                        FontStyle.Bold);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;

            y -= 58f;
        }
    }
}
