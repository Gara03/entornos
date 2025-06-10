using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Cinemachine;
using Unity.Collections;


public class PlayerController : NetworkBehaviour
{
    [SerializeField] Rigidbody rigidBody;
    private TextMeshProUGUI coinText;

    [Header("Cinemachine")]
    public GameObject virtualCameraObject;
    public Transform mainCameraTransform;

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false; // <-- Esto debe ser una NetworkVariable
    public string uniqueID; // Esto probablemente no es necesario si usas ClientId

    [Header("Movement Settings")]
    Vector2 _input;
    private NetworkVariable<float> netSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float _rotSpeed = 720f;
    public float zombieSpeedModifier = 0.8f;

    public Animator animator;

    private float horizontalInput;
    private float verticalInput;

    [SerializeField] private TextMeshPro nameText3D;
    private NetworkVariable<FixedString32Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Server);

    // --- NUEVO: NetworkVariable para isZombie ---
    // Esto asegura que el estado de 'isZombie' se sincroniza automáticamente entre el servidor y todos los clientes.
    public NetworkVariable<bool> IsZombieNetVar = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody>();

        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }
    void Start()
    {
        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                Transform coinTextTransform = panel.Find("CoinsValue");
                if (coinTextTransform != null)
                    coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }
        UpdateCoinUI();
    }

    public override void OnNetworkSpawn()
    {
        if (virtualCameraObject != null)
        {
            virtualCameraObject.SetActive(IsOwner);
        }

        if (IsServer)
        {
            // Inicializa la NetworkVariable con el rol asignado en GameManager
            if (GameManager.playerRoles.ContainsKey(OwnerClientId))
            {
                IsZombieNetVar.Value = GameManager.playerRoles[OwnerClientId];
                isZombie = IsZombieNetVar.Value; // Inicializa la variable local para el servidor
            }
            //playerName.Value = new FixedString32Bytes(GetNameByClientId(OwnerClientId));
        }
        else
        {
            // Para clientes, inicializa la variable local con el valor actual de la NetworkVariable
            isZombie = IsZombieNetVar.Value;
        }

        // Suscribirse al cambio de la NetworkVariable para actualizar el estado local
        IsZombieNetVar.OnValueChanged += OnIsZombieChanged;

        playerName.OnValueChanged += OnNameChanged;
        OnNameChanged(new FixedString32Bytes(""), playerName.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        IsZombieNetVar.OnValueChanged -= OnIsZombieChanged;
        playerName.OnValueChanged -= OnNameChanged;
    }

    // --- NUEVO: Callback para cuando la NetworkVariable isZombie cambie ---
    private void OnIsZombieChanged(bool oldIsZombie, bool newIsZombie)
    {
        isZombie = newIsZombie; // Actualiza la variable local 'isZombie'
        Debug.Log($"[PlayerController {OwnerClientId}] Rol actualizado via NetworkVariable: oldIsZombie={oldIsZombie}, newIsZombie={newIsZombie}.");
    }

    // --- NUEVO RPC para que el servidor actualice el rol del cliente (llamado desde GameManager) ---
    [ClientRpc]
    public void SetIsZombieClientRpc(bool newIsZombie)
    {
        // Esto solo se llama en el cliente propietario del PlayerController
        // y el servidor ya habrá actualizado IsZombieNetVar.
        // Lo que necesitamos es que el GameManager actualice su diccionario estático en TODOS los clientes.
        // La actualización de IsZombieNetVar ya hace que OnIsZombieChanged se llame en todos.
        // Lo que necesitamos es que el GameManager también actualice su diccionario estático en todos los clientes.
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            // Esto solo lo hace el servidor para su propio `playerRoles` y lo propaga a los clientes a través del RPC del GameManager.
            // O podemos hacer que cada PlayerController le pida al GameManager que actualice su rol en el diccionario estático.
            // La mejor forma es que GameManager tenga un RPC para actualizar su diccionario estático.
        }
    }

    private void OnNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        if (nameText3D != null)
            nameText3D.text = newName.ToString();
    }


    void Update()
    {
        if (!IsOwner)
        {
            animator.SetFloat("Speed", netSpeed.Value);
            // El estado de isZombie ya está sincronizado por IsZombieNetVar
            // y se maneja en OnIsZombieChanged para cualquier lógica de apariencia.
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        horizontalInput = _input.x;
        verticalInput = _input.y;

        // Usamos el valor sincronizado de IsZombieNetVar
        float currentSpeed = IsZombieNetVar.Value ? zombieSpeedModifier * moveSpeed : moveSpeed;

        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 worldMove = transform.TransformDirection(moveDirection) * currentSpeed * Time.fixedDeltaTime;

            Vector3 targetPosition = rigidBody.position + worldMove;
            rigidBody.MovePosition(targetPosition);

            Quaternion targetRotation = Quaternion.LookRotation(worldMove);
            Quaternion newRotation = Quaternion.RotateTowards(rigidBody.rotation, targetRotation, _rotSpeed * Time.fixedDeltaTime);
            rigidBody.MoveRotation(newRotation);
        }

        HandleAnimations();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    void HandleAnimations()
    {
        float speedValue = Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput);
        netSpeed.Value = speedValue;

        animator.SetFloat("Speed", speedValue);
    }

    public void CoinCollected()
    {
        if (!IsZombieNetVar.Value) // Usar el valor sincronizado
        {
            CoinsCollected++;
            UpdateCoinUI();
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = $"{CoinsCollected}";
    }

    // --- NUEVO: Método para cambiar el rol del jugador (llamado desde la lógica de colisión en el servidor) ---
    [ServerRpc(RequireOwnership = false)]
    public void ChangeToZombieServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        // Aseguramos que el rol se actualiza en el diccionario estático del GameManager en el servidor
        if (GameManager.playerRoles.ContainsKey(clientId))
        {
            GameManager.playerRoles[clientId] = true; // true = zombie
            Debug.Log($"[Server] Cliente {clientId} se ha convertido en ZOMBI en GameManager.playerRoles.");
        }
        else
        {
            Debug.LogWarning($"[Server] Cliente {clientId} no encontrado en GameManager.playerRoles al intentar convertir a zombie.");
            return;
        }

        // Actualizamos la NetworkVariable en este PlayerController para que se sincronice con todos los clientes
        IsZombieNetVar.Value = true;
        isZombie = true; // Actualiza la variable local del servidor

        // Informar al GameManager para que actualice su diccionario estático en TODOS los clientes
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.UpdatePlayerRoleClientRpc(clientId, true); // Enviar RPC para actualizar diccionario estático en clientes
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Solo el servidor procesa las colisiones para la conversión

        PlayerController otherPlayer = other.GetComponent<PlayerController>();
        if (otherPlayer != null && otherPlayer.IsOwner && this.IsZombieNetVar.Value && !otherPlayer.IsZombieNetVar.Value)
        {
            // Si yo soy un zombie (this) y el otro es un humano (otherPlayer)
            Debug.Log($"[Server] Zombie {this.OwnerClientId} tocó a Humano {otherPlayer.OwnerClientId}. Convirtiendo...");
            otherPlayer.ChangeToZombieServerRpc(otherPlayer.OwnerClientId);
        }
    }

    // Dentro de PlayerController.cs

    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        // El servidor recibe el nombre y actualiza la NetworkVariable
        playerName.Value = new FixedString32Bytes(name);
    }

    // Este método nos permite LEER el nombre actual de un jugador.
    public FixedString32Bytes GetCurrentName()
    {
        return playerName.Value;
    }

    // Este método nos permite ASIGNAR un nombre, pero solo si se ejecuta en el servidor.
    // Es perfecto para cuando el servidor crea al nuevo zombi.
    public void InitializeNameOnServer(FixedString32Bytes newName)
    {
        if (!IsServer) return; // Medida de seguridad
        playerName.Value = newName;
    }
}