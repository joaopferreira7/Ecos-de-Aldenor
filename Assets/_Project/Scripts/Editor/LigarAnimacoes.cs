using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// LIGA AS ANIMACOES QUE OS PACOTES JA TRAZIAM.
    ///
    /// Auditoria que motivou esta ferramenta: os personagens do jogo sao sprites
    /// dos pacotes GothicVania, e cada um deles veio com mais clipes do que o
    /// jogo usava - todos no MESMO tamanho de quadro do clipe que ja estava
    /// ligado, ou seja, prontos para entrar sem reposicionar nada:
    ///
    ///   - o chefe usava so o "parado" do mago (5 quadros). O mago tem uma
    ///     conjuracao de 10 quadros, e o pacote tem uma bola de fogo (3) e uma
    ///     morte (9) no mesmo tamanho de quadro dele;
    ///   - o esqueleto usava a caminhada (8) o tempo todo. O pacote tem um
    ///     "levantar do chao" de 6 quadros e uma morte de 5;
    ///   - o fantasma usava 4 quadros. Existe a mesma animacao com halo, para o
    ///     estado de alerta;
    ///   - Ren nunca soltou a poeira (SlideDust) que veio junto com ele.
    ///
    /// Fazer essa ligacao a mao no Inspector sao dezenas de arrastares de sprite
    /// que ninguem consegue revisar depois. Aqui os caminhos ficam escritos, e o
    /// menu "Ecos de Aldenor -> Ligar animacoes dos pacotes" refaz tudo se algum
    /// prefab for reimportado.
    /// </summary>
    public static class LigarAnimacoes
    {
        const string Cemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/";
        const string Igreja = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/";
        const string HeroKnight = "Assets/GameAssets/Hero Knight - Pixel Art/";
        const string Sfx = "Assets/GameAssets/Leohpaz/RPG_Essentials_Free/";

        [MenuItem("Ecos de Aldenor/Ligar animacoes dos pacotes")]
        public static void Ligar()
        {
            var log = new StringBuilder("Animacoes ligadas aos prefabs:\n");

            // --- Ren: poeira do rolamento e da aterrissagem -------------------
            var ren = Carregar("Assets/_Project/Prefabs/Player/Ren.prefab");
            var pc = Achar(ren, "PlayerController");
            if (pc != null)
            {
                var so = new SerializedObject(pc);
                Sprites(so, "dustFrames", SubSprites(HeroKnight + "Sprites/SlideDust.png"));
                Objeto(so, "rollSfx", Clip(Sfx + "8_Atk_Magic_SFX/25_Wind_01.wav"));
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine("  Ren: poeira (SlideDust, 5 quadros) + som do rolamento");
            }
            Salvar(ren);

            // --- Sombra Rastejante (fantasma) ---------------------------------
            var sombra = Carregar("Assets/_Project/Prefabs/Enemies/SombraRastejante.prefab");
            var sombraEnemy = Achar(sombra, "SombraRastejante");
            if (sombraEnemy != null)
            {
                var so = new SerializedObject(sombraEnemy);
                Sprites(so, "framesParado", Frames(Cemiterio + "Characters/Enemies/ghost/Sprites/ghost-{0}.png", 1, 4));
                so.FindProperty("fpsParado").floatValue = 6f;
                Sprites(so, "framesPerseguindo", Frames(Cemiterio + "Characters/Enemies/ghost/SpritesHalo/ghost-halo-{0}.png", 1, 4));
                so.FindProperty("fpsPerseguindo").floatValue = 10f;
                Sprites(so, "framesMorte", Frames(Cemiterio + "Characters/Enemies/EnemyDeath/Sprites/enemy-death-{0}.png", 1, 5));
                so.FindProperty("fpsMorte").floatValue = 12f;
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine("  Sombra Rastejante: parado (4) + alerta com halo (4) + morte (5)");
            }
            Salvar(sombra);

            // --- Guardiao de Pedra (esqueleto) --------------------------------
            var guardiao = Carregar("Assets/_Project/Prefabs/Enemies/GuardiaoDePedra.prefab");
            var guardiaoEnemy = Achar(guardiao, "GuardiaoDePedra");
            if (guardiaoEnemy != null)
            {
                var andar = Frames(Cemiterio + "Characters/Enemies/skeleton/Sprites/Walk/skeleton-{0}.png", 1, 8);
                var so = new SerializedObject(guardiaoEnemy);
                Sprites(so, "framesParado", andar.Take(1).ToList());
                so.FindProperty("fpsParado").floatValue = 6f;
                Sprites(so, "framesAndando", andar);
                so.FindProperty("fpsAndando").floatValue = 10f;
                Sprites(so, "framesDespertar", Frames(Cemiterio + "Characters/Enemies/skeleton/skeleton-rise/skeleton-rise-{0}.png", 1, 6));
                so.FindProperty("fpsDespertar").floatValue = 9f;
                Objeto(so, "despertarSfx", Clip(Sfx + "8_Buffs_Heals_SFX/30_Revive_03.wav"));
                Sprites(so, "framesMorte", Frames(Cemiterio + "Characters/Enemies/EnemyDeath/Sprites/enemy-death-{0}.png", 1, 5));
                so.FindProperty("fpsMorte").floatValue = 12f;
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine("  Guardiao de Pedra: caminhada (8) + levantar do chao (6) + morte (5)");
            }
            Salvar(guardiao);

            // --- A Vigilia (mago) ---------------------------------------------
            var chefe = Carregar("Assets/_Project/Prefabs/Enemies/AVigilia.prefab");
            var chefeEnemy = Achar(chefe, "AVigilia");
            if (chefeEnemy != null)
            {
                var so = new SerializedObject(chefeEnemy);
                Sprites(so, "framesConjurar", Frames(Igreja + "SPRITES/wizard/Fire/sprites/f-{0:00}.png", 1, 10));
                Sprites(so, "fireballFrames", Frames(Igreja + "SPRITES/fx/fireball/fireball-sprites/fireball{0}.png", 1, 3));
                so.FindProperty("fireballFps").floatValue = 14f;
                Sprites(so, "framesMorte", Frames(Igreja + "SPRITES/fx/enemy-death/enemy-death-sprites/enemy-death{0}.png", 1, 9));
                so.FindProperty("fpsMorte").floatValue = 12f;
                Objeto(so, "castSfx", Clip(Sfx + "8_Atk_Magic_SFX/45_Charge_05.wav"));
                // A virada de fase usava o MESMO toque de carga da conjuracao;
                // dois momentos diferentes com o mesmo som viram um so na
                // cabeca de quem joga. A virada ganha um toque de encontro.
                Objeto(so, "phaseChangeSfx", Clip(Sfx + "10_Battle_SFX/55_Encounter_02.wav"));
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine("  A Vigilia: conjuracao (10) + bola de fogo (3) + morte (9)");
            }
            Salvar(chefe);

            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Poe som nos botoes de todas as telas, e nos toques de pausa.
        ///
        /// Os menus eram mudos: o unico retorno de um clique era a troca de cena
        /// que chega meio segundo depois, atras do fade. Os toques ja estavam no
        /// projeto (pacote Leohpaz, 10_UI_Menu_SFX) sem nenhum uso.
        /// </summary>
        [MenuItem("Ecos de Aldenor/Ligar sons de interface")]
        public static void LigarSonsDeUI()
        {
            var foco = Clip(Sfx + "10_UI_Menu_SFX/001_Hover_01.wav");
            var clique = Clip(Sfx + "10_UI_Menu_SFX/013_Confirm_03.wav");

            string[] cenas = { "MainMenu", "Tutorial", "Phase1", "Phase2", "FinalPhase",
                               "VictoryScreen", "GameOverScreen", "Credits" };
            int postos = 0;

            foreach (var nome in cenas)
            {
                var cena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    "Assets/_Project/Scenes/" + nome + ".unity",
                    UnityEditor.SceneManagement.OpenSceneMode.Single);

                var alvo = GameObject.Find("UISounds");
                if (alvo == null)
                {
                    alvo = new GameObject("UISounds");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(alvo, cena);
                }

                var comp = alvo.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "UISounds");
                if (comp == null) comp = alvo.AddComponent(System.Type.GetType("EcosDeAldenor.UI.UISounds, Assembly-CSharp"));

                var so = new SerializedObject(comp);
                Objeto(so, "somDeFoco", foco);
                Objeto(so, "somDeClique", clique);
                so.ApplyModifiedPropertiesWithoutUndo();

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cena);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cena);
                postos++;
            }

            // Pausa e retomada ficam no HUD, que e um prefab usado nas fases.
            var hud = Carregar("Assets/_Project/Prefabs/UI/HUDCanvas.prefab");
            var pause = Achar(hud, "PauseManager");
            if (pause != null)
            {
                var so = new SerializedObject(pause);
                Objeto(so, "pausarSfx", Clip(Sfx + "10_UI_Menu_SFX/092_Pause_04.wav"));
                Objeto(so, "retomarSfx", Clip(Sfx + "10_UI_Menu_SFX/098_Unpause_04.wav"));
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hud);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Sons de interface ligados em " + postos + " cenas + toques de pausa no HUD.");
        }

        // --- utilidades ------------------------------------------------------

        static GameObject Carregar(string caminho)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (go == null) Debug.LogError("Prefab nao encontrado: " + caminho);
            return go;
        }

        static void Salvar(GameObject prefab)
        {
            if (prefab != null) EditorUtility.SetDirty(prefab);
        }

        /// <summary>Acha um componente pelo nome do tipo (evita depender do namespace).</summary>
        static Component Achar(GameObject prefab, string tipo)
        {
            if (prefab == null) return null;
            var c = prefab.GetComponents<Component>().FirstOrDefault(x => x != null && x.GetType().Name == tipo);
            if (c == null) Debug.LogError("Componente " + tipo + " nao encontrado em " + prefab.name);
            return c;
        }

        /// <summary>Todos os sprites de um PNG (um arquivo pode conter varios).</summary>
        static List<Sprite> SubSprites(string caminho)
        {
            var todos = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>()
                                     .OrderBy(s => s.name, System.StringComparer.Ordinal).ToList();
            if (todos.Count == 0) Debug.LogWarning("Sem sprites em " + caminho);
            return todos;
        }

        /// <summary>Sequencia de PNGs numerados, na ordem dos quadros.</summary>
        static List<Sprite> Frames(string formato, int de, int ate)
        {
            var lista = new List<Sprite>();
            for (int i = de; i <= ate; i++)
            {
                string caminho = string.Format(formato, i);
                var s = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
                if (s == null) { Debug.LogWarning("Quadro faltando: " + caminho); continue; }
                lista.Add(s);
            }
            return lista;
        }

        static AudioClip Clip(string caminho)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
            if (c == null) Debug.LogWarning("Som nao encontrado: " + caminho);
            return c;
        }

        static void Sprites(SerializedObject so, string campo, List<Sprite> valores)
        {
            var prop = so.FindProperty(campo);
            if (prop == null) { Debug.LogWarning("Campo inexistente: " + campo); return; }

            prop.arraySize = valores.Count;
            for (int i = 0; i < valores.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }

        static void Objeto(SerializedObject so, string campo, Object valor)
        {
            var prop = so.FindProperty(campo);
            if (prop == null) { Debug.LogWarning("Campo inexistente: " + campo); return; }
            prop.objectReferenceValue = valor;
        }
    }
}
