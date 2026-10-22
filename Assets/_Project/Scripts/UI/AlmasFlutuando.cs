using UnityEngine;
using UnityEngine.UI;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Almas subindo devagar pela tela do menu: o titulo do jogo e "as almas
    /// ecoam nas trevas", e ate agora nada no menu se mexia. Poucas, pequenas e
    /// transparentes - movimento de fundo, nao espetaculo competindo com os
    /// botoes.
    /// </summary>
    public class AlmasFlutuando : MonoBehaviour
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private int quantidade = 9;
        [SerializeField] private Vector2 tamanho = new Vector2(18f, 30f);
        [SerializeField] private Vector2 velocidade = new Vector2(18f, 38f);
        [SerializeField] private Color cor = new Color(1f, 0.75f, 0.9f, 1f);

        /// <summary>Configura por codigo, antes do Start.</summary>
        public void Configurar(Sprite novoSprite, int novaQuantidade, Color novaCor, Vector2? novoTamanho = null)
        {
            sprite = novoSprite;
            quantidade = novaQuantidade;
            cor = novaCor;
            if (novoTamanho.HasValue) tamanho = novoTamanho.Value;
        }

        private RectTransform[] almas;
        private Image[] imagens;
        private float[] vel, fase, alphaMax;

        private void Start()
        {
            if (sprite == null) return;
            var raiz = (RectTransform)transform;
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = raiz.offsetMax = Vector2.zero;

            almas = new RectTransform[quantidade];
            imagens = new Image[quantidade];
            vel = new float[quantidade];
            fase = new float[quantidade];
            alphaMax = new float[quantidade];

            for (int i = 0; i < quantidade; i++)
            {
                var go = new GameObject("Alma", typeof(RectTransform));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(raiz, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                float t = Random.Range(tamanho.x, tamanho.y);
                rt.sizeDelta = new Vector2(t, t);
                rt.anchoredPosition = new Vector2(Random.Range(-900f, 900f), Random.Range(0f, 1080f));

                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;

                almas[i] = rt;
                imagens[i] = img;
                vel[i] = Random.Range(velocidade.x, velocidade.y);
                fase[i] = Random.Range(0f, 10f);
                alphaMax[i] = Random.Range(0.25f, 0.6f);
            }
        }

        private void Update()
        {
            if (almas == null) return;
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;

            for (int i = 0; i < almas.Length; i++)
            {
                var p = almas[i].anchoredPosition;
                p.y += vel[i] * dt;
                p.x += Mathf.Sin(t * 0.7f + fase[i]) * 8f * dt;
                if (p.y > 1120f) p = new Vector2(Random.Range(-900f, 900f), -40f);
                almas[i].anchoredPosition = p;

                // Some perto do topo e nasce apagada embaixo.
                float altura = Mathf.Clamp01(p.y / 1080f);
                float a = alphaMax[i] * Mathf.Sin(altura * Mathf.PI) * (0.75f + 0.25f * Mathf.Sin(t * 2f + fase[i]));
                imagens[i].color = new Color(cor.r, cor.g, cor.b, a);
            }
        }
    }
}
