using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// PROLOGO, PARTE 1: Aldenor, na noite em que a nevoa desceu.
    ///
    /// A cidade da Fase 3, sem nada de jogo (a mesma cena-base do epilogo):
    ///
    ///   1. o entardecer: os moradores andam pela rua e os sinos tocam - o
    ///      "reino de sinos e lampioes" da historia, agora ouvido;
    ///   2. a camera sobe um pouco, para a igreja na colina;
    ///   3. o relampago: a musica some, a luz cai, a nevoa desce e os sinos
    ///      param de tocar;
    ///   4. os moradores param, perdem a cor, e as almas deles sao arrancadas e
    ///      levadas pelo ceu em direcao a igreja.
    ///
    /// Continua em PrologoNaIgreja. ESC pula o prologo inteiro.
    ///
    /// O epilogo desfaz esta cena no mesmo lugar: as almas voltam, os moradores
    /// recuperam a cor e os sinos tocam de novo.
    /// </summary>
    public class PrologoNaCidade : MonoBehaviour
    {
        [SerializeField] private EpilogoNaCidade.Morador[] moradores;
        [SerializeField] private float escalaDosMoradores = 3f;
        [SerializeField] private AudioClip trovao;
        [SerializeField] private AudioClip somDaAlma;
        [SerializeField] private string proximaCena = "PrologoIgreja";
        [SerializeField] private string cenaSePular = "Tutorial";

        private static readonly Color LuzDoEntardecer = new Color(1f, 0.74f, 0.58f, 1f);
        private static readonly Color FundoDoEntardecer = new Color(1f, 0.84f, 0.74f, 1f);
        private static readonly Color LuzDaTempestade = new Color(0.34f, 0.36f, 0.62f, 1f);
        private static readonly Color FundoDaTempestade = new Color(0.42f, 0.42f, 0.62f, 1f);
        private static readonly Color Cinza = new Color(0.48f, 0.46f, 0.6f, 0.85f);
        private const float Chao = -0.4f;

        private Cinema cinema;
        private Transform foco;
        private Light2D luzGlobal;
        private SpriteRenderer[] fundo;
        private readonly List<SpriteRenderer> corpos = new List<SpriteRenderer>();
        private readonly List<float> direcoes = new List<float>();
        private bool andando = true;
        private bool saindo;

        private void Awake()
        {
            var nevoa = FindAnyObjectByType<AmbienteVivo>();
            if (nevoa != null) nevoa.ComecarSemNevoa();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            cinema = Cinema.Criar();

            var seguidor = Camera.main != null ? Camera.main.GetComponent<SceneScript.CameraFollow2D>() : null;
            foco = new GameObject("FocoDaCena").transform;
            foco.position = new Vector3(-5.9f, 2.2f, 0f);
            if (seguidor != null)
            {
                seguidor.target = foco;
                seguidor.smoothSpeed = 1.6f;
                seguidor.transform.position = new Vector3(foco.position.x, foco.position.y, -10f);
            }

            var luz = GameObject.Find("Lighting");
            luzGlobal = luz != null ? luz.GetComponent<Light2D>() : null;
            var camadas = FindObjectsByType<ParallaxLayer>();
            fundo = new SpriteRenderer[camadas.Length];
            for (int i = 0; i < camadas.Length; i++) fundo[i] = camadas[i].GetComponent<SpriteRenderer>();
            StartCoroutine(Encenacao.Clima(luzGlobal, fundo, LuzDoEntardecer, 0.85f, LuzDoEntardecer, 0.85f,
                                           FundoDoEntardecer, FundoDoEntardecer, 0f));

            // Os moradores, vivos e andando.
            foreach (var m in moradores)
            {
                var sr = Encenacao.Ator("Morador", m.andando, new Vector3(m.x, Chao, 0f), escalaDosMoradores, 2);
                sr.flipX = m.espelhar;
                corpos.Add(sr);
                direcoes.Add(m.espelhar ? -1f : 1f);
                StartCoroutine(Encenacao.Animar(sr, m.andando, 8f));
            }

            StartCoroutine(Cena());
        }

        private void Update()
        {
            if (!saindo && cinema != null && cinema.PularPedido)
            {
                StopAllCoroutines();
                DespertarDoRen.Pendente = false;   // pulou a historia: o tutorial abre direto
                StartCoroutine(Sair(cenaSePular));
                return;
            }

            if (!andando) return;
            for (int i = 0; i < corpos.Count; i++)
            {
                var c = corpos[i];
                var p = c.transform.position;
                p.x += direcoes[i] * 0.7f * Time.deltaTime;
                if (Mathf.Abs(p.x - moradores[i].x) > 1.6f) { direcoes[i] = -direcoes[i]; c.flipX = direcoes[i] < 0f; }
                c.transform.position = p;
            }
        }

        private IEnumerator Cena()
        {
            yield return cinema.Faixas(true, 1f);

            // 1. O entardecer, os sinos.
            StartCoroutine(Panoramica(0f, 2.2f, 9f));
            StartCoroutine(TocarSinos(1.2f));
            yield return cinema.Narrar("Aldenor era um reino de sinos e lampiões acesos.", 2.6f);

            // 2. A igreja na colina.
            StartCoroutine(Panoramica(0f, 3.2f, 4f));
            yield return cinema.Narrar("No alto da colina, a velha igreja velava por todos.", 2.4f);

            // 3. A nevoa desce; os sinos se calam.
            StartCoroutine(BaixarMusica(2.5f));
            yield return Encenacao.Relampago(cinema, trovao);
            StartCoroutine(Encenacao.Clima(luzGlobal, fundo, LuzDoEntardecer, 0.85f, LuzDaTempestade, 0.42f,
                                           FundoDoEntardecer, FundoDaTempestade, 3f));
            var nevoa = FindAnyObjectByType<AmbienteVivo>();
            if (nevoa != null) StartCoroutine(nevoa.Adensar(4f));
            StartCoroutine(Panoramica(0f, 2.2f, 3f));
            yield return cinema.Narrar("Até a noite em que a névoa desceu. E os sinos se calaram.", 2.2f);

            // 4. Os moradores param, apagam-se, e as almas sao levadas.
            andando = false;
            for (int i = 0; i < corpos.Count; i++)
            {
                corpos[i].name = "MoradorParado";
                StartCoroutine(Encenacao.Animar(corpos[i], moradores[i].parado, 5f));
            }
            StartCoroutine(Encenacao.Relampago(cinema, trovao));
            for (int i = 0; i < corpos.Count; i++)
            {
                StartCoroutine(ArrancarAlma(corpos[i]));
                yield return new WaitForSeconds(0.45f);
            }
            yield return cinema.Narrar("As almas do povo foram arrancadas dos corpos e levadas para a colina.", 2.6f);

            yield return Sair(proximaCena);
        }

        private IEnumerator ArrancarAlma(SpriteRenderer corpo)
        {
            var de = corpo.transform.position + new Vector3(0f, 0.3f, 0f);
            AudioManager.Instance?.PlaySfx(somDaAlma);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                corpo.color = Color.Lerp(Color.white, Cinza, t / 0.7f);
                yield return null;
            }
            var alma = Encenacao.Alma(de, new Color(1f, 0.8f, 0.95f, 0.95f));
            // Sobe girando e segue para a direita e para o alto - para a igreja.
            for (float t = 0f; t < 3f && alma != null; t += Time.deltaTime)
            {
                alma.position = de + new Vector3(Mathf.Sin(t * 4f) * 0.3f + t * t * 1.2f, t * 2.4f, 0f);
                if (Random.value < 0.3f) Efeitos.Brasas(alma.position, 1, new Color(1f, 0.8f, 0.95f, 1f), forca: 0.2f, subida: 0.3f, tamanho: 0.1f, vida: 0.5f);
                yield return null;
            }
            if (alma != null) Destroy(alma.gameObject);
        }

        private IEnumerator TocarSinos(float espera)
        {
            var sinos = new List<Sino>(FindObjectsByType<Sino>());
            sinos.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            yield return new WaitForSeconds(espera);
            foreach (var s in sinos)
            {
                s.Badalar();
                yield return new WaitForSeconds(1.3f);
            }
        }

        private IEnumerator Panoramica(float x, float y, float duracao)
        {
            var de = foco.position;
            var para = new Vector3(x, y, 0f);
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                foco.position = Vector3.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / duracao));
                yield return null;
            }
            foco.position = para;
        }

        private static IEnumerator BaixarMusica(float duracao)
        {
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                AudioManager.Instance?.SetMusicIntensity(1f - t / duracao);
                yield return null;
            }
            AudioManager.Instance?.StopMusic();
        }

        private IEnumerator Sair(string cena)
        {
            saindo = true;
            yield return cinema.Cortina(Color.black, 1f, 1f);
            if (SceneController.Instance != null) SceneController.Instance.LoadScene(cena);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(cena);
        }
    }
}
