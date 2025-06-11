using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameMode
{
    Tiempo,
    Monedas
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    [SerializeField] NetworkManager _NetworkManager;
    [SerializeField] CoinManager coinManager;

    [Header("Prefabs")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject zombiePrefab;
    [SerializeField] private GameObject coinManagerPrefab;

    [Header("Team Settings")]
    [SerializeField] private int maxHumans = 2;
    [SerializeField] private int maxZombies = 2;

    [Header("Lobby UI")]
    [SerializeField] Toggle T_Moneda;
    [SerializeField] Toggle T_Tiempo;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyCountText;
    [SerializeField] private Button startGameButton;
    [SerializeField] GameObject ModeSelect;

    [Header("Panels")]
    [SerializeField] private EndGamePanelController endGamePanel;
    [SerializeField] private GameObject ErrorPanel;
    [SerializeField] GameObject StartPanel;

    [Header("Game Mode Settings")]
    [SerializeField] public GameMode gameMode;
    [SerializeField] private float matchDuration = 30f;
    [SerializeField] private int minutes = 5;

    [Header("Spawning & Level")]
    [SerializeField] private List<Transform> humanSpawnPoints;
    [SerializeField] private List<Transform> zombieSpawnPoints;
    public LevelBuilder levelBuilder;
    private int seed;

    // --- State Variables ---
    private NetworkVariable<float> syncedTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> readyPlayerCount = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private List<ulong> clientsReady = new List<ulong>();
    private bool partidalista = false;
    private float timeRemaining;
    private bool timerRunning = false;
    private bool timerInitialized = false;
    public bool botonesActivos = true;
    public static Dictionary<ulong, bool> playerRoles = new Dictionary<ulong, bool>(); // true = zombie, false = humano

    // ----> LISTA CLAVE: Para registrar todos los objetos de red que se instancian en la partida.
    private List<NetworkObject> spawnedNetworkObjects = new List<NetworkObject>();
    public NetworkVariable<float> GetSyncedTime()
    {
        return syncedTime;
    }
    #region Unity & Connection Callbacks
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
        if (!botonesActivos) return;
        if (GUILayout.Button("Host"))
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            _NetworkManager.StartHost();
        }
        if (GUILayout.Button("Client"))
        {
            _NetworkManager.StartClient();
        }
        if (GUILayout.Button("Server"))
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            _NetworkManager.StartServer();
        }
    }

    void StatusLabels()
    {
        var mode = _NetworkManager.IsHost ? "Host" : _NetworkManager.IsServer ? "Servidor" : "Cliente";
        GUILayout.Label("Transport: " + _NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Modo: " + mode);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("[GameManager] OnNetworkSpawn() en servidor.");
            playerRoles.Clear();
            seed = Random.Range(int.MinValue, int.MaxValue);

            if (levelBuilder != null)
            {
                levelBuilder.Build(seed);
                InformClientsToBuildLevelClientRpc(seed);
            }
            else
            {
                Debug.LogError("LevelBuilder no está asignado.");
            }

            //if (coinManagerPrefab != null)
            //{
            //    GameObject coinObj = Instantiate(coinManagerPrefab);
            //    NetworkObject netObj = coinObj.GetComponent<NetworkObject>();
            //    netObj.Spawn();
            //    // ----> AÑADIDO: Guardamos el NetworkObject del CoinManager.
            //    spawnedNetworkObjects.Add(netObj);
            //    Debug.Log($"[GameManager] CoinManager instanciado y spawneado: {netObj.NetworkObjectId}");
            //}

            ModeSelect.SetActive(true);
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneReloaded;
        }
        else
        {
            Debug.Log("[GameManager] OnNetworkSpawn() en cliente.");
            ModeSelect.SetActive(false);
            RequestBuildLevelServerRpc();
        }

        ConfigureLobbyUI();
        readyPlayerCount.OnValueChanged += OnReadyCountChanged;

        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => UpdateHostLobbyUI();
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => UpdateHostLobbyUI();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Limpieza de eventos para evitar errores.
        if (NetworkManager.Singleton != null)
        {
            readyPlayerCount.OnValueChanged -= OnReadyCountChanged;
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }
        if (IsServer && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneReloaded;
        }
        base.OnNetworkDespawn();
    }
    #endregion

    #region Game Loop (Update & State)
    private void Update()
    {
        if (partidalista && !timerInitialized)
        {
            if (StartPanel.activeSelf)
            {
                StartPanel.SetActive(false);
            }

            if (gameMode == GameMode.Tiempo)
            {
                timeRemaining = matchDuration;
                timerRunning = true;
                timerInitialized = true;
            }
        }

        if (IsServer)
        {
            //HandleGameModeToggles();
            UpdateTimer();
            CheckEndGameConditions();
        }
        else // Cliente
        {
            if (timerRunning && gameMode == GameMode.Tiempo)
            {
                UIManager.Instance?.UpdateTimerDisplay(syncedTime.Value);
            }
        }
    }

    private void HandleGameModeToggles()
    {
        if (T_Moneda.isOn)
        {
            if (gameMode != GameMode.Monedas)
            {
                gameMode = GameMode.Monedas;
                TellModeClientRpc(gameMode);
            }
        }
        else if (T_Tiempo.isOn) // Usar else if para evitar llamadas RPC innecesarias
        {
            if (gameMode != GameMode.Tiempo)
            {
                gameMode = GameMode.Tiempo;
                TellModeClientRpc(gameMode);
            }
        }
    }

    private void UpdateTimer()
    {
        if (timerRunning && gameMode == GameMode.Tiempo)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0f) timeRemaining = 0f;
            syncedTime.Value = timeRemaining;

            if (timeRemaining <= 0f)
            {
                timerRunning = false;
                Debug.Log("[GameManager] Tiempo agotado, ganan los humanos!");
                EndGameTimerClientRpc("Human", "Tiempo");
            }
        }
    }

    private void CheckEndGameConditions()
    {
        // Condición 1: No quedan humanos
        int humanosVivos = playerRoles.Count(kvp => !kvp.Value);
        if (partidalista && humanosVivos == 0 && NetworkManager.Singleton.ConnectedClients.Count > 1)
        {
            Debug.Log("[GameManager] No quedan humanos vivos, fin de partida. Ganan los zombis.");
            EndGameZombieWinClientRpc("Zombie", "DaIgual");
        }

        // Condición 2: Se consiguen las monedas
        if (gameMode == GameMode.Monedas)
        {
            var coinManagerInstance = GetCoinManagerInstance();
            if (coinManagerInstance != null && coinManagerInstance.globalCoins.Value >= 10)
            {
                EndGameCoinsClientRpc("Human", "Monedas");
            }
        }

        // Condición 3: Un jugador se desconecta
        if (NetworkManager.Singleton.ConnectedClients.Count < 2 && partidalista)
        {
            ShowErrorPanelClientRpc();
        }
    }
    #endregion

    #region Player Connection & Spawning
    private void HandleClientConnected(ulong clientId)
    {
        StartCoroutine(SpawnPlayerWithDelay(clientId));
    }

    private IEnumerator SpawnPlayerWithDelay(ulong clientId)
    {
        yield return new WaitForSeconds(1.0f);
        Debug.Log($"[GameManager] Cliente conectado y nivel listo para spawn: {clientId}");

        bool isZombie = !AsignarRol(clientId); // AsignarRol devuelve isHuman
        Vector3 spawnPosition = ObtenerPuntoDeSpawn(isZombie);
        GameObject prefab = isZombie ? zombiePrefab : playerPrefab;

        GameObject instancia = Instantiate(prefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = instancia.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);

        // ----> AÑADIDO: Guardamos el NetworkObject del jugador en nuestra lista.
        spawnedNetworkObjects.Add(netObj);

        PlayerController pc = instancia.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.IsZombieNetVar.Value = isZombie;
            UpdatePlayerRoleClientRpc(clientId, isZombie);
            Debug.Log($"[GameManager] Jugador {clientId} instanciado como {(isZombie ? "Zombi" : "Humano")}.");
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Cliente desconectado: {clientId}");
        if (playerRoles.ContainsKey(clientId))
        {
            playerRoles.Remove(clientId);
        }
        RemovePlayerRoleClientRpc(clientId);
        UIManager.Instance?.ForceRefreshCounts();
    }

    private void OnSceneReloaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log("[GameManager] Escena recargada en todos los clientes. Respawneando jugadores.");
        DesactivarBotonesClientRpc();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            StartCoroutine(SpawnPlayerWithDelay(client.ClientId));
        }
    }
    #endregion

    [ClientRpc]
    public void DesactivarBotonesClientRpc()
    {
        botonesActivos = false;
    }
    
    #region Lobby & Ready System
    // ----> REEMPLAZO COMPLETO: Esta función ahora asegura que el botón de listo sea interactuable.
    private void ConfigureLobbyUI()
    {
        if (IsHost)
        {
            readyButton.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
            readyCountText.gameObject.SetActive(true);
            // Limpiamos listeners viejos antes de añadir uno nuevo para evitar duplicados.
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(TellServerToStartGame);
            UpdateHostLobbyUI();
        }
        else // Es un cliente
        {
            readyButton.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
            readyCountText.gameObject.SetActive(false);
            // Limpiamos listeners viejos.
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(AvisarServerJugadorListo_out);
            // ----> CORRECCIÓN CLAVE: Aseguramos que el botón sea usable al (re)iniciar.
            readyButton.interactable = true;
        }
    }

    public void AvisarServerJugadorListo_out()
    {
        string chosenName = nameInputField.text;
        var localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        if (localPlayerController != null)
        {
            localPlayerController.SetPlayerNameServerRpc(chosenName);
        }
        PlayerClickedReadyServerRpc();
        readyButton.interactable = false;
    }

    private void OnReadyCountChanged(int previousValue, int newValue)
    {
        if (IsHost)
        {
            UpdateHostLobbyUI();
        }
    }

    private void UpdateHostLobbyUI()
    {
        if (!IsHost) return;
        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        readyCountText.text = $"Listos: {readyPlayerCount.Value} / {totalPlayers}";
        bool todosListos = (readyPlayerCount.Value == totalPlayers);
        startGameButton.interactable = (readyPlayerCount.Value >= 2 && todosListos);
    }

    private void TellServerToStartGame()
    {
        string hostName = nameInputField.text;
        if (string.IsNullOrEmpty(hostName)) hostName = "Host";
        var localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        if (localPlayerController != null)
        {
            localPlayerController.SetPlayerNameServerRpc(hostName);
        }
        TellServerToStartGameServerRpc();
    }
    #endregion

    #region Restart Logic
    // ----> REEMPLAZO COMPLETO: Nueva función de reinicio, mucho más robusta.
    [ServerRpc(RequireOwnership = false)]
    public void ReiniciarPartidaServerRpc()
    {
        Debug.Log("[GameManager] Petición de reinicio recibida en el servidor.");

        // 1. Limpiar TODOS los objetos de red instanciados
        Debug.Log($"[GameManager] Despawneando {spawnedNetworkObjects.Count} objetos de red.");
        foreach (var netObj in spawnedNetworkObjects)
        {
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true); // El 'true' destruye el objeto en todos los clientes.
            }
        }
        spawnedNetworkObjects.Clear(); // Limpiamos la lista para la siguiente partida.

        // 2. Resetear el estado de la partida en el servidor
        partidalista = false;
        timerRunning = false;
        timerInitialized = false;
        playerRoles.Clear();
        clientsReady.Clear();
        readyPlayerCount.Value = 1; // Reseteamos el contador (el host cuenta como 1 por defecto)

        // 3. Informar a TODOS los clientes que deben resetear su UI del lobby
        ResetLobbyUIClientRpc();

        // 4. Recargar la escena. Esto reconstruirá la escena y llamará a OnSceneReloaded cuando esté lista.
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // ----> NUEVA FUNCIÓN: RPC para que los clientes reseteen su UI.
    [ClientRpc]
    private void ResetLobbyUIClientRpc()
    {
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Recibida orden de resetear UI del Lobby.");
        if (StartPanel != null) StartPanel.SetActive(true);
        if (endGamePanel != null) endGamePanel.gameObject.SetActive(false);

        // Si no somos el host, reactivamos nuestro botón de "listo".
        if (!IsHost && readyButton != null)
        {
            readyButton.interactable = true;
        }
    }
    #endregion

    #region Server RPCs
    [ServerRpc(RequireOwnership = false)]
    private void PlayerClickedReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        var clientId = rpcParams.Receive.SenderClientId;

        // Si este cliente NO estaba ya en la lista de listos...
        if (!clientsReady.Contains(clientId))
        {
            // Lo añadimos
            clientsReady.Add(clientId);

            // Y AHORA actualizamos el contador.
            // Será 1 (el host) + el número de clientes que han pulsado el botón.
            readyPlayerCount.Value = 1 + clientsReady.Count;

            Debug.Log($"[GameManager] Cliente {clientId} ha marcado listo. Total listos: {readyPlayerCount.Value}");
        }
        else
        {
            Debug.LogWarning($"[GameManager] Cliente {clientId} ha intentado marcar listo de nuevo.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TellServerToStartGameServerRpc()
    {
        // Primero, asignamos el modo de juego oficial basado en los toggles.
        if (T_Moneda.isOn)
        {
            gameMode = GameMode.Monedas;
        }
        else // Si no es moneda, asumimos que es tiempo (o cualquier otro modo por defecto)
        {
            gameMode = GameMode.Tiempo;
        }

        // AHORA que 'gameMode' está 100% actualizado, lo comunicamos a los clientes.
        TellModeClientRpc(gameMode);
        Debug.Log($"[GameManager] Partida iniciada oficialmente en modo: {gameMode}");

        // Y DESPUÉS, actuamos en consecuencia.
        if (gameMode == GameMode.Monedas)
        {
            Debug.Log("[GameManager] Creando CoinManager para el modo Monedas...");
            if (coinManagerPrefab != null)
            {
                GameObject coinObj = Instantiate(coinManagerPrefab);
                NetworkObject netObj = coinObj.GetComponent<NetworkObject>();
                netObj.Spawn();
                RegisterSpawnedObject(netObj);

                CoinManager cm = coinObj.GetComponent<CoinManager>();
                if(cm != null)
                {
                    cm.ResetCoinServerRpc();
                }
            }
        }
        if (gameMode == GameMode.Tiempo)
        {
            SetActiveFalseCoinsClientRpc();
        }
        // Si es modo Tiempo, simplemente no hace nada, que es lo correcto.

        // El resto de la función se queda igual.
        partidalista = true;
        StartGameClientRpc();
    }
    [ClientRpc]
    public void SetActiveFalseCoinsClientRpc()
    {
        GameObject[] coinsInScene = GameObject.FindGameObjectsWithTag("Moneda");
        if (coinsInScene.Length > 0)
        {
            foreach (GameObject coin in coinsInScene)
            {
                coin.SetActive(false);
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestBuildLevelServerRpc()
    {
        Debug.Log("Cliente ha solicitado construir el nivel.");
        InformClientsToBuildLevelClientRpc(seed);
    }
    #endregion

    #region Client RPCs
    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("¡La partida comienza!");
        StartPanel.SetActive(false);
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

    [ClientRpc]
    private void TellModeClientRpc(GameMode gameMode_)
    {
        gameMode = gameMode_;
    }

    [ClientRpc]
    public void UpdatePlayerRoleClientRpc(ulong clientId, bool isZombie)
    {
        Debug.Log($"[GameManager ClientRpc] Actualizando rol para el cliente {clientId}: isZombie = {isZombie}");
        playerRoles[clientId] = isZombie;
        UIManager.Instance?.ForceRefreshCounts();
    }

    [ClientRpc]
    public void RemovePlayerRoleClientRpc(ulong clientId)
    {
        if (playerRoles.ContainsKey(clientId))
        {
            playerRoles.Remove(clientId);
            Debug.Log($"[GameManager ClientRpc] Rol eliminado para cliente {clientId} en este cliente.");
        }
        UIManager.Instance?.ForceRefreshCounts();
    }

    [ClientRpc]
    private void ShowErrorPanelClientRpc()
    {
        if (ErrorPanel != null)
        {
            ErrorPanel.SetActive(true);
        }
    }
    #endregion

    #region End Game
    public void EndGame(string winnerTeam, string reason)
    {
        // Tu lógica de EndGame...
    }

    [ClientRpc]
    public void EndGameTimerClientRpc(string winnerTeam, string gameMode)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool localPlayerIsZombie = playerRoles.ContainsKey(localId) && playerRoles[localId];
        EndGame(winnerTeam, gameMode, localPlayerIsZombie);
    }

    [ClientRpc]
    public void EndGameCoinsClientRpc(string winnerTeam, string gameMode)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool localPlayerIsZombie = playerRoles.ContainsKey(localId) && playerRoles[localId];
        EndGame(winnerTeam, gameMode, localPlayerIsZombie);
    }

    [ClientRpc]
    public void EndGameZombieWinClientRpc(string winnerTeam, string gameMode)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool localPlayerIsZombie = playerRoles.ContainsKey(localId) && playerRoles[localId];
        EndGame(winnerTeam, gameMode, localPlayerIsZombie);
    }

    // Esta es una versión simplificada de tu EndGame, ajústala a tu panel.
    public void EndGame(string winnerTeam, string gameMode, bool localPlayerIsZombie)
    {
        string resultMessage = ""; // Empezamos con un string vacío

        // --- Esta es tu lógica original, que funciona perfectamente ---

        // Condición 1: Ganan los humanos por monedas
        if (winnerTeam == "Human" && gameMode == "Monedas")
        {
            resultMessage = localPlayerIsZombie
                ? "Has perdido... los humanos han recolectado todas las monedas."
                : "¡Ganaste! Habéis recolectado todas las monedas.";
        }
        // Condición 2: Ganan los humanos por tiempo
        else if (winnerTeam == "Human" && gameMode == "Tiempo")
        {
            resultMessage = localPlayerIsZombie
                ? "Has perdido... se ha acabado el tiempo y los humanos han sobrevivido."
                : "¡Ganaste! Habéis sobrevivido.";
        }
        // Condición 3: Ganan los zombis porque no quedan humanos
        else if (winnerTeam == "Zombie" && gameMode == "DaIgual")
        {
            resultMessage = localPlayerIsZombie
                ? "¡Ganaste! Habéis atrapado a todos los humanos."
                : "Has perdido... los zombis os han atrapado a todos.";
        }
        else
        {
            // Mensaje por si ocurre una condición inesperada
            resultMessage = "La partida ha terminado.";
        }

        // Finalmente, mostramos el panel con el mensaje correcto
        if (endGamePanel != null)
        {
            endGamePanel.ShowEndGamePanel(resultMessage);
        }
        else
        {
            Debug.LogError("EndGamePanel no está asignado en el GameManager.");
        }
    }

    public void RegisterSpawnedObject(NetworkObject newObject)
    {
        if (IsServer)
        {
            spawnedNetworkObjects.Add(newObject);
            Debug.Log($"[GameManager] Objeto '{newObject.name}' registrado para limpieza. Total en lista: {spawnedNetworkObjects.Count}");
        }
    }
    #endregion

    #region Utility & Role Assignment
    private CoinManager GetCoinManagerInstance()
    {
        return FindObjectOfType<CoinManager>();
    }

    public bool AsignarRol(ulong clientId)
    {
        int totalZombies = playerRoles.Count(kvp => kvp.Value);
        int totalHumanos = playerRoles.Count - totalZombies;

        bool preferirHumano = (totalZombies > totalHumanos);

        if (preferirHumano && totalHumanos < maxHumans)
        {
            playerRoles[clientId] = false; // Humano
            return true;
        }
        else if (!preferirHumano && totalZombies < maxZombies)
        {
            playerRoles[clientId] = true; // Zombi
            return false;
        }

        // Fallback si el equipo preferido está lleno
        if (totalHumanos < maxHumans)
        {
            playerRoles[clientId] = false; // Humano
            return true;
        }
        else if (totalZombies < maxZombies)
        {
            playerRoles[clientId] = true; // Zombi
            return false;
        }

        Debug.LogError($"No se pudo asignar rol a {clientId}, juego lleno.");
        playerRoles[clientId] = false; // Default a humano si todo falla
        return true;
    }

    private Vector3 ObtenerPuntoDeSpawn(bool isZombie)
    {
        if (isZombie)
        {
            int numZombis = playerRoles.Count(kvp => kvp.Value);
            int spawnIndex = (numZombis - 1) % zombieSpawnPoints.Count;
            if (spawnIndex >= 0 && spawnIndex < zombieSpawnPoints.Count)
            {
                return zombieSpawnPoints[spawnIndex].position;
            }
        }
        else // Es Humano
        {
            int numHumanos = playerRoles.Count(kvp => !kvp.Value);
            int spawnIndex = (numHumanos - 1) % humanSpawnPoints.Count;
            if (spawnIndex >= 0 && spawnIndex < humanSpawnPoints.Count)
            {
                return humanSpawnPoints[spawnIndex].position;
            }
        }
        // Fallback por si algo sale mal
        return new Vector3(0, 5, 0);
    }
    #endregion
}