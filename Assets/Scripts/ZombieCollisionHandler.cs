using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ZombieCollisionHandler : NetworkBehaviour
{
    [SerializeField] private GameObject zombiePrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return; // Solo el due�o del zombie detecta la colisi�n

        PlayerController target = collision.gameObject.GetComponent<PlayerController>();

        if (target != null && !target.isZombie)
        {
            Debug.Log("Colisi�n con humano, intentando infectar...");

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

            targetObj.Despawn(true); // Mueve esto aqu� dentro

            StartCoroutine(RespawnAsZombie(targetClientId, pos, rot));
        }
    }

    private IEnumerator RespawnAsZombie(ulong clientId, Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(0.2f); // Delay para evitar conflicto con despawn

        GameObject newZombie = Instantiate(zombiePrefab, position, rotation);
        NetworkObject netObj = newZombie.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.SpawnWithOwnership(clientId);

            PlayerController pc = newZombie.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.isZombie = true;
            }

            Debug.Log($"[Infecci�n] El jugador {clientId} ha sido transformado en zombi.");
        }
        else
        {
            Debug.LogError("�Zombie prefab sin NetworkObject!");
        }
    }

}