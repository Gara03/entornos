/*using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera mainCamera; // La cámara principal
    public KeyCode switchKey = KeyCode.C; // Tecla para alternar la vista
    public float transitionSpeed = 2f; // Velocidad de la transición
    public float topDownHeight = 20f; // Altura para la vista cenital
    public float topDownRotation = 90f; // Rotación para la vista cenital (mirando hacia abajo)

    private CameraController cameraController; // Referencia al script CameraController
    private Vector3 targetPosition; // Posición objetivo para la cámara
    private Quaternion targetRotation; // Rotación objetivo para la cámara
    private bool isTopDownView = false; // Estado actual de la vista
    private bool isTransitioning = false; // Indica si la cámara está en transición

    void Start()
    {
        // Obtiene el componente CameraController de la cámara principal
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<CameraController>();

            if (cameraController == null)
            {
                Debug.LogWarning("CameraController no encontrado en la cámara principal.");
            }
        }
        else
        {
            Debug.LogError("Referencia a la cámara principal no asignada.");
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        if (Input.GetKeyDown(switchKey) && !isTransitioning)
        {
            ToggleCameraView();
        }

        if (isTransitioning)
        {
            PerformTransition();
        }
    }

    private void ToggleCameraView()
    {
        isTopDownView = !isTopDownView;

        if (isTopDownView)
        {
            Debug.Log("Cambiando a cenital...");

            // Calcula la posición y rotación para la vista cenital
            targetPosition = mainCamera.transform.position + Vector3.up * topDownHeight;
            targetRotation = Quaternion.Euler(topDownRotation, mainCamera.transform.eulerAngles.y, 0);

            // Desactiva el script CameraController
            EnableCameraController(false);
        }
        else
        {
            Debug.Log("Volviendo a tercera persona...");

            // Calcula la posición y rotación para la vista de tercera persona
            targetPosition = mainCamera.transform.position - Vector3.up * topDownHeight;
            targetRotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);

            // Reactiva el script CameraController
            EnableCameraController(true);
        }

        // Inicia la transición
        isTransitioning = true;
    }

    private void PerformTransition()
    {
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            Time.deltaTime * transitionSpeed
        );

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * transitionSpeed
        );

        // Comprueba si la cámara llegó al objetivo
        if (Vector3.Distance(mainCamera.transform.position, targetPosition) < 0.1f)
        {
            isTransitioning = false; // Finaliza la transición
        }
    }

    // Activa o desactiva el script CameraController
    private void EnableCameraController(bool enable)
    {
        if (cameraController != null)
        {
            cameraController.enabled = enable;
        }
    }
}
*/