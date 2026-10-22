using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EcosDeAldenor.Core;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// O PROLOGO: a historia de Aldenor contada antes do primeiro passo do Ren.
    ///
    /// Cinco capitulos curtos, cada um um pequeno palco montado com a arte dos
    /// proprios pacotes, com uma frase escrita letra por letra na faixa de
    /// baixo. A primeira versao eram quadros PARADOS com legenda - bonitos,
    /// mas ilustracoes, nao uma cena. Agora cada capitulo ENCENA o que a frase
    /// diz, e a frase so comeca depois do momento acontecer:
    ///
    ///   I   Aldenor      a cidade ao entardecer, gente andando, vaga-lumes;
    ///   II  A Vigilia    o anjo diante do altar, sob colunas de luz;
    ///   III A Nevoa      relampago; os moradores se desfazem em almas que
    ///                    sobem, espectros surgem na nevoa;
    ///   IV  O Manto      o anjo se dissolve em fumaca e no lugar dele surge o
    ///                    mago encapuzado - planta a transformacao da luta final;
    ///   V   O Cavaleiro  o Ren se levanta do chao (a animacao de morte dele,
    ///                    ao contrario) e os seis ecos acendem um a um.
    ///
    /// O enquadramento e de cinema: faixas pretas em cima e embaixo, vinheta,
    /// um deslizar lento de camera e o nome do capitulo na fonte gotica.
    ///
    /// Espaco, Enter ou clique completam a frase; de novo, passam ao proximo
    /// capitulo. ESC (ou o botao Pular) vai direto ao tutorial.
    /// </summary>
    public class Prologo : MonoBehaviour
    {
        [System.Serializable]
        public class Ator
        {
            public string nome = "Ator";
            [Tooltip("Ciclo principal (em loop, salvo 'repetir' desligado).")]
            public Sprite[] quadros;
            public float fps = 8f;
            public bool repetir = true;
            [Tooltip("Gesto tocado UMA vez antes do ciclo (ex.: o Ren se levantando).")]
            public Sprite[] entrada;
            public float fpsEntrada = 10f;
            [Tooltip("Ampliacao inteira da arte (pixel art borra em escala quebrada).")]
            public float zoom = 3f;
            [Tooltip("Pes do personagem, a partir do centro-base da imagem, em px de referencia.")]
            public Vector2 posicao;
            public bool espelhar;
            [Tooltip("Caminhada horizontal, px/s (negativo = para a esquerda).")]
            public float andar;
            [Tooltip("Altura da flutuacao, em px. 0 = parado no chao.")]
            public float flutuar;
            public Color cor = Color.white;

            [Header("Tempo no capitulo")]
            [Tooltip("Segundos ate aparecer (0 = ja esta la).")]
            public float surgir;
            [Tooltip("Segundos ate se desfazer (negativo = fica ate o fim).")]
            public float sumir = -1f;
            public float fade = 0.6f;
            [Tooltip("Ao se desfazer, solta almas que sobem.")]
            public bool almasAoSumir;
            public AudioClip somAoSurgir;
            public AudioClip somAoSumir;

            [Header("Luz propria")]
            [Tooltip("Brilho atras do personagem (alfa 0 = sem brilho).")]
            public Color brilho = Color.clear;
            public float tamanhoDoBrilho = 1.6f;
        }

        [System.Serializable]
        public class Luz
        {
            [Tooltip("Centro, a partir do centro-base da imagem, em px.")]
            public Vector2 posicao;
            public Vector2 tamanho = new Vector2(400f, 400f);
            public Color cor = new Color(1f, 0.9f, 0.6f, 0.3f);
            [Tooltip("Quanto a luz respira (0 = fixa).")]
            public float pulso = 0.25f;
            public float surgir;
        }

        [System.Serializable]
        public class Quadro
        {
            [Tooltip("Nome do capitulo, na fonte gotica, acima da legenda.")]
            public string capitulo;
            [TextArea(2, 4)] public string texto;
            [Tooltip("Segundos de cena antes de a legenda comecar (o momento acontece primeiro).")]
            public float atrasoDoTexto = 0.4f;
            public FundoDeCena.Camada[] camadas;
            public Color corDeBase = Color.clear;
            [Tooltip("Veu por cima do cenario (e debaixo dos atores): o clima do capitulo.")]
            public Color veu = new Color(0.02f, 0.01f, 0.05f, 0.3f);
            public Ator[] atores;
            public Luz[] luzes;
            [Tooltip("Particulas subindo (almas, vaga-lumes). Sem sprite = brilho redondo.")]
            public Sprite alma;
            public int quantidadeDeAlmas;
            public Color corDaAlma = new Color(1f, 0.75f, 0.9f, 1f);
            public Vector2 tamanhoDaAlma = new Vector2(18f, 30f);
            [Tooltip("Faixas de nevoa rente ao chao (alfa 0 = sem nevoa).")]
            public Color nevoa = Color.clear;
            [Tooltip("Instantes (s) de relampago.")]
            public float[] relampagos;
            public AudioClip somDoRelampago;
            [Tooltip("Deslizar lento de camera, px/s.")]
            public float panoramica = 6f;
        }

        [SerializeField] private Quadro[] quadros;
        [SerializeField] private string titulo = "Ecos de Aldenor";
        [SerializeField] private string subtitulo = "As almas ecoam nas trevas";
        [SerializeField] private string proximaCena = "Tutorial";
        [SerializeField] private Sprite almaDoTitulo;

        [Header("Ritmo")]
        [SerializeField] private float letrasPorSegundo = 38f;
        [SerializeField] private float tempoDeFade = 0.9f;
        [Tooltip("Espera depois da frase completa, alem de um tempo proporcional ao tamanho dela.")]
        [SerializeField] private float esperaMinima = 2.4f;

        private const float AlturaDaLegenda = 230f;
        private const float FaixaDeCima = 60f;

        // Estado vivo de cada personagem.
        private class AtorVivo
        {
            public Ator dados;
            public RectTransform rt;
            public Image img;
            public Image brilho;
            public CanvasGroup grupo;
            public int painel;
            public float relogio;
            public int quadro;
            public bool naEntrada;
            public bool sumiu;
        }

        private class Painel
        {
            public CanvasGroup grupo;
            public RectTransform conteudo;
            public Image clarao;
            public readonly List<(Image img, Luz luz)> luzes = new List<(Image, Luz)>();
            public readonly List<(RectTransform rt, Image img, float vel, float fase)> nevoa =
                new List<(RectTransform, Image, float, float)>();
            public int proximoRelampago;
            public float tempo;
        }

        private readonly List<Painel> paineis = new List<Painel>();
        private readonly List<AtorVivo> atores = new List<AtorVivo>();
        private readonly List<(RectTransform rt, Image img, Vector2 vel, float vida, float t)> fagulhas =
            new List<(RectTransform, Image, Vector2, float, float)>();

        private Text legenda;
        private Text capitulo;
        private CanvasGroup grupoDoCapitulo;
        private Text dica;
        private RectTransform botaoPular;
        private bool avancar;
        private bool saindo;

        private void Start()
        {
            Time.timeScale = 1f;
            UIFactory.GarantirEventSystem();
            Montar();
            StartCoroutine(Contar());
        }

        // ------------------------------------------------------------------
        // Montagem
        // ------------------------------------------------------------------

        private void Montar()
        {
            var canvas = UIFactory.CriarCanvas("PrologoCanvas", 10);
            var raiz = (RectTransform)canvas.transform;

            Fixo(UIFactory.Painel(raiz, "Preto", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));

            // Area da imagem, entre as duas faixas de cinema, com mascara.
            var imagem = new GameObject("Imagem", typeof(RectTransform)).GetComponent<RectTransform>();
            imagem.SetParent(raiz, false);
            imagem.anchorMin = Vector2.zero;
            imagem.anchorMax = Vector2.one;
            imagem.offsetMin = new Vector2(0f, AlturaDaLegenda);
            imagem.offsetMax = new Vector2(0f, -FaixaDeCima);
            imagem.gameObject.AddComponent<RectMask2D>();

            if (quadros != null)
                for (int i = 0; i < quadros.Length; i++) paineis.Add(MontarPainel(imagem, quadros[i], i));

            // Vinheta por cima de todos os capitulos: fecha o olhar no centro.
            var vinheta = UIFactory.Painel(imagem, "Vinheta", new Color(1f, 1f, 1f, 0.85f),
                                           Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var vimg = vinheta.GetComponent<Image>();
            vimg.sprite = RuntimeSprites.Vinheta();
            vimg.raycastTarget = false;

            MontarMolduras(raiz);
            MontarLegenda(raiz);

            var pular = UIFactory.Botao(raiz, "Pular", "Pular  (Esc)", 20, new Vector2(200f, 44f), Vector2.zero, Pular);
            botaoPular = (RectTransform)pular.transform;
            botaoPular.anchorMin = botaoPular.anchorMax = new Vector2(1f, 1f);
            botaoPular.pivot = new Vector2(1f, 1f);
            botaoPular.anchoredPosition = new Vector2(-24f, -8f);
        }

        /// <summary>
        /// Fios dourados nas bordas da imagem, com losangos no fio de baixo - o
        /// mesmo ouro das outras telas, com cara de iluminura.
        /// </summary>
        private void MontarMolduras(RectTransform raiz)
        {
            var ouro = UIFactory.CorAcento;
            var ouroFraco = new Color(ouro.r, ouro.g, ouro.b, 0.35f);

            Fixo(UIFactory.Painel(raiz, "FioDeBaixo", ouro, Vector2.zero, new Vector2(1f, 0f),
                                  new Vector2(0f, AlturaDaLegenda - 2f), new Vector2(0f, AlturaDaLegenda + 1f)));
            Fixo(UIFactory.Painel(raiz, "FioDeBaixo2", ouroFraco, Vector2.zero, new Vector2(1f, 0f),
                                  new Vector2(0f, AlturaDaLegenda - 8f), new Vector2(0f, AlturaDaLegenda - 7f)));
            Fixo(UIFactory.Painel(raiz, "FioDeCima", ouro, new Vector2(0f, 1f), Vector2.one,
                                  new Vector2(0f, -FaixaDeCima - 1f), new Vector2(0f, -FaixaDeCima + 2f)));

            foreach (float x in new[] { -160f, 0f, 160f })
            {
                float lado = x == 0f ? 18f : 10f;
                var losango = UIFactory.Caixa(raiz, "Losango", new Vector2(lado, lado), Vector2.zero);
                losango.anchorMin = losango.anchorMax = new Vector2(0.5f, 0f);
                losango.anchoredPosition = new Vector2(x, AlturaDaLegenda);
                losango.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var img = losango.gameObject.AddComponent<Image>();
                img.sprite = RuntimeSprites.Solido();
                img.color = x == 0f ? ouro : ouroFraco;
                img.raycastTarget = false;
            }
        }

        private void MontarLegenda(RectTransform raiz)
        {
            capitulo = UIFactory.Titulo(raiz, "Capitulo", "", 38, UIFactory.CorAcento, new Vector2(1200f, 50f), Vector2.zero);
            var crt = capitulo.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, AlturaDaLegenda - 46f);
            grupoDoCapitulo = capitulo.gameObject.AddComponent<CanvasGroup>();
            grupoDoCapitulo.alpha = 0f;

            legenda = UIFactory.Texto(raiz, "Legenda", "", 32, UIFactory.CorTexto, TextAnchor.UpperCenter,
                                      new Vector2(1400f, 120f), Vector2.zero);
            var lrt = legenda.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 92f);
            legenda.supportRichText = true;
            legenda.lineSpacing = 1.15f;

            dica = UIFactory.Texto(raiz, "Dica", "Espaço para continuar", 18, UIFactory.CorTextoFraco,
                                   TextAnchor.MiddleRight, new Vector2(500f, 30f), Vector2.zero);
            var drt = dica.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 0f);
            drt.pivot = new Vector2(1f, 0.5f);
            drt.anchoredPosition = new Vector2(-36f, 24f);
            dica.color = new Color(1f, 1f, 1f, 0f);
        }

        private Painel MontarPainel(RectTransform pai, Quadro q, int indice)
        {
            var p = new Painel();
            var rt = Esticar(new GameObject("Capitulo" + (indice + 1), typeof(RectTransform)).GetComponent<RectTransform>(), pai);
            p.grupo = rt.gameObject.AddComponent<CanvasGroup>();
            p.grupo.alpha = 0f;
            p.grupo.blocksRaycasts = false;

            // Conteudo: o que a "camera" desliza.
            p.conteudo = Esticar(new GameObject("Conteudo", typeof(RectTransform)).GetComponent<RectTransform>(), rt);

            var cenario = Esticar(new GameObject("Cenario", typeof(RectTransform)).GetComponent<RectTransform>(), p.conteudo);
            cenario.gameObject.AddComponent<FundoDeCena>().Configurar(q.camadas, q.corDeBase, q.veu);

            if (q.luzes != null)
                foreach (var luz in q.luzes)
                {
                    var img = ImagemSolta(p.conteudo, "Luz", RuntimeSprites.Brilho(), luz.cor, luz.tamanho, luz.posicao);
                    p.luzes.Add((img, luz));
                }

            if (q.quantidadeDeAlmas > 0)
            {
                var almas = Esticar(new GameObject("Almas", typeof(RectTransform)).GetComponent<RectTransform>(), p.conteudo);
                almas.gameObject.AddComponent<AlmasFlutuando>().Configurar(q.alma != null ? q.alma : RuntimeSprites.Brilho(),
                                                                           q.quantidadeDeAlmas, q.corDaAlma, q.tamanhoDaAlma);
            }

            if (q.atores != null)
                foreach (var a in q.atores) MontarAtor(p.conteudo, a, indice);

            if (q.nevoa.a > 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    var tam = new Vector2(Random.Range(800f, 1300f), Random.Range(140f, 220f));
                    var pos = new Vector2(Random.Range(-1000f, 1000f), Random.Range(10f, 160f));
                    var img = ImagemSolta(p.conteudo, "Nevoa", RuntimeSprites.Brilho(), q.nevoa, tam, pos);
                    p.nevoa.Add((img.rectTransform, img, Random.Range(-18f, 18f), Random.Range(0f, 6f)));
                }
            }

            // Clarao do relampago: por cima do capitulo inteiro.
            p.clarao = UIFactory.Painel(rt, "Relampago", new Color(0.85f, 0.9f, 1f, 0f),
                                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
            p.clarao.raycastTarget = false;
            return p;
        }

        private void MontarAtor(RectTransform pai, Ator a, int painel)
        {
            var primeiro = (a.entrada != null && a.entrada.Length > 0) ? a.entrada[0]
                         : (a.quadros != null && a.quadros.Length > 0 ? a.quadros[0] : null);
            if (primeiro == null) return;

            var vivo = new AtorVivo { dados = a, painel = painel, naEntrada = a.entrada != null && a.entrada.Length > 0 };
            var tam = new Vector2(primeiro.rect.width * a.zoom, primeiro.rect.height * a.zoom);

            if (a.brilho.a > 0f)
                vivo.brilho = ImagemSolta(pai, a.nome + "_Brilho", RuntimeSprites.Brilho(), a.brilho,
                                          tam * a.tamanhoDoBrilho, a.posicao + new Vector2(0f, tam.y * 0.45f));

            var rt = new GameObject(a.nome, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(pai, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = tam;
            rt.anchoredPosition = a.posicao;
            if (a.espelhar) rt.localScale = new Vector3(-1f, 1f, 1f);

            vivo.rt = rt;
            vivo.img = rt.gameObject.AddComponent<Image>();
            vivo.img.sprite = primeiro;
            vivo.img.color = a.cor;
            vivo.img.raycastTarget = false;
            vivo.grupo = rt.gameObject.AddComponent<CanvasGroup>();
            vivo.grupo.alpha = a.surgir > 0f ? 0f : 1f;
            if (vivo.brilho != null && a.surgir > 0f) vivo.brilho.color = Color.clear;
            atores.Add(vivo);
        }

        private static RectTransform Esticar(RectTransform rt, Transform pai)
        {
            rt.SetParent(pai, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void Fixo(RectTransform rt) => rt.GetComponent<Image>().raycastTarget = false;

        private static Image ImagemSolta(RectTransform pai, string nome, Sprite sprite, Color cor, Vector2 tamanho, Vector2 posicao)
        {
            var rt = new GameObject(nome, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(pai, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = tamanho;
            rt.anchoredPosition = posicao;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = cor;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------------
        // Conducao
        // ------------------------------------------------------------------

        private void Update()
        {
            if (saindo) return;

            if (Input.GetKeyDown(KeyCode.Escape)) { Pular(); return; }

            bool clique = Input.GetMouseButtonDown(0) && !SobreOBotaoPular();
            if (clique || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
                avancar = true;

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < paineis.Count; i++)
                if (paineis[i].grupo.alpha > 0f) AtualizarPainel(i, dt);

            // Cada personagem so vive enquanto o capitulo dele esta na tela: o
            // relogio dele e o do capitulo, nao o da cena.
            foreach (var a in atores)
                if (a.rt != null && paineis[a.painel].grupo.alpha > 0f) AtualizarAtor(a, dt);

            AtualizarFagulhas(dt);
        }

        private void AtualizarPainel(int i, float dt)
        {
            var p = paineis[i];
            var q = quadros[i];
            p.tempo += dt;

            p.conteudo.anchoredPosition = new Vector2(-q.panoramica * p.tempo, 0f);

            foreach (var (img, luz) in p.luzes)
            {
                float entrar = Mathf.Clamp01((p.tempo - luz.surgir) / 1f);
                float respira = 1f - luz.pulso + luz.pulso * Mathf.Sin(p.tempo * 2.3f + luz.posicao.x * 0.01f);
                img.color = new Color(luz.cor.r, luz.cor.g, luz.cor.b, luz.cor.a * respira * entrar);
            }

            foreach (var (rt, img, vel, fase) in p.nevoa)
            {
                var pos = rt.anchoredPosition;
                pos.x += vel * dt;
                if (pos.x > 1300f) pos.x = -1300f;
                if (pos.x < -1300f) pos.x = 1300f;
                rt.anchoredPosition = pos;
                float a = q.nevoa.a * (0.6f + 0.4f * Mathf.Sin(p.tempo * 0.8f + fase));
                img.color = new Color(q.nevoa.r, q.nevoa.g, q.nevoa.b, a);
            }

            // Relampago: piscadas brancas e o trovao.
            if (q.relampagos != null && p.proximoRelampago < q.relampagos.Length &&
                p.tempo >= q.relampagos[p.proximoRelampago])
            {
                p.proximoRelampago++;
                AudioManager.Instance?.PlaySfx(q.somDoRelampago);
                StartCoroutine(Relampejar(p.clarao));
            }
        }

        private IEnumerator Relampejar(Image clarao)
        {
            float[] curva = { 0.85f, 0.15f, 0.7f, 0.3f, 0f };
            foreach (float a in curva)
            {
                clarao.color = new Color(clarao.color.r, clarao.color.g, clarao.color.b, a);
                yield return new WaitForSecondsRealtime(0.06f);
            }
        }

        private void AtualizarAtor(AtorVivo a, float dt)
        {
            var d = a.dados;
            float t = paineis[a.painel].tempo;

            // Aparecer e desfazer-se.
            float alvo = 1f;
            if (d.surgir > 0f)
            {
                alvo = Mathf.Clamp01((t - d.surgir) / Mathf.Max(0.01f, d.fade));
                if (t >= d.surgir && t - dt < d.surgir) AudioManager.Instance?.PlaySfx(d.somAoSurgir);
            }

            if (d.sumir >= 0f && t >= d.sumir)
            {
                alvo = Mathf.Min(alvo, 1f - Mathf.Clamp01((t - d.sumir) / Mathf.Max(0.01f, d.fade)));
                if (!a.sumiu)
                {
                    a.sumiu = true;
                    AudioManager.Instance?.PlaySfx(d.somAoSumir);
                    if (d.almasAoSumir) SoltarAlmas(a);
                }
            }
            a.grupo.alpha = alvo;
            if (a.brilho != null)
            {
                float respira = 0.8f + 0.2f * Mathf.Sin(t * 2.5f);
                a.brilho.color = new Color(d.brilho.r, d.brilho.g, d.brilho.b, d.brilho.a * alvo * respira);
            }
            if (alvo <= 0f) return;

            // Quadros: o gesto de entrada uma vez, depois o ciclo.
            var clipe = a.naEntrada ? d.entrada : d.quadros;
            float fps = a.naEntrada ? d.fpsEntrada : d.fps;
            if (clipe != null && clipe.Length > 0)
            {
                a.relogio += dt;
                float intervalo = 1f / Mathf.Max(0.01f, fps);
                while (a.relogio >= intervalo)
                {
                    a.relogio -= intervalo;
                    a.quadro++;
                    if (a.quadro < clipe.Length) continue;

                    if (a.naEntrada && d.quadros != null && d.quadros.Length > 0)
                    {
                        a.naEntrada = false;
                        a.quadro = 0;
                        clipe = d.quadros;
                        intervalo = 1f / Mathf.Max(0.01f, d.fps);
                    }
                    else a.quadro = (!a.naEntrada && d.repetir) ? 0 : clipe.Length - 1;
                }
                a.img.sprite = clipe[Mathf.Clamp(a.quadro, 0, clipe.Length - 1)];
            }

            // Movimento.
            var pos = a.rt.anchoredPosition;
            pos.x += d.andar * dt;
            pos.y = d.posicao.y + (d.flutuar > 0f ? Mathf.Sin(t * 1.7f + d.posicao.x * 0.01f) * d.flutuar : 0f);
            a.rt.anchoredPosition = pos;
            if (a.brilho != null)
                a.brilho.rectTransform.anchoredPosition = pos + new Vector2(0f, a.rt.sizeDelta.y * 0.45f);
        }

        /// <summary>
        /// Almas saindo de um corpo que se desfaz: luzinhas que sobem depressa,
        /// balancando, e se apagam - "as almas foram arrancadas dos corpos".
        /// </summary>
        private void SoltarAlmas(AtorVivo a)
        {
            var pai = (RectTransform)a.rt.parent;
            var centro = a.rt.anchoredPosition + new Vector2(0f, a.rt.sizeDelta.y * 0.5f);
            for (int i = 0; i < 7; i++)
            {
                float t = Random.Range(14f, 30f);
                var img = ImagemSolta(pai, "AlmaArrancada", RuntimeSprites.Brilho(), new Color(1f, 0.75f, 0.95f, 0.9f),
                                      new Vector2(t, t), centro + Random.insideUnitCircle * 30f);
                var vel = new Vector2(Random.Range(-40f, 40f), Random.Range(120f, 220f));
                fagulhas.Add((img.rectTransform, img, vel, Random.Range(1.4f, 2.4f), 0f));
            }
        }

        private void AtualizarFagulhas(float dt)
        {
            for (int i = fagulhas.Count - 1; i >= 0; i--)
            {
                var (rt, img, vel, vida, t) = fagulhas[i];
                if (rt == null) { fagulhas.RemoveAt(i); continue; }
                t += dt;
                if (t >= vida) { Destroy(rt.gameObject); fagulhas.RemoveAt(i); continue; }
                rt.anchoredPosition += new Vector2(vel.x + Mathf.Sin(t * 6f) * 30f, vel.y) * dt;
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, 0.9f * (1f - t / vida));
                fagulhas[i] = (rt, img, vel, vida, t);
            }
        }

        private bool SobreOBotaoPular()
        {
            return botaoPular != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(botaoPular, Input.mousePosition, null);
        }

        private IEnumerator Contar()
        {
            yield return null;   // os cenarios se montam no Start deles

            for (int i = 0; i < paineis.Count; i++)
            {
                var q = quadros[i];

                // Cross-fade: o capitulo novo surge POR CIMA do anterior, e o
                // nome dele aparece junto.
                StartCoroutine(UIFactory.Desvanecer(grupoDoCapitulo, grupoDoCapitulo.alpha, 0f, 0.3f));
                yield return UIFactory.Desvanecer(paineis[i].grupo, 0f, 1f, tempoDeFade);
                if (i > 0) paineis[i - 1].grupo.gameObject.SetActive(false);
                capitulo.text = q.capitulo ?? "";
                StartCoroutine(UIFactory.Desvanecer(grupoDoCapitulo, 0f, 1f, 0.6f));

                // O momento acontece antes da frase (pode ser encurtado).
                avancar = false;
                for (float t = 0f; t < q.atrasoDoTexto && !avancar; t += Time.unscaledDeltaTime) yield return null;

                string texto = q.texto ?? "";
                yield return Escrever(texto);
                yield return EsperarAvanco(esperaMinima + texto.Length / 32f);
                legenda.text = "";
            }

            StartCoroutine(UIFactory.Desvanecer(grupoDoCapitulo, grupoDoCapitulo.alpha, 0f, 0.4f));
            yield return Titulo();
            Ir();
        }

        /// <summary>
        /// Maquina de escrever. O resto da frase ja esta no texto, so que
        /// transparente: assim a quebra de linha e decidida pela frase inteira
        /// desde a primeira letra, e as palavras nao pulam de linha enquanto
        /// aparecem.
        /// </summary>
        private IEnumerator Escrever(string texto)
        {
            avancar = false;
            float n = 0f;
            while (n < texto.Length)
            {
                if (avancar) { avancar = false; break; }
                n += letrasPorSegundo * Time.unscaledDeltaTime;
                int k = Mathf.Clamp(Mathf.FloorToInt(n), 0, texto.Length);
                legenda.text = texto.Substring(0, k) + "<color=#00000000>" + texto.Substring(k) + "</color>";
                yield return null;
            }
            legenda.text = texto;
        }

        private IEnumerator EsperarAvanco(float limite)
        {
            avancar = false;
            for (float t = 0f; t < limite && !avancar; t += Time.unscaledDeltaTime)
            {
                // A dica so aparece com a frase completa, piscando devagar.
                float a = 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
                dica.color = new Color(UIFactory.CorTextoFraco.r, UIFactory.CorTextoFraco.g,
                                       UIFactory.CorTextoFraco.b, Mathf.Min(1f, t * 2f) * a);
                yield return null;
            }
            avancar = false;
            dica.color = new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>
        /// Cartao final: o nome do jogo no escuro, com uma luz atras e as almas
        /// subindo - o mesmo motivo do menu, fechando a historia nele.
        /// </summary>
        private IEnumerator Titulo()
        {
            var canvas = UIFactory.CriarCanvas("TituloFinal", 20);
            var raiz = (RectTransform)canvas.transform;
            var grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
            Fixo(UIFactory.Painel(raiz, "Preto", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));

            var luz = UIFactory.Caixa(raiz, "Luz", new Vector2(1500f, 700f), new Vector2(0f, 30f));
            var limg = luz.gameObject.AddComponent<Image>();
            limg.sprite = RuntimeSprites.Brilho();
            limg.color = new Color(0.55f, 0.25f, 0.6f, 0.45f);
            limg.raycastTarget = false;

            var almas = Esticar(new GameObject("Almas", typeof(RectTransform)).GetComponent<RectTransform>(), raiz);
            almas.gameObject.AddComponent<AlmasFlutuando>().Configurar(almaDoTitulo != null ? almaDoTitulo : RuntimeSprites.Brilho(),
                                                                       14, new Color(1f, 0.75f, 0.9f, 1f), new Vector2(18f, 34f));

            var nome = UIFactory.Titulo(raiz, "Nome", titulo, 150, UIFactory.CorTitulo, new Vector2(1700f, 190f), new Vector2(0f, 50f));
            var sombra = nome.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.25f, 0f, 0.1f, 0.9f);
            sombra.effectDistance = new Vector2(5f, -5f);

            var ouro = UIFactory.CorAcento;
            UIFactory.Linha(raiz, new Vector2(520f, 2f), new Vector2(0f, -45f), ouro);
            var losango = UIFactory.Caixa(raiz, "Losango", new Vector2(16f, 16f), new Vector2(0f, -45f));
            losango.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var limg2 = losango.gameObject.AddComponent<Image>();
            limg2.sprite = RuntimeSprites.Solido();
            limg2.color = ouro;

            UIFactory.Texto(raiz, "Subtitulo", subtitulo, 30, ouro, TextAnchor.MiddleCenter,
                            new Vector2(1200f, 40f), new Vector2(0f, -95f));

            yield return UIFactory.Desvanecer(grupo, 0f, 1f, 1.4f);
            yield return EsperarAvanco(2.6f);
        }

        public void Pular()
        {
            if (saindo) return;
            StopAllCoroutines();
            Ir();
        }

        private void Ir()
        {
            if (saindo) return;
            saindo = true;
            if (SceneController.Instance != null) SceneController.Instance.LoadScene(proximaCena);
            else SceneManager.LoadScene(proximaCena);
        }
    }
}
