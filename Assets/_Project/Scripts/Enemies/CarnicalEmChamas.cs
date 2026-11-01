using UnityEngine;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// CARNICAL EM CHAMAS - o inimigo da Fase 4 (O Claustro).
    ///
    /// Um morto de cabeca em chamas que nao persegue: DISPARA. Ensina a pular
    /// por cima de uma ameaca rasteira, que e o que a onda de choque da
    /// investida da Vigilia cobra:
    ///
    ///   patrulha  -> anda devagar, sacudindo o fogo da cabeca;
    ///   aviso     -> ao ver o Ren, para e a chama se ATICA (brasas e brilho):
    ///                um instante curto, mas ha um;
    ///   disparada -> corre em linha reta ate o fim da propria faixa, sem
    ///                virar, deixando um rastro de fogo que queima por 1,5 s;
    ///   ofegante  -> para no fim da corrida, apagado: a janela para punir.
    ///
    /// A arte e do pacote Church (o mesmo do chefe): as duas corridas de 8
    /// quadros viram a caminhada e a disparada. Nao ha quadros de ataque nem
    /// de morte; a morte usa a baforada comum aos inimigos.
    /// </summary>
    public class CarnicalEmChamas : EnemyBase
    {
        [Header("Disparada")]
        [SerializeField] private float tempoDeAviso = 0.4f;
        [Tooltip("Velocidade da disparada, em multiplos da velocidade de patrulha.")]
        [SerializeField] private float multiplicadorDaDisparada = 2.5f;
        [SerializeField] private float tempoOfegante = 1f;
        [SerializeField] private float recarga = 1.5f;
        [Tooltip("De quanto em quanto ele deixa um trecho de fogo no chao.")]
        [SerializeField] private float espacoDoRastro = 0.45f;
        [SerializeField] private float vidaDoRastro = 1.5f;
        [SerializeField] private int danoDoRastro = 1;
        [SerializeField] private Sprite[] framesDisparada;
        [SerializeField] private float fpsDisparada = 16f;
        [SerializeField] private AudioClip disparadaSfx;

        private enum Estado { Patrulha, Aviso, Disparada, Ofegante }
        private Estado estado = Estado.Patrulha;
        private float relogio;
        private float proximaDisparada;
        private float direcao;
        private float alvoX;
        private float ultimoRastroX;
        private float ultimoX;
        private float parado;
        private Color corDoCorpo = Color.white;

        protected override void Awake()
        {
            base.Awake();
            if (corpo != null) corDoCorpo = corpo.color;
            tempoDeAviso = DifficultySettings.TelegrafoDoChefe(tempoDeAviso);
            tempoOfegante = DifficultySettings.RespiroDoChefe(tempoOfegante);
            danoDoRastro = DifficultySettings.DanoDeInimigo(danoDoRastro);
        }

        protected override void Update()
        {
            if (estado == Estado.Patrulha || healthSystem.IsDead)
            {
                base.Update();
                return;
            }

            AtualizarClarao();
            if (contactDamageTimer > 0f) contactDamageTimer -= Time.deltaTime;
            relogio += Time.deltaTime;

            switch (estado)
            {
                case Estado.Aviso: Avisar(); break;
                case Estado.Disparada: Correr(); break;
                case Estado.Ofegante: Ofegar(); break;
            }
        }

        protected override void ChasePlayer()
        {
            if (Time.time < proximaDisparada) { Patrol(); return; }

            direcao = detectedPlayer.position.x >= transform.position.x ? 1f : -1f;

            // Corre ate a ponta da faixa do lado do Ren - nunca alem dela, onde
            // pode haver abismo.
            if (LimitesDaPatrulha(out float a, out float b)) alvoX = direcao > 0f ? b : a;
            else alvoX = transform.position.x + direcao * 6f;

            // Ja esta encostado na ponta: nao ha corrida a fazer.
            if (Mathf.Abs(alvoX - transform.position.x) < 0.5f) { Patrol(); return; }

            OlharPara(direcao);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (spriteAnim != null && framesAndando != null && framesAndando.Length > 0)
                spriteAnim.PlayLoop(framesAndando, fpsAndando * 0.5f);
            Mudar(Estado.Aviso);
        }

        private void Avisar()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // A chama se atica: brilho subindo e brasas saindo da cabeca.
            float k = Mathf.Clamp01(relogio / tempoDeAviso);
            if (corpo != null && !EmClarao) corpo.color = Color.Lerp(corDoCorpo, new Color(1f, 0.75f, 0.4f, 1f), k);
            if (Random.value < 0.5f)
                Efeitos.Brasas(Cabeca(), 1, new Color(1f, 0.6f, 0.2f, 1f), forca: 1f, subida: 2f, tamanho: 0.14f, vida: 0.5f);

            if (relogio < tempoDeAviso) return;

            if (corpo != null) corpo.color = corDoCorpo;
            if (spriteAnim != null && framesDisparada != null && framesDisparada.Length > 0)
                spriteAnim.PlayLoop(framesDisparada, fpsDisparada);
            AudioManager.Instance?.PlaySfx(disparadaSfx);
            Efeitos.Poeira(Pes(), 8, 1f);
            ultimoRastroX = ultimoX = transform.position.x;
            parado = 0f;
            Mudar(Estado.Disparada);
        }

        private void Correr()
        {
            float x = transform.position.x;
            rb.linearVelocity = new Vector2(direcao * patrolSpeed * multiplicadorDaDisparada, rb.linearVelocity.y);
            CheckContactDamage();

            if (Mathf.Abs(x - ultimoRastroX) >= espacoDoRastro)
            {
                ultimoRastroX = x;
                FogoNoChao.Criar(Pes(), vidaDoRastro, danoDoRastro, playerLayer);
            }

            // Chegou a ponta - ou bateu em algo e nao sai do lugar.
            parado = Mathf.Abs(x - ultimoX) < 0.005f ? parado + Time.deltaTime : 0f;
            ultimoX = x;
            bool chegou = direcao > 0f ? x >= alvoX : x <= alvoX;
            if (!chegou && parado < 0.25f && relogio < 6f) return;

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Efeitos.Poeira(Pes(), 6, 0.8f);
            if (spriteAnim != null && framesAndando != null && framesAndando.Length > 0)
                spriteAnim.PlayLoop(framesAndando, fpsAndando * 0.4f);
            Mudar(Estado.Ofegante);
        }

        private void Ofegar()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (corpo != null && !EmClarao) corpo.color = Color.Lerp(corDoCorpo, Color.gray, 0.4f);
            if (relogio >= tempoOfegante) VoltarAPatrulha();
        }

        private void VoltarAPatrulha()
        {
            if (corpo != null) corpo.color = corDoCorpo;
            proximaDisparada = Time.time + recarga;
            Mudar(Estado.Patrulha);
        }

        private void Mudar(Estado novo)
        {
            estado = novo;
            relogio = 0f;
        }

        /// <summary>Bater no aviso cancela a disparada; no meio dela, nao - ela e um compromisso.</summary>
        protected override void HandleDamaged()
        {
            if (estado == Estado.Aviso || estado == Estado.Ofegante) VoltarAPatrulha();
            base.HandleDamaged();
        }

        /// <summary>A onda do sino para ate a disparada.</summary>
        public override void Atordoar(float segundos)
        {
            if (estado != Estado.Patrulha) VoltarAPatrulha();
            base.Atordoar(segundos);
        }

        protected override void HandleDeath()
        {
            estado = Estado.Patrulha;
            base.HandleDeath();
        }

        private Vector3 Pes()
        {
            var col = GetComponent<Collider2D>();
            return new Vector3(transform.position.x, col != null ? col.bounds.min.y : transform.position.y, 0f);
        }

        private Vector3 Cabeca()
        {
            var col = GetComponent<Collider2D>();
            return new Vector3(transform.position.x, col != null ? col.bounds.max.y : transform.position.y + 1f, 0f);
        }
    }
}
