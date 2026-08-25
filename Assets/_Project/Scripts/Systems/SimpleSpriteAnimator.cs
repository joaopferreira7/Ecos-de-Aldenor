using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Animador de sprites minimalista: percorre em loop um conjunto de frames
    /// a uma taxa fixa. Usado por inimigos cujos sprites (pacote Gothic) sao
    /// pequenas folhas de 4 quadros sem um AnimatorController proprio - assim
    /// evitamos manter um controlador de animacao completo so para um loop idle.
    /// Nao mexe na escala do objeto, entao o "flip" horizontal feito pelo
    /// EnemyBase (via localScale) continua funcionando normalmente.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;

        private SpriteRenderer spriteRenderer;
        private float timer;
        private int index;

        public void SetFrames(Sprite[] newFrames, float fps)
        {
            frames = newFrames;
            framesPerSecond = fps;
            index = 0;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[0];
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null) return;

            timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.01f, framesPerSecond);
            if (timer >= interval)
            {
                timer -= interval;
                index = (index + 1) % frames.Length;
                spriteRenderer.sprite = frames[index];
            }
        }
    }
}
