using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ErrorPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [SerializeField] private Button botonMenu;
    [SerializeField] private Button botonSalir;

    private void Start()
    {

        if (botonMenu != null)
        {
            botonMenu.onClick.AddListener(IrAlMenu);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n en el inspector.");
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.AddListener(CerrarJuego);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n de salir en el inspector.");
        }
    }
    public void IrAlMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
    public void CerrarJuego()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();

        // En el editor de Unity, esto no funciona, as� que para testear puedes hacer:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
