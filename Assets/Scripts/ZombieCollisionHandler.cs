using UnityEngine;

public class ZombieCollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
        Debug.Log("Colisión detectada con " + collision.gameObject.name);
        if (playerController != null && !playerController.isZombie)
        {
            playerController.isZombie = true;
            Debug.Log("PlayerController encontrado: " + playerController.uniqueID);

            // Obtener el prefab de humano desde el LevelManager
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null && collision.gameObject.name.Contains(levelManager.PlayerPrefabName))
            {
                // Cambiar el humano a zombie
                levelManager.ChangeToZombie(collision.gameObject, playerController.enabled);
            }
        }
    }
}


