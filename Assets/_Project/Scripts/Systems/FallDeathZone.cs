using UnityEngine;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Zona de 'morte por queda', posicionada bem abaixo do nivel. Se o
    /// jogador cair para fora do mapa (por exemplo, ao pular por uma borda),
    /// esta zona aciona o RespawnHandler, que o retorna ao ultimo checkpoint
    /// em vez de cair infinitamente (ou vai para Game Over se ainda nao
    /// houver checkpoint registrado na fase).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FallDeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            RespawnHandler respawnHandler = other.GetComponent<RespawnHandler>();
            if (respawnHandler != null)
            {
                respawnHandler.HandleFallOutOfBounds();
                return;
            }

            other.GetComponent<HealthSystem>()?.Kill();
        }
    }
}
