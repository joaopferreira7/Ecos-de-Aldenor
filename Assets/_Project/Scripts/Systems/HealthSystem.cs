using System;
using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Sistema de vida generico, reutilizavel tanto pelo Player quanto pelos inimigos.
    /// Segue o principio de Responsabilidade Unica (SRP do SOLID): sua unica funcao
    /// e gerenciar vida, dano, cura e morte, sem conhecer detalhes de quem o utiliza.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Configuracao de Vida")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerabilityDuration = 1f;

        [Header("Audio (opcional)")]
        [SerializeField] private AudioClip damageSfx;
        [SerializeField] private AudioClip deathSfx;

        private int currentHealth;
        private bool isInvulnerable;
        private float invulnerabilityTimer;

        // Ultima posicao de onde veio o dano. Permite a quem reage (por exemplo o
        // PlayerController) empurrar a vitima para o lado oposto ao do agressor.
        private Vector2 lastDamageSource;
        private bool hasDamageSource;

        // Eventos para que outros sistemas (UI, animacao, audio) reajam sem
        // que o HealthSystem precise conhece-los diretamente (baixo acoplamento).
        public event Action<int, int> OnHealthChanged; // (atual, maximo)
        public event Action OnDamaged;
        public event Action OnDeath;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;
        public float InvulnerabilityDuration => invulnerabilityDuration;
        public bool IsInvulnerable => isInvulnerable;

        /// <summary>
        /// Devolve de onde veio o ultimo dano, se essa informacao foi fornecida.
        /// </summary>
        public bool TryGetLastDamageSource(out Vector2 source)
        {
            source = lastDamageSource;
            return hasDamageSource;
        }

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (isInvulnerable)
            {
                invulnerabilityTimer -= Time.deltaTime;
                if (invulnerabilityTimer <= 0f)
                {
                    isInvulnerable = false;
                }
            }
        }

        /// <summary>
        /// Aplica dano, respeitando o periodo de invulnerabilidade.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (isInvulnerable || IsDead) return;

            hasDamageSource = false;
            ApplyDamage(amount);
        }

        /// <summary>
        /// Igual a TakeDamage, mas informa de onde veio o golpe para que a reacao
        /// (recuo, VFX) possa ser direcional.
        /// </summary>
        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (isInvulnerable || IsDead) return;

            lastDamageSource = sourcePosition;
            hasDamageSource = true;
            ApplyDamage(amount);
        }

        private void ApplyDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();

            if (IsDead)
            {
                AudioManager.Instance?.PlaySfx(deathSfx);
                OnDeath?.Invoke();
            }
            else
            {
                AudioManager.Instance?.PlaySfx(damageSfx);
                isInvulnerable = true;
                invulnerabilityTimer = invulnerabilityDuration;
            }
        }

        /// <summary>
        /// Concede um periodo de invulnerabilidade sem causar dano. Usado ao
        /// reaparecer num checkpoint: sem isso um inimigo proximo pode acertar o
        /// jogador no mesmo instante em que ele volta, e o empurrao resultante
        /// pode joga-lo de novo no abismo.
        /// </summary>
        public void GrantInvulnerability(float duration)
        {
            if (IsDead) return;

            isInvulnerable = true;
            invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, duration);
        }

        /// <summary>
        /// Aplica dano ignorando a invulnerabilidade. Usado pela queda fora do
        /// mapa: cair logo apos levar um golpe nao pode sair de graca.
        /// </summary>
        public void ForceDamage(int amount)
        {
            if (IsDead) return;

            isInvulnerable = false;
            hasDamageSource = false;
            ApplyDamage(amount);
        }

        /// <summary>
        /// Forca a morte imediatamente, ignorando invulnerabilidade. Usado por
        /// exemplo quando o jogador cai para fora do mapa.
        /// </summary>
        public void Kill()
        {
            if (IsDead) return;

            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            AudioManager.Instance?.PlaySfx(deathSfx);
            OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Restaura a vida ao maximo. Usado ao reiniciar em um checkpoint.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isInvulnerable = false;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
