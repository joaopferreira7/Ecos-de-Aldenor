using System.Collections.Generic;
using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Movimento de fundo das fases: nevoa rasteira e almas soltas no ar.
    ///
    /// Fora o Ren e os inimigos, nada se mexia na tela - o cenario era uma
    /// pintura parada. Duas faixas de nevoa (uma atras, outra na frente do
    /// chao) deslizam em velocidades diferentes, e pontos de luz sobem devagar
    /// balancando, como almas que ainda vagam pelo cemiterio.
    ///
    /// Tudo e gerado em codigo (a textura da nevoa e o brilho das almas), em
    /// volta da camera: nao importa o tamanho da fase, e nao ha arte nova.
    /// </summary>
    public class AmbienteVivo : MonoBehaviour
    {
        [Header("Nevoa")]
        [SerializeField] private Color corDaNevoa = new Color(0.62f, 0.56f, 0.82f, 0.30f);
        [Tooltip("Altura (mundo) da base da nevoa - em geral o topo do chao.")]
        [SerializeField] private float baseDaNevoa = -0.9f;
        [SerializeField] private float alturaDaNevoa = 1.6f;
        [SerializeField] private float velocidadeDaNevoa = 0.35f;

        [Header("Almas no ar")]
        [SerializeField] private Color corDasAlmas = new Color(0.62f, 0.86f, 1f, 0.85f);
        [SerializeField] private int quantidade = 16;

        [Tooltip("Material sem luz (Sprite-Unlit-Default): a nevoa e as almas nao devem escurecer com as luzes 2D.")]
        [SerializeField] private Material material;

        private class Faixa { public SpriteRenderer sr; public float velocidade; public float largura; public float parallax; }
        private class Alma { public Transform t; public SpriteRenderer sr; public float sobe, fase, balanco, alpha, escala; }

        private readonly List<Faixa> faixas = new List<Faixa>();
        private readonly List<Alma> almas = new List<Alma>();
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            if (cam == null) { enabled = false; return; }

            var nevoa = SpriteDaNevoa();
            // Atras dos personagens e mais densa; na frente, bem leve, so para
            // os pes afundarem um pouco nela.
            faixas.Add(NovaFaixa("NevoaFundo", nevoa, -4, corDaNevoa, velocidadeDaNevoa, 0.85f, 0f));
            faixas.Add(NovaFaixa("NevoaFrente", nevoa, 30, new Color(corDaNevoa.r, corDaNevoa.g, corDaNevoa.b, corDaNevoa.a * 0.35f),
                                 -velocidadeDaNevoa * 1.6f, 1.15f, -0.08f));

            var brilho = RuntimeSprites.Brilho();
            for (int i = 0; i < quantidade; i++)
            {
                var go = new GameObject("Alma_" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = brilho;
                sr.sortingOrder = -3;
                if (material != null) sr.sharedMaterial = material;
                var a = new Alma
                {
                    t = go.transform, sr = sr,
                    sobe = Random.Range(0.18f, 0.45f),
                    fase = Random.Range(0f, 10f),
                    balanco = Random.Range(0.15f, 0.4f),
                    alpha = Random.Range(0.35f, 1f),
                    escala = Random.Range(0.16f, 0.3f),
                };
                go.transform.localScale = Vector3.one * a.escala;
                Reposicionar(a, true);
                almas.Add(a);
            }
        }

        private Faixa NovaFaixa(string nome, Sprite s, int ordem, Color cor, float vel, float parallax, float desceY)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Tiled;
            float largura = s.bounds.size.x;
            // Ladrilha so na horizontal; a altura vem da escala (ladrilhar na
            // vertical repetiria a base densa no alto da faixa).
            sr.size = new Vector2(largura * 8f, 1f);
            go.transform.localScale = new Vector3(1f, alturaDaNevoa, 1f);
            sr.color = cor;
            sr.sortingOrder = ordem;
            if (material != null) sr.sharedMaterial = material;
            go.transform.position = new Vector3(0f, baseDaNevoa + desceY + alturaDaNevoa * 0.5f, 0f);
            return new Faixa { sr = sr, velocidade = vel, largura = largura, parallax = parallax };
        }

        private void LateUpdate()
        {
            float t = Time.time;
            float cx = cam.transform.position.x;

            // A faixa segue a camera, mas a textura desliza por baixo: o resto
            // da divisao pela largura do ladrilho esconde o salto.
            foreach (var f in faixas)
            {
                float desloc = t * f.velocidade + cx * (1f - f.parallax);
                float x = cx - Mathf.Repeat(desloc, f.largura);
                var p = f.sr.transform.position;
                f.sr.transform.position = new Vector3(x, p.y, p.z);
            }

            float meiaAltura = cam.orthographicSize;
            float meiaLargura = meiaAltura * cam.aspect;
            Vector3 c = cam.transform.position;
            foreach (var a in almas)
            {
                var p = a.t.position;
                p.y += a.sobe * Time.deltaTime;
                p.x += Mathf.Sin(t * 0.9f + a.fase) * a.balanco * Time.deltaTime;

                // Saiu da tela por cima, ou ficou para tras com a camera andando.
                if (p.y > c.y + meiaAltura + 0.5f || Mathf.Abs(p.x - c.x) > meiaLargura + 1f)
                {
                    a.t.position = p;
                    Reposicionar(a, false);
                    continue;
                }
                a.t.position = p;

                // Cintila e some perto do alto, em vez de sumir de uma vez.
                float topo = Mathf.InverseLerp(c.y + meiaAltura + 0.5f, c.y + meiaAltura - 2f, p.y);
                float brilho = 0.65f + 0.35f * Mathf.Sin(t * 3f + a.fase * 2f);
                a.sr.color = new Color(corDasAlmas.r, corDasAlmas.g, corDasAlmas.b, corDasAlmas.a * a.alpha * brilho * topo);
            }
        }

        private void Reposicionar(Alma a, bool espalhar)
        {
            float meiaAltura = cam.orthographicSize;
            float meiaLargura = meiaAltura * cam.aspect;
            Vector3 c = cam.transform.position;
            float y = espalhar ? Random.Range(c.y - meiaAltura, c.y + meiaAltura)
                               : c.y - meiaAltura - Random.Range(0.2f, 1.2f);
            a.t.position = new Vector3(c.x + Random.Range(-meiaLargura - 0.5f, meiaLargura + 0.5f), y, 0f);
        }

        /// <summary>
        /// Textura de nevoa: ruido suave em poucos pixels (filtro de ponto,
        /// como o resto da arte), denso embaixo e sumindo para cima. Repete nas
        /// bordas laterais para ladrilhar sem emenda.
        /// </summary>
        private static Sprite SpriteDaNevoa()
        {
            const int w = 96, h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w * Mathf.PI * 2f;   // periodico na largura
                    float ondas = 0.5f
                                  + 0.25f * Mathf.Sin(u * 2f + v * 3f)
                                  + 0.15f * Mathf.Sin(u * 5f - v * 5f + 1.3f)
                                  + 0.10f * Mathf.Sin(u * 9f + 2.1f);
                    float topo = Mathf.Clamp01(1f - v * 1.15f);            // some para cima
                    float borda = Mathf.SmoothStep(0f, 1f, v * 6f);          // e um pouco na base
                    float a = Mathf.Clamp01(ondas) * topo * topo * borda;
                    a = Mathf.Round(a * 5f) / 5f;                             // degraus, a cara pixel
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            // 24 px de altura viram 1 unidade; o SpriteRenderer estica para a altura pedida.
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h, 0, SpriteMeshType.FullRect);
            s.name = "NevoaGerada";
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
