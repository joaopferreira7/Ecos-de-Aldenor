using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Toca a musica de fundo definida para a cena atual assim que ela carrega.
    /// Mantido como um componente simples e independente para nao acoplar essa
    /// responsabilidade ao GameManager ou SceneController.
    /// </summary>
    public class SceneMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip sceneMusic;
        [Tooltip("Se falso, a trilha toca uma unica vez (ex.: sting de vitoria/derrota).")]
        [SerializeField] private bool loop = true;

        [Tooltip("Corrige o nivel DESTA faixa. As trilhas vem de pacotes diferentes e nao " +
                 "foram masterizadas juntas: 1 = como gravada, 0,35 = bem mais baixa.")]
        [Range(0f, 2f)]
        [SerializeField] private float trackGain = 1f;

        private void Start()
        {
            AudioManager.Instance?.PlayMusic(sceneMusic, loop, trackGain);
        }
    }
}
