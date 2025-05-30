using Unity.Netcode;
using UnityEngine;

public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Solo servidor maneja la lógica

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.isZombie == false)
            {
                player.CoinCollected();
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
