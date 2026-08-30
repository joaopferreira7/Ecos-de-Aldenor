using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Distribui as decoracoes ao longo do chao solido de forma
        /// DETERMINISTICA, em vez de empurrar as que caem num lugar ruim.
        ///
        /// Empurrar para a borda mais proxima (a abordagem anterior) juntava
        /// varios objetos exatamente na mesma coordenada, e nada impedia que uma
        /// lapide ficasse atravessada numa plataforma.
        ///
        /// Aqui o espaco disponivel e calculado de verdade: tira-se do chao o
        /// que nao pode receber decoracao (vaos, plataformas BAIXAS - que sao
        /// macicas -, a porta de cripta e a area de nascimento) e o que sobra e
        /// repartido respeitando a largura real de cada sprite. Plataformas
        /// ALTAS nao entram na conta: elas sao finas e ha passagem livre embaixo.
        ///
        /// O cenario foi decorado quando as fases eram uma faixa continua de
        /// 30u. Com o relevo segmentado nao ha espaco para todas as pecas sem
        /// que se encavalem, entao as que nao cabem sao DESATIVADAS (nao
        /// apagadas) e o metodo devolve quantas sobraram, para quem chamou poder
        /// registrar. Melhor um cemiterio mais esparso do que lapides fundidas.
        ///
        /// Correntes e luzes ficam fora dessa disputa: as correntes pendem bem
        /// acima da acao e as luzes precisam cobrir a fase inteira, inclusive
        /// sobre plataformas e abismos.
        /// </summary>
        public static int EspalharDecoracoes(UnityEngine.SceneManagement.Scene cena,
                                             Vector2[] segmentos, Vector3[] plataformas,
                                             float xPorta, float xNascimento,
                                             float xMundoMin, float xMundoMax)
        {
            var livres = new List<Vector2>(segmentos);
            foreach (var p in plataformas)
            {
                if (p.z >= 1.8f) continue;                 // alta e fina: passa-se por baixo
                livres = Subtrair(livres, p.x - 0.3f, p.y + 0.3f);
            }
            livres = Subtrair(livres, xPorta - 1.5f, xPorta + 1.5f);
            livres = Subtrair(livres, xNascimento - 1.2f, xNascimento + 1.2f);
            livres = livres.Where(i => i.y - i.x >= 1.0f).OrderBy(i => i.x).ToList();

            var chao = new List<SpriteRenderer>();
            var correntes = new List<Transform>();
            var luzes = new List<Transform>();

            foreach (var root in cena.GetRootGameObjects())
            {
                if (!root.name.StartsWith("Decoration")) continue;
                if (root.name == "Decoration_Door") continue;      // a porta e a entrada: fica onde esta

                var alvos = new List<Transform>();
                if (root.GetComponent<SpriteRenderer>() != null) alvos.Add(root.transform);
                foreach (Transform c in root.transform) alvos.Add(c);

                foreach (var t in alvos)
                {
                    if (t.name.StartsWith("Chain")) { correntes.Add(t); continue; }
                    if (t.GetComponent<SpriteRenderer>() != null) { chao.Add(t.GetComponent<SpriteRenderer>()); continue; }
                    if (t.GetComponent<UnityEngine.Rendering.Universal.Light2D>() != null) luzes.Add(t);
                }
            }

            // Reativa tudo antes de medir: uma execucao anterior pode ter
            // desligado pecas que agora cabem.
            foreach (var d in chao) d.gameObject.SetActive(true);

            // Um gerador com semente fixa por cena: o resultado varia de fase
            // para fase, mas e sempre o mesmo se a ferramenta rodar de novo.
            var rng = new System.Random(Semente(cena.name));

            // Embaralha a ORDEM das pecas antes de posicionar. Sem isso os
            // objetos saem agrupados por tipo (todos os cranios sao irmaos
            // consecutivos na hierarquia) e o cemiterio vira uma fileira.
            var pecas = chao.Select(Medir).ToList();

            for (int i = pecas.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = pecas[i]; pecas[i] = pecas[j]; pecas[j] = tmp;
            }

            int couberam = Distribuir(pecas, livres, 0.35f, rng);

            for (int i = couberam; i < pecas.Count; i++)
                pecas[i].t.gameObject.SetActive(false);

            // Correntes pendem acima de tudo: so precisam de espacamento proprio.
            EspacarNaLargura(correntes, xMundoMin + 2f, xMundoMax - 2f, 0f);
            // Luzes cobrem a fase inteira, meio passo desencontradas das correntes.
            EspacarNaLargura(luzes, xMundoMin + 1.5f, xMundoMax - 1.5f, 0.5f);

            return pecas.Count - couberam;
        }

        /// <summary>
        /// Preenche os intervalos livres da esquerda para a direita e depois
        /// redistribui, dentro de cada intervalo, a folga que sobrou - assim as
        /// pecas ficam espacadas por igual em vez de amontoadas num canto.
        /// Devolve quantas pecas couberam.
        /// </summary>
        /// <summary>
        /// Largura e desvio do pivo de uma peca, calculados a partir do sprite e
        /// da escala - NAO de Renderer.bounds.
        ///
        /// Renderer.bounds devolve valor defasado para um objeto que acabou de
        /// ser reativado no mesmo quadro (caso comum aqui, ja que a execucao
        /// anterior pode ter desligado pecas). Medir errado a largura fazia o
        /// distribuidor achar que cabia mais do que cabe, e as pecas voltavam a
        /// se encavalar. A geometria do sprite nao depende do estado do renderer.
        /// </summary>
        static Peca Medir(SpriteRenderer sr)
        {
            float escala = Mathf.Abs(sr.transform.lossyScale.x);
            float largura, desvio;

            if (sr.drawMode == SpriteDrawMode.Simple && sr.sprite != null)
            {
                largura = sr.sprite.bounds.size.x * escala;
                desvio = sr.sprite.bounds.center.x * escala;   // pivo fora do centro
            }
            else
            {
                largura = sr.size.x * escala;                  // tiled/sliced: centrado
                desvio = 0f;
            }

            return new Peca { t = sr.transform, largura = largura, desvioCentro = desvio };
        }

        /// <summary>Uma peca de cenario e o que se precisa saber para posiciona-la.</summary>
        struct Peca
        {
            public Transform t;
            public float largura;
            public float desvioCentro;
        }

        static int Distribuir(List<Peca> itens, List<Vector2> livres, float folga, System.Random rng)
        {
            if (itens.Count == 0 || livres.Count == 0) return 0;

            int k = 0;
            foreach (var iv in livres)
            {
                // Quantas cabem neste intervalo, e com que largura somada.
                int inicio = k;
                float usado = 0f;
                while (k < itens.Count)
                {
                    float extra = itens[k].largura + (k > inicio ? folga : 0f);
                    if (usado + extra > iv.y - iv.x) break;
                    usado += extra;
                    k++;
                }
                int quantas = k - inicio;
                if (quantas == 0) continue;

                // A sobra do intervalo e repartida entre os espacos com PESOS
                // SORTEADOS, nao em partes iguais. Espacamento uniforme fazia as
                // pecas parecerem enfileiradas numa grade; um cemiterio precisa
                // ser irregular. Como os pesos apenas repartem a mesma sobra, a
                // garantia de nao haver sobreposicao se mantem.
                float sobra = (iv.y - iv.x) - usado;
                var pesos = new float[quantas + 1];
                float soma = 0f;
                for (int i = 0; i < pesos.Length; i++)
                {
                    pesos[i] = 0.35f + (float)rng.NextDouble();
                    soma += pesos[i];
                }

                float cursor = iv.x + sobra * pesos[0] / soma;
                for (int i = inicio; i < k; i++)
                {
                    // 'cursor' e a borda esquerda desejada; o transform recua o
                    // desvio do pivo para o DESENHO cair no lugar calculado.
                    float centro = cursor + itens[i].largura * 0.5f;
                    var pos = itens[i].t.position;
                    itens[i].t.position = new Vector3(centro - itens[i].desvioCentro, pos.y, pos.z);
                    cursor += itens[i].largura + folga + sobra * pesos[i - inicio + 1] / soma;
                }
            }
            return k;
        }

        /// <summary>Hash estavel de string, para a semente nao mudar entre execucoes.</summary>
        static int Semente(string s)
        {
            int h = 17;
            foreach (char c in s) h = unchecked(h * 31 + c);
            return h;
        }

        /// <summary>Espaca objetos uniformemente numa faixa, sem checar colisao.</summary>
        static void EspacarNaLargura(List<Transform> itens, float xMin, float xMax, float desvio)
        {
            for (int i = 0; i < itens.Count; i++)
            {
                float t = (i + 0.5f + desvio) / Mathf.Max(1, itens.Count);
                float x = Mathf.Lerp(xMin, xMax, Mathf.Repeat(t, 1f));
                itens[i].position = new Vector3(x, itens[i].position.y, itens[i].position.z);
            }
        }

        /// <summary>Remove a faixa [a, b] de uma lista de intervalos.</summary>
        static List<Vector2> Subtrair(List<Vector2> intervalos, float a, float b)
        {
            var saida = new List<Vector2>();
            foreach (var iv in intervalos)
            {
                if (b <= iv.x || a >= iv.y) { saida.Add(iv); continue; }   // nao encosta
                if (a > iv.x) saida.Add(new Vector2(iv.x, a));
                if (b < iv.y) saida.Add(new Vector2(b, iv.y));
            }
            return saida;
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
