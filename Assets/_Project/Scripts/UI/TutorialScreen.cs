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

        // Altura do painel e espaco entre blocos. O layout e EMPILHADO: cada
        // bloco pede a propria altura e o cursor desce sozinho. Antes as posicoes
        // eram fixas, e quando a linha "Esquivar" entrou na tabela de controles
        // a ultima linha caiu em cima do objetivo - texto sobre texto.
        private const float AlturaPainel = 820f;
        private const float Folga = 14f;
        private float cursor;

        /// <summary>Reserva uma faixa de altura e devolve o centro dela.</summary>
        private float Proximo(float altura, float folgaDepois = Folga)
        {
            float centro = cursor - altura * 0.5f;
            cursor -= altura + folgaDepois;
            return centro;
        }

        private void Construir()
        {
            canvas = UIFactory.CriarCanvas("TutorialScreenCanvas", 4000);

            // Escurecimento cobrindo a cena inteira: a fase continua visivel ao
            // fundo, mas apagada, entao fica claro que o jogo ainda nao comecou.
            // Quase opaco: as placas do tutorial no cenario nao podem competir
            // com a tela por atencao.
            UIFactory.Painel(canvas.transform, "Escurecer", new Color(0.02f, 0.01f, 0.04f, 0.97f),
                             Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var painel = UIFactory.Painel(canvas.transform, "Painel", UIFactory.CorFundoPainel,
                                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          Vector2.zero, Vector2.zero);
            painel.sizeDelta = new Vector2(1080f, AlturaPainel);
            painel.anchoredPosition = Vector2.zero;

            // Fio dourado no topo e na base, como moldura discreta.
            float meia = AlturaPainel * 0.5f;
            UIFactory.Linha(painel, new Vector2(1080f, 3f), new Vector2(0f, meia), UIFactory.CorAcento);
            UIFactory.Linha(painel, new Vector2(1080f, 3f), new Vector2(0f, -meia), UIFactory.CorAcento);

            cursor = meia - 28f;

            UIFactory.Titulo(painel, "Titulo", "Como Jogar", 72, UIFactory.CorTitulo,
                             new Vector2(1000f, 80f), new Vector2(0f, Proximo(80f, 2f)));

            UIFactory.Texto(painel, "Subtitulo", "Ren desperta no cemitério de Aldenor",
                            24, UIFactory.CorTextoFraco, TextAnchor.MiddleCenter,
                            new Vector2(1000f, 30f), new Vector2(0f, Proximo(30f, 22f)));

            // --- Controles: acao a esquerda, tecla a direita ------------------
            foreach (var (acao, tecla) in Controles.Lista)
                LinhaDeControle(painel, acao, tecla);

            cursor -= 8f;
            UIFactory.Linha(painel, new Vector2(880f, 1f), new Vector2(0f, Proximo(1f, 20f)),
                            new Color(0.5f, 0.42f, 0.62f, 0.45f));

            // --- Objetivo e avisos --------------------------------------------
            // Duas linhas de altura reservadas: o objetivo quebra em telas
            // estreitas e nao pode invadir o bloco de baixo.
            UIFactory.Texto(painel, "Objetivo",
                            "Reúna as 6 almas espalhadas pelas fases para abrir a igreja\ne derrote A Vigília.",
                            26, UIFactory.CorAcento, TextAnchor.MiddleCenter,
                            new Vector2(960f, 64f), new Vector2(0f, Proximo(64f, 16f)));

            UIFactory.Texto(painel, "Avisos",
                            "Os altares acesos são checkpoints.\n" +
                            "Cair no abismo custa um coração e devolve você ao último altar.\n" +
                            "Na luta final, a marca no chão mostra onde o golpe vai cair: role para fora dela.",
                            22, UIFactory.CorTexto, TextAnchor.MiddleCenter,
                            new Vector2(980f, 90f), new Vector2(0f, Proximo(90f, 18f)));

            UIFactory.Texto(painel, "Dificuldade",
                            "Dificuldade: " + DifficultySettings.Nome + "  (altere no Menu Principal)",
                            20, UIFactory.CorTextoFraco, TextAnchor.MiddleCenter,
                            new Vector2(940f, 28f), new Vector2(0f, Proximo(28f, 18f)));

            // --- Comecar -------------------------------------------------------
            UIFactory.Botao(painel, "BotaoComecar", "COMEÇAR", 30,
                            new Vector2(340f, 66f), new Vector2(0f, Proximo(66f, 8f)), Continuar);

            UIFactory.Texto(painel, "Atalho", "ou pressione Espaço", 18, UIFactory.CorTextoFraco,
                            TextAnchor.MiddleCenter, new Vector2(600f, 26f), new Vector2(0f, Proximo(26f, 0f)));

            // Se um dia o conteudo crescer alem do painel, o aviso aparece no
            // console em vez de o texto vazar silenciosamente pela moldura.
            if (cursor < -meia)
                Debug.LogWarning("TutorialScreen: o conteudo passou da borda do painel em " + (-meia - cursor) + " px.");
        }

        /// <summary>Uma linha da tabela de controles, no cursor de empilhamento.</summary>
        private void LinhaDeControle(Transform pai, string acao, string tecla)
        {
            Controles.Linha(pai, acao, tecla, Proximo(46f, 10f));
        }
    }
}
