using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EcosDeAldenor.Systems;
using EcosDeAldenor.Enemies;
using EcosDeAldenor.Player;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// O RELEVO DAS FASES, em numeros.
    ///
    /// O <see cref="LevelTerrainBuilder"/> sabe MONTAR um relevo, mas nao sabia
    /// QUAL: as medidas de cada fase so existiam dentro dos arquivos .unity, que
    /// ninguem le nem revisa. Na pratica o "level design por dados" so valia
    /// enquanto durasse a sessao que rodou a ferramenta - reconstruir uma fase
    /// depois disso exigia adivinhar os numeros de volta a partir da cena.
    ///
    /// Aqui eles ficam versionados, num lugar so, ao lado das regras que os
    /// justificam. Mudar uma fase e mudar uma linha desta tabela e rodar
    /// "Ecos de Aldenor/Reconstruir fases".
    /// </summary>
    public static class LevelLayouts
    {
        /// <summary>Total de almas espalhadas pelo jogo. Precisa bater com
        /// GameManager.fragmentsRequiredForFinalPhase e com o "N / 6" do HUD.</summary>
        public const int AlmasNoJogo = 6;

        public struct Fase
        {
            public string cena;
            /// <summary>Faixas de chao: x = inicio, y = fim. O espaco entre duas vira abismo.</summary>
            public Vector2[] segmentos;
            /// <summary>Plataformas: x = inicio, y = fim, z = altura do topo.</summary>
            public Vector3[] plataformas;
            /// <summary>Patrulhas, na ordem em que os inimigos aparecem da esquerda
            /// para a direita: x = posicao, y = ponto A, z = ponto B.</summary>
            public Vector3[] patrulhas;
            public float xMundoMin, xMundoMax;
        }

        /// <summary>
        /// RITMO DOS ABISMOS. O alcance do Ren e 2,97 u no mesmo nivel, e por
        /// muito tempo TODOS os vaos do jogo mediam exatamente 2,30 u - o maximo
        /// da regra. O jogo inteiro pedia o mesmo pulo, sempre no limite: sem
        /// progressao, e sem nenhum salto que o jogador pudesse dar com folga
        /// para ganhar confianca antes do proximo.
        ///
        /// Agora os vaos crescem ao longo do jogo (1,60 -> 2,30 na Phase1;
        /// 1,40 -> 1,90 -> 2,30 na Phase2), e o vao maximo aparece uma vez por
        /// fase, como climax. O ultimo abismo continua sendo o mais dificil, mas
        /// agora chega depois de dois mais faceis que ensinam a medida do pulo.
        /// </summary>
        public static readonly Fase[] Fases =
        {
            new Fase
            {
                cena = "Tutorial",
                // Espaco seguro: chao inteiro, sem abismo. A alma exige subir
                // dois degraus, entao a licao de pulo e praticada, nao lida.
                segmentos = new[] { new Vector2(-15f, 15f) },
                plataformas = new[]
                {
                    new Vector3(-6.8f, -5.2f, 0.9f),
                    new Vector3(-3.4f, -1.8f, 0.9f),
                    new Vector3(-0.6f,  1.0f, 2.0f),
                    new Vector3( 3.4f,  5.0f, 0.9f),
                },
                patrulhas = new Vector3[0],
                xMundoMin = -15f, xMundoMax = 15f,
            },
            new Fase
            {
                cena = "Phase1",
                // A: sala de combate + primeira subida | B: trecho de plataforma
                // com uma saliencia alta | C: reta final guardada.
                segmentos = new[]
                {
                    new Vector2(-15.0f, -1.6f),
                    new Vector2(  0.0f,  7.4f),   // vao de 1,60 u: o primeiro do jogo
                    new Vector2(  9.7f, 15.0f),   // vao de 2,30 u: o climax da fase
                },
                plataformas = new[]
                {
                    new Vector3(-8.0f, -6.4f, 0.9f),
                    new Vector3( 1.6f,  3.0f, 0.9f),
                    new Vector3( 4.2f,  5.8f, 2.2f),
                    new Vector3(12.2f, 13.6f, 0.9f),
                },
                // A patrulha da sala de combate era de 1,6 u - o inimigo mal
                // saia do lugar e a "sala" nao se lia como sala. Vai ate onde a
                // rocha da Plat_1 e o altar permitem.
                patrulhas = new[]
                {
                    new Vector3(-5.00f, -5.60f, -3.60f),
                    new Vector3(10.80f,  9.95f, 11.60f),
                },
                xMundoMin = -15f, xMundoMax = 15f,
            },
            new Fase
            {
                cena = "Phase2",
                // Alterna corredor de aproximacao -> combate -> plataforma ->
                // escalada final, com os abismos crescendo a cada troca.
                segmentos = new[]
                {
                    new Vector2(-15.0f, -7.8f),
                    new Vector2( -6.4f, -0.6f),   // vao de 1,40 u
                    new Vector2(  1.3f,  5.6f),   // vao de 1,90 u
                    new Vector2(  7.9f, 15.0f),   // vao de 2,30 u
                },
                plataformas = new[]
                {
                    new Vector3( 1.8f,  2.9f, 0.9f),
                    new Vector3( 4.2f,  5.6f, 2.2f),
                    // A escalada final foi 0,3 u para a direita para abrir
                    // corredor ao guardiao (ver patrulhas abaixo). A distancia
                    // entre as duas continua a mesma, entao a subida nao mudou.
                    new Vector3(11.2f, 12.3f, 0.9f),
                    new Vector3(12.7f, 13.8f, 2.2f),
                },
                // O guardiao da direita patrulhava 0,4 u: com a perseguicao agora
                // limitada a propria faixa, ele tinha virado estatua. Anda o
                // corredor entre o altar e a rocha da escalada final.
                patrulhas = new[]
                {
                    new Vector3(-2.20f, -3.30f, -1.30f),
                    new Vector3( 4.30f,  3.50f,  5.20f),
                    new Vector3( 9.95f,  9.30f, 10.65f),
                },
                xMundoMin = -15f, xMundoMax = 15f,
            },
            new Fase
            {
                cena = "FinalPhase",
                // Arena limpa: qualquer degrau macico travaria a perseguicao do
                // chefe, e um abismo tornaria a luta uma questao de empurrao.
                segmentos = new[] { new Vector2(-15f, 15f) },
                plataformas = new Vector3[0],
                patrulhas = new Vector3[0],
                xMundoMin = -15f, xMundoMax = 15f,
            },
        };

        [MenuItem("Ecos de Aldenor/Reconstruir fases")]
        public static void ReconstruirTudo()
        {
            var sb = new StringBuilder("Reconstrucao das fases:\n");
            foreach (var f in Fases)
            {
                var cena = EditorSceneManager.OpenScene(CaminhoDaCena(f.cena), OpenSceneMode.Single);

                LevelTerrainBuilder.Construir(f.segmentos, f.plataformas, f.xMundoMin, f.xMundoMax);

                // Porta e nascimento saem da propria cena: sao ancoras de arte e
                // de fluxo, nao medidas de relevo, e ficam onde o artista pos.
                int sobraram = LevelTerrainBuilder.EspalharDecoracoes(
                    cena, f.segmentos, f.plataformas,
                    XdaPorta(cena), XdoNascimento(cena), f.xMundoMin, f.xMundoMax);

                var inimigos = Object.FindObjectsByType<EnemyBase>()
                                     .OrderBy(e => e.transform.position.x).ToList();
                for (int i = 0; i < f.patrulhas.Length && i < inimigos.Count; i++)
                {
                    var p = f.patrulhas[i];
                    LevelTerrainBuilder.PosicionarInimigo(inimigos[i], p.x, p.y, p.z);
                }

                int pecasDeFundo = CompletarParallax(cena);

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                sb.AppendLine(string.Format("  {0}: {1} segmentos, {2} plataformas, {3} decoracoes sem lugar, {4} pecas de fundo acrescentadas",
                    f.cena, f.segmentos.Length, f.plataformas.Length, sobraram, pecasDeFundo));
            }
            Debug.Log(sb.ToString());
        }

        // ------------------------------------------------------------------
        // Parallax
        // ------------------------------------------------------------------

        /// <summary>
        /// Faixa de mundo que a camera chega a mostrar, e o quanto ela anda.
        /// Sai dos limites do proprio seguidor de camera, nao de um numero
        /// escrito na mao: mexer no enquadramento nao pode furar o fundo em
        /// silencio.
        /// </summary>
        static void AlcanceDaCamera(Scene cena, out float camMin, out float camMax, out float meiaTela)
        {
            camMin = -5.93f; camMax = 5.93f; meiaTela = 8.89f;

            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            // 16:9 e o formato da build; um formato mais largo mostraria mais
            // mundo de cada lado e pediria mais fundo.
            meiaTela = cam.orthographicSize * (16f / 9f);

            var seguidor = cam.GetComponent("CameraFollow2D");
            if (seguidor == null) return;

            var so = new SerializedObject(seguidor);
            var usa = so.FindProperty("useBounds");
            if (usa != null && !usa.boolValue) return;
            var min = so.FindProperty("minX");
            var max = so.FindProperty("maxX");
            if (min != null) camMin = min.floatValue;
            if (max != null) camMax = max.floatValue;
        }

        /// <summary>
        /// Acrescenta peca de fundo nas pontas ate a camada cobrir tudo o que a
        /// camera mostra, da entrada ao fim da fase.
        ///
        /// O ceu era UMA peca de 22,29 u para uma fase que a camera percorre em
        /// 29,64 u, e as montanhas eram tres de 5,90 u: nas duas pontas de cada
        /// fase de cemiterio sobrava um rasgo preto no alto da tela - bem onde o
        /// jogador nasce, alias. O cemiterio (fator 0,70) ja tinha tres pecas e
        /// nunca falhou, que e justamente a solucao aplicada aqui as demais.
        ///
        /// Quanto MENOR o fator de parallax, mais a camada fica para tras da
        /// camera e mais mundo ela precisa cobrir - por isso o ceu, o mais
        /// distante de todos, e o que mais sofria.
        ///
        /// A conta e refeita a cada execucao a partir da cobertura atual, entao
        /// rodar de novo nao acrescenta peca nenhuma.
        /// </summary>
        static int CompletarParallax(Scene cena)
        {
            float camMin, camMax, meiaTela;
            AlcanceDaCamera(cena, out camMin, out camMax, out meiaTela);
            float precisaEsq = camMin - meiaTela;
            float precisaDir = camMax + meiaTela;
            float percurso = camMax - camMin;

            // Agrupa as pecas de uma mesma camada: mesmo sprite, mesmo fator,
            // mesma altura. Sao as copias lado a lado que formam a faixa.
            var camadas = Object.FindObjectsByType<ParallaxLayer>()
                .Where(p => p.GetComponent<SpriteRenderer>() != null
                         && p.GetComponent<SpriteRenderer>().sprite != null)
                .GroupBy(p => new
                {
                    sprite = p.GetComponent<SpriteRenderer>().sprite.name,
                    fator = new SerializedObject(p).FindProperty("parallaxFactor").floatValue,
                    altura = Mathf.Round(p.transform.position.y * 100f),
                });

            int criadas = 0;
            foreach (var camada in camadas)
            {
                var pecas = camada.OrderBy(p => p.transform.position.x).ToList();
                float largura = pecas[0].GetComponent<SpriteRenderer>().bounds.size.x;
                if (largura <= 0.01f) continue;
                float meia = largura * 0.5f;
                float fator = camada.Key.fator;

                // A ponta esquerda e vista no comeco da fase, quando a camada
                // ainda nao se deslocou; a direita, no fim, ja deslocada.
                var maisEsquerda = pecas.First();
                while (maisEsquerda.transform.position.x - meia > precisaEsq + 0.01f)
                {
                    maisEsquerda = Clonar(maisEsquerda, maisEsquerda.transform.position.x - largura);
                    criadas++;
                }

                var maisDireita = pecas.Last();
                while (maisDireita.transform.position.x + meia + percurso * fator < precisaDir - 0.01f)
                {
                    maisDireita = Clonar(maisDireita, maisDireita.transform.position.x + largura);
                    criadas++;
                }
            }
            return criadas;
        }

        /// <summary>Copia uma peca de fundo para o lado, mantendo tudo o mais.</summary>
        static ParallaxLayer Clonar(ParallaxLayer modelo, float x)
        {
            var copia = Object.Instantiate(modelo.gameObject, modelo.transform.parent);
            copia.transform.position = new Vector3(x, modelo.transform.position.y, modelo.transform.position.z);
            copia.name = ProximoNome(modelo.name, x < modelo.transform.position.x ? -1 : 1);
            Undo.RegisterCreatedObjectUndo(copia, "Completar parallax");
            return copia.GetComponent<ParallaxLayer>();
        }

        /// <summary>
        /// Segue a nomenclatura ja usada no cenario ("graveyard_0_-1",
        /// "mountains_0_1"): mesmo prefixo, indice vizinho.
        /// </summary>
        static string ProximoNome(string nome, int passo)
        {
            int corte = nome.LastIndexOf('_');
            int indice;
            if (corte > 0 && int.TryParse(nome.Substring(corte + 1), out indice))
                return nome.Substring(0, corte + 1) + (indice + passo);
            return nome + (passo < 0 ? "_esq" : "_dir");
        }

        // ------------------------------------------------------------------
        // Verificador
        // ------------------------------------------------------------------

        struct Faixa { public string nome; public float a, b, topo; }

        /// <summary>
        /// Confere, sem abrir o jogo, que o relevo continua jogavel: que nenhum
        /// vao passa do alcance, que toda plataforma e alcancavel, que nenhum
        /// corpo nasce dentro de rocha ou sobre o vazio, que as patrulhas nao
        /// cruzam abismo nem entram na rocha, que toda alma esta ao alcance de
        /// quem pisa na superficie sob ela, e que o cenario nao se encavala nem
        /// cobre um altar.
        ///
        /// E a rede de seguranca do level design por dados: mudar um numero da
        /// tabela acima e rodar isto custa segundos, contra minutos de jogo a
        /// mao para descobrir que uma alma ficou ilhada.
        /// </summary>
        [MenuItem("Ecos de Aldenor/Verificar fases")]
        public static void VerificarTudo()
        {
            int problemas;
            string relatorio = Relatorio(out problemas);
            if (problemas == 0) Debug.Log(relatorio);
            else Debug.LogWarning(relatorio);
        }

        /// <summary>
        /// Roda a verificacao e DEVOLVE o relatorio, em vez de so escreve-lo no
        /// console. Assim da para chamar a checagem de fora do editor (de um
        /// script de entrega, por exemplo) e ler o resultado.
        /// </summary>
        public static string Relatorio(out int problemasEncontrados)
        {
            var sb = new StringBuilder("Verificacao das fases:\n");
            int problemas = 0, almas = 0;

            foreach (var f in Fases)
            {
                var cena = EditorSceneManager.OpenScene(CaminhoDaCena(f.cena), OpenSceneMode.Single);
                problemas += Verificar(cena, sb, ref almas);
            }

            if (almas != AlmasNoJogo)
            {
                sb.AppendLine(string.Format("  [ALMAS] o jogo tem {0} almas espalhadas, mas a meta e {1}: o HUD mostraria \"{0} / {1}\"", almas, AlmasNoJogo));
                problemas++;
            }

            sb.AppendLine(problemas == 0
                ? "OK: nenhum problema encontrado."
                : string.Format("{0} problema(s) encontrado(s).", problemas));

            problemasEncontrados = problemas;
            return sb.ToString();
        }

        static int Verificar(Scene cena, StringBuilder sb, ref int almas)
        {
            sb.AppendLine("  === " + cena.name + " ===");
            int probs = 0;

            var chao = new List<Faixa>();
            var plats = new List<Faixa>();
            foreach (var root in cena.GetRootGameObjects())
            {
                if (root.name != "Terrain") continue;
                foreach (Transform c in root.transform)
                {
                    var sr = c.GetComponent<SpriteRenderer>();
                    if (sr == null || c.GetComponent<BoxCollider2D>() == null) continue;
                    var faixa = new Faixa
                    {
                        nome = c.name,
                        a = c.position.x - sr.size.x * 0.5f,
                        b = c.position.x + sr.size.x * 0.5f,
                        topo = c.position.y + sr.size.y * 0.5f,
                    };
                    if (c.name.StartsWith("Chao_")) chao.Add(faixa);
                    else if (c.name.StartsWith("Plat_")) plats.Add(faixa);
                }
            }
            chao = chao.OrderBy(f => f.a).ToList();
            plats = plats.OrderBy(f => f.a).ToList();

            var vaos = new List<Vector2>();
            for (int i = 0; i + 1 < chao.Count; i++)
            {
                float larg = chao[i + 1].a - chao[i].b;
                vaos.Add(new Vector2(chao[i].b, chao[i + 1].a));
                if (larg > LevelTerrainBuilder.VaoMaximo + 0.01f)
                {
                    sb.AppendLine(string.Format("    [VAO] abismo de {0:F2} u em x={1:F2} passa do alcance ({2:F2} u)",
                        larg, chao[i].b, LevelTerrainBuilder.VaoMaximo));
                    probs++;
                }
            }

            // Toda plataforma precisa de um apoio a no maximo um degrau abaixo,
            // e perto o bastante para o pulo cobrir a subida.
            foreach (var p in plats)
            {
                float melhorDegrau = float.MaxValue;
                foreach (var s in chao)
                    if (p.a - 2.3f < s.b && p.b + 2.3f > s.a) melhorDegrau = Mathf.Min(melhorDegrau, p.topo - s.topo);
                foreach (var o in plats)
                {
                    if (o.nome == p.nome || o.topo >= p.topo) continue;
                    if (p.a - 2.3f < o.b && p.b + 2.3f > o.a) melhorDegrau = Mathf.Min(melhorDegrau, p.topo - o.topo);
                }
                if (melhorDegrau > LevelTerrainBuilder.DegrauMaximo + 0.01f)
                {
                    sb.AppendLine(string.Format("    [DEGRAU] {0} (topo {1:F2}) so tem apoio {2:F2} u abaixo - o pulo sobe {3:F2} u",
                        p.nome, p.topo, melhorDegrau, LevelTerrainBuilder.DegrauMaximo));
                    probs++;
                }
            }

            // Cenario: nada encavalado, nada sobre o vazio, nada em cima de altar.
            var decos = new List<Faixa>();
            foreach (var root in cena.GetRootGameObjects())
            {
                if (!root.name.StartsWith("Decoration") || root.name == "Decoration_Door") continue;
                var alvos = new List<Transform> { root.transform };
                foreach (Transform c in root.transform) alvos.Add(c);
                foreach (var t in alvos)
                {
                    if (t.name.StartsWith("Chain") || !t.gameObject.activeInHierarchy) continue;
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    // Silhuetas de fundo (ordem negativa) ficam ATRAS do terreno e
                    // nao tem colisor: elas se sobrepoem umas as outras e passam
                    // por tras de lapides e altares de proposito - e assim que se
                    // desenha profundidade. As regras abaixo existem para as pecas
                    // de chao, que dividem uma faixa de terreno e precisam ser
                    // lidas lado a lado; aplicar as mesmas regras a uma arvore
                    // pintada no fundo so gera alarme falso.
                    if (sr.sortingOrder < 0) continue;
                    float esc = Mathf.Abs(t.lossyScale.x);
                    float larg = (sr.drawMode == SpriteDrawMode.Simple && sr.sprite != null)
                        ? sr.sprite.bounds.size.x * esc : sr.size.x * esc;
                    float desvio = (sr.drawMode == SpriteDrawMode.Simple && sr.sprite != null)
                        ? sr.sprite.bounds.center.x * esc : 0f;
                    float cx = t.position.x + desvio;
                    decos.Add(new Faixa { nome = t.name, a = cx - larg * 0.5f, b = cx + larg * 0.5f, topo = t.position.y });
                }
            }
            decos = decos.OrderBy(d => d.a).ToList();
            for (int i = 0; i + 1 < decos.Count; i++)
            {
                float s = decos[i].b - decos[i + 1].a;
                if (s > 0.05f)
                {
                    sb.AppendLine(string.Format("    [CENARIO] {0} e {1} se encavalam em {2:F2} u", decos[i].nome, decos[i + 1].nome, s));
                    probs++;
                }
            }
            foreach (var d in decos)
            {
                float cx = (d.a + d.b) * 0.5f;
                foreach (var v in vaos)
                    if (cx > v.x && cx < v.y)
                    {
                        sb.AppendLine(string.Format("    [CENARIO] {0} flutua sobre o abismo em x={1:F2}", d.nome, cx));
                        probs++;
                    }
                foreach (var p in plats)
                    if (p.topo < 1.8f && d.b > p.a && d.a < p.b)
                    {
                        sb.AppendLine(string.Format("    [CENARIO] {0} atravessa a rocha da {1}", d.nome, p.nome));
                        probs++;
                    }
            }

            foreach (var altar in Object.FindObjectsByType<Checkpoint>())
            {
                float x = altar.transform.position.x;
                probs += SobreOVazio(x, chao, sb, "[ALTAR] altar sobre o vazio");
                foreach (var d in decos)
                    if (d.b > x - 0.6f && d.a < x + 0.6f)
                    {
                        sb.AppendLine(string.Format("    [ALTAR] {0} cobre o altar em x={1:F2}", d.nome, x));
                        probs++;
                    }
            }

            // Almas: cada uma precisa estar ao alcance de quem pisa na superficie
            // logo abaixo dela.
            foreach (var alma in Object.FindObjectsByType<Fragment>())
            {
                almas++;
                float x = alma.transform.position.x, y = alma.transform.position.y;
                float superficie = float.MinValue;
                foreach (var s in chao) if (x >= s.a && x <= s.b) superficie = Mathf.Max(superficie, s.topo);
                foreach (var p in plats) if (x >= p.a && x <= p.b) superficie = Mathf.Max(superficie, p.topo);
                if (superficie == float.MinValue)
                {
                    sb.AppendLine(string.Format("    [ALMA] alma em x={0:F2} nao tem chao nem plataforma embaixo", x));
                    probs++;
                }
                else if (y - superficie > 1.73f)
                {
                    sb.AppendLine(string.Format("    [ALMA] alma em x={0:F2} esta {1:F2} u acima da superficie - o pulo sobe 1,73 u", x, y - superficie));
                    probs++;
                }
            }

            // Patrulhas: nem cruzar abismo, nem esbarrar na rocha de uma
            // plataforma baixa (o inimigo fica preso contra a pedra).
            foreach (var inimigo in Object.FindObjectsByType<EnemyBase>())
            {
                var so = new SerializedObject(inimigo);
                var pa = so.FindProperty("pointA").objectReferenceValue as Transform;
                var pb = so.FindProperty("pointB").objectReferenceValue as Transform;
                if (pa == null || pb == null) continue;      // o chefe nao patrulha

                float a = Mathf.Min(pa.position.x, pb.position.x);
                float b = Mathf.Max(pa.position.x, pb.position.x);

                if (b - a < 1.0f)
                {
                    sb.AppendLine(string.Format("    [PATRULHA] {0} anda so {1:F2} u - parece uma estatua", inimigo.name, b - a));
                    probs++;
                }
                foreach (var v in vaos)
                    if (b > v.x && a < v.y)
                    {
                        sb.AppendLine(string.Format("    [PATRULHA] {0} cruza o abismo em x={1:F2}", inimigo.name, v.x));
                        probs++;
                    }
                foreach (var p in plats)
                    if (p.topo < 1.8f && b > p.a - 0.5f && a < p.b + 0.5f)
                    {
                        sb.AppendLine(string.Format("    [PATRULHA] {0} esbarra na rocha da {1}", inimigo.name, p.nome));
                        probs++;
                    }
            }

            // Nascimento: chao solido e longe de inimigo.
            var jogador = Object.FindAnyObjectByType<PlayerController>();
            if (jogador != null)
            {
                float x = jogador.transform.position.x;
                probs += SobreOVazio(x, chao, sb, "[NASCIMENTO] o jogador nasce sobre o vazio");
                foreach (var p in plats)
                    if (x > p.a - 0.4f && x < p.b + 0.4f)
                    {
                        sb.AppendLine(string.Format("    [NASCIMENTO] o jogador nasce dentro da rocha da {0}", p.nome));
                        probs++;
                    }
                foreach (var inimigo in Object.FindObjectsByType<EnemyBase>())
                    if (Mathf.Abs(inimigo.transform.position.x - x) < 3f)
                    {
                        sb.AppendLine(string.Format("    [NASCIMENTO] {0} nasce a {1:F2} u do jogador", inimigo.name, Mathf.Abs(inimigo.transform.position.x - x)));
                        probs++;
                    }
            }

            // A arena do chefe nao pode ter porta de saida. A vitoria ali e
            // responsabilidade do BossDeathTrigger, que espera o chefe cair; uma
            // PhaseExit no mesmo cenario e um atalho que entrega a tela de
            // Vitoria a quem simplesmente correr para a direita, sem lutar.
            var chefe = Object.FindAnyObjectByType<AVigilia>();
            if (chefe != null)
                foreach (var porta in Object.FindObjectsByType<PhaseExit>())
                {
                    sb.AppendLine(string.Format("    [ARENA] a fase do chefe tem uma PhaseExit em x={0:F2}: da para vencer sem matar o chefe",
                        porta.transform.position.x));
                    probs++;
                }

            // Fundo: nenhuma camada pode acabar antes do que a camera mostra,
            // ou aparece um rasgo preto no alto da tela na ponta da fase.
            float camMin, camMax, meiaTela;
            AlcanceDaCamera(cena, out camMin, out camMax, out meiaTela);
            float percurso = camMax - camMin;

            var grupos = Object.FindObjectsByType<ParallaxLayer>()
                .Where(p => p.GetComponent<SpriteRenderer>() != null
                         && p.GetComponent<SpriteRenderer>().sprite != null)
                .GroupBy(p => new
                {
                    sprite = p.GetComponent<SpriteRenderer>().sprite.name,
                    fator = new SerializedObject(p).FindProperty("parallaxFactor").floatValue,
                    altura = Mathf.Round(p.transform.position.y * 100f),
                });

            foreach (var grupo in grupos)
            {
                var pecas = grupo.OrderBy(p => p.transform.position.x).ToList();
                float meia = pecas[0].GetComponent<SpriteRenderer>().bounds.size.x * 0.5f;
                float esq = pecas.First().transform.position.x - meia;
                float dir = pecas.Last().transform.position.x + meia + percurso * grupo.Key.fator;

                if (esq > camMin - meiaTela + 0.01f)
                {
                    sb.AppendLine(string.Format("    [FUNDO] a camada {0} deixa {1:F2} u sem cobertura na entrada da fase",
                        grupo.Key.sprite, esq - (camMin - meiaTela)));
                    probs++;
                }
                if (dir < camMax + meiaTela - 0.01f)
                {
                    sb.AppendLine(string.Format("    [FUNDO] a camada {0} deixa {1:F2} u sem cobertura no fim da fase",
                        grupo.Key.sprite, (camMax + meiaTela) - dir));
                    probs++;
                }
            }

            if (probs == 0) sb.AppendLine("    ok");
            return probs;
        }

        static int SobreOVazio(float x, List<Faixa> chao, StringBuilder sb, string rotulo)
        {
            foreach (var s in chao) if (x >= s.a && x <= s.b) return 0;
            sb.AppendLine(string.Format("    {0} (x={1:F2})", rotulo, x));
            return 1;
        }

        static string CaminhoDaCena(string nome) => "Assets/_Project/Scenes/" + nome + ".unity";

        static float XdaPorta(Scene cena)
        {
            foreach (var root in cena.GetRootGameObjects())
                if (root.name == "Decoration_Door") return root.transform.position.x;
            var saida = Object.FindAnyObjectByType<PhaseExit>();
            return saida != null ? saida.transform.position.x : 14f;
        }

        static float XdoNascimento(Scene cena)
        {
            var jogador = Object.FindAnyObjectByType<PlayerController>();
            return jogador != null ? jogador.transform.position.x : -10f;
        }
    }
}
