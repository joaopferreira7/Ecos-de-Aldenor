using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// Indicadores visuais dos ataques do chefe.
    ///
    /// O problema que este componente resolve: A Vigilia e um sprite fantasma de
    /// cinco quadros, sem Animator e sem animacao de golpe. Um piscar de cor no
    /// corpo era todo o aviso que existia, e so para o ataque especial - o golpe
    /// corpo a corpo nao avisava nada. Sem animacao de ataque, o aviso tem de
    /// vir de outro lugar: da AREA. E o que se ve em quase todo chefe legivel:
    /// a marca no chao aparece antes, cresce enquanto o golpe carrega e some no
    /// impacto, entao o jogador le "onde" e "quando" sem precisar decorar o
    /// inimigo.
    ///
    /// Sao tres pecas, todas geradas em codigo (RuntimeSprites), nenhuma arte
    /// nova:
    ///   - AREA: disco no chao com contorno, no ponto exato onde o especial vai
    ///     cair. O preenchimento cresce do centro para a borda; quando encosta
    ///     no contorno, o golpe acontece.
    ///   - ARCO: faixa a frente do chefe, mostrando o alcance do golpe corpo a
    ///     corpo e para que lado ele vai.
    ///   - AURA: brilho atras do chefe que cresce durante a carga, para separar
    ///     "chefe andando" de "chefe carregando" mesmo se o jogador estiver
    ///     olhando para o proprio personagem.
    ///
    /// Os objetos ficam soltos na cena (nao sao filhos do chefe) de proposito: o
    /// chefe tem escala 6, e como filhos os indicadores herdariam essa escala e
    /// tambem o espelhamento horizontal do flip.
    /// </summary>
    public class BossTelegraph : MonoBehaviour
    {
        private const int OrdemNoChao = 2;   // abaixo do chefe (3), acima do cenario

        private SpriteRenderer areaFundo;    // disco que cresce
        private SpriteRenderer areaBorda;    // contorno fixo da area
        private SpriteRenderer arco;         // faixa do golpe corpo a corpo
        private SpriteRenderer aura;         // brilho de carga em volta do chefe

        private Vector3 areaCentro;
        private float areaRaio;
        private float duracao;
        private float restante;
        private bool areaAtiva;

        private Transform donoDoArco;
        private float arcoAlcance;
        private float arcoY;         // altura do chao, onde a faixa e desenhada
        private float arcoDirecao = 1f;
        private bool arcoAtivo;

        private Color cor = Color.red;
        private float flash;      // 0..1, clarao do impacto
        private float auraAlvo;   // escala desejada da aura

        private void Awake()
        {
            areaFundo = Criar("TelegrafoArea", RuntimeSprites.Disco(), OrdemNoChao);
            areaBorda = Criar("TelegrafoBorda", RuntimeSprites.Anel(), OrdemNoChao + 1);
            arco = Criar("TelegrafoArco", RuntimeSprites.Solido(), OrdemNoChao);
            aura = Criar("TelegrafoAura", RuntimeSprites.Brilho(), OrdemNoChao - 1);
            EsconderTudo();
        }

        private void OnDestroy()
        {
            // Os indicadores nao sao filhos do chefe, entao a morte dele nao os
            // leva junto: quem cria, limpa.
            Remover(areaFundo); Remover(areaBorda); Remover(arco); Remover(aura);
        }

        private static void Remover(SpriteRenderer sr)
        {
            if (sr != null) Destroy(sr.gameObject);
        }

        private SpriteRenderer Criar(string nome, Sprite sprite, int ordem)
        {
            var go = new GameObject(nome);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = ordem;
            sr.enabled = false;
            return sr;
        }

        /// <summary>
        /// Marca no chao onde o golpe forte vai cair. O ponto e travado agora:
        /// se o jogador sair da marca a tempo, o golpe erra - e isso que torna o
        /// ataque justo em vez de inevitavel.
        /// </summary>
        public void MostrarArea(Vector3 centro, float raio, float tempoDeCarga, Color corAviso)
        {
            areaCentro = centro;
            areaRaio = raio;
            duracao = Mathf.Max(0.01f, tempoDeCarga);
            restante = duracao;
            cor = corAviso;
            areaAtiva = true;
            flash = 0f;

            areaFundo.enabled = true;
            areaBorda.enabled = true;
            areaFundo.transform.position = centro;
            areaBorda.transform.position = centro;
            // A altura e achatada: e uma marca no chao vista de lado, nao um circulo.
            areaBorda.transform.localScale = new Vector3(raio * 2f, raio * 0.9f, 1f);
        }

        /// <summary>
        /// Faixa a frente do chefe mostrando alcance e direcao do golpe corpo a
        /// corpo. Acompanha o chefe, mas a direcao fica travada em quem chamou.
        /// </summary>
        public void MostrarArco(Transform dono, float alcance, float direcao, float alturaDoChao,
                                float tempoDeCarga, Color corAviso)
        {
            donoDoArco = dono;
            arcoAlcance = alcance;
            arcoY = alturaDoChao;
            arcoDirecao = direcao >= 0f ? 1f : -1f;
            duracao = Mathf.Max(0.01f, tempoDeCarga);
            restante = duracao;
            cor = corAviso;
            arcoAtivo = true;
            flash = 0f;

            arco.enabled = true;
        }

        /// <summary>Brilho de carga em volta do chefe (escala 0 desliga).</summary>
        public void Aura(Transform dono, float escala, Color corAura)
        {
            auraAlvo = escala;
            if (escala <= 0.01f) return;

            aura.enabled = true;
            aura.color = new Color(corAura.r, corAura.g, corAura.b, 0.34f);
            if (dono != null) aura.transform.position = dono.position;
        }

        /// <summary>
        /// O golpe saiu: o indicador pisca em branco e se apaga depressa. E o
        /// quadro que fecha a leitura - aviso, impacto, fim.
        /// </summary>
        public void Disparar()
        {
            flash = 1f;
            restante = 0f;
        }

        public void Cancelar()
        {
            areaAtiva = false;
            arcoAtivo = false;
            flash = 0f;
            auraAlvo = 0f;
            EsconderTudo();
        }

        private void EsconderTudo()
        {
            if (areaFundo != null) areaFundo.enabled = false;
            if (areaBorda != null) areaBorda.enabled = false;
            if (arco != null) arco.enabled = false;
            if (aura != null) aura.enabled = false;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (restante > 0f) restante = Mathf.Max(0f, restante - dt);

            // 0 no inicio da carga, 1 no momento do golpe.
            float progresso = duracao > 0f ? 1f - (restante / duracao) : 1f;
            bool piscando = flash > 0f;
            if (piscando) flash = Mathf.Max(0f, flash - dt * 5f);

            AtualizarArea(progresso, piscando);
            AtualizarArco(progresso, piscando);
            AtualizarAura(dt);

            // Terminado o clarao, os indicadores saem de cena.
            if (!piscando && flash <= 0f && restante <= 0f && (areaAtiva || arcoAtivo))
            {
                areaAtiva = false;
                arcoAtivo = false;
                areaFundo.enabled = false;
                areaBorda.enabled = false;
                arco.enabled = false;
            }
        }

        private void AtualizarArea(float progresso, bool piscando)
        {
            if (!areaAtiva) return;

            // O disco cresce do centro ate encostar no contorno: o jogador ve o
            // tempo que ainda tem.
            float escala = Mathf.Lerp(0.15f, 1f, progresso) * areaRaio * 2f;
            areaFundo.transform.position = areaCentro;
            areaFundo.transform.localScale = new Vector3(escala, escala * 0.45f, 1f);

            Color c = piscando ? Color.Lerp(cor, Color.white, flash) : cor;
            float alphaFundo = piscando ? 0.75f * flash : Mathf.Lerp(0.18f, 0.42f, progresso);
            float alphaBorda = piscando ? 0.9f * flash : Mathf.Lerp(0.55f, 0.95f, progresso);

            // Pulso curto no fim da carga: os ultimos 25% piscam mais rapido.
            if (!piscando && progresso > 0.75f)
            {
                float pulso = Mathf.PingPong(Time.time * 10f, 1f) * 0.25f;
                alphaFundo += pulso;
                alphaBorda = Mathf.Min(1f, alphaBorda + pulso);
            }

            areaFundo.color = new Color(c.r, c.g, c.b, alphaFundo);
            areaBorda.color = new Color(c.r, c.g, c.b, alphaBorda);
        }

        private void AtualizarArco(float progresso, bool piscando)
        {
            if (!arcoAtivo || donoDoArco == null) return;

            // Faixa RENTE AO CHAO que sai do chefe para o lado do golpe, crescendo
            // ate o alcance total no instante do impacto. Fica no chao, e nao na
            // altura do peito, pelo mesmo motivo da marca do especial: encostada
            // no corpo do chefe (que desenha por cima dela) ela sumia.
            float comprimento = arcoAlcance * Mathf.Lerp(0.45f, 1f, progresso);
            const float altura = 0.8f;
            Vector3 baseP = donoDoArco.position;
            arco.transform.position = new Vector3(baseP.x + arcoDirecao * comprimento * 0.5f,
                                                  arcoY, baseP.z);
            arco.transform.localScale = new Vector3(comprimento, altura, 1f);

            Color c = piscando ? Color.Lerp(cor, Color.white, flash) : cor;
            float alpha = piscando ? 0.95f * flash : Mathf.Lerp(0.35f, 0.8f, progresso);
            if (!piscando && progresso > 0.7f) alpha += Mathf.PingPong(Time.time * 12f, 1f) * 0.2f;
            arco.color = new Color(c.r, c.g, c.b, alpha);
        }

        private void AtualizarAura(float dt)
        {
            if (aura == null || !aura.enabled) return;

            float atual = aura.transform.localScale.x;
            float nova = Mathf.MoveTowards(atual, auraAlvo, dt * 6f);
            aura.transform.localScale = new Vector3(nova, nova, 1f);
            if (auraAlvo <= 0.01f && nova <= 0.02f) aura.enabled = false;
        }
    }
}
