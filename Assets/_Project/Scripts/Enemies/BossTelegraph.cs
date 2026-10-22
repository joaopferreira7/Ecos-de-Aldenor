using System.Collections.Generic;
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
    ///   - MARCAS: discos no chao com contorno, nos pontos exatos onde os golpes
    ///     vao cair. O preenchimento cresce do centro para a borda; quando
    ///     encosta no contorno, o golpe acontece. Podem existir varias ao mesmo
    ///     tempo, cada uma com o seu relogio - e o que torna legiveis a salva de
    ///     tres bolas de fogo e a chuva do anjo: o jogador ve a ORDEM em que vao
    ///     cair pelo quanto cada disco ja encheu.
    ///   - FAIXA: retangulo rente ao chao mostrando alcance e direcao. Serve ao
    ///     golpe curto (acompanha o chefe) e a investida do anjo (travada no
    ///     chao, do ponto de partida ate onde ele vai parar).
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

        /// <summary>Uma marca no chao, com o proprio relogio.</summary>
        private class Marca
        {
            public SpriteRenderer fundo;     // disco que cresce
            public SpriteRenderer borda;     // contorno fixo
            public SpriteRenderer pilar;     // coluna de luz descendo do alto (opcional)
            public bool comPilar;
            public Vector3 centro;
            public float raio;
            public float duracao;
            public float restante;
            public float flash;              // 0..1, clarao do impacto
            public Color cor;
            public bool ativa;
        }

        private readonly List<Marca> marcas = new List<Marca>();

        private SpriteRenderer faixa;        // golpe curto / investida
        private SpriteRenderer aura;         // brilho de carga em volta do chefe

        private Transform donoDaFaixa;       // nulo = faixa travada no chao
        private Vector3 origemFixa;
        private float faixaAlcance;
        private float faixaY;
        private float faixaAltura = 0.8f;
        private float faixaDirecao = 1f;
        private float faixaDuracao;
        private float faixaRestante;
        private float faixaFlash;
        private Color faixaCor = Color.red;
        private bool faixaAtiva;
        private bool faixaCresce = true;     // a investida mostra o percurso inteiro desde o inicio

        private float auraAlvo;              // escala desejada da aura

        // CIRCULO DE CONJURACAO: anel rente ao chao sob o mago, com runas
        // (pontos) girando em volta e um anel interno que pulsa. So existe
        // enquanto alguem pede a cada quadro - quando o pedido para, ele some.
        private const int Runas = 8;
        private SpriteRenderer circuloExterno;
        private SpriteRenderer circuloInterno;
        private readonly SpriteRenderer[] runas = new SpriteRenderer[Runas];
        private Vector3 circuloCentro;
        private float circuloRaio = 1f;
        private float circuloForca;          // 0..1, sobe quando pedido e cai quando nao
        private float circuloAlvo;
        private Color circuloCor = Color.red;
        private float giroDasRunas;

        private void Awake()
        {
            faixa = Criar("TelegrafoFaixa", RuntimeSprites.Solido(), OrdemNoChao);
            aura = Criar("TelegrafoAura", RuntimeSprites.Brilho(), OrdemNoChao - 1);
            circuloExterno = Criar("CirculoExterno", RuntimeSprites.Anel(), OrdemNoChao);
            circuloInterno = Criar("CirculoInterno", RuntimeSprites.Anel(), OrdemNoChao);
            for (int i = 0; i < Runas; i++) runas[i] = Criar("Runa", RuntimeSprites.Disco(), OrdemNoChao + 1);
            NovaMarca();   // a primeira ja existe: o especial simples nao paga a criacao no meio da luta
        }

        private void OnDestroy()
        {
            // Os indicadores nao sao filhos do chefe, entao a morte dele nao os
            // leva junto: quem cria, limpa.
            foreach (var m in marcas) { Remover(m.fundo); Remover(m.borda); Remover(m.pilar); }
            Remover(circuloExterno); Remover(circuloInterno);
            foreach (var r in runas) Remover(r);
            Remover(faixa);
            Remover(aura);
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

        private Marca NovaMarca()
        {
            var m = new Marca
            {
                fundo = Criar("TelegrafoArea", RuntimeSprites.Disco(), OrdemNoChao),
                borda = Criar("TelegrafoBorda", RuntimeSprites.Anel(), OrdemNoChao + 1),
                pilar = Criar("TelegrafoPilar", RuntimeSprites.Brilho(), OrdemNoChao),
            };
            marcas.Add(m);
            return m;
        }

        /// <summary>
        /// Marca no chao onde um golpe vai cair. O ponto e travado agora: se o
        /// jogador sair da marca a tempo, o golpe erra - e isso que torna o
        /// ataque justo em vez de inevitavel. Quando o disco encosta no
        /// contorno, a marca pisca em branco sozinha: e o instante do impacto.
        /// </summary>
        /// <param name="pilar">
        /// Coluna de luz descendo do alto ate a marca: diz que o golpe vem DE
        /// CIMA (a chuva do anjo), e nao arremessado pelo chefe.
        /// </param>
        public void MostrarArea(Vector3 centro, float raio, float tempoDeCarga, Color corAviso, bool pilar = false)
        {
            Marca m = marcas.Find(x => !x.ativa) ?? NovaMarca();
            m.comPilar = pilar;
            m.pilar.enabled = pilar;

            m.centro = centro;
            m.raio = raio;
            m.duracao = Mathf.Max(0.01f, tempoDeCarga);
            m.restante = m.duracao;
            m.cor = corAviso;
            m.flash = 0f;
            m.ativa = true;

            m.fundo.enabled = true;
            m.borda.enabled = true;
            m.fundo.transform.position = centro;
            m.borda.transform.position = centro;
            // A altura e achatada: e uma marca no chao vista de lado, nao um circulo.
            m.borda.transform.localScale = new Vector3(raio * 2f, raio * 0.9f, 1f);
        }

        /// <summary>
        /// Faixa a frente do chefe mostrando alcance e direcao do golpe corpo a
        /// corpo. Acompanha o chefe, mas a direcao fica travada em quem chamou.
        /// </summary>
        public void MostrarArco(Transform dono, float alcance, float direcao, float alturaDoChao,
                                float tempoDeCarga, Color corAviso)
        {
            PrepararFaixa(alcance, direcao, alturaDoChao, 0.8f, tempoDeCarga, corAviso);
            donoDaFaixa = dono;
            faixaCresce = true;
        }

        /// <summary>
        /// Faixa TRAVADA no chao, do ponto de partida ate onde o golpe vai.
        /// E o aviso da investida do anjo: o caminho inteiro aparece de uma vez
        /// (nao cresce), porque a pergunta que o jogador precisa responder nao e
        /// "ate onde" e sim "quando" - e o "quando" e dado pelo brilho, que
        /// acelera no fim da carga.
        /// </summary>
        public void MostrarFaixa(Vector3 origem, float comprimento, float direcao, float alturaDoChao,
                                 float alturaDaFaixa, float tempoDeCarga, Color corAviso)
        {
            PrepararFaixa(comprimento, direcao, alturaDoChao, alturaDaFaixa, tempoDeCarga, corAviso);
            donoDaFaixa = null;
            origemFixa = origem;
            faixaCresce = false;
        }

        private void PrepararFaixa(float alcance, float direcao, float alturaDoChao, float altura,
                                   float tempoDeCarga, Color corAviso)
        {
            faixaAlcance = alcance;
            faixaY = alturaDoChao;
            faixaAltura = altura;
            faixaDirecao = direcao >= 0f ? 1f : -1f;
            faixaDuracao = Mathf.Max(0.01f, tempoDeCarga);
            faixaRestante = faixaDuracao;
            faixaCor = corAviso;
            faixaFlash = 0f;
            faixaAtiva = true;
            faixa.enabled = true;
        }

        /// <summary>Brilho de carga em volta do chefe (escala 0 desliga).</summary>
        public void Aura(Transform dono, float escala, Color corAura, float alfa = 0.34f)
        {
            auraAlvo = escala;
            if (escala <= 0.01f) return;

            aura.enabled = true;
            aura.color = new Color(corAura.r, corAura.g, corAura.b, alfa);
            if (dono != null) aura.transform.position = dono.position;
        }

        /// <summary>
        /// O golpe saiu: a faixa e as marcas cuja carga ja terminou piscam em
        /// branco e se apagam depressa. E o quadro que fecha a leitura - aviso,
        /// impacto, fim. Marcas que ainda estao carregando (a segunda e a
        /// terceira de uma salva) seguem o proprio relogio.
        /// </summary>
        public void Disparar()
        {
            if (faixaAtiva) { faixaFlash = 1f; faixaRestante = 0f; }
            foreach (var m in marcas)
                if (m.ativa && m.restante <= 0.05f) { m.flash = 1f; m.restante = 0f; }
        }

        public void Cancelar()
        {
            foreach (var m in marcas)
            {
                m.ativa = false;
                m.flash = 0f;
                m.fundo.enabled = false;
                m.borda.enabled = false;
                m.pilar.enabled = false;
            }
            circuloAlvo = 0f;
            circuloForca = 0f;
            EsconderCirculo();
            faixaAtiva = false;
            faixaFlash = 0f;
            faixa.enabled = false;
            auraAlvo = 0f;
            aura.enabled = false;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (var m in marcas) AtualizarMarca(m, dt);
            AtualizarFaixa(dt);
            AtualizarAura(dt);
            AtualizarCirculo(dt);
        }

        /// <summary>
        /// Pede o circulo de conjuracao NESTE quadro. Chamado a cada quadro
        /// enquanto o mago conjura; parar de chamar faz o circulo se apagar.
        /// </summary>
        public void Circulo(Vector3 centroNoChao, float raio, Color cor)
        {
            circuloCentro = centroNoChao;
            circuloRaio = raio;
            circuloCor = cor;
            circuloAlvo = 1f;
        }

        private void AtualizarCirculo(float dt)
        {
            circuloForca = Mathf.MoveTowards(circuloForca, circuloAlvo, dt * (circuloAlvo > 0f ? 3f : 2f));
            circuloAlvo = 0f;   // o pedido vale um quadro so
            if (circuloForca <= 0.01f) { EsconderCirculo(); return; }

            giroDasRunas += dt * 90f;
            float f = circuloForca;
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);

            // Visto de lado, um circulo no chao e uma elipse achatada.
            circuloExterno.enabled = true;
            circuloExterno.transform.position = circuloCentro;
            float r = circuloRaio * Mathf.Lerp(0.6f, 1f, f);
            circuloExterno.transform.localScale = new Vector3(r * 2f, r * 0.55f, 1f);
            circuloExterno.color = new Color(circuloCor.r, circuloCor.g, circuloCor.b, 0.85f * f);

            circuloInterno.enabled = true;
            circuloInterno.transform.position = circuloCentro;
            float ri = r * Mathf.Lerp(0.45f, 0.65f, pulso);
            circuloInterno.transform.localScale = new Vector3(ri * 2f, ri * 0.55f, 1f);
            circuloInterno.color = new Color(1f, 0.85f, 0.6f, 0.6f * f);

            for (int i = 0; i < Runas; i++)
            {
                float ang = (giroDasRunas + i * 360f / Runas) * Mathf.Deg2Rad;
                var runa = runas[i];
                runa.enabled = true;
                // As runas da metade "da frente" da elipse ficam maiores e mais claras.
                float frente = 0.5f - 0.5f * Mathf.Sin(ang);
                runa.transform.position = circuloCentro + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r * 0.275f, 0f);
                float t = Mathf.Lerp(0.09f, 0.16f, frente) * f;
                runa.transform.localScale = new Vector3(t, t, 1f);
                runa.color = Color.Lerp(circuloCor, Color.white, 0.4f * frente) * new Color(1f, 1f, 1f, f);
                runa.sortingOrder = frente > 0.5f ? OrdemNoChao + 2 : OrdemNoChao - 1;
            }
        }

        private void EsconderCirculo()
        {
            if (circuloExterno != null) circuloExterno.enabled = false;
            if (circuloInterno != null) circuloInterno.enabled = false;
            foreach (var r in runas) if (r != null) r.enabled = false;
        }

        private void AtualizarMarca(Marca m, float dt)
        {
            if (!m.ativa) return;

            bool estavaCarregando = m.restante > 0f;
            if (m.restante > 0f) m.restante = Mathf.Max(0f, m.restante - dt);
            // Acabou a carga: e o impacto. A marca pisca sozinha, para que uma
            // salva de varias marcas nao dependa de quem a lancou lembrar de
            // disparar cada uma.
            if (estavaCarregando && m.restante <= 0f && m.flash <= 0f) m.flash = 1f;

            bool piscando = m.flash > 0f;
            if (piscando) m.flash = Mathf.Max(0f, m.flash - dt * 5f);

            if (!piscando && m.restante <= 0f)
            {
                m.ativa = false;
                m.fundo.enabled = false;
                m.borda.enabled = false;
                m.pilar.enabled = false;
                return;
            }

            // 0 no inicio da carga, 1 no momento do golpe.
            float progresso = 1f - (m.restante / m.duracao);

            // O disco cresce do centro ate encostar no contorno: o jogador ve o
            // tempo que ainda tem.
            float escala = Mathf.Lerp(0.15f, 1f, progresso) * m.raio * 2f;
            m.fundo.transform.position = m.centro;
            m.fundo.transform.localScale = new Vector3(escala, escala * 0.45f, 1f);

            Color c = piscando ? Color.Lerp(m.cor, Color.white, m.flash) : m.cor;
            float alphaFundo = piscando ? 0.75f * m.flash : Mathf.Lerp(0.18f, 0.42f, progresso);
            float alphaBorda = piscando ? 0.9f * m.flash : Mathf.Lerp(0.55f, 0.95f, progresso);

            // Pulso curto no fim da carga: os ultimos 25% piscam mais rapido.
            if (!piscando && progresso > 0.75f)
            {
                float pulso = Mathf.PingPong(Time.time * 10f, 1f) * 0.25f;
                alphaFundo += pulso;
                alphaBorda = Mathf.Min(1f, alphaBorda + pulso);
            }

            m.fundo.color = new Color(c.r, c.g, c.b, alphaFundo);
            m.borda.color = new Color(c.r, c.g, c.b, alphaBorda);

            if (m.comPilar)
            {
                // Coluna de luz descendo do alto: estreita e fraca no comeco,
                // larga e forte quando o golpe esta para cair; no impacto, um
                // clarao branco que ocupa a coluna toda.
                const float altura = 10f;
                float largura = m.raio * Mathf.Lerp(0.5f, 1.1f, progresso);
                m.pilar.transform.position = m.centro + new Vector3(0f, altura * 0.45f, 0f);
                m.pilar.transform.localScale = new Vector3(largura, altura, 1f);
                Color cp = piscando ? Color.white : Color.Lerp(new Color(1f, 0.9f, 0.6f), m.cor, 0.35f);
                float ap = piscando ? 0.75f * m.flash : Mathf.Lerp(0.06f, 0.32f, progresso);
                m.pilar.color = new Color(cp.r, cp.g, cp.b, ap);
            }
        }

        private void AtualizarFaixa(float dt)
        {
            if (!faixaAtiva) return;

            if (faixaRestante > 0f) faixaRestante = Mathf.Max(0f, faixaRestante - dt);
            bool piscando = faixaFlash > 0f;
            if (piscando) faixaFlash = Mathf.Max(0f, faixaFlash - dt * 5f);

            if (!piscando && faixaRestante <= 0f)
            {
                faixaAtiva = false;
                faixa.enabled = false;
                return;
            }

            float progresso = 1f - (faixaRestante / faixaDuracao);

            // Faixa RENTE AO CHAO que sai do chefe para o lado do golpe. Fica no
            // chao, e nao na altura do peito, pelo mesmo motivo das marcas:
            // encostada no corpo do chefe (que desenha por cima dela) ela sumia.
            float comprimento = faixaCresce ? faixaAlcance * Mathf.Lerp(0.45f, 1f, progresso) : faixaAlcance;
            Vector3 baseP = donoDaFaixa != null ? donoDaFaixa.position : origemFixa;
            faixa.transform.position = new Vector3(baseP.x + faixaDirecao * comprimento * 0.5f, faixaY, baseP.z);
            faixa.transform.localScale = new Vector3(comprimento, faixaAltura, 1f);

            // A faixa curta (golpe de perto) pode ser forte: ela fica a frente do
            // chefe. A longa (investida) atravessa a arena por baixo do Ren, e
            // por isso e mais transparente - o aviso nao pode esconder o jogador.
            float maximo = faixaCresce ? 0.8f : 0.45f;
            Color c = piscando ? Color.Lerp(faixaCor, Color.white, faixaFlash) : faixaCor;
            float alpha = piscando ? maximo * faixaFlash : Mathf.Lerp(maximo * 0.4f, maximo, progresso);
            if (!piscando && progresso > 0.7f) alpha += Mathf.PingPong(Time.time * 12f, 1f) * (faixaCresce ? 0.2f : 0.15f);
            faixa.color = new Color(c.r, c.g, c.b, alpha);
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
