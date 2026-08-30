using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Classe base para todos os inimigos do jogo. Implementa patrulha entre
    /// dois pontos, deteccao de jogador por raio, e a integracao de animacao
    /// seguindo o contrato do LightBandit_AnimController (AnimState, Grounded,
    /// Attack, Hurt, Death), original do asset Bandits - Pixel Art.
    ///
    /// O dano de contato usa uma checagem de overlap (nao OnCollisionStay2D),
    /// para nao depender de resolucao fisica de colisao entre corpos dinamicos -
    /// isso evita o inimigo ser 'empurrado' de forma abrupta ao encostar no jogador.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public class EnemyBase : MonoBehaviour
    {
        [Header("Patrulha")]
        [SerializeField] protected Transform pointA;
        [SerializeField] protected Transform pointB;
        [SerializeField] protected float patrolSpeed = 1.5f;

        [Header("Deteccao do Jogador")]
        [SerializeField] protected float detectionRadius = 3f;
        [SerializeField] protected LayerMask playerLayer;

        [Header("Combate")]
        [SerializeField] protected int contactDamage = 1;
        [SerializeField] protected float contactRadius = 0.4f;
        [SerializeField] protected float contactDamageCooldown = 1f;

        [Header("VFX de Morte (opcional)")]
        [SerializeField] protected Sprite[] deathPoofFrames;
        [SerializeField] protected Color deathPoofTint = Color.white;
        [SerializeField] protected float deathPoofScale = 1.2f;

        protected Rigidbody2D rb;
        protected HealthSystem healthSystem;
        protected Animator animator;
        protected Vector3 pointAPos;
        protected Vector3 pointBPos;
        protected Vector3 currentTargetPos;
        protected Transform detectedPlayer;
        protected bool movingToB = true;
        private float contactDamageTimer;
        private int lastAnimState = -1;
        private float stunTimer;
        private float knockbackTimer;
        [SerializeField] protected float hitStunDuration = 0.35f;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            healthSystem = GetComponent<HealthSystem>();
            animator = GetComponent<Animator>();

            // pointA/pointB sao filhos do inimigo no prefab: capturamos a posicao
            // mundial deles uma unica vez aqui, pois senao eles se moveriam junto
            // com o inimigo (que e o proprio pai) e a patrulha nunca chegaria ao alvo.
            if (pointA != null) pointAPos = pointA.position;
            if (pointB != null) pointBPos = pointB.position;
            currentTargetPos = pointBPos;

            healthSystem.OnDamaged += HandleDamaged;
            healthSystem.OnDeath += HandleDeath;
        }

        protected virtual void OnDestroy()
        {
            healthSystem.OnDamaged -= HandleDamaged;
            healthSystem.OnDeath -= HandleDeath;
        }

        protected virtual void Update()
        {
            if (healthSystem.IsDead) return;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                // Durante o recuo do golpe (knockback), deixa a velocidade coastar
                // e desacelerar suavemente; depois disso, trava o movimento horizontal.
                if (knockbackTimer > 0f)
                {
                    knockbackTimer -= Time.deltaTime;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                return; // enquanto atordoado, nao persegue, nao patrulha e nao causa dano de contato
            }

            DetectPlayer();
            CheckContactDamage();

            if (contactDamageTimer > 0f)
            {
                contactDamageTimer -= Time.deltaTime;
            }

            if (detectedPlayer != null)
            {
                ChasePlayer();
            }
            else
            {
                Patrol();
            }
        }

        /// <summary>
        /// Movimento de patrulha entre pointA e pointB. Vira de direcao ao chegar perto do alvo.
        /// </summary>
        protected virtual void Patrol()
        {
            if (pointA == null || pointB == null) return;

            MoveTowards(currentTargetPos, patrolSpeed);

            if (Vector2.Distance(transform.position, currentTargetPos) < 0.2f)
            {
                movingToB = !movingToB;
                currentTargetPos = movingToB ? pointBPos : pointAPos;
            }
        }

        protected virtual void DetectPlayer()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
            detectedPlayer = hit != null ? hit.transform : null;
        }

        protected virtual void ChasePlayer()
        {
            // O inimigo persegue, mas nao abandona o proprio territorio: o alvo
            // e limitado a faixa entre os dois pontos de patrulha.
            //
            // Sem esse limite ele seguia o jogador ate qualquer lugar - andava
            // para fora da borda e caia no abismo, e pior, empurrava o jogador
            // junto. Perto de um altar de checkpoint isso virava um ciclo: o
            // jogador renascia, era empurrado de volta para o vazio e caia de
            // novo ate perder todas as vidas.
            float limiteA = Mathf.Min(pointAPos.x, pointBPos.x);
            float limiteB = Mathf.Max(pointAPos.x, pointBPos.x);
            float alvoX = Mathf.Clamp(detectedPlayer.position.x, limiteA, limiteB);

            MoveTowards(new Vector3(alvoX, transform.position.y, transform.position.z),
                        patrolSpeed * 1.5f);
        }

        /// <summary>
        /// Verifica via overlap (sem depender de fisica de colisao) se o jogador
        /// esta perto o suficiente para levar dano de contato, respeitando um
        /// cooldown para nao aplicar dano a cada frame.
        /// </summary>
        protected virtual void CheckContactDamage()
        {
            if (contactDamageTimer > 0f) return;

            Collider2D hit = Physics2D.OverlapCircle(transform.position, contactRadius, playerLayer);
            if (hit != null)
            {
                HealthSystem targetHealth = hit.GetComponent<HealthSystem>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(contactDamage, transform.position);
                    contactDamageTimer = contactDamageCooldown;
                }
            }
        }

        protected void MoveTowards(Vector3 target, float speed)
        {
            Vector2 direction = (target - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

            // Segue a convencao original do asset Bandits: input positivo (para a
            // direita) espelha a escala para -1 (o sprite base olha para a direita
            // por padrao neste pacote).
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                float absScale = Mathf.Abs(scale.x);
                scale.x = direction.x > 0f ? -absScale : absScale;
                transform.localScale = scale;
            }

            UpdateAnimator(Mathf.Abs(direction.x) > 0.01f);
        }

        protected void UpdateAnimator(bool isMoving)
        {
            if (animator == null) return;

            int animState = isMoving ? 2 : 0; // 2 = Run, 0 = Idle (contrato do LightBandit_AnimController)
            if (animState != lastAnimState)
            {
                animator.SetInteger("AnimState", animState);
                lastAnimState = animState;
            }

            animator.SetBool("Grounded", true);
        }

        /// <summary>
        /// Aplica um impulso de recuo (knockback) ao inimigo, na direcao do golpe.
        /// Chamado pelo jogador ao acertar. O impulso decai durante o atordoamento.
        /// </summary>
        public virtual void ApplyKnockback(Vector2 direction, float force)
        {
            if (rb == null) return;
            rb.linearVelocity = new Vector2(direction.x * force,
                rb.linearVelocity.y + Mathf.Max(0f, direction.y) * force * 0.3f);
            stunTimer = hitStunDuration;
            knockbackTimer = hitStunDuration;
        }

        protected virtual void HandleDamaged()
        {
            // Usa a comparacao sobrecarregada do Unity (nao o operador ?. do C#),
            // pois inimigos com sprites Gothic nao tem Animator - o ?. nao trata
            // o "null falso" do Unity e lancaria MissingComponentException.
            if (animator != null) animator.SetTrigger("Hurt");
            stunTimer = hitStunDuration;
        }

        protected virtual void HandleDeath()
        {
            if (animator != null) animator.SetTrigger("Death");

            // "Poof" de morte: pequena baforada de particulas/explosao no lugar do
            // inimigo, tingida com a cor do inimigo, dando feedback claro do abate.
            if (deathPoofFrames != null && deathPoofFrames.Length > 0)
            {
                OneShotVFX.Spawn(deathPoofFrames, transform.position, deathPoofScale, 16f, 20, deathPoofTint);
            }

            Destroy(gameObject, 0.6f); // tempo para a animacao de morte tocar antes de remover
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, contactRadius);

            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pointA.position, pointB.position);
            }
        }
    }
}
