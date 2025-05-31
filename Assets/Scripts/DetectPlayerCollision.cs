using Unity.Netcode;
using UnityEngine;

public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.isZombie)
            {
                // Moneda personal
                player.CoinCollected();


                if (CoinManager.instance != null)
                {
                    Debug.Log("[DetectPlayerCollision] CoinManager encontrado. Llamando a AddCoinServerRpc()");
                    CoinManager.instance.AddCoinServerRpc();
                }
                else
                {
                    Debug.LogWarning("[DetectPlayerCollision] CoinManager.instance es NULL en servidor.");
                }

                // Sonido
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

                // Destruir moneda
                Destroy(gameObject);
            }
        }
    }
}
