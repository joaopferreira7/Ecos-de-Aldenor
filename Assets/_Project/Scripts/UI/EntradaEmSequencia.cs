using UnityEngine;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Faz os filhos deste objeto aparecerem um depois do outro.
    ///
    /// Os creditos eram uma parede de texto que ja estava inteira na tela no
    /// primeiro quadro. Surgindo bloco a bloco (titulo, equipe, assets...), a
    /// tela ganha ritmo de encerramento e o olho le na ordem certa. So mexe na
    /// transparencia: a posicao continua por conta do layout.
    /// </summary>
    public class EntradaEmSequencia : MonoBehaviour
    {
        [SerializeField] private float atrasoInicial = 0.3f;
        [SerializeField] private float intervalo = 0.35f;
        [SerializeField] private float duracao = 0.6f;

        private CanvasGroup[] grupos;
        private float tempo;

        private void Start()
        {
            grupos = new CanvasGroup[transform.childCount];
            for (int i = 0; i < grupos.Length; i++)
            {
                var filho = transform.GetChild(i);
                var g = filho.GetComponent<CanvasGroup>();
                if (g == null) g = filho.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0f;
                grupos[i] = g;
            }
        }

        private void Update()
        {
            if (grupos == null) return;
            tempo += Time.unscaledDeltaTime;
            bool terminou = true;
            for (int i = 0; i < grupos.Length; i++)
            {
                float t = (tempo - atrasoInicial - i * intervalo) / duracao;
                grupos[i].alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                if (t < 1f) terminou = false;
            }
            if (terminou) enabled = false;
        }
    }
}
