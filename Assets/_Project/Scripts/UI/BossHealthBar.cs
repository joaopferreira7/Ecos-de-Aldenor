using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Systems;
using EcosDeAldenor.Enemies;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Barra de vida do chefe, exibida no topo da tela durante a luta contra
    /// "A Vigilia". Constroi a propria UI em tempo de execucao (moldura + fundo
    /// + trilha de dano + preenchimento + nome) e se vincula ao HealthSystem do
    /// chefe via eventos, sem checagem por frame. Deve estar sob um Canvas.
    ///
    /// IMPORTANTE (bug corrigido): um Image do tipo Filled SO respeita o
    /// fillAmount se tiver um sprite de origem. Sem sprite, o Unity desenha o
    /// quad inteiro e ignora o preenchimento - por isso a barra "nao diminuia"
    /// e ficava com aparencia estranha. Aqui geramos um sprite branco solido em
    /// runtime e o atribuimos as imagens, garantindo o preenchimento correto.
    ///
    /// Game feel: o preenchimento vermelho cai suavemente ate a vida atual,
    /// enquanto uma "trilha" clara atras dele recua mais devagar, revelando por
    /// um instante o quanto de dano acabou de ser causado (estilo classico de
    /// barras de chefe).
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private string bossName = "A Vigília";
        [SerializeField] private HealthSystem bossHealth;

        [Header("Cores")]
        [SerializeField] private Color fillColor = new Color(0.78f, 0.10f, 0.14f);
        [SerializeField] private Color trailColor = new Color(0.95f, 0.75f, 0.35f);
        [SerializeField] private Color frameColor = new Color(0.62f, 0.50f, 0.78f, 0.95f);

        private Image fillImage;   // preenchimento vermelho (vida atual)
        private Image trailImage;  // trilha clara que recua devagar
        private CanvasGroup group;

        private static Sprite _solidSprite;

        private float targetFill = 1f;   // alvo = vida atual / maxima
        private float displayFill = 1f;  // valor exibido no preenchimento (segue rapido)
        private float trailFill = 1f;    // valor da trilha (segue devagar)

        private void Start()
        {
            if (bossHealth == null)
            {
                var boss = FindObjectOfType<AVigilia>();
                if (boss != null) bossHealth = boss.GetComponent<HealthSystem>();
            }

            BuildUI();

            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged += UpdateBar;
                bossHealth.OnDeath += HandleBossDeath;
                UpdateBar(bossHealth.CurrentHealth, bossHealth.MaxHealth);
                // Inicializa sem animacao de entrada.
                displayFill = trailFill = targetFill;
                ApplyFill();
            }
            else if (group != null)
            {
                group.alpha = 0f; // sem chefe, esconde
            }
        }

        private void OnDestroy()
        {
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged -= UpdateBar;
                bossHealth.OnDeath -= HandleBossDeath;
            }
        }

        private void Update()
        {
            // O preenchimento vermelho persegue o alvo rapidamente...
            displayFill = Mathf.MoveTowards(displayFill, targetFill, Time.deltaTime * 1.6f);
            // ...e a trilha clara o alcanca com atraso, revelando o dano recente.
            if (trailFill > displayFill)
                trailFill = Mathf.MoveTowards(trailFill, displayFill, Time.deltaTime * 0.55f);
            else
                trailFill = displayFill;

            ApplyFill();
        }

        private void ApplyFill()
        {
            if (fillImage != null) fillImage.fillAmount = displayFill;
            if (trailImage != null) trailImage.fillAmount = trailFill;
        }

        private static Font GetFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        /// <summary>
        /// Gera (uma unica vez) um sprite branco solido. Necessario para que os
        /// Images do tipo Filled respeitem o fillAmount.
        /// </summary>
        private static Sprite GetSolidSprite()
        {
            if (_solidSprite != null) return _solidSprite;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var px = new Color32[64];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            _solidSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            _solidSprite.name = "BossBarSolid";
            return _solidSprite;
        }

        private static Image AddImage(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax, bool filled)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.sprite = GetSolidSprite();
            img.color = color;
            if (filled)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                img.fillAmount = 1f;
            }
            return img;
        }

        private void BuildUI()
        {
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            // Painel raiz, ancorado no topo-centro da tela.
            var panel = new GameObject("BossBarPanel", typeof(RectTransform));
            var prt = panel.GetComponent<RectTransform>();
            prt.SetParent(transform, false);
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -22f);
            prt.sizeDelta = new Vector2(760f, 62f);

            // Nome do chefe com sombra, para leitura sobre qualquer fundo.
            var shadow = new GameObject("BossNameShadow", typeof(RectTransform));
            var srt = shadow.GetComponent<RectTransform>();
            srt.SetParent(prt, false);
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(2f, -28f);
            srt.offsetMax = new Vector2(2f, -2f);
            var shadowText = shadow.AddComponent<Text>();
            StyleName(shadowText, new Color(0f, 0f, 0f, 0.65f));

            var nameGo = new GameObject("BossName", typeof(RectTransform));
            var nrt = nameGo.GetComponent<RectTransform>();
            nrt.SetParent(prt, false);
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0.5f, 1f);
            nrt.offsetMin = new Vector2(0f, -26f);
            nrt.offsetMax = new Vector2(0f, 0f);
            var nameText = nameGo.AddComponent<Text>();
            StyleName(nameText, new Color(0.92f, 0.80f, 0.98f));

            // Moldura externa (borda) da barra.
            var frame = AddImage(prt, "BarFrame", frameColor,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-372f, -60f), new Vector2(372f, -30f), false);
            frame.rectTransform.pivot = new Vector2(0.5f, 1f);

            // Fundo escuro recuado dentro da moldura.
            var bg = AddImage(frame.transform, "BarBG", new Color(0.06f, 0.05f, 0.08f, 0.95f),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(3f, 3f), new Vector2(-3f, -3f), false);

            // Trilha de dano (fica atras do preenchimento principal).
            trailImage = AddImage(bg.transform, "BarTrail", trailColor,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, true);

            // Preenchimento principal (vida atual).
            fillImage = AddImage(bg.transform, "BarFill", fillColor,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, true);

            // Divisores das 3 fases do chefe (marcas em 1/3 e 2/3).
            AddDivider(bg.transform, 1f / 3f);
            AddDivider(bg.transform, 2f / 3f);
        }

        private void StyleName(Text t, Color color)
        {
            t.text = bossName;
            t.font = GetFont();
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void AddDivider(Transform parent, float at)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(at, 0f);
            rt.anchorMax = new Vector2(at, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(2f, -4f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = GetSolidSprite();
            img.color = new Color(0f, 0f, 0f, 0.55f);
        }

        private void UpdateBar(int current, int max)
        {
            targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        }

        private void HandleBossDeath()
        {
            targetFill = 0f;
            if (group != null) group.alpha = 0f;
        }
    }
}
