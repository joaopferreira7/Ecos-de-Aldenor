using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// Ferramenta de editor que monta o terreno de uma fase a partir de uma
    /// descricao simples: uma lista de segmentos de chao (o espaco entre dois
    /// segmentos vira um vao mortal) e uma lista de plataformas suspensas.
    ///
    /// Ela existe porque as fases nasceram como uma unica faixa plana de 30u,
    /// sem nada para pular. Descrever o relevo por dados - e nao arrastando
    /// objetos na mao - deixa o balanceamento reproduzivel: basta mudar os
    /// numeros e reconstruir.
    ///
    /// Limites medidos do pulo do Ren (jumpForce 11, gravityScale 3, drag 0.5):
    ///   altura maxima ...... 1.73u
    ///   alcance horizontal . 3.00u no mesmo nivel
    /// Por isso as regras de projeto sao: degrau ate 1.3u e vao ate 2.3u.
    /// </summary>
    public static class LevelTerrainBuilder
    {
        // Paleta do "solo coeso gotico" ja adotada no projeto.
        //
        // As plataformas usam pedra MAIS CLARA que o chao e um friso de topo bem
        // destacado: numa cena escura, o jogador precisa distinguir de relance
        // onde da para pisar. Contraste na aresta e o que faz uma saliencia ser
        // lida como saliencia, e nao como um retangulo escuro no fundo.
        static readonly Color CorPedra = new Color(0.34f, 0.32f, 0.46f);
        static readonly Color CorPedraPlataforma = new Color(0.47f, 0.45f, 0.63f);
        static readonly Color CorTerra = new Color(0.11f, 0.10f, 0.16f);
        static readonly Color CorBorda = new Color(0.58f, 0.55f, 0.74f);
        static readonly Color CorBordaPlataforma = new Color(0.78f, 0.74f, 0.95f);
        static readonly Color CorAbismo = new Color(0.04f, 0.035f, 0.06f);

        public const float TopoChao = -0.4f;
        public const float EspessuraChao = 1.6f;

        /// <summary>Degrau maximo que o Ren vence com folga confortavel.</summary>
        public const float DegrauMaximo = 1.3f;
        /// <summary>Vao horizontal maximo no mesmo nivel, com folga.</summary>
        public const float VaoMaximo = 2.3f;

        /// <summary>
        /// Reconstroi o terreno da cena aberta.
        /// </summary>
        /// <param name="segmentos">Faixas de chao: x = inicio, y = fim.</param>
        /// <param name="plataformas">Plataformas: x = inicio, y = fim, z = altura do topo.</param>
        public static List<Vector2> Construir(Vector2[] segmentos, Vector3[] plataformas,
                                              float xMundoMin, float xMundoMax)
        {
            // Reaproveita sprites e material do terreno original para nao destoar.
            // Precisa achar tambem objetos DESATIVADOS: numa segunda execucao o
            // terreno antigo ja foi desligado por esta propria ferramenta.
            var chaoAntigo = AcharRaiz("Ground");
            var baseAntiga = AcharRaiz("GroundEarthBase");
            var bordaAntiga = AcharRaiz("GroundEdge");
            if (chaoAntigo == null || baseAntiga == null)
                throw new System.Exception("Cena sem Ground/GroundEarthBase - terreno original nao encontrado.");

            var srChao = chaoAntigo.GetComponent<SpriteRenderer>();
            Sprite spritePedra = srChao.sprite;
            Sprite spriteSolido = baseAntiga.GetComponent<SpriteRenderer>().sprite;
            Material matLit = srChao.sharedMaterial;

            chaoAntigo.SetActive(false);
            baseAntiga.SetActive(false);
            if (bordaAntiga != null) bordaAntiga.SetActive(false);

            var raizGO = AcharRaiz("Terrain") ?? new GameObject("Terrain");
            raizGO.transform.position = Vector3.zero;
            Transform raiz = raizGO.transform;

            // Remove pecas de uma construcao anterior para a operacao ser idempotente.
            var antigos = new List<GameObject>();
            foreach (Transform c in raiz) antigos.Add(c.gameObject);
            foreach (var go in antigos) Object.DestroyImmediate(go);

            // Fundo de abismo: faz os vaos lerem como fenda escura, nao como ceu.
            Bloco(raiz, "Abismo", xMundoMin - 1f, xMundoMax + 1f, TopoChao, 46f,
                  spriteSolido, CorAbismo, -6, false, matLit);

            for (int i = 0; i < segmentos.Length; i++)
            {
                string nome = "Chao_" + (char)('A' + i);
                Bloco(raiz, nome + "_Terra", segmentos[i].x, segmentos[i].y, TopoChao, 6f,
                      spriteSolido, CorTerra, -2, false, matLit);
                Bloco(raiz, nome, segmentos[i].x, segmentos[i].y, TopoChao, EspessuraChao,
                      spritePedra, CorPedra, 0, true, matLit);
                Bloco(raiz, nome + "_Borda", segmentos[i].x, segmentos[i].y, TopoChao, 0.16f,
                      spriteSolido, CorBorda, 1, false, matLit);
            }

            for (int i = 0; i < plataformas.Length; i++)
            {
                var p = plataformas[i];
                string nome = "Plat_" + (i + 1);
                // A plataforma e tratada como um PEDACO DO MESMO SOLO: mesma
                // alvenaria, mesma aresta clara no topo, so que mais espessa que
                // uma laje fina. Um contorno escuro maior faria a saliencia
                // parecer um quadro pendurado, e nao rocha.
                //
                // A espessura depende da altura: uma plataforma ALTA precisa ser
                // fina para deixar passagem livre por baixo (o jogador tem ~1.2u
                // e os inimigos ~1.3u de altura); uma plataforma BAIXA e um
                // degrau macico mesmo, feito para subir em cima.
                float espessura = EspessuraDaPlataforma(p.z);
                Bloco(raiz, nome, p.x, p.y, p.z, espessura, spritePedra, CorPedraPlataforma, 0, true, matLit);
                Bloco(raiz, nome + "_Borda", p.x, p.y, p.z, 0.16f, spriteSolido, CorBordaPlataforma, 1, false, matLit);
            }

            // Devolve os vaos (x = inicio, y = fim) para quem precisar desviar
            // decoracoes, inimigos e coletaveis de cima do vazio.
            var vaos = new List<Vector2>();
            for (int i = 0; i + 1 < segmentos.Length; i++)
                vaos.Add(new Vector2(segmentos[i].y, segmentos[i + 1].x));
            return vaos;
        }

        /// <summary>
        /// Procura um objeto raiz da cena aberta pelo nome, incluindo os que
        /// estao desativados (GameObject.Find ignora inativos).
        /// </summary>
        static GameObject AcharRaiz(string nome)
        {
            var cena = EditorSceneManager.GetActiveScene();
            foreach (var root in cena.GetRootGameObjects())
                if (root.name == nome) return root;
            return null;
        }

        static void Bloco(Transform raiz, string nome, float xMin, float xMax, float topo, float altura,
                          Sprite sprite, Color cor, int ordem, bool solido, Material mat)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(raiz, false);

            float w = xMax - xMin;
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, topo - altura * 0.5f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, altura);
            sr.color = cor;
            sr.sortingOrder = ordem;
            sr.sharedMaterial = mat;

            if (solido)
            {
                var bc = go.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(w, altura);
                bc.offset = Vector2.zero;
                go.layer = LayerMask.NameToLayer("Ground");
            }
        }

        /// <summary>
        /// Espessura de uma plataforma segundo a altura do seu topo. Acima de
        /// 1.8u ela fica fina o bastante para que jogador e inimigos passem por
        /// baixo; abaixo disso e um degrau macico (nao ha altura util para
        /// passagem de qualquer jeito, entao vale mais parecer rocha solida).
        /// </summary>
        public static float EspessuraDaPlataforma(float topo) => topo >= 1.8f ? 0.6f : 0.9f;

        /// <summary>Verdadeiro se x cai dentro de algum vao (com margem de seguranca).</summary>
        public static bool SobreVao(float x, List<Vector2> vaos, float margem = 0.5f)
        {
            foreach (var v in vaos)
                if (x > v.x - margem && x < v.y + margem) return true;
            return false;
        }

        /// <summary>Empurra x para fora do vao mais proximo, para o lado mais perto.</summary>
        public static float ForaDoVao(float x, List<Vector2> vaos, float folga = 1.2f)
        {
            foreach (var v in vaos)
            {
                if (x > v.x - 0.5f && x < v.y + 0.5f)
                    return (Mathf.Abs(x - v.x) < Mathf.Abs(x - v.y)) ? v.x - folga : v.y + folga;
            }
            return x;
        }

        /// <summary>
        /// Move decoracoes (filhas dos objetos "Decoration*") que ficaram
        /// suspensas sobre um vao de volta para o chao solido mais proximo.
        /// Nunca move o proprio container, so os filhos com renderizador.
        /// </summary>
        public static void AjustarDecoracoes(UnityEngine.SceneManagement.Scene cena, List<Vector2> vaos)
        {
            foreach (var root in cena.GetRootGameObjects())
            {
                if (!root.name.StartsWith("Decoration")) continue;

                bool ehContainer = root.GetComponent<SpriteRenderer>() == null;
                var alvos = new List<Transform>();
                if (ehContainer) { foreach (Transform c in root.transform) alvos.Add(c); }
                else alvos.Add(root.transform);

                foreach (var t in alvos)
                {
                    float x = t.position.x;
                    if (!SobreVao(x, vaos)) continue;
                    t.position = new Vector3(ForaDoVao(x, vaos), t.position.y, t.position.z);
                }
            }
        }

        /// <summary>Reposiciona um inimigo e os seus dois pontos de patrulha.</summary>
        public static void PosicionarInimigo(EcosDeAldenor.Enemies.EnemyBase inimigo, float x, float a, float b, float y = 0.5f)
        {
            if (inimigo == null) return;
            inimigo.transform.position = new Vector3(x, y, inimigo.transform.position.z);

            var so = new SerializedObject(inimigo);
            var pa = so.FindProperty("pointA").objectReferenceValue as Transform;
            var pb = so.FindProperty("pointB").objectReferenceValue as Transform;
            if (pa != null) pa.position = new Vector3(a, y, pa.position.z);
            if (pb != null) pb.position = new Vector3(b, y, pb.position.z);
        }

        /// <summary>Altura em que um coletavel deve flutuar sobre uma superficie.</summary>
        public static float AlturaDoColetavel(float topoDaSuperficie) => topoDaSuperficie + 1.4f;

        /// <summary>Altura da base de um checkpoint/altar sobre uma superficie.</summary>
        public static float AlturaDoAltar(float topoDaSuperficie) => topoDaSuperficie + 0.9f;
    }
}
