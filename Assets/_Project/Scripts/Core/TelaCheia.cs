using UnityEngine;

namespace EcosDeAldenor.Core
{
    /// <summary>
    /// Abre o jogo sempre em tela cheia, na resolucao do monitor.
    ///
    /// O Player Settings ja pede tela cheia, mas o Unity grava no registro do
    /// Windows o modo da ultima sessao (HKCU\Software\DefaultCompany\Ecos de
    /// Aldenor) e esse valor passa por cima da configuracao: bastava um
    /// Alt+Enter, ou uma build antiga em janela, para o .exe abrir numa janela
    /// pequena dali em diante. Aqui o modo e imposto a cada abertura.
    ///
    /// Alt+Enter continua alternando durante a partida; so a abertura e fixa.
    /// </summary>
    public static class TelaCheia
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Impor()
        {
            var nativa = Screen.currentResolution;
            Screen.SetResolution(nativa.width, nativa.height, FullScreenMode.FullScreenWindow, nativa.refreshRateRatio);
        }
#endif
    }
}
