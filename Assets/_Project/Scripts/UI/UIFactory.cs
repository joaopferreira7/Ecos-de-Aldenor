using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Pecas de UI montadas por codigo, na mesma linguagem visual dos menus que
    /// ja existiam na cena (fundo roxo escuro quase opaco, destaque roxo, texto
    /// lilas claro, titulo azulado).
    ///
    /// A tela de tutorial e o seletor de dificuldade sao construidos em runtime
    /// pelo mesmo motivo que a barra do chefe ja era: sao paineis com muitos
    /// elementos repetidos, e monta-los no editor significa dezenas de objetos a
    /// mais no YAML da cena para revisar em cada merge. Aqui as cores e as
    /// medidas ficam num arquivo so, e as duas telas novas saem iguais.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color CorFundoPainel = new Color(0.07f, 0.05f, 0.10f, 1f);   // opaco: em espaco linear, 0.94 ja deixava o fundo vazar
        public static readonly Color CorBotao = new Color(0.14f, 0.10f, 0.18f, 0.92f);
        public static readonly Color CorBotaoDestaque = new Color(0.35f, 0.18f, 0.45f, 1f);
        public static readonly Color CorTexto = new Color(0.90f, 0.85f, 0.98f, 1f);
        public static readonly Color CorTextoFraco = new Color(0.70f, 0.62f, 0.80f, 1f);
        public static readonly Color CorTitulo = new Color(0.78f, 0.92f, 1f, 1f);
        public static readonly Color CorAcento = new Color(0.95f, 0.80f, 0.42f, 1f);

        // TIPOGRAFIA
        //
        // O jogo inteiro era escrito em Arial (a LegacyRuntime do Unity), e era
        // o que mais o fazia parecer prototipo: uma fonte de sistema, lisa,
        // ao lado de arte em pixel. Agora sao duas fontes (licenca OFL, em
        // Resources/Fonts com os textos de licenca):
        //   - New Rocker, gotica, so para TITULOS - e a voz do jogo;
        //   - Pixelify Sans, de pixel, para todo o resto.
        // A primeira escolha de titulo foi uma gotica de pixel (Jacquard 24), e
        // ela foi trocada porque as maiusculas goticas confundem justamente as
        // palavras do jogo: o "A" vira "X" e o "V" vira "D" ("X Bigilia",
        // "Ditoria"). A New Rocker mantem o ar gotico com maiusculas legiveis.
        // As duas tem todos os acentos do portugues. NAO tem setas (← →): quem
        // escreve texto de controle usa a palavra "setas".
        private static Font fonteTexto;
        private static Font fonteTitulo;

        /// <summary>Fonte de texto corrido, botoes e HUD.</summary>
        public static Font Fonte()
        {
            if (fonteTexto == null) fonteTexto = Carregar("Fonts/PixelifySans");
            return fonteTexto;
        }

        /// <summary>Fonte gotica dos titulos.</summary>
        public static Font FonteTitulo()
        {
            if (fonteTitulo == null) fonteTitulo = Carregar("Fonts/NewRocker");
            return fonteTitulo;
        }

        private static Font Carregar(string caminho)
        {
            Font f = Resources.Load<Font>(caminho);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        /// <summary>
        /// Canvas proprio, por cima de tudo. A tela de tutorial usa um destes em
        /// vez de entrar no Canvas do HUD para nao herdar nada dele (nem ordem,
        /// nem escala) e para poder ser destruida inteira de uma vez.
        /// </summary>
        public static Canvas CriarCanvas(string nome, int ordem, Transform pai = null)
        {
            var go = new GameObject(nome);
            if (pai != null) go.transform.SetParent(pai, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordem;

            var escala = go.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 0f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Painel(Transform pai, string nome, Color cor,
                                           Vector2 ancoraMin, Vector2 ancoraMax,
                                           Vector2 deslocMin, Vector2 deslocMax)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(pai, false);
            rt.anchorMin = ancoraMin;
            rt.anchorMax = ancoraMax;
            rt.offsetMin = deslocMin;
            rt.offsetMax = deslocMax;

            var img = go.AddComponent<Image>();
            img.sprite = RuntimeSprites.Solido();
            img.color = cor;
            return rt;
        }

        /// <summary>Caixa de tamanho fixo, ancorada no centro do pai.</summary>
        public static RectTransform Caixa(Transform pai, string nome, Vector2 tamanho, Vector2 posicao)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(pai, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamanho;
            rt.anchoredPosition = posicao;
            return rt;
        }

        public static Text Texto(Transform pai, string nome, string conteudo, int tamanhoFonte,
                                 Color cor, TextAnchor alinhamento, Vector2 tamanho, Vector2 posicao,
                                 FontStyle estilo = FontStyle.Normal)
        {
            var rt = Caixa(pai, nome, tamanho, posicao);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = conteudo;
            t.font = Fonte();
            t.fontSize = tamanhoFonte;
            // Negrito e italico sao SINTETICOS na fonte de pixel (o Unity engrossa
            // ou inclina o desenho), e em pixel isso borra o contorno. A hierarquia
            // vem de tamanho, cor e da fonte de titulo, nunca de estilo.
            t.fontStyle = FontStyle.Normal;
            t.color = cor;
            t.alignment = alinhamento;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Texto na fonte gotica de titulo.</summary>
        public static Text Titulo(Transform pai, string nome, string conteudo, int tamanhoFonte,
                                  Color cor, Vector2 tamanho, Vector2 posicao)
        {
            var t = Texto(pai, nome, conteudo, tamanhoFonte, cor, TextAnchor.MiddleCenter, tamanho, posicao);
            t.font = FonteTitulo();
            return t;
        }

        /// <summary>
        /// Botao no estilo dos que ja estao no Menu Principal: retangulo roxo
        /// escuro, texto lilas centralizado e realce roxo ao passar o mouse.
        /// </summary>
        public static Button Botao(Transform pai, string nome, string rotulo, int tamanhoFonte,
                                   Vector2 tamanho, Vector2 posicao, UnityEngine.Events.UnityAction aoClicar)
        {
            var rt = Caixa(pai, nome, tamanho, posicao);

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RuntimeSprites.Solido();
            img.color = Color.white;

            var botao = rt.gameObject.AddComponent<Button>();
            botao.targetGraphic = img;
            var cores = botao.colors;
            cores.normalColor = CorBotao;
            cores.highlightedColor = CorBotaoDestaque;
            cores.pressedColor = new Color(0.245f, 0.126f, 0.315f, 1f);
            cores.selectedColor = CorBotaoDestaque;
            cores.fadeDuration = 0.12f;
            botao.colors = cores;
            if (aoClicar != null) botao.onClick.AddListener(aoClicar);

            // Os botoes de codigo entram no mesmo esquema de som dos de cena.
            UISounds.Aplicar(botao);

            var texto = Texto(rt, "Text", rotulo, tamanhoFonte, CorTexto, TextAnchor.MiddleCenter,
                              tamanho, Vector2.zero, FontStyle.Bold);
            var trt = texto.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return botao;
        }

        /// <summary>Linha fina de separacao/contorno.</summary>
        public static void Linha(Transform pai, Vector2 tamanho, Vector2 posicao, Color cor)
        {
            var rt = Caixa(pai, "Linha", tamanho, posicao);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RuntimeSprites.Solido();
            img.color = cor;
        }

        // ------------------------------------------------------------------
        // Janela modal (Opcoes, Controles)
        // ------------------------------------------------------------------

        /// <summary>
        /// Janela por cima de tudo, no mesmo desenho da tela de tutorial: fundo
        /// escurecido, painel com fio dourado em cima e embaixo, titulo gotico e
        /// um botao "Voltar" no rodape. Devolve o painel; o conteudo vai entre o
        /// titulo e o rodape. Funciona com o jogo pausado (a UI nao depende do
        /// timeScale), entao serve tanto ao menu quanto a pausa.
        /// </summary>
        public static RectTransform AbrirModal(string titulo, Vector2 tamanho, System.Action aoFechar = null)
        {
            GarantirEventSystem();
            var canvas = CriarCanvas("Modal_" + titulo, 4500);

            Painel(canvas.transform, "Escurecer", new Color(0.02f, 0.01f, 0.04f, 0.9f),
                   Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var painel = Painel(canvas.transform, "Painel", CorFundoPainel,
                                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            painel.sizeDelta = tamanho;

            float meia = tamanho.y * 0.5f;
            Linha(painel, new Vector2(tamanho.x, 3f), new Vector2(0f, meia), CorAcento);
            Linha(painel, new Vector2(tamanho.x, 3f), new Vector2(0f, -meia), CorAcento);

            Titulo(painel, "Titulo", titulo, 72, CorTitulo, new Vector2(tamanho.x - 40f, 90f),
                   new Vector2(0f, meia - 70f));

            var go = canvas.gameObject;
            Botao(painel, "Voltar", "Voltar", 30, new Vector2(260f, 60f), new Vector2(0f, -meia + 60f),
                  () => { aoFechar?.Invoke(); Object.Destroy(go); });
            return painel;
        }

        /// <summary>
        /// Controle deslizante no estilo da UI do jogo: trilho escuro, trilha
        /// roxa preenchida e um puxador dourado.
        /// </summary>
        public static Slider Deslizante(Transform pai, string nome, Vector2 tamanho, Vector2 posicao,
                                        float valor, UnityEngine.Events.UnityAction<float> aoMudar)
        {
            var raiz = Caixa(pai, nome, tamanho, posicao);
            var slider = raiz.gameObject.AddComponent<Slider>();

            Painel(raiz, "Trilho", new Color(0.18f, 0.14f, 0.24f, 1f),
                   new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);

            var areaFill = new GameObject("AreaPreenchida", typeof(RectTransform)).GetComponent<RectTransform>();
            areaFill.SetParent(raiz, false);
            areaFill.anchorMin = new Vector2(0f, 0.3f);
            areaFill.anchorMax = new Vector2(1f, 0.7f);
            areaFill.offsetMin = Vector2.zero;
            areaFill.offsetMax = Vector2.zero;
            var fill = Painel(areaFill, "Preenchido", CorBotaoDestaque, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var areaPuxador = new GameObject("AreaPuxador", typeof(RectTransform)).GetComponent<RectTransform>();
            areaPuxador.SetParent(raiz, false);
            areaPuxador.anchorMin = Vector2.zero;
            areaPuxador.anchorMax = Vector2.one;
            areaPuxador.offsetMin = new Vector2(10f, 0f);
            areaPuxador.offsetMax = new Vector2(-10f, 0f);
            var puxador = Painel(areaPuxador, "Puxador", CorAcento, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            puxador.sizeDelta = new Vector2(20f, 0f);

            slider.fillRect = fill;
            slider.handleRect = puxador;
            slider.targetGraphic = puxador.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = valor;
            if (aoMudar != null) slider.onValueChanged.AddListener(aoMudar);
            return slider;
        }

        /// <summary>
        /// Garante que exista um EventSystem na cena. Sem ele nenhum botao
        /// responde a clique - e as cenas de fase nao tem um.
        /// </summary>
        public static void GarantirEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
