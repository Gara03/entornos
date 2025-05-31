using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ZombieCollisionHandler : NetworkBehaviour
{
    [SerializeField] private GameObject zombiePrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return; // Solo el dueño del zombie detecta la colisión

        PlayerController target = collision.gameObject.GetComponent<PlayerController>();

        if (target != null && !target.isZombie)
        {
            Debug.Log("Colisión con humano, intentando infectar...");

            NetworkObject netObj = target.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                TryInfectServerRpc(netObj.NetworkObjectId, netObj.OwnerClientId);
            }
        }
    }

    [ServerRpc]
    private void TryInfectServerRpc(ulong targetNetworkId, ulong targetClientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            Vector3 pos = targetObj.transform.position;
            Quaternion rot = targetObj.transform.rotation;

            //targetObj.Despawn(true); // Mueve esto aquí dentro
            targetObj.Despawn();

            StartCoroutine(RespawnZombieAfterDespawn(targetClientId, pos, rot));
        }
    }

    private IEnumerator RespawnZombieAfterDespawn(ulong clientId, Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(0.2f); // Espera un frame para que se complete el despawn

        GameObject newZombie = Instantiate(zombiePrefab, position, rotation);
        NetworkObject netObj = newZombie.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.SpawnWithOwnership(clientId); // Asigna el ownership al jugador original

            PlayerController pc = newZombie.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.isZombie = true;
                GameManager.playerRoles[clientId] = true; // <- actualiza el diccionario
            }

            Debug.Log($"[Infección] Jugador {clientId} ha sido transformado en zombi.");
        }
        else
        {
            Debug.LogError("¡Zombie prefab sin NetworkObject!");
        }
    }

}