using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Collections; // Necesario para FixedString
using System.Runtime.CompilerServices;
using System.Collections;

public enum GameMode
{
    Tiempo,
    Monedas
}

public class GameManager : NetworkBehaviour
{
    [SerializeField] NetworkManager _NetworkManager;

    [SerializeField] CoinManager coinManager;

    [Header("Prefabs")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject zombiePrefab;

    [Header("Team Settings")]
    [SerializeField] private int maxHumans = 2;
    [SerializeField] private int maxZombies = 2;

    [SerializeField] Toggle T_Moneda;
    [SerializeField] Toggle T_Tiempo;

    private GameObject[] Coins;

    //contador
    [SerializeField] private float matchDuration = 30f; // Duración total de la partida en segundos
    private float timeRemaining;
    private bool timerRunning = false;
    private NetworkVariable<float> syncedTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool timerInitialized = false;

    // EN GameManager.cs
    private NetworkVariable<int> readyPlayerCount = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool partidalista = false;

    [Header("Game Mode Settings")]
    [SerializeField] public GameMode gameMode;
    [SerializeField] private int minutes = 5;

    [SerializeField] private GameObject coinManagerPrefab;

    [Header("Puntos de Spawn Manuales")]
    [SerializeField] private List<Transform> humanSpawnPoints;
    [SerializeField] private List<Transform> zombieSpawnPoints;
    private float remainingSeconds;


    private int seed;
    public LevelBuilder levelBuilder;

    public static Dictionary<ulong, bool> playerRoles = new Dictionary<ulong, bool>(); // true = zombie, false = humano

    [Header("Panels")]
    [SerializeField] private EndGamePanelController endGamePanel;
    [SerializeField] private GameObject ErrorPanel;
    [SerializeField] GameObject StartPanel;

    [Header("Lobby UI")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyCountText;
    [SerializeField] private Button startGameButton;

    [SerializeField] GameObject ModeSelect;

    public bool botonesActivos = true;


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
        if (!botonesActivos)
        {
            return;
        }
        if (GUILayout.Button("Host"))
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
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

    private void Start()
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 4 && IsServer)
        {
            DesactivarUIClientRpc();
        }
        if (IsClient && !IsServer)
        {
            Debug.Log("[GameManager] Cliente (no host), solicitando construir el nivel.");
        }

        if (IsServer)
        {
            Debug.Log("[GameManager] Start() en servidor.");
            //contador
            if (partidalista && !timerRunning && gameMode == GameMode.Tiempo)
            {
                timeRemaining = matchDuration;
                timerRunning = true;
            }
            timeRemaining = matchDuration;  //contador
        }

        remainingSeconds = minutes * 60;
    }

    private void Update()
    {
        //contador
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
            if (NetworkManager.Singleton.ConnectedClients.Count < 2 && partidalista)
            {
                ShowErrorPanelClientRpc();
            }

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
            //contador
            if (timerRunning && gameMode == GameMode.Tiempo)
            {
                timeRemaining -= Time.deltaTime;

                if (timeRemaining < 0f) timeRemaining = 0f;

                syncedTime.Value = timeRemaining; // esto sincroniza a los clientes
                //Debug.Log("[GameManager] Temporizador corriendo: " + timeRemaining);

                if (timeRemaining <= 0f)
                {
                    timerRunning = false;
                    Debug.Log("[GameManager] Tiempo agotado, ganan los humanos!");
                    // --- MODIFICACIÓN: Llamamos a un nuevo RPC para manejar el fin de juego por tiempo
                    EndGameTimerClientRpc("Human", "Tiempo");
                }
            }
            // Comprobamos si quedan humanos vivos
            int humanosVivos = 0;

            foreach (var kvp in playerRoles)
            {
                if (!kvp.Value) // false significa que es humano
                {
                    humanosVivos++;
                }
            }

            if (humanosVivos == 0 && partidalista && NetworkManager.Singleton.ConnectedClients.Count > 1)
            {
                Debug.Log("[GameManager] No quedan humanos vivos, fin de partida. Ganan los zombis.");
                // --- MODIFICACIÓN: Llamamos a un nuevo RPC para manejar el fin de juego por zombies
                EndGameZombieWinClientRpc("Zombie", "DaIgual");
            }

        }
        var coinManager = GetCoinManagerInstance();
        if (coinManager != null && coinManager.globalCoins.Value >= 10)
        {
            // --- MODIFICACIÓN: Llamamos a un nuevo RPC para manejar el fin de juego por monedas
            EndGameCoinsClientRpc("Human", "Monedas");
        }
        //contador
        // El cliente solo actualiza su propia UI, no la lógica de fin de juego
        if (!IsServer && timerRunning && gameMode == GameMode.Tiempo)
        {
            timeRemaining = syncedTime.Value; // El cliente toma el tiempo sincronizado del servidor
            UIManager.Instance?.UpdateTimerDisplay(timeRemaining);
        }
    }
    //contador
    public NetworkVariable<float> GetSyncedTime()
    {
        return syncedTime;
    }

    // --- Función original EndGame, ahora solo llamada por los ClientRpc
    public void EndGame(string winnerTeam, string gameMode, bool localPlayerIsZombie)
    {
        string resultMessage = "";

        if (winnerTeam == "Human" && gameMode == "Monedas")
        {
            resultMessage = localPlayerIsZombie
                ? "Has perdido... los humanos han recolectado todas las monedas."
                : "¡Ganaste! Los humanos habéis recolectado todas las monedas.";
        }
        else if (winnerTeam == "Human" && gameMode == "Tiempo")
        {
            resultMessage = localPlayerIsZombie
                ? "Has perdido... se te ha acabado el tiempo y los humanos han sobrevivido."
                : "¡Ganaste! Los humanos habéis sobrevivido.";
        }
        else if (winnerTeam == "Zombie" && gameMode == "DaIgual")
        {
            resultMessage = localPlayerIsZombie
                ? "¡Ganaste! Los zombis habéis atrapado a todos los humanos."
                : "Has perdido... los zombis os han atrapado a todos.";
        }

        endGamePanel.ShowEndGamePanel(resultMessage);
    }

    // Estos RPCs finales ahora obtendrán el rol local del diccionario playerRoles,
    // que se ha actualizado en todos los clientes gracias a UpdatePlayerRoleClientRpc.
    [ClientRpc]
    public void EndGameTimerClientRpc(string winnerTeam, string gameMode)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool localPlayerIsZombie = playerRoles.ContainsKey(localId) && playerRoles[localId];
        Debug.Log($"[GameManager Client] Cliente {localId}: WinnerTeam = {winnerTeam}, GameMode = {gameMode}, Mi rol (playerRoles) = {localPlayerIsZombie}"); // Mantén este log para verificar
        EndGame(winnerTeam, gameMode, localPlayerIsZombie);
    }

    [ClientRpc]
    public void EndGameCoinsClientRpc(string winnerTeam, string gameMode)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool localPlayerIsZombie = playerRoles.ContainsKey(localId) && playerRoles[localId];
        Debug.Log($"[GameManager Client] Cliente {localId}: WinnerTeam = {winnerTeam}, GameMode = {gameMode}, Mi rol (playerRoles) = {localPlayerIsZombie}"); // Mantén este log para verificar
        EndGame(winnerTeam, gameMode, localPlayerIsZombie);
    }


    [ClientRpc]
    public void EndGameZombieWinClientRpc(string winnerTeam, string gameMode)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool localPlayerIsZombie = playerRoles.ContainsKey(localId) && playerRoles[localId];
        Debug.Log($"[GameManager Client] Cliente {localId}: WinnerTeam = {winnerTeam}, GameMode = {gameMode}, Mi rol (playerRoles) = {localPlayerIsZombie}"); // Mantén este log para verificar
        EndGame(winnerTeam, gameMode, localPlayerIsZombie);
    }

    // --- RPC NUEVO E IMPORTANTE para actualizar el diccionario playerRoles en todos los clientes ---
    // Este es el RPC CRÍTICO para asegurar la sincronización del rol en TODOS los clientes.
    [ClientRpc]
    public void UpdatePlayerRoleClientRpc(ulong clientId, bool isZombie)
    {
        Debug.Log($"[GameManager ClientRpc] Actualizando rol para el cliente {clientId}: isZombie = {isZombie}");
        playerRoles[clientId] = isZombie; // Esto actualiza el diccionario estático en TODOS los clientes
        UIManager.Instance?.ForceRefreshCounts(); // Si tienes UI que muestre el conteo de roles, fuerza su actualización
    }


    [ClientRpc]
    private void ShowErrorPanelClientRpc()
    {
        if (ErrorPanel != null)
        {
            ErrorPanel.SetActive(true);
        }
    }


    [ClientRpc]
    public void DesactivarUIClientRpc()
    {
        botonesActivos = false;
    }
    private CoinManager GetCoinManagerInstance()
    {
        if (CoinManager.instance != null)
            return CoinManager.instance;

        return FindObjectOfType<CoinManager>();
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
            playerRoles.Clear();
            seed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log("[GameManager] OnNetworkSpawn() en servidor.");
            if (levelBuilder == null)
            {
                Debug.LogError("LevelBuilder no está asignado.");
                return;
            }

            levelBuilder.Build(seed);
            //humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            //zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();

            InformClientsToBuildLevelClientRpc(seed);

            if (coinManagerPrefab != null)
            {
                GameObject coinObj = Instantiate(coinManagerPrefab);
                NetworkObject netObj = coinObj.GetComponent<NetworkObject>();
                netObj.Spawn();

                Debug.Log($"[GameManager] CoinManager instanciado y spawneado: {netObj.NetworkObjectId}");
            }

            ModeSelect.SetActive(true);
            /*
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            */
        }
        else
        {
            Debug.Log("[GameManager] OnNetworkSpawn() en cliente.");
            ModeSelect.SetActive(false);
            RequestBuildLevelServerRpc();
        }
        ConfigureLobbyUI(); // Configura la UI del lobby al spawnear
        readyPlayerCount.OnValueChanged += OnReadyCountChanged; // Nos suscribimos a los cambios

        // Si somos el host, también necesitamos actualizar la UI cuando un jugador se conecta o desconecta
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => UpdateHostLobbyUI();
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => {
                // Si un jugador se desconecta, recalculamos los listos. 
                // (Una lógica más avanzada podría descontar si el que se fue estaba listo)
                // Por ahora, solo actualizamos el total.
                UpdateHostLobbyUI();
            };
        }

    }

    // IMPORTANTE: Asegúrate de desuscribirte de los eventos en OnNetworkDespawn
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null) // Asegúrate de que el Singleton no sea nulo al desuscribirte
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }
        base.OnNetworkDespawn();
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
        // Ya no spawneamos directamente aquí, ahora llamamos a una corrutina que lo hará.
        StartCoroutine(SpawnPlayerWithDelay(clientId));
    }
    private IEnumerator SpawnPlayerWithDelay(ulong clientId)
    {
        // ESPERA UN SEGUNDO. Esto le da tiempo de sobra al LevelBuilder del cliente
        // para recibir el RPC del servidor y construir el nivel antes de que aparezca el jugador.
        yield return new WaitForSeconds(1.0f);

        Debug.Log($"[GameManager] Cliente conectado y nivel listo: {clientId}");

        bool isHuman = AsignarRol(clientId);
        Vector3 spawnPosition = ObtenerPuntoDeSpawn(isHuman); // Esto usará tu lógica de spawn ordenado
        GameObject prefab = isHuman ? playerPrefab : zombiePrefab;

        GameObject instancia = Instantiate(prefab, spawnPosition, Quaternion.identity);

        NetworkObject netObj = instancia.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);

        // El resto de la lógica de asignación de rol se queda igual
        PlayerController pc = instancia.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.IsZombieNetVar.Value = !isHuman;
            UpdatePlayerRoleClientRpc(clientId, !isHuman);
            Debug.Log($"[GameManager] Jugador {clientId} {(isHuman ? "Humano" : "Zombi")} instanciado en {spawnPosition}.");
        }
        else
        {
            Debug.LogError($"[GameManager] El prefab para el cliente {clientId} no tiene un PlayerController.");
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Cliente desconectado: {clientId}");

        // Elimina al jugador del diccionario de roles
        if (playerRoles.ContainsKey(clientId))
        {
            playerRoles.Remove(clientId);
            Debug.Log($"[GameManager] Rol eliminado para cliente {clientId}");
        }
        // ¡Importante! Llama a este ClientRpc para que TODOS los clientes
        // también eliminen al jugador de su diccionario playerRoles.
        RemovePlayerRoleClientRpc(clientId);

        UIManager.Instance?.ForceRefreshCounts();
    }

    // Añade este nuevo ClientRpc para eliminar un rol en todos los clientes cuando un jugador se desconecta
    [ClientRpc]
    public void RemovePlayerRoleClientRpc(ulong clientId)
    {
        if (playerRoles.ContainsKey(clientId))
        {
            playerRoles.Remove(clientId);
            Debug.Log($"[GameManager ClientRpc] Rol eliminado para cliente {clientId} en el cliente.");
        }
        UIManager.Instance?.ForceRefreshCounts(); // Asegúrate de refrescar la UI
    }

    public void AvisarServerJugadorListo_out()
    {
        // 1. Lógica del nombre (se queda igual)
        string chosenName = nameInputField.text;
        var localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        if (localPlayerController != null)
        {
            localPlayerController.SetPlayerNameServerRpc(chosenName);
        }

        // 2. Avisamos al servidor que estamos listos
        PlayerClickedReadyServerRpc();

        // 3. Desactivamos el botón para no poder pulsarlo más
        readyButton.interactable = false;
    }

    // EN GameManager.cs
    [ServerRpc(RequireOwnership = false)]
    private void PlayerClickedReadyServerRpc()
    {
        // El servidor simplemente incrementa el contador.
        readyPlayerCount.Value++;
    }

    public bool AsignarRol(ulong clientId)
    {
        int totalZombies = 0;
        int totalHumanos = 0;

        // Contamos los roles actuales. Esta parte estaba bien.
        foreach (var rol in playerRoles.Values)
        {
            if (rol) totalZombies++; // true es zombi
            else totalHumanos++;    // false es humano
        }

        // Determinar el rol preferido para mantener el equilibrio
        // Si Z <= H, queremos un Zombi para igualar. Si no (Z > H), queremos un Humano.
        bool preferirHumano = (totalZombies > totalHumanos);

        // --- Lógica de Asignación Simplificada ---

        if (preferirHumano)
        {
            // Se prefieren humanos porque ya hay más zombis.
            if (totalHumanos < maxHumans)
            {
                playerRoles[clientId] = false; // Asignar Humano
                Debug.Log($"Jugador {clientId} asignado como HUMANO (Regla: Z > H). Equipos: {totalHumanos + 1}H/{totalZombies}Z");
                return true; // isHuman = true
            }
        }
        else // Se prefieren zombis (porque Zombis <= Humanos)
        {
            if (totalZombies < maxZombies)
            {
                playerRoles[clientId] = true; // Asignar Zombi
                Debug.Log($"Jugador {clientId} asignado como ZOMBI (Regla: Z <= H). Equipos: {totalHumanos}H/{totalZombies + 1}Z");
                return false; // isHuman = false
            }
        }

        // --- Fallback: El equipo preferido estaba lleno, intentamos con el otro ---
        Debug.LogWarning("El equipo preferido estaba lleno. Intentando asignar al equipo alternativo.");

        if (totalHumanos < maxHumans) // ¿Hay hueco para un humano como segunda opción?
        {
            playerRoles[clientId] = false;
            Debug.Log($"Jugador {clientId} asignado como HUMANO (Fallback). Equipos: {totalHumanos + 1}H/{totalZombies}Z");
            return true;
        }
        else if (totalZombies < maxZombies) // ¿Hay hueco para un zombi como segunda opción?
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI (Fallback). Equipos: {totalHumanos}H/{totalZombies + 1}Z");
            return false;
        }

        // Si llegamos aquí, es que el juego está completamente lleno.
        Debug.LogError($"No se pudo asignar rol para {clientId}. El juego está lleno. Desconectando jugador (o manejar de otra forma).");
        // Aquí podrías desconectar al cliente porque no hay sitio. Por ahora, lo asignamos como humano.
        playerRoles[clientId] = false;
        return true;
    }

    private Vector3 ObtenerPuntoDeSpawn(bool isHuman)
    {
        if (isHuman)
        {
            int numHumanos = 0;
            foreach (var rol in playerRoles.Values) { if (!rol) numHumanos++; }
            int spawnIndex = numHumanos - 1;

            if (spawnIndex >= 0 && spawnIndex < humanSpawnPoints.Count)
            {
                // Devolvemos la posición del Transform en la lista
                return humanSpawnPoints[spawnIndex].position;
            }
            // Fallback por si algo sale mal
            return new Vector3(13, 1, 13);
        }
        else // Es Zombi
        {
            int numZombis = 0;
            foreach (var rol in playerRoles.Values) { if (rol) numZombis++; }
            int spawnIndex = numZombis - 1;

            if (spawnIndex >= 0 && spawnIndex < zombieSpawnPoints.Count)
            {
                // Devolvemos la posición del Transform en la lista
                return zombieSpawnPoints[spawnIndex].position;
            }
            // Fallback por si algo sale mal
            return new Vector3(4, 1, 13);
        }
    }

    // EN GameManager.cs
    private void ConfigureLobbyUI()
    {
        if (IsHost)
        {
            readyButton.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
            readyCountText.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(TellServerToStartGame);
            UpdateHostLobbyUI(); // Actualizamos la UI del host por primera vez
        }
        else // Es un cliente
        {
            readyButton.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
            readyCountText.gameObject.SetActive(false);
        }
    }
    // EN GameManager.cs
    private void UpdateHostLobbyUI()
    {
        if (!IsHost) return;

        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        readyCountText.text = $"Listos: {readyPlayerCount.Value} / {totalPlayers}";

        // El host puede empezar si hay al menos 2 jugadores listos y todos los que están conectados están listos.
        bool todosLosConectadosEstanListos = (readyPlayerCount.Value == totalPlayers);
        startGameButton.interactable = (readyPlayerCount.Value >= 2 && todosLosConectadosEstanListos);
    }

    private void OnReadyCountChanged(int previousValue, int newValue)
    {
        // El host actualiza su UI cuando el contador cambia
        if (IsHost)
        {
            UpdateHostLobbyUI();
        }
    }

    private void TellServerToStartGame()
    {
        // 1. Validar y obtener el nombre del Host desde el InputField.
        string hostName = "Host Anónimo"; // Nombre por defecto por si está vacío.
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            hostName = nameInputField.text;
        }

        // 2. Obtener el PlayerController del Host (que es el jugador local en esta máquina).
        var localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        // 3. Llamar al RPC para que el servidor actualice el nombre del Host.
        if (localPlayerController != null)
        {
            localPlayerController.SetPlayerNameServerRpc(hostName);
        }

        // Este método es llamado por el botón del Host
        TellServerToStartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TellServerToStartGameServerRpc()
    {
        // El servidor recibe la orden y avisa a todos los clientes para que empiecen
        partidalista = true;
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // Todos los clientes (incluido el host) reciben esta llamada
        Debug.Log("¡La partida comienza!");
        StartPanel.SetActive(false); // Ocultamos el panel de inicio
                                     // Aquí puedes añadir más lógica de inicio de partida si es necesario
    }
}