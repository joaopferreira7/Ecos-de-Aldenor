using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Fundo das telas de menu feito com a arte do proprio jogo, em camadas.
    ///
    /// Menu, Vitoria, Derrota e Creditos usavam a mesma imagem do pacote
    /// Brackeys - uma sala de masmorra que nao aparece em nenhum lugar do jogo -
    /// quase coberta de preto. A tela que vende o jogo nao mostrava o jogo.
    /// Aqui cada tela monta o seu cenario com as pecas dos pacotes GothicVania:
    /// o cemiterio com a lua no menu e na derrota, a nave da igreja na vitoria.
    ///
    /// Cada camada e uma fileira de sprites lado a lado, ampliada em numero
    /// inteiro de vezes (a arte e pixel e borra em escala quebrada), repetida
    /// ate cobrir a largura e, se tiver velocidade, deslizando devagar - o
    /// parallax das fases, so que no menu.
    /// </summary>
    public class FundoDeCena : MonoBehaviour
    {
        [System.Serializable]
        public class Camada
        {
            public string nome = "Camada";
            public Sprite[] sprites;
            [Tooltip("Ampliacao da arte (pixels de arte -> pixels da tela de referencia 1080p).")]
            public float zoom = 4f;
            [Tooltip("Distancia da base da tela ate a base da camada, em pixels de referencia.")]
            public float y;
            [Tooltip("Cobre a tela inteira com o primeiro sprite (ceu), ignorando 'y'.")]
            public bool cobrirTela;
            [Tooltip("Deslizamento horizontal, em pixels por segundo. 0 = parada.")]
            public float velocidade;
            public Color cor = Color.white;
        }

        [SerializeField] private Camada[] camadas;
        // As fatias da igreja tem 1 px transparente nas bordas e nao cobrem a
        // altura toda: sem uma base lisa, a cor da camera aparecia em frestas.
        [Tooltip("Cor lisa atras das camadas (transparente = sem base).")]
        [SerializeField] private Color corDeBase = Color.clear;
        [Tooltip("Escurecimento por cima de tudo, para o texto ler bem sobre a arte.")]
        [SerializeField] private Color escurecer = new Color(0.02f, 0.01f, 0.05f, 0.45f);

        private readonly List<(RectTransform faixa, float largura, float velocidade)> moveis =
            new List<(RectTransform, float, float)>();

        /// <summary>
        /// Configura por codigo, antes do Start (o prologo monta um cenario
        /// destes por quadro da historia, sem objetos de cena para cada um).
        /// </summary>
        public void Configurar(Camada[] novasCamadas, Color novaCorDeBase, Color novoEscurecer)
        {
            camadas = novasCamadas;
            corDeBase = novaCorDeBase;
            escurecer = novoEscurecer;
        }

        private void Start()
        {
            var raiz = (RectTransform)transform;
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = Vector2.zero;
            raiz.offsetMax = Vector2.zero;
            raiz.SetAsFirstSibling();   // atras de todo o resto da tela

            if (corDeBase.a > 0f)
                UIFactory.Painel(raiz, "Base", corDeBase, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                         .GetComponent<Image>().raycastTarget = false;

            if (camadas != null)
                foreach (var c in camadas) Montar(raiz, c);

            var veu = UIFactory.Painel(raiz, "Escurecer", escurecer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            veu.GetComponent<Image>().raycastTarget = false;
        }

        private void Montar(RectTransform raiz, Camada c)
        {
            if (c.sprites == null || c.sprites.Length == 0 || c.sprites[0] == null) return;

            var faixa = new GameObject(c.nome, typeof(RectTransform)).GetComponent<RectTransform>();
            faixa.SetParent(raiz, false);

            if (c.cobrirTela)
            {
                // Ceu: centralizado e grande o bastante para nao sobrar borda.
                var s = c.sprites[0];
                float zoom = Mathf.Max(c.zoom, 1080f / s.rect.height, 1920f / s.rect.width);
                faixa.anchorMin = faixa.anchorMax = new Vector2(0.5f, 0.5f);
                faixa.sizeDelta = new Vector2(s.rect.width * zoom, s.rect.height * zoom);
                Imagem(faixa, s, c.cor, Vector2.zero, faixa.sizeDelta, new Vector2(0.5f, 0.5f));
                return;
            }

            // Fileira: os sprites em sequencia, repetida ate cobrir a tela (e
            // mais uma volta, se a camada desliza).
            faixa.anchorMin = faixa.anchorMax = new Vector2(0.5f, 0f);
            faixa.pivot = new Vector2(0.5f, 0f);

            float larguraDaSequencia = 0f;
            foreach (var s in c.sprites) if (s != null) larguraDaSequencia += s.rect.width * c.zoom;
            if (larguraDaSequencia <= 1f) return;

            float alvo = 1920f + (c.velocidade > 0f ? larguraDaSequencia : 0f);
            int repeticoes = Mathf.CeilToInt(alvo / larguraDaSequencia) + 1;
            // Parada, a fileira e centralizada: com numero impar de voltas, o
            // centro da tela cai no meio da sequencia (o altar), e nao na emenda.
            if (c.velocidade <= 0f && repeticoes % 2 == 0) repeticoes++;
            float total = larguraDaSequencia * repeticoes;
            float x = -total * 0.5f;
            float altura = 0f;

            for (int r = 0; r < repeticoes; r++)
            {
                foreach (var s in c.sprites)
                {
                    if (s == null) continue;
                    var tam = new Vector2(s.rect.width * c.zoom, s.rect.height * c.zoom);
                    Imagem(faixa, s, c.cor, new Vector2(x + tam.x * 0.5f, 0f), tam, new Vector2(0.5f, 0f));
                    x += tam.x;
                    altura = Mathf.Max(altura, tam.y);
                }
            }

            faixa.sizeDelta = new Vector2(total, altura);
            faixa.anchoredPosition = new Vector2(0f, c.y);
            if (c.velocidade > 0f) moveis.Add((faixa, larguraDaSequencia, c.velocidade));
        }

        private static void Imagem(RectTransform pai, Sprite s, Color cor, Vector2 pos, Vector2 tam, Vector2 pivo)
        {
            var go = new GameObject(s.name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(pai, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, pivo.y);
            rt.pivot = pivo;
            rt.sizeDelta = tam;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.color = cor;
            img.raycastTarget = false;
        }

        private void Update()
        {
            // Tempo real: o fundo continua vivo mesmo com o jogo pausado.
            float t = Time.unscaledTime;
            foreach (var (faixa, largura, velocidade) in moveis)
            {
                float dx = (t * velocidade) % largura;
                faixa.anchoredPosition = new Vector2(-dx + largura * 0.5f, faixa.anchoredPosition.y);
            }
        }
    }
}
