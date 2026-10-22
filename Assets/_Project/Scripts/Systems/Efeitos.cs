using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Efeitos passageiros feitos de um sprite so: brasas, penas, poeira,
    /// imagens residuais, colunas de luz, chao chamuscado, ondas de choque.
    ///
    /// Tudo aqui e a mesma coisa vista de angulos diferentes - um sprite que
    /// nasce, se move, muda de tamanho e de transparencia e some. Em vez de um
    /// ParticleSystem (que no URP 2D pede material e shader proprios, e quebra
    /// em silencio na build se o shader nao for incluido), e um componente de
    /// vinte linhas com os sprites gerados em codigo pelo RuntimeSprites -
    /// os mesmos que ja desenham as marcas de ataque do chefe.
    ///
    /// Os metodos estaticos sao o "vocabulario" visual da luta: o chefe pede
    /// Brasas, Penas, Residuo, Chamuscado... e nao precisa saber como cada um
    /// e feito.
    /// </summary>
    public class Efemero : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Vector3 velocidade;
        private float gravidade;
        private float arrasto;
        private float vida;
        private float tempo;
        private float alphaMaximo;
        private float fracaoDeEntrada;    // parte da vida gasta aparecendo
        private Vector3 escalaInicial;
        private Vector3 escalaFinal;
        private float giro;
        private Color cor;

        public static Efemero Criar(Sprite sprite, Vector3 posicao, Color cor, float vida, Vector3 escalaInicial,
                                    Vector3 escalaFinal, int ordem, Vector3 velocidade = default,
                                    float gravidade = 0f, float arrasto = 0f, float fracaoDeEntrada = 0.1f,
                                    float giro = 0f, float rotacao = 0f, bool espelhar = false)
        {
            if (sprite == null) return null;
            var go = new GameObject("Efeito");
            go.transform.position = posicao;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotacao);
            go.transform.localScale = escalaInicial;

            var e = go.AddComponent<Efemero>();
            e.sr = go.AddComponent<SpriteRenderer>();
            e.sr.sprite = sprite;
            e.sr.sortingOrder = ordem;
            e.sr.flipX = espelhar;
            e.cor = cor;
            e.sr.color = new Color(cor.r, cor.g, cor.b, 0f);
            e.velocidade = velocidade;
            e.gravidade = gravidade;
            e.arrasto = arrasto;
            e.vida = Mathf.Max(0.02f, vida);
            e.alphaMaximo = cor.a;
            e.fracaoDeEntrada = Mathf.Clamp01(fracaoDeEntrada);
            e.escalaInicial = escalaInicial;
            e.escalaFinal = escalaFinal;
            e.giro = giro;
            return e;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            tempo += dt;
            float p = tempo / vida;
            if (p >= 1f) { Destroy(gameObject); return; }

            velocidade.y -= gravidade * dt;
            if (arrasto > 0f) velocidade *= Mathf.Max(0f, 1f - arrasto * dt);
            transform.position += velocidade * dt;
            if (giro != 0f) transform.Rotate(0f, 0f, giro * dt);
            transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, p);

            float a = p < fracaoDeEntrada
                ? p / Mathf.Max(0.001f, fracaoDeEntrada)
                : 1f - (p - fracaoDeEntrada) / Mathf.Max(0.001f, 1f - fracaoDeEntrada);
            sr.color = new Color(cor.r, cor.g, cor.b, alphaMaximo * Mathf.Clamp01(a));
        }
    }

    /// <summary>O vocabulario visual: cada metodo e um efeito com nome.</summary>
    public static class Efeitos
    {
        private const int OrdemFrente = 22;   // na frente do chefe (3) e do jogador
        private const int OrdemChao = 1;      // rente ao chao, atras dos personagens

        /// <summary>
        /// Brasas: pontinhos de fogo que saltam e sobem, se apagando. Usadas no
        /// impacto do fogo, nas maos do mago enquanto conjura, no rastro da bola
        /// de fogo e no anjo em furia.
        /// </summary>
        public static void Brasas(Vector3 origem, int quantidade, Color cor, float forca = 3f,
                                  float subida = 1.5f, float tamanho = 0.12f, float vida = 0.7f, int ordem = OrdemFrente)
        {
            for (int i = 0; i < quantidade; i++)
            {
                Vector2 dir = Random.insideUnitCircle;
                var v = new Vector3(dir.x * forca, Mathf.Abs(dir.y) * forca * 0.6f + subida, 0f) * Random.Range(0.4f, 1f);
                float t = tamanho * Random.Range(0.6f, 1.3f);
                Efemero.Criar(RuntimeSprites.Disco(), origem, cor, vida * Random.Range(0.6f, 1.2f),
                              new Vector3(t, t, 1f), new Vector3(t * 0.2f, t * 0.2f, 1f), ordem, v,
                              gravidade: -0.6f, arrasto: 1.5f, fracaoDeEntrada: 0.05f);
            }
        }

        /// <summary>
        /// Penas: retalhos claros que flutuam caindo e girando. O anjo solta
        /// penas o tempo todo, e uma rajada delas na investida e no golpe de asa.
        /// </summary>
        public static void Penas(Vector3 origem, int quantidade, float forca = 2f, float espalhamento = 0.8f,
                                 Vector2? direcao = null, Color? cor = null)
        {
            Color c = cor ?? new Color(0.95f, 0.95f, 1f, 0.9f);
            for (int i = 0; i < quantidade; i++)
            {
                Vector2 d = (direcao ?? Vector2.zero) + Random.insideUnitCircle * espalhamento;
                var pos = origem + (Vector3)(Random.insideUnitCircle * 0.5f);
                float largura = Random.Range(0.12f, 0.2f);
                Efemero.Criar(RuntimeSprites.Disco(), pos, c, Random.Range(0.9f, 1.6f),
                              new Vector3(largura, largura * 0.35f, 1f), new Vector3(largura * 0.7f, largura * 0.25f, 1f),
                              OrdemFrente, (Vector3)(d * forca), gravidade: 0.9f, arrasto: 2.5f,
                              fracaoDeEntrada: 0.05f, giro: Random.Range(-220f, 220f), rotacao: Random.Range(0f, 360f));
            }
        }

        /// <summary>Poeira levantada do chao (chegada da investida, aparicao do mago).</summary>
        public static void Poeira(Vector3 chao, int quantidade, float largura = 1.2f, Color? cor = null)
        {
            Color c = cor ?? new Color(0.55f, 0.48f, 0.62f, 0.55f);
            for (int i = 0; i < quantidade; i++)
            {
                var pos = chao + new Vector3(Random.Range(-largura, largura), 0.05f, 0f);
                float t = Random.Range(0.35f, 0.7f);
                var v = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.3f, 1f), 0f);
                Efemero.Criar(RuntimeSprites.Brilho(), pos, c, Random.Range(0.5f, 0.9f),
                              new Vector3(t, t * 0.6f, 1f), new Vector3(t * 2.2f, t * 1.4f, 1f), OrdemChao + 3, v,
                              arrasto: 2f, fracaoDeEntrada: 0.15f);
            }
        }

        /// <summary>
        /// Imagem residual: uma copia do sprite atual de alguem, no lugar em que
        /// ele estava, que se apaga. Em sequencia vira o rastro de um movimento
        /// rapido - e o que faz a investida do anjo LER como velocidade.
        /// </summary>
        public static void Residuo(SpriteRenderer origem, Color cor, float vida = 0.35f)
        {
            if (origem == null || origem.sprite == null) return;
            var t = origem.transform;
            Efemero.Criar(origem.sprite, t.position, cor, vida, t.lossyScale, t.lossyScale * 1.04f,
                          origem.sortingOrder - 1, fracaoDeEntrada: 0.01f);
        }

        /// <summary>
        /// Chao chamuscado: mancha escura que fica alguns segundos onde o fogo
        /// caiu. Lembra o jogador de onde o golpe acertou e da peso ao impacto.
        /// </summary>
        public static void Chamuscado(Vector3 ponto, float raio)
        {
            Efemero.Criar(RuntimeSprites.Brilho(), ponto, new Color(0.08f, 0.02f, 0.04f, 0.75f), 2.8f,
                          new Vector3(raio * 2.2f, raio * 0.6f, 1f), new Vector3(raio * 2.4f, raio * 0.65f, 1f),
                          OrdemChao, fracaoDeEntrada: 0.02f);
            // Um brilho de brasa no meio, que esfria primeiro.
            Efemero.Criar(RuntimeSprites.Brilho(), ponto, new Color(1f, 0.45f, 0.1f, 0.6f), 1.1f,
                          new Vector3(raio * 1.4f, raio * 0.35f, 1f), new Vector3(raio * 0.6f, raio * 0.15f, 1f),
                          OrdemChao + 1, fracaoDeEntrada: 0.02f);
        }

        /// <summary>Onda de choque: anel achatado que se expande e some.</summary>
        public static void Onda(Vector3 ponto, float raioFinal, Color cor, float vida = 0.45f)
        {
            Efemero.Criar(RuntimeSprites.Anel(), ponto, cor, vida,
                          new Vector3(0.3f, 0.12f, 1f), new Vector3(raioFinal * 2f, raioFinal * 0.8f, 1f),
                          OrdemChao + 2, fracaoDeEntrada: 0.02f);
        }

        /// <summary>Clarao redondo de luz, curto (estouro, aparicao).</summary>
        public static void Lampejo(Vector3 ponto, float tamanho, Color cor, float vida = 0.35f, int ordem = OrdemFrente)
        {
            Efemero.Criar(RuntimeSprites.Brilho(), ponto, cor, vida,
                          new Vector3(tamanho * 0.4f, tamanho * 0.4f, 1f), new Vector3(tamanho, tamanho, 1f),
                          ordem, fracaoDeEntrada: 0.08f);
        }

        /// <summary>
        /// Raios de luz saindo de um ponto (a transformacao). Faixas finas que
        /// crescem para fora e se apagam, cada uma num angulo.
        /// </summary>
        public static void Raios(Vector3 centro, int quantidade, float comprimento, Color cor, float vida = 0.8f)
        {
            for (int i = 0; i < quantidade; i++)
            {
                // Cada faixa atravessa o centro (dois raios por faixa): meia volta basta.
                float ang = i * 180f / quantidade + Random.Range(-6f, 6f);
                Efemero.Criar(RuntimeSprites.Brilho(), centro, cor, vida * Random.Range(0.8f, 1.2f),
                              new Vector3(0.6f, 0.15f, 1f), new Vector3(comprimento * Random.Range(0.7f, 1.1f), 0.35f, 1f),
                              OrdemFrente - 1, fracaoDeEntrada: 0.15f, rotacao: ang);
            }
        }
    }
}
