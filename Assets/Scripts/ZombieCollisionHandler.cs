using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Collections;

public class ZombieCollisionHandler : NetworkBehaviour
{
    [SerializeField] private GameObject zombiePrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return; 

        PlayerController target = collision.gameObject.GetComponent<PlayerController>();

        // Solo infectamos si colisionamos con un PlayerController que sea humano
        if (target != null && !target.IsZombieNetVar.Value) // Usamos la NetworkVariable para verificar el estado actual
        {
            Debug.Log($"[ServerRpc Call] Zombie {OwnerClientId} colisionó con el humano {target.OwnerClientId}, enviando solicitud de infección.");

            // Pasamos el ID del NetworkObject y el OwnerClientId del objetivo
            TryInfectServerRpc(target.NetworkObject.NetworkObjectId, target.OwnerClientId);
        }

        /*if (target != null && !target.isZombie)
        {
            Debug.Log("Colisión con humano, intentando infectar...");

            NetworkObject netObj = target.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                TryInfectServerRpc(netObj.NetworkObjectId, netObj.OwnerClientId);
            }
        }*/
    }

    [ServerRpc]
    private void TryInfectServerRpc(ulong targetNetworkId, ulong targetClientId)
    {
        // Este código se ejecuta en el servidor.
        Debug.Log($"[Servidor] Recibido TryInfectServerRpc para el objetivo {targetClientId}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            PlayerController humanPlayerController = targetObj.GetComponent<PlayerController>();

            // Doble verificación en el servidor para asegurar que es un humano y no se ha infectado ya
            if (humanPlayerController != null && !humanPlayerController.IsZombieNetVar.Value)
            {
                Debug.Log($"[Servidor] Humano {targetClientId} confirmado para infección. Despawneando y Spawneando como zombi.");

                FixedString32Bytes nameToPreserve = humanPlayerController.GetCurrentName();

                Vector3 pos = targetObj.transform.position;
                Quaternion rot = targetObj.transform.rotation;

                // Despawn el NetworkObject del humano.
                // Es importante que este despawn sea manejado por el servidor.
                targetObj.Despawn(true); // El 'true' opcional puede ser útil para asegurar que el objeto se destruye completamente.

                // Iniciamos la corrutina para respawnear como zombi después de un pequeño retraso
                StartCoroutine(RespawnZombieAfterDespawn(targetClientId, pos, rot, nameToPreserve));
            }
            else
            {
                Debug.LogWarning($"[Servidor] El objetivo {targetClientId} es nulo o ya es un zombi. No se necesita infección.");
            }
        }
        else
        {
            Debug.LogError($"[Servidor] Objeto de red objetivo con ID {targetNetworkId} no encontrado en objetos spawneados.");
        }
    }

    private IEnumerator RespawnZombieAfterDespawn(ulong clientId, Vector3 position, Quaternion rotation, FixedString32Bytes nameToPreserve)
    {
        // Pequeño retraso para asegurar que el despawn se ha propagado o procesado
        yield return new WaitForSeconds(0.2f);

        // Crea una nueva instancia del prefab de zombi
        GameObject newZombie = Instantiate(zombiePrefab, position, Quaternion.identity);
        NetworkObject netObj = newZombie.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            // Spawnea el nuevo zombi con la misma propiedad del cliente original
            netObj.SpawnWithOwnership(clientId);

            PlayerController pc = newZombie.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.InitializeNameOnServer(nameToPreserve);

                pc.IsZombieNetVar.Value = true; // El servidor establece la NetworkVariable (se sincroniza a todos)
                pc.isZombie = true; // Actualiza la variable local en el servidor

                // Actualiza el diccionario estático del GameManager en el servidor
                if (GameManager.playerRoles.ContainsKey(clientId))
                {
                    GameManager.playerRoles[clientId] = true; // true = zombi
                    Debug.Log($"[Servidor] GameManager.playerRoles actualizado para el cliente {clientId} a ZOMBIE.");
                }

                // Llama al ClientRpc en GameManager para actualizar el diccionario en TODOS los clientes
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    gm.UpdatePlayerRoleClientRpc(clientId, true);
                }
                else
                {
                    Debug.LogError("[Servidor] GameManager no encontrado para actualizar roles en clientes.");
                }

                Debug.Log($"[Infección] Jugador {clientId} ha sido transformado en zombi (Prefab).");
            }
            else
            {
                Debug.LogError("¡El prefab del zombi no tiene un componente PlayerController!");
            }
        }
        else
        {
            Debug.LogError("¡El prefab del zombi no tiene un NetworkObject!");
        }
    }
}