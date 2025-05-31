using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode
{
    Tiempo,
    Monedas
}

public class GameManager : NetworkBehaviour
{
    [SerializeField] NetworkManager _NetworkManager;

    [Header("Prefabs")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject zombiePrefab;

    [Header("Team Settings")]
    [SerializeField] private int maxHumans = 2;
    [SerializeField] private int maxZombies = 2;

    [SerializeField] GameObject StartPanel;

    [SerializeField] GameObject ModeSelect;

    [SerializeField] Toggle T_Moneda;
    [SerializeField] Toggle T_Tiempo;

    private GameObject[] Coins;

    private bool jugador1ready = false;
    private bool jugador2ready = false;
    private bool jugador3ready = false;
    private bool jugador4ready = false;

    private bool partidalista = false;

    [Header("Game Mode Settings")]
    [SerializeField] public GameMode gameMode;
    [SerializeField] private int minutes = 5;

    [SerializeField] private GameObject coinManagerPrefab;

    private List<Vector3> humanSpawnPoints = new List<Vector3>();
    private List<Vector3> zombieSpawnPoints = new List<Vector3>();
    private float remainingSeconds;


    private int seed;
    public LevelBuilder levelBuilder;

    private Dictionary<ulong, bool> playerRoles = new Dictionary<ulong, bool>(); // true = zombie, false = humano

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!_NetworkManager.IsClient && !_NetworkManager.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }
        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host"))
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            _NetworkManager.StartHost();
            
        }
        if (GUILayout.Button("Client"))
        {
            _NetworkManager.StartClient();
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
        if (GUILayout.Button("Server"))
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            _NetworkManager.StartServer();
            
        }
    }

    void StatusLabels()
    {
        var mode = _NetworkManager.IsHost ? "Host" : _NetworkManager.IsServer ? "Servidor" : "Cliente";
        GUILayout.Label("Transport: " + _NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Modo: " + mode);
    }

    private void Start()
    {
        if (IsClient && !IsServer)
        {
            Debug.Log("[GameManager] Cliente (no host), solicitando construir el nivel.");
        }

        if (IsServer)
        {
            Debug.Log("[GameManager] Start() en servidor.");
        }

        remainingSeconds = minutes * 60;
    }

    private void Update()
    {
        if(jugador1ready && jugador2ready && jugador3ready && jugador4ready)
        {
            partidalista = true;
            partidalistaClientRpc();
        }

        if (partidalista)
        {
            if (StartPanel.activeSelf)
            {
                StartPanel.SetActive(false);
            }
        }
        if (IsServer)
        {
            if (T_Moneda.isOn)
            {
                gameMode = GameMode.Monedas;
                TellModeClientRpc(gameMode);
            }
            if (T_Tiempo.isOn)
            {
                gameMode = GameMode.Tiempo;
                TellModeClientRpc(gameMode);
            }
        }
    }

    [ClientRpc]
    private void TellModeClientRpc(GameMode gameMode_)
    {
        gameMode = gameMode_;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log("[GameManager] OnNetworkSpawn() en servidor.");
            if (levelBuilder == null)
            {
                Debug.LogError("LevelBuilder no está asignado.");
                return;
            }

            levelBuilder.Build(seed);
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();

            InformClientsToBuildLevelClientRpc(seed);

            if (coinManagerPrefab != null)
            {
                GameObject coinObj = Instantiate(coinManagerPrefab);
                NetworkObject netObj = coinObj.GetComponent<NetworkObject>();
                netObj.Spawn();

                Debug.Log($"[GameManager] CoinManager instanciado y spawneado: {netObj.NetworkObjectId}");
            }

            ModeSelect.SetActive(true);
        }
        else
        {
            Debug.Log("[GameManager] OnNetworkSpawn() en cliente.");
            ModeSelect.SetActive(false);
            RequestBuildLevelServerRpc();
        }

    }

    [ClientRpc]
    private void partidalistaClientRpc()
    {
        partidalista = true;
        Coins = GameObject.FindGameObjectsWithTag("Moneda");
        if (gameMode == GameMode.Tiempo)
        {
            for (int i = 0; i < Coins.Length; i++) 
            {
                Coins[i].SetActive(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestBuildLevelServerRpc()
    {
        Debug.Log("Cliente ha solicitado construir el nivel.");
        InformClientsToBuildLevelClientRpc(seed);
    }

    [ClientRpc]
    private void InformClientsToBuildLevelClientRpc(int seed)
    {
        if (!IsServer && levelBuilder != null)
        {
            Debug.Log("[GameManager] Cliente construyendo nivel con semilla:" + seed);
            levelBuilder.Build(seed);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Cliente conectado: {clientId}");

        bool isHuman = AsignarRol(clientId);
        Vector3 spawnPosition = ObtenerPuntoDeSpawn(isHuman);
        GameObject prefab = isHuman ? playerPrefab : zombiePrefab;

        // Creamos el objeto pero aún no lo spawneamos
        GameObject instancia = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Spawneamos como PlayerObject (esto asigna ownership y activa IsOwner correctamente)
        NetworkObject netObj = instancia.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);

        // Marcar como zombi en el PlayerController
        PlayerController pc = instancia.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.isZombie = !isHuman;
        }

        Debug.Log($"[GameManager] Jugador {clientId} {(isHuman ? "Humano" : "Zombi")} instanciado en {spawnPosition}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerReadyServerRpc(ulong clientId)
    {
        switch (clientId)
        {
            case 0:
                jugador1ready = true;
                break;
            case 1:
                jugador2ready = true;
                break;
            case 2:
                jugador3ready = true;
                break;
            case 3:
                jugador4ready = true;
                break;
        }
    }

    public void AvisarServerJugadorListo_out()
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        PlayerReadyServerRpc(clientId);
    }

    private bool AsignarRol(ulong clientId)
    {
        int totalZombies = 0;
        int totalHumanos = 0;

        foreach (var entry in playerRoles.Values)
        {
            if (entry) totalZombies++;
            else totalHumanos++;
        }

        // Lógica original tuya, respetada al 100%
        if (totalZombies == 0 && totalHumanos == 0)
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI (primer jugador)");
            return false;
        }
        else if (totalZombies < totalHumanos && totalZombies < maxZombies)
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI");
            return false;
        }
        else if (totalZombies > totalHumanos && totalHumanos < maxHumans)
        {
            playerRoles[clientId] = false;
            Debug.Log($"Jugador {clientId} asignado como HUMANO");
            return true;
        }
        else if (totalHumanos == totalZombies && totalZombies < maxZombies)
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI");
            return false;
        }
        else if (totalZombies >= maxZombies && totalHumanos >= maxHumans)
        {
            Debug.Log($"ERROR: La sala está llena, intentalo más tarde :)");
            return false;
        }

        Debug.LogWarning("No se pudo asignar rol. Asignando como HUMANO por defecto.");
        playerRoles[clientId] = false;
        return true;
    }

    private Vector3 ObtenerPuntoDeSpawn(bool isHuman)
    {
        if (isHuman && humanSpawnPoints.Count > 0)
        {
            return humanSpawnPoints[Random.Range(0, humanSpawnPoints.Count)];
        }
        else if (!isHuman && zombieSpawnPoints.Count > 0)
        {
            return zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Count)];
        }
        else
        {
            Debug.LogWarning("No hay puntos de spawn definidos.");
            return Vector3.zero;
        }
    }
}
