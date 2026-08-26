using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Player
{
    /// <summary>
    /// Controla o movimento, pulo, ataque e animacoes do personagem Ren.
    /// A ponte com o Animator segue exatamente o contrato de parametros do
    /// HeroKnight_AnimController (AnimState, Grounded, AirSpeedY, Jump,
    /// Attack1/2/3, Hurt, Death), original do asset Hero Knight - Pixel Art.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float jumpForce = 7.5f;

        [Header("Deteccao de Chao")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Ataque")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackPointOffset = 0.6f;
        [SerializeField] private float attackRadius = 0.6f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float attackCooldown = 0.25f;
        [SerializeField] private float comboResetTime = 1f;
        [SerializeField] private AudioClip attackSfx;
        [Tooltip("Som tocado ao pular.")]
        [SerializeField] private AudioClip jumpSfx;

        [Header("VFX de Impacto")]
        [Tooltip("Frames da faisca de impacto (Hitspark FX) disparada ao acertar um inimigo.")]
        [SerializeField] private Sprite[] hitSparkFrames;
        [SerializeField] private float hitSparkScale = 1.1f;
        [SerializeField] private float hitSparkFps = 24f;
        [SerializeField] private Color hitSparkTint = Color.white;

        [Header("Impacto / Game feel")]
        [Tooltip("Forca do recuo (knockback) aplicado ao inimigo acertado.")]
        [SerializeField] private float knockbackForce = 6f;
        [Tooltip("Duracao do congelamento de tempo (hit stop) ao acertar.")]
        [SerializeField] private float hitStopDuration = 0.05f;

        private Rigidbody2D rb;
        private HealthSystem healthSystem;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private bool isGrounded;
        private float horizontalInput;
        private float attackTimer;
        private float timeSinceAttack;
        private int currentAttack;
        private int facingDirection = 1;
        private float delayToIdle;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            healthSystem = GetComponent<HealthSystem>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            healthSystem.OnDamaged += HandleDamaged;
            healthSystem.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            healthSystem.OnDamaged -= HandleDamaged;
            healthSystem.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (healthSystem.IsDead) return;

            timeSinceAttack += Time.deltaTime;

            ReadInput();
            CheckGrounded();
            HandleAttackCooldown();
            UpdateFacing();
            UpdateAnimatorMovementParams();

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                Jump();
            }
            else if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
            {
                Attack();
            }
            else if (Mathf.Abs(horizontalInput) > Mathf.Epsilon)
            {
                delayToIdle = 0.05f;
                animator.SetInteger("AnimState", 1);
            }
            else
            {
                delayToIdle -= Time.deltaTime;
                if (delayToIdle < 0f)
                {
                    animator.SetInteger("AnimState", 0);
                }
            }
        }

        private void FixedUpdate()
        {
            if (healthSystem.IsDead) return;
            Move();
        }

        private void ReadInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }

        private void Move()
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        private void UpdateFacing()
        {
            // Usa SpriteRenderer.flipX (nao escala negativa) para evitar efeitos
            // colaterais em colliders/fisica ao espelhar o personagem.
            if (horizontalInput > 0f)
            {
                spriteRenderer.flipX = false;
                facingDirection = 1;
            }
            else if (horizontalInput < 0f)
            {
                spriteRenderer.flipX = true;
                facingDirection = -1;
            }

            if (attackPoint != null)
            {
                Vector3 localPos = attackPoint.localPosition;
                localPos.x = Mathf.Abs(attackPointOffset) * facingDirection;
                attackPoint.localPosition = localPos;
            }
        }

        private void UpdateAnimatorMovementParams()
        {
            animator.SetFloat("AirSpeedY", rb.linearVelocity.y);
            animator.SetBool("Grounded", isGrounded);
        }

        private void CheckGrounded()
        {
            if (groundCheck == null) return;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        private void Jump()
        {
            animator.SetTrigger("Jump");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            AudioManager.Instance?.PlaySfx(jumpSfx);
        }

        private void HandleAttackCooldown()
        {
            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            attackTimer = attackCooldown;

            currentAttack++;
            if (currentAttack > 3) currentAttack = 1;
            if (timeSinceAttack > comboResetTime) currentAttack = 1;

            animator.SetTrigger("Attack" + currentAttack);
            AudioManager.Instance?.PlaySfx(attackSfx);
            timeSinceAttack = 0f;

            if (attackPoint == null) return;

            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
            bool hitSomething = false;
            foreach (Collider2D enemy in hitEnemies)
            {
                HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
                if (enemyHealth == null) continue;

                enemyHealth.TakeDamage(attackDamage);
                hitSomething = true;

                // Faisca de impacto no ponto de contato: da peso e clareza ao golpe.
                if (hitSparkFrames != null && hitSparkFrames.Length > 0)
                {
                    Vector3 sparkPos = enemy.ClosestPoint(attackPoint.position);
                    OneShotVFX.Spawn(hitSparkFrames, sparkPos, hitSparkScale, hitSparkFps, 25, hitSparkTint);
                }

                // Recuo (knockback) na direcao do golpe.
                var eb = enemy.GetComponent<EcosDeAldenor.Enemies.EnemyBase>();
                if (eb != null)
                {
                    Vector2 dir = new Vector2(facingDirection, 0.2f).normalized;
                    eb.ApplyKnockback(dir, knockbackForce);
                }
            }

            // Hit stop: um breve congelamento do tempo da peso ao impacto.
            if (hitSomething) HitStop.Do(hitStopDuration);
        }

        private void HandleDamaged()
        {
            animator.SetTrigger("Hurt");
        }

        private void HandleDeath()
        {
            animator.SetTrigger("Death");
        }

        /// <summary>
        /// Chamado pelo RespawnHandler apos reposicionar o jogador no checkpoint.
        /// O estado 'Death' do Animator nao possui transicao de saida (e terminal
        /// por design do asset original), entao e necessario forcar a volta para
        /// Idle manualmente, alem de zerar velocidade e estado de combate.
        /// </summary>
        public void ResetForRespawn()
        {
            rb.linearVelocity = Vector2.zero;
            currentAttack = 0;
            attackTimer = 0f;
            timeSinceAttack = 0f;

            animator.Rebind();
            animator.Update(0f);
            animator.Play("Idle", 0, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            }
        }
    }
}
