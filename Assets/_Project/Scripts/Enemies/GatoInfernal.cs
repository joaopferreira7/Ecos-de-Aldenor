using UnityEngine;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// GATO INFERNAL - o inimigo da Fase 3 (A Cidade Silenciosa).
    ///
    /// Os outros inimigos se resolvem batendo. Este ensina a ESQUIVAR, que e o
    /// que a luta final cobra (investida e passo sombrio da Vigilia):
    ///
    ///   patrulha  -> ao ver o Ren, para e se ENCOLHE (corpo achatado e
    ///                avermelhado): e o aviso;
    ///   salto     -> pula num arco curto ate onde o Ren estava quando o aviso
    ///                comecou - quem rolou ou pulou no aviso nao esta mais la;
    ///   pouso     -> fica ATORDOADO um instante: a janela para punir.
    ///
    /// Segue a regra de todo golpe do jogo: carga -> impacto -> respiro.
    ///
    /// O pacote traz so a corrida (4 quadros), sem ataque nem morte: o aviso e
    /// feito por cor e deformacao, como a carga do chefe, e a morte usa a
    /// baforada de morte comum aos inimigos.
    ///
    /// O salto nunca sai da faixa de patrulha: um inimigo que pula sobre um
    /// abismo leva o jogador junto, e a fase foi desenhada com isso em mente.
    /// </summary>
    public class GatoInfernal : EnemyBase
    {
        [Header("Salto")]
        [Tooltip("Tempo encolhido antes de saltar: o aviso.")]
        [SerializeField] private float tempoDeAviso = 0.5f;
        [Tooltip("Distancia maxima do salto, em unidades.")]
        [SerializeField] private float alcanceDoSalto = 3.2f;
        [Tooltip("Tempo no ar. Fixo, para o arco ser sempre o mesmo e o jogador aprender o ritmo.")]
        [SerializeField] private float tempoDeVoo = 0.55f;
        [Tooltip("Atordoado depois de pousar: a janela para punir.")]
        [SerializeField] private float tempoAtordoado = 1f;
        [Tooltip("Espera entre um salto e o proximo.")]
        [SerializeField] private float recarga = 1.2f;
        [SerializeField] private Color corDoAviso = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private AudioClip saltoSfx;
        [SerializeField] private AudioClip pousoSfx;

        private enum Estado { Patrulha, Encolhendo, NoAr, Atordoado }
        private Estado estado = Estado.Patrulha;
        private float relogio;
        private float proximoSalto;
        private float alvoX;
        private Vector3 escalaBase;
        private Color corDoCorpo = Color.white;

        protected override void Awake()
        {
            base.Awake();
            escalaBase = transform.localScale;
            if (corpo != null) corDoCorpo = corpo.color;
            tempoDeAviso = DifficultySettings.TelegrafoDoChefe(tempoDeAviso);
            tempoAtordoado = DifficultySettings.RespiroDoChefe(tempoAtordoado);
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
                case Estado.Encolhendo: Encolher(); break;
                case Estado.NoAr: Voar(); break;
                case Estado.Atordoado: Descansar(); break;
            }
        }

        /// <summary>Viu o Ren: em vez de correr atras dele, prepara o salto.</summary>
        protected override void ChasePlayer()
        {
            if (Time.time < proximoSalto) { base.ChasePlayer(); return; }

            // Trava o alvo AGORA: o aviso inteiro e o tempo que o jogador tem
            // para sair do lugar.
            alvoX = detectedPlayer.position.x;
            OlharPara(alvoX - transform.position.x);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator(false);
            Mudar(Estado.Encolhendo);
        }

        private void Encolher()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Achata e avermelha aos poucos; o tremor no fim diz "agora".
            float k = Mathf.Clamp01(relogio / tempoDeAviso);
            float tremor = k > 0.7f ? Mathf.Sin(Time.time * 70f) * 0.03f : 0f;
            Escalar(1f + 0.12f * k, 1f - 0.25f * k + tremor);
            if (corpo != null && !EmClarao) corpo.color = Color.Lerp(corDoCorpo, corDoAviso, k);

            if (relogio >= tempoDeAviso) Saltar();
        }

        private void Saltar()
        {
            Escalar(1f, 1f);
            if (corpo != null) corpo.color = corDoCorpo;

            float x = transform.position.x;
            float destino = Mathf.Clamp(alvoX, x - alcanceDoSalto, x + alcanceDoSalto);
            if (LimitesDaPatrulha(out float a, out float b)) destino = Mathf.Clamp(destino, a, b);

            // Arco balistico que pousa no destino em exatamente tempoDeVoo:
            // sobe a metade do tempo e desce a outra metade.
            float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            rb.linearVelocity = new Vector2((destino - x) / tempoDeVoo, g * tempoDeVoo * 0.5f);

            AudioManager.Instance?.PlaySfx(saltoSfx);
            Efeitos.Poeira(PesNoChao(), 6, 0.8f);
            Mudar(Estado.NoAr);
        }

        private void Voar()
        {
            CheckContactDamage();

            // Pousou: ja passou do topo do arco e parou de cair. O limite de
            // tempo cobre um pouso numa quina que nunca zera a velocidade.
            bool pousou = relogio > tempoDeVoo * 0.5f && rb.linearVelocity.y > -0.05f;
            if (!pousou && relogio < tempoDeVoo + 0.8f) return;

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AudioManager.Instance?.PlaySfx(pousoSfx);
            Efeitos.Poeira(PesNoChao(), 10, 1.4f);
            CameraShake.Shake(0.08f, 0.06f);
            Mudar(Estado.Atordoado);
        }

        private void Descansar()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Ofegante: balanca devagar, um pouco apagado - e a hora de bater.
            float ofego = Mathf.Sin(relogio * 9f) * 0.04f;
            Escalar(1f, 1f + ofego);
            if (corpo != null && !EmClarao) corpo.color = Color.Lerp(corDoCorpo, Color.gray, 0.35f);

            if (relogio >= tempoAtordoado) VoltarAPatrulha();
        }

        private void VoltarAPatrulha()
        {
            Escalar(1f, 1f);
            if (corpo != null) corpo.color = corDoCorpo;
            proximoSalto = Time.time + recarga;
            Mudar(Estado.Patrulha);
        }

        private void Mudar(Estado novo)
        {
            estado = novo;
            relogio = 0f;
        }

        /// <summary>Um golpe no meio do aviso cancela o salto: bater primeiro tambem vale.</summary>
        protected override void HandleDamaged()
        {
            if (estado == Estado.Encolhendo || estado == Estado.Atordoado) VoltarAPatrulha();
            base.HandleDamaged();
        }

        public override void Atordoar(float segundos)
        {
            if (estado != Estado.Patrulha) VoltarAPatrulha();
            base.Atordoar(segundos);
        }

        protected override void HandleDeath()
        {
            Escalar(1f, 1f);
            estado = Estado.Patrulha;
            base.HandleDeath();
        }

        /// <summary>
        /// Deforma o corpo sem desfazer o lado para onde ele olha (o sinal da
        /// escala X e o espelhamento - ver EnemyBase.OlharPara).
        /// </summary>
        private void Escalar(float largura, float altura)
        {
            float lado = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(lado * Mathf.Abs(escalaBase.x) * largura, escalaBase.y * altura, escalaBase.z);
        }

        private Vector3 PesNoChao()
        {
            var col = GetComponent<Collider2D>();
            float y = col != null ? col.bounds.min.y : transform.position.y;
            return new Vector3(transform.position.x, y, 0f);
        }
    }
}
