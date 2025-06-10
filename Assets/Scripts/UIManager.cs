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
    //contador
    [SerializeField] private TextMeshProUGUI timerText;

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
        StartCoroutine(WaitForGameManager());//contador

        if (globalCoinText == null)
        {
            GameObject canvas = GameObject.Find("CanvasPlayer");
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("PanelHud");
                if (panel != null)
                {
                    //contador
                    Transform timerTextTransform = panel.Find("TimerText");
                    if (timerTextTransform != null)
                    {
                        timerText = timerTextTransform.GetComponent<TextMeshProUGUI>();
                        Debug.Log("[UIManager] timerText asignado dinámicamente.");
                    }
                    else
                    {
                        Debug.LogWarning("[UIManager] No se encontró TimerText.");
                    }
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
    //contador
    private void OnTimeChanged(float oldTime, float newTime)
    {
        UpdateTimerDisplay(newTime);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        //contador
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.GetSyncedTime().OnValueChanged -= OnTimeChanged;
        }

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

        foreach (var entry in GameManager.playerRoles)
        {
            if (entry.Value)
                zombieCount++;
            else
                humanCount++;
        }

        humansNum.Value = humanCount;
        zombiesNum.Value = zombieCount;
    }

    public void ForceRefreshCounts()
    {
        if (!IsServer) return;

        int humanCount = 0;
        int zombieCount = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

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

        Debug.Log($"[UIManager] Conteo forzado: Humanos={humanCount}, Zombis={zombieCount}");
    }

    private IEnumerator WaitForCoinManager()
    {
        float timeout = 5f;
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
            globalCoinText.text = (current).ToString();
    }

    //contador
    public void UpdateTimerDisplay(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    //contador
    private IEnumerator WaitForGameManager()
    {
        float timeout = 5f;
        float elapsed = 0f;

        GameManager gm = null;
        while (gm == null && elapsed < timeout)
        {
            gm = FindObjectOfType<GameManager>();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (gm != null)
        {
            gm.GetSyncedTime().OnValueChanged += OnTimeChanged;
            UpdateTimerDisplay(gm.GetSyncedTime().Value); // sincronizar con el valor actual
            Debug.Log("[UIManager] Suscripción al temporizador exitosa.");
        }
        else
        {
            Debug.LogWarning("[UIManager] No se encontró GameManager a tiempo.");
        }
    }
}
