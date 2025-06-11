using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CoinManager : NetworkBehaviour
{
    public static CoinManager instance;


    public NetworkVariable<int> globalCoins = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (instance == null)
        {
            instance = this;
        }

        Debug.Log("[CoinManage] Spawneado y listo.");
        Debug.Log("[CoinManager] NetworkObject ID: " + NetworkObjectId + ", Owner: " + OwnerClientId);

    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCoinServerRpc()
    {
        globalCoins.Value++;
        Debug.Log($"[CoinManager] AddCoinServerRpc llamado. Nuevo valor: {globalCoins.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetCoinServerRpc()
    {
        globalCoins.Value = 0;
        Debug.Log($"[CoinManager] AddCoinServerRpc llamado. Nuevo valor: {globalCoins.Value}");
    }


}
