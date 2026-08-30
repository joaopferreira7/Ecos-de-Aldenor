using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Chefe final do jogo. Estende EnemyBase mas sobrescreve o comportamento
    /// de Update com uma maquina de estados propria (padrao State Machine),
    /// pois um chefe precisa de logica de combate mais rica do que a patrulha
    /// simples dos inimigos comuns. A cada 1/3 de vida perdida, o chefe muda
    /// de fase e passa a usar o ataque especial com mais frequencia.
    ///
    /// Camada de "game feel": durante o Telegraph o corpo pisca em vermelho
    /// (aviso claro antes do ataque especial), os ataques disparam VFX e tremor
    /// de camera, e a morte gera uma explosao. Os frames de VFX sao atribuidos
    /// no prefab (fireball/explosion do pacote Brackeys).
    /// </summary>
    public class AVigilia : EnemyBase
    {
        [Header("VFX do Chefe")]
        [SerializeField] private Sprite[] specialVfxFrames;
        [SerializeField] private Sprite[] deathVfxFrames;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.3f, 0.25f);

        private SpriteRenderer bodyRenderer;
        private Color baseColor = Color.white;

        private enum BossState
        {
            Idle,
            Chase,
            AttackBasic,
            AttackSpecial,
            Telegraph,
            Retreat
        }

        [Header("Configuracao do Chefe")]
        [SerializeField] private float basicAttackRange = 1.2f;
        [SerializeField] private float specialAttackRange = 3f;
        [SerializeField] private int basicAttackDamage = 1;
        [SerializeField] private int specialAttackDamage = 2;
        [SerializeField] private float telegraphDuration = 0.8f;
        [SerializeField] private float retreatDuration = 1f;
        [SerializeField] private float stateCooldown = 1.2f;

        [Tooltip("Toque que marca a virada de fase do chefe (a cada terco de vida perdido).")]
        [SerializeField] private AudioClip phaseChangeSfx;

        private BossState currentState = BossState.Idle;
        private float stateTimer;
        private int currentPhase = 1; // 1, 2 ou 3 - muda conforme a vida diminui

        protected override void Awake()
        {
            base.Awake();
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) baseColor = bodyRenderer.color;
            healthSystem.OnHealthChanged += EvaluatePhase;
        }

        protected override void Update()
        {
            // Nao usa a logica de Update da classe base (patrulha simples).
            // O chefe roda sua propria maquina de estados.
            if (healthSystem.IsDead) return;

            DetectPlayer();
            UpdateStateMachine();
            UpdateTelegraphFlash();
        }

        /// <summary>
        /// Enquanto carrega o ataque especial, o corpo pisca em vermelho num ritmo
        /// que acelera conforme o golpe se aproxima - aviso visual claro ao jogador.
        /// </summary>
        private void UpdateTelegraphFlash()
        {
            if (bodyRenderer == null) return;

            if (currentState == BossState.Telegraph)
            {
                float progress = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, telegraphDuration));
                float blink = Mathf.PingPong(Time.time * (6f + progress * 12f), 1f);
                bodyRenderer.color = Color.Lerp(baseColor, telegraphColor, blink);
            }
            else
            {
                bodyRenderer.color = baseColor;
            }
        }

        private void UpdateStateMachine()
        {
            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case BossState.Idle:
                    if (detectedPlayer != null)
                    {
                        ChangeState(BossState.Chase);
                    }
                    break;

                case BossState.Chase:
                    if (detectedPlayer == null)
                    {
                        ChangeState(BossState.Idle);
                        break;
                    }

                    float distance = Vector2.Distance(transform.position, detectedPlayer.position);

                    if (distance <= basicAttackRange)
                    {
                        ChangeState(BossState.AttackBasic);
                    }
                    else if (distance <= specialAttackRange && ShouldUseSpecialAttack())
                    {
                        ChangeState(BossState.Telegraph);
                    }
                    else
                    {
                        MoveTowards(detectedPlayer.position, patrolSpeed * 1.2f);
                    }
                    break;

                case BossState.AttackBasic:
                    if (stateTimer <= 0f)
                    {
                        ApplyDamageIfInRange(basicAttackRange, basicAttackDamage);
                        ChangeState(BossState.Retreat);
                    }
                    break;

                case BossState.Telegraph:
                    // Fase de aviso visual antes do ataque especial - da tempo
                    // para o jogador reagir (bom design de chefes: ataques
                    // fortes devem ser previsiveis, nao instantaneos).
                    if (stateTimer <= 0f)
                    {
                        ChangeState(BossState.AttackSpecial);
                    }
                    break;

                case BossState.AttackSpecial:
                    if (stateTimer <= 0f)
                    {
                        ApplyDamageIfInRange(specialAttackRange, specialAttackDamage);
                        ChangeState(BossState.Retreat);
                    }
                    break;

                case BossState.Retreat:
                    if (stateTimer <= 0f)
                    {
                        ChangeState(detectedPlayer != null ? BossState.Chase : BossState.Idle);
                    }
                    break;
            }
        }

        private void ChangeState(BossState newState)
        {
            currentState = newState;

            if (newState == BossState.AttackBasic)
            {
                if (animator != null) animator.SetTrigger("Attack");
                CameraShake.Shake(0.15f, 0.12f);
            }
            else if (newState == BossState.AttackSpecial)
            {
                if (animator != null) animator.SetTrigger("Attack");
                CameraShake.Shake(0.4f, 0.4f);
                // Explosao no ponto do jogador (ou a frente do chefe, se sumiu).
                Vector3 fxPos = detectedPlayer != null ? detectedPlayer.position : transform.position + Vector3.right * transform.localScale.x;
                OneShotVFX.Spawn(specialVfxFrames, fxPos, 2.2f, 16f);
            }

            UpdateAnimator(newState == BossState.Chase);

            stateTimer = newState switch
            {
                BossState.AttackBasic => stateCooldown * 0.5f,
                BossState.Telegraph => telegraphDuration,
                BossState.AttackSpecial => stateCooldown * 0.5f,
                BossState.Retreat => retreatDuration,
                _ => 0f
            };
        }

        /// <summary>
        /// Quanto mais avancada a fase do chefe, maior a chance de usar o
        /// ataque especial em vez de simplesmente perseguir.
        /// </summary>
        private bool ShouldUseSpecialAttack()
        {
            float chance = currentPhase switch
            {
                1 => 0.2f,
                2 => 0.4f,
                _ => 0.6f
            };
            return Random.value < chance;
        }

        private void ApplyDamageIfInRange(float range, int damage)
        {
            if (detectedPlayer == null) return;

            float distance = Vector2.Distance(transform.position, detectedPlayer.position);
            if (distance <= range)
            {
                HealthSystem playerHealth = detectedPlayer.GetComponent<HealthSystem>();
                if (playerHealth != null) playerHealth.TakeDamage(damage, transform.position);
            }
        }

        protected override void HandleDeath()
        {
            // Explosao grande e tremor forte na morte do chefe.
            Sprite[] deathFrames = (deathVfxFrames != null && deathVfxFrames.Length > 0) ? deathVfxFrames : specialVfxFrames;
            OneShotVFX.Spawn(deathFrames, transform.position, 3.5f, 14f);
            CameraShake.Shake(0.6f, 0.55f);
            if (bodyRenderer != null) bodyRenderer.color = baseColor;
            base.HandleDeath();
        }

        /// <summary>
        /// Avalia a fase do chefe (1, 2 ou 3) com base na vida restante.
        /// Chamado toda vez que o chefe recebe dano.
        /// </summary>
        private void EvaluatePhase(int current, int max)
        {
            float healthPercent = (float)current / max;
            int anterior = currentPhase;

            if (healthPercent <= 1f / 3f)
            {
                currentPhase = 3;
            }
            else if (healthPercent <= 2f / 3f)
            {
                currentPhase = 2;
            }
            else
            {
                currentPhase = 1;
            }

            if (currentPhase > anterior) EntrarNaFase(currentPhase);
        }

        /// <summary>
        /// A luta aperta a cada terco de vida do chefe, e a trilha aperta junto.
        ///
        /// As fases ja mudavam o comportamento dele, mas em silencio: o jogador
        /// via o chefe ficar mais agressivo sem nada marcar a virada. Uma trilha
        /// que sobe de intensidade e um toque de batalha na passagem dizem "isto
        /// mudou" no momento exato em que muda - e e o que faz uma luta longa
        /// parecer ter atos, em vez de um bloco so.
        ///
        /// A intensidade multiplica o volume da trilha; ela volta a 1 sozinha
        /// quando outra cena pede a sua musica, entao a escalada nao vaza para a
        /// tela de Vitoria.
        /// </summary>
        private void EntrarNaFase(int fase)
        {
            AudioManager.Instance?.SetMusicIntensity(fase == 3 ? 1.25f : 1.12f);
            AudioManager.Instance?.PlaySfx(phaseChangeSfx);
            CameraShake.Shake(0.25f, 0.2f);
        }
    }
}
