using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Escolha de dificuldade no Menu Principal: uma linha com Fácil, Médio e
    /// Difícil abaixo dos botoes existentes, mostrando qual esta selecionada e o
    /// que ela muda.
    ///
    /// A linha e montada em runtime dentro do Canvas que ja existe na cena, no
    /// mesmo estilo dos outros botoes (UIFactory). O valor escolhido nao e
    /// guardado aqui: vai para DifficultySettings, que grava em PlayerPrefs e e
    /// consultado pelo jogador, pelos inimigos e pelo chefe quando cada um
    /// acorda. Assim, trocar de dificuldade so exige clicar antes de "Jogar".
    /// </summary>
    public class DifficultySelector : MonoBehaviour
    {
        [Tooltip("Altura da linha de dificuldade dentro do Canvas (abaixo dos botoes do menu).")]
        [SerializeField] private float alturaNoMenu = -238f;

        private readonly Dificuldade[] opcoes =
        {
            Dificuldade.Facil, Dificuldade.Medio, Dificuldade.Dificil
        };

        private readonly Button[] botoes = new Button[3];
        private Text descricao;

        private void Start()
        {
            Canvas canvas = CanvasDoMenu();
            if (canvas == null)
            {
                Debug.LogWarning("DifficultySelector: nenhum Canvas na cena; a escolha de dificuldade nao sera exibida.");
                return;
            }

            Construir(canvas.transform);
            Realcar();
        }


        /// <summary>
        /// Procura o Canvas do menu, e nao qualquer Canvas: o SceneController
        /// mantem um Canvas de fade persistente (DontDestroyOnLoad) com
        /// CanvasGroup em alpha 0. Cair nele deixaria os botoes invisiveis.
        /// Por isso a busca so aceita Canvas desta cena, e prefere o de menor
        /// ordem de desenho - o do proprio menu.
        /// </summary>
        private Canvas CanvasDoMenu()
        {
            Canvas escolhido = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.gameObject.scene != gameObject.scene) continue;
                if (escolhido == null || c.sortingOrder < escolhido.sortingOrder) escolhido = c;
            }
            return escolhido;
        }

        private void Construir(Transform pai)
        {
            var grupo = UIFactory.Caixa(pai, "SeletorDeDificuldade", new Vector2(900f, 150f),
                                        new Vector2(0f, alturaNoMenu));

            UIFactory.Texto(grupo, "Rotulo", "DIFICULDADE", 20, UIFactory.CorTextoFraco,
                            TextAnchor.MiddleCenter, new Vector2(900f, 26f), new Vector2(0f, 52f),
                            FontStyle.Bold);

            float[] x = { -210f, 0f, 210f };
            for (int i = 0; i < opcoes.Length; i++)
            {
                Dificuldade d = opcoes[i];
                botoes[i] = UIFactory.Botao(grupo, "Dif_" + d, DifficultySettings.NomeDe(d), 24,
                                            new Vector2(190f, 52f), new Vector2(x[i], 6f),
                                            () => Escolher(d));
            }

            descricao = UIFactory.Texto(grupo, "Descricao", "", 18, UIFactory.CorTextoFraco,
                                        TextAnchor.MiddleCenter, new Vector2(900f, 40f),
                                        new Vector2(0f, -42f));
        }

        private void Escolher(Dificuldade d)
        {
            DifficultySettings.Atual = d;
            Realcar();
        }

        /// <summary>
        /// Marca a opcao ativa. Como os botoes do projeto nao usam sprite, o
        /// destaque e feito na propria cor normal do botao - o Unity a aplica
        /// sozinho ao sair do hover, entao a selecao continua visivel depois que
        /// o mouse sai de cima.
        /// </summary>
        private void Realcar()
        {
            for (int i = 0; i < botoes.Length; i++)
            {
                if (botoes[i] == null) continue;

                bool ativa = opcoes[i] == DifficultySettings.Atual;

                var cores = botoes[i].colors;
                cores.normalColor = ativa ? UIFactory.CorBotaoDestaque : UIFactory.CorBotao;
                botoes[i].colors = cores;

                var texto = botoes[i].GetComponentInChildren<Text>();
                if (texto != null) texto.color = ativa ? UIFactory.CorAcento : UIFactory.CorTexto;
            }

            if (descricao != null) descricao.text = DifficultySettings.DescricaoDe(DifficultySettings.Atual);
        }
    }
}
