using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// EPILOGO, PARTE 2: o amanhecer em Aldenor, com os creditos.
    ///
    /// A cena e a propria cidade da Fase 3 (mesmo fundo, casas, props e os tres
    /// campanarios), sem nada de jogo, dirigida por este componente:
    ///
    ///   1. noite e nevoa; os moradores estao la, cinzentos e parados - os
    ///      corpos sem alma;
    ///   2. as almas descem do ceu, uma para cada morador; quem recebe a sua
    ///      ganha cor e volta a andar. A camera atravessa a cidade com elas;
    ///   3. amanhece: a luz esquenta, a nevoa se levanta e os tres sinos tocam;
    ///   4. o Ren chega, com a luz dourada que o anjo deixou, e para diante da
    ///      igreja na colina;
    ///   5. os creditos sobem por cima dessa imagem;
    ///   6. o titulo, e a tela de Vitoria.
    ///
    /// Espaco acelera os creditos; ESC pula para a Vitoria.
    /// </summary>
    public class EpilogoNaCidade : MonoBehaviour
    {
        [System.Serializable]
        public class Morador
        {
            public Sprite[] parado;
            public Sprite[] andando;
            public float x;
            [Tooltip("Desenho virado para a esquerda.")]
            public bool espelhar;
        }

        [SerializeField] private Morador[] moradores;
        [SerializeField] private Sprite[] renCorrendo;
        [SerializeField] private Sprite[] renParado;
        [SerializeField] private float escalaDosMoradores = 3f;
        [SerializeField] private AudioClip somDaAlma;
        [SerializeField] private string proximaCena = "VictoryScreen";

        [Header("Luz")]
        [SerializeField] private Color luzDaNoite = new Color(0.42f, 0.45f, 0.70f, 1f);
        [SerializeField] private float intensidadeDaNoite = 0.45f;
        [SerializeField] private Color luzDoAmanhecer = new Color(1f, 0.82f, 0.68f, 1f);
        [SerializeField] private float intensidadeDoAmanhecer = 0.95f;
        [SerializeField] private Color fundoDaNoite = new Color(0.55f, 0.55f, 0.78f, 1f);
        [SerializeField] private Color fundoDoAmanhecer = new Color(1f, 0.86f, 0.80f, 1f);

        private static readonly Color Cinza = new Color(0.48f, 0.46f, 0.6f, 0.8f);
        private static readonly Color Ouro = new Color(1f, 0.85f, 0.5f, 1f);
        private const float Chao = -0.4f;

        private Cinema cinema;
        private Transform foco;
        private Light2D luzGlobal;
        private readonly List<SpriteRenderer> fundo = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> corpos = new List<SpriteRenderer>();
        private bool saindo;

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
            foreach (var p in FindObjectsByType<ParallaxLayer>()) fundo.Add(p.GetComponent<SpriteRenderer>());
            Iluminar(0f);

            foreach (var m in moradores) corpos.Add(NovoMorador(m));

            StartCoroutine(Cena());
        }

        private void Update()
        {
            if (!saindo && cinema != null && cinema.PularPedido) { StopAllCoroutines(); StartCoroutine(Sair()); }
        }

        private IEnumerator Cena()
        {
            yield return cinema.Faixas(true, 1f);

            // 2. As almas descem e a camera atravessa a cidade com elas.
            StartCoroutine(Panoramica(5.9f, 11f));
            StartCoroutine(cinema.Narrar("Os ecos voltaram para casa.", 4f));
            for (int i = 0; i < corpos.Count; i++)
            {
                StartCoroutine(AlmaDesce(i));
                yield return new WaitForSeconds(1.5f);
            }
            yield return new WaitForSeconds(1.6f);
            StartCoroutine(AlmaQueSobe(new Vector3(foco.position.x - 1f, 1.5f, 0f)));
            StartCoroutine(AlmaQueSobe(new Vector3(foco.position.x + 1.5f, 1.8f, 0f)));
            yield return cinema.Narrar("Um a um, os moradores de Aldenor lembraram como era respirar.", 2.6f);

            // 3. Amanhece; a nevoa se levanta; os sinos tocam.
            StartCoroutine(Panoramica(0f, 5f));
            StartCoroutine(Amanhecer(8f));
            var nevoa = FindAnyObjectByType<AmbienteVivo>();
            if (nevoa != null) StartCoroutine(nevoa.Dissipar(6f));
            StartCoroutine(TocarSinos());
            yield return new WaitForSeconds(1.2f);
            yield return cinema.Narrar("E, pela primeira vez desde a névoa, os sinos de Aldenor tocaram.", 3f);

            // 4. O Ren chega diante da igreja na colina.
            yield return RenChega(-11f, -2.4f);
            yield return cinema.Narrar("A Vigília enfim descansou.", 1.8f);
            yield return cinema.Narrar("Mas um reino de sinos precisa de quem o guarde — e Aldenor voltou a ter quem a velasse.", 3.2f);

            // 5. Os creditos.
            yield return Creditos();

            // 6. O titulo e a Vitoria.
            yield return Titulo();
            yield return Sair();
        }

        // ------------------------------------------------------------------
        // Atores
        // ------------------------------------------------------------------

        private SpriteRenderer NovoMorador(Morador m)
        {
            var go = new GameObject("Morador");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = m.parado != null && m.parado.Length > 0 ? m.parado[0] : null;
            sr.sortingOrder = 2;
            sr.color = Cinza;
            sr.flipX = m.espelhar;
            go.transform.localScale = new Vector3(escalaDosMoradores, escalaDosMoradores, 1f);
            float meia = sr.sprite != null ? sr.sprite.bounds.extents.y * escalaDosMoradores : 0.6f;
            go.transform.position = new Vector3(m.x, Chao + meia - 0.03f, 0f);
            StartCoroutine(Animar(sr, m.parado, 5f, () => false));
            return sr;
        }

        private IEnumerator AlmaDesce(int i)
        {
            var corpo = corpos[i];
            var m = moradores[i];
            var alvo = corpo.transform.position + new Vector3(0f, 0.2f, 0f);
            var alma = Alma(alvo + new Vector3(Random.Range(-1.5f, 1.5f), 8f, 0f));
            var de = alma.position;
            for (float t = 0f; t < 1.8f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.8f);
                alma.position = Vector3.Lerp(de, alvo, k) + new Vector3(Mathf.Sin(t * 5f) * 0.25f * (1f - k), 0f, 0f);
                if (Random.value < 0.4f) Efeitos.Brasas(alma.position, 1, new Color(1f, 0.85f, 0.95f, 1f), forca: 0.2f, subida: 0.2f, tamanho: 0.1f, vida: 0.4f);
                yield return null;
            }
            Destroy(alma.gameObject);

            AudioManager.Instance?.PlaySfx(somDaAlma);
            Efeitos.Lampejo(alvo, 2.2f, new Color(1f, 0.9f, 0.7f, 0.8f), 0.5f);
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                corpo.color = Color.Lerp(Cinza, Color.white, t / 0.9f);
                yield return null;
            }
            corpo.color = Color.white;

            // Vivo de novo: quem sabe andar, anda - um passeio curto, de ida e volta.
            if (m.andando != null && m.andando.Length > 0) StartCoroutine(Passear(corpo, m));
        }

        private IEnumerator Passear(SpriteRenderer corpo, Morador m)
        {
            float origem = corpo.transform.position.x;
            float dir = m.espelhar ? -1f : 1f;
            bool andando = true;
            StopCoroutineSeAnimando(corpo);
            StartCoroutine(Animar(corpo, m.andando, 8f, () => !andando));
            while (corpo != null)
            {
                var p = corpo.transform.position;
                p.x += dir * 0.7f * Time.deltaTime;
                corpo.transform.position = p;
                corpo.flipX = dir < 0f;
                if (Mathf.Abs(p.x - origem) > 1.4f) dir = -dir;
                yield return null;
            }
            andando = false;
        }

        // A animacao do morador parado e trocada pela de andar: cada corpo tem
        // uma so animacao viva, marcada pelo nome do objeto.
        private void StopCoroutineSeAnimando(SpriteRenderer corpo) => corpo.name = "MoradorAndando";

        private IEnumerator Animar(SpriteRenderer sr, Sprite[] quadros, float fps, System.Func<bool> parar)
        {
            if (quadros == null || quadros.Length == 0) yield break;
            string dono = sr.name;
            int i = 0;
            while (sr != null && !parar() && sr.name == dono)
            {
                sr.sprite = quadros[i++ % quadros.Length];
                yield return new WaitForSeconds(1f / fps);
            }
        }

        private IEnumerator AlmaQueSobe(Vector3 de)
        {
            var alma = Alma(de);
            for (float t = 0f; t < 3.5f; t += Time.deltaTime)
            {
                alma.position = de + new Vector3(Mathf.Sin(t * 2f) * 0.4f, t * 2.6f, 0f);
                yield return null;
            }
            Destroy(alma.gameObject);
        }

        private static Transform Alma(Vector3 onde)
        {
            var go = new GameObject("Alma");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprites.Brilho();
            sr.color = new Color(1f, 0.85f, 0.95f, 0.95f);
            sr.sortingOrder = 20;
            go.transform.localScale = Vector3.one * 0.5f;
            go.transform.position = onde;
            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = 1.6f;
            luz.color = new Color(1f, 0.8f, 0.95f, 1f);
            luz.intensity = 0.8f;
            return go.transform;
        }

        private IEnumerator RenChega(float de, float ate)
        {
            var go = new GameObject("Ren");
            // Os pes na rua: a base do desenho, nao o centro dele.
            var pose = renParado != null && renParado.Length > 0 ? renParado[0] : null;
            float y = pose != null ? Chao - pose.bounds.min.y : 0.5f;
            go.transform.position = new Vector3(de, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var luz = new GameObject("Luz").AddComponent<Light2D>();
            luz.transform.SetParent(go.transform, false);
            luz.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = 2.6f;
            luz.color = Ouro;
            luz.intensity = 1.1f;

            bool correndo = true;
            StartCoroutine(Animar(sr, renCorrendo, 12f, () => !correndo));
            while (go.transform.position.x < ate)
            {
                go.transform.position += new Vector3(3.2f * Time.deltaTime, 0f, 0f);
                yield return null;
            }
            correndo = false;
            yield return null;
            sr.name = "RenParado";
            StartCoroutine(Animar(sr, renParado, 8f, () => false));
            yield return new WaitForSeconds(0.6f);
        }

        // ------------------------------------------------------------------
        // Luz, camera, sinos
        // ------------------------------------------------------------------

        private void Iluminar(float k)
        {
            if (luzGlobal != null)
            {
                luzGlobal.color = Color.Lerp(luzDaNoite, luzDoAmanhecer, k);
                luzGlobal.intensity = Mathf.Lerp(intensidadeDaNoite, intensidadeDoAmanhecer, k);
            }
            var tom = Color.Lerp(fundoDaNoite, fundoDoAmanhecer, k);
            foreach (var sr in fundo) if (sr != null) sr.color = tom;
        }

        private IEnumerator Amanhecer(float duracao)
        {
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                Iluminar(Mathf.SmoothStep(0f, 1f, t / duracao));
                yield return null;
            }
            Iluminar(1f);
        }

        private IEnumerator Panoramica(float x, float duracao)
        {
            float de = foco.position.x;
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                foco.position = new Vector3(Mathf.Lerp(de, x, Mathf.SmoothStep(0f, 1f, t / duracao)), foco.position.y, 0f);
                yield return null;
            }
        }

        private IEnumerator TocarSinos()
        {
            var sinos = new List<Sino>(FindObjectsByType<Sino>());
            sinos.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            foreach (var s in sinos)
            {
                yield return new WaitForSeconds(1.4f);
                var vida = s.GetComponent<HealthSystem>();
                if (vida != null) vida.TakeDamage(1);
            }
        }

        // ------------------------------------------------------------------
        // Creditos, titulo, saida
        // ------------------------------------------------------------------

        private IEnumerator Creditos()
        {
            var canvas = UIFactory.CriarCanvas("Creditos", 4400);
            var raiz = (RectTransform)canvas.transform;

            // Meia-tela escura a direita: o Ren e a igreja ficam a esquerda, a vista.
            var veu = UIFactory.Painel(raiz, "Veu", new Color(0.02f, 0.01f, 0.05f, 0.78f),
                                       new Vector2(0.56f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            veu.GetComponent<Image>().raycastTarget = false;
            var grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;

            var coluna = new GameObject("Coluna", typeof(RectTransform)).GetComponent<RectTransform>();
            coluna.SetParent(veu, false);
            coluna.anchorMin = new Vector2(0f, 0f);
            coluna.anchorMax = new Vector2(1f, 0f);
            coluna.pivot = new Vector2(0.5f, 1f);
            coluna.anchoredPosition = Vector2.zero;

            float y = 0f;
            Linha(coluna, ref y, "Ecos de Aldenor", 64, true, UIFactory.CorTitulo, 40f);
            Linha(coluna, ref y, "Os sinos tocam outra vez", 26, false, UIFactory.CorAcento, 90f);
            Secao(coluna, ref y, "Equipe", new[]
            {
                "Gabriel Quirino Bressanin", "Guilherme Barros Oliveira", "Heitor Lupino",
                "João Pedro Ferreira", "Matheus Risso",
            });
            Secao(coluna, ref y, "Disciplina", new[] { "Desenvolvimento de Jogos Digitais", "Ciência da Computação — P2" });
            Secao(coluna, ref y, "Arte", new[]
            {
                "GothicVania (Cemitério, Igreja, Cidade)", "Luis Zuno \"Ansimuz\" — CC0",
                "Hero Knight — LuizMelo", "Bandits — Sven Thole",
                "2D Mega Pack — Brackeys", "2D Dungeon Tilemap e Free GameUI — Aether2D",
                "Hitspark FX — Jason Lee",
            });
            Secao(coluna, ref y, "Música", new[]
            {
                "Fases — Seth_Makes_Sounds", "Epic Boss Battle — Juhani Junkala", "Vila — Pascal Belisle",
            });
            Secao(coluna, ref y, "Sons", new[] { "RPG Essentials — Leohpaz" });
            Secao(coluna, ref y, "Fontes", new[] { "New Rocker — Pablo Impallari", "Pixelify Sans — Stefie Justprince" });
            y += 120f;
            Linha(coluna, ref y, "Obrigado por jogar.", 34, true, UIFactory.CorAcento, 0f);
            coluna.sizeDelta = new Vector2(0f, y);

            yield return UIFactory.Desvanecer(grupo, 0f, 1f, 1.2f);

            // Sobe da base da tela ate a ultima linha passar do topo. A camera
            // sobe junto, devagar, para o ceu do amanhecer.
            float altura = ((RectTransform)canvas.transform).rect.height;
            // Para quando o "Obrigado por jogar" chega ao meio da tela, em vez
            // de rolar a tela inteira vazia ate ele sumir.
            float total = y + altura * 0.5f;
            float pos = 0f;
            // So um pouco: o Ren e a igreja na colina continuam em quadro.
            StartCoroutine(SubirCamera(2.2f, 3.0f, 30f));
            while (pos < total)
            {
                float vel = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0) ? 420f : 70f;
                pos += vel * Time.deltaTime;
                coluna.anchoredPosition = new Vector2(0f, pos);
                yield return null;
            }
            yield return UIFactory.Desvanecer(grupo, 1f, 0f, 0.8f, true);
        }

        private IEnumerator SubirCamera(float de, float ate, float duracao)
        {
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                foco.position = new Vector3(foco.position.x, Mathf.Lerp(de, ate, Mathf.SmoothStep(0f, 1f, t / duracao)), 0f);
                yield return null;
            }
        }

        private static void Secao(RectTransform coluna, ref float y, string titulo, string[] linhas)
        {
            Linha(coluna, ref y, titulo, 34, true, UIFactory.CorAcento, 14f);
            foreach (var l in linhas) Linha(coluna, ref y, l, 24, false, UIFactory.CorTexto, 6f);
            y += 56f;
        }

        private static void Linha(RectTransform coluna, ref float y, string texto, int tamanho, bool titulo, Color cor, float depois)
        {
            Text t = titulo
                ? UIFactory.Titulo(coluna, "Linha", texto, tamanho, cor, new Vector2(760f, tamanho + 14f), Vector2.zero)
                : UIFactory.Texto(coluna, "Linha", texto, tamanho, cor, TextAnchor.MiddleCenter, new Vector2(760f, tamanho + 12f), Vector2.zero);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            y += tamanho + 14f + depois;
        }

        private IEnumerator Titulo()
        {
            var canvas = UIFactory.CriarCanvas("Titulo", 4450);
            var grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            var nome = UIFactory.Titulo(canvas.transform, "Nome", "Ecos de Aldenor", 140, UIFactory.CorTitulo,
                                        new Vector2(1700f, 180f), new Vector2(0f, 60f));
            var sombra = nome.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.25f, 0f, 0.1f, 0.9f);
            sombra.effectDistance = new Vector2(5f, -5f);
            UIFactory.Linha(canvas.transform, new Vector2(520f, 2f), new Vector2(0f, -35f), UIFactory.CorAcento);
            UIFactory.Texto(canvas.transform, "Sub", "Os sinos tocam outra vez", 30, UIFactory.CorAcento, TextAnchor.MiddleCenter,
                            new Vector2(1200f, 40f), new Vector2(0f, -80f));
            yield return UIFactory.Desvanecer(grupo, 0f, 1f, 1.6f);
            yield return cinema.Pausa(3f);
        }

        private IEnumerator Sair()
        {
            saindo = true;
            yield return cinema.Cortina(Color.black, 1f, 1.2f);
            if (SceneController.Instance != null) SceneController.Instance.LoadScene(proximaCena);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(proximaCena);
        }
    }
}
