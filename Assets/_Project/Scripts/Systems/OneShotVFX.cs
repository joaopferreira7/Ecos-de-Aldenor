using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Efeito visual descartavel: percorre uma folha de sprites UMA vez e se
    /// destroi ao terminar. Usado para explosoes, bolas de fogo e "poofs" de
    /// morte. Um metodo estatico de fabrica cria a instancia ja configurada,
    /// para os chamadores (ex.: chefe) dispararem VFX em uma unica linha.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OneShotVFX : MonoBehaviour
    {
        private Sprite[] frames;
        private float fps = 14f;
        private SpriteRenderer sr;
        private float timer;
        private int index;

        public static void Spawn(Sprite[] frames, Vector3 position, float scale, float fps, int sortingOrder = 20, Color? tint = null)
        {
            if (frames == null || frames.Length == 0) return;

            var go = new GameObject("VFX");
            go.transform.position = position;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = sortingOrder;
            if (tint.HasValue) sr.color = tint.Value;

            var vfx = go.AddComponent<OneShotVFX>();
            vfx.frames = frames;
            vfx.fps = fps;
            vfx.sr = sr;
        }

        private void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null) return;

            timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.01f, fps);
            if (timer >= interval)
            {
                timer -= interval;
                index++;
                if (index >= frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                sr.sprite = frames[index];
            }
        }
    }
}
