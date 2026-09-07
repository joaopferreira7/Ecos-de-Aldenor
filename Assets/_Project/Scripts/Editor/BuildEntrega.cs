using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;

namespace EcosDeAldenor.EditorTools
{
    /// <summary>
    /// Gera a build de entrega sempre do mesmo jeito.
    ///
    /// A build era feita a mao pelo Build Settings, e e justamente ai que um
    /// trabalho de faculdade costuma se perder: uma cena de fora da lista, a
    /// cena inicial trocada, um aviso ignorado. Como o pacote precisa ser
    /// gerado de novo a cada correcao ate a entrega (18/11/2026), vale mais um
    /// comando que confere as pre-condicoes antes de construir.
    ///
    /// Ha duas portas de entrada:
    ///  - o menu "Ecos de Aldenor/", para o dia a dia com o Editor aberto;
    ///  - <see cref="Verificar"/> e <see cref="GerarExecutavel"/>, chamados por
    ///    -executeMethod pelo montar.ps1. Esses encerram o Editor com codigo de
    ///    saida, senao o script nao tem como saber que a build falhou.
    ///
    /// A saida vai para "Build/" na raiz do projeto, que o .gitignore ja
    /// ignora - build nao se versiona.
    /// </summary>
    public static class BuildEntrega
    {
        const string Pasta = "Build";
        const string Executavel = "Ecos de Aldenor.exe";
        const int CenasEsperadas = 8;

        // ---------- menu ----------

        [MenuItem("Ecos de Aldenor/Verificar projeto")]
        public static void VerificarPeloMenu()
        {
            string relatorio;
            if (Conferir(out relatorio)) Debug.Log(relatorio);
            else Debug.LogError(relatorio);
        }

        [MenuItem("Ecos de Aldenor/Gerar build Windows")]
        public static void Gerar()
        {
            string relatorio;
            Construir(out relatorio);
        }

        // ---------- linha de comando (montar.ps1) ----------

        /// <summary>Confere as pre-condicoes sem construir nada.</summary>
        public static void Verificar()
        {
            string relatorio;
            bool ok = Conferir(out relatorio);
            if (ok) Debug.Log(relatorio); else Debug.LogError(relatorio);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Confere e gera o executavel.</summary>
        public static void GerarExecutavel()
        {
            string relatorio;
            bool ok = Construir(out relatorio);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        // ---------- implementacao ----------

        static bool Conferir(out string relatorio)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Verificacao do projeto ===");
            bool ok = true;

            // O verificador de fases troca a cena aberta quatro vezes. Se houver
            // alteracao pendente, o Unity abre a caixa "salvar antes de trocar?"
            // e trava tudo que nao tenha alguem na frente do Editor - o script de
            // build, o MCP. Salvar antes e a saida; o aviso deixa claro que a
            // cena foi gravada por conta disso.
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                {
                    sb.AppendLine("  aviso: " + EditorSceneManager.GetSceneAt(i).name +
                                  " tinha alteracoes pendentes e foi salva antes da verificacao.");
                    break;
                }
            EditorSceneManager.SaveOpenScenes();

            var cenas = EditorBuildSettings.scenes.Where(c => c.enabled).ToArray();

            // A primeira cena da lista e a que abre o jogo. Se nao for o menu, o
            // jogador cai direto numa fase e nao ha como voltar.
            if (cenas.Length == 0 || !cenas[0].path.EndsWith("MainMenu.unity"))
            {
                sb.AppendLine("ERRO: a primeira cena da lista precisa ser MainMenu.unity. Hoje e: " +
                              (cenas.Length > 0 ? cenas[0].path : "(lista vazia)"));
                ok = false;
            }

            if (cenas.Length != CenasEsperadas)
            {
                sb.AppendLine(string.Format("ERRO: o jogo tem {0} cenas, a lista traz {1}. Confira File > Build Settings.",
                                            CenasEsperadas, cenas.Length));
                ok = false;
            }

            foreach (var c in cenas)
                if (!File.Exists(c.path))
                {
                    sb.AppendLine("ERRO: cena listada no build nao existe em disco: " + c.path);
                    ok = false;
                }

            // Uma fase quebrada nao se ve na build; ve-se jogando, tarde demais.
            // O verificador mede a geometria real das fases (alcance de pulo,
            // vaos, atores dentro da rocha, patrulha valida).
            int problemas;
            string fases = LevelLayouts.Relatorio(out problemas);
            sb.AppendLine(fases);
            if (problemas > 0)
            {
                sb.AppendLine("ERRO: o verificador de fases achou " + problemas + " problema(s).");
                ok = false;
            }

            sb.AppendLine(ok ? "Verificacao OK." : "Verificacao FALHOU.");
            relatorio = sb.ToString();
            return ok;
        }

        static bool Construir(out string relatorio)
        {
            string conferencia;
            if (!Conferir(out conferencia))
            {
                relatorio = "Build cancelada.\n" + conferencia;
                Debug.LogError(relatorio);
                return false;
            }
            Debug.Log(conferencia);

            // A conferencia acima abre as fases uma a uma para medir a
            // geometria. Sem salvar aqui, o BuildPlayer para e pergunta se
            // queremos salvar as cenas - e um build automatizado nao tem quem
            // responda a caixa de dialogo.
            EditorSceneManager.SaveOpenScenes();

            var cenas = EditorBuildSettings.scenes.Where(c => c.enabled).ToArray();

            string raiz = Directory.GetParent(Application.dataPath).FullName;
            string destino = Path.Combine(raiz, Pasta);

            // Construir por cima de uma build antiga faz o Unity perguntar se
            // pode substituir os arquivos - de novo, uma caixa de dialogo que
            // trava qualquer build sem gente na frente. Alem disso, sobra de
            // build anterior vai junto no zip da entrega. A pasta e saida
            // descartavel (o .gitignore ignora "Build/"), entao some com ela.
            if (Directory.Exists(destino))
            {
                Directory.Delete(destino, true);
                Debug.Log("Build anterior removida de " + destino);
            }
            Directory.CreateDirectory(destino);

            var opcoes = new BuildPlayerOptions
            {
                scenes = cenas.Select(c => c.path).ToArray(),
                locationPathName = Path.Combine(destino, Executavel),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport relat = BuildPipeline.BuildPlayer(opcoes);
            var resumo = relat.summary;

            var sb = new StringBuilder();
            sb.AppendLine("Build " + resumo.result);
            sb.AppendLine("  saida: " + resumo.outputPath);
            sb.AppendLine(string.Format("  tamanho: {0:F1} MB", resumo.totalSize / (1024f * 1024f)));
            sb.AppendLine(string.Format("  tempo: {0:F0} s", resumo.totalTime.TotalSeconds));
            sb.AppendLine(string.Format("  erros: {0} | avisos: {1}", resumo.totalErrors, resumo.totalWarnings));
            sb.AppendLine("  cenas: " + string.Join(", ", cenas.Select(c => Path.GetFileNameWithoutExtension(c.path)).ToArray()));

            relatorio = sb.ToString();
            bool ok = resumo.result == BuildResult.Succeeded;
            if (ok) Debug.Log(relatorio); else Debug.LogError(relatorio);
            return ok;
        }
    }
}
