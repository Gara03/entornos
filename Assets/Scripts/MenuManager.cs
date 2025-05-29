using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene"); // Cambia "MainScene" por el nombre de tu escena principal

    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Salir en el editor
#else
            Application.Quit(); // Salir en una build
#endif
    }
}
