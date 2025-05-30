using Unity.Netcode;
using UnityEngine;

public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        UIManager.Instance?.AddGlobalCoinServerRpc();

        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.isZombie)
            {
                player.CoinCollected(); // Actualiza solo el UI local

                // Incrementa contador global
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.AddGlobalCoinServerRpc(); // llama al servidor para que aumente la moneda global
                }

                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
