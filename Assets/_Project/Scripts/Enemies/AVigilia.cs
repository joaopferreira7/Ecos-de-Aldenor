using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EcosDeAldenor.Core;
using EcosDeAldenor.Player;
using EcosDeAldenor.Systems;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Chefe final do jogo. Estende EnemyBase mas sobrescreve o comportamento
    /// de Update com uma maquina de estados propria (padrao State Machine),
    /// pois um chefe precisa de logica de combate mais rica do que a patrulha
    /// simples dos inimigos comuns.
    ///
    /// DUAS FORMAS, QUATRO ATOS
    ///
    /// A luta era um bloco so: o mesmo mago, os mesmos dois golpes, do primeiro
    /// ao ultimo ponto de vida. Agora ela tem atos, cada um marcado na barra:
    ///
    ///   100% - 75%  MAGO        golpe curto e bola de fogo;
    ///    75% - 50%  MAGO        + SALVA: tres bolas de fogo que caem em fila,
    ///                           uma depois da outra;
    ///         50%   TRANSFORMACAO - o capuz se desfaz e revela o anjo que a
    ///                           Vigilia era antes da nevoa (o prologo conta);
    ///    50% - 25%  ANJO        golpe de asa, INVESTIDA (atravessa a arena numa
    ///                           faixa anunciada) e CHUVA DE FOGO (varias marcas,
    ///                           cada uma com o seu relogio);
    ///    25% -  0%  ANJO EM FURIA: avisos mais curtos, mais marcas, menos
    ///                           respiro, e o corpo fica avermelhado.
    ///
    /// O anjo e o mago sao do mesmo pacote (GothicVania Church), ambos de manto
    /// vermelho - a transformacao le como "a mesma criatura, sem disfarce", e
    /// nao como um segundo chefe que entrou pela porta.
    ///
    /// LEITURA DOS ATAQUES
    ///
    /// Todo golpe segue o mesmo ciclo de tres tempos - CARGA, IMPACTO, RESPIRO:
    ///   1. CARGA: o chefe PARA, toca a animacao de ataque esticada para caber
    ///      no tempo do aviso, muda de cor e infla, e um indicador mostra onde o
    ///      golpe vai cair. As cores sao um codigo fixo: AMBAR e o golpe curto,
    ///      VERMELHO e fogo caindo numa marca, LILAS e o caminho da investida.
    ///   2. IMPACTO: o dano so vale dentro da area que foi mostrada. Sair dela -
    ///      ou rolar por cima no tempo certo - faz o golpe errar.
    ///   3. RESPIRO: o chefe fica parado; e a janela de contra-ataque. Depois da
    ///      investida o respiro e maior, porque e o golpe mais arriscado de fugir.
    ///
    /// A dificuldade escolhida no menu estica ou encurta a CARGA e o RESPIRO
    /// (ver DifficultySettings).
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
        [Tooltip("Cor do caminho da investida do anjo.")]
        [SerializeField] private Color corInvestida = new Color(0.82f, 0.72f, 1f);

        private SpriteRenderer bodyRenderer;
        private BossTelegraph telegrafo;
        private Color baseColor = Color.white;
        private float baseScaleAbs = 1f;

        private enum BossState
        {
            Idle,
            Chase,
            CargaBasica,     // aviso do golpe corpo a corpo
            AttackBasic,     // impacto do golpe corpo a corpo
            Telegraph,       // aviso da bola de fogo
            AttackSpecial,   // impacto da bola de fogo
            CargaSalva,      // mago: tres bolas de fogo em fila
            CargaInvestida,  // anjo: aviso da investida
            Investida,       // anjo: atravessando a faixa
            CargaChuva,      // anjo: fogo caindo do alto em varias marcas
            Transformando,
            Retreat
        }

        private enum Forma { Mago, Anjo }

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

        [Header("Atos da luta (fracao da vida)")]
        [Tooltip("Abaixo disto o mago passa a usar a SALVA de tres bolas de fogo.")]
        [Range(0f, 1f)] [SerializeField] private float limiarAgressivo = 0.75f;
        [Tooltip("Abaixo disto a Vigilia se transforma no anjo.")]
        [Range(0f, 1f)] [SerializeField] private float limiarTransformacao = 0.5f;
        [Tooltip("Abaixo disto o anjo entra em furia.")]
        [Range(0f, 1f)] [SerializeField] private float limiarFuria = 0.25f;

        [Header("Salva (mago)")]
        [SerializeField] private int salvaQuantidade = 3;
        [Tooltip("Distancia entre as marcas da salva.")]
        [SerializeField] private float salvaEspaco = 2.6f;
        [Tooltip("Tempo entre uma bola e a seguinte.")]
        [SerializeField] private float salvaIntervalo = 0.4f;
        [SerializeField] private float salvaRaio = 1.3f;
        [SerializeField] private float salvaCarga = 0.9f;

        [Header("Forma de anjo")]
        [SerializeField] private string nomeDoAnjo = "A Vigília Desvelada";
        [SerializeField] private string epitetoDoAnjo = "O anjo que velava Aldenor";
        [Tooltip("Batida de asas (pacote GothicVania Church, angel/idle).")]
        [SerializeField] private Sprite[] framesAnjoParado;
        [SerializeField] private float fpsAnjoParado = 10f;
        [Tooltip("Pose de ataque do anjo (angel/attack).")]
        [SerializeField] private Sprite[] framesAnjoAtaque;
        [Tooltip("Escala do anjo. O quadro dele (122x117) e maior que o do mago (81x66).")]
        [SerializeField] private float escalaDoAnjo = 4.4f;
        [Tooltip("Colisor do anjo, em unidades locais (divididas pela escala).")]
        [SerializeField] private Vector2 colisorDoAnjo = new Vector2(0.3f, 0.66f);
        [SerializeField] private Vector2 deslocamentoDoColisorDoAnjo = new Vector2(0f, -0.3f);
        [SerializeField] private Color corDaBarraDoAnjo = new Color(0.92f, 0.45f, 0.1f);
        [SerializeField] private float alcanceDoGolpeDeAsa = 1.7f;

        [Header("Investida (anjo)")]
        [SerializeField] private float investidaCarga = 0.9f;
        [SerializeField] private float investidaVelocidade = 15f;
        [Tooltip("Quanto a investida passa do ponto onde o jogador estava.")]
        [SerializeField] private float investidaAlem = 2.5f;
        [SerializeField] private float investidaMaxima = 10f;

        [Header("Chuva de fogo (anjo)")]
        [SerializeField] private int chuvaQuantidade = 3;
        [SerializeField] private float chuvaCarga = 1f;
        [SerializeField] private float chuvaIntervalo = 0.3f;
        [SerializeField] private float chuvaRaio = 1.3f;
        [Tooltip("Quanto as marcas se espalham em volta do jogador.")]
        [SerializeField] private float chuvaEspalhamento = 4.5f;

        [Header("Sons")]
        [Tooltip("Som do lancamento da bola de fogo.")]
        [SerializeField] private AudioClip castSfx;
        [Tooltip("Toque que marca a virada de ato.")]
        [SerializeField] private AudioClip phaseChangeSfx;
        [SerializeField] private AudioClip impactoSfx;
        [SerializeField] private AudioClip transformacaoInicioSfx;
        [SerializeField] private AudioClip transformacaoEstouroSfx;
        [SerializeField] private AudioClip investidaSfx;
        [SerializeField] private AudioClip passoSfx;
        [SerializeField] private AudioClip invocarSfx;

        [Header("Efeitos de golpe (pacote Hitspark)")]
        [Tooltip("Corte - golpe curto do mago e golpe de asa do anjo.")]
        [SerializeField] private Sprite[] faiscaDeCorte;
        [Tooltip("Estrela de impacto - quando um golpe acerta o Ren.")]
        [SerializeField] private Sprite[] faiscaDeImpacto;

        [Header("Passo sombrio (mago)")]
        [SerializeField] private float passoSumir = 0.35f;
        [Tooltip("Tempo entre sumir e reaparecer - a marca do destino fica visivel durante ele.")]
        [SerializeField] private float passoEspera = 0.5f;
        [SerializeField] private float passoSurgir = 0.3f;
        [Tooltip("A que distancia do Ren o mago reaparece.")]
        [SerializeField] private float passoDistancia = 3.2f;
        [Tooltip("Limites da arena: o mago nunca reaparece dentro de uma parede.")]
        [SerializeField] private Vector2 limitesDaArena = new Vector2(-12.5f, 12.5f);

        [Header("Invocacao dos mortos (mago, aos 75%)")]
        [Tooltip("Prefab erguido do chao (o Guardiao de Pedra).")]
        [SerializeField] private GameObject prefabDoMorto;
        [SerializeField] private int mortosInvocados = 2;

        [Header("Direcao")]
        [Tooltip("O desenho do anjo e quase frontal, com o corpo virado para a direita.")]
        [SerializeField] private bool anjoOlhaParaDireita = true;

        private BossState currentState = BossState.Idle;
        private float stateTimer;
        private Forma forma = Forma.Mago;
        private bool agressivo;
        private bool furia;
        private bool transformando;
        private float decisaoTimer;

        // Alvo travado no inicio da carga: o golpe cai onde foi anunciado.
        private Vector3 pontoDeImpacto;
        private float direcaoDoGolpe = 1f;
        private float duracaoDaCargaAtual;
        private bool bolaLancada;

        // Investida em andamento.
        private float investidaFimX;
        private float investidaLimite;
        private float investidaUltimoX;
        private int investidaParado;
        private bool investidaAcertou;

        // Valores ja ajustados pela dificuldade (calculados uma vez, no Awake).
        private float cargaBasica;
        private float cargaEspecial;
        private float cargaSalva;
        private float cargaInvestida;
        private float cargaChuva;
        private float respiro;
        private float recarga;   // pausa curta do proprio impacto

        /// <summary>Um golpe agendado: a marca ja esta no chao, a bola ainda vai cair.</summary>
        private class Impacto
        {
            public Vector3 ponto;
            public float raio;
            public float tempo;      // ate o impacto
            public float voo;        // quanto antes do impacto a bola sai
            public bool doCeu;       // cai de cima (anjo) ou e arremessada (mago)
            public bool comCorte;    // efeito visual de impacto proprio
            public bool lancado;
            public int dano;
        }

        // Cenas curtas que tomam o controle do chefe (passo sombrio, invocacao).
        private bool emCena;
        private bool invocacaoPendente;
        private readonly List<EnemyBase> mortos = new List<EnemyBase>();

        // Relogios dos efeitos continuos.
        private float relogioBrasas;
        private float relogioPenas;
        private float relogioResiduo;
        private float relogioPoeira;
        private SpriteRenderer halo;   // luz atras do anjo

        private readonly List<Impacto> impactos = new List<Impacto>();
        private readonly List<Projetil> projeteis = new List<Projetil>();
        private Transform jogador;
        private int mascaraDoChao;

        public float LimiarAgressivo => limiarAgressivo;
        public float LimiarTransformacao => limiarTransformacao;
        public float LimiarFuria => limiarFuria;

        protected override void Awake()
        {
            base.Awake();
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) baseColor = bodyRenderer.color;
            baseScaleAbs = Mathf.Abs(transform.localScale.x);
            mascaraDoChao = LayerMask.GetMask("Ground");

            // O componente de indicadores e criado aqui em vez de ficar no
            // prefab: ele nao tem nada para configurar no Inspector e assim o
            // prefab do chefe continua intacto.
            telegrafo = GetComponent<BossTelegraph>();
            if (telegrafo == null) telegrafo = gameObject.AddComponent<BossTelegraph>();

            // Dificuldade: o Medio devolve exatamente os valores do prefab.
            cargaBasica = DifficultySettings.TelegrafoDoChefe(basicWindUpDuration);
            cargaEspecial = DifficultySettings.TelegrafoDoChefe(telegraphDuration);
            cargaSalva = DifficultySettings.TelegrafoDoChefe(salvaCarga);
            cargaInvestida = DifficultySettings.TelegrafoDoChefe(investidaCarga);
            cargaChuva = DifficultySettings.TelegrafoDoChefe(chuvaCarga);
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

        protected override void OnDestroy()
        {
            healthSystem.OnHealthChanged -= EvaluatePhase;
            base.OnDestroy();
        }

        protected override void Update()
        {
            // Nao usa a logica de Update da classe base (patrulha simples).
            // O chefe roda sua propria maquina de estados.
            if (healthSystem.IsDead || transformando) return;

            DetectPlayer();
            AtualizarImpactos(Time.deltaTime);
            AtualizarAmbiente();
            if (emCena) return;

            // A invocacao espera o fim do golpe em andamento: interromper uma
            // carga deixaria uma marca no chao prometendo um golpe que nao vem.
            if (invocacaoPendente && (currentState == BossState.Chase || currentState == BossState.Retreat ||
                                      currentState == BossState.Idle))
            {
                invocacaoPendente = false;
                StartCoroutine(Invocar());
                return;
            }

            UpdateStateMachine();
            UpdateCorpo();
        }

        private bool EstaCarregando =>
            currentState == BossState.Telegraph || currentState == BossState.CargaBasica ||
            currentState == BossState.CargaSalva || currentState == BossState.CargaInvestida ||
            currentState == BossState.CargaChuva;

        private Color CorDaCarga => currentState switch
        {
            BossState.CargaBasica => basicTelegraphColor,
            BossState.CargaInvestida => corInvestida,
            _ => telegraphColor
        };

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
                bool forte = currentState != BossState.CargaBasica;
                Color corAviso = CorDaCarga;

                float progresso = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, duracaoDaCargaAtual));
                float blink = Mathf.PingPong(Time.time * (6f + progresso * 14f), 1f);
                // Pisca so ate a metade do caminho: tingido de vermelho por inteiro,
                // o manto roxo do mago ficava da mesma cor da aura atras dele e o
                // corpo sumia justamente quando mais precisa ser lido.
                bodyRenderer.color = Color.Lerp(baseColor, corAviso, blink * 0.55f);

                float inchar = 1f + progresso * (forte ? 0.16f : 0.09f);
                AplicarEscala(inchar);

                telegrafo.Aura(transform, (forte ? 3.4f : 2.2f) * Mathf.Lerp(0.4f, 1f, progresso), corAviso,
                               forma == Forma.Mago ? 0.2f : 0.34f);
                EfeitosDaCarga(progresso, corAviso);
            }
            else
            {
                bodyRenderer.color = baseColor;
                AplicarEscala(1f);
                if (currentState != BossState.AttackBasic && currentState != BossState.AttackSpecial &&
                    currentState != BossState.Investida)
                    telegrafo.Aura(transform, 0f, baseColor);
            }
        }

        /// <summary>
        /// O que acompanha cada carga, alem da cor e do inchaco:
        ///   - o mago conjura sobre um CIRCULO de runas no chao e solta brasas
        ///     pelas maos - o fogo se formando antes de ser arremessado;
        ///   - o anjo, ao chamar a chuva, se acende em dourado; ao preparar a
        ///     investida, levanta poeira e perde penas, como quem toma impulso.
        /// </summary>
        private void EfeitosDaCarga(float progresso, Color cor)
        {
            float dt = Time.deltaTime;
            Vector3 pes = new Vector3(transform.position.x, AlturaDosPes(), 0f);

            if (forma == Forma.Mago && currentState != BossState.CargaBasica)
            {
                telegrafo.Circulo(pes, 1.8f, cor);
                relogioBrasas -= dt;
                if (relogioBrasas <= 0f)
                {
                    relogioBrasas = Mathf.Lerp(0.09f, 0.03f, progresso);
                    var maos = transform.position + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.2f, 0.9f), 0f);
                    Efeitos.Brasas(maos, 1, new Color(1f, 0.6f, 0.15f, 1f), forca: 0.8f, subida: 1.6f, tamanho: 0.11f);
                }
            }
            else if (forma == Forma.Anjo && currentState == BossState.CargaChuva)
            {
                relogioBrasas -= dt;
                if (relogioBrasas <= 0f)
                {
                    relogioBrasas = 0.05f;
                    var maos = transform.position + new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(0.8f, 2f), 0f);
                    Efeitos.Brasas(maos, 1, new Color(1f, 0.9f, 0.55f, 1f), forca: 0.5f, subida: 2.4f, tamanho: 0.12f);
                }
            }
            else if (currentState == BossState.CargaInvestida)
            {
                relogioPoeira -= dt;
                if (relogioPoeira <= 0f)
                {
                    relogioPoeira = 0.12f;
                    Efeitos.Poeira(pes, 1, 0.9f);
                    Efeitos.Penas(transform.position + new Vector3(0f, 1f, 0f), 1, 1f, 0.6f);
                }
            }
        }

        /// <summary>
        /// O que o anjo faz o tempo todo, atacando ou nao: uma luz atras dele,
        /// que respira, e penas se soltando das asas. Em furia a luz fica
        /// vermelha e brasas sobem do corpo. E o que separa a segunda forma de
        /// "o mesmo chefe com outro desenho".
        /// </summary>
        private void AtualizarAmbiente()
        {
            if (forma != Forma.Anjo || halo == null) return;
            float dt = Time.deltaTime;

            Color luz = furia ? new Color(1f, 0.25f, 0.2f, 1f) : new Color(1f, 0.85f, 0.5f, 1f);
            float respira = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f);
            halo.transform.position = transform.position + new Vector3(0f, 0.6f, 0f);
            float t = Mathf.Lerp(5.5f, 6.3f, respira);
            halo.transform.localScale = new Vector3(t, t, 1f);
            halo.color = new Color(luz.r, luz.g, luz.b, Mathf.Lerp(0.18f, 0.3f, respira));

            relogioPenas -= dt;
            if (relogioPenas <= 0f)
            {
                relogioPenas = Random.Range(0.25f, 0.5f);
                var asa = transform.position + new Vector3(Random.Range(-1.8f, 1.8f), Random.Range(0.5f, 2.2f), 0f);
                Efeitos.Penas(asa, 1, 0.6f, 0.4f);
            }

            if (furia)
            {
                relogioBrasas -= dt;
                if (relogioBrasas <= 0f && !EstaCarregando)
                {
                    relogioBrasas = 0.1f;
                    var corpo = transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.5f, 1.5f), 0f);
                    Efeitos.Brasas(corpo, 1, new Color(1f, 0.3f, 0.15f, 1f), forca: 0.4f, subida: 1.8f, tamanho: 0.1f);
                }
            }
        }

        /// <summary>
        /// Vira o corpo para o Ren. Usado nas cargas de golpes A DISTANCIA (o
        /// ponto ja esta travado no chao; o corpo pode acompanhar o alvo) e no
        /// respiro. Nos golpes de perto e na investida a direcao NAO acompanha:
        /// o golpe vai para onde foi anunciado, e o corpo tem de concordar com
        /// a faixa desenhada.
        /// </summary>
        private void AcompanharJogador()
        {
            if (jogador == null) return;
            float dx = jogador.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f) OlharPara(dx);
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
                    AcompanharJogador();
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
                    if (forma == Forma.Mago) DecidirComoMago(distance);
                    else DecidirComoAnjo(distance);
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
                    AcompanharJogador();
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

                // A salva e a chuva ja deixaram os golpes agendados (ver
                // AtualizarImpactos). O chefe so espera o ultimo cair.
                case BossState.CargaSalva:
                case BossState.CargaChuva:
                    Congelar();
                    AcompanharJogador();
                    if (stateTimer <= 0f) ChangeState(BossState.Retreat);
                    break;

                case BossState.CargaInvestida:
                    Congelar();
                    if (stateTimer <= 0f) ChangeState(BossState.Investida);
                    break;

                case BossState.Investida:
                    AtualizarInvestida();
                    break;

                // O dano ja saiu na entrada do estado, junto com o clarao. Aqui
                // so se espera o golpe terminar de ser visto.
                case BossState.AttackBasic:
                case BossState.AttackSpecial:
                    Congelar();
                    if (stateTimer <= 0f) ChangeState(BossState.Retreat);
                    break;

                case BossState.Retreat:
                    Congelar();
                    AcompanharJogador();
                    if (stateTimer <= 0f)
                    {
                        ChangeState(detectedPlayer != null ? BossState.Chase : BossState.Idle);
                    }
                    break;
            }
        }

        /// <summary>
        /// Escolha de golpe do mago. O golpe curto e imediato quando o jogador
        /// encosta; os golpes a distancia sao sorteados a cada 0,3 s, nao a cada
        /// quadro - sortear 60 vezes por segundo fazia a bola de fogo sair quase
        /// sempre no primeiro instante em que o jogador entrava no alcance, e o
        /// chefe nunca chegava a perseguir.
        /// </summary>
        private void DecidirComoMago(float distancia)
        {
            decisaoTimer -= Time.deltaTime;
            bool sorteio = decisaoTimer <= 0f;
            if (sorteio) decisaoTimer = 0.3f;

            if (distancia <= basicAttackRange)
            {
                // Encurralado, o mago agressivo as vezes some em vez de bater.
                if (sorteio && agressivo && Random.value < 0.25f) { StartCoroutine(PassoSombrio()); return; }
                ChangeState(BossState.CargaBasica);
                return;
            }

            if (sorteio)
            {
                // Longe demais para andar ate o Ren: atravessa a sombra.
                if (distancia > 5f && Random.value < 0.4f) { StartCoroutine(PassoSombrio()); return; }
                if (agressivo && distancia <= 7f && Random.value < 0.3f) { ChangeState(BossState.CargaSalva); return; }
                if (distancia <= specialAttackRange && ShouldUseSpecialAttack()) { ChangeState(BossState.Telegraph); return; }
            }

            MoveTowards(detectedPlayer.position, patrolSpeed * 1.2f);
        }

        /// <summary>
        /// Escolha de golpe do anjo. Ele luta a MEIA distancia: encosta so ate
        /// uns tres passos do jogador e dali escolhe entre investir e chover
        /// fogo. Quem gruda nele leva o golpe de asa.
        /// </summary>
        private void DecidirComoAnjo(float distancia)
        {
            if (distancia <= alcanceDoGolpeDeAsa) { ChangeState(BossState.CargaBasica); return; }

            decisaoTimer -= Time.deltaTime;
            if (decisaoTimer <= 0f)
            {
                decisaoTimer = furia ? 0.25f : 0.35f;
                float sorte = Random.value;
                float chanceInvestida = furia ? 0.4f : 0.3f;
                float chanceChuva = furia ? 0.35f : 0.25f;

                if (distancia >= 2.5f && sorte < chanceInvestida) { ChangeState(BossState.CargaInvestida); return; }
                if (sorte < chanceInvestida + chanceChuva) { ChangeState(BossState.CargaChuva); return; }
            }

            if (distancia > 3f) MoveTowards(detectedPlayer.position, patrolSpeed * 1.4f);
            else
            {
                Congelar();
                OlharPara(detectedPlayer.position.x - transform.position.x);
            }
        }

        private void Congelar()
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator(false);
        }

        private float Ritmo => furia ? 0.85f : 1f;   // a furia encurta avisos e respiros

        private void ChangeState(BossState newState)
        {
            currentState = newState;
            float duracao = 0f;

            switch (newState)
            {
                case BossState.CargaBasica:
                    duracao = cargaBasica * Ritmo;
                    direcaoDoGolpe = DirecaoAteOJogador();
                    OlharPara(direcaoDoGolpe);
                    float alcance = (forma == Forma.Anjo ? alcanceDoGolpeDeAsa : basicAttackRange) * 1.2f;
                    telegrafo.MostrarArco(transform, alcance, direcaoDoGolpe, AlturaDosPes(), duracao, basicTelegraphColor);
                    Conjurar(duracao);
                    break;

                case BossState.Telegraph:
                    duracao = cargaEspecial * Ritmo;
                    direcaoDoGolpe = DirecaoAteOJogador();
                    OlharPara(direcaoDoGolpe);
                    // Trava o ponto agora: o jogador tem a carga inteira para sair dali.
                    pontoDeImpacto = PontoNoChaoDoJogador();
                    telegrafo.MostrarArea(pontoDeImpacto, specialImpactRadius, duracao, telegraphColor);
                    Conjurar(duracao);
                    bolaLancada = false;
                    AudioManager.Instance?.PlaySfx(castSfx);
                    break;

                case BossState.CargaSalva:
                    duracao = IniciarSalva();
                    break;

                case BossState.CargaInvestida:
                    duracao = IniciarInvestida();
                    break;

                case BossState.Investida:
                    telegrafo.Disparar();
                    AudioManager.Instance?.PlaySfx(investidaSfx);
                    CameraShake.Shake(0.15f, 0.12f);
                    // Arrancada: penas para tras, lampejo lilas e onda no chao.
                    Efeitos.Penas(transform.position + new Vector3(0f, 1f, 0f), 14, 4f, 0.5f, new Vector2(-direcaoDoGolpe, 0.3f));
                    Efeitos.Lampejo(transform.position, 4f, new Color(0.85f, 0.75f, 1f, 0.7f));
                    Efeitos.Onda(new Vector3(transform.position.x, AlturaDosPes(), 0f), 2.2f, corInvestida);
                    relogioResiduo = 0f;
                    investidaUltimoX = transform.position.x;
                    investidaParado = 0;
                    investidaAcertou = false;
                    duracao = investidaLimite;
                    break;

                case BossState.CargaChuva:
                    duracao = IniciarChuva();
                    break;

                case BossState.AttackBasic:
                    telegrafo.Disparar();
                    EfeitoDeCorte();
                    AplicarDanoDoArco();
                    CameraShake.Shake(0.18f, 0.14f);
                    if (animator != null) animator.SetTrigger("Attack");
                    duracao = recarga * 0.15f;
                    break;

                case BossState.AttackSpecial:
                    telegrafo.Disparar();
                    // A explosao sai da propria bola de fogo quando ela pousa;
                    // sem quadros de bola, o chefe ainda explode sozinho.
                    if (fireballFrames == null || fireballFrames.Length == 0)
                        OneShotVFX.Spawn(specialVfxFrames, pontoDeImpacto, 2.2f, 16f);
                    EfeitoDeImpacto(pontoDeImpacto, specialImpactRadius);
                    AplicarDanoEm(pontoDeImpacto, specialImpactRadius, specialAttackDamage);
                    AudioManager.Instance?.PlaySfx(impactoSfx);
                    CameraShake.Shake(0.4f, 0.4f);
                    if (animator != null) animator.SetTrigger("Attack");
                    duracao = recarga * 0.2f;
                    break;

                case BossState.Retreat:
                    // Fim do gesto: volta a pose de descanso. Sem isto o chefe
                    // seguia perseguindo congelado no ultimo quadro do ataque.
                    if (spriteAnim != null) spriteAnim.VoltarAoPadrao();
                    duracao = respiro * Ritmo;
                    break;

                case BossState.Chase:
                    UpdateAnimator(true);
                    break;
            }

            if (EstaCarregando) duracaoDaCargaAtual = duracao;
            stateTimer = duracao;
        }

        // ------------------------------------------------------------------
        // Golpes novos
        // ------------------------------------------------------------------

        /// <summary>
        /// SALVA (mago, a partir de 75%): tres marcas em fila na direcao do
        /// jogador - uma antes dele, uma nele, uma depois - e as bolas caem
        /// nessa ordem. A fuga e ler a ordem: recuar para a marca que ja
        /// explodiu, ou rolar por baixo da que ainda esta no ar.
        /// </summary>
        private float IniciarSalva()
        {
            direcaoDoGolpe = DirecaoAteOJogador();
            OlharPara(direcaoDoGolpe);
            Vector3 centro = PontoNoChaoDoJogador();
            float carga = cargaSalva * Ritmo;

            for (int i = 0; i < salvaQuantidade; i++)
            {
                float desvio = (i - (salvaQuantidade - 1) * 0.5f) * salvaEspaco;
                Vector3 p = NoChao(new Vector3(centro.x + direcaoDoGolpe * desvio, centro.y, centro.z));
                float tempo = carga + i * salvaIntervalo;
                Agendar(p, salvaRaio, tempo, 0.55f, false, basicAttackDamage, telegraphColor);
            }

            Conjurar(carga);
            AudioManager.Instance?.PlaySfx(castSfx);
            return carga + (salvaQuantidade - 1) * salvaIntervalo + 0.1f;
        }

        /// <summary>
        /// INVESTIDA (anjo): o caminho inteiro aparece no chao - do anjo ate
        /// alem do jogador - e depois ele o atravessa em linha reta. Rolar para
        /// fora da faixa, ou por cima dela no instante certo, e a resposta.
        /// </summary>
        private float IniciarInvestida()
        {
            direcaoDoGolpe = DirecaoAteOJogador();
            OlharPara(direcaoDoGolpe);

            float distancia = detectedPlayer != null ? Mathf.Abs(detectedPlayer.position.x - transform.position.x) : 4f;
            float comprimento = Mathf.Clamp(distancia + investidaAlem, 3f, investidaMaxima);
            investidaFimX = transform.position.x + direcaoDoGolpe * comprimento;
            investidaLimite = comprimento / Mathf.Max(1f, investidaVelocidade) + 0.4f;

            float carga = cargaInvestida * Ritmo;
            // Faixa baixa e translucida: ela marca o CHAO do caminho. Na primeira
            // versao (alta e quase opaca) ela cobria o Ren inteiro - e um aviso
            // que esconde o proprio jogador atrapalha mais do que avisa.
            telegrafo.MostrarFaixa(transform.position, comprimento, direcaoDoGolpe, AlturaDosPes() + 0.28f,
                                   0.7f, carga, corInvestida);
            Conjurar(carga);
            AudioManager.Instance?.PlaySfx(castSfx);
            return carga;
        }

        private void AtualizarInvestida()
        {
            float x = transform.position.x;
            rb.linearVelocity = new Vector2(direcaoDoGolpe * investidaVelocidade, rb.linearVelocity.y);

            // Rastro: copias do anjo ficando para tras, poeira no chao. E o que
            // faz uma investida ler como velocidade, e nao como teletransporte.
            relogioResiduo -= Time.deltaTime;
            if (relogioResiduo <= 0f)
            {
                relogioResiduo = 0.04f;
                Efeitos.Residuo(bodyRenderer, new Color(0.8f, 0.65f, 1f, 0.55f), 0.3f);
                Efeitos.Poeira(new Vector3(x, AlturaDosPes(), 0f), 1, 0.4f);
            }

            // Acerta UMA vez por investida, quem estiver no caminho do corpo.
            if (!investidaAcertou && jogador != null)
            {
                Vector2 d = jogador.position - transform.position;
                if (Mathf.Abs(d.x) < 1.1f && Mathf.Abs(d.y) < 1.8f)
                {
                    investidaAcertou = true;
                    Ferir(specialAttackDamage);
                }
            }

            // Para no fim da faixa, numa parede (parou de andar) ou no limite de tempo.
            bool chegou = direcaoDoGolpe > 0f ? x >= investidaFimX : x <= investidaFimX;
            investidaParado = Mathf.Abs(x - investidaUltimoX) < 0.01f ? investidaParado + 1 : 0;
            investidaUltimoX = x;

            if (chegou || investidaParado > 5 || stateTimer <= 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                CameraShake.Shake(0.2f, 0.18f);
                var chao = new Vector3(x, AlturaDosPes(), 0f);
                Efeitos.Onda(chao, 2.6f, corInvestida, 0.5f);
                Efeitos.Poeira(chao, 8, 1.2f);
                Efeitos.Penas(transform.position + new Vector3(0f, 1f, 0f), 10, 2.5f, 0.9f);
                ChangeState(BossState.Retreat);
                // Depois do golpe mais arriscado de fugir vem o maior respiro.
                stateTimer *= 1.4f;
            }
        }

        /// <summary>
        /// CHUVA DE FOGO (anjo): o anjo ergue os bracos e marca varios pontos do
        /// chao em volta do jogador - um exatamente embaixo dele. O fogo cai de
        /// cima, um por vez; cada marca mostra pelo proprio disco quanto falta.
        /// </summary>
        private float IniciarChuva()
        {
            if (jogador != null) OlharPara(jogador.position.x - transform.position.x);
            Vector3 centro = PontoNoChaoDoJogador();
            float carga = cargaChuva * Ritmo;
            int quantidade = chuvaQuantidade + (furia ? 1 : 0);

            var pontos = new List<Vector3> { centro };
            for (int tentativa = 0; pontos.Count < quantidade && tentativa < 30; tentativa++)
            {
                float x = centro.x + Random.Range(-chuvaEspalhamento, chuvaEspalhamento);
                // Marcas grudadas viram uma so; afastadas demais, um corredor livre.
                bool perto = pontos.Exists(p => Mathf.Abs(p.x - x) < chuvaRaio * 1.6f);
                if (perto) continue;
                Vector3 p2 = NoChao(new Vector3(x, centro.y, centro.z));
                if (p2.y > centro.y - 3f) pontos.Add(p2);
            }

            for (int i = 0; i < pontos.Count; i++)
                Agendar(pontos[i], chuvaRaio, carga + i * chuvaIntervalo, 0.45f, true, basicAttackDamage, telegraphColor);

            Efeitos.Lampejo(transform.position + new Vector3(0f, 1.2f, 0f), 5f, new Color(1f, 0.9f, 0.6f, 0.6f), 0.5f);
            Conjurar(carga);
            AudioManager.Instance?.PlaySfx(castSfx);
            return carga + (pontos.Count - 1) * chuvaIntervalo + 0.1f;
        }

        /// <summary>
        /// Fogo caindo numa marca: chao chamuscado que fica alguns segundos,
        /// brasas espirrando, onda e um lampejo - o impacto tem PESO, e o chao
        /// lembra onde ele caiu.
        /// </summary>
        private void EfeitoDeImpacto(Vector3 ponto, float raio)
        {
            Efeitos.Chamuscado(ponto, raio);
            Efeitos.Brasas(ponto + new Vector3(0f, 0.2f, 0f), 12, new Color(1f, 0.55f, 0.15f, 1f), forca: 3.5f, subida: 2f);
            Efeitos.Onda(ponto, raio * 1.3f, new Color(1f, 0.5f, 0.2f, 0.9f));
            Efeitos.Lampejo(ponto + new Vector3(0f, 0.4f, 0f), raio * 2.4f, new Color(1f, 0.7f, 0.3f, 0.75f));
        }

        /// <summary>
        /// O golpe de perto ganha um CORTE desenhado a frente do chefe: chama
        /// alaranjada no mago, luz branca e penas no anjo. Antes o golpe era so
        /// a faixa piscando - nada mostrava o braco (ou a asa) passando.
        /// </summary>
        private void EfeitoDeCorte()
        {
            bool anjo = forma == Forma.Anjo;
            float alcance = (anjo ? alcanceDoGolpeDeAsa : basicAttackRange) * 0.8f;
            var ponto = transform.position + new Vector3(direcaoDoGolpe * alcance, anjo ? 0.4f : 0.1f, 0f);
            Color cor = anjo ? new Color(1f, 0.95f, 0.8f, 1f) : new Color(1f, 0.6f, 0.25f, 1f);

            if (faiscaDeCorte != null && faiscaDeCorte.Length > 0)
                OneShotVFX.Spawn(faiscaDeCorte, ponto, anjo ? 3.2f : 2.4f, 22f, 24, cor);
            Efeitos.Lampejo(ponto, anjo ? 3f : 2f, new Color(cor.r, cor.g, cor.b, 0.6f), 0.2f);

            if (anjo) Efeitos.Penas(ponto, 8, 3f, 0.5f, new Vector2(direcaoDoGolpe, 0.2f));
            else Efeitos.Brasas(ponto, 8, cor, forca: 2.5f, subida: 1f);
        }

        /// <summary>
        /// PASSO SOMBRIO: o mago se desfaz em fumaca e reaparece perto do Ren.
        ///
        /// Justo porque avisa: antes de ele surgir, uma marca escura com fumaca
        /// aparece no chao do destino, e ele chega sem atacar - o primeiro golpe
        /// depois do passo e uma carga normal, com aviso normal. Enquanto esta
        /// desfeito nao pode ser atingido (nao ha corpo para atingir).
        /// </summary>
        private IEnumerator PassoSombrio()
        {
            emCena = true;
            currentState = BossState.Retreat;
            telegrafo.Aura(transform, 0f, baseColor);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            float total = passoSumir + passoEspera + passoSurgir;
            healthSystem.GrantInvulnerability(total);
            var caixa = GetComponent<Collider2D>();

            // Some.
            AudioManager.Instance?.PlaySfx(passoSfx);
            var origem = transform.position;
            OneShotVFX.Spawn(framesMorte, origem, 3.4f, 18f, 25, new Color(0.45f, 0.2f, 0.55f));
            Efeitos.Poeira(new Vector3(origem.x, AlturaDosPes(), 0f), 5, 0.9f, new Color(0.35f, 0.15f, 0.45f, 0.6f));
            for (float t = 0f; t < passoSumir; t += Time.deltaTime)
            {
                if (bodyRenderer != null) bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t / passoSumir);
                yield return null;
            }
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (caixa != null) caixa.enabled = false;
            rb.simulated = false;

            // Destino: do lado do Ren que tiver mais espaco, longe das paredes.
            float alvoX = jogador != null ? jogador.position.x : transform.position.x;
            float lado = alvoX > (limitesDaArena.x + limitesDaArena.y) * 0.5f ? -1f : 1f;
            if (Random.value < 0.35f) lado = -lado;
            float destinoX = Mathf.Clamp(alvoX + lado * passoDistancia, limitesDaArena.x, limitesDaArena.y);
            if (Mathf.Abs(destinoX - alvoX) < passoDistancia * 0.6f)
                destinoX = Mathf.Clamp(alvoX - lado * passoDistancia, limitesDaArena.x, limitesDaArena.y);
            var destino = new Vector3(destinoX, transform.position.y, transform.position.z);
            float chao = AlturaDosPes();
            var marca = new Vector3(destinoX, chao, 0f);

            // O aviso de onde ele vai surgir.
            Efeitos.Onda(marca, 1.4f, new Color(0.6f, 0.3f, 0.8f, 0.9f), passoEspera);
            for (float t = 0f; t < passoEspera; t += 0.1f)
            {
                Efeitos.Poeira(marca, 1, 0.6f, new Color(0.35f, 0.15f, 0.45f, 0.6f));
                yield return new WaitForSeconds(0.1f);
            }

            // Surge.
            transform.position = destino;
            if (jogador != null) OlharPara(jogador.position.x - destinoX);
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            if (caixa != null) caixa.enabled = true;
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            OneShotVFX.Spawn(framesMorte, destino, 3.4f, 18f, 25, new Color(0.45f, 0.2f, 0.55f));
            Efeitos.Lampejo(destino, 3f, new Color(0.7f, 0.4f, 0.9f, 0.6f));
            AudioManager.Instance?.PlaySfx(passoSfx);
            for (float t = 0f; t < passoSurgir; t += Time.deltaTime)
            {
                if (bodyRenderer != null) bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, t / passoSurgir);
                yield return null;
            }
            if (bodyRenderer != null) bodyRenderer.color = baseColor;

            emCena = false;
            ChangeState(BossState.Retreat);
            stateTimer *= 0.5f;   // chega e logo decide: o passo ja foi o "respiro"
        }

        /// <summary>
        /// INVOCACAO (uma vez, ao ficar agressivo): a Vigilia guarda as almas
        /// de Aldenor - e chama os mortos do cemiterio para defende-la. Ergue
        /// os bracos sobre um circulo grande, o chao se abre em dois pontos
        /// longe do Ren, e Guardioes de Pedra se levantam (a animacao de 6
        /// quadros do esqueleto saindo da terra). Erguendo-se, eles nao andam
        /// nem machucam: ha tempo de se posicionar.
        /// </summary>
        private IEnumerator Invocar()
        {
            if (prefabDoMorto == null || mortosInvocados <= 0) yield break;

            emCena = true;
            currentState = BossState.Retreat;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AcompanharJogador();
            AudioManager.Instance?.PlaySfx(invocarSfx);
            Conjurar(1.3f);

            // Os pontos: dos dois lados do Ren, longe o bastante para nao
            // nascerem em cima dele.
            float centro = jogador != null ? jogador.position.x : transform.position.x;
            float chao = AlturaDosPes();
            var pontos = new List<Vector3>();
            for (int i = 0; i < mortosInvocados; i++)
            {
                float lado = (i % 2 == 0) ? -1f : 1f;
                float x = Mathf.Clamp(centro + lado * (4f + i / 2 * 2f), limitesDaArena.x, limitesDaArena.y);
                if (jogador != null && Mathf.Abs(x - centro) < 3f) x = Mathf.Clamp(centro - lado * 4f, limitesDaArena.x, limitesDaArena.y);
                pontos.Add(new Vector3(x, chao, 0f));
            }

            const float preparo = 1.3f;
            for (float t = 0f; t < preparo; t += Time.deltaTime)
            {
                float pr = t / preparo;
                telegrafo.Circulo(new Vector3(transform.position.x, chao, 0f), 2.6f, new Color(0.55f, 1f, 0.6f, 1f));
                telegrafo.Aura(transform, Mathf.Lerp(1.5f, 4f, pr), new Color(0.4f, 1f, 0.5f, 1f));
                if (Random.value < 0.25f)
                    foreach (var pt in pontos) Efeitos.Poeira(pt, 1, 0.7f, new Color(0.3f, 0.45f, 0.3f, 0.6f));
                yield return null;
            }

            CameraShake.Shake(0.35f, 0.25f);
            foreach (var pt in pontos)
            {
                Efeitos.Onda(pt, 1.6f, new Color(0.5f, 1f, 0.55f, 0.9f));
                Efeitos.Poeira(pt, 6, 1f, new Color(0.3f, 0.25f, 0.2f, 0.7f));
                var morto = Instantiate(prefabDoMorto, pt + new Vector3(0f, 0.9f, 0f), Quaternion.identity);
                var inimigo = morto.GetComponent<EnemyBase>();
                if (inimigo != null)
                {
                    inimigo.ErguerAgora();
                    mortos.Add(inimigo);
                }
            }

            telegrafo.Aura(transform, 0f, baseColor);
            if (spriteAnim != null) spriteAnim.VoltarAoPadrao();
            emCena = false;
            ChangeState(BossState.Retreat);
        }

        /// <summary>Os mortos invocados desabam (transformacao ou morte do chefe).</summary>
        private void LibertarMortos()
        {
            foreach (var m in mortos)
            {
                if (m == null) continue;
                var vida = m.GetComponent<HealthSystem>();
                if (vida != null && !vida.IsDead) vida.Kill();
            }
            mortos.Clear();
        }

        private void Agendar(Vector3 ponto, float raio, float tempo, float voo, bool doCeu, int dano, Color cor)
        {
            // O fogo que vem de cima (a chuva) ganha uma coluna de luz sobre a marca.
            telegrafo.MostrarArea(ponto, raio, tempo, cor, doCeu);
            impactos.Add(new Impacto { ponto = ponto, raio = raio, tempo = tempo, voo = voo, doCeu = doCeu, dano = dano });
        }

        /// <summary>
        /// Relogio dos golpes agendados. Roda em todo quadro, independente do
        /// estado - uma salva lancada continua caindo mesmo que o chefe ja tenha
        /// passado ao respiro. Cada bola sai `voo` segundos antes do impacto e
        /// voa exatamente esse tempo, entao pousa no quadro em que o disco da
        /// marca encosta no contorno.
        /// </summary>
        private void AtualizarImpactos(float dt)
        {
            for (int i = impactos.Count - 1; i >= 0; i--)
            {
                var imp = impactos[i];
                imp.tempo -= dt;

                if (!imp.lancado && imp.tempo <= imp.voo)
                {
                    imp.lancado = true;
                    Vector3 de = imp.doCeu
                        ? imp.ponto + new Vector3(Random.Range(-0.4f, 0.4f), 7.5f, 0f)
                        : transform.position + new Vector3(direcaoDoGolpe * 0.45f, 0.25f, 0f);
                    var bola = Projetil.Lancar(fireballFrames, fireballFps, de, imp.ponto, Mathf.Max(0.05f, imp.tempo),
                                               fireballEscala, imp.doCeu ? 0f : 1.2f, specialVfxFrames, 1.8f,
                                               rastro: new Color(1f, 0.55f, 0.15f, 1f));
                    if (bola != null) projeteis.Add(bola);
                }

                if (imp.tempo <= 0f)
                {
                    EfeitoDeImpacto(imp.ponto, imp.raio);
                    AplicarDanoEm(imp.ponto, imp.raio, imp.dano);
                    AudioManager.Instance?.PlaySfx(impactoSfx);
                    CameraShake.Shake(0.15f, 0.16f);
                    impactos.RemoveAt(i);
                }
            }

            projeteis.RemoveAll(p => p == null);
        }

        private void CancelarGolpes()
        {
            impactos.Clear();
            foreach (var p in projeteis) if (p != null) Destroy(p.gameObject);
            projeteis.Clear();
            telegrafo.Cancelar();
        }

        // ------------------------------------------------------------------
        // Transformacao
        // ------------------------------------------------------------------

        /// <summary>
        /// A VIGILIA SE REVELA.
        ///
        /// Uma troca de sprite de um quadro para o outro nao le como
        /// transformacao - le como bug. Por isso a troca acontece ATRAS de um
        /// clarao de tela cheia, e o que vem antes dele prepara o olho:
        ///   1. tudo para: golpes no ar sao cancelados, o Ren perde o controle
        ///      (ninguem leva dano durante a cena) e a camera vai ate o chefe;
        ///   2. ~1,6 s de convulsao: o corpo pisca branco e vermelho cada vez mais
        ///      rapido, fumaça negra se desprende dele (a morte da igreja, em
        ///      pedacos) e a aura cresce;
        ///   3. estouro: trovao, tremor forte, clarao branco - e por tras dele o
        ///      mago vira anjo;
        ///   4. o anjo abre as asas sob o cartao com o novo nome, a barra troca
        ///      de nome e de cor, a trilha sobe, e a luta recomeca.
        /// </summary>
        private IEnumerator Transformar()
        {
            transformando = true;
            currentState = BossState.Transformando;
            CancelarGolpes();
            rb.linearVelocity = Vector2.zero;
            healthSystem.GrantInvulnerability(4.5f);

            var ren = jogador != null ? jogador.GetComponent<PlayerController>() : FindAnyObjectByType<PlayerController>();
            if (ren != null)
            {
                ren.ControleBloqueado = true;
                var rbRen = ren.GetComponent<Rigidbody2D>();
                if (rbRen != null) rbRen.linearVelocity = new Vector2(0f, rbRen.linearVelocity.y);
            }

            var seguidor = Camera.main != null ? Camera.main.GetComponent<SceneScript.CameraFollow2D>() : null;
            Transform alvoOriginal = seguidor != null ? seguidor.target : null;
            float suavidade = seguidor != null ? seguidor.smoothSpeed : 0f;
            if (seguidor != null) { seguidor.target = transform; seguidor.smoothSpeed = 2.5f; }

            AudioManager.Instance?.SetMusicIntensity(0.55f);
            AudioManager.Instance?.PlaySfx(transformacaoInicioSfx);
            // Os mortos que ela ergueu caem junto com o disfarce.
            LibertarMortos();
            if (spriteAnim != null) spriteAnim.VoltarAoPadrao();

            // Convulsao.
            const float convulsao = 1.6f;
            float proximaFumaca = 0f;
            for (float t = 0f; t < convulsao; t += Time.deltaTime)
            {
                float p = t / convulsao;
                float pisca = Mathf.PingPong(t * (8f + p * 22f), 1f);
                if (bodyRenderer != null) bodyRenderer.color = Color.Lerp(Color.white, telegraphColor, pisca);
                AplicarEscala(1f + 0.06f * Mathf.Sin(t * 40f) * p);
                telegrafo.Aura(transform, Mathf.Lerp(2f, 6f, p), telegraphColor);
                telegrafo.Circulo(new Vector3(transform.position.x, AlturaDosPes(), 0f), Mathf.Lerp(1.5f, 3.2f, p), telegraphColor);
                if (Random.value < 0.4f)
                    Efeitos.Brasas(transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.4f, 1.2f), 0f),
                                   1, new Color(1f, 0.35f, 0.25f, 1f), forca: 0.6f, subida: 2.5f, tamanho: 0.13f);

                proximaFumaca -= Time.deltaTime;
                if (proximaFumaca <= 0f)
                {
                    proximaFumaca = Mathf.Lerp(0.3f, 0.1f, p);
                    var lugar = transform.position + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(-0.6f, 1.2f), 0f);
                    OneShotVFX.Spawn(framesMorte, lugar, 1.6f, 14f, 25, new Color(0.35f, 0.1f, 0.2f));
                    CameraShake.Shake(0.08f, 0.04f + 0.1f * p);
                }
                yield return null;
            }

            // Estouro: a troca acontece atras do clarao.
            AudioManager.Instance?.PlaySfx(transformacaoEstouroSfx);
            CameraShake.Shake(0.7f, 0.6f);
            OneShotVFX.Spawn(framesMorte, transform.position, 5f, 12f, 25, Color.white);
            var clarao = UIFactory.Clarao(Color.white);
            VirarAnjo();
            // Por tras do clarao: as asas se abrem num estouro de penas e luz.
            Efeitos.Raios(transform.position + new Vector3(0f, 0.8f, 0f), 7, 14f, new Color(1f, 0.9f, 0.6f, 0.55f), 1.4f);
            Efeitos.Penas(transform.position + new Vector3(0f, 1f, 0f), 40, 6f, 1f);
            Efeitos.Onda(new Vector3(transform.position.x, AlturaDosPes(), 0f), 5f, new Color(1f, 0.9f, 0.6f, 1f), 0.8f);
            StartCoroutine(UIFactory.Desvanecer(clarao, 1f, 0f, 0.9f, true));

            FindAnyObjectByType<BossHealthBar>()?.Renomear(nomeDoAnjo, corDaBarraDoAnjo);
            AudioManager.Instance?.SetMusicIntensity(1.3f);

            var cartao = UIFactory.CartaoDeTitulo(nomeDoAnjo, epitetoDoAnjo, new Color(1f, 0.86f, 0.62f));
            StartCoroutine(UIFactory.Desvanecer(cartao, 0f, 1f, 0.4f));

            // O anjo abre as asas: a pose de ataque, devagar.
            if (spriteAnim != null && framesAnjoAtaque != null && framesAnjoAtaque.Length > 0)
                spriteAnim.PlayOnce(framesAnjoAtaque, 4f, () => spriteAnim.VoltarAoPadrao());

            yield return new WaitForSeconds(1.9f);

            StartCoroutine(UIFactory.Desvanecer(cartao, 1f, 0f, 0.5f, true));
            if (seguidor != null) { seguidor.target = alvoOriginal; seguidor.smoothSpeed = suavidade; }
            yield return new WaitForSeconds(0.4f);

            if (ren != null) ren.ControleBloqueado = false;
            telegrafo.Aura(transform, 0f, baseColor);
            transformando = false;
            ChangeState(BossState.Retreat);
        }

        private void VirarAnjo()
        {
            forma = Forma.Anjo;
            spriteOlhaParaDireita = anjoOlhaParaDireita;

            // A luz que fica atras do anjo pelo resto da luta.
            var go = new GameObject("HaloDoAnjo");
            halo = go.AddComponent<SpriteRenderer>();
            halo.sprite = RuntimeSprites.Brilho();
            halo.sortingOrder = 1;
            if (bodyRenderer != null) bodyRenderer.color = baseColor;

            if (spriteAnim != null && framesAnjoParado != null && framesAnjoParado.Length > 0)
                spriteAnim.SetFrames(framesAnjoParado, fpsAnjoParado);
            if (framesAnjoAtaque != null && framesAnjoAtaque.Length > 0)
                framesConjurar = framesAnjoAtaque;

            baseScaleAbs = escalaDoAnjo;
            AplicarEscala(1f);

            var caixa = GetComponent<BoxCollider2D>();
            if (caixa != null)
            {
                caixa.size = colisorDoAnjo;
                caixa.offset = deslocamentoDoColisorDoAnjo;
            }

            // O anjo enxerga a arena inteira: o mago so reagia de perto.
            detectionRadius = Mathf.Max(detectionRadius, 12f);
            patrolSpeed *= 1.3f;
        }

        /// <summary>
        /// Ultimo quarto da vida: avisos e respiros 15% mais curtos, uma marca a
        /// mais na chuva, mais investidas - e o corpo avermelhado para o
        /// jogador VER que algo mudou, alem de sentir.
        /// </summary>
        private void EntrarEmFuria()
        {
            furia = true;
            baseColor = new Color(1f, 0.72f, 0.72f);
            AudioManager.Instance?.SetMusicIntensity(1.45f);
            AudioManager.Instance?.PlaySfx(phaseChangeSfx);
            CameraShake.Shake(0.3f, 0.25f);
        }

