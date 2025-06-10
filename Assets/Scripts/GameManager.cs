using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Collections; // Necesario para FixedString
using System.Runtime.CompilerServices;

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

    // --- CAMBIO IMPORTANTE: Usaremos NetworkDictionary para playerRoles si es posible,
    // --- o una NetworkList y la actualizaremos con RPCs para asegurar la sincronización.
    // --- Para mantener la estructura actual, vamos a hacer que el servidor envíe el rol.
    // --- Por ahora, mantenemos el static Dictionary, pero su uso directo en clientes
    // --- para determinar el rol local es el problema.

    public static Dictionary<ulong, bool> playerRoles = new Dictionary<ulong, bool>(); // true = zombie, false = humano

    [SerializeField] private EndGamePanelController endGamePanel;
    [SerializeField] private GameObject ErrorPanel;
    [SerializeField] GameObject StartPanel;

    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button readyButton;

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
        if (jugador1ready && jugador2ready && jugador3ready && jugador4ready)
        {
            partidalista = true;
            partidalistaClientRpc();
        }

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
    public void FinHumanosClientRpc()
    {
        SceneManager.LoadScene("HumansWin");
    }
    [ClientRpc]
    public void FinZombiesClientRpc()
    {
        SceneManager.LoadScene("ZobiesWin");
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
            // El servidor actualiza el rol en el PlayerController del cliente
            pc.SetIsZombieClientRpc(!isHuman); // Asegúrate de que PlayerController tiene este RPC
            pc.isZombie = !isHuman; // Actualiza la variable local del servidor para consistencia

            // **** ¡¡¡LA LÍNEA CRÍTICA QUE FALTABA!!! ****
            // Llama al ClientRpc para que TODOS los clientes (incluido el zombi original)
            // actualicen su diccionario GameManager.playerRoles con el rol de este jugador.
            UpdatePlayerRoleClientRpc(clientId, !isHuman); // !isHuman porque isZombie es true si no es humano

            Debug.Log($"[GameManager] Jugador {clientId} {(isHuman ? "Humano" : "Zombi")} instanciado en {spawnPosition}. Rol sincronizado.");
        }
        else
        {
            Debug.LogError($"[GameManager] El prefab {(isHuman ? "Human" : "Zombie")} para el cliente {clientId} no tiene un PlayerController.");
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
        // 1. Validar y obtener el nombre del InputField
        string chosenName = "Jugador Anónimo";
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            chosenName = nameInputField.text;
        }

        // 2. Obtener el PlayerController del jugador LOCAL
        var localPlayerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        // 3. Llamar al RPC del PlayerController para que el servidor actualice su nombre
        if (localPlayerController != null)
        {
            localPlayerController.SetPlayerNameServerRpc(chosenName);
        }
        else
        {
            Debug.LogError("No se encontró el PlayerController del jugador local.");
            return;
        }

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        PlayerReadyServerRpc(clientId);
    }

    public bool AsignarRol(ulong clientId) // Public para que PlayerController lo pueda usar si se convierte
    {
        int totalZombies = 0;
        int totalHumanos = 0;

        foreach (var entry in playerRoles.Values)
        {
            if (entry) totalZombies++;
            else totalHumanos++;
        }

        // --- Lógica de asignación de roles. Asegúrate de que el rol se guarda correctamente en playerRoles.
        // --- Si un jugador ya está en playerRoles, significa que ya tiene un rol.
        // --- Esta lógica parece asignar el rol solo al conectarse.
        // --- Si un humano se convierte en zombie, playerRoles DEBE actualizarse en el servidor
        // --- y luego esa actualización debe propagarse a los clientes.

        bool assignedRole = false;
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            // El host es siempre zombie
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} (Host) asignado como ZOMBI");
            assignedRole = true;
            return false; // Retorna false si es zombie (isHuman = false)
        }
        else if (totalZombies == 0 && totalHumanos == 0) // Primer jugador (no host si host ya asignó)
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI (primer jugador)");
            assignedRole = true;
            return false;
        }
        else if (totalZombies < totalHumanos && totalZombies < maxZombies)
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI");
            assignedRole = true;
            return false;
        }
        else if (totalHumanos < maxHumans) // Priorizar humanos si hay espacio
        {
            playerRoles[clientId] = false;
            Debug.Log($"Jugador {clientId} asignado como HUMANO");
            assignedRole = true;
            return true;
        }
        else if (totalZombies < maxZombies) // Si no hay espacio para humanos, y hay espacio para zombies
        {
            playerRoles[clientId] = true;
            Debug.Log($"Jugador {clientId} asignado como ZOMBI");
            assignedRole = true;
            return false;
        }
        // Fallback: si no se pudo asignar por límite o lógica
        if (!assignedRole)
        {
            Debug.LogWarning("No se pudo asignar rol. Asignando como HUMANO por defecto (o ya en límite).");
            playerRoles[clientId] = false; // Asignar como humano por defecto si no hay otra opción
            return true;
        }
        return false; // Por defecto, si no se cumple ninguna condición previa (debería ser inalcanzable con la lógica de assignedRole)
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