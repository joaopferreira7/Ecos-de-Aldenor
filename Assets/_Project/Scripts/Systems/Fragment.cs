using UnityEngine;
using EcosDeAldenor.Core;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Item colecionavel espalhado pelas fases. Ao ser tocado pelo jogador,
    /// soma 1 fragmento no GameManager e se destroi. Requer um Collider2D
    /// marcado como Trigger no mesmo GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Fragment : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private AudioClip collectSfx;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            GameManager.Instance?.AddFragment(value);
            AudioManager.Instance?.PlaySfx(collectSfx);

            Destroy(gameObject);
        }
    }
}
