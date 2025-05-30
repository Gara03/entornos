using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIManager : NetworkBehaviour
{
    [SerializeField] NetworkManager _NetworkManager;

    public static UIManager Instance;

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI humanCountText;
    [SerializeField] private TextMeshProUGUI zombieCountText;
    [SerializeField] private TextMeshProUGUI globalCoinText;

    private NetworkVariable<int> humansNum = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> zombiesNum = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> globalCoins = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Mostrar los valores actualizados en UI (todos los clientes)
        humanCountText.text = humansNum.Value.ToString();
        zombieCountText.text = zombiesNum.Value.ToString();
        /*if (globalCoinText != null)
            globalCoinText.text = globalCoins.Value.ToString();*/

        // Solo el servidor actualiza los datos de red
        if (!IsServer) return;

        int humanCount = 0;
        int zombieCount = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                if (player.isZombie)
                    zombieCount++;
                else
                    humanCount++;
            }
        }

        humansNum.Value = humanCount;
        zombiesNum.Value = zombieCount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddGlobalCoinServerRpc()
    {
        globalCoins.Value++;
    }

    private void OnEnable()
    {
        globalCoins.OnValueChanged += OnGlobalCoinsChanged;
    }

    private void OnDisable()
    {
        globalCoins.OnValueChanged -= OnGlobalCoinsChanged;
    }

    private void OnGlobalCoinsChanged(int previousValue, int newValue)
    {
        if (globalCoinText != null)
            globalCoinText.text = newValue.ToString();
    }
}
