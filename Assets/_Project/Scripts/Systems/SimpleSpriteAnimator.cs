using System;
using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Animador de sprites minimalista para os personagens dos pacotes Gothic,
    /// que sao conjuntos de PNGs soltos sem AnimatorController proprio.
    ///
    /// Comecou como um loop unico (so o "parado"), e era so isso que fantasma,
    /// esqueleto e chefe faziam: uma pose em loop, a vida inteira, empurrada
    /// pelo Rigidbody. Andar, despertar, lancar magia e morrer sao animacoes que
    /// EXISTEM nos pacotes, no mesmo tamanho de quadro, e nao tinham como ser
    /// tocadas porque este componente nao sabia trocar de clipe.
    ///
    /// Agora sabe. Sao duas ideias, so:
    ///   - CLIPE EM LOOP (PlayLoop): o estado continuo - parado, andando.
    ///   - CLIPE DE UMA VEZ (PlayOnce): o gesto que comeca e termina - despertar,
    ///     lancar, morrer. Ao terminar, avisa quem pediu, e por isso o codigo de
    ///     combate pode esperar a animacao em vez de adivinhar tempos.
    ///
    /// O clipe do Inspector continua sendo o padrao (o "parado"), entao os
    /// prefabs que ja existiam seguem funcionando sem mudar nada.
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

        private Sprite[] atual;          // clipe tocando agora
        private float fpsAtual;
        private bool umaVezSo;           // clipe que nao repete
        private Action aoTerminar;
        private bool terminado;

        /// <summary>Esta tocando um gesto que ainda nao acabou?</summary>
        public bool EmGesto => umaVezSo && !terminado;

        /// <summary>Troca o clipe padrao (o que toca em loop quando nada mais toca).</summary>
        public void SetFrames(Sprite[] newFrames, float fps)
        {
            frames = newFrames;
            framesPerSecond = fps;
            PlayLoop(newFrames, fps);
        }

        /// <summary>Toca um clipe em loop. Repetir o mesmo clipe nao reinicia a animacao.</summary>
        public void PlayLoop(Sprite[] clipe, float fps)
        {
            if (clipe == null || clipe.Length == 0) return;
            if (atual == clipe && !umaVezSo) { fpsAtual = fps; return; }

            atual = clipe;
            fpsAtual = fps;
            umaVezSo = false;
            terminado = false;
            aoTerminar = null;
            index = 0;
            timer = 0f;
            Aplicar();
        }

        /// <summary>
        /// Toca um clipe uma unica vez e segura o ultimo quadro. O retorno de
        /// chamada permite encadear ("quando a conjuracao terminar, solte a bola
        /// de fogo") sem duplicar a duracao da animacao em algum float de codigo.
        /// </summary>
        public void PlayOnce(Sprite[] clipe, float fps, Action quandoTerminar = null)
        {
            if (clipe == null || clipe.Length == 0)
            {
                quandoTerminar?.Invoke();
                return;
            }

            atual = clipe;
            fpsAtual = fps;
            umaVezSo = true;
            terminado = false;
            aoTerminar = quandoTerminar;
            index = 0;
            timer = 0f;
            Aplicar();
        }

        /// <summary>Volta ao clipe do Inspector (a pose de descanso).</summary>
        public void VoltarAoPadrao()
        {
            PlayLoop(frames, framesPerSecond);
        }

        /// <summary>Duracao, em segundos, que um clipe levaria nesta taxa.</summary>
        public static float Duracao(Sprite[] clipe, float fps)
        {
            if (clipe == null || clipe.Length == 0 || fps <= 0f) return 0f;
            return clipe.Length / fps;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            atual = frames;
            fpsAtual = framesPerSecond;
        }

        private void Update()
        {
            if (atual == null || atual.Length == 0 || spriteRenderer == null) return;
            if (umaVezSo && terminado) return;

            timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.01f, fpsAtual);
            while (timer >= interval)
            {
                timer -= interval;
                index++;

                if (index >= atual.Length)
                {
                    if (umaVezSo)
                    {
                        // Segura o ultimo quadro: um gesto que termina no ar
                        // (morte, conjuracao) nao pode voltar ao primeiro quadro.
                        index = atual.Length - 1;
                        terminado = true;
                        Aplicar();

                        var callback = aoTerminar;
                        aoTerminar = null;
                        callback?.Invoke();
                        return;
                    }
                    index = 0;
                }
            }

            Aplicar();
        }

        private void Aplicar()
        {
            if (spriteRenderer != null && atual != null && atual.Length > 0)
                spriteRenderer.sprite = atual[Mathf.Clamp(index, 0, atual.Length - 1)];
        }
    }
}
