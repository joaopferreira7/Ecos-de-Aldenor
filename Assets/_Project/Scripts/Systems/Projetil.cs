using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Um projetil que sai de um ponto e chega a outro num tempo exato.
    ///
    /// Ele nao causa dano: quem causa e quem o lancou, no instante em que ele
    /// chega. Parece estranho, mas e de proposito. O ataque do chefe ja anuncia
    /// uma AREA no chao e promete acertar quando o circulo se completa; se o
    /// dano viesse de uma colisao fisica da bola de fogo, a promessa poderia ser
    /// quebrada por um quadro perdido ou por um collider mal encostado. Aqui a
    /// bola de fogo e o RELOGIO visivel dessa promessa - ela pousa exatamente
    /// quando o aviso termina - e o dano continua sendo decidido pela area
    /// anunciada. O jogador ve uma coisa so acontecendo.
    ///
    /// O arco de voo e uma parabola leve: uma bola de fogo em linha reta parece
    /// deslizar, com o arco ela parece ter sido ARREMESSADA.
    /// </summary>
    public class Projetil : MonoBehaviour
    {
        private Sprite[] frames;
        private float fps = 12f;
        private SpriteRenderer sr;

        private Vector3 origem;
        private Vector3 destino;
        private float duracao;
        private float tempo;
        private float altura;

        private Sprite[] framesDeImpacto;
        private float escalaDoImpacto;

        private float timerQuadro;
        private int quadro;

        /// <summary>
        /// Lanca o projetil. A duracao e o contrato: ele chega em `tempoDeVoo`
        /// segundos, custe o que custar a distancia.
        /// </summary>
        public static Projetil Lancar(Sprite[] frames, float fps, Vector3 de, Vector3 para,
                                      float tempoDeVoo, float escala = 1f, float arco = 1.2f,
                                      Sprite[] impacto = null, float escalaImpacto = 2.2f,
                                      Color? tinta = null, int ordem = 6)
        {
            if (frames == null || frames.Length == 0) return null;

            var go = new GameObject("Projetil");
            go.transform.position = de;
            go.transform.localScale = new Vector3(escala, escala, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = ordem;
            if (tinta.HasValue) sr.color = tinta.Value;

            var p = go.AddComponent<Projetil>();
            p.frames = frames;
            p.fps = fps;
            p.sr = sr;
            p.origem = de;
            p.destino = para;
            p.duracao = Mathf.Max(0.05f, tempoDeVoo);
            p.altura = arco;
            p.framesDeImpacto = impacto;
            p.escalaDoImpacto = escalaImpacto;
            return p;
        }

        private void Update()
        {
            tempo += Time.deltaTime;
            float t = Mathf.Clamp01(tempo / duracao);

            // Linha reta no plano, mais um arco que sobe e desce (4t(1-t) vale 0
            // nas pontas e 1 no meio - a parabola mais simples que existe).
            Vector3 pos = Vector3.Lerp(origem, destino, t);
            pos.y += altura * 4f * t * (1f - t);
            transform.position = pos;

            AnimarQuadros();

            if (t >= 1f) Chegar();
        }

        private void AnimarQuadros()
        {
            timerQuadro += Time.deltaTime;
            float intervalo = 1f / Mathf.Max(0.01f, fps);
            while (timerQuadro >= intervalo)
            {
                timerQuadro -= intervalo;
                quadro = (quadro + 1) % frames.Length;
                sr.sprite = frames[quadro];
            }
        }

        private void Chegar()
        {
            if (framesDeImpacto != null && framesDeImpacto.Length > 0)
                OneShotVFX.Spawn(framesDeImpacto, destino, escalaDoImpacto, 16f);

            Destroy(gameObject);
        }
    }
}
