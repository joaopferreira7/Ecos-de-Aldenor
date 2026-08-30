using System.Collections;
using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Player
{
    /// <summary>
    /// Controla o movimento, pulo, ataque e animacoes do personagem Ren.
    /// A ponte com o Animator segue exatamente o contrato de parametros do
    /// HeroKnight_AnimController (AnimState, Grounded, AirSpeedY, Jump,
    /// Attack1/2/3, Hurt, Death), original do asset Hero Knight - Pixel Art.
    ///
    /// O pulo usa as tecnicas classicas de "game feel" de plataforma:
    /// coyote time (o pulo ainda vale por alguns quadros apos sair da borda),
    /// jump buffer (o comando dado pouco antes de aterrissar nao e perdido),
    /// altura variavel (soltar o botao corta a subida) e gravidade maior na
    /// queda, o que elimina a sensacao "flutuante" do arco simetrico.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 4f;
        [Tooltip("Aceleracao no chao (unidades/s^2). Valores altos = resposta imediata.")]
        [SerializeField] private float groundAcceleration = 90f;
        [Tooltip("Aceleracao no ar - um pouco menor da peso ao salto sem tirar o controle.")]
        [SerializeField] private float airAcceleration = 45f;

        [Header("Pulo")]
        [SerializeField] private float jumpForce = 7.5f;
        [Tooltip("Tempo apos sair do chao em que o pulo ainda e aceito (coyote time).")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("Tempo em que um comando de pulo fica guardado esperando o chao (jump buffer).")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Tooltip("Multiplicador de gravidade durante a queda: deixa a descida mais rapida que a subida.")]
        [SerializeField] private float fallGravityMultiplier = 1.9f;
        [Tooltip("Multiplicador de gravidade ao soltar o botao durante a subida (pulo curto).")]
        [SerializeField] private float lowJumpGravityMultiplier = 2.6f;
        [Tooltip("Velocidade maxima de queda, evita ganhar velocidade absurda em quedas longas.")]
        [SerializeField] private float maxFallSpeed = 16f;

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
        [Tooltip("Som tocado ao aterrissar depois de uma queda com peso.")]
        [SerializeField] private AudioClip landSfx;

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
        [Tooltip("Forca do empurrao sofrido pelo jogador ao levar dano.")]
        [SerializeField] private float hurtKnockbackForce = 5.5f;
        [Tooltip("Tempo em que o jogador perde o controle apos levar dano (deixa o recuo legivel).")]
        [SerializeField] private float hurtControlLock = 0.18f;
        [Tooltip("Cor do flash de invulnerabilidade apos levar dano.")]
        [SerializeField] private Color hurtFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

        private Rigidbody2D rb;
        private HealthSystem healthSystem;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private bool isGrounded;
        private bool wasGrounded;
        private float horizontalInput;
        private float attackTimer;
        private float timeSinceAttack;
        private int currentAttack;
        private int facingDirection = 1;
        private float delayToIdle;

        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool jumpHeld;
        private float baseGravityScale;
        private float hurtLockTimer;
        private float lastFallSpeed;

        private Color baseSpriteColor = Color.white;
        private Coroutine flashRoutine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            healthSystem = GetComponent<HealthSystem>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            baseGravityScale = rb.gravityScale;
            if (spriteRenderer != null) baseSpriteColor = spriteRenderer.color;

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
            if (hurtLockTimer > 0f) hurtLockTimer -= Time.deltaTime;

            ReadInput();
            CheckGrounded();
            HandleAttackCooldown();
            UpdateJumpTimers();
            UpdateFacing();
            UpdateAnimatorMovementParams();

            // Pulo e ataque sao independentes: um nao pode "engolir" o outro no
            // mesmo quadro, como acontecia quando estavam em cadeia else-if.
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                Jump();
            }

            if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
            {
                Attack();
            }

            UpdateAnimationState();
        }

        private void FixedUpdate()
        {
            if (healthSystem.IsDead) return;
            Move();
            ApplyJumpGravity();
        }

        private void ReadInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            jumpHeld = Input.GetButton("Jump");
        }

        /// <summary>
        /// Atualiza as duas janelas de tolerancia do pulo. O coyote time perdoa
        /// quem aperta logo depois de sair da borda; o buffer perdoa quem aperta
        /// logo antes de encostar no chao.
        /// </summary>
        private void UpdateJumpTimers()
        {
            coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;

            if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
            else jumpBufferTimer -= Time.deltaTime;
        }

        private void Move()
        {
            // Sem controle logo apos levar dano, para o recuo ser percebido.
            if (hurtLockTimer > 0f) return;

            float targetSpeed = horizontalInput * moveSpeed;
            float accel = isGrounded ? groundAcceleration : airAcceleration;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        /// <summary>
        /// Gravidade dinamica: mais forte na queda e ao soltar o botao durante a
        /// subida. E o que separa um pulo "flutuante" de um pulo com peso.
        /// </summary>
        private void ApplyJumpGravity()
        {
            float vy = rb.linearVelocity.y;

            if (vy < -0.01f)
            {
                rb.gravityScale = baseGravityScale * fallGravityMultiplier;
                if (vy < -maxFallSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
                }
            }
            else if (vy > 0.01f && !jumpHeld)
            {
                rb.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
            }
            else
            {
                rb.gravityScale = baseGravityScale;
            }
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

        private void UpdateAnimationState()
        {
            if (Mathf.Abs(horizontalInput) > Mathf.Epsilon)
            {
                delayToIdle = 0.05f;
                animator.SetInteger("AnimState", 1);
            }
            else
            {
                delayToIdle -= Time.deltaTime;
                if (delayToIdle < 0f) animator.SetInteger("AnimState", 0);
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

            wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (!isGrounded)
            {
                lastFallSpeed = Mathf.Min(lastFallSpeed, rb.linearVelocity.y);
            }
            else if (!wasGrounded)
            {
                HandleLanding();
            }
        }

        /// <summary>
        /// Aterrissagem: so quedas com alguma altura ganham som, e as mais
        /// fortes tambem sacodem a camera. Pulinhos curtos ficam em silencio
        /// para o feedback nao virar ruido constante.
        /// </summary>
        private void HandleLanding()
        {
            if (lastFallSpeed < -6f) AudioManager.Instance?.PlaySfx(landSfx);
            if (lastFallSpeed < -10f) CameraShake.Shake(0.12f, 0.09f);
            lastFallSpeed = 0f;
        }

        private void Jump()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            animator.SetTrigger("Jump");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            rb.gravityScale = baseGravityScale;
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

        /// <summary>
        /// Reacao ao levar dano: hit stop curto, empurrao para longe da fonte e
        /// piscar enquanto dura a invulnerabilidade - o jogador precisa ver e
        /// sentir que foi atingido, e saber quando volta a ser vulneravel.
        /// </summary>
        private void HandleDamaged()
        {
            animator.SetTrigger("Hurt");
            HitStop.Do(0.07f, 0.02f);

            float dir = -facingDirection;
            Vector2 source;
            if (healthSystem.TryGetLastDamageSource(out source))
            {
                dir = Mathf.Sign(transform.position.x - source.x);
                if (Mathf.Approximately(dir, 0f)) dir = -facingDirection;
            }

            rb.linearVelocity = new Vector2(dir * hurtKnockbackForce, hurtKnockbackForce * 0.55f);
            hurtLockTimer = hurtControlLock;

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(InvulnerabilityFlash(healthSystem.InvulnerabilityDuration));
        }

        private IEnumerator InvulnerabilityFlash(float duration)
        {
            if (spriteRenderer == null) yield break;

            const float blinkInterval = 0.08f;
            float elapsed = 0f;
            bool tinted = false;

            while (elapsed < duration && !healthSystem.IsDead)
            {
                tinted = !tinted;
                spriteRenderer.color = tinted ? hurtFlashColor : baseSpriteColor;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            spriteRenderer.color = baseSpriteColor;
            flashRoutine = null;
        }

        private void HandleDeath()
        {
            animator.SetTrigger("Death");

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = null;
            if (spriteRenderer != null) spriteRenderer.color = baseSpriteColor;
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
            rb.gravityScale = baseGravityScale;
            currentAttack = 0;
            attackTimer = 0f;
            timeSinceAttack = 0f;
            hurtLockTimer = 0f;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            lastFallSpeed = 0f;

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = null;
            if (spriteRenderer != null) spriteRenderer.color = baseSpriteColor;

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
