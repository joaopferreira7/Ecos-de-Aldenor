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
                rect.sizeDelta = new Vector2(40f, 40f);

                heartImages[i] = img;
            }
        }

        private void UpdateHearts(int current, int max)
        {
            if (heartImages == null) return;

            for (int i = 0; i < heartImages.Length; i++)
            {
                heartImages[i].sprite = i < current ? heartFullSprite : heartEmptySprite;
            }
        }

        private void UpdateFragmentsText(int total)
        {
            if (fragmentsText != null)
            {
                int goal = GameManager.Instance != null ? GameManager.Instance.FragmentsRequired : total;
                // Contador grande em destaque + objetivo em tom menor/discreto.
                fragmentsText.text = $"{total}<size=18><color=#C9A24B> / {goal}</color></size>";
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
        /// Anuncio central de que as almas exigidas foram reunidas. O aviso e
        /// construido em codigo para nao depender de fiacao em cada cena.
        /// </summary>
        private System.Collections.IEnumerator AnunciarFaseFinal()
        {
            AudioManager.Instance?.PlaySfx(finalPhaseUnlockedSfx);

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
                unlockText.font = fragmentsText != null ? fragmentsText.font
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                unlockText.fontSize = 34;
                unlockText.color = new Color(1f, 0.85f, 0.55f, 1f);
                unlockText.text = "AS ALMAS FORAM REUNIDAS\n<size=20><color=#B9A7D6>O santuario final se abriu</color></size>";

                var sombra = go.AddComponent<Shadow>();
                sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
                sombra.effectDistance = new Vector2(2f, -2f);
            }

            unlockText.gameObject.SetActive(true);
            Color baseCor = unlockText.color;

            // Entra, permanece legivel e sai. Tempo nao-escalonado para o aviso
            // nao ser afetado por hit stop ou pausa.
            yield return Fade(baseCor, 0f, 1f, 0.35f);
            float t = 0f;
            while (t < 2.4f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Fade(baseCor, 1f, 0f, 0.7f);

            unlockText.gameObject.SetActive(false);
            unlockBanner = null;
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
