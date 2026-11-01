using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Enemies;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// MONTA AS FASES 3 E 4, entre o cemiterio e a arena do chefe:
    ///
    ///   Phase3  A Cidade Silenciosa - Aldenor tomada pela nevoa. Gatos
    ///           Infernais (pedem esquiva) e tres sinos a fazer tocar.
    ///   Phase4  O Claustro - a nave da igreja por dentro. Carnicais em Chamas
    ///           (pedem pulo), braseiros com a marca vermelha do chefe e tres
    ///           cirios do manto a apagar.
    ///
    /// Cada fase parte de uma cena que ja funciona - a Phase3 da Phase2, a
    /// Phase4 da FinalPhase - e troca o que precisa: fundo, inimigos, objetivos
    /// e a porta. O relevo vem da tabela de LevelLayouts, como o das outras.
    ///
    /// Os dois inimigos novos viram prefabs feitos a partir dos que existem
    /// (mesmos sensores, luz dos olhos, camadas e fisica), trocando so o
    /// script e a arte.
    ///
    /// Rodar de novo refaz tudo do zero.
    ///
    /// Menu: "Ecos de Aldenor -> Montar fases novas (3 e 4)".
    /// </summary>
    public static class MontarFasesNovas
    {
        const string Cenas = "Assets/_Project/Scenes/";
        const string Prefabs = "Assets/_Project/Prefabs/Enemies/";
        const string Cemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/";
        const string Igreja = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/";
        const string Cidade = "Assets/GameAssets/GothicVania-town-files/GothicVania-town-files/PNG/environment/";
        const string Sfx = "Assets/GameAssets/Leohpaz/RPG_Essentials_Free/";
        const string TrilhaDasFasesNovas = "Assets/GameAssets/2D Dungeon Tilemap/Musics & Sounds CC0/Music 3.wav";

        // Ordem do enum PhaseExit.NextScene (gravado como numero nas cenas).
        const int DestinoFinalPhase = 2, DestinoPhase3 = 4, DestinoPhase4 = 5;

        [MenuItem("Ecos de Aldenor/Montar fases novas (3 e 4)")]
        public static void Montar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CorrigirImportacao();
            var gato = PrefabDoGato();
            var carnical = PrefabDoCarnical();

            MontarCidade(gato);
            MontarClaustro(carnical);
            LigarPortaDaPhase2();
            ColocarNaBuild();

            string relatorio = LevelLayouts.Reconstruir(LevelLayouts.FaseDaCena("Phase3")) + "\n" +
                               LevelLayouts.Reconstruir(LevelLayouts.FaseDaCena("Phase4"));
            Debug.Log("Fases novas montadas:\n" + relatorio);
        }

        // ------------------------------------------------------------------
        // Prefabs dos inimigos novos
        // ------------------------------------------------------------------

        static GameObject PrefabDoGato()
        {
            var quadros = Frames(Cemiterio + "Characters/Enemies/hell-gato/Sprites/hell-gato-{0}.png", 1, 4);
            return NovoInimigo<SombraRastejante, GatoInfernal>("SombraRastejante", "GatoInfernal", quadros[0], 2.6f,
                new Vector2(0.62f, 0.4f), new Vector2(0f, -0.06f), new Color(1f, 0.3f, 0.2f), new Vector3(0.28f, 0.05f, 0f),
                so =>
                {
                    Lista(so, "framesParado", quadros.Take(1).ToArray());
                    Lista(so, "framesAndando", quadros);
                    Lista(so, "framesPerseguindo", new Sprite[0]);
                    Lista(so, "framesDespertar", new Sprite[0]);
                    Lista(so, "framesMorte", new Sprite[0]);
                    so.FindProperty("fpsAndando").floatValue = 10f;
                    so.FindProperty("patrolSpeed").floatValue = 1.6f;
                    so.FindProperty("detectionRadius").floatValue = 4.2f;
                    // O corpo do gato tem 1,6 u de largura e colide com o do Ren: os
                    // centros nunca ficam a menos de ~1,2 u. Com o raio de 0,55 da
                    // Sombra (de quem o prefab foi copiado), o gato encostava e
                    // nunca machucava.
                    so.FindProperty("contactRadius").floatValue = 1.05f;
                    // E o golpe dele e o salto: encostar machuca, mas menos amiude.
                    so.FindProperty("contactDamageCooldown").floatValue = 1.2f;
                    so.FindProperty("spriteOlhaParaDireita").boolValue = false;
                    so.FindProperty("saltoSfx").objectReferenceValue = Clip(Sfx + "10_Battle_SFX/03_Claw_03.wav");
                    so.FindProperty("pousoSfx").objectReferenceValue = Clip(Sfx + "12_Player_Movement_SFX/45_Landing_01.wav");
                }, vida: 2);
        }

        static GameObject PrefabDoCarnical()
        {
            var corrida = Frames(Igreja + "SPRITES/burning-ghoul/run 1/sprites/f-{0:00}.png", 1, 8);
            var disparada = Frames(Igreja + "SPRITES/burning-ghoul/run 2/sprites/f-{0:00}.png", 1, 8);
            return NovoInimigo<GuardiaoDePedra, CarnicalEmChamas>("GuardiaoDePedra", "CarnicalEmChamas", corrida[0], 2.7f,
                new Vector2(0.3f, 0.5f), new Vector2(0f, -0.02f), new Color(1f, 0.55f, 0.2f), new Vector3(-0.05f, 0.24f, 0f),
                so =>
                {
                    Lista(so, "framesParado", corrida.Take(1).ToArray());
                    Lista(so, "framesAndando", corrida);
                    Lista(so, "framesPerseguindo", new Sprite[0]);
                    Lista(so, "framesDespertar", new Sprite[0]);
                    Lista(so, "framesMorte", new Sprite[0]);
                    Lista(so, "framesDisparada", disparada);
                    so.FindProperty("fpsAndando").floatValue = 7f;
                    so.FindProperty("patrolSpeed").floatValue = 1.4f;
                    so.FindProperty("detectionRadius").floatValue = 4.5f;
                    so.FindProperty("contactRadius").floatValue = 0.6f;
                    so.FindProperty("spriteOlhaParaDireita").boolValue = false;
                    so.FindProperty("disparadaSfx").objectReferenceValue = Clip(Sfx + "8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav");
                }, vida: 2);
        }

        /// <summary>
        /// Copia o prefab de um inimigo existente e troca o script dele pelo do
        /// inimigo novo. A troca e feita pelo campo m_Script: os campos de mesmo
        /// nome (patrulha, camadas, sons de dano, baforada de morte) passam
        /// intactos, e so os da classe nova precisam ser preenchidos.
        /// </summary>
        static GameObject NovoInimigo<TVelho, TNovo>(string origem, string nome, Sprite sprite, float escala,
                                                    Vector2 tamanhoDoCorpo, Vector2 desvioDoCorpo, Color corDosOlhos,
                                                    Vector3 posicaoDosOlhos, System.Action<SerializedObject> configurar, int vida)
            where TVelho : EnemyBase where TNovo : EnemyBase
        {
            string caminho = Prefabs + nome + ".prefab";
            AssetDatabase.DeleteAsset(caminho);
            AssetDatabase.CopyAsset(Prefabs + origem + ".prefab", caminho);

            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            raiz.name = nome;
            raiz.transform.localScale = new Vector3(escala, escala, 1f);

            var velho = raiz.GetComponent<TVelho>();
            var soScript = new SerializedObject(velho);
            soScript.FindProperty("m_Script").objectReferenceValue = AssetDatabase.FindAssets("t:MonoScript " + typeof(TNovo).Name)
                .Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
                .First(m => m != null && m.GetClass() == typeof(TNovo));
            soScript.ApplyModifiedPropertiesWithoutUndo();

            var novo = raiz.GetComponent<TNovo>();
            var so = new SerializedObject(novo);
            configurar(so);
            so.ApplyModifiedPropertiesWithoutUndo();

            var sr = raiz.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;

            var anim = raiz.GetComponent<SimpleSpriteAnimator>();
            if (anim != null)
            {
                var soAnim = new SerializedObject(anim);
                Lista(soAnim, "frames", new[] { sprite });
                soAnim.ApplyModifiedPropertiesWithoutUndo();
            }

            var corpo = raiz.GetComponent<BoxCollider2D>();
            corpo.size = tamanhoDoCorpo;
            corpo.offset = desvioDoCorpo;

            var soVida = new SerializedObject(raiz.GetComponent<HealthSystem>());
            soVida.FindProperty("maxHealth").intValue = vida;
            soVida.ApplyModifiedPropertiesWithoutUndo();

            var sensor = raiz.transform.Find("GroundSensor");
            if (sensor != null) sensor.localPosition = new Vector3(0f, desvioDoCorpo.y - tamanhoDoCorpo.y * 0.5f, 0f);

            var olhos = raiz.transform.Find("EyeGlow");
            if (olhos != null)
            {
                olhos.localPosition = posicaoDosOlhos;
                var luz = olhos.GetComponent<Light2D>();
                if (luz != null) luz.color = corDosOlhos;
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
            PrefabUtility.UnloadPrefabContents(raiz);
            return AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        }

        // ------------------------------------------------------------------
        // Phase3 - A Cidade Silenciosa
        // ------------------------------------------------------------------

        static void MontarCidade(GameObject gato)
        {
            var cena = NovaCenaAPartirDe("Phase2", "Phase3");

            Remover<EnemyBase>();
            Remover<Fragment>();
            RemoverRaiz("Decoration_Gravestone");

            // Pecas de chao da cidade: os props do pacote Town, no lugar das
            // lapides. As luzes de lampiao que ja existiam continuam.
            var deco = Raiz("Decoration");
            foreach (var t in Filhos(deco)) if (t.GetComponent<SpriteRenderer>() != null) Object.DestroyImmediate(t.gameObject);
            var noite = new Color(0.72f, 0.70f, 0.86f, 1f);
            Prop(deco, "Lampiao_1", "street-lamp", 1.9f, noite);
            Prop(deco, "Caixotes", "crate-stack", 1.6f, noite);
            Prop(deco, "Barril_1", "barrel", 1.8f, noite);
            Prop(deco, "Carroca", "wagon", 1.6f, noite);
            Prop(deco, "Lampiao_2", "street-lamp", 1.9f, noite);
            Prop(deco, "Poco", "well", 1.7f, noite);
            Prop(deco, "Barril_2", "barrel", 1.8f, noite);
            Prop(deco, "Lampiao_3", "street-lamp", 1.9f, noite);

            // Silhuetas de casas atras do chao, no lugar das arvores mortas.
            var fundo = Raiz("Decoration_Fundo");
            foreach (var t in Filhos(fundo)) Object.DestroyImmediate(t.gameObject);
            string[] casas = { "house-a", "house-c", "house-b", "house-a", "house-c", "house-b" };
            float[] xs = { -13.2f, -8.4f, -2.2f, 3.6f, 9.0f, 13.6f };
            for (int i = 0; i < casas.Length; i++)
            {
                var casa = Prop(fundo, "Fundo_" + casas[i], casas[i], 2.3f, new Color(0.34f, 0.32f, 0.46f, 1f), -5);
                casa.position = new Vector3(xs[i], casa.position.y - 0.1f, 0f);
                if (i % 2 == 1) casa.localScale = new Vector3(-casa.localScale.x, casa.localScale.y, 1f);
            }

            TrocarFundoPelaCidade();

            Iluminar(new Color(0.52f, 0.55f, 0.76f), 0.5f);
            Musica(Clip(TrilhaDasFasesNovas), 1f, true);

            PosicionarAltares(-3.6f, 4.6f);
            Porta(DestinoPhase4);

            // Inimigos na ordem da tabela de patrulhas (da esquerda para a direita).
            Instanciar(Prefab("SombraRastejante"), -7.0f);
            Instanciar(gato, -1.1f);
            Instanciar(gato, 6.0f);
            Instanciar(gato, 7.8f);

            var objetivos = new GameObject("Sinos").transform;
            NovoSino(objetivos, -13.3f, LevelTerrainBuilder.TopoChao);
            NovoSino(objetivos, 3.5f, 2.2f);   // no telhado alto
            NovoSino(objetivos, 13.1f, LevelTerrainBuilder.TopoChao);

            Salvar(cena);
        }

        static void NovoSino(Transform pai, float x, float superficie)
        {
            // O objeto fica onde o sino se prende a viga. O sino (48 px a 1,8x,
            // 0,86 u) termina 0,37 u acima do chao: o golpe do Ren, na altura do
            // peito, acerta o corpo dele.
            var go = Objetivo<Sino>(pai, "Sino", new Vector3(x, superficie + 1.23f, 0f));

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.75f, 0.8f);
            col.offset = new Vector2(0f, -0.45f);

            var so = new SerializedObject(go.GetComponent<Sino>());
            so.FindProperty("camadaDosInimigos").intValue = LayerMask.GetMask("Enemy");
            so.FindProperty("camadaDoChao").intValue = LayerMask.GetMask("Ground");
            so.FindProperty("madeiraDaCidade").objectReferenceValue = Primeiro(Cidade + "layers/tileset.png");
            so.FindProperty("escala").floatValue = 1.8f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void TrocarFundoPelaCidade()
        {
            var ceu = Primeiro(Cidade + "layers/background.png");
            var cidade = Primeiro(Cidade + "layers/middleground.png");
            var parallax = Raiz("ParallaxBg");

            foreach (var p in parallax.GetComponentsInChildren<ParallaxLayer>(true).ToList())
            {
                var sr = p.GetComponent<SpriteRenderer>();
                if (p.name.StartsWith("background")) { sr.sprite = ceu; sr.color = new Color(0.78f, 0.76f, 0.9f, 1f); }
                else Object.DestroyImmediate(p.gameObject);
            }

            // A cidade na colina, com a igreja no alto: e para la que o Ren vai.
            // Peca de 11,8 u; o LevelLayouts completa as pontas.
            var modelo = parallax.GetComponentsInChildren<ParallaxLayer>(true).First();
            const float escala = 3.07f;
            float largura = cidade.bounds.size.x * escala;
            for (int k = -1; k <= 1; k++)
            {
                var go = Object.Instantiate(modelo.gameObject, parallax.transform);
                go.name = "middleground_0_" + k;
                go.transform.localScale = new Vector3(escala, escala, 1f);
                go.transform.position = new Vector3(k * largura, 1.2f, 18f);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = cidade;
                sr.sortingOrder = -55;
                sr.color = new Color(0.74f, 0.72f, 0.88f, 1f);
                var so = new SerializedObject(go.GetComponent<ParallaxLayer>());
                so.FindProperty("parallaxFactor").floatValue = 0.4f;
                so.FindProperty("lockY").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ------------------------------------------------------------------
        // Phase4 - O Claustro
        // ------------------------------------------------------------------

        static void MontarClaustro(GameObject carnical)
        {
            // Antes de copiar: a bola de fogo e a explosao do chefe vao para os braseiros.
            var chefe = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "AVigilia.prefab")
                                                          .GetComponent<AVigilia>());
            var bola = Sprites(chefe.FindProperty("fireballFrames"));
            var impacto = Sprites(chefe.FindProperty("specialVfxFrames"));

            var cena = NovaCenaAPartirDe("FinalPhase", "Phase4");

            Remover<AVigilia>();
            Remover<IntroDoChefe>();
            Remover<EcosDeAldenor.UI.BossHealthBar>();
            var deco = Raiz("Decoration");
            foreach (var t in Filhos(deco)) if (t.name == "BossAura") Object.DestroyImmediate(t.gameObject);

            Iluminar(new Color(0.58f, 0.44f, 0.56f), 0.45f);
            Musica(Clip(TrilhaDasFasesNovas), 1f, true);

            // A arena tem um altar so; o claustro precisa de dois.
            var altar = Object.FindObjectsByType<Checkpoint>().First();
            var segundo = Object.Instantiate(altar.gameObject);
            segundo.name = "Checkpoint";
            PosicionarAltares(-3.0f, 5.0f);

            // Porta de saida (a arena nao tinha nenhuma, de proposito).
            var porta = new GameObject("PhaseExit");
            porta.transform.position = new Vector3(14f, 0.5f, 0f);
            var gatilho = porta.AddComponent<BoxCollider2D>();
            gatilho.isTrigger = true;
            gatilho.size = new Vector2(1f, 4f);
            porta.AddComponent<PhaseExit>();
            Porta(DestinoFinalPhase);

            // Ninguem patrulha a menos de 1,6 u de um abismo: o empurrao de um
            // golpe (e a disparada do carnical) nao pode terminar no vazio.
            Instanciar(Prefab("GuardiaoDePedra"), -7.0f);
            Instanciar(carnical, -0.5f);
            Instanciar(carnical, 7.4f);

            var objetivos = new GameObject("Cirios").transform;
            NovoCirio(objetivos, -12.6f, LevelTerrainBuilder.TopoChao);
            NovoCirio(objetivos, 5.6f, 3.5f);   // no alto da nave
            NovoCirio(objetivos, 13.2f, LevelTerrainBuilder.TopoChao);

            var armadilhas = new GameObject("Braseiros").transform;
            NovoBraseiro(armadilhas, 2.0f, 0.9f, bola, impacto);     // no degrau
            NovoBraseiro(armadilhas, 6.7f, 3.5f, bola, impacto);     // no alto, sobre o corredor do carnical

            Salvar(cena);
        }

        static void NovoCirio(Transform pai, float x, float superficie)
        {
            var go = Objetivo<Cirio>(pai, "Cirio", new Vector3(x, superficie, 0f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.6f, 1.4f);
            col.offset = new Vector2(0f, 1.0f);

            var so = new SerializedObject(go.GetComponent<Cirio>());
            so.FindProperty("pedraDaIgreja").objectReferenceValue = Primeiro(Igreja + "ENVIRONMENT/tileset.png");
            so.FindProperty("escala").floatValue = 0.9f;
            so.FindProperty("apagarSfx").objectReferenceValue = Clip(Sfx + "8_Atk_Magic_SFX/25_Wind_01.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void NovoBraseiro(Transform pai, float x, float superficie, Sprite[] bola, Sprite[] impacto)
        {
            var go = new GameObject("Braseiro");
            go.transform.SetParent(pai, false);
            go.transform.position = new Vector3(x, superficie, 0f);
            go.AddComponent<SpriteRenderer>();
            var b = go.AddComponent<Braseiro>();

            var so = new SerializedObject(b);
            so.FindProperty("camadaDoJogador").intValue = LayerMask.GetMask("Player");
            so.FindProperty("camadaDoChao").intValue = LayerMask.GetMask("Ground");
            so.FindProperty("escala").floatValue = 1f;
            Lista(so, "framesDaBola", bola);
            Lista(so, "framesDoImpacto", impacto);
            so.FindProperty("lancarSfx").objectReferenceValue = Clip(Sfx + "8_Atk_Magic_SFX/45_Charge_05.wav");
            so.FindProperty("impactoSfx").objectReferenceValue = Clip(Sfx + "8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // Pecas comuns
        // ------------------------------------------------------------------

        static GameObject Objetivo<T>(Transform pai, string nome, Vector3 posicao) where T : ObjetivoDaFase
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.position = posicao;
            go.layer = LayerMask.NameToLayer("Enemy");
            go.AddComponent<SpriteRenderer>();

            // Um ponto de vida: o primeiro golpe cumpre o objetivo.
            var vida = go.AddComponent<HealthSystem>();
            var so = new SerializedObject(vida);
            so.FindProperty("maxHealth").intValue = 1;
            so.FindProperty("invulnerabilityDuration").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<T>();
            return go;
        }

        static UnityEngine.SceneManagement.Scene NovaCenaAPartirDe(string origem, string destino)
        {
            string caminho = Cenas + destino + ".unity";
            AssetDatabase.DeleteAsset(caminho);
            AssetDatabase.CopyAsset(Cenas + origem + ".unity", caminho);
            return EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
        }

        static void Salvar(UnityEngine.SceneManagement.Scene cena)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        static void LigarPortaDaPhase2()
        {
            var cena = EditorSceneManager.OpenScene(Cenas + "Phase2.unity", OpenSceneMode.Single);
            Porta(DestinoPhase3);
            Salvar(cena);
        }

        static void Porta(int destino)
        {
            foreach (var porta in Object.FindObjectsByType<PhaseExit>())
            {
                var so = new SerializedObject(porta);
                so.FindProperty("nextScene").enumValueIndex = destino;
                so.FindProperty("exigirAlmasDaFase").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void PosicionarAltares(params float[] xs)
        {
            var altares = Object.FindObjectsByType<Checkpoint>().OrderBy(c => c.transform.position.x).ToList();
            for (int i = 0; i < altares.Count && i < xs.Length; i++)
                altares[i].transform.position = new Vector3(xs[i], LevelTerrainBuilder.AlturaDoAltar(LevelTerrainBuilder.TopoChao), 0f);
        }

        static void Iluminar(Color cor, float intensidade)
        {
            var luz = Raiz("Lighting")?.GetComponent<Light2D>();
            if (luz == null) return;
            luz.color = cor;
            luz.intensity = intensidade;
            EditorUtility.SetDirty(luz);
        }

        static void Musica(AudioClip clipe, float ganho, bool tocarAoIniciar)
        {
            var player = Object.FindAnyObjectByType<SceneMusicPlayer>();
            var so = new SerializedObject(player);
            so.FindProperty("sceneMusic").objectReferenceValue = clipe;
            so.FindProperty("trackGain").floatValue = ganho;
            so.FindProperty("tocarAoIniciar").boolValue = tocarAoIniciar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Instanciar(GameObject prefab, float x)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(x, 0.5f, 0f);
        }

        static GameObject Prefab(string nome) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + nome + ".prefab");

        /// <summary>Uma peca de cenario do pacote Town, apoiada no chao.</summary>
        static Transform Prop(GameObject pai, string nome, string arquivo, float escala, Color cor, int ordem = 0)
        {
            var sprite = Primeiro(Cidade + "props-sliced/" + arquivo + ".png");
            var go = new GameObject(nome);
            go.transform.SetParent(pai.transform, false);
            go.transform.localScale = new Vector3(escala, escala, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = cor;
            sr.sortingOrder = ordem;
            float altura = sprite.bounds.size.y * escala;
            go.transform.position = new Vector3(0f, LevelTerrainBuilder.TopoChao + altura * 0.5f - 0.04f, 0f);
            return go.transform;
        }

        static void Remover<T>() where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include)) Object.DestroyImmediate(c.gameObject);
        }

        static void RemoverRaiz(string nome)
        {
            var go = Raiz(nome);
            if (go != null) Object.DestroyImmediate(go);
        }

        static GameObject Raiz(string nome) =>
            EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(r => r.name == nome);

        static List<Transform> Filhos(GameObject go)
        {
            var lista = new List<Transform>();
            if (go != null) foreach (Transform t in go.transform) lista.Add(t);
            return lista;
        }

        static void ColocarNaBuild()
        {
            var lista = EditorBuildSettings.scenes
                .Where(s => !s.path.EndsWith("/Phase3.unity") && !s.path.EndsWith("/Phase4.unity")).ToList();
            int depois = lista.FindIndex(s => s.path.EndsWith("/Phase2.unity")) + 1;
            if (depois <= 0) depois = lista.Count;
            lista.Insert(depois, new EditorBuildSettingsScene(Cenas + "Phase4.unity", true));
            lista.Insert(depois, new EditorBuildSettingsScene(Cenas + "Phase3.unity", true));
            EditorBuildSettings.scenes = lista.ToArray();
        }

        /// <summary>
        /// Os props da cidade e os sprites do gato e do carnical vinham com
        /// filtro bilinear: ampliados, borram. Pixel art pede Point.
        /// </summary>
        static void CorrigirImportacao()
        {
            var pastas = new[]
            {
                Cidade + "props-sliced",
                Cidade + "layers",
                Cemiterio + "Characters/Enemies/hell-gato/Sprites",
                Igreja + "SPRITES/burning-ghoul/run 1/sprites",
                Igreja + "SPRITES/burning-ghoul/run 2/sprites",
            };
            foreach (var caminho in AssetDatabase.FindAssets("t:Texture2D", pastas).Select(AssetDatabase.GUIDToAssetPath))
            {
                var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (imp == null) continue;
                if (imp.filterMode == FilterMode.Point && imp.textureCompression == TextureImporterCompression.Uncompressed) continue;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }

        static void Lista(SerializedObject so, string campo, Sprite[] valores)
        {
            var p = so.FindProperty(campo);
            p.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }

        static Sprite[] Sprites(SerializedProperty lista)
        {
            var saida = new Sprite[lista.arraySize];
            for (int i = 0; i < saida.Length; i++) saida[i] = lista.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            return saida;
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

        static AudioClip Clip(string caminho) => AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
    }
}
