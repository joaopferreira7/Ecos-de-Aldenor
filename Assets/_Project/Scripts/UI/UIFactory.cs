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
        public static readonly Color CorFundoPainel = new Color(0.07f, 0.05f, 0.10f, 0.94f);
        public static readonly Color CorBotao = new Color(0.14f, 0.10f, 0.18f, 0.92f);
        public static readonly Color CorBotaoDestaque = new Color(0.35f, 0.18f, 0.45f, 1f);
        public static readonly Color CorTexto = new Color(0.90f, 0.85f, 0.98f, 1f);
        public static readonly Color CorTextoFraco = new Color(0.70f, 0.62f, 0.80f, 1f);
        public static readonly Color CorTitulo = new Color(0.78f, 0.92f, 1f, 1f);
        public static readonly Color CorAcento = new Color(0.95f, 0.80f, 0.42f, 1f);

        public static Font Fonte()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            t.fontStyle = estilo;
            t.color = cor;
            t.alignment = alinhamento;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
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
