using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// O "aparelho de cinema" das cenas dentro do jogo (o epilogo): faixas
    /// pretas que entram por cima e por baixo, caixa de dialogo com o nome de
    /// quem fala e o texto letra por letra, legenda de narracao e cortes para
    /// uma cor (preto, branco).
    ///
    /// Tudo em tempo REAL (unscaled): o hit stop da morte do chefe zera o
    /// timeScale por instantes, e a cena nao pode engasgar com isso.
    ///
    /// Espaco, Enter ou clique completam a frase e depois avancam; ESC pede para
    /// pular a cena inteira (quem dirige le PularPedido).
    /// </summary>
    public class Cinema : MonoBehaviour
    {
        private const float AlturaDaFaixa = 150f;

        private RectTransform faixaDeCima, faixaDeBaixo;
        private CanvasGroup caixa;
        private Text nome, fala, legenda, dica;
        private CanvasGroup grupoDaLegenda;
        private Image cortina;
        private bool avancar;

        public bool PularPedido { get; private set; }

        public static Cinema Criar()
        {
            var canvas = UIFactory.CriarCanvas("Cinema", 4500);
            var cinema = canvas.gameObject.AddComponent<Cinema>();
            cinema.Montar((RectTransform)canvas.transform);
            return cinema;
        }

        private void Montar(RectTransform raiz)
        {
            faixaDeCima = Faixa(raiz, "FaixaDeCima", new Vector2(0f, 1f));
            faixaDeBaixo = Faixa(raiz, "FaixaDeBaixo", new Vector2(0f, 0f));

            // Caixa de dialogo dentro da faixa de baixo: nome em gotico dourado,
            // fala em pixel, um fio dourado separando a faixa da imagem.
            var go = new GameObject("Dialogo", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(faixaDeBaixo, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            caixa = go.AddComponent<CanvasGroup>();
            caixa.alpha = 0f;

            nome = UIFactory.Titulo(rt, "Nome", "", 36, UIFactory.CorAcento, new Vector2(800f, 46f), Vector2.zero);
            Ancorar(nome.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f));
            fala = UIFactory.Texto(rt, "Fala", "", 30, UIFactory.CorTexto, TextAnchor.UpperCenter, new Vector2(1500f, 90f), Vector2.zero);
            Ancorar(fala.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f));
            fala.supportRichText = true;
            dica = UIFactory.Texto(rt, "Dica", "Espaço  ▸", 18, UIFactory.CorTextoFraco, TextAnchor.MiddleRight, new Vector2(300f, 30f), Vector2.zero);
            Ancorar(dica.rectTransform, new Vector2(1f, 0f), new Vector2(-170f, 22f));
            dica.color = Color.clear;

            // Legenda de narracao: mesma faixa, sem nome, em italico de pixel.
            legenda = UIFactory.Texto(faixaDeBaixo, "Legenda", "", 30, UIFactory.CorTexto, TextAnchor.MiddleCenter,
                                      new Vector2(1600f, 110f), Vector2.zero);
            Ancorar(legenda.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            grupoDaLegenda = legenda.gameObject.AddComponent<CanvasGroup>();
            grupoDaLegenda.alpha = 0f;

            // Cortina: tela cheia, por cima de tudo, para os cortes.
            var cort = UIFactory.Painel(raiz, "Cortina", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cortina = cort.GetComponent<Image>();
            cortina.raycastTarget = false;

            var pular = UIFactory.Texto(raiz, "Pular", "Esc  pula a cena", 18, UIFactory.CorTextoFraco, TextAnchor.MiddleRight,
                                        new Vector2(400f, 30f), Vector2.zero);
            Ancorar(pular.rectTransform, new Vector2(1f, 1f), new Vector2(-220f, -24f));
            pular.transform.SetParent(faixaDeCima, true);
        }

        private static RectTransform Faixa(RectTransform raiz, string nome, Vector2 canto)
        {
            var f = UIFactory.Painel(raiz, nome, Color.black, new Vector2(0f, canto.y), new Vector2(1f, canto.y), Vector2.zero, Vector2.zero);
            f.pivot = new Vector2(0.5f, canto.y);
            f.sizeDelta = new Vector2(0f, AlturaDaFaixa);
            f.anchoredPosition = new Vector2(0f, canto.y > 0.5f ? AlturaDaFaixa : -AlturaDaFaixa);
            f.GetComponent<Image>().raycastTarget = false;

            // Fio dourado na borda que encosta na imagem.
            var fio = UIFactory.Painel(f, "Fio", new Color(UIFactory.CorAcento.r, UIFactory.CorAcento.g, UIFactory.CorAcento.b, 0.75f),
                                       new Vector2(0f, 1f - canto.y), new Vector2(1f, 1f - canto.y), Vector2.zero, Vector2.zero);
            fio.pivot = new Vector2(0.5f, 1f - canto.y);
            fio.sizeDelta = new Vector2(0f, 3f);
            fio.GetComponent<Image>().raycastTarget = false;
            return f;
        }

        private static void Ancorar(RectTransform rt, Vector2 ancora, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = ancora;
            rt.anchoredPosition = pos;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) PularPedido = true;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
                avancar = true;
        }

        // ------------------------------------------------------------------

        /// <summary>As faixas pretas entram (1) ou saem (0).</summary>
        public IEnumerator Faixas(bool entrar, float duracao = 0.8f)
        {
            float de = faixaDeBaixo.anchoredPosition.y, para = entrar ? 0f : -AlturaDaFaixa;
            for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
            {
                float y = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / duracao));
                faixaDeBaixo.anchoredPosition = new Vector2(0f, y);
                faixaDeCima.anchoredPosition = new Vector2(0f, -y);
                yield return null;
            }
            faixaDeBaixo.anchoredPosition = new Vector2(0f, para);
            faixaDeCima.anchoredPosition = new Vector2(0f, -para);
        }

        /// <summary>
        /// Uma fala: o nome aparece, o texto e escrito letra por letra e a cena
        /// espera o jogador (ou um tempo proporcional ao tamanho da frase).
        /// </summary>
        public IEnumerator Falar(string quem, string texto, Color corDoNome)
        {
            if (PularPedido) yield break;
            nome.text = quem;
            nome.color = corDoNome;
            if (caixa.alpha < 1f) yield return UIFactory.Desvanecer(caixa, caixa.alpha, 1f, 0.25f);
            yield return Escrever(fala, texto);
            yield return Esperar(2.2f + texto.Length / 28f);
        }

        public IEnumerator FecharDialogo()
        {
            yield return UIFactory.Desvanecer(caixa, caixa.alpha, 0f, 0.3f);
            fala.text = "";
        }

        /// <summary>Narracao na faixa de baixo, sem nome, que some sozinha.</summary>
        public IEnumerator Narrar(string texto, float permanencia)
        {
            if (PularPedido) yield break;
            legenda.text = "";
            yield return UIFactory.Desvanecer(grupoDaLegenda, 0f, 1f, 0.4f);
            yield return Escrever(legenda, texto);
            yield return Esperar(permanencia);
            yield return UIFactory.Desvanecer(grupoDaLegenda, 1f, 0f, 0.6f);
        }

        /// <summary>Cobre a tela com uma cor (alfa 1) ou descobre (alfa 0).</summary>
        public IEnumerator Cortina(Color cor, float alvo, float duracao)
        {
            Color inicio = cortina.color.a > 0.001f ? cortina.color : new Color(cor.r, cor.g, cor.b, 0f);
            for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Lerp(inicio.a, alvo, t / duracao);
                cortina.color = new Color(cor.r, cor.g, cor.b, a);
                yield return null;
            }
            cortina.color = new Color(cor.r, cor.g, cor.b, alvo);
        }

        /// <summary>Espera um tempo, encurtado se o jogador apertar para avancar.</summary>
        public IEnumerator Pausa(float segundos)
        {
            avancar = false;
            for (float t = 0f; t < segundos && !avancar && !PularPedido; t += Time.unscaledDeltaTime) yield return null;
            avancar = false;
        }

        /// <summary>
        /// Maquina de escrever. O resto da frase ja esta no texto, transparente:
        /// a quebra de linha e decidida pela frase inteira desde a primeira letra.
        /// </summary>
        private IEnumerator Escrever(Text alvo, string texto)
        {
            avancar = false;
            float n = 0f;
            while (n < texto.Length && !PularPedido)
            {
                if (avancar) { avancar = false; break; }
                n += 40f * Time.unscaledDeltaTime;
                int k = Mathf.Clamp(Mathf.FloorToInt(n), 0, texto.Length);
                alvo.text = texto.Substring(0, k) + "<color=#00000000>" + texto.Substring(k) + "</color>";
                yield return null;
            }
            alvo.text = texto;
        }

        private IEnumerator Esperar(float limite)
        {
            avancar = false;
            for (float t = 0f; t < limite && !avancar && !PularPedido; t += Time.unscaledDeltaTime)
            {
                float a = 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
                dica.color = new Color(UIFactory.CorTextoFraco.r, UIFactory.CorTextoFraco.g, UIFactory.CorTextoFraco.b, Mathf.Min(1f, t * 2f) * a);
                yield return null;
            }
            avancar = false;
            dica.color = Color.clear;
        }
    }
}
