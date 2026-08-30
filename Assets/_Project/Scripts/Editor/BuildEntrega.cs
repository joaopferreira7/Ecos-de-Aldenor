using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
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
    /// A saida vai para "Build/" na raiz do projeto, que o .gitignore ja
    /// ignora - build nao se versiona.
    /// </summary>
    public static class BuildEntrega
    {
        const string Pasta = "Build";
        const string Executavel = "Ecos de Aldenor.exe";

        [MenuItem("Ecos de Aldenor/Gerar build Windows")]
        public static void Gerar()
        {
            var cenas = EditorBuildSettings.scenes.Where(c => c.enabled).ToArray();

            // A primeira cena da lista e a que abre o jogo. Se nao for o menu, o
            // jogador cai direto numa fase e nao ha como voltar.
            if (cenas.Length == 0 || !cenas[0].path.EndsWith("MainMenu.unity"))
            {
                Debug.LogError("Build cancelada: a primeira cena da lista precisa ser MainMenu.unity. " +
                               "Hoje e: " + (cenas.Length > 0 ? cenas[0].path : "(lista vazia)"));
                return;
            }

            if (cenas.Length != 8)
            {
                Debug.LogError(string.Format("Build cancelada: o jogo tem 8 cenas, a lista traz {0}. " +
                                             "Confira File > Build Settings.", cenas.Length));
                return;
            }

            // Uma fase quebrada nao se ve na build; ve-se jogando, tarde demais.
            int problemas;
            string relatorio = LevelLayouts.Relatorio(out problemas);
            if (problemas > 0)
            {
                Debug.LogError("Build cancelada: o verificador de fases achou problema.\n" + relatorio);
                return;
            }

            string raiz = Directory.GetParent(Application.dataPath).FullName;
            string destino = Path.Combine(raiz, Pasta);
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

            if (resumo.result == BuildResult.Succeeded) Debug.Log(sb.ToString());
            else Debug.LogError(sb.ToString());
        }
    }
}