#if UNITY_EDITOR
        // ------------------------------------------------------------------
        // Testes pelo Inspector (botao direito no componente, em play mode).
        // Esperar a sorte trazer um golpe especifico para ver como ele ficou
        // custa minutos; daqui cada golpe sai na hora.
        // ------------------------------------------------------------------

        private bool PodeTestar => Application.isPlaying && !healthSystem.IsDead && !transformando && !emCena;

        [ContextMenu("Testar: bola de fogo")]
        public void TestarBolaDeFogo() { if (PodeTestar && forma == Forma.Mago) ChangeState(BossState.Telegraph); }

        [ContextMenu("Testar: salva")]
        public void TestarSalva() { if (PodeTestar && forma == Forma.Mago) ChangeState(BossState.CargaSalva); }

        [ContextMenu("Testar: passo sombrio")]
        public void TestarPasso() { if (PodeTestar && forma == Forma.Mago) StartCoroutine(PassoSombrio()); }

        [ContextMenu("Testar: invocacao")]
        public void TestarInvocacao() { if (PodeTestar && forma == Forma.Mago) StartCoroutine(Invocar()); }

        [ContextMenu("Testar: transformacao")]
        public void TestarTransformacao()
        {
            if (!PodeTestar || forma != Forma.Mago) return;
            healthSystem.ForceDamage(Mathf.Max(0, healthSystem.CurrentHealth - Mathf.FloorToInt(healthSystem.MaxHealth * limiarTransformacao)));
        }

        [ContextMenu("Testar: investida")]
        public void TestarInvestida() { if (PodeTestar && forma == Forma.Anjo) ChangeState(BossState.CargaInvestida); }

        [ContextMenu("Testar: chuva de fogo")]
        public void TestarChuva() { if (PodeTestar && forma == Forma.Anjo) ChangeState(BossState.CargaChuva); }

        [ContextMenu("Testar: golpe de perto")]
        public void TestarGolpe() { if (PodeTestar) ChangeState(BossState.CargaBasica); }
