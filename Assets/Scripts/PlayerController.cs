using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Cinemachine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] Rigidbody rigidBody;
    private TextMeshProUGUI coinText;

    [Header("Cinemachine")]
    public GameObject virtualCameraObject;
    public Transform mainCameraTransform; // Añade esta referencia a la cámara principal

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false;
    public string uniqueID;

    [Header("Movement Settings")]
    Vector2 _input;

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float _rotSpeed = 720f;   // Rotación más rápida para mejor sensación
    public float zombieSpeedModifier = 0.8f;

    public Animator animator;

    private float horizontalInput;
    private float verticalInput;

    private void Awake()
    {
        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody>();

        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;  // asignar cámara principal si no está seteada
    }

    void Start()
    {
        if (IsOwner && virtualCameraObject != null)
            virtualCameraObject.SetActive(true);

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

    void FixedUpdate()
    {
        if (!IsOwner) return;

        horizontalInput = _input.x;
        verticalInput = _input.y;

        float currentSpeed = isZombie ? zombieSpeedModifier * moveSpeed : moveSpeed;

        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            // Movimiento local a world
            Vector3 worldMove = transform.TransformDirection(moveDirection) * currentSpeed * Time.fixedDeltaTime;

            // Mover Rigidbody con MovePosition para evitar conflictos con física
            Vector3 targetPosition = rigidBody.position + worldMove;
            rigidBody.MovePosition(targetPosition);

            // Rotar hacia dirección movimiento usando MoveRotation para suavidad y sin vibrar
            Quaternion targetRotation = Quaternion.LookRotation(worldMove);
            Quaternion newRotation = Quaternion.RotateTowards(rigidBody.rotation, targetRotation, _rotSpeed * Time.fixedDeltaTime);
            rigidBody.MoveRotation(newRotation);
        }
        else
        {
            // No mover horizontal, solo dejar que gravedad haga efecto (si Rigidbody.useGravity = true)
            // No tocamos velocidad ni posición
        }

        HandleAnimations();
    }



    public void OnMove(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    void HandleAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
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
