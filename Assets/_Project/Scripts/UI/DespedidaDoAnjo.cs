using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Core;
using EcosDeAldenor.Player;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// EPILOGO, PARTE 1: a despedida do anjo, na propria arena.
    ///
    /// A luta acaba e a cena continua no mesmo lugar, com os personagens de
    /// verdade, em vez de cortar para quadros:
    ///
    ///   1. o chefe cai na explosao de sempre; as faixas de cinema entram;
    ///   2. no lugar dele surge o ANJO, sem o manto, cansado, flutuando baixo;
    ///   3. o Ren caminha ate ele (o controle do jogador fica com o roteiro);
    ///   4. os dois conversam;
    ///   5. o anjo sobe e se desfaz em luz e penas - as seis almas saem dele e
    ///      sobem pelos vitrais, e um sino toca longe;
    ///   6. a luz dourada que ficava atras do anjo passa para o Ren;
    ///   7. corte em branco para a parte 2 (EpilogoNaCidade).
    ///
    /// ESC pula direto para a parte 2.
    /// </summary>
    public class DespedidaDoAnjo : MonoBehaviour
    {
        private static readonly Color Ouro = new Color(1f, 0.85f, 0.5f, 1f);
        private static readonly Color CorDoAnjo = new Color(1f, 0.88f, 0.62f, 1f);
        private static readonly Color CorDoRen = new Color(0.72f, 0.86f, 1f, 1f);

        private Vector3 ondeCaiu;
        private Vector3 escala;
        private Sprite[] framesDoAnjo;
        private string proximaCena;

        /// <summary>
        /// Chamado na morte do chefe, ainda com ele na cena: guarda onde e com
        /// que tamanho ele estava, porque o objeto dele some logo depois.
        /// </summary>
        public static void Iniciar(Vector3 posicao, Vector3 escalaDoChefe, Sprite[] framesAnjo, string proximaCena)
        {
            var go = new GameObject("DespedidaDoAnjo");
            var d = go.AddComponent<DespedidaDoAnjo>();
            d.ondeCaiu = posicao;
            d.escala = new Vector3(Mathf.Abs(escalaDoChefe.x), Mathf.Abs(escalaDoChefe.y), 1f);
            d.framesDoAnjo = framesAnjo;
            d.proximaCena = proximaCena;
        }

        private void Start() => StartCoroutine(Cena());

        private IEnumerator Cena()
        {
            var cinema = Cinema.Criar();

            // O jogador assiste: sem controle, sem dano, sem menu de pausa (o
            // ESC daqui pula a cena).
            var renGO = GameObject.FindGameObjectWithTag("Player");
            var ren = renGO != null ? renGO.GetComponent<PlayerController>() : null;
            if (ren != null)
            {
                ren.ControleBloqueado = true;
                renGO.GetComponent<HealthSystem>()?.GrantInvulnerability(999f);
            }
            var pausa = FindAnyObjectByType<PauseManager>();
            if (pausa != null) pausa.enabled = false;

            var seguidor = Camera.main != null ? Camera.main.GetComponent<SceneScript.CameraFollow2D>() : null;
            var foco = new GameObject("FocoDaCena").transform;
            foco.position = renGO != null ? renGO.transform.position : ondeCaiu;
            if (seguidor != null) { seguidor.target = foco; seguidor.smoothSpeed = 2f; }

            StartCoroutine(BaixarMusica(2.5f));
            StartCoroutine(cinema.Faixas(true, 1.2f));

            // 1. A explosao da morte termina de ser vista.
            yield return Esperar(cinema, 1.8f);
            if (cinema.PularPedido) { yield return Sair(cinema); yield break; }

            // 2. O anjo, sem o manto, onde o chefe caiu.
            var anjo = Ator("Anjo", ondeCaiu, escala);
            anjo.color = new Color(CorDoAnjo.r, CorDoAnjo.g, CorDoAnjo.b, 0f);
            var luzDoAnjo = Luz(anjo.transform, Ouro, 3.5f, 0f);
            Efeitos.Lampejo(ondeCaiu + Vector3.up, 4f, new Color(1f, 0.9f, 0.6f, 0.8f), 0.6f);
            Efeitos.Penas(ondeCaiu + Vector3.up, 16, 2f, 1.2f);
            float lado = renGO != null ? Mathf.Sign(renGO.transform.position.x - ondeCaiu.x) : -1f;
            anjo.flipX = lado < 0f;     // o desenho do anjo olha para a direita
            StartCoroutine(Animar(anjo, 6f));
            StartCoroutine(Flutuar(anjo.transform, 0.08f));
            for (float t = 0f; t < 1.4f; t += Time.unscaledDeltaTime)
            {
                float k = t / 1.4f;
                anjo.color = new Color(CorDoAnjo.r, CorDoAnjo.g, CorDoAnjo.b, k);
                luzDoAnjo.intensity = 1.2f * k;
                yield return null;
            }

            // 3. O Ren caminha ate ele.
            if (ren != null)
            {
                float alvoX = ondeCaiu.x + lado * 2.4f;
                float dir = Mathf.Sign(alvoX - renGO.transform.position.x);
                for (float t = 0f; t < 5f && Mathf.Abs(alvoX - renGO.transform.position.x) > 0.15f; t += Time.unscaledDeltaTime)
                {
                    ren.AndarPorRoteiro(dir);
                    foco.position = Vector3.Lerp(foco.position, (renGO.transform.position + ondeCaiu) * 0.5f, Time.unscaledDeltaTime * 2f);
                    if (cinema.PularPedido) break;
                    yield return null;
                }
                ren.AndarPorRoteiro(0f);
                ren.OlharPara(-lado);
            }
            foco.position = renGO != null ? (renGO.transform.position + ondeCaiu) * 0.5f : ondeCaiu;
            yield return Esperar(cinema, 0.6f);

            // 4. A conversa.
            yield return cinema.Falar("A Vigília", "Cavaleiro... o manto pesava mais do que qualquer espada.", CorDoAnjo);
            yield return cinema.Falar("A Vigília", "Eu prendi as almas de Aldenor por medo de perdê-las. E as perdi assim mesmo.", CorDoAnjo);
            yield return cinema.Falar("Ren", "Elas ainda podem voltar para casa.", CorDoRen);
            yield return cinema.Falar("A Vigília", "Então leve-as. E leve a minha luz também — Aldenor vai precisar de quem a vele.", CorDoAnjo);
            yield return cinema.FecharDialogo();
            if (cinema.PularPedido) { yield return Sair(cinema); yield break; }

            // 5. O anjo sobe e se desfaz; as almas saem dele e sobem pelos vitrais.
            AudioManager.Instance?.PlaySfx(RuntimeAudio.Sino());
            StartCoroutine(SoltarAlmas(ondeCaiu + new Vector3(0f, 1.2f, 0f)));
            var inicio = anjo.transform.position;
            for (float t = 0f; t < 2.6f; t += Time.unscaledDeltaTime)
            {
                float k = t / 2.6f;
                anjo.transform.position = inicio + new Vector3(0f, 1.6f * Mathf.SmoothStep(0f, 1f, k), 0f);
                luzDoAnjo.intensity = 1.2f + 1.5f * k;
                if (Random.value < 0.25f) Efeitos.Penas(anjo.transform.position + Vector3.up, 1, 1.5f, 0.8f);
                foco.position = Vector3.Lerp(foco.position, inicio + new Vector3(-lado * 1.2f, 1.2f, 0f), Time.unscaledDeltaTime);
                yield return null;
            }
            Efeitos.Lampejo(anjo.transform.position + Vector3.up, 7f, new Color(1f, 0.95f, 0.75f, 0.9f), 0.8f);
            Efeitos.Raios(anjo.transform.position + Vector3.up, 12, 4f, Ouro, 1.2f);
            Efeitos.Penas(anjo.transform.position + Vector3.up, 30, 3f, 1.5f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                anjo.color = new Color(1f, 1f, 1f, 1f - t);
                luzDoAnjo.intensity = 2.7f * (1f - t);
                yield return null;
            }
            Destroy(anjo.gameObject);

            // 6. A luz dourada passa para o Ren.
            if (renGO != null)
            {
                var luzDoRen = Luz(renGO.transform, Ouro, 2.6f, 0f);
                luzDoRen.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                Efeitos.Onda(renGO.transform.position, 2.4f, Ouro, 0.8f);
                foco.position = renGO.transform.position + new Vector3(0f, 0.6f, 0f);
                for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
                {
                    luzDoRen.intensity = 1.1f * (t / 1.5f);
                    yield return null;
                }
            }
            yield return cinema.Narrar("A Vigília enfim descansou.", 1.6f);

            // 7. Corte em branco para a cidade.
            yield return Sair(cinema);
        }

        private IEnumerator Sair(Cinema cinema)
        {
            yield return cinema.Cortina(Color.white, 1f, 1.4f);
            if (SceneController.Instance != null) SceneController.Instance.LoadScene(proximaCena);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(proximaCena);
        }

        /// <summary>As seis almas giram em volta do anjo e depois sobem, cada uma por um vitral.</summary>
        private IEnumerator SoltarAlmas(Vector3 centro)
        {
            const int n = 6;
            var almas = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Alma");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = RuntimeSprites.Brilho();
                sr.color = new Color(1f, 0.85f, 0.95f, 0.95f);
                sr.sortingOrder = 30;
                go.transform.localScale = Vector3.one * 0.55f;
                go.transform.position = centro;
                almas[i] = go.transform;
            }

            for (float t = 0f; t < 4.5f; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < n; i++)
                {
                    if (almas[i] == null) continue;
                    float ang = i * Mathf.PI * 2f / n + t * 2.4f;
                    float raio = Mathf.Min(1.4f, t * 1.2f);
                    float subida = Mathf.Max(0f, t - 1.4f);
                    var p = centro + new Vector3(Mathf.Cos(ang) * raio * (1f + subida * 0.6f),
                                                 Mathf.Sin(ang) * raio * 0.5f + subida * subida * 1.3f, 0f);
                    almas[i].position = p;
                    if (Random.value < 0.3f)
                        Efeitos.Brasas(p, 1, new Color(1f, 0.8f, 0.95f, 1f), forca: 0.2f, subida: 0.3f, tamanho: 0.1f, vida: 0.5f);
                }
                yield return null;
            }
            foreach (var a in almas) if (a != null) Destroy(a.gameObject);
        }

        private IEnumerator BaixarMusica(float duracao)
        {
            for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
            {
                AudioManager.Instance?.SetMusicIntensity(Mathf.Lerp(1f, 0.25f, t / duracao));
                yield return null;
            }
        }

        private static IEnumerator Esperar(Cinema cinema, float segundos)
        {
            for (float t = 0f; t < segundos && !cinema.PularPedido; t += Time.unscaledDeltaTime) yield return null;
        }

        private SpriteRenderer Ator(string nome, Vector3 posicao, Vector3 tamanho)
        {
            var go = new GameObject(nome);
            go.transform.position = posicao;
            go.transform.localScale = tamanho;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = framesDoAnjo != null && framesDoAnjo.Length > 0 ? framesDoAnjo[0] : null;
            sr.sortingOrder = 4;
            return sr;
        }

        private IEnumerator Animar(SpriteRenderer sr, float fps)
        {
            if (framesDoAnjo == null || framesDoAnjo.Length == 0) yield break;
            int i = 0;
            while (sr != null)
            {
                sr.sprite = framesDoAnjo[i++ % framesDoAnjo.Length];
                yield return new WaitForSecondsRealtime(1f / fps);
            }
        }

        private static IEnumerator Flutuar(Transform t, float altura)
        {
            while (t != null)
            {
                // Respeita quem mais estiver movendo o anjo (a subida): so soma o balanco.
                t.position += new Vector3(0f, Mathf.Sin(Time.unscaledTime * 2f) * altura * Time.unscaledDeltaTime, 0f);
                yield return null;
            }
        }

        private static Light2D Luz(Transform pai, Color cor, float raio, float intensidade)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(pai, false);
            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = raio;
            luz.pointLightInnerRadius = 0.2f;
            luz.color = cor;
            luz.intensity = intensidade;
            return luz;
        }
    }
}
