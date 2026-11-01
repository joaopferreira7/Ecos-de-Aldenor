using UnityEngine;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Core;
using EcosDeAldenor.Enemies;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// BRASEIRO do Claustro (Fase 4): uma armadilha, nao um inimigo.
    ///
    /// Quando o Ren passa por perto, ele marca o chao onde o Ren esta com a
    /// MARCA VERMELHA e, um segundo depois, uma bola de fogo cai ali. E o mesmo
    /// golpe da Vigilia, com o mesmo indicador (BossTelegraph), o mesmo projetil
    /// e as mesmas cores - de proposito: a fase ensina a ler a marca vermelha
    /// antes de o chefe cobrar essa leitura.
    ///
    /// O ponto e travado no inicio do aviso. Quem sai da marca, escapa.
    /// </summary>
    public class Braseiro : MonoBehaviour
    {
        [SerializeField] private float alcance = 5f;
        [SerializeField] private float intervalo = 2.6f;
        [SerializeField] private float aviso = 1f;
        [SerializeField] private float raio = 0.85f;
        [SerializeField] private int dano = 1;
        [SerializeField] private LayerMask camadaDoJogador;
        [SerializeField] private LayerMask camadaDoChao;
        [SerializeField] private Sprite[] framesDaBola;
        [SerializeField] private Sprite[] framesDoImpacto;
        [SerializeField] private AudioClip lancarSfx;
        [SerializeField] private AudioClip impactoSfx;
        [SerializeField] private float escala = 1f;

        private static readonly Color CorDoFogo = new Color(1f, 0.3f, 0.25f);

        private BossTelegraph telegrafo;
        private Transform jogador;
        private SpriteRenderer chama;
        private Light2D luz;
        private float proximo;
        private float impactoEm = -1f;
        private Vector3 ponto;

        private void Awake()
        {
            aviso = DifficultySettings.TelegrafoDoChefe(aviso);
            dano = DifficultySettings.DanoDeInimigo(dano);
            telegrafo = gameObject.AddComponent<BossTelegraph>();

            var corpo = GetComponent<SpriteRenderer>();
            if (corpo == null) corpo = gameObject.AddComponent<SpriteRenderer>();
            corpo.sprite = ArteGerada.Braseiro();
            corpo.sortingOrder = 2;
            transform.localScale = new Vector3(escala, escala, 1f);

            var go = new GameObject("Chama");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 18f / 24f, 0f);   // o carvao, a 18 px da base
            chama = go.AddComponent<SpriteRenderer>();
            chama.sprite = ArteGerada.Chama();
            chama.sortingOrder = 3;

            luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = 3f;
            luz.color = new Color(1f, 0.5f, 0.25f, 1f);
            luz.intensity = 0.8f;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jogador = p.transform;
            proximo = Time.time + 1f;
        }

        private void Update()
        {
            AnimarChama();

            if (impactoEm >= 0f)
            {
                if (Time.time >= impactoEm) Impacto();
                return;
            }

            if (jogador == null || Time.time < proximo) return;
            var d = jogador.position - transform.position;
            if (Mathf.Abs(d.x) > alcance || Mathf.Abs(d.y) > 4f) return;

            Lancar();
        }

        private void Lancar()
        {
            // O ponto e travado AGORA, no chao sob o Ren.
            var chao = Physics2D.Raycast(jogador.position, Vector2.down, 6f, camadaDoChao);
            if (chao.collider == null) { proximo = Time.time + 0.5f; return; }
            ponto = chao.point;

            telegrafo.MostrarArea(ponto, raio, aviso, CorDoFogo);
            var boca = chama.transform.position + new Vector3(0f, 0.3f, 0f);
            Projetil.Lancar(framesDaBola, 12f, boca, ponto, aviso, 1.4f, 2.2f, framesDoImpacto, 2f,
                            rastro: new Color(1f, 0.55f, 0.15f, 1f));
            Efeitos.Lampejo(boca, 1.6f, new Color(1f, 0.6f, 0.2f, 0.8f), 0.25f);
            AudioManager.Instance?.PlaySfx(lancarSfx);
            impactoEm = Time.time + aviso;
        }

        private void Impacto()
        {
            impactoEm = -1f;
            proximo = Time.time + intervalo;
            telegrafo.Disparar();

            Efeitos.Chamuscado(ponto, raio);
            Efeitos.Brasas(ponto + new Vector3(0f, 0.2f, 0f), 10, new Color(1f, 0.55f, 0.15f, 1f), forca: 3f, subida: 2f);
            Efeitos.Onda(ponto, raio * 1.3f, new Color(1f, 0.5f, 0.2f, 0.9f));
            AudioManager.Instance?.PlaySfx(impactoSfx);
            CameraShake.Shake(0.1f, 0.08f);

            // Mesma regra do chefe: so acerta quem ficou dentro da marca.
            var hit = Physics2D.OverlapCircle(ponto + new Vector3(0f, 0.4f, 0f), raio, camadaDoJogador);
            if (hit == null) return;
            var vida = hit.GetComponent<HealthSystem>();
            if (vida != null) vida.TakeDamage(dano, ponto);
        }

        private void AnimarChama()
        {
            if (chama == null) return;
            float t = Time.time * 10f + transform.position.x;
            float carga = impactoEm >= 0f ? 1.35f : 1f;   // atica enquanto a bola esta no ar
            chama.transform.localScale = new Vector3(1.0f * carga * (1f + 0.08f * Mathf.Sin(t * 1.3f)),
                                                     1.1f * carga * (1f + 0.18f * Mathf.Sin(t)), 1f);
            luz.intensity = (0.7f + 0.15f * Mathf.Sin(t * 0.6f)) * carga;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 1f);
            Gizmos.DrawLine(transform.position + Vector3.left * alcance, transform.position + Vector3.right * alcance);
        }
    }
}
