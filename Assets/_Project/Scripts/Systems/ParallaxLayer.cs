using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Camada de fundo com efeito de parallax: acompanha a camera principal a uma
    /// fracao da sua velocidade, criando ilusao de profundidade. Um fator 0 deixa
    /// a camada praticamente fixa na tela (ceu/lua bem distante); 1 a prende ao
    /// mundo (primeiro plano). Colocado nas camadas de cenario do cemiterio.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = quase fixo na tela (bem distante); 1 = colado ao mundo.")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactor = 0.5f;
        [Tooltip("Se verdadeiro, ignora o movimento vertical da camera (bom para o chao do cenario).")]
        [SerializeField] private bool lockY = false;

        private Transform cam;
        private Vector3 startPos;
        private Vector3 camStart;

        private void Start()
        {
            var mainCam = Camera.main;
            cam = mainCam != null ? mainCam.transform : null;
            startPos = transform.position;
            if (cam != null) camStart = cam.position;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            Vector3 delta = cam.position - camStart;
            float x = startPos.x + delta.x * parallaxFactor;
            float y = lockY ? startPos.y : startPos.y + delta.y * parallaxFactor;
            transform.position = new Vector3(x, y, transform.position.z);
        }
    }
}
