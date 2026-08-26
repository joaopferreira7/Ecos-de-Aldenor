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

        private void Start()
        {
            AudioManager.Instance?.PlayMusic(sceneMusic, loop);
        }
    }
}
