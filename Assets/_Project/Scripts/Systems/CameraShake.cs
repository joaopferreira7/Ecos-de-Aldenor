using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Efeito de tremor de camera para dar impacto (dano, ataques do chefe,
    /// mortes). Roda em LateUpdate com ordem de execucao alta para aplicar o
    /// deslocamento DEPOIS do CameraFollow2D ja ter posicionado a camera no
    /// alvo - assim o tremor soma-se ao seguimento sem brigar com ele.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private float duration;
        private float magnitude;
        private float timer;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Dispara um tremor. Chamada estatica conveniente.</summary>
        public static void Shake(float duration, float magnitude)
        {
            if (Instance != null) Instance.Begin(duration, magnitude);
        }

        public void Begin(float dur, float mag)
        {
            // Nao interrompe um tremor mais forte em andamento.
            if (timer > 0f && magnitude > mag) return;
            duration = Mathf.Max(0.01f, dur);
            magnitude = mag;
            timer = duration;
        }

        private void LateUpdate()
        {
            if (timer <= 0f) return;

            timer -= Time.deltaTime;
            float damper = Mathf.Clamp01(timer / duration);
            Vector2 offset = Random.insideUnitCircle * magnitude * damper;
            transform.position += new Vector3(offset.x, offset.y, 0f);
        }
    }
}
