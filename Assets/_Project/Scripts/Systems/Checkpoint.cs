using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Altar de alma que serve de checkpoint. Ao ser tocado pelo jogador,
    /// registra a posicao e a cena atual no GameManager, restaura a vida do
    /// jogador ao maximo e acende (a chama de alma brilha em verde e a luz
    /// aumenta) como feedback claro de que foi registrado. Substitui o antigo
    /// pentagrama por um marco mais adequado ao tema "Ecos de Aldenor".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Feedback de ativacao")]
        [SerializeField] private Sprite activatedSprite;
        [SerializeField] private AudioClip activateSfx;

        [Header("Altar de alma (opcional)")]
        [SerializeField] private SpriteRenderer soulRenderer;
        [SerializeField] private Light2D shrineLight;
        [SerializeField] private Color inactiveColor = new Color(0.4f, 0.55f, 0.7f);
        [SerializeField] private Color activeColor = new Color(0.45f, 1f, 0.6f);
        [SerializeField] private float inactiveIntensity = 0.8f;
        [SerializeField] private float activeIntensity = 2.2f;

        private bool activated;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            // Estado "adormecido" ate ser ativado.
            if (shrineLight != null)
            {
                shrineLight.color = inactiveColor;
                shrineLight.intensity = inactiveIntensity;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            GameManager.Instance?.SetCheckpoint(transform.position, SceneManager.GetActiveScene().name);

            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            playerHealth?.ResetHealth();

            if (!activated)
            {
                activated = true;

                if (activatedSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = activatedSprite;
                }

                // Acende o altar: chama de alma verde e luz mais forte.
                if (soulRenderer != null) soulRenderer.color = activeColor;
                if (shrineLight != null)
                {
                    shrineLight.color = activeColor;
                    shrineLight.intensity = activeIntensity;
                }

                AudioManager.Instance?.PlaySfx(activateSfx);
            }
        }
    }
}
