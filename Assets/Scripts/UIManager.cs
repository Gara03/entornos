using System.Collections;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (globalCoinText == null)
        {
            GameObject canvas = GameObject.Find("CanvasPlayer");
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("PanelHud");
                if (panel != null)
                {
                    Transform coinTextTransform = panel.Find("CoinsValue");
                    if (coinTextTransform != null)
                    {
                        globalCoinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                        Debug.Log("[UIManager] globalCoinText asignado dinámicamente.");
                    }
                    else
                    {
                        Debug.LogWarning("[UIManager] No se encontró CoinsValue.");
                    }
                }
                else
                {
                    Debug.LogWarning("[UIManager] No se encontró PanelHud.");
                }
            }
            else
            {
                Debug.LogWarning("[UIManager] No se encontró CanvasPlayer.");
            }
        }

        StartCoroutine(WaitForCoinManager());


        Debug.Log("[UIManager] el canvas es:"+FindObjectOfType<UIManager>());

    }


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

    }

    private void Update()
    {
        // Mostrar los valores actualizados en UI (todos los clientes)
        humanCountText.text = humansNum.Value.ToString();
        zombieCountText.text = zombiesNum.Value.ToString();

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

    private IEnumerator WaitForCoinManager()
    {
        float timeout = 5f; // evita esperar eternamente
        float elapsed = 0f;

        while (CoinManager.instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (CoinManager.instance != null)
        {
            CoinManager.instance.globalCoins.OnValueChanged += OnGlobalCoinsChanged;
            OnGlobalCoinsChanged(0, CoinManager.instance.globalCoins.Value);
            Debug.Log("[UIManager] CoinManager conectado correctamente.");
        }
        else
        {
            Debug.LogWarning("[UIManager] Timeout esperando CoinManager.");
        }
    }



    private void OnGlobalCoinsChanged(int previous, int current)
    {
        if (globalCoinText != null)
            globalCoinText.text = current.ToString();
    }
}
