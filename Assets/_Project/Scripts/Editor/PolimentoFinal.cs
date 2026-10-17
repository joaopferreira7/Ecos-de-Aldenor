using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EcosDeAldenor.Systems;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// POLIMENTO FINAL - itens 15 a 24 da revisao.
    ///
    ///   - Creditos: nomes da equipe sem a virgula sobrando e em duas linhas;
    ///     blocos surgindo em sequencia.
    ///   - Fases: correntes soltas no ceu removidas; nevoa rasteira e almas no
    ///     ar (AmbienteVivo); emenda do fundo da igreja fechada.
    ///   - Ren: uma faisca diferente para cada golpe do combo.
    ///   - Trilhas longas tocadas em Streaming.
    ///   - Limpeza: pastas __MACOSX importadas como asset e a SampleScene.
    ///
    /// (A grama nas plataformas e a placa do HUD vem do LevelTerrainBuilder e
    /// do UIManager; esta ferramenta reconstroi as fases para aplicar a primeira.)
    ///
    /// Como as outras ferramentas do projeto, pode rodar quantas vezes quiser.
    /// </summary>
    public static class PolimentoFinal
    {
        const string Cenas = "Assets/_Project/Scenes/";
        const string Faiscas = "Assets/GameAssets/Hitspark FX Batch1 sheet (png&jpg)/";
        const string PrefabRen = "Assets/_Project/Prefabs/Player/Ren.prefab";
        const string MaterialSemLuz = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static readonly string[] Fases = { "Tutorial", "Phase1", "Phase2", "FinalPhase" };

        [MenuItem("Ecos de Aldenor/Polimento final (creditos, fases, faiscas, limpeza)")]
        public static void Aplicar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var log = new StringBuilder("Polimento final:\n");

            Faisca(log);
            Musica(log);

            // A grama das plataformas vem da reconstrucao do terreno.
            LevelLayouts.ReconstruirTudo();

            foreach (var fase in Fases)
            {
                var cena = EditorSceneManager.OpenScene(Cenas + fase + ".unity");
                log.AppendLine("  " + fase + ": " + Fase(fase));
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            var creditos = EditorSceneManager.OpenScene(Cenas + "Credits.unity");
            log.AppendLine("  Credits: " + Creditos());
            EditorSceneManager.MarkSceneDirty(creditos);
            EditorSceneManager.SaveScene(creditos);

            Limpeza(log);

            EditorSceneManager.OpenScene(Cenas + "MainMenu.unity");
            Debug.Log(log.ToString());
        }

        // ------------------------------------------------------------------
        // Fases
        // ------------------------------------------------------------------

        static string Fase(string fase)
        {
            var partes = new List<string>();
            bool igreja = fase == "FinalPhase";

            // 19. Correntes: diagonais no meio do ceu, presas a nada.
            int correntes = 0;
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Chain") && t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        correntes++;
                    }
            if (correntes > 0) partes.Add(correntes + " correntes soltas desligadas");

            // 17. Emenda do fundo da igreja: a fatia da parede tem 1 px
            // transparente na borda direita, e a camera via o vazio entre uma
            // copia e a outra como uma risca escura de cima a baixo.
            if (igreja)
            {
                var paredes = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                    .Where(s => s.name.StartsWith("ChurchWall_")).ToList();
                foreach (var sr in paredes)
                {
                    if (!int.TryParse(sr.name.Substring("ChurchWall_".Length), out int i)) continue;
                    float escala = sr.transform.lossyScale.x;
                    float pixel = escala / sr.sprite.pixelsPerUnit;
                    float passo = sr.sprite.rect.width * pixel - pixel;   // sobrepoe a coluna vazia
                    var p = sr.transform.position;
                    sr.transform.position = new Vector3(i * passo, p.y, p.z);
                }
                if (paredes.Count > 0) partes.Add("emenda do fundo fechada (" + paredes.Count + " paredes)");
            }

            // 21. Movimento de fundo.
            var amb = Object.FindAnyObjectByType<AmbienteVivo>(FindObjectsInactive.Include);
            if (amb == null) amb = new GameObject("AmbienteVivo").AddComponent<AmbienteVivo>();
            var so = new SerializedObject(amb);
            so.FindProperty("material").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(MaterialSemLuz);
            so.FindProperty("baseDaNevoa").floatValue = LevelTerrainBuilder.TopoChao - 0.12f;   // nasce na linha do chao
            so.FindProperty("corDaNevoa").colorValue = igreja ? new Color(0.58f, 0.45f, 0.78f, 0.18f)
                                                              : new Color(0.62f, 0.56f, 0.82f, 0.22f);
            so.FindProperty("quantidade").intValue = igreja ? 10 : 16;
            so.ApplyModifiedPropertiesWithoutUndo();
            partes.Add("nevoa e almas no ar");
            if (so.FindProperty("material").objectReferenceValue == null) partes.Add("AVISO: material sem luz nao encontrado");

            return string.Join(", ", partes);
        }

        // ------------------------------------------------------------------
        // 15. Creditos
        // ------------------------------------------------------------------

        static string Creditos()
        {
            var nomes = GameObject.Find("TeamNames");
            if (nomes != null)
            {
                // "Integrantes: Gabriel Bressanin, " + um nome por linha: a
                // virgula sobrava e a lista ocupava sete linhas.
                nomes.GetComponent<Text>().text =
                    "Desenvolvido pelo grupo da disciplina\n" +
                    "Desenvolvimento de Jogos Digitais (P2)\n\n" +
                    "Gabriel Bressanin  ·  Guilherme Barros  ·  Heitor Lupino\n" +
                    "João Pedro Ferreira  ·  Matheus Risso";
            }

            var conteudo = GameObject.Find("CreditsContent");
            if (conteudo != null && conteudo.GetComponent<EntradaEmSequencia>() == null)
                conteudo.AddComponent<EntradaEmSequencia>();
            return "equipe em duas linhas, blocos surgindo em sequencia";
        }

        // ------------------------------------------------------------------
        // 20. Faiscas por golpe
        // ------------------------------------------------------------------

        static void Faisca(StringBuilder log)
        {
            var raiz = PrefabUtility.LoadPrefabContents(PrefabRen);
            var pc = raiz.GetComponent<EcosDeAldenor.Player.PlayerController>();
            var so = new SerializedObject(pc);
            Quadros(so.FindProperty("hitSparkFrames"), "Hitspark FX Batch1 Line1 sheet.png");
            Quadros(so.FindProperty("hitSparkGolpe2"), "Hitspark FX Batch1 Line3 sheet.png");
            Quadros(so.FindProperty("hitSparkFinal"), "Hitspark FX Batch1 Star5 sheet.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabRen);
            PrefabUtility.UnloadPrefabContents(raiz);
            log.AppendLine("  Ren: golpe 1 risco curto, golpe 2 risco longo, golpe final estrela maior");
        }

        static void Quadros(SerializedProperty arr, string arquivo)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Faiscas + arquivo).OfType<Sprite>()
                          .OrderBy(s => s.rect.x).ToArray();
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        // ------------------------------------------------------------------
        // 22. Musica em Streaming
        // ------------------------------------------------------------------

        static void Musica(StringBuilder log)
        {
            // Trilhas de minutos inteiras descomprimidas na memoria ao carregar
            // a cena: dezenas de MB e uma travadinha em cada troca de fase.
            var cenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var trocadas = new List<string>();
            foreach (var caminho in AssetDatabase.GetDependencies(cenas, true).Distinct())
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
                if (clip == null || clip.length < 30f) continue;
                var imp = (AudioImporter)AssetImporter.GetAtPath(caminho);
                var cfg = imp.defaultSampleSettings;
                if (cfg.loadType == AudioClipLoadType.Streaming) continue;
                cfg.loadType = AudioClipLoadType.Streaming;
                imp.defaultSampleSettings = cfg;
                imp.SaveAndReimport();
                trocadas.Add(System.IO.Path.GetFileName(caminho));
            }
            log.AppendLine("  Musica em Streaming: " + (trocadas.Count > 0 ? string.Join(", ", trocadas) : "ja estava"));
        }

        // ------------------------------------------------------------------
        // 23 e 24. Limpeza
        // ------------------------------------------------------------------

        static void Limpeza(StringBuilder log)
        {
            int apagadas = 0;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets/GameAssets" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/__MACOSX") && AssetDatabase.IsValidFolder(p) && AssetDatabase.DeleteAsset(p)) apagadas++;
            }
            log.AppendLine("  Pastas __MACOSX apagadas: " + apagadas);

            const string sample = "Assets/Scenes/SampleScene.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sample) != null)
            {
                AssetDatabase.DeleteAsset(sample);
                if (AssetDatabase.IsValidFolder("Assets/Scenes") &&
                    AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }).Length == 0)
                    AssetDatabase.DeleteAsset("Assets/Scenes");
                log.AppendLine("  SampleScene apagada");
            }
        }
    }
}
