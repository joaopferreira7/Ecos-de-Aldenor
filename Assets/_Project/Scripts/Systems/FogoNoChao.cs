using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Um trecho de chao em chamas que dura pouco e queima quem pisa nele.
    ///
    /// E o rastro do Carnical em Chamas: a disparada dele deixa uma fileira
    /// destes, e o jogador aprende que a ameaca nao e so o corpo que passa -
    /// o caminho por onde ele passou tambem queima por um instante. Pular por
    /// cima dele, e nao so sair do caminho, e a resposta.
    ///
    /// A arte e toda gerada (brilho + brasas + chao chamuscado), no mesmo
    /// codigo de cores do fogo do chefe.
    /// </summary>
    public class FogoNoChao : MonoBehaviour
    {
        private float vida;
        private float restante;
        private int dano;
        private LayerMask alvo;
        private Vector2 tamanho;
        private SpriteRenderer chama;
        private float proximaBrasa;
        private float fase;

        private static readonly Color CorDaChama = new Color(1f, 0.5f, 0.12f, 0.85f);

        public static FogoNoChao Criar(Vector3 pesNoChao, float vida, int dano, LayerMask alvo, float largura = 0.7f)
        {
            var go = new GameObject("FogoNoChao");
            go.transform.position = pesNoChao;

            var f = go.AddComponent<FogoNoChao>();
            f.vida = f.restante = Mathf.Max(0.1f, vida);
            f.dano = dano;
            f.alvo = alvo;
            f.tamanho = new Vector2(largura, 0.5f);
            f.fase = Random.value * 10f;

            f.chama = go.AddComponent<SpriteRenderer>();
            f.chama.sprite = RuntimeSprites.Brilho();
            f.chama.color = CorDaChama;
            f.chama.sortingOrder = 4;
            go.transform.localScale = new Vector3(largura * 1.4f, 0.9f, 1f);

            Efeitos.Chamuscado(pesNoChao, largura * 0.6f);
            return f;
        }

        private void Update()
        {
            restante -= Time.deltaTime;
            if (restante <= 0f) { Destroy(gameObject); return; }

            // A chama tremula e encolhe no fim: o jogador ve quando ela vai apagar.
            float k = restante / vida;
            float tremula = 0.85f + 0.15f * Mathf.Sin(Time.time * 18f + fase);
            transform.localScale = new Vector3(tamanho.x * 1.4f, 0.9f * Mathf.Lerp(0.35f, 1f, k) * tremula, 1f);
            chama.color = new Color(CorDaChama.r, CorDaChama.g, CorDaChama.b, CorDaChama.a * Mathf.Clamp01(k * 1.5f));

            proximaBrasa -= Time.deltaTime;
            if (proximaBrasa <= 0f)
            {
                proximaBrasa = 0.12f;
                Efeitos.Brasas(transform.position + new Vector3(0f, 0.15f, 0f), 1, new Color(1f, 0.6f, 0.2f, 1f),
                               forca: 0.4f, subida: 1.2f, tamanho: 0.12f, vida: 0.5f);
            }

            // Queima por contato. A invulnerabilidade do jogador ja impede que
            // o dano se repita a cada quadro.
            var hit = Physics2D.OverlapBox(transform.position + new Vector3(0f, tamanho.y * 0.5f, 0f), tamanho, 0f, alvo);
            if (hit != null)
            {
                var vidaDoAlvo = hit.GetComponent<HealthSystem>();
                if (vidaDoAlvo != null) vidaDoAlvo.TakeDamage(dano, transform.position);
            }
        }
    }
}
