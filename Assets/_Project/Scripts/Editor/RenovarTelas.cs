using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// RENOVACAO DAS TELAS - entrada do chefe, telas finais, menu e pausa.
    ///
    ///   - Arena: a luta comeca em silencio e com a barra escondida; um
    ///     gatilho dispara a apresentacao da Vigilia (IntroDoChefe).
    ///   - Vitoria e Derrota: fundo com a arte do jogo, o Ren animado, resumo
    ///     da partida (almas, mortes, tempo, dificuldade); a vitoria leva aos
    ///     creditos; "Game Over" vira "Fim de Jogo".
    ///   - Menu: cemiterio com a lua deslizando ao fundo e almas subindo;
    ///     botoes de Opcoes e Controles; "Creditos" com acento; um som de
    ///     clique por botao (havia dois).
    ///   - Pausa: botoes no estilo do menu (eram pretos), moldura, e as opcoes
    ///     Reiniciar fase, Opcoes e Controles.
    ///
    /// Repetivel: identifica tudo por nome e refaz sem duplicar.
    /// </summary>
    public static class RenovarTelas
    {
        const string Cemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/Environment/";
        const string IgrejaFundo = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/ENVIRONMENT/backgrounds.png";
        const string HeroAnim = "Assets/GameAssets/Hero Knight - Pixel Art/Animations/";
        const string SomEncontro = "Assets/GameAssets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/55_Encounter_02.wav";
        const string PrefabHud = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";

        static readonly Color CorBotao = new Color(0.14f, 0.10f, 0.18f, 0.92f);
        static readonly Color CorBotaoDestaque = new Color(0.35f, 0.18f, 0.45f, 1f);
        static readonly Color CorPainel = new Color(0.07f, 0.05f, 0.10f, 0.94f);
        static readonly Color CorAcento = new Color(0.95f, 0.80f, 0.42f, 1f);

        [MenuItem("Ecos de Aldenor/Renovar telas (menu, finais, pausa, chefe)")]
        public static void Renovar()
        {
            var log = new StringBuilder("Renovacao das telas:\n");
            Abrir("FinalPhase", log, Arena);
            Abrir("MainMenu", log, Menu);
            Abrir("VictoryScreen", log, Vitoria);
            Abrir("GameOverScreen", log, Derrota);
            Abrir("Credits", log, Creditos);
            Pausa(log);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void Abrir(string nome, StringBuilder log, System.Func<string> acao)
        {
            var cena = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + nome + ".unity", OpenSceneMode.Single);
            string feito = acao();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            log.AppendLine("  " + nome + ": " + feito);
        }

        // ------------------------------------------------------------------
        // 11. Entrada do chefe
        // ------------------------------------------------------------------

        static string Arena()
        {
            var chefe = Object.FindAnyObjectByType<EcosDeAldenor.Enemies.AVigilia>();
            var barra = Object.FindAnyObjectByType<BossHealthBar>();
            var trilha = Object.FindAnyObjectByType<EcosDeAldenor.Systems.SceneMusicPlayer>();
            if (chefe == null) return "chefe nao encontrado";

            // A arena comeca em silencio: a trilha so entra na apresentacao.
            if (trilha != null)
            {
                var so = new SerializedObject(trilha);
                so.FindProperty("tocarAoIniciar").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var go = GameObject.Find("IntroDoChefe");
            if (go == null) go = new GameObject("IntroDoChefe");

            // Gatilho a 10 u do chefe: bem antes do raio em que ele percebe o
            // jogador (5 u), para ele nunca atacar no meio da cena.
            go.transform.position = new Vector3(chefe.transform.position.x - 10f, 1.5f, 0f);
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 10f);

            var intro = go.GetComponent<EcosDeAldenor.Enemies.IntroDoChefe>();
            if (intro == null) intro = go.AddComponent<EcosDeAldenor.Enemies.IntroDoChefe>();
            var si = new SerializedObject(intro);
            si.FindProperty("chefe").objectReferenceValue = chefe;
            si.FindProperty("barra").objectReferenceValue = barra;
            si.FindProperty("trilha").objectReferenceValue = trilha;
            si.FindProperty("somDeEncontro").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(SomEncontro);
            si.ApplyModifiedPropertiesWithoutUndo();
            return "gatilho de apresentacao em x=" + go.transform.position.x.ToString("0.0") + ", trilha em silencio ate la";
        }

        // ------------------------------------------------------------------
        // 13. Menu principal
        // ------------------------------------------------------------------

        static string Menu()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var gerente = Object.FindAnyObjectByType<MainMenuManager>();

            Desligar("Background");
            Desligar("Vignette");
            FundoDoCemiterio(canvas.transform, new Color(1f, 1f, 1f), 0.42f, true);

            var fragmento = GameObject.Find("TitleFragment");
            var almas = Garantir<AlmasFlutuando>(canvas.transform, "AlmasFlutuando", 1);
            var sa = new SerializedObject(almas);
            sa.FindProperty("sprite").objectReferenceValue = fragmento != null ? fragmento.GetComponent<Image>().sprite : null;
            sa.ApplyModifiedPropertiesWithoutUndo();

            // Cinco botoes: os tres que existiam e dois novos.
            var jogar = GameObject.Find("PlayButton").GetComponent<Button>();
            var creditos = GameObject.Find("CreditsButton").GetComponent<Button>();
            var sair = GameObject.Find("QuitButton").GetComponent<Button>();
            var opcoes = Clonar(creditos, "OptionsButton", "Opções");
            var controles = Clonar(creditos, "ControlsButton", "Controles");
            Rotulo(creditos, "Créditos");

            Religar(jogar, gerente.OnPlayClicked);
            Religar(opcoes, gerente.OnOptionsClicked);
            Religar(controles, gerente.OnControlsClicked);
            Religar(creditos, gerente.OnCreditsClicked);
            Religar(sair, gerente.OnQuitClicked);

            var ordem = new[] { jogar, opcoes, controles, creditos, sair };
            for (int i = 0; i < ordem.Length; i++)
            {
                var rt = (RectTransform)ordem[i].transform;
                rt.sizeDelta = new Vector2(300f, 56f);
                rt.anchoredPosition = new Vector2(0f, 90f - i * 70f);
            }

            Posicao("TitleFragment", 340f);
            Posicao("Title", 255f);
            Posicao("Subtitle", 172f);

            var seletor = Object.FindAnyObjectByType<DifficultySelector>();
            if (seletor != null)
            {
                var ss = new SerializedObject(seletor);
                ss.FindProperty("alturaNoMenu").floatValue = -300f;
                ss.ApplyModifiedPropertiesWithoutUndo();
            }
            return "fundo do cemiterio, almas, Opcoes e Controles, Créditos com acento";
        }

        // ------------------------------------------------------------------
        // 12. Telas finais
        // ------------------------------------------------------------------

        static string Vitoria()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var gerente = Garantir<MainMenuManager>(canvas.transform, "MainMenuManager", -1);

            Desligar("Background");
            FundoDaIgreja(canvas.transform, 0.5f);
            Ren(canvas.transform, "HeroKnight_Idle.anim", true, new Vector2(-560f, 118f), 0f);

            Centralizar("Title", 150f);
            Posicao("V_Frag", 262f);
            Posicao("V_Sub", 50f);
            Resumo(canvas.transform, -80f);

            var menu = GameObject.Find("MainMenuButton").GetComponent<Button>();
            var creditos = Clonar(menu, "CreditsButton", "Créditos");
            Religar(creditos, gerente.OnCreditsClicked);
            Religar(menu, gerente.OnBackToMenuClicked);
            Lado(creditos, menu, -230f);
            return "igreja ao fundo, Ren, resumo, botao de creditos";
        }

        static string Derrota()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var gerente = Garantir<MainMenuManager>(canvas.transform, "MainMenuManager", -1);

            Desligar("Background");
            FundoDoCemiterio(canvas.transform, new Color(0.85f, 0.5f, 0.55f), 0.55f, false);
            Ren(canvas.transform, "HeroKnight_Death.anim", false, new Vector2(-470f, 150f), 0.35f);

            var titulo = GameObject.Find("Title");
            if (titulo != null) titulo.GetComponent<Text>().text = "Fim de Jogo";
            Centralizar("Title", 150f);
            Posicao("GO_Sub", 50f);
            Resumo(canvas.transform, -80f);

            var tentar = GameObject.Find("RetryButton").GetComponent<Button>();
            var menu = GameObject.Find("MainMenuButton").GetComponent<Button>();
            Religar(tentar, gerente.OnRetryClicked);
            Religar(menu, gerente.OnBackToMenuClicked);
            Lado(tentar, menu, -230f);
            return "cemiterio avermelhado, Ren caido, resumo, \"Fim de Jogo\"";
        }

        static string Creditos()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            Desligar("BackgroundPanel");
            FundoDaIgreja(canvas.transform, 0.78f);

            // O Voltar estava dentro do layout vertical com altura 0: sem caixa,
            // so a palavra colada na ultima linha. Sai do layout e vira um
            // botao do menu logo abaixo da lista.
            var voltar = GameObject.Find("BackButton").GetComponent<Button>();
            var le = voltar.GetComponent<LayoutElement>();
            if (le == null) le = voltar.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            var rt = (RectTransform)voltar.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 56f);
            rt.anchoredPosition = new Vector2(0f, -70f);
            Estilo(voltar);
            return "igreja ao fundo, botao Voltar com caixa";
        }

        // ------------------------------------------------------------------
        // 14. Pausa (prefab do HUD)
        // ------------------------------------------------------------------

        static void Pausa(StringBuilder log)
        {
            var raiz = PrefabUtility.LoadPrefabContents(PrefabHud);
            var gerente = raiz.GetComponentInChildren<PauseManager>(true);
            var painel = raiz.transform.Find("PausePanel");

            // Por ultimo na hierarquia: desenha por cima dos coracoes e da barra.
            painel.SetAsLastSibling();
            painel.GetComponent<Image>().color = new Color(0.02f, 0.01f, 0.04f, 0.86f);

            var moldura = painel.Find("Moldura");
            if (moldura == null)
            {
                moldura = new GameObject("Moldura", typeof(RectTransform), typeof(Image)).transform;
                moldura.SetParent(painel, false);
            }
            moldura.SetAsFirstSibling();
            var rm = (RectTransform)moldura;
            rm.anchorMin = rm.anchorMax = new Vector2(0.5f, 0.5f);
            rm.sizeDelta = new Vector2(560f, 540f);
            rm.anchoredPosition = new Vector2(0f, 25f);
            moldura.GetComponent<Image>().color = CorPainel;
            Fio(moldura, "FioTopo", 270f);
            Fio(moldura, "FioBase", -270f);

            var titulo = (RectTransform)painel.Find("PauseTitle");
            titulo.anchoredPosition = new Vector2(0f, 205f);

            var continuar = painel.Find("ResumeButton").GetComponent<Button>();
            var menu = painel.Find("MainMenuButton").GetComponent<Button>();
            var reiniciar = Clonar(continuar, "RestartButton", "Reiniciar fase");
            var opcoes = Clonar(continuar, "OptionsButton", "Opções");
            var controles = Clonar(continuar, "ControlsButton", "Controles");

            Religar(continuar, gerente.OnResumeButtonClicked);
            Religar(reiniciar, gerente.OnRestartClicked);
            Religar(opcoes, gerente.OnOptionsClicked);
            Religar(controles, gerente.OnControlsClicked);
            Religar(menu, gerente.OnMainMenuButtonClicked);

            var ordem = new[] { continuar, reiniciar, opcoes, controles, menu };
            for (int i = 0; i < ordem.Length; i++)
            {
                var rt = (RectTransform)ordem[i].transform;
                rt.sizeDelta = new Vector2(300f, 56f);
                rt.anchoredPosition = new Vector2(0f, 110f - i * 70f);
                Estilo(ordem[i]);
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabHud);
            PrefabUtility.UnloadPrefabContents(raiz);
            log.AppendLine("  HUD: pausa com moldura, botoes no estilo do menu, Reiniciar fase / Opções / Controles");
        }

        static void Fio(Transform pai, string nome, float y)
        {
            var fio = pai.Find(nome);
            if (fio == null)
            {
                fio = new GameObject(nome, typeof(RectTransform), typeof(Image)).transform;
                fio.SetParent(pai, false);
            }
            var rt = (RectTransform)fio;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 3f);
            rt.anchoredPosition = new Vector2(0f, y);
            fio.GetComponent<Image>().color = CorAcento;
        }

        // ------------------------------------------------------------------
        // Pecas compartilhadas
        // ------------------------------------------------------------------

        /// <summary>Cemiterio com a lua: ceu, montanhas e tumulos em camadas.</summary>
        static void FundoDoCemiterio(Transform canvas, Color cor, float escuro, bool desliza)
        {
            var fundo = Garantir<FundoDeCena>(canvas, "FundoDeCena", 0);
            var so = new SerializedObject(fundo);
            var camadas = so.FindProperty("camadas");
            camadas.arraySize = 3;
            Camada(camadas.GetArrayElementAtIndex(0), "Ceu", new[] { Primeiro(Cemiterio + "background.png") }, 5f, 0f, true, 0f, cor);
            Camada(camadas.GetArrayElementAtIndex(1), "Montanhas", new[] { Primeiro(Cemiterio + "mountains.png") }, 3f, 30f, false, desliza ? 6f : 0f, cor * 0.9f);
            Camada(camadas.GetArrayElementAtIndex(2), "Tumulos", new[] { Primeiro(Cemiterio + "graveyard.png") }, 3f, 0f, false, desliza ? 14f : 0f, cor);
            so.FindProperty("escurecer").colorValue = new Color(0.02f, 0.01f, 0.05f, escuro);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Nave da igreja: janela, coluna e o altar com trono ao centro.</summary>
        static void FundoDaIgreja(Transform canvas, float escuro)
        {
            var fatias = AssetDatabase.LoadAllAssetsAtPath(IgrejaFundo).OfType<Sprite>()
                         .OrderBy(s => s.name).ToArray();   // 0 janela, 1 coluna, 2 altar, 3 gargula, 4 tocha
            var fundo = Garantir<FundoDeCena>(canvas, "FundoDeCena", 0);
            var so = new SerializedObject(fundo);
            var camadas = so.FindProperty("camadas");
            camadas.arraySize = 1;
            // Altar ao centro, janelas dos lados e as colunas fechando a nave.
            var fileira = new[] { fatias[1], fatias[0], fatias[2], fatias[0], fatias[1] };
            Camada(camadas.GetArrayElementAtIndex(0), "Nave", fileira, 5f, 0f, false, 0f, Color.white);
            so.FindProperty("corDeBase").colorValue = new Color32(39, 38, 56, 255);   // o fundo liso da propria arte
            so.FindProperty("escurecer").colorValue = new Color(0.02f, 0.01f, 0.05f, escuro);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Camada(SerializedProperty c, string nome, Sprite[] sprites, float zoom, float y,
                           bool cobrir, float velocidade, Color cor)
        {
            c.FindPropertyRelative("nome").stringValue = nome;
            var arr = c.FindPropertyRelative("sprites");
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            c.FindPropertyRelative("zoom").floatValue = zoom;
            c.FindPropertyRelative("y").floatValue = y;
            c.FindPropertyRelative("cobrirTela").boolValue = cobrir;
            c.FindPropertyRelative("velocidade").floatValue = velocidade;
            c.FindPropertyRelative("cor").colorValue = new Color(cor.r, cor.g, cor.b, 1f);
        }

        /// <summary>O Ren na tela, tocando uma das animacoes do proprio personagem.</summary>
        static void Ren(Transform canvas, string clipe, bool repetir, Vector2 posicao, float atraso)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroAnim + clipe);
            var quadros = new List<Sprite>();
            if (clip != null)
                foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    if (b.propertyName == "m_Sprite")
                        foreach (var k in AnimationUtility.GetObjectReferenceCurve(clip, b))
                            if (k.value is Sprite s) quadros.Add(s);
            if (quadros.Count == 0) return;

            var go = GameObject.Find("Ren");
            if (go == null)
            {
                go = new GameObject("Ren", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas, false);
            }
            go.transform.SetSiblingIndex(2);   // logo depois do fundo e do veu
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(quadros[0].rect.width * 5f, quadros[0].rect.height * 5f);
            rt.anchoredPosition = posicao;
            var img = go.GetComponent<Image>();
            img.sprite = quadros[0];
            img.raycastTarget = false;

            var anim = go.GetComponent<AnimacaoDeImagem>();
            if (anim == null) anim = go.AddComponent<AnimacaoDeImagem>();
            var so = new SerializedObject(anim);
            var arr = so.FindProperty("quadros");
            arr.arraySize = quadros.Count;
            for (int i = 0; i < quadros.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = quadros[i];
            so.FindProperty("fps").floatValue = 10f;
            so.FindProperty("repetir").boolValue = repetir;
            so.FindProperty("atraso").floatValue = atraso;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Resumo(Transform canvas, float y)
        {
            var r = Garantir<ResumoDaPartida>(canvas, "Resumo", -1);
            var rt = (RectTransform)r.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000f, 120f);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        /// <summary>Acha (ou cria) um filho do canvas com o componente pedido.</summary>
        static T Garantir<T>(Transform canvas, string nome, int indice) where T : Component
        {
            var existente = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            GameObject go;
            if (existente != null) go = existente.gameObject;
            else
            {
                go = canvas.Find(nome) != null ? canvas.Find(nome).gameObject : new GameObject(nome, typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                go.AddComponent<T>();
            }
            if (indice >= 0) go.transform.SetSiblingIndex(indice);
            return go.GetComponent<T>();
        }

        static Button Clonar(Button modelo, string nome, string rotulo)
        {
            var pai = modelo.transform.parent;
            var existente = pai.Find(nome);
            if (existente != null) Object.DestroyImmediate(existente.gameObject);

            var copia = Object.Instantiate(modelo.gameObject, pai);
            copia.name = nome;
            var b = copia.GetComponent<Button>();
            Rotulo(b, rotulo);
            return b;
        }

        static void Rotulo(Button b, string texto)
        {
            var t = b.GetComponentInChildren<Text>(true);
            if (t != null) t.text = texto;
        }

        /// <summary>
        /// Limpa o clique e liga um metodo so. Os botoes do menu chamavam
        /// tambem um AudioSource.Play antigo, e com o som de interface novo
        /// cada clique soava duas vezes.
        /// </summary>
        static void Religar(Button b, UnityAction acao)
        {
            while (b.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(b.onClick, 0);
            UnityEventTools.AddVoidPersistentListener(b.onClick, acao);
            EditorUtility.SetDirty(b);
        }

        /// <summary>Cores do menu principal: roxo escuro, realce roxo ao passar o mouse.</summary>
        static void Estilo(Button b)
        {
            var img = b.GetComponent<Image>();
            if (img != null) img.color = Color.white;
            var cores = b.colors;
            cores.normalColor = CorBotao;
            cores.highlightedColor = CorBotaoDestaque;
            cores.pressedColor = new Color(0.245f, 0.126f, 0.315f, 1f);
            cores.selectedColor = CorBotaoDestaque;
            cores.colorMultiplier = 1f;
            cores.fadeDuration = 0.12f;
            b.colors = cores;
        }

        /// <summary>Dois botoes lado a lado, na altura dada.</summary>
        static void Lado(Button esquerdo, Button direito, float y)
        {
            foreach (var (b, x) in new[] { (esquerdo, -165f), (direito, 165f) })
            {
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(300f, 60f);
                rt.anchoredPosition = new Vector2(x, y);
                Estilo(b);
            }
        }

        static void Desligar(string nome)
        {
            var go = GameObject.Find(nome);
            if (go != null) go.SetActive(false);
        }

        static void Posicao(string nome, float y)
        {
            var go = GameObject.Find(nome);
            if (go != null) ((RectTransform)go.transform).anchoredPosition = new Vector2(0f, y);
        }

        static void Centralizar(string nome, float y)
        {
            var go = GameObject.Find(nome);
            if (go == null) return;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        static Sprite Primeiro(string caminho)
        {
            return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
        }
    }
}
