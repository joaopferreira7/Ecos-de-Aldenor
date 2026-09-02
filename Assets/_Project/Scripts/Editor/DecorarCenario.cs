using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// SILHUETAS DE FUNDO: as arvores, estatuas e colunas que os pacotes ja
    /// tinham e o cenario nunca usou.
    ///
    /// O cemiterio do jogo era decorado com quatro pecas do pacote Brackeys
    /// (lapide, pedra, cranio, porta) repetidas ao longo da fase. Enquanto isso,
    /// o pacote que fornece o PROPRIO FUNDO do cemiterio - o mesmo desenho, a
    /// mesma paleta - traz em objects.png quatro lapides, duas arvores mortas,
    /// uma estatua encapuzada e dois arbustos que nunca foram abertos. A igreja
    /// da luta final tem uma coluna (column.png) igualmente parada.
    ///
    /// Elas entram como CAMADA DE FUNDO, atras do terreno: nao disputam espaco
    /// de chao com as lapides que ja estao la (o distribuidor de decoracao
    /// desliga as pecas que nao cabem), nao tem colisor e nao mudam nenhuma
    /// medida de level design. Sao profundidade, e so - que e exatamente o que
    /// faltava numa fase onde o olhar ia do chao direto para o ceu.
    ///
    /// Rodar de novo refaz a camada do zero, entao a ferramenta e repetivel.
    /// </summary>
    public static class DecorarCenario
    {
        const string Cemiterio = "Assets/GameAssets/gothicvania-cemetery-files/gothicvania-cemetery-files/Assets/";
        const string Igreja = "Assets/GameAssets/gothicvania church files/gothicvania church files/Assets/";

        const string RaizFundo = "Decoration_Fundo";
        const int OrdemFundo = -5;   // atras do terreno (-2) e a frente do parallax

        [MenuItem("Ecos de Aldenor/Decorar cenario (props dos pacotes)")]
        public static void Decorar()
        {
            var log = new StringBuilder("Camada de fundo refeita:\n");

            foreach (var fase in LevelLayouts.Fases)
            {
                if (fase.cena == "FinalPhase") continue;   // a igreja tem tratamento proprio
                log.AppendLine("  " + fase.cena + ": " + DecorarCemiterio(fase) + " pecas");
            }

            log.AppendLine("  FinalPhase: " + DecorarIgreja() + " pecas");
            Debug.Log(log.ToString());
        }

        // --- cemiterio -------------------------------------------------------

        static int DecorarCemiterio(LevelLayouts.Fase fase)
        {
            var cena = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + fase.cena + ".unity",
                                                    OpenSceneMode.Single);
            var raiz = RecriarRaiz(cena);

            // objects.png ja vem fatiado: 0-3 lapides, 4 e 8 arvores mortas,
            // 5 arbusto grande, 6 arbusto pequeno, 7 estatua, 9 arvore grande.
            var objetos = Fatias(Cemiterio + "Environment/objects.png");
            if (objetos.Count < 10) { Debug.LogWarning("objects.png sem as 10 fatias esperadas."); return 0; }

            // A ordem alterna silhueta ALTA e BAIXA: so arvores viram uma fileira,
            // so arbustos somem no chao.
            Sprite[] ciclo =
            {
                objetos[9],  // arvore grande
                objetos[5],  // arbusto grande
                objetos[4],  // arvore morta
                objetos[7],  // estatua encapuzada
                objetos[8],  // arvore morta (variacao)
                objetos[6],  // arbusto pequeno
            };

            float topo = TopoDoChao(cena);
            var rng = new System.Random(fase.cena.GetHashCode());
            int postas = 0;
            int i = 0;

            foreach (var seg in fase.segmentos)
            {
                // Uma peca a cada ~4,5 u, com folga nas pontas do segmento para a
                // silhueta nao nascer pela metade em cima de um abismo.
                for (float x = seg.x + 1.4f; x <= seg.y - 1.4f; x += 3.4f)
                {
                    var sprite = ciclo[i++ % ciclo.Length];
                    float jitter = (float)(rng.NextDouble() * 1.6 - 0.8);
                    Criar(raiz.transform, sprite, x + jitter, topo, rng);
                    postas++;
                }
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            return postas;
        }

        // --- igreja ----------------------------------------------------------

        static int DecorarIgreja()
        {
            var cena = EditorSceneManager.OpenScene("Assets/_Project/Scenes/FinalPhase.unity", OpenSceneMode.Single);
            var raiz = RecriarRaiz(cena);

            var coluna = Fatias(Igreja + "ENVIRONMENT/column.png").FirstOrDefault();
            if (coluna == null) { Debug.LogWarning("column.png nao encontrado."); return 0; }

            var fase = LevelLayouts.Fases.FirstOrDefault(f => f.cena == "FinalPhase");
            float topo = TopoDoChao(cena);
            var rng = new System.Random(99);
            int postas = 0;

            // Colunas espacadas ao longo da arena: e o que faz uma sala parecer
            // uma NAVE de igreja, em vez de um corredor com uma parede pintada.
            foreach (var seg in fase.segmentos)
            {
                for (float x = seg.x + 2.5f; x <= seg.y - 2.5f; x += 6.5f)
                {
                    Criar(raiz.transform, coluna, x, topo, rng, 1.15f);
                    postas++;
                }
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            return postas;
        }

        // --- utilidades ------------------------------------------------------

        static GameObject RecriarRaiz(UnityEngine.SceneManagement.Scene cena)
        {
            foreach (var root in cena.GetRootGameObjects())
                if (root.name == RaizFundo) Object.DestroyImmediate(root);

            var novo = new GameObject(RaizFundo);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(novo, cena);
            return novo;
        }

        /// <summary>
        /// Topo do chao andavel, lido da propria cena (os blocos "Chao_*" que o
        /// construtor de terreno cria). Assim a silhueta pousa no chao mesmo que
        /// a altura do terreno mude.
        /// </summary>
        static float TopoDoChao(UnityEngine.SceneManagement.Scene cena)
        {
            foreach (var root in cena.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Chao_")) continue;
                    if (t.name.EndsWith("_Terra") || t.name.EndsWith("_Borda")) continue;

                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    return t.position.y + sr.size.y * 0.5f;
                }
            }
            return -0.4f;   // medida atual das fases, caso a cena mude de nomes
        }

        static void Criar(Transform pai, Sprite sprite, float x, float topoDoChao,
                          System.Random rng, float escala = 1f)
        {
            var go = new GameObject("Fundo_" + sprite.name);
            go.transform.SetParent(pai, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = OrdemFundo;

            // Perspectiva aerea, MAS numa cena que ja e escura.
            //
            // A primeira versao escurecia as silhuetas para 45% e elas
            // simplesmente sumiam: um cemiterio noturno nao tem contraste
            // sobrando para gastar. Aqui o afastamento e dado quase todo pelo
            // desvio para o azul do ceu, com pouquissima perda de luz - a peca
            // continua legivel e mesmo assim nao briga com o personagem.
            // As arvores do pacote sao marrom-quente e o cemiterio inteiro e
            // violeta: sem correcao elas viram as manchas mais chamativas da
            // tela, na frente do proprio personagem. O tom violeta abaixo as
            // devolve para a paleta da cena sem apaga-las.
            float luz = 0.78f + (float)rng.NextDouble() * 0.12f;
            sr.color = new Color(luz * 0.74f, luz * 0.62f, luz * 0.92f, 1f);

            // Espelhar metade das pecas evita a leitura de "copia e cola".
            if (rng.Next(2) == 0) sr.flipX = true;

            float variacao = escala * (0.9f + (float)rng.NextDouble() * 0.25f);
            go.transform.localScale = new Vector3(variacao, variacao, 1f);

            // Pivo central: sobe metade da altura para o pe encostar no chao.
            float altura = sprite.bounds.size.y * variacao;
            go.transform.position = new Vector3(x, topoDoChao + altura * 0.5f - 0.1f, 0f);
        }

        static List<Sprite> Fatias(string caminho)
        {
            return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>()
                   .OrderBy(s => Indice(s.name)).ToList();
        }

        /// <summary>Ordena "objects_10" depois de "objects_9" (e nao antes, como no texto).</summary>
        static int Indice(string nome)
        {
            int p = nome.LastIndexOf('_');
            int n;
            if (p >= 0 && int.TryParse(nome.Substring(p + 1), out n)) return n;
            return 0;
        }
    }
}
