using UnityEngine;
using EcosDeAldenor.Core;
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
    /// LEITURA DOS ATAQUES (o que mudou e por que)
    ///
    /// Antes, os dois golpes eram praticamente invisiveis. O corpo a corpo nao
    /// tinha aviso nenhum: o chefe entrava em AttackBasic, tremia a camera na
    /// hora errada (no comeco) e so aplicava o dano 0,6 s depois - o jogador
    /// levava o golpe sem ter visto golpe nenhum. E os dois ataques acertavam
    /// por distancia no instante do dano, entao fugir nao adiantava: o dano
    /// perseguia. Sem animacao de ataque (o chefe e um sprite de 5 quadros sem
    /// Animator), nada disso dava para ler.
    ///
    /// Agora todo golpe segue o mesmo ciclo de tres tempos - CARGA, IMPACTO,
    /// RESPIRO:
    ///   1. CARGA: o chefe PARA de andar, o corpo muda de cor (ambar no golpe
    ///      curto, vermelho no especial), pulsa e infla, e um indicador mostra
    ///      onde o golpe vai cair (faixa a frente, ou marca no chao). O
    ///      indicador cresce ate encostar no contorno: quando encosta, bate.
    ///   2. IMPACTO: dano e clarao no mesmo quadro, e a area valida e a area que
    ///      foi mostrada. Sair dela faz o golpe errar - o que torna a esquiva um
    ///      recurso de verdade.
    ///   3. RESPIRO: recuo, tempo para contra-atacar.
    ///
    /// A dificuldade escolhida no menu estica ou encurta a CARGA e o RESPIRO
    /// (ver DifficultySettings): no Facil o aviso e longo e generoso, no Dificil
    /// e curto e o chefe volta a atacar quase em seguida.
    /// </summary>
    public class AVigilia : EnemyBase
    {
        [Header("VFX do Chefe")]
        [Tooltip("Quadros da bola de fogo (pacote GothicVania Church, fx/fireball).")]
        [SerializeField] private Sprite[] fireballFrames;
        [SerializeField] private float fireballFps = 14f;
        [SerializeField] private float fireballEscala = 1.6f;
        [SerializeField] private Sprite[] specialVfxFrames;
        [SerializeField] private Sprite[] deathVfxFrames;
        [Tooltip("Cor do corpo enquanto carrega o ataque ESPECIAL.")]
        [SerializeField] private Color telegraphColor = new Color(1f, 0.3f, 0.25f);
        [Tooltip("Quadros da conjuracao do mago (pacote GothicVania Church, wizard/Fire).")]
        [SerializeField] private Sprite[] framesConjurar;
        [Tooltip("Cor do corpo enquanto carrega o golpe CORPO A CORPO.")]
        [SerializeField] private Color basicTelegraphColor = new Color(1f, 0.72f, 0.3f);

        private SpriteRenderer bodyRenderer;
        private BossTelegraph telegrafo;
        private Color baseColor = Color.white;
        private float baseScaleAbs = 1f;

        private enum BossState
        {
            Idle,
            Chase,
            CargaBasica,   // aviso do golpe corpo a corpo
            AttackBasic,   // impacto do golpe corpo a corpo
            Telegraph,     // aviso do golpe especial
            AttackSpecial, // impacto do golpe especial
            Retreat
        }

        [Header("Configuracao do Chefe")]
        [SerializeField] private float basicAttackRange = 1.2f;
        [SerializeField] private float specialAttackRange = 3f;
        [SerializeField] private int basicAttackDamage = 1;
        [SerializeField] private int specialAttackDamage = 2;
        [Tooltip("Duracao do aviso do golpe corpo a corpo (antes do impacto).")]
        [SerializeField] private float basicWindUpDuration = 0.5f;
        [Tooltip("Duracao do aviso do ataque especial.")]
        [SerializeField] private float telegraphDuration = 0.8f;
        [Tooltip("Raio da marca no chao do ataque especial - so quem estiver dentro leva dano.")]
        [SerializeField] private float specialImpactRadius = 1.7f;
        [SerializeField] private float retreatDuration = 1f;
        [SerializeField] private float stateCooldown = 1.2f;

        [Tooltip("Som do lancamento da bola de fogo.")]
        [SerializeField] private AudioClip castSfx;
        [Tooltip("Toque que marca a virada de fase do chefe (a cada terco de vida perdido).")]
        [SerializeField] private AudioClip phaseChangeSfx;

        private BossState currentState = BossState.Idle;
        private float stateTimer;
        private int currentPhase = 1; // 1, 2 ou 3 - muda conforme a vida diminui

        // Alvo travado no inicio da carga: o golpe cai onde foi anunciado.
        private Vector3 pontoDeImpacto;
        private float direcaoDoGolpe = 1f;
        private float duracaoDaCargaAtual;
        private bool bolaLancada;

        // Valores ja ajustados pela dificuldade (calculados uma vez, no Awake).
        private float cargaBasica;
        private float cargaEspecial;
        private float respiro;
        private float recarga;   // pausa curta do proprio impacto

        protected override void Awake()
        {
            base.Awake();
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) baseColor = bodyRenderer.color;
            baseScaleAbs = Mathf.Abs(transform.localScale.x);

            // O componente de indicadores e criado aqui em vez de ficar no
            // prefab: ele nao tem nada para configurar no Inspector e assim o
            // prefab do chefe continua intacto.
            telegrafo = GetComponent<BossTelegraph>();
            if (telegrafo == null) telegrafo = gameObject.AddComponent<BossTelegraph>();

            // Dificuldade: o Medio devolve exatamente os valores do prefab.
            cargaBasica = DifficultySettings.TelegrafoDoChefe(basicWindUpDuration);
            cargaEspecial = DifficultySettings.TelegrafoDoChefe(telegraphDuration);
            respiro = DifficultySettings.RespiroDoChefe(retreatDuration);
            recarga = DifficultySettings.RespiroDoChefe(stateCooldown);

            basicAttackDamage = DifficultySettings.DanoDeInimigo(basicAttackDamage);
            specialAttackDamage = DifficultySettings.DanoDeInimigo(specialAttackDamage);

            // Nenhum golpe pode matar de vida cheia: mesmo no Dificil o jogador
            // sempre tem direito a um erro antes de morrer.
            specialAttackDamage = Mathf.Clamp(specialAttackDamage, 1,
                Mathf.Max(1, DifficultySettings.VidaDoJogador - 1));

            healthSystem.OnHealthChanged += EvaluatePhase;
        }

        protected override void Update()
        {
            // Nao usa a logica de Update da classe base (patrulha simples).
            // O chefe roda sua propria maquina de estados.
            if (healthSystem.IsDead) return;

            DetectPlayer();
            UpdateStateMachine();
            UpdateCorpo();
        }

        private bool EstaCarregando =>
            currentState == BossState.Telegraph || currentState == BossState.CargaBasica;

        /// <summary>
        /// O corpo do chefe conta o mesmo que os indicadores: enquanto carrega,
        /// ele muda de cor, pulsa cada vez mais rapido e incha um pouco (a
        /// "antecipacao" classica da animacao). Fora da carga, volta ao normal -
        /// e essa diferenca constante que separa chefe parado de chefe atacando.
        /// </summary>
        private void UpdateCorpo()
        {
            if (bodyRenderer == null) return;

            // O clarao de dano (EnemyBase) tem prioridade sobre a cor de carga:
            // saber que o golpe do jogador acertou vale mais, naquele instante,
            // do que saber que o chefe esta carregando.
            if (EmClarao) return;

            if (EstaCarregando)
            {
                bool especial = currentState == BossState.Telegraph;
                Color corAviso = especial ? telegraphColor : basicTelegraphColor;

                float progresso = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, duracaoDaCargaAtual));
                float blink = Mathf.PingPong(Time.time * (6f + progresso * 14f), 1f);
                bodyRenderer.color = Color.Lerp(baseColor, corAviso, blink);

                float inchar = 1f + progresso * (especial ? 0.16f : 0.09f);
                AplicarEscala(inchar);

                telegrafo.Aura(transform, (especial ? 3.4f : 2.2f) * Mathf.Lerp(0.4f, 1f, progresso), corAviso);
            }
            else
            {
                bodyRenderer.color = baseColor;
                AplicarEscala(1f);
                if (currentState != BossState.AttackBasic && currentState != BossState.AttackSpecial)
                    telegrafo.Aura(transform, 0f, baseColor);
            }
        }

        /// <summary>
        /// Multiplica a escala do chefe preservando o SINAL de X, que e o que o
        /// EnemyBase usa para virar o sprite de lado.
        /// </summary>
        private void AplicarEscala(float fator)
        {
            Vector3 escala = transform.localScale;
            float sinal = escala.x >= 0f ? 1f : -1f;
            escala.x = sinal * baseScaleAbs * fator;
            escala.y = baseScaleAbs * fator;
            transform.localScale = escala;
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
                        ChangeState(BossState.CargaBasica);
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

                // Durante a carga o chefe fica parado: e o quadro de anuncio.
                // Parar tambem impede que ele vire de lado no meio do aviso e
                // acabe batendo para um lado diferente do que foi mostrado.
                case BossState.CargaBasica:
                    Congelar();
                    if (stateTimer <= 0f) ChangeState(BossState.AttackBasic);
                    break;

                case BossState.Telegraph:
                    Congelar();
                    // A bola de fogo sai na metade da carga e VOA o resto do
                    // tempo, pousando no mesmo instante em que a marca no chao
                    // se completa. Ela e o relogio do ataque, visivel no ar.
                    if (!bolaLancada && stateTimer <= cargaEspecial * 0.5f)
                    {
                        bolaLancada = true;
                        LancarBolaDeFogo(Mathf.Max(0.05f, stateTimer));
                    }
                    if (stateTimer <= 0f) ChangeState(BossState.AttackSpecial);
                    break;

                // O dano ja saiu na entrada do estado, junto com o clarao. Aqui
                // so se espera o golpe terminar de ser visto.
                case BossState.AttackBasic:
                case BossState.AttackSpecial:
                    Congelar();
                    if (stateTimer <= 0f) ChangeState(BossState.Retreat);
                    break;

                case BossState.Retreat:
                    if (stateTimer <= 0f)
                    {
                        ChangeState(detectedPlayer != null ? BossState.Chase : BossState.Idle);
                    }
                    break;
            }
        }

        private void Congelar()
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator(false);
        }

        private void ChangeState(BossState newState)
        {
            currentState = newState;

            switch (newState)
            {
                case BossState.CargaBasica:
                    duracaoDaCargaAtual = cargaBasica;
                    direcaoDoGolpe = DirecaoAteOJogador();
                    Virar(direcaoDoGolpe);
                    telegrafo.MostrarArco(transform, basicAttackRange * 1.2f, direcaoDoGolpe,
                                          AlturaDosPes(), cargaBasica, basicTelegraphColor);
                    Conjurar(cargaBasica);
                    break;

                case BossState.Telegraph:
                    duracaoDaCargaAtual = cargaEspecial;
                    direcaoDoGolpe = DirecaoAteOJogador();
                    Virar(direcaoDoGolpe);
                    // Trava o ponto agora: o jogador tem a carga inteira para sair dali.
                    pontoDeImpacto = PontoNoChaoDoJogador();
                    telegrafo.MostrarArea(pontoDeImpacto, specialImpactRadius, cargaEspecial, telegraphColor);
                    Conjurar(cargaEspecial);
                    bolaLancada = false;
                    AudioManager.Instance?.PlaySfx(castSfx);
                    break;

                case BossState.AttackBasic:
                    telegrafo.Disparar();
                    AplicarDanoDoArco();
                    CameraShake.Shake(0.18f, 0.14f);
                    if (animator != null) animator.SetTrigger("Attack");
                    break;

                case BossState.AttackSpecial:
                    telegrafo.Disparar();
                    // A explosao sai da propria bola de fogo quando ela pousa;
                    // sem quadros de bola, o chefe ainda explode sozinho.
                    if (fireballFrames == null || fireballFrames.Length == 0)
                        OneShotVFX.Spawn(specialVfxFrames, pontoDeImpacto, 2.2f, 16f);
                    AplicarDanoDaArea();
                    CameraShake.Shake(0.4f, 0.4f);
                    if (animator != null) animator.SetTrigger("Attack");
                    break;
            }

            if (newState == BossState.Chase) UpdateAnimator(true);

            stateTimer = newState switch
            {
                BossState.CargaBasica => cargaBasica,
                BossState.Telegraph => cargaEspecial,
                BossState.AttackBasic => recarga * 0.15f,
                BossState.AttackSpecial => recarga * 0.2f,
                BossState.Retreat => respiro,
                _ => 0f
            };
        }

        /// <summary>
        /// Posicao do jogador rebaixada ate os pes dele. A marca de impacto e uma
        /// mancha no CHAO: desenhada na altura do centro do personagem, ela
        /// flutuava no ar, encostada nas lapides, e deixava de parecer chao.
        /// </summary>
        private Vector3 PontoNoChaoDoJogador()
        {
            if (detectedPlayer == null) return transform.position;

            Vector3 p = detectedPlayer.position;
            var col = detectedPlayer.GetComponent<Collider2D>();
            if (col != null) p.y = col.bounds.min.y + 0.08f;
            return p;
        }

        /// <summary>
        /// Toca a animacao de conjuracao do mago (10 quadros do pacote Church),
        /// esticada ou comprimida para caber EXATAMENTE no tempo de carga.
        ///
        /// O sprite do chefe tinha essa animacao desde sempre e nunca a tocava:
        /// ele passava a luta inteira no clipe parado, e todo o aviso de ataque
        /// vinha de fora dele (cor, escala, marca no chao). Com a conjuracao, o
        /// proprio corpo do chefe passa a dizer o que ele esta fazendo - e o
        /// ultimo quadro do gesto cai no mesmo instante do golpe.
        /// </summary>
        private void Conjurar(float duracao)
        {
            if (spriteAnim == null || framesConjurar == null || framesConjurar.Length == 0) return;
            if (duracao <= 0.01f) return;

            spriteAnim.PlayOnce(framesConjurar, framesConjurar.Length / duracao);
        }

        /// <summary>
        /// Arremessa a bola de fogo das maos do chefe ate a marca no chao.
        /// </summary>
        private void LancarBolaDeFogo(float tempoDeVoo)
        {
            if (fireballFrames == null || fireballFrames.Length == 0) return;

            Vector3 maos = transform.position + new Vector3(direcaoDoGolpe * 0.45f, 0.25f, 0f);
            Projetil.Lancar(fireballFrames, fireballFps, maos, pontoDeImpacto, tempoDeVoo,
                            fireballEscala, 1.1f, specialVfxFrames, 2.2f);
        }

        /// <summary>Altura do chao sob o chefe, para desenhar a faixa do golpe.</summary>
        private float AlturaDosPes()
        {
            var col = GetComponent<Collider2D>();
            return col != null ? col.bounds.min.y + 0.12f : transform.position.y;
        }

        private float DirecaoAteOJogador()
        {
            if (detectedPlayer == null) return transform.localScale.x >= 0f ? -1f : 1f;
            return detectedPlayer.position.x >= transform.position.x ? 1f : -1f;
        }

        /// <summary>
        /// Vira o sprite para o lado do golpe. Segue a convencao do pacote
        /// Bandits usada no EnemyBase: olhar para a direita = escala X negativa.
        /// </summary>
        private void Virar(float direcao)
        {
            Vector3 escala = transform.localScale;
            escala.x = direcao > 0f ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
            transform.localScale = escala;
        }

        /// <summary>
        /// Quanto mais avancada a fase do chefe, maior a chance de usar o
        /// ataque especial em vez de simplesmente perseguir. A dificuldade
        /// desloca essa chance para os dois lados.
        /// </summary>
        private bool ShouldUseSpecialAttack()
        {
            float chance = currentPhase switch
            {
                1 => 0.2f,
                2 => 0.4f,
                _ => 0.6f
            };
            chance = Mathf.Clamp01(chance + DifficultySettings.AjusteDeChanceEspecial);
            return Random.value < chance;
        }

        /// <summary>
        /// Dano do golpe corpo a corpo: acerta quem estiver DENTRO da faixa que
        /// foi desenhada - a frente do chefe, do lado anunciado.
        /// </summary>
        private void AplicarDanoDoArco()
        {
            if (detectedPlayer == null) return;

            Vector2 delta = detectedPlayer.position - transform.position;
            float alcance = basicAttackRange * 1.2f;

            bool noLadoCerto = Mathf.Sign(delta.x) == Mathf.Sign(direcaoDoGolpe) || Mathf.Abs(delta.x) < 0.2f;
            if (!noLadoCerto) return;
            if (Mathf.Abs(delta.x) > alcance || Mathf.Abs(delta.y) > 1.2f) return;

            Ferir(basicAttackDamage);
        }

        /// <summary>
        /// Dano do especial: acerta apenas quem estiver dentro da marca no chao.
        /// Quem saiu a tempo nao leva nada.
        /// </summary>
        private void AplicarDanoDaArea()
        {
            if (detectedPlayer == null) return;

            // A marca fica no chao, o jogador e medido pelo centro: por isso a
            // comparacao horizontal usa o raio desenhado e a vertical e mais
            // folgada - quem esta em pe (ou pulando baixo) sobre a marca leva o
            // golpe; quem saiu de lado, nao.
            Vector2 d = (Vector2)detectedPlayer.position - (Vector2)pontoDeImpacto;
            if (Mathf.Abs(d.x) > specialImpactRadius) return;
            if (d.y < -0.5f || d.y > specialImpactRadius + 1f) return;

            Ferir(specialAttackDamage);
        }

        private void Ferir(int dano)
        {
            HealthSystem playerHealth = detectedPlayer.GetComponent<HealthSystem>();
            if (playerHealth != null) playerHealth.TakeDamage(dano, transform.position);
        }

        protected override void HandleDeath()
        {
            // Explosao grande e tremor forte na morte do chefe.
            Sprite[] deathFrames = (deathVfxFrames != null && deathVfxFrames.Length > 0) ? deathVfxFrames : specialVfxFrames;
            OneShotVFX.Spawn(deathFrames, transform.position, 3.5f, 14f);
            CameraShake.Shake(0.6f, 0.55f);
            if (bodyRenderer != null) bodyRenderer.color = baseColor;
            AplicarEscala(1f);
            if (telegrafo != null) telegrafo.Cancelar();
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
