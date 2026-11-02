using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// PROLOGO, PARTE 2: a igreja na colina, onde a Vigilia vela.
    ///
    /// A arena do chefe, sem nada de jogo e em silencio:
    ///
    ///   1. o anjo diante do altar, dourado; as almas arrancadas da cidade
    ///      chegam e giram em volta dele - e a ele que elas recorrem;
    ///   2. o relampago: a luz fica vermelha e o anjo estremece;
    ///   3. o manto: fumaca vermelha, e no lugar do anjo surge o mago
    ///      encapuzado - a mesma criatura que o Ren vai enfrentar, e a
    ///      transformacao da luta final ao contrario;
    ///   4. as almas fogem como ecos, para longe, para o cemiterio;
    ///   5. o nome do jogo, e o tutorial, onde o Ren desperta.
    /// </summary>
    public class PrologoNaIgreja : MonoBehaviour
    {
        [SerializeField] private Sprite[] anjo;
        [SerializeField] private Sprite[] mago;
        [SerializeField] private Sprite[] fumaca;
        [SerializeField] private float escala = 6f;
        [SerializeField] private AudioClip trovao;
        [SerializeField] private AudioClip somSombrio;
        [SerializeField] private string proximaCena = "Tutorial";

        private static readonly Color Ouro = new Color(1f, 0.85f, 0.5f, 1f);
        private static readonly Color CorDaAlma = new Color(1f, 0.8f, 0.95f, 0.95f);
        private const float Chao = -0.4f;

        private Cinema cinema;
        private Light2D luzGlobal;
        private SpriteRenderer[] fundo;
        private readonly List<Transform> almas = new List<Transform>();
        private bool orbitando = true;
        private bool saindo;
        private Vector3 centro;

        private void Start()
        {
            Time.timeScale = 1f;
            cinema = Cinema.Criar();

            var seguidor = Camera.main != null ? Camera.main.GetComponent<SceneScript.CameraFollow2D>() : null;
            var foco = new GameObject("FocoDaCena").transform;
            foco.position = new Vector3(0f, 2.2f, 0f);
            if (seguidor != null)
            {
                seguidor.target = foco;
                seguidor.transform.position = new Vector3(0f, 2.2f, -10f);
            }

            var luz = GameObject.Find("Lighting");
            luzGlobal = luz != null ? luz.GetComponent<Light2D>() : null;
            var camadas = FindObjectsByType<ParallaxLayer>();
            fundo = new SpriteRenderer[camadas.Length];
            for (int i = 0; i < camadas.Length; i++) fundo[i] = camadas[i].GetComponent<SpriteRenderer>();

            StartCoroutine(Cena());
        }

        private void Update()
        {
            if (!saindo && cinema != null && cinema.PularPedido)
            {
                StopAllCoroutines();
                DespertarDoRen.Pendente = false;
                StartCoroutine(Sair());
            }
        }

        private IEnumerator Cena()
        {
            AudioManager.Instance?.StopMusic();
            StartCoroutine(Encenacao.Clima(luzGlobal, fundo, new Color(0.7f, 0.62f, 0.8f), 0.55f, new Color(0.7f, 0.62f, 0.8f), 0.55f,
                                           Color.white, Color.white, 0f));
            yield return cinema.Faixas(true, 0.8f);

            // 1. O anjo, dourado, e as almas chegando.
            var vigilia = Encenacao.Ator("Anjo", anjo, new Vector3(0f, Chao + 0.9f, 0f), escala, 4);
            vigilia.color = new Color(1f, 1f, 1f, 0f);
            var luzDoAnjo = Encenacao.Luz(vigilia.transform, Ouro, 4f, 0f);
            centro = vigilia.transform.position + new Vector3(0f, 0.3f, 0f);
            StartCoroutine(Encenacao.Animar(vigilia, anjo, 8f));
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                vigilia.color = new Color(1f, 1f, 1f, t / 1.4f);
                luzDoAnjo.intensity = 1.3f * t / 1.4f;
                yield return null;
            }
            StartCoroutine(ChegarAlmas());
            yield return cinema.Narrar("No alto da colina, a Vigília velava pelas almas de Aldenor.", 2.6f);
            yield return cinema.Narrar("Naquela noite, elas subiram até ela, pedindo abrigo.", 2.2f);

            // 2. O relampago; a luz fica vermelha; o anjo estremece.
            yield return Encenacao.Relampago(cinema, trovao);
            StartCoroutine(Encenacao.Clima(luzGlobal, fundo, new Color(0.7f, 0.62f, 0.8f), 0.55f, new Color(0.75f, 0.25f, 0.3f), 0.5f,
                                           Color.white, new Color(0.85f, 0.55f, 0.6f), 2f));
            var origem = vigilia.transform.position;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                vigilia.transform.position = origem + new Vector3(Mathf.Sin(t * 60f) * 0.06f * t, 0f, 0f);
                vigilia.color = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), t / 1.6f);
                luzDoAnjo.color = Color.Lerp(Ouro, new Color(0.9f, 0.15f, 0.2f), t / 1.6f);
                yield return null;
            }
            yield return cinema.Narrar("Mas a Vigília não as protegeu.", 1.6f);

            // 3. O manto: fumaca, e o mago no lugar do anjo.
            AudioManager.Instance?.PlaySfx(somSombrio);
            CameraShake.Shake(0.3f, 0.2f);
            var nuvem = Encenacao.Ator("Manto", fumaca, new Vector3(0f, Chao, 0f), escala * 1.3f, 6);
            nuvem.color = new Color(0.65f, 0.15f, 0.25f, 1f);
            StartCoroutine(Encenacao.Animar(nuvem, fumaca, 12f, false));
            yield return new WaitForSeconds(0.35f);
            vigilia.gameObject.SetActive(false);
            var vulto = Encenacao.Ator("Vigilia", mago, new Vector3(0f, Chao, 0f), escala, 5);
            StartCoroutine(Encenacao.Animar(vulto, mago, 6f));
            var luzDoVulto = Encenacao.Luz(vulto.transform, new Color(0.9f, 0.15f, 0.2f), 3.5f, 1.2f);
            luzDoVulto.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            yield return new WaitForSeconds(0.6f);
            Destroy(nuvem.gameObject);
            yield return cinema.Narrar("Cobriu-se com um manto vermelho e passou a guardá-las como prisioneiras.", 2.6f);

            // 4. As almas fogem como ecos.
            orbitando = false;
            yield return Espalhar();
            yield return cinema.Narrar("E os ecos de Aldenor foram espalhados pelo cemitério.", 2.4f);

            // 5. O nome do jogo.
            yield return cinema.Cortina(Color.black, 1f, 1.2f);
            yield return Encenacao.Titulo(cinema, "As almas ecoam nas trevas", 2.8f);
            DespertarDoRen.Pendente = true;
            yield return Sair();
        }

        private IEnumerator ChegarAlmas()
        {
            for (int i = 0; i < 6; i++)
            {
                var de = new Vector3(-10f - i * 0.6f, 6f + (i % 3) * 0.8f, 0f);
                var alma = Encenacao.Alma(de, CorDaAlma);
                almas.Add(alma);
                StartCoroutine(Orbitar(alma, i));
                yield return new WaitForSeconds(0.35f);
            }
        }

        private IEnumerator Orbitar(Transform alma, int i)
        {
            var de = alma.position;
            for (float t = 0f; orbitando && alma != null; t += Time.deltaTime)
            {
                float ang = i * Mathf.PI / 3f + t * 1.6f;
                var volta = centro + new Vector3(Mathf.Cos(ang) * 1.8f, Mathf.Sin(ang) * 0.8f, 0f);
                // Primeiro chega voando, depois gira em volta do anjo.
                alma.position = Vector3.Lerp(de, volta, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 2.2f)));
                yield return null;
            }
        }

        private IEnumerator Espalhar()
        {
            var inicio = new List<Vector3>();
            foreach (var a in almas) inicio.Add(a != null ? a.position : Vector3.zero);
            for (float t = 0f; t < 2.4f; t += Time.deltaTime)
            {
                for (int i = 0; i < almas.Count; i++)
                {
                    if (almas[i] == null) continue;
                    // Para a esquerda e para baixo, para fora da igreja: o cemiterio.
                    var dir = new Vector3(-1f - i * 0.25f, (i % 2 == 0 ? -0.4f : 0.5f), 0f);
                    almas[i].position = inicio[i] + dir * (t * t * 3f);
                }
                yield return null;
            }
            foreach (var a in almas) if (a != null) Destroy(a.gameObject);
            almas.Clear();
        }

        private IEnumerator Sair()
        {
            saindo = true;
            yield return cinema.Cortina(Color.black, 1f, 0.8f);
            if (SceneController.Instance != null) SceneController.Instance.LoadScene(proximaCena);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(proximaCena);
        }
    }
}
