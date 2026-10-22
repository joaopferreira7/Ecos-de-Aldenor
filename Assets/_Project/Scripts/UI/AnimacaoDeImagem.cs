using UnityEngine;
using UnityEngine.UI;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Toca uma sequencia de sprites numa Image de UI - o equivalente do
    /// SimpleSpriteAnimator para telas. Serve para por o Ren nas telas finais
    /// usando as animacoes que vieram com o personagem (parado na vitoria,
    /// caindo na derrota), em vez de uma imagem estatica ou de nada.
    /// Conta em tempo real, para funcionar com o jogo pausado.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class AnimacaoDeImagem : MonoBehaviour
    {
        [SerializeField] private Sprite[] quadros;
        [SerializeField] private float fps = 8f;
        [Tooltip("Desligado: toca uma vez e segura o ultimo quadro (ex.: morte).")]
        [SerializeField] private bool repetir = true;
        [Tooltip("Espera antes de comecar, em segundos.")]
        [SerializeField] private float atraso;

        private Image imagem;
        private float tempo;

        /// <summary>Configura por codigo (o prologo cria os personagens em runtime).</summary>
        public void Configurar(Sprite[] novosQuadros, float novoFps, bool emLoop = true)
        {
            quadros = novosQuadros;
            fps = novoFps;
            repetir = emLoop;
            tempo = 0f;
            if (imagem == null) imagem = GetComponent<Image>();
            if (quadros != null && quadros.Length > 0) imagem.sprite = quadros[0];
        }

        private void Awake()
        {
            imagem = GetComponent<Image>();
            if (quadros != null && quadros.Length > 0) imagem.sprite = quadros[0];
        }

        private void Update()
        {
            if (quadros == null || quadros.Length == 0) return;
            tempo += Time.unscaledDeltaTime;
            float t = tempo - atraso;
            if (t < 0f) return;

            int i = Mathf.FloorToInt(t * fps);
            i = repetir ? i % quadros.Length : Mathf.Min(i, quadros.Length - 1);
            imagem.sprite = quadros[i];
        }
    }
}
