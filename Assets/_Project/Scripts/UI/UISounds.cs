using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// Som nos botoes: um toque ao passar o mouse, outro ao clicar.
    ///
    /// Os menus do jogo eram mudos - a unica reacao a um clique era a troca de
    /// cena, que so chega meio segundo depois, atras do fade. Um menu sem som de
    /// clique parece travado mesmo quando funciona, e e das coisas que mais
    /// rapido fazem uma tela parecer inacabada.
    ///
    /// Os sons ja estavam no projeto sem uso (pacote Leohpaz, pasta
    /// 10_UI_Menu_SFX). Este componente varre os botoes da cena e pendura o
    /// gatilho em cada um, para nao ser preciso ligar som por som no Inspector -
    /// e a UI construida em codigo (tela de tutorial, seletor de dificuldade)
    /// pede o mesmo tratamento pelo metodo estatico.
    /// </summary>
    public class UISounds : MonoBehaviour
    {
        [SerializeField] private AudioClip somDeFoco;   // mouse por cima
        [SerializeField] private AudioClip somDeClique;

        private static UISounds instancia;

        private void Awake()
        {
            instancia = this;
        }

        private void OnDestroy()
        {
            if (instancia == this) instancia = null;
        }

        private void Start()
        {
            foreach (var botao in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Aplicar(botao);
        }

        /// <summary>
        /// Da voz a um botao. Silencioso e sem erro quando nao ha um UISounds na
        /// cena - assim a UI de codigo pode chamar sempre, sem checar.
        /// </summary>
        public static void Aplicar(Button botao)
        {
            if (botao == null || instancia == null) return;
            if (botao.GetComponent<UISoundHook>() != null) return;

            var hook = botao.gameObject.AddComponent<UISoundHook>();
            hook.Configurar(instancia.somDeFoco, instancia.somDeClique);
        }
    }

    /// <summary>
    /// O gatilho propriamente dito, num componente por botao. Ser um componente
    /// (e nao um listener anonimo) e o que impede o mesmo botao de ganhar dois
    /// sons quando a varredura roda de novo.
    /// </summary>
    public class UISoundHook : MonoBehaviour, IPointerEnterHandler
    {
        private AudioClip foco;
        private AudioClip clique;
        private Button botao;

        public void Configurar(AudioClip somDeFoco, AudioClip somDeClique)
        {
            foco = somDeFoco;
            clique = somDeClique;

            botao = GetComponent<Button>();
            if (botao != null) botao.onClick.AddListener(() => AudioManager.Instance?.PlaySfx(clique));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (botao != null && !botao.interactable) return;
            AudioManager.Instance?.PlaySfx(foco);
        }
    }
}