#endif

        // ------------------------------------------------------------------
        // Utilidades de posicao e dano
        // ------------------------------------------------------------------

        /// <summary>
        /// Posicao do jogador rebaixada ate os pes dele. A marca de impacto e uma
        /// mancha no CHAO: desenhada na altura do centro do personagem, ela
        /// flutuava no ar, encostada nas lapides, e deixava de parecer chao.
        /// </summary>
        private Vector3 PontoNoChaoDoJogador()
        {
            if (jogador == null) return transform.position;

            Vector3 p = jogador.position;
            var col = jogador.GetComponent<Collider2D>();
            if (col != null) p.y = col.bounds.min.y + 0.08f;
            return p;
        }

        /// <summary>Desce um ponto ate o chao embaixo dele (marcas de outras alturas).</summary>
        private Vector3 NoChao(Vector3 p)
        {
            var hit = Physics2D.Raycast(new Vector2(p.x, p.y + 2f), Vector2.down, 8f, mascaraDoChao);
            if (hit.collider != null) p.y = hit.point.y + 0.08f;
            return p;
        }

        /// <summary>
        /// Toca a animacao de ataque esticada ou comprimida para caber
        /// EXATAMENTE no tempo de carga - o ultimo quadro do gesto cai no mesmo
        /// instante do golpe. No mago e a conjuracao (10 quadros); no anjo, a
        /// pose de bracos erguidos (3).
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
            var bola = Projetil.Lancar(fireballFrames, fireballFps, maos, pontoDeImpacto, tempoDeVoo,
                                       fireballEscala, 1.1f, specialVfxFrames, 2.2f,
                                       rastro: new Color(1f, 0.55f, 0.15f, 1f));
            Efeitos.Lampejo(maos, 1.6f, new Color(1f, 0.6f, 0.2f, 0.8f), 0.25f);
            if (bola != null) projeteis.Add(bola);
        }

        /// <summary>Altura do chao sob o chefe, para desenhar a faixa do golpe.</summary>
        private float AlturaDosPes()
        {
            var col = GetComponent<Collider2D>();
            return col != null ? col.bounds.min.y + 0.12f : transform.position.y;
        }

        private float DirecaoAteOJogador()
        {
            if (jogador == null) return LadoQueOlha();
            return jogador.position.x >= transform.position.x ? 1f : -1f;
        }

        protected override void DetectPlayer()
        {
            base.DetectPlayer();
            // Guarda o jogador mesmo quando ele sai do raio: golpes ja
            // agendados precisam saber onde ele esta no instante do impacto.
            if (detectedPlayer != null) jogador = detectedPlayer;
        }

        /// <summary>
        /// Chance de a bola de fogo sair num sorteio (a cada 0,3 s). Sobe quando
        /// o mago fica agressivo; a dificuldade desloca para os dois lados.
        /// </summary>
        private bool ShouldUseSpecialAttack()
        {
            float chance = agressivo ? 0.4f : 0.25f;
            chance = Mathf.Clamp01(chance + DifficultySettings.AjusteDeChanceEspecial);
            return Random.value < chance;
        }

        /// <summary>
        /// Dano do golpe corpo a corpo: acerta quem estiver DENTRO da faixa que
        /// foi desenhada - a frente do chefe, do lado anunciado.
        /// </summary>
        private void AplicarDanoDoArco()
        {
            if (jogador == null) return;

            Vector2 delta = jogador.position - transform.position;
            float alcance = (forma == Forma.Anjo ? alcanceDoGolpeDeAsa : basicAttackRange) * 1.2f;

            bool noLadoCerto = Mathf.Sign(delta.x) == Mathf.Sign(direcaoDoGolpe) || Mathf.Abs(delta.x) < 0.2f;
            if (!noLadoCerto) return;
            if (Mathf.Abs(delta.x) > alcance || Mathf.Abs(delta.y) > 1.6f) return;

            Ferir(basicAttackDamage);
        }

        /// <summary>
        /// Dano de uma marca no chao: acerta apenas quem estiver dentro dela.
        /// A marca fica no chao e o jogador e medido pelo centro: por isso a
        /// comparacao horizontal usa o raio desenhado e a vertical e mais
        /// folgada - quem esta em pe (ou pulando baixo) sobre a marca leva o
        /// golpe; quem saiu de lado, nao.
        /// </summary>
        private void AplicarDanoEm(Vector3 ponto, float raio, int dano)
        {
            if (jogador == null) return;

            Vector2 d = (Vector2)jogador.position - (Vector2)ponto;
            if (Mathf.Abs(d.x) > raio) return;
            if (d.y < -0.5f || d.y > raio + 1f) return;

            Ferir(dano);
        }

        private void Ferir(int dano)
        {
            if (jogador == null) return;
            HealthSystem playerHealth = jogador.GetComponent<HealthSystem>();
            if (playerHealth == null || playerHealth.IsInvulnerable || playerHealth.IsDead) return;

            playerHealth.TakeDamage(dano, transform.position);
            // A estrela de impacto no Ren: o golpe que ACERTA tem de parecer
            // diferente do que passou perto.
            if (faiscaDeImpacto != null && faiscaDeImpacto.Length > 0)
                OneShotVFX.Spawn(faiscaDeImpacto, jogador.position + new Vector3(0f, 0.4f, 0f), 2.4f, 24f, 26,
                                 forma == Forma.Anjo ? new Color(1f, 0.95f, 0.75f) : new Color(1f, 0.6f, 0.3f));
        }

        protected override void HandleDeath()
        {
            CancelarGolpes();
            LibertarMortos();
            if (halo != null) Destroy(halo.gameObject);
            // Explosao grande e tremor forte na morte do chefe.
            Sprite[] deathFrames = (deathVfxFrames != null && deathVfxFrames.Length > 0) ? deathVfxFrames : specialVfxFrames;
            OneShotVFX.Spawn(deathFrames, transform.position, 3.5f, 14f);
            CameraShake.Shake(0.6f, 0.55f);
            if (bodyRenderer != null) bodyRenderer.color = baseColor;
            AplicarEscala(1f);
            base.HandleDeath();
        }

        /// <summary>
        /// Acompanha a vida e vira os atos da luta. Chamado toda vez que o
        /// chefe recebe dano.
        /// </summary>
        private void EvaluatePhase(int current, int max)
        {
            if (max <= 0 || healthSystem.IsDead) return;
            float vida = (float)current / max;

            if (!agressivo && vida <= limiarAgressivo)
            {
                agressivo = true;
                EntrarNaFase();
                if (forma == Forma.Mago && vida > limiarTransformacao) invocacaoPendente = true;
            }

            if (forma == Forma.Mago && !transformando && vida <= limiarTransformacao)
            {
                // Um passo sombrio ou uma invocacao em andamento cede lugar a
                // transformacao: o corpo volta a ser visivel antes da cena.
                StopAllCoroutines();
                emCena = false;
                invocacaoPendente = false;
                if (bodyRenderer != null) { bodyRenderer.enabled = true; bodyRenderer.color = baseColor; }
                var caixa = GetComponent<Collider2D>();
                if (caixa != null) caixa.enabled = true;
                rb.simulated = true;
                StartCoroutine(Transformar());
                return;
            }

            if (forma == Forma.Anjo && !furia && vida <= limiarFuria) EntrarEmFuria();
        }

        /// <summary>
        /// A luta aperta a cada ato, e a trilha aperta junto. Uma trilha que
        /// sobe de intensidade e um toque de batalha na passagem dizem "isto
        /// mudou" no momento exato em que muda. A intensidade volta a 1 sozinha
        /// quando outra cena pede a sua musica.
        /// </summary>
        private void EntrarNaFase()
        {
            AudioManager.Instance?.SetMusicIntensity(1.12f);
            AudioManager.Instance?.PlaySfx(phaseChangeSfx);
            CameraShake.Shake(0.25f, 0.2f);
        }
    }
}
