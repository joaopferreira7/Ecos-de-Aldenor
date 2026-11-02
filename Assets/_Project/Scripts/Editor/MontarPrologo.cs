using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EcosDeAldenor.Enemies;
using EcosDeAldenor.Player;
using EcosDeAldenor.Systems;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// MONTA O PROLOGO: a historia de Aldenor contada DENTRO do jogo, nos
    /// cenarios de verdade, em tres partes:
    ///
    ///   Prologo        a cidade (a da Fase 3, sem nada de jogo): o entardecer,
    ///                  os sinos, a nevoa, as almas arrancadas - PrologoNaCidade;
    ///   PrologoIgreja  a arena da igreja: o anjo, as almas pedindo abrigo, o
    ///                  manto, os ecos espalhados - PrologoNaIgreja;
    ///   Tutorial       o Ren desperta entre as lapides e o controle passa ao
    ///                  jogador - DespertarDoRen.
    ///
    /// A primeira versao eram cinco quadros num quadro de cinema (o componente
    /// Prologo). Depois que o epilogo virou cena dentro do jogo, o comeco e o
    /// fim ficaram desiguais; agora um espelha o outro, no mesmo palco.
    ///
    /// Os caminhos dos pacotes e os utilitarios daqui tambem servem as outras
    /// ferramentas (MontarEpilogo).
    ///
    /// Menu: "Ecos de Aldenor -> Montar prologo".
    /// </summary>
    public static class MontarPrologo
    {
        const string Cena = "Assets/_Project/Scenes/Prologo.unity";
        const string CenaDaIgreja = "Assets/_Project/Scenes/PrologoIgreja.unity";
        const string Arena = "Assets/_Project/Scenes/FinalPhase.unity";
        const string Tutorial = "Assets/_Project/Scenes/Tutorial.unity";
        const string Menu = "Assets/GameAssets/2D Dungeon Tilemap/Musics & Sounds CC0/Lobby Music.wav";

        internal const string Cidade = "Assets/GameAssets/GothicVania-town-files/GothicVania-town-files/PNG/";
        internal const string Cemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/";
        internal const string Igreja = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/";
        internal const string HeroKnight = "Assets/GameAssets/Hero Knight - Pixel Art/";
        internal const string Sfx = "Assets/GameAssets/Leohpaz/RPG_Essentials_Free/";
        internal const string Alma = "Assets/Brackeys/2D Mega Pack/Items & Icons/Gothic/SoulFragment.png";

        [MenuItem("Ecos de Aldenor/Montar prologo")]
        public static void Montar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CorrigirImportacao();
            MontarCidade();
            MontarIgreja();
            PrepararTutorial();
            ColocarNaBuild();
            Debug.Log("Prologo montado: Prologo (cidade), PrologoIgreja e o despertar no Tutorial.");
        }

        // ------------------------------------------------------------------

        static void MontarCidade()
        {
            var cena = MontarEpilogo.CidadeSemJogo(Cena);

            // A trilha do menu continua (o AudioManager nao reinicia a mesma
            // faixa) ate a nevoa calar tudo.
            var musica = Object.FindAnyObjectByType<SceneMusicPlayer>();
            if (musica != null)
            {
                var so = new SerializedObject(musica);
                so.FindProperty("sceneMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Menu);
                so.FindProperty("tocarAoIniciar").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var diretor = new GameObject("PrologoNaCidade").AddComponent<PrologoNaCidade>();
            var sod = new SerializedObject(diretor);
            MontarEpilogo.Moradores(sod.FindProperty("moradores"));
            sod.FindProperty("trovao").objectReferenceValue = Clip(Sfx + "8_Atk_Magic_SFX/18_Thunder_02.wav");
            sod.FindProperty("somDaAlma").objectReferenceValue = Clip(Sfx + "8_Buffs_Heals_SFX/39_Absorb_04.wav");
            sod.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        static void MontarIgreja()
        {
            AssetDatabase.DeleteAsset(CenaDaIgreja);
            AssetDatabase.CopyAsset(Arena, CenaDaIgreja);
            var cena = EditorSceneManager.OpenScene(CenaDaIgreja, OpenSceneMode.Single);

            // Nada de jogo: so a nave, as luzes e a nevoa. A trilha da arena ja
            // comeca calada (a luta so a toca quando o chefe se revela).
            MontarEpilogo.Remover<PlayerController>();
            MontarEpilogo.Remover<UIManager>();
            MontarEpilogo.Remover<EnemyBase>();
            MontarEpilogo.Remover<IntroDoChefe>();
            MontarEpilogo.Remover<Checkpoint>();

            var diretor = new GameObject("PrologoNaIgreja").AddComponent<PrologoNaIgreja>();
            var so = new SerializedObject(diretor);
            MontarEpilogo.Lista(so.FindProperty("anjo"), Frames(Igreja + "SPRITES/angel/idle/sprites/f-{0:00}.png", 1, 8));
            MontarEpilogo.Lista(so.FindProperty("mago"), Frames(Igreja + "SPRITES/wizard/Idle/sprites/f-{0:00}.png", 1, 5));
            MontarEpilogo.Lista(so.FindProperty("fumaca"), Frames(Igreja + "SPRITES/fx/enemy-death/enemy-death-sprites/enemy-death{0}.png", 1, 9));
            so.FindProperty("trovao").objectReferenceValue = Clip(Sfx + "8_Atk_Magic_SFX/18_Thunder_02.wav");
            so.FindProperty("somSombrio").objectReferenceValue = Clip(Sfx + "8_Buffs_Heals_SFX/21_Debuff_01.wav");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        /// <summary>O tutorial ganha o despertar do Ren, que so roda vindo do prologo.</summary>
        static void PrepararTutorial()
        {
            var cena = EditorSceneManager.OpenScene(Tutorial, OpenSceneMode.Single);
            var despertar = Object.FindAnyObjectByType<DespertarDoRen>();
            if (despertar == null) despertar = new GameObject("DespertarDoRen").AddComponent<DespertarDoRen>();

            var so = new SerializedObject(despertar);
            MontarEpilogo.Lista(so.FindProperty("renMorrendo"), FramesDoClipe(HeroKnight + "Animations/HeroKnight_DeathNoBlood.anim"));
            so.FindProperty("somDoDespertar").objectReferenceValue = Clip(Sfx + "8_Buffs_Heals_SFX/30_Revive_03.wav");
            so.FindProperty("somDoEco").objectReferenceValue = Clip(Sfx + "8_Buffs_Heals_SFX/39_Absorb_04.wav");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        /// <summary>Prologo logo depois do menu, e a igreja logo depois dele.</summary>
        static void ColocarNaBuild()
        {
            var lista = EditorBuildSettings.scenes.Where(s => s.path != Cena && s.path != CenaDaIgreja).ToList();
            int depoisDoMenu = lista.FindIndex(s => s.path.EndsWith("MainMenu.unity")) + 1;
            depoisDoMenu = Mathf.Clamp(depoisDoMenu, 0, lista.Count);
            lista.Insert(depoisDoMenu, new EditorBuildSettingsScene(CenaDaIgreja, true));
            lista.Insert(depoisDoMenu, new EditorBuildSettingsScene(Cena, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }

        // ------------------------------------------------------------------
        // Utilitarios (usados tambem pelas outras ferramentas)
        // ------------------------------------------------------------------

        /// <summary>
        /// A arte da cidade e os moradores nunca tinham sido usados e vinham com
        /// filtro bilinear e compressao: ampliados 3x, ficam borrados e com
        /// manchas. Pixel art pede Point e sem compressao, como o resto do jogo.
        /// </summary>
        internal static void CorrigirImportacao()
        {
            var pastas = new[]
            {
                Cidade + "sprites/oldman-walk",
                Cidade + "sprites/bearded-idle",
                Cidade + "sprites/hat-man-idle",
                Cidade + "sprites/woman-walk",
                Cidade + "sprites/oldman-idle",
                Cidade + "sprites/woman-idle",
                Cidade + "sprites/bearded-walk",
                Cidade + "sprites/hat-man-walk",
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

        internal static Sprite Primeiro(string caminho)
        {
            var s = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
            if (s == null) Debug.LogWarning("Sprite nao encontrado: " + caminho);
            return s;
        }

        internal static Sprite[] Frames(string formato, int de, int ate)
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
        internal static Sprite[] FramesDoClipe(string caminho)
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

        internal static AudioClip Clip(string caminho) => AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
    }
}
