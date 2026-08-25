using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Da vida visual a itens colecionaveis (ex.: fragmentos de alma): faz o
    /// sprite flutuar suavemente para cima e para baixo e pulsar de brilho.
    /// Puramente cosmetico - nao interfere na colisao/coleta. O movimento usa
    /// a posicao local, entao funciona mesmo que o objeto pai seja reposicionado.
    /// </summary>
    public class CollectibleFloat : MonoBehaviour
    {
        [Header("Flutuacao")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        [Header("Pulso de brilho (opcional)")]
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private float glowMinAlpha = 0.25f;
        [SerializeField] private float glowMaxAlpha = 0.7f;
        [SerializeField] private float glowSpeed = 2.5f;

        private Vector3 startLocalPos;
        private float phaseOffset;

        private void Start()
        {
            startLocalPos = transform.localPosition;
            // Cada item comeca em uma fase diferente para nao pulsarem em unissono.
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float t = Time.time * bobSpeed + phaseOffset;
            transform.localPosition = startLocalPos + Vector3.up * (Mathf.Sin(t) * bobAmplitude);

            if (glowRenderer != null)
            {
                float a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha,
                    (Mathf.Sin(Time.time * glowSpeed + phaseOffset) + 1f) * 0.5f);
                Color c = glowRenderer.color;
                c.a = a;
                glowRenderer.color = c;
            }
        }
    }
}
