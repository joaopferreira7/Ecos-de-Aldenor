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

        private Image[] heartImages;

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
            if (fragmentsText == null) return;
            fragmentsText.text = $"Fragmentos: {total}";
        }
    }
}
