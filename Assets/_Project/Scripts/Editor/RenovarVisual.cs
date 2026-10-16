using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// RENOVACAO VISUAL - aplica, de uma vez e de forma repetivel, a passada que
    /// tirou do jogo a cara de prototipo:
    ///
    ///   - tipografia: as duas fontes de pixel no lugar da Arial, em toda tela;
    ///   - placas do Tutorial: acentos, sem setas ASCII, com placa de fundo;
    ///   - Vitoria: o icone da alma saiu de cima do "O" do titulo;
    ///   - camera: o chao desce para o terco de baixo da tela, em vez de deixar
    ///     um terco inteiro de terra e preto aparecendo;
    ///   - altar: estatua encapuzada com chama de alma, no lugar da pedra
    ///     cinza com o mesmo sprite do item coletavel;
    ///   - entrada: portao de pilares no cemiterio e porta em arco na igreja,
    ///     no lugar do retangulo marrom;
    ///   - chao e pecas de cenario: rodam pelo "Reconstruir fases", que agora
    ///     poe a grama do cemiterio e troca a arte das pecas (ver
    ///     LevelTerrainBuilder.VestirDecoracoes).
    ///
    /// Tudo o que esta aqui le e escreve por nome e por caminho, e pode rodar
    /// de novo sem duplicar nada.
    /// </summary>
    public static class RenovarVisual
    {
        const string FonteTitulo = "Assets/_Project/Resources/Fonts/NewRocker.ttf";
        const string FonteTexto = "Assets/_Project/Resources/Fonts/PixelifySans.ttf";
        const string PastaChama = "Assets/_Project/Art/ChamaDeAlma/";
        const string Grama = "Assets/_Project/Art/Cenario/grama-cemiterio.png";
        const string TilesetCemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/Environment/tileset.png";
        const string ObjetosCemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/Environment/objects.png";
        const string TilesetIgreja = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/ENVIRONMENT/tileset.png";
        const string PrefabAltar = "Assets/_Project/Prefabs/Environment/Checkpoint.prefab";
        const string PrefabHud = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";

        static readonly string[] TodasAsCenas =
            { "MainMenu", "Tutorial", "Phase1", "Phase2", "FinalPhase", "VictoryScreen", "GameOverScreen", "Credits" };
        static readonly string[] CenasDeFase = { "Tutorial", "Phase1", "Phase2", "FinalPhase" };

        /// <summary>
        /// Altura minima da camera. Com o chao em -0,4 e meia altura de tela 5,
        /// o valor antigo (1) deixava 36% da tela abaixo do chao. Em 2,2 o chao
        /// fica a ~24% da base - o enquadramento classico de plataforma, com o
        /// espaco sobrando para cima, onde estao as plataformas e as almas.
        /// </summary>
        const float CameraMinY = 2.2f;

        [MenuItem("Ecos de Aldenor/Renovar visual")]
        public static void Renovar()
        {
            var log = new StringBuilder("Renovacao visual:\n");

            ConfigurarImportacao(log);
            AltarDeAlma(log);
            foreach (var nome in TodasAsCenas) RenovarCena(nome, log);
            FontesDoHud(log);
            AssetDatabase.SaveAssets();

            // Chao com grama, pecas de cenario no estilo do fundo e
            // redistribuicao: o mesmo caminho do menu "Reconstruir fases".
            LevelLayouts.ReconstruirTudo();

            int problemas;
            string relatorio = LevelLayouts.Relatorio(out problemas);
            log.AppendLine(problemas == 0 ? "  verificador de fases: OK" : "  verificador de fases: " + problemas + " problema(s)\n" + relatorio);
            Debug.Log(log.ToString());
        }

        // ------------------------------------------------------------------
        // Importacao das artes novas
        // ------------------------------------------------------------------

        static void ConfigurarImportacao(StringBuilder log)
        {
            foreach (var f in new[] { FonteTitulo, FonteTexto })
            {
                var imp = AssetImporter.GetAtPath(f) as TrueTypeFontImporter;
                if (imp == null) continue;
                // A de pixel quer contorno em degraus nitidos; a de titulo e um
                // desenho vetorial e serrilha se for rasterizada sem suavizacao.
                imp.fontRenderingMode = f == FonteTexto ? FontRenderingMode.HintedRaster : FontRenderingMode.Smooth;
                imp.fontTextureCase = FontTextureCase.Dynamic;
                imp.SaveAndReimport();
            }

            var pngs = new List<string> { Grama };
            for (int i = 1; i <= 4; i++) pngs.Add(PastaChama + "chama-" + i + ".png");
            foreach (var p in pngs) ImportarPixel(p);

            // O pilar do cemiterio vira peca de cenario isolada; a tela inteira
            // do tileset precisa de malha retangular para desenhar sem recorte.
            log.AppendLine("  importacao: fontes de pixel e " + pngs.Count + " sprites novos");
        }

        static void ImportarPixel(string caminho)
        {
            var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (imp == null) { Debug.LogWarning("Sem importer: " + caminho); return; }

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;

            // Malha retangular: o modo Tiled (usado na grama) precisa dela.
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // Cenas
        // ------------------------------------------------------------------

        static void RenovarCena(string nome, StringBuilder log)
        {
            var cena = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + nome + ".unity", OpenSceneMode.Single);
            var feito = new List<string>();

            int textos = TrocarFontes(nome);
            if (textos > 0) feito.Add(textos + " textos com fonte nova");

            if (nome == "Tutorial") feito.Add(PlacasDoTutorial());
            if (nome == "VictoryScreen") feito.Add(TituloDaVitoria());
            if (nome == "Credits") feito.Add(CreditarFontes());

            if (CenasDeFase.Contains(nome))
            {
                feito.Add(Camera());
                feito.Add(Entrada(nome == "FinalPhase"));
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            log.AppendLine("  " + nome + ": " + string.Join(", ", feito.Where(f => !string.IsNullOrEmpty(f))));
        }

        // --- Tipografia ---------------------------------------------------

        static bool EhTitulo(Text t)
        {
            return t.name == "Title" || t.name == "CTitle" || t.name == "PauseTitle" || t.name.EndsWith("Header");
        }

        /// <summary>
        /// Titulos na New Rocker, em caixa normal ("Ecos de Aldenor", nao
        /// "ECOS DE ALDENOR": letra gotica toda em maiuscula vira um bloco
        /// ilegivel). Todo o resto na Pixelify Sans. Negrito e italico saem: em
        /// fonte de pixel eles sao sinteticos e borram o contorno.
        /// </summary>
        static readonly Dictionary<string, string> TitulosEmCaixaNormal = new Dictionary<string, string>
        {
            { "ECOS DE ALDENOR", "Ecos de Aldenor" },
            { "VITÓRIA!", "Vitória!" },
            { "GAME OVER", "Game Over" },
            { "CRÉDITOS", "Créditos" },
            { "PAUSADO", "Pausado" },
        };

        static int TrocarFontes(string cena)
        {
            var titulo = AssetDatabase.LoadAssetAtPath<Font>(FonteTitulo);
            var texto = AssetDatabase.LoadAssetAtPath<Font>(FonteTexto);
            int n = 0;

            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(t)) continue;   // HUD: tratado no prefab
                Undo.RecordObject(t, "Fonte");

                if (EhTitulo(t))
                {
                    t.font = titulo;
                    string caixa;
                    if (TitulosEmCaixaNormal.TryGetValue(t.text, out caixa)) t.text = caixa;
                    if (t.name.EndsWith("Header")) t.fontSize = 48;
                    else t.fontSize = (cena == "MainMenu") ? 96 : 72;
                }
                else
                {
                    t.font = texto;
                    // Texto de botao: a fonte de pixel desenha mais fina que a
                    // Arial, e no tamanho antigo o rotulo sumia dentro do botao.
                    if (t.GetComponentInParent<Button>() != null) t.fontSize = 30;
                }

                // Menu: o titulo na fonte nova e mais alto e encostava no
                // subtitulo logo abaixo.
                if (cena == "MainMenu" && t.name == "Subtitle")
                    ((RectTransform)t.transform).anchoredPosition = new Vector2(0f, 122f);

                t.fontStyle = FontStyle.Normal;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                EditorUtility.SetDirty(t);
                n++;
            }
            return n;
        }

        static void FontesDoHud(StringBuilder log)
        {
            var titulo = AssetDatabase.LoadAssetAtPath<Font>(FonteTitulo);
            var texto = AssetDatabase.LoadAssetAtPath<Font>(FonteTexto);

            var raiz = PrefabUtility.LoadPrefabContents(PrefabHud);
            int n = 0;
            foreach (var t in raiz.GetComponentsInChildren<Text>(true))
            {
                bool ehTitulo = t.name == "PauseTitle";
                t.font = ehTitulo ? titulo : texto;
                if (!ehTitulo && t.GetComponentInParent<Button>() != null) t.fontSize = 30;
                string caixa;
                if (ehTitulo && TitulosEmCaixaNormal.TryGetValue(t.text, out caixa)) t.text = caixa;
                if (ehTitulo) t.fontSize = 72;
                t.fontStyle = FontStyle.Normal;
                n++;
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabHud);
            PrefabUtility.UnloadPrefabContents(raiz);
            log.AppendLine("  HUD: " + n + " textos com fonte nova");
        }

        // --- Placas do Tutorial -------------------------------------------

        /// <summary>
        /// Texto novo de cada placa, pelo texto antigo. Sem setas ASCII (as
        /// fontes de pixel nao tem setas), com os acentos que faltavam.
        /// </summary>
        static readonly Dictionary<string, string> TextosDasPlacas = new Dictionary<string, string>
        {
            { "MOVER\nA / D  ou  <-  ->", "MOVER\nA / D  ou  setas" },
            { "PULAR\nESPACO", "PULAR\nEspaço" },
            { "ATACAR\nClique Esquerdo", "ATACAR\nClique esquerdo" },
            { "PAUSAR\nESC", "PAUSAR\nEsc" },
            { "SAIDA  ->", "SAÍDA" },
            { "~ Tutorial ~", "Tutorial" },
            { "ECOS DE ALDENOR", "Ecos de Aldenor" },
        };

        static string PlacasDoTutorial()
        {
            var titulo = AssetDatabase.LoadAssetAtPath<Font>(FonteTitulo);
            var texto = AssetDatabase.LoadAssetAtPath<Font>(FonteTexto);
            var dicas = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include).ToList();
            if (dicas.Count == 0) return "";

            // A placa de esquiva nao existia: o rolamento so era ensinado na
            // tela de tutorial. Ela nasce como copia da placa de ataque, que fica
            // logo antes no caminho.
            var ataque = dicas.FirstOrDefault(d => d.text.StartsWith("ATACAR"));
            if (ataque != null && !dicas.Any(d => d.text.StartsWith("ESQUIVAR")))
            {
                var copia = Object.Instantiate(ataque.gameObject, ataque.transform.parent);
                copia.name = "Hint";
                var tm = copia.GetComponent<TextMesh>();
                tm.text = "ESQUIVAR\nShift  ou  botão direito";
                dicas.Add(tm);
            }

            // Espacamento ao longo do caminho, da esquerda para a direita, com
            // a placa nova entre atacar e pausar.
            var posX = new Dictionary<string, float>
            {
                { "ATACAR", 2.6f }, { "ESQUIVAR", 6.4f }, { "PAUSAR", 10.0f },
            };

            foreach (var d in dicas)
            {
                Undo.RecordObject(d, "Placa");
                string novo;
                if (TextosDasPlacas.TryGetValue(d.text, out novo)) d.text = novo;

                bool ehTitulo = d.text == "Ecos de Aldenor" || d.text == "Tutorial";
                d.font = ehTitulo ? titulo : texto;
                d.fontStyle = FontStyle.Normal;
                var mr = d.GetComponent<MeshRenderer>();
                mr.sharedMaterial = d.font.material;

                string chave = d.text.Split('\n')[0];
                float x;
                if (posX.TryGetValue(chave, out x))
                    d.transform.position = new Vector3(x, d.transform.position.y, d.transform.position.z);

                // Titulo nao ganha placa: ele e a "fachada" da fase, nao uma dica.
                // Recria a placa a cada execucao, para ela pegar os valores atuais
                // do componente (cor, folga) em vez dos gravados na cena.
                var placa = d.GetComponent<EcosDeAldenor.Systems.PlacaDeDica>();
                if (placa != null) Object.DestroyImmediate(placa);
                if (!ehTitulo) d.gameObject.AddComponent<EcosDeAldenor.Systems.PlacaDeDica>();

                EditorUtility.SetDirty(d);
            }
            return dicas.Count + " placas (incluindo a nova de esquiva)";
        }

        // --- Vitoria ---------------------------------------------------------

        static string TituloDaVitoria()
        {
            var titulo = GameObject.Find("Title");
            var icone = GameObject.Find("V_Frag");
            var sub = GameObject.Find("V_Sub");
            if (titulo == null || icone == null) return "";

            // Titulo, icone e frase empilhados com folga. O defeito vinha das
            // ANCORAS: o titulo estava ancorado a 60% da altura e o icone no
            // centro, entao "titulo em 130, icone em 215" punha o icone dentro
            // das letras. Os tres passam a medir a partir do mesmo centro.
            var rt = (RectTransform)titulo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 140f);
            rt.sizeDelta = new Vector2(1000f, 110f);
            ((RectTransform)icone.transform).anchoredPosition = new Vector2(0f, 250f);
            if (sub != null) ((RectTransform)sub.transform).anchoredPosition = new Vector2(0f, 40f);
            return "icone acima do titulo";
        }

        // --- Creditos --------------------------------------------------------

        static string CreditarFontes()
        {
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (!t.text.Contains("Fonte")) continue;
                t.text = t.text
                    .Replace("Gothicvania (Cemiterio, Igreja, Cidade)", "GothicVania (Cemitério, Igreja, Cidade)")
                    .Replace("Fonte: Legacy Runtime (Unity)",
                             "Fontes: New Rocker (Pablo Impallari) e Pixelify Sans (Stefie Justprince) — SIL OFL 1.1")
                    .Replace("Fontes: Jacquard 24 (Sarah Cadigan-Fried) e Pixelify Sans (Stefie Justprince) — SIL OFL 1.1",
                             "Fontes: New Rocker (Pablo Impallari) e Pixelify Sans (Stefie Justprince) — SIL OFL 1.1");
                EditorUtility.SetDirty(t);
                return "fontes creditadas";
            }
            return "";
        }

        // --- Camera ----------------------------------------------------------

        static string Camera()
        {
            var cam = Object.FindAnyObjectByType<UnityEngine.Camera>();
            if (cam == null) return "";

            foreach (var mb in cam.GetComponents<MonoBehaviour>())
            {
                var so = new SerializedObject(mb);
                var minY = so.FindProperty("minY");
                if (minY == null) continue;
                minY.floatValue = CameraMinY;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // A camera ja nasce na altura em que vai ficar: o parallax mede o
            // deslocamento a partir da posicao inicial, e uma camera que nasce
            // baixa e sobe no primeiro segundo desalinha as camadas de fundo.
            var p = cam.transform.position;
            cam.transform.position = new Vector3(p.x, Mathf.Max(p.y, CameraMinY), p.z);
            return "camera com minY " + CameraMinY;
        }

        // --- Entrada da fase ---------------------------------------------------

        /// <summary>
        /// O "Door.png" do Brackeys era um retangulo marrom de 2,4 x 3,8 u,
        /// enterrado 1,8 u no chao, atras do ponto de nascimento. No cemiterio
        /// ele vira um portao de dois pilares com raizes e cranios (o pilar do
        /// proprio tileset do cemiterio), com o Ren nascendo entre eles; na
        /// igreja, a porta em arco do tileset da igreja - a porta por onde ele
        /// entrou.
        /// </summary>
        static string Entrada(bool igreja)
        {
            var porta = GameObject.Find("Decoration_Door");
            if (porta == null) return "";
            var sr = porta.GetComponent<SpriteRenderer>();
            float chao = LevelTerrainBuilder.TopoChao;

            // Remove o pilar direito de uma execucao anterior.
            var antigo = porta.transform.Find("PilarDireito");
            if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

            sr.color = new Color(0.82f, 0.8f, 0.9f, 1f);
            sr.flipX = false;
            sr.drawMode = SpriteDrawMode.Simple;

            if (igreja)
            {
                var arco = Fatia(TilesetIgreja, 7);
                float escala = 2.6f / arco.bounds.size.y;           // ~2,6 u de altura
                sr.sprite = arco;
                porta.transform.localScale = new Vector3(escala, escala, 1f);
                porta.transform.position = new Vector3(-11f, chao + 1.3f - 0.02f, 1f);
                return "porta em arco";
            }

            var pilar = Fatia(TilesetCemiterio, 3);
            const float escalaPilar = 1.6f;
            float meiaAltura = pilar.bounds.size.y * escalaPilar * 0.5f;
            float y = chao + meiaAltura - 0.08f;

            sr.sprite = pilar;
            porta.transform.localScale = new Vector3(escalaPilar, escalaPilar, 1f);
            porta.transform.position = new Vector3(-11.4f, y, 1f);

            var direito = new GameObject("PilarDireito");
            direito.transform.SetParent(porta.transform, false);
            direito.transform.position = new Vector3(-8.8f, y, 1f);
            var srd = direito.AddComponent<SpriteRenderer>();
            srd.sprite = pilar;
            srd.flipX = true;
            srd.color = sr.color;
            srd.sortingOrder = sr.sortingOrder;
            srd.sharedMaterial = sr.sharedMaterial;
            return "portao de pilares";
        }

        // ------------------------------------------------------------------
        // Altar de alma (prefab)
        // ------------------------------------------------------------------

        /// <summary>
        /// O altar era a pedra cinza do Brackeys (maior que o Ren) com o MESMO
        /// sprite da alma coletavel em cima: o jogador nao tinha como saber que
        /// aquilo era um checkpoint, e a "alma" do altar parecia um item que nao
        /// dava para pegar.
        ///
        /// Agora e a estatua encapuzada do cemiterio - uma vigia de pedra, que e
        /// literalmente o tema do jogo - com uma chama de alma que tremula sobre
        /// ela: azul fraca enquanto o altar dorme, verde forte depois de aceso.
        /// A chama (Art/ChamaDeAlma) foi desenhada em tons de cinza justamente
        /// para receber essas duas cores.
        ///
        /// As medidas assumem a escala 0,8 e a altura 0,5 que todas as quatro
        /// instancias usam nas cenas.
        /// </summary>
        static void AltarDeAlma(StringBuilder log)
        {
            const float escalaInstancia = 0.8f, yInstancia = 0.5f;
            float chao = LevelTerrainBuilder.TopoChao;

            var estatua = Fatia(ObjetosCemiterio, 7);
            var chamas = Enumerable.Range(1, 4)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(PastaChama + "chama-" + i + ".png"))
                .Where(s => s != null).ToArray();
            if (estatua == null || chamas.Length == 0) { log.AppendLine("  altar: arte nao encontrada"); return; }

            var raiz = PrefabUtility.LoadPrefabContents(PrefabAltar);

            // A pedra sai; o componente fica (desligado) porque o Checkpoint
            // ainda consulta o SpriteRenderer da raiz.
            var srRaiz = raiz.GetComponent<SpriteRenderer>();
            if (srRaiz != null) srRaiz.enabled = false;

            var corpo = raiz.transform.Find("Estatua");
            if (corpo == null)
            {
                corpo = new GameObject("Estatua").transform;
                corpo.SetParent(raiz.transform, false);
            }
            // Comparacao explicita, e nao "??": o GetComponent do editor devolve
            // um "nulo falso" do Unity que o operador do C# trata como valido.
            var srCorpo = corpo.GetComponent<SpriteRenderer>();
            if (srCorpo == null) srCorpo = corpo.gameObject.AddComponent<SpriteRenderer>();
            srCorpo.sprite = estatua;
            srCorpo.color = new Color(0.84f, 0.82f, 0.92f, 1f);
            srCorpo.sortingOrder = 1;
            if (srRaiz != null) srCorpo.sharedMaterial = srRaiz.sharedMaterial;

            // Estatua com ~1,5 u de altura, base encostada no chao.
            const float escalaLocal = 2.4f;
            float alturaMundo = estatua.bounds.size.y * escalaLocal * escalaInstancia;
            float baseLocal = (chao - yInstancia) / escalaInstancia;
            corpo.localScale = new Vector3(escalaLocal, escalaLocal, 1f);
            corpo.localPosition = new Vector3(0f, baseLocal + (alturaMundo / escalaInstancia) * 0.5f - 0.06f, 0f);

            // Chama logo acima da cabeca da estatua.
            float topoLocal = baseLocal + alturaMundo / escalaInstancia;
            var chama = raiz.transform.Find("SoulFlame");
            var srChama = chama.GetComponent<SpriteRenderer>();
            srChama.sprite = chamas[0];
            // O recorte da estatua tem ~10 px vazios acima do capuz: a chama
            // desce para dentro dessa margem, senao fica boiando solta no ar.
            chama.localScale = new Vector3(3f, 3f, 1f);
            chama.localPosition = new Vector3(0f, topoLocal + 0.22f, 0f);

            var anim = chama.GetComponent<EcosDeAldenor.Systems.SimpleSpriteAnimator>();
            if (anim == null) anim = chama.gameObject.AddComponent<EcosDeAldenor.Systems.SimpleSpriteAnimator>();
            var soAnim = new SerializedObject(anim);
            var quadros = soAnim.FindProperty("frames");
            quadros.arraySize = chamas.Length;
            for (int i = 0; i < chamas.Length; i++) quadros.GetArrayElementAtIndex(i).objectReferenceValue = chamas[i];
            soAnim.FindProperty("framesPerSecond").floatValue = 8f;
            soAnim.ApplyModifiedPropertiesWithoutUndo();

            var luz = raiz.transform.Find("ShrineLight");
            if (luz != null) luz.localPosition = chama.localPosition;

            // Cores: o Checkpoint pinta chama e luz com elas.
            var cp = raiz.GetComponent<EcosDeAldenor.Systems.Checkpoint>();
            var so = new SerializedObject(cp);
            var inativa = new Color(0.42f, 0.58f, 0.95f, 1f);
            so.FindProperty("inactiveColor").colorValue = inativa;
            so.FindProperty("activeColor").colorValue = new Color(0.5f, 1f, 0.7f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            srChama.color = inativa;

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabAltar);
            PrefabUtility.UnloadPrefabContents(raiz);
            log.AppendLine("  altar: estatua de vigia com chama de alma animada");
        }

        // ------------------------------------------------------------------

        static Sprite Fatia(string caminho, int indice)
        {
            return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>()
                   .FirstOrDefault(s => s.name.EndsWith("_" + indice));
        }
    }
}
