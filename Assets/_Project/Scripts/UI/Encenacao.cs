using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Pecas comuns as cenas dentro do jogo (prologo e epilogo): atores que
    /// tocam quadros soltos dos pacotes, almas de luz, luzes pontuais e o
    /// cartao com o nome do jogo.
    /// </summary>
    public static class Encenacao
    {
        /// <summary>Um personagem feito de quadros soltos, com os pes no chao dado.</summary>
        public static SpriteRenderer Ator(string nome, Sprite[] quadros, Vector3 pes, float escala, int ordem = 3)
        {
            var go = new GameObject(nome);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = quadros != null && quadros.Length > 0 ? quadros[0] : null;
            sr.sortingOrder = ordem;
            go.transform.localScale = new Vector3(escala, escala, 1f);
            float baseDoDesenho = sr.sprite != null ? sr.sprite.bounds.min.y * escala : 0f;
            go.transform.position = new Vector3(pes.x, pes.y - baseDoDesenho, 0f);
            return sr;
        }

        /// <summary>
        /// Toca os quadros em loop enquanto o ator existir e o nome dele nao
        /// mudar - renomear o ator e o jeito de trocar de animacao (a anterior
        /// para sozinha).
        /// </summary>
        public static IEnumerator Animar(SpriteRenderer sr, Sprite[] quadros, float fps, bool repetir = true)
        {
            if (sr == null || quadros == null || quadros.Length == 0) yield break;
            string dono = sr.name;
            for (int i = 0; sr != null && sr.name == dono; i++)
            {
                if (!repetir && i >= quadros.Length) yield break;
                sr.sprite = quadros[repetir ? i % quadros.Length : i];
                yield return new WaitForSeconds(1f / fps);
            }
        }

        /// <summary>Uma alma: brilho redondo com luz propria.</summary>
        public static Transform Alma(Vector3 onde, Color cor, float tamanho = 0.5f)
        {
            var go = new GameObject("Alma");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprites.Brilho();
            sr.color = cor;
            sr.sortingOrder = 20;
            go.transform.localScale = Vector3.one * tamanho;
            go.transform.position = onde;
            Luz(go.transform, cor, 1.6f, 0.8f);
            return go.transform;
        }

        public static Light2D Luz(Transform pai, Color cor, float raio, float intensidade)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(pai, false);
            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = raio;
            luz.pointLightInnerRadius = 0.1f;
            luz.color = cor;
            luz.intensity = intensidade;
            return luz;
        }

        /// <summary>Leva a luz global e o tom do fundo de um estado a outro.</summary>
        public static IEnumerator Clima(Light2D global, SpriteRenderer[] fundo,
                                        Color luzDe, float intDe, Color luzPara, float intPara,
                                        Color fundoDe, Color fundoPara, float duracao)
        {
            for (float t = 0f; t <= duracao; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, duracao <= 0f ? 1f : t / duracao);
                if (global != null)
                {
                    global.color = Color.Lerp(luzDe, luzPara, k);
                    global.intensity = Mathf.Lerp(intDe, intPara, k);
                }
                var tom = Color.Lerp(fundoDe, fundoPara, k);
                foreach (var sr in fundo) if (sr != null) sr.color = tom;
                if (duracao <= 0f) yield break;
                yield return null;
            }
        }

        /// <summary>Um relampago: clarao branco e o trovao.</summary>
        public static IEnumerator Relampago(Cinema cinema, AudioClip trovao)
        {
            AudioManager.Instance?.PlaySfx(trovao);
            CameraShake.Shake(0.25f, 0.15f);
            yield return cinema.Cortina(Color.white, 0.8f, 0.05f);
            yield return cinema.Cortina(Color.white, 0.1f, 0.12f);
            yield return cinema.Cortina(Color.white, 0.6f, 0.06f);
            yield return cinema.Cortina(Color.white, 0f, 0.35f);
        }

        /// <summary>O nome do jogo no escuro, com a frase embaixo.</summary>
        public static IEnumerator Titulo(Cinema cinema, string subtitulo, float permanencia)
        {
            // Por cima do cinema: o titulo aparece sobre a cortina preta.
            var canvas = UIFactory.CriarCanvas("Titulo", 4600);
            var grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            var nome = UIFactory.Titulo(canvas.transform, "Nome", "Ecos de Aldenor", 140, UIFactory.CorTitulo,
                                        new Vector2(1700f, 180f), new Vector2(0f, 60f));
            var sombra = nome.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.25f, 0f, 0.1f, 0.9f);
            sombra.effectDistance = new Vector2(5f, -5f);
            UIFactory.Linha(canvas.transform, new Vector2(520f, 2f), new Vector2(0f, -35f), UIFactory.CorAcento);
            UIFactory.Texto(canvas.transform, "Sub", subtitulo, 30, UIFactory.CorAcento, TextAnchor.MiddleCenter,
                            new Vector2(1200f, 40f), new Vector2(0f, -80f));
            yield return UIFactory.Desvanecer(grupo, 0f, 1f, 1.4f);
            yield return cinema.Pausa(permanencia);
        }
    }
}
