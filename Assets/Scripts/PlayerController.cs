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
    public bool isZombie = false;
    public string uniqueID;

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
            // Convertir el nombre (string) a FixedString32Bytes antes de asignar
            playerName.Value = new FixedString32Bytes(GetNameByClientId(OwnerClientId));
        }

        // Escuchar cambios y actualizar visualmente el texto
        playerName.OnValueChanged += OnNameChanged;
        OnNameChanged(new FixedString32Bytes(""), playerName.Value); // Llama con FixedString también aquí
    }


    private string GetNameByClientId(ulong clientId)
    {
        return clientId switch
        {
            0 => "Alberto",
            1 => "Laura",
            2 => "Ángela",
            3 => "Gara",
            _ => $"Jugador{clientId}"
        };
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
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        horizontalInput = _input.x;
        verticalInput = _input.y;

        float currentSpeed = isZombie ? zombieSpeedModifier * moveSpeed : moveSpeed;

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
        if (!isZombie)
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
}