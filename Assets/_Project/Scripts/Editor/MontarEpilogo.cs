using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EcosDeAldenor.Enemies;
using EcosDeAldenor.Player;
using EcosDeAldenor.Systems;
using EcosDeAldenor.UI;
using static EcosDeAldenor.EditorTools.MontarPrologo;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// MONTA A CENA DO EPILOGO (parte 2): o amanhecer em Aldenor, com os
    /// creditos. A parte 1 - a despedida do anjo - acontece na propria arena
    /// (DespedidaDoAnjo) e termina carregando esta cena.
    ///
    /// A cena e a cidade da Fase 3 copiada, sem nada de jogo: sai o Ren
    /// jogavel, o HUD, os inimigos, as portas, os altares e o abismo; o chao
    /// vira uma faixa so, os props sao redistribuidos e os tres campanarios
    /// descem para a rua. Quem conduz e o EpilogoNaCidade (moradores, almas,
    /// amanhecer, sinos, o Ren chegando e os creditos).
    ///
    /// A primeira versao do epilogo eram quadros, no estilo do prologo; a cena
    /// dentro do jogo foi o pedido depois de jogar.
    ///
    /// Menu: "Ecos de Aldenor -> Montar epilogo".
    /// </summary>
    public static class MontarEpilogo
    {
        const string Cena = "Assets/_Project/Scenes/Epilogo.unity";
        const string Origem = "Assets/_Project/Scenes/Phase3.unity";
        const string Trilha = "Assets/GameAssets/GothicVania-town-files/GothicVania-town-files/Music/rpg_village02_loop.mp3";

        [MenuItem("Ecos de Aldenor/Montar epilogo")]
        public static void Montar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CorrigirImportacao();

            AssetDatabase.DeleteAsset(Cena);
            AssetDatabase.CopyAsset(Origem, Cena);
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);

            // Nada de jogo.
            Remover<PlayerController>();
            Remover<UIManager>();
            Remover<EnemyBase>();
            Remover<PhaseExit>();
            Remover<FallDeathZone>();
            Remover<Checkpoint>();
            Remover<Fragment>();
            foreach (var nome in new[] { "Decoration_Door", "WallLeft", "WallRight" })
            {
                var go = cena.GetRootGameObjects().FirstOrDefault(r => r.name == nome);
                if (go != null) Object.DestroyImmediate(go);
            }

            // Uma rua so, sem abismo nem plataforma: e uma cidade, nao uma fase.
            var rua = new[] { new Vector2(-15f, 15f) };
            LevelTerrainBuilder.Construir(rua, new Vector3[0], -15f, 15f);
            LevelTerrainBuilder.EspalharDecoracoes(cena, rua, new Vector3[0], 100f, -100f, -15f, 15f);

            // Os campanarios descem para a rua: um de cada lado da praca e um no meio.
            float[] xs = { -10.5f, 3.2f, 10.5f };
            var sinos = Object.FindObjectsByType<Sino>().OrderBy(s => s.transform.position.x).ToList();
            for (int i = 0; i < sinos.Count && i < xs.Length; i++)
                sinos[i].transform.position = new Vector3(xs[i], LevelTerrainBuilder.TopoChao + 1.23f, 0f);

            var musica = Object.FindAnyObjectByType<SceneMusicPlayer>();
            if (musica != null)
            {
                var so = new SerializedObject(musica);
                so.FindProperty("sceneMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Trilha);
                so.FindProperty("trackGain").floatValue = 1f;
                so.FindProperty("tocarAoIniciar").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var camera = Camera.main;
            if (camera != null)
            {
                var seguidor = camera.GetComponent<SceneScript.CameraFollow2D>();
                if (seguidor != null) seguidor.target = null;
            }

            var diretor = new GameObject("EpilogoNaCidade").AddComponent<EpilogoNaCidade>();
            var sod = new SerializedObject(diretor);
            Moradores(sod.FindProperty("moradores"));
            Lista(sod.FindProperty("renCorrendo"), FramesDoClipe(MontarPrologo.HeroKnight + "Animations/HeroKnight_Run.anim"));
            Lista(sod.FindProperty("renParado"), FramesDoClipe(MontarPrologo.HeroKnight + "Animations/HeroKnight_Idle.anim"));
            sod.FindProperty("somDaAlma").objectReferenceValue = Clip(Sfx + "8_Buffs_Heals_SFX/39_Absorb_04.wav");
            sod.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log("Epilogo montado em " + Cena + " a partir da cidade da Fase 3.");
        }

        /// <summary>
        /// Os quatro moradores do prologo e mais dois, espalhados pela rua. Os
        /// tres primeiros ficam na metade esquerda, onde a camera comeca.
        /// </summary>
        static void Moradores(SerializedProperty lista)
        {
            var dados = new (string parado, int nParado, string andando, int nAndando, float x, bool espelhar)[]
            {
                ("oldman-idle", 8, "oldman-walk", 12, -12.5f, false),
                ("woman-idle", 7, "woman-walk", 6, -7.8f, true),
                ("bearded-idle", 5, "bearded-walk", 6, -4.6f, false),
                ("hat-man-idle", 4, "hat-man-walk", 6, 0.6f, true),
                ("woman-idle", 7, "woman-walk", 6, 6.2f, false),
                ("oldman-idle", 8, "oldman-walk", 12, 12.8f, true),
            };
            lista.arraySize = dados.Length;
            for (int i = 0; i < dados.Length; i++)
            {
                var d = dados[i];
                var item = lista.GetArrayElementAtIndex(i);
                Lista(item.FindPropertyRelative("parado"),
                      Frames(Cidade + "sprites/" + d.parado + "/" + d.parado + "-{0}.png", 1, d.nParado));
                Lista(item.FindPropertyRelative("andando"),
                      Frames(Cidade + "sprites/" + d.andando + "/" + d.andando + "-{0}.png", 1, d.nAndando));
                item.FindPropertyRelative("x").floatValue = d.x;
                item.FindPropertyRelative("espelhar").boolValue = d.espelhar;
            }
        }

        static void Lista(SerializedProperty p, Sprite[] valores)
        {
            p.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }

        static void Remover<T>() where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
                if (c != null) Object.DestroyImmediate(c.gameObject);
        }
    }
}
