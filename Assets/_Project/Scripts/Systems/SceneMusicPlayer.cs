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

        private void Start()
        {
            AudioManager.Instance?.PlayMusic(sceneMusic);
        }
    }
}
