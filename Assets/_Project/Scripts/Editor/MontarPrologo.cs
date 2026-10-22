using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using EcosDeAldenor.Systems;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// MONTA A CENA DO PROLOGO.
    ///
    /// O prologo e um componente que constroi os quadros da historia em
    /// runtime; esta ferramenta cria a cena que o hospeda e preenche os dados
    /// de cada quadro (texto, camadas de cenario, personagens), apontando para
    /// a arte pelos CAMINHOS dos arquivos. Assim:
    ///   - a historia inteira fica legivel aqui, num lugar so - mudar uma frase
    ///     e mudar uma linha e rodar o menu de novo;
    ///   - a cena pode ser refeita do zero se alguem a estragar no editor.
    ///
    /// Toda a arte vem dos pacotes ja presentes. O pacote GothicVania Town, que
    /// so tinha a trilha em uso, entra aqui com a cidade e os quatro moradores.
    /// As camadas da cidade e os moradores vinham importados com filtro
    /// bilinear (borram o pixel art ampliado) e sao corrigidos para Point.
    ///
    /// Menu: "Ecos de Aldenor -> Montar prologo".
    /// </summary>
    public static class MontarPrologo
    {
        const string Cena = "Assets/_Project/Scenes/Prologo.unity";
        const string Cidade = "Assets/GameAssets/GothicVania-town-files/GothicVania-town-files/PNG/";
        const string Cemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/";
        const string Igreja = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/";
        const string HeroKnight = "Assets/GameAssets/Hero Knight - Pixel Art/";
        const string Sfx = "Assets/GameAssets/Leohpaz/RPG_Essentials_Free/";
        const string Alma = "Assets/Brackeys/2D Mega Pack/Items & Icons/Gothic/SoulFragment.png";

        // Fatias da nave da igreja usadas na tela de Vitoria (backgrounds.png),
        // na mesma ordem: as duas pontas, os vitrais e o altar no meio.
        const long NaveA = 2583609812203033409;
        const long NaveB = 4544235448077845365;
        const long NaveC = 6172491537803308644;

        [MenuItem("Ecos de Aldenor/Montar prologo")]
        public static void Montar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CorrigirImportacao();

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var eventos = new GameObject("EventSystem");
            eventos.AddComponent<EventSystem>();
            eventos.AddComponent<StandaloneInputModule>();

            // A mesma trilha do menu: o AudioManager nao reinicia uma faixa que
            // ja esta tocando, entao a musica atravessa a troca de cena sem corte.
            var musica = new GameObject("SceneMusic").AddComponent<SceneMusicPlayer>();
            var soMusica = new SerializedObject(musica);
            soMusica.FindProperty("sceneMusic").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/2D Dungeon Tilemap/Musics & Sounds CC0/Lobby Music.wav");
            soMusica.ApplyModifiedPropertiesWithoutUndo();

            var sons = new GameObject("UISounds").AddComponent<UISounds>();
            var soSons = new SerializedObject(sons);
            soSons.FindProperty("somDeFoco").objectReferenceValue = Clip(Sfx + "10_UI_Menu_SFX/001_Hover_01.wav");
            soSons.FindProperty("somDeClique").objectReferenceValue = Clip(Sfx + "10_UI_Menu_SFX/013_Confirm_03.wav");
            soSons.ApplyModifiedPropertiesWithoutUndo();

            var prologo = new GameObject("Prologo").AddComponent<Prologo>();
            var campos = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(Prologo).GetField("quadros", campos).SetValue(prologo, Quadros());
            typeof(Prologo).GetField("almaDoTitulo", campos).SetValue(prologo, Primeiro(Alma));
            EditorUtility.SetDirty(prologo);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, Cena);
            ColocarNaBuild();

            Debug.Log("Prologo montado em " + Cena + " e incluido na build logo depois do MainMenu.");
        }

        // ------------------------------------------------------------------
        // A historia
        // ------------------------------------------------------------------

        static Prologo.Quadro[] Quadros()
        {
            var trovao = Clip(Sfx + "8_Atk_Magic_SFX/18_Thunder_02.wav");
            var absorver = Clip(Sfx + "8_Buffs_Heals_SFX/39_Absorb_04.wav");
            var sombrio = Clip(Sfx + "8_Buffs_Heals_SFX/21_Debuff_01.wav");
            var reviver = Clip(Sfx + "8_Buffs_Heals_SFX/30_Revive_03.wav");

            var velho = Frames(Cidade + "sprites/oldman-walk/oldman-walk-{0}.png", 1, 12);
            var barbudo = Frames(Cidade + "sprites/bearded-idle/bearded-idle-{0}.png", 1, 5);
            var chapeu = Frames(Cidade + "sprites/hat-man-idle/hat-man-idle-{0}.png", 1, 4);
            var mulher = Frames(Cidade + "sprites/woman-walk/woman-walk-{0}.png", 1, 6);
            var mulherParada = Frames(Cidade + "sprites/woman-idle/woman-idle-{0}.png", 1, 7);
            var velhoParado = Frames(Cidade + "sprites/oldman-idle/oldman-idle-{0}.png", 1, 8);
            var espectro = Frames(Cemiterio + "Characters/Enemies/ghost/SpritesHalo/ghost-halo-{0}.png", 1, 4);
            var anjo = Frames(Igreja + "SPRITES/angel/idle/sprites/f-{0:00}.png", 1, 8);
            var mago = Frames(Igreja + "SPRITES/wizard/Idle/sprites/f-{0:00}.png", 1, 5);
            var fumaca = Frames(Igreja + "SPRITES/fx/enemy-death/enemy-death-sprites/enemy-death{0}.png", 1, 9);
            var eco = new[] { Primeiro(Alma) };

            // O Ren "desperta": a animacao de morte dele tocada AO CONTRARIO e
            // um homem se levantando do chao. Depois, o parado de sempre.
            var renLevantando = FramesDoClipe(HeroKnight + "Animations/HeroKnight_DeathNoBlood.anim").Reverse().ToArray();
            var renParado = FramesDoClipe(HeroKnight + "Animations/HeroKnight_Idle.anim");

            // Os moradores do capitulo I, de novo no III - apagados, parados,
            // e logo arrancados de si mesmos.
            var cinza = new Color(0.55f, 0.5f, 0.68f, 1f);

            return new[]
            {
                // I. A cidade viva, ao entardecer.
                new Prologo.Quadro
                {
                    capitulo = "I  ·  Aldenor",
                    texto = "Aldenor já foi um reino de sinos e lampiões acesos. " +
                            "No alto da colina, a velha igreja velava por todos.",
                    atrasoDoTexto = 0.6f,
                    camadas = CamadasDaCidade(Color.white),
                    veu = new Color(0.05f, 0.02f, 0.06f, 0.1f),
                    panoramica = 6f,
                    luzes = new[]
                    {
                        // O sol que se poe atras da cidade, e o sino da igreja aceso.
                        new Prologo.Luz { posicao = new Vector2(0f, 470f), tamanho = new Vector2(2200f, 520f),
                                          cor = new Color(1f, 0.6f, 0.3f, 0.28f), pulso = 0.08f },
                        new Prologo.Luz { posicao = new Vector2(40f, 520f), tamanho = new Vector2(380f, 380f),
                                          cor = new Color(1f, 0.85f, 0.5f, 0.22f), pulso = 0.2f },
                    },
                    // Vaga-lumes: os lampioes da frase, ganhando vida.
                    quantidadeDeAlmas = 14,
                    corDaAlma = new Color(1f, 0.85f, 0.45f, 1f),
                    tamanhoDaAlma = new Vector2(10f, 18f),
                    atores = new[]
                    {
                        Ator("Velho", velho, 9f, 3f, new Vector2(-650f, 40f), andar: 28f),
                        Ator("Barbudo", barbudo, 7f, 3f, new Vector2(-250f, 40f)),
                        Ator("Chapeu", chapeu, 6f, 3f, new Vector2(330f, 40f), espelhar: true),
                        Ator("Mulher", mulher, 9f, 3f, new Vector2(720f, 40f), espelhar: true, andar: -32f),
                    },
                },

                // II. O anjo guardiao, sob colunas de luz.
                new Prologo.Quadro
                {
                    capitulo = "II  ·  A Vigília",
                    texto = "Diziam que um anjo guardava aquela igreja. Chamavam-na de A Vigília: " +
                            "enquanto ela velasse, nenhuma alma de Aldenor se perderia.",
                    atrasoDoTexto = 1f,
                    camadas = CamadasDaIgreja(Color.white),
                    corDeBase = new Color(0.153f, 0.149f, 0.22f, 1f),
                    veu = new Color(0.05f, 0.04f, 0.12f, 0.08f),
                    panoramica = 4f,
                    luzes = new[]
                    {
                        new Prologo.Luz { posicao = new Vector2(-270f, 420f), tamanho = new Vector2(240f, 1200f),
                                          cor = new Color(1f, 0.93f, 0.7f, 0.16f), pulso = 0.3f },
                        new Prologo.Luz { posicao = new Vector2(270f, 420f), tamanho = new Vector2(240f, 1200f),
                                          cor = new Color(1f, 0.93f, 0.7f, 0.16f), pulso = 0.3f },
                        new Prologo.Luz { posicao = new Vector2(0f, 300f), tamanho = new Vector2(760f, 760f),
                                          cor = new Color(1f, 0.85f, 0.5f, 0.32f), pulso = 0.2f },
                    },
                    // As almas de Aldenor, douradas e protegidas.
                    quantidadeDeAlmas = 14,
                    corDaAlma = new Color(1f, 0.92f, 0.65f, 1f),
                    tamanhoDaAlma = new Vector2(12f, 22f),
                    atores = new[]
                    {
                        Ator("Anjo", anjo, 10f, 4f, new Vector2(0f, 60f), flutuar: 14f,
                             brilho: new Color(1f, 0.9f, 0.6f, 0.45f), tamanhoDoBrilho: 1.2f),
                    },
                },

                // III. A nevoa: relampago, e os moradores se desfazem em almas.
                new Prologo.Quadro
                {
                    capitulo = "III  ·  A Névoa",
                    texto = "Até a noite em que a névoa desceu. Os sinos se calaram, e as almas do povo " +
                            "foram arrancadas dos corpos e espalhadas como ecos pelo cemitério.",
                    atrasoDoTexto = 2.8f,
                    camadas = CamadasDaCidade(new Color(0.38f, 0.36f, 0.55f)),
                    veu = new Color(0.10f, 0.04f, 0.16f, 0.5f),
                    panoramica = 5f,
                    nevoa = new Color(0.5f, 0.38f, 0.65f, 0.38f),
                    relampagos = new[] { 1.1f, 7.5f },
                    somDoRelampago = trovao,
                    quantidadeDeAlmas = 8,
                    alma = Primeiro(Alma),
                    tamanhoDaAlma = new Vector2(22f, 34f),
                    atores = new[]
                    {
                        Ator("VelhoApagado", velhoParado, 6f, 3f, new Vector2(-520f, 40f), cor: cinza,
                             sumir: 1.3f, fade: 0.9f, almasAoSumir: true, somAoSumir: absorver),
                        Ator("BarbudoApagado", barbudo, 6f, 3f, new Vector2(-180f, 40f), cor: cinza,
                             sumir: 1.6f, fade: 0.9f, almasAoSumir: true),
                        Ator("ChapeuApagado", chapeu, 6f, 3f, new Vector2(240f, 40f), espelhar: true, cor: cinza,
                             sumir: 1.9f, fade: 0.9f, almasAoSumir: true),
                        Ator("MulherApagada", mulherParada, 6f, 3f, new Vector2(560f, 40f), espelhar: true, cor: cinza,
                             sumir: 2.2f, fade: 0.9f, almasAoSumir: true),

                        Ator("Espectro1", espectro, 6f, 3f, new Vector2(-560f, 40f), andar: 14f, flutuar: 10f, surgir: 2.3f, fade: 1.2f),
                        Ator("Espectro2", espectro, 6f, 3f, new Vector2(60f, 40f), espelhar: true, andar: -10f, flutuar: 10f, surgir: 2.8f, fade: 1.2f),
                        Ator("Espectro3", espectro, 6f, 3f, new Vector2(620f, 40f), espelhar: true, andar: -16f, flutuar: 10f, surgir: 3.3f, fade: 1.2f),
                    },
                },

                // IV. O manto: o anjo se dissolve, e no lugar dele surge o mago.
                new Prologo.Quadro
                {
                    capitulo = "IV  ·  O Manto",
                    texto = "A Vigília não as protegeu. Cobriu-se com um capuz e passou a guardá-las " +
                            "como prisioneiras. Dizem que o anjo ainda está lá, sob aquele manto.",
                    atrasoDoTexto = 3f,
                    camadas = CamadasDaIgreja(new Color(0.55f, 0.32f, 0.38f)),
                    corDeBase = new Color(0.08f, 0.05f, 0.09f, 1f),
                    veu = new Color(0.16f, 0f, 0.04f, 0.4f),
                    panoramica = 3f,
                    luzes = new[]
                    {
                        new Prologo.Luz { posicao = new Vector2(0f, 300f), tamanho = new Vector2(900f, 760f),
                                          cor = new Color(0.85f, 0.1f, 0.15f, 0.38f), pulso = 0.3f, surgir = 2f },
                    },
                    quantidadeDeAlmas = 10,
                    corDaAlma = new Color(1f, 0.3f, 0.25f, 1f),
                    tamanhoDaAlma = new Vector2(8f, 14f),
                    atores = new[]
                    {
                        Ator("AnjoQueSeEsconde", anjo, 10f, 4f, new Vector2(0f, 60f), flutuar: 14f,
                             cor: new Color(0.85f, 0.78f, 0.85f, 1f), sumir: 1.7f, fade: 0.5f),
                        // A fumaca da igreja, tocada uma vez no instante da troca.
                        Ator("Manto", fumaca, 12f, 6f, new Vector2(0f, 20f), repetir: false,
                             cor: new Color(0.6f, 0.15f, 0.25f, 1f), surgir: 1.45f, fade: 0.1f, sumir: 2.35f),
                        Ator("Vigilia", mago, 6f, 7f, new Vector2(0f, 40f), flutuar: 8f, surgir: 2f, fade: 0.8f,
                             somAoSurgir: sombrio, brilho: new Color(0.9f, 0.15f, 0.2f, 0.4f), tamanhoDoBrilho: 1.4f),
                    },
                },

                // V. O ultimo cavaleiro se levanta, e os seis ecos acendem.
                new Prologo.Quadro
                {
                    capitulo = "V  ·  O Último Cavaleiro",
                    texto = "Ren, o último cavaleiro de pé, desperta entre as lápides. Seis ecos ainda " +
                            "brilham no escuro. Reúna-os, suba à igreja e encerre a vigília.",
                    atrasoDoTexto = 3.4f,
                    camadas = CamadasDoCemiterio(),
                    veu = new Color(0.02f, 0.01f, 0.05f, 0.28f),
                    panoramica = 5f,
                    luzes = new[]
                    {
                        new Prologo.Luz { posicao = new Vector2(0f, 600f), tamanho = new Vector2(800f, 700f),
                                          cor = new Color(0.9f, 0.3f, 0.6f, 0.15f), pulso = 0.15f },
                    },
                    atores = new[]
                    {
                        Ator("Ren", renParado, 8f, 4f, new Vector2(-220f, 20f), entrada: renLevantando, fpsEntrada: 8f,
                             surgir: 0.3f, fade: 0.4f, somAoSurgir: reviver),
                        Eco(eco, new Vector2(-640f, 250f), 1.7f, absorver),
                        Eco(eco, new Vector2(-380f, 420f), 2.0f, absorver),
                        Eco(eco, new Vector2(60f, 300f), 2.3f, absorver),
                        Eco(eco, new Vector2(300f, 460f), 2.6f, absorver),
                        Eco(eco, new Vector2(560f, 260f), 2.9f, absorver),
                        Eco(eco, new Vector2(780f, 400f), 3.2f, absorver),
                    },
                },
            };
        }

        /// <summary>Um dos seis ecos: a alma do HUD, ampliada, com luz propria.</summary>
        static Prologo.Ator Eco(Sprite[] sprite, Vector2 posicao, float surgir, AudioClip som)
        {
            return Ator("Eco", sprite, 1f, 3f, posicao, flutuar: 12f, surgir: surgir, fade: 0.7f, somAoSurgir: som,
                        brilho: new Color(1f, 0.55f, 0.85f, 0.6f), tamanhoDoBrilho: 3.2f);
        }

        // ------------------------------------------------------------------
        // Cenarios (os mesmos do menu e da vitoria, mais a cidade)
        // ------------------------------------------------------------------

        static FundoDeCena.Camada[] CamadasDaCidade(Color cor)
        {
            return new[]
            {
                new FundoDeCena.Camada { nome = "Ceu", sprites = new[] { Primeiro(Cidade + "environment/layers/background.png") },
                                         zoom = 6f, cobrirTela = true, cor = cor },   // 6: folga para a camera deslizar
                // A cidade sobre a colina, com a igreja no alto. Zoom 3: a imagem
                // inteira cabe na altura da tela, torres incluidas.
                new FundoDeCena.Camada { nome = "Cidade", sprites = new[] { Primeiro(Cidade + "environment/layers/middleground.png") },
                                         zoom = 3f, y = 0f, cor = cor },
            };
        }

        static FundoDeCena.Camada[] CamadasDaIgreja(Color cor)
        {
            var fatias = AssetDatabase.LoadAllAssetsAtPath(Igreja + "ENVIRONMENT/backgrounds.png").OfType<Sprite>().ToList();
            Sprite Fatia(long id) => fatias.FirstOrDefault(s =>
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string _, out long local) && local == id);

            var a = Fatia(NaveA); var b = Fatia(NaveB); var c = Fatia(NaveC);
            return new[]
            {
                new FundoDeCena.Camada { nome = "Nave", sprites = new[] { a, b, c, b, a }, zoom = 4f, y = 0f, cor = cor },
            };
        }

        static FundoDeCena.Camada[] CamadasDoCemiterio()
        {
            return new[]
            {
                new FundoDeCena.Camada { nome = "Ceu", sprites = new[] { Primeiro(Cemiterio + "Environment/background.png") },
                                         zoom = 6f, cobrirTela = true, cor = Color.white },
                new FundoDeCena.Camada { nome = "Montanhas", sprites = new[] { Primeiro(Cemiterio + "Environment/mountains.png") },
                                         zoom = 3f, y = 30f, velocidade = 4f, cor = new Color(0.9f, 0.9f, 0.9f, 1f) },
                new FundoDeCena.Camada { nome = "Tumulos", sprites = new[] { Primeiro(Cemiterio + "Environment/graveyard.png") },
                                         zoom = 3f, y = 0f, velocidade = 8f, cor = Color.white },
            };
        }

        static Prologo.Ator Ator(string nome, Sprite[] quadros, float fps, float zoom, Vector2 posicao,
                                 bool espelhar = false, float andar = 0f, float flutuar = 0f, Color? cor = null,
                                 bool repetir = true, Sprite[] entrada = null, float fpsEntrada = 10f,
                                 float surgir = 0f, float sumir = -1f, float fade = 0.6f, bool almasAoSumir = false,
                                 AudioClip somAoSurgir = null, AudioClip somAoSumir = null,
                                 Color? brilho = null, float tamanhoDoBrilho = 1.6f)
        {
            return new Prologo.Ator
            {
                nome = nome, quadros = quadros, fps = fps, zoom = zoom, posicao = posicao,
                espelhar = espelhar, andar = andar, flutuar = flutuar, cor = cor ?? Color.white,
                repetir = repetir, entrada = entrada, fpsEntrada = fpsEntrada,
                surgir = surgir, sumir = sumir, fade = fade, almasAoSumir = almasAoSumir,
                somAoSurgir = somAoSurgir, somAoSumir = somAoSumir,
                brilho = brilho ?? Color.clear, tamanhoDoBrilho = tamanhoDoBrilho,
            };
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        /// <summary>
        /// A arte da cidade e os moradores nunca tinham sido usados e vinham com
        /// filtro bilinear e compressao: ampliados 3x, ficam borrados e com
        /// manchas. Pixel art pede Point e sem compressao, como o resto do jogo.
        /// </summary>
        static void CorrigirImportacao()
        {
            // So o que o prologo usa: as duas camadas da cidade e os quatro
            // moradores. O resto do pacote fica como veio.
            var pastas = new[]
            {
                Cidade + "sprites/oldman-walk",
                Cidade + "sprites/bearded-idle",
                Cidade + "sprites/hat-man-idle",
                Cidade + "sprites/woman-walk",
                Cidade + "sprites/oldman-idle",
                Cidade + "sprites/woman-idle",
            };
            var caminhos = AssetDatabase.FindAssets("t:Texture2D", pastas).Select(AssetDatabase.GUIDToAssetPath).ToList();
            caminhos.Add(Cidade + "environment/layers/background.png");
            caminhos.Add(Cidade + "environment/layers/middleground.png");

            foreach (var caminho in caminhos)
            {
                var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (imp == null) continue;
                if (imp.filterMode == FilterMode.Point && imp.textureCompression == TextureImporterCompression.Uncompressed) continue;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }

        static void ColocarNaBuild()
        {
            var lista = EditorBuildSettings.scenes.Where(s => s.path != Cena).ToList();
            int depoisDoMenu = lista.FindIndex(s => s.path.EndsWith("MainMenu.unity")) + 1;
            lista.Insert(Mathf.Clamp(depoisDoMenu, 0, lista.Count), new EditorBuildSettingsScene(Cena, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }

        static Sprite Primeiro(string caminho)
        {
            var s = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
            if (s == null) Debug.LogWarning("Sprite nao encontrado: " + caminho);
            return s;
        }

        static Sprite[] Frames(string formato, int de, int ate)
        {
            var lista = new List<Sprite>();
            for (int i = de; i <= ate; i++)
            {
                var s = Primeiro(string.Format(formato, i));
                if (s != null) lista.Add(s);
            }
            return lista.ToArray();
        }

        /// <summary>Os quadros de um AnimationClip de sprites, na ordem em que ele os toca.</summary>
        static Sprite[] FramesDoClipe(string caminho)
        {
            var clipe = AssetDatabase.LoadAssetAtPath<AnimationClip>(caminho);
            if (clipe == null) { Debug.LogWarning("Clipe nao encontrado: " + caminho); return new Sprite[0]; }

            foreach (var ligacao in AnimationUtility.GetObjectReferenceCurveBindings(clipe))
            {
                if (ligacao.propertyName != "m_Sprite") continue;
                return AnimationUtility.GetObjectReferenceCurve(clipe, ligacao)
                                       .Select(k => k.value as Sprite).Where(s => s != null).ToArray();
            }
            return new Sprite[0];
        }

        static AudioClip Clip(string caminho) => AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
    }
}
