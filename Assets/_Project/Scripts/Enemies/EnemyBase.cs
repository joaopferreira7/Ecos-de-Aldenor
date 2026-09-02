using UnityEngine;
using EcosDeAldenor.Core;
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

        // ANIMACAO DOS INIMIGOS GOTHIC
        //
        // Fantasma, esqueleto e chefe sao PNGs soltos dos pacotes GothicVania,
        // sem AnimatorController - o Animator do prefab original (Bandits) foi
        // removido junto com a troca de arte. O resultado e que eles passavam o
        // jogo inteiro numa pose so, deslizando pelo chao.
        //
        // Os pacotes trazem os clipes que faltavam, todos no mesmo tamanho de
        // quadro do clipe parado (o esqueleto tem caminhada de 8 quadros e um
        // "levantar do chao" de 6; o fantasma tem a versao com halo; ha uma
        // morte de 5 quadros). Basta ligar cada um ao estado certo.
        [Header("Animacao por sprites (pacotes Gothic)")]
        [Tooltip("Pose de descanso. Vazio = usa o clipe do SimpleSpriteAnimator.")]
        [SerializeField] protected Sprite[] framesParado;
        [SerializeField] protected float fpsParado = 6f;
        [Tooltip("Ciclo de caminhada, tocado enquanto o inimigo se move.")]
        [SerializeField] protected Sprite[] framesAndando;
        [SerializeField] protected float fpsAndando = 10f;
        [Tooltip("Ciclo alternativo tocado enquanto persegue o jogador (ex.: o fantasma com halo).")]
        [SerializeField] protected Sprite[] framesPerseguindo;
        [SerializeField] protected float fpsPerseguindo = 12f;
        [Tooltip("Gesto tocado UMA vez, ao avistar o jogador pela primeira vez.")]
        [SerializeField] protected Sprite[] framesDespertar;
        [SerializeField] protected float fpsDespertar = 10f;
        [SerializeField] protected AudioClip despertarSfx;
        [Tooltip("Gesto de morte, tocado antes de o inimigo sumir.")]
        [SerializeField] protected Sprite[] framesMorte;
        [SerializeField] protected float fpsMorte = 12f;

        [Header("Feedback de dano")]
        [Tooltip("Cor do clarao ao levar um golpe - o inimigo precisa REAGIR ao ser acertado.")]
        [SerializeField] protected Color corDoClarao = Color.white;
        [SerializeField] protected float duracaoDoClarao = 0.12f;

        protected Rigidbody2D rb;
        protected HealthSystem healthSystem;
        protected Animator animator;
        protected Vector3 pointAPos;
        protected Vector3 pointBPos;
        protected Vector3 currentTargetPos;
        protected Transform detectedPlayer;
        protected bool movingToB = true;
        protected SimpleSpriteAnimator spriteAnim;
        protected SpriteRenderer corpo;
        private Color corBase = Color.white;
        private bool jaDespertou;
        private float despertarTimer;
        private float clarao;

        /// <summary>Esta piscando por ter levado dano agora? Quem pinta o corpo
        /// por conta propria (o chefe) precisa saber para nao apagar o clarao.</summary>
        protected bool EmClarao => clarao > 0f;

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
            spriteAnim = GetComponent<SimpleSpriteAnimator>();
            corpo = GetComponent<SpriteRenderer>();
            if (corpo != null) corBase = corpo.color;

            // pointA/pointB sao filhos do inimigo no prefab: capturamos a posicao
            // mundial deles uma unica vez aqui, pois senao eles se moveriam junto
            // com o inimigo (que e o proprio pai) e a patrulha nunca chegaria ao alvo.
            // Dificuldade escolhida no menu. Fica aqui, na base de todos os
            // inimigos, para valer igualmente para espectros, esqueletos e chefe
            // sem duplicar prefabs - no Medio os multiplicadores sao 1 e os
            // valores do prefab passam intactos.
            healthSystem.SetMaxHealth(DifficultySettings.VidaDeInimigo(healthSystem.MaxHealth));
            contactDamage = DifficultySettings.DanoDeInimigo(contactDamage);
            patrolSpeed = DifficultySettings.VelocidadeDeInimigo(patrolSpeed);
            contactDamageCooldown = DifficultySettings.CooldownDeContato(contactDamageCooldown);

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
            AtualizarClarao();

            if (healthSystem.IsDead) return;

            // Enquanto o inimigo se levanta, ele nao anda nem machuca: o gesto
            // de despertar e um aviso ao jogador, e um aviso que ja empurra o
            // inimigo para cima dele nao e aviso nenhum.
            if (despertarTimer > 0f)
            {
                despertarTimer -= Time.deltaTime;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                DetectPlayer();
                return;
            }

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

            if (detectedPlayer != null && !jaDespertou) Despertar();
        }

        /// <summary>
        /// Primeira vez que o inimigo ve o jogador. O esqueleto tem uma animacao
        /// de se levantar do chao (6 quadros) que nunca era tocada; e ela que
        /// transforma "um esqueleto que ja estava andando" em "um esqueleto que
        /// acordou por sua causa".
        /// </summary>
        protected virtual void Despertar()
        {
            jaDespertou = true;
            if (framesDespertar == null || framesDespertar.Length == 0 || spriteAnim == null) return;

            despertarTimer = SimpleSpriteAnimator.Duracao(framesDespertar, fpsDespertar);
            spriteAnim.PlayOnce(framesDespertar, fpsDespertar);
            AudioManager.Instance?.PlaySfx(despertarSfx);
        }

        /// <summary>
        /// Clarao branco ao levar dano. Sem isto, acertar um inimigo Gothic nao
        /// produzia nenhuma reacao NELE - a faisca aparecia no ar e o bicho
        /// seguia igual, o que faz o golpe parecer que nao contou.
        /// </summary>
        private void AtualizarClarao()
        {
            if (corpo == null || clarao <= 0f) return;

            clarao -= Time.deltaTime / Mathf.Max(0.01f, duracaoDoClarao);
            corpo.color = clarao > 0f ? Color.Lerp(corBase, corDoClarao, Mathf.Clamp01(clarao)) : corBase;
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
            //
            // Sem pontos de patrulha nao ha territorio a respeitar, e o limite
            // seria a origem do mundo: pointAPos/pointBPos ficariam em zero e o
            // inimigo perseguiria x = 0 em vez do jogador. Nesse caso persegue
            // livremente, como antes.
            if (pointA == null || pointB == null)
            {
                MoveTowards(detectedPlayer.position, patrolSpeed * 1.5f);
                return;
            }

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
            AtualizarClipe(isMoving);

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
        /// Escolhe entre parado e andando. Um gesto em andamento (despertar,
        /// morte) tem prioridade: nada de trocar de clipe no meio dele.
        /// </summary>
        protected void AtualizarClipe(bool isMoving)
        {
            if (spriteAnim == null || spriteAnim.EmGesto) return;

            // Perseguindo tem clipe proprio quando existe: e o "estado de alerta"
            // do inimigo, e o que diz ao jogador que ele foi notado.
            if (detectedPlayer != null && framesPerseguindo != null && framesPerseguindo.Length > 0)
                spriteAnim.PlayLoop(framesPerseguindo, fpsPerseguindo);
            else if (isMoving && framesAndando != null && framesAndando.Length > 0)
                spriteAnim.PlayLoop(framesAndando, fpsAndando);
            else if (framesParado != null && framesParado.Length > 0)
                spriteAnim.PlayLoop(framesParado, fpsParado);
            else if (!isMoving)
                spriteAnim.VoltarAoPadrao();
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
            clarao = 1f;
            stunTimer = hitStunDuration;
        }

        protected virtual void HandleDeath()
        {
            if (animator != null) animator.SetTrigger("Death");

            // Para de empurrar, de machucar e de colidir: um cadaver em queda
            // que ainda causa dano de contato e o tipo de coisa que faz a morte
            // do inimigo parecer um bug.
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
            if (corpo != null) corpo.color = corBase;

            float espera = 0.6f;
            if (spriteAnim != null && framesMorte != null && framesMorte.Length > 0)
            {
                spriteAnim.PlayOnce(framesMorte, fpsMorte);
                espera = SimpleSpriteAnimator.Duracao(framesMorte, fpsMorte) + 0.15f;
            }

            // "Poof" de morte: pequena baforada de particulas/explosao no lugar do
            // inimigo, tingida com a cor do inimigo, dando feedback claro do abate.
            if (deathPoofFrames != null && deathPoofFrames.Length > 0)
            {
                OneShotVFX.Spawn(deathPoofFrames, transform.position, deathPoofScale, 16f, 20, deathPoofTint);
            }

            Destroy(gameObject, espera); // tempo para a animacao de morte tocar antes de remover
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
