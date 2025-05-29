using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Cinemachine;

//AYUDA: unique.Id no sirve de nada porque el network te lo da hecho, lo primero que seria es eliminar el botom de host que solo da amarguras, crear nodo servidor, y que todo los clienetes se conecten
// luego puedes poner el boton de host otra vez y ya funcionaria bien, ghestionar conexiones y reconexiones sin cerrar servidor, 


public class PlayerController : NetworkBehaviour  
{
    [SerializeField] CharacterController controller;

    private TextMeshProUGUI coinText;

    
    [Header("Cinemachine")]
    public GameObject virtualCameraObject; // referencia al GameObject que tiene la virtual camera

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false; // Añadir una propiedad para el estado del jugador
    public string uniqueID; // Añadir una propiedad para el identificador único

    [Header("Movement Settings")]
    Vector2 _input;                         //cuando digas que va para delante va para delante sin tener que cambiar el codigo, x rotacion y translacion
    
    //el serializefield es para que se mantenga privado pero se oueda modificar desde el inspector, pero no se puede acceder desde otros scripts
    [SerializeField] float moveSpeed = 1f;           // Velocidad de movimiento
    [SerializeField] float _rotSpeed = 270f;           //velocidad rot
    public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
    public Animator animator;              // Referencia al Animator

    Transform _playerTransform;             //para sacar el transform del player

    private float horizontalInput;         // Entrada horizontal (A/D o flechas)
    private float verticalInput;           // Entrada vertical (W/S o flechas)

    private void Awake()
    {
        _playerTransform = transform;       //para acceder al transform del gameobject facilmente, nos devuleve la transformada del objeto
    }

    void Start()
    {
        

        Debug.Log($"[{OwnerClientId}] IsOwner: {IsOwner} - Activando cámara: {virtualCameraObject?.name}");

        if (IsOwner && virtualCameraObject != null)
        {
            virtualCameraObject.SetActive(true);
        }


        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");

        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar el TextMeshProUGUI llamado "CoinsValue" dentro del Panel
                Transform coinTextTransform = panel.Find("CoinsValue");
                if (coinTextTransform != null)
                {
                    coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        UpdateCoinUI();
    }

    void FixedUpdate()  //cuando utilizamos fisicas, fixed update pq quiere que las interpolaciones se hagan en tiempos constantes y da menos errores
    {

        if (!IsOwner) return;

        //zombie movimiento
        float currentSpeed = moveSpeed;
        if (isZombie)
        {
            currentSpeed = zombieSpeedModifier * moveSpeed;
        }


        _playerTransform.Translate(Vector3.forward * (_input.y * currentSpeed * Time.fixedDeltaTime));     //movernos hacia adelante //ESTA ABAJO MODIFICAR
        _playerTransform.Rotate(Vector3.up * (_input.x * _rotSpeed * Time.fixedDeltaTime));                //rotamos sobre y

 

        // Leer entrada del teclado
        //horizontalInput = Input.GetAxis("Horizontal");
        //verticalInput = Input.GetAxis("Vertical");

        // Mover el jugador
        //MovePlayer();
        //PlayerMove();

        // Manejar las animaciones del jugador
        HandleAnimations();
    }

    public void OnMove(InputAction.CallbackContext contex)
    {
        _input = contex.ReadValue<Vector2>();       //cuando input se ejecute fixed update esta esperando un imput para moverse

        //zombie
        horizontalInput = _input.x;
        verticalInput = _input.y;
    }

    /*void PlayerMove()
    {
        if (cameraTransform == null) { return; }

        // Calcular la dirección de movimiento en relación a la cámara
        Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
        moveDirection.y = 0f; // Asegurarnos de que el movimiento es horizontal (sin componente Y)

        _playerTransform.Translate(Vector3.forward * (_input.y * moveSpeed * Time.fixedDeltaTime));     //movernos hacia adelante //ESTA ABAJO MODIFICAR
        _playerTransform.Translate(Vector3.left * (_input.y * moveSpeed * Time.fixedDeltaTime));


        //_playerTransform.Rotate(Vector3.up * (_input.x * _rotSpeed * Time.fixedDeltaTime));             //rotamos sobre y


    }*/
    /*void MovePlayer()
    {
        if (cameraTransform == null) { return; }

        // Calcular la dirección de movimiento en relación a la cámara
        Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
        moveDirection.y = 0f; // Asegurarnos de que el movimiento es horizontal (sin componente Y)

        // Mover el jugador usando el Transform
        if (moveDirection != Vector3.zero)
        {
            // Calcular la rotación en Y basada en la dirección del movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);

            // Ajustar la velocidad si es zombie
            float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;

            // Mover al jugador en la dirección deseada
            transform.Translate(moveDirection * adjustedSpeed * Time.deltaTime, Space.World);
        }
    }*/

    void HandleAnimations()
    {
        // Animaciones basadas en la dirección del movimiento
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));  // Controla el movimiento (caminar/correr)
    }

    public void CoinCollected()
    {
        if (!isZombie) // Solo los humanos pueden recoger monedas
        {
            this.CoinsCollected++;
            UpdateCoinUI();
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = $"{CoinsCollected}";
        }
    }
}

