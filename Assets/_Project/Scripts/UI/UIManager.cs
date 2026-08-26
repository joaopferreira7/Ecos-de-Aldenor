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

        private Image[] heartImages;
        private int lastFragmentCount = -1;
        private Coroutine iconPulse;

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
            if (fragmentIcon != null && lastFragmentCount >= 0 && total > lastFragmentCount)
            {
                if (iconPulse != null) StopCoroutine(iconPulse);
                iconPulse = StartCoroutine(PulseIcon());
            }
            lastFragmentCount = total;
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
