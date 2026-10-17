using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Controla o HUD principal: exibe a vida do jogador como uma fileira de
    /// icones de coracao (cheio/vazio) -- mais legivel para um sistema de vida
    /// discreto (poucos pontos de vida) do que uma barra continua -- alem do
    /// contador de fragmentos. Assina eventos do HealthSystem e do GameManager
    /// para atualizar a UI apenas quando necessario, sem checagem por frame.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private Transform heartsContainer;
        [SerializeField] private Sprite heartFullSprite;
        [SerializeField] private Sprite heartEmptySprite;
        [SerializeField] private Text fragmentsText;
        [Tooltip("Icone da alma no HUD; recebe um pulso ao coletar um fragmento.")]
        [SerializeField] private RectTransform fragmentIcon;
        [Tooltip("Som tocado quando a ultima alma exigida e reunida.")]
        [SerializeField] private AudioClip finalPhaseUnlockedSfx;

        private Image[] heartImages;
        private int ultimaVida = -1;
        private RectTransform ultimoCoracao;   // pulsa quando resta so ele
        private int lastFragmentCount = -1;
        private Coroutine iconPulse;
        private Coroutine unlockBanner;
        private Text unlockText;

        private void Start()
        {
            if (playerHealth == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerHealth = playerObj.GetComponent<HealthSystem>();
            }

            if (playerHealth != null)
            {
                BuildHearts(playerHealth.MaxHealth);
                MontarPlacas(playerHealth.MaxHealth);
                playerHealth.OnHealthChanged += UpdateHearts;
                UpdateHearts(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsChanged += UpdateFragmentsText;
                UpdateFragmentsText(GameManager.Instance.TotalFragments);
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHearts;
            if (GameManager.Instance != null) GameManager.Instance.OnFragmentsChanged -= UpdateFragmentsText;
        }

        /// <summary>
        /// Cria um icone de coracao para cada ponto de vida maximo do jogador.
        /// </summary>
        private void BuildHearts(int maxHealth)
        {
            if (heartsContainer == null) return;

            heartImages = new Image[maxHealth];
            for (int i = 0; i < maxHealth; i++)
            {
                GameObject heartObj = new GameObject($"Heart_{i}");
                heartObj.transform.SetParent(heartsContainer, false);

                Image img = heartObj.AddComponent<Image>();
                img.sprite = heartFullSprite;
                img.preserveAspect = true;

                RectTransform rect = heartObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(TamCoracao, TamCoracao);
                var le = heartObj.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = TamCoracao;

                heartImages[i] = img;
            }
        }

        private void UpdateHearts(int current, int max)
        {
            if (heartImages == null) return;

            for (int i = 0; i < heartImages.Length; i++)
            {
                bool perdeuAgora = ultimaVida >= 0 && i >= current && i < ultimaVida;
                bool ganhouAgora = ultimaVida >= 0 && i < current && i >= ultimaVida;
                if (perdeuAgora) StartCoroutine(PiscarCoracao(heartImages[i]));
                else heartImages[i].sprite = i < current ? heartFullSprite : heartEmptySprite;
                if (ganhouAgora) StartCoroutine(Pulsar((RectTransform)heartImages[i].transform, 0.35f, 0.4f));
            }

            // Com uma vida so, o ultimo coracao bate - o aviso de perigo que a
            // fileira parada nao dava.
            ultimoCoracao = current == 1 ? (RectTransform)heartImages[0].transform : null;
            if (current != 1) heartImages[0].transform.localScale = Vector3.one;
            ultimaVida = current;
        }

        private void Update()
        {
            if (ultimoCoracao == null) return;
            float s = 1f + 0.12f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 7f));
            ultimoCoracao.localScale = Vector3.one * s;
        }

        /// <summary>
        /// O coracao perdido incha e pisca antes de apagar: um coracao que so
        /// trocava de sprite passava despercebido no meio da luta.
        /// </summary>
        private System.Collections.IEnumerator PiscarCoracao(Image coracao)
        {
            var rt = (RectTransform)coracao.transform;
            coracao.sprite = heartFullSprite;
            for (int i = 0; i < 6; i++)
            {
                coracao.enabled = i % 2 == 0;
                rt.localScale = Vector3.one * (1.45f - i * 0.075f);
                float t = 0f;
                while (t < 0.07f) { t += Time.unscaledDeltaTime; yield return null; }
            }
            coracao.enabled = true;
            coracao.sprite = heartEmptySprite;
            rt.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator Pulsar(RectTransform alvo, float dur, float forca)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                alvo.localScale = Vector3.one * (1f + forca * Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI));
                yield return null;
            }
            alvo.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------
        // Placas do HUD
        // ------------------------------------------------------------------

        const float TamCoracao = 44f;
        static readonly Color CorPlaca = new Color(0.04f, 0.03f, 0.07f, 0.88f);

        /// <summary>
        /// Os coracoes ficavam soltos no canto com um vao de ~100 px entre eles
        /// (o layout esticava os filhos) e o contador de almas era pequeno e
        /// sem apoio. Agora cada um fica numa placa escura com friso dourado,
        /// legivel sobre qualquer parte do cenario.
        /// </summary>
        private void MontarPlacas(int vidas)
        {
            if (heartsContainer != null)
            {
                var layout = heartsContainer.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 6f;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                    layout.childAlignment = TextAnchor.MiddleLeft;
                }
                float largura = vidas * TamCoracao + (vidas - 1) * 6f + 32f;
                var placa = Placa("PlacaVida", heartsContainer.parent, new Vector2(0f, 1f),
                                  new Vector2(18f, -18f), new Vector2(largura, 66f));
                var hc = (RectTransform)heartsContainer;
                hc.SetParent(placa, false);
                hc.anchorMin = Vector2.zero;
                hc.anchorMax = Vector2.one;
                hc.pivot = new Vector2(0.5f, 0.5f);
                hc.offsetMin = new Vector2(16f, 0f);
                hc.offsetMax = new Vector2(-16f, 0f);
            }

            if (fragmentsText != null && fragmentIcon != null)
            {
                var placa = Placa("PlacaAlmas", fragmentsText.transform.parent, new Vector2(1f, 1f),
                                  new Vector2(-18f, -18f), new Vector2(196f, 66f));
                fragmentIcon.SetParent(placa, false);
                fragmentIcon.anchorMin = fragmentIcon.anchorMax = new Vector2(0f, 0.5f);
                fragmentIcon.pivot = new Vector2(0.5f, 0.5f);
                fragmentIcon.sizeDelta = new Vector2(50f, 50f);
                fragmentIcon.anchoredPosition = new Vector2(44f, 0f);

                var tr = fragmentsText.rectTransform;
                tr.SetParent(placa, false);
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.offsetMin = new Vector2(80f, 0f);
                tr.offsetMax = new Vector2(-12f, 0f);
                fragmentsText.alignment = TextAnchor.MiddleLeft;
                fragmentsText.fontSize = 40;
                fragmentsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        private static RectTransform Placa(string nome, Transform pai, Vector2 canto, Vector2 pos, Vector2 tamanho)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pai, false);
            rt.SetAsFirstSibling();   // atras do resto do HUD (e da pausa)
            rt.anchorMin = rt.anchorMax = rt.pivot = canto;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamanho;
            var img = go.GetComponent<Image>();
            img.color = CorPlaca;
            img.raycastTarget = false;

            var friso = new GameObject("Friso", typeof(RectTransform), typeof(Image));
            var fr = (RectTransform)friso.transform;
            fr.SetParent(rt, false);
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 0f);
            fr.pivot = new Vector2(0.5f, 0f);
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = new Vector2(0f, 3f);
            var fi = friso.GetComponent<Image>();
            fi.color = new Color(UIFactory.CorAcento.r, UIFactory.CorAcento.g, UIFactory.CorAcento.b, 0.8f);
            fi.raycastTarget = false;
            return rt;
        }

        private void UpdateFragmentsText(int total)
        {
            if (fragmentsText != null)
            {
                int goal = GameManager.Instance != null ? GameManager.Instance.FragmentsRequired : total;
                // Contador grande em destaque + objetivo em tom menor/discreto.
                fragmentsText.text = $"{total}<size=26><color=#C9A24B> / {goal}</color></size>";
            }

            // Pulso no icone apenas quando o total aumenta (coleta), nao na inicializacao.
            bool coletouAgora = lastFragmentCount >= 0 && total > lastFragmentCount;
            if (fragmentIcon != null && coletouAgora)
            {
                if (iconPulse != null) StopCoroutine(iconPulse);
                iconPulse = StartCoroutine(PulseIcon());
            }

            // Ao reunir a ultima alma exigida, o jogador precisa SABER que a fase
            // final abriu - senao o objetivo do jogo fica invisivel.
            int meta = GameManager.Instance != null ? GameManager.Instance.FragmentsRequired : int.MaxValue;
            if (coletouAgora && lastFragmentCount < meta && total >= meta)
            {
                if (unlockBanner != null) StopCoroutine(unlockBanner);
                unlockBanner = StartCoroutine(AnunciarFaseFinal());
            }

            lastFragmentCount = total;
        }

        /// <summary>
        /// Anuncio central de que as almas exigidas foram reunidas.
        /// </summary>
        private System.Collections.IEnumerator AnunciarFaseFinal()
        {
            yield return Aviso("As almas foram reunidas\n" +
                               "<size=26><color=#B9A7D6>O santuário final se abriu</color></size>",
                               finalPhaseUnlockedSfx, 2.4f);
            unlockBanner = null;
        }

        /// <summary>
        /// Mostra um aviso no centro da tela. Usado tambem pela porta de saida
        /// de uma fase quando ainda ha alma para recolher: e o unico lugar onde
        /// o jogador pode descobrir que faltou algo, e antes disso a porta
        /// simplesmente nao reagia - sem mensagem, sem som, sem pista.
        /// </summary>
        public void MostrarAviso(string texto, float permanencia = 1.8f)
        {
            if (unlockBanner != null) StopCoroutine(unlockBanner);
            unlockBanner = StartCoroutine(AvisoAvulso(texto, permanencia));
        }

        private System.Collections.IEnumerator AvisoAvulso(string texto, float permanencia)
        {
            yield return Aviso(texto, null, permanencia);
            unlockBanner = null;
        }

        /// <summary>
        /// O aviso propriamente dito: entra, permanece legivel e sai. O texto e
        /// construido em codigo para nao depender de fiacao em cada cena, e o
        /// tempo e nao-escalonado para nao ser afetado por hit stop ou pausa.
        /// </summary>
        private System.Collections.IEnumerator Aviso(string texto, AudioClip som, float permanencia)
        {
            AudioManager.Instance?.PlaySfx(som);

            if (unlockText == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                var go = new GameObject("FinalPhaseUnlocked", typeof(RectTransform));
                go.transform.SetParent(canvas != null ? canvas.transform : transform, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 90f);
                rect.sizeDelta = new Vector2(720f, 120f);

                unlockText = go.AddComponent<Text>();
                unlockText.alignment = TextAnchor.MiddleCenter;
                unlockText.supportRichText = true;
                unlockText.raycastTarget = false;
                // Anuncio de virada da historia: fonte gotica de titulo.
                unlockText.font = UIFactory.FonteTitulo();
                unlockText.fontSize = 48;

                var sombra = go.AddComponent<Shadow>();
                sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
                sombra.effectDistance = new Vector2(2f, -2f);
            }

            unlockText.text = texto;
            unlockText.color = new Color(1f, 0.85f, 0.55f, 1f);
            unlockText.gameObject.SetActive(true);
            Color baseCor = unlockText.color;

            yield return Fade(baseCor, 0f, 1f, 0.35f);
            float t = 0f;
            while (t < permanencia) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Fade(baseCor, 1f, 0f, 0.7f);

            unlockText.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator Fade(Color baseCor, float de, float para, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(de, para, Mathf.Clamp01(t / dur));
                unlockText.color = new Color(baseCor.r, baseCor.g, baseCor.b, a);
                yield return null;
            }
            unlockText.color = new Color(baseCor.r, baseCor.g, baseCor.b, para);
        }

        /// <summary>
        /// Da um "punch" de escala no icone de alma para dar feedback de coleta.
        /// Usa tempo nao-escalonado para funcionar mesmo com o jogo pausado.
        /// </summary>
        private System.Collections.IEnumerator PulseIcon()
        {
            const float dur = 0.28f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + 0.4f * Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                fragmentIcon.localScale = Vector3.one * s;
                yield return null;
            }
            fragmentIcon.localScale = Vector3.one;
        }
    }
}
