using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Necesario para acceder a Button

public class BotonMenu : MonoBehaviour
{
    [SerializeField] private Button boton;

    private void Start()
    {
        // Aseg�rate de que el bot�n est� asignado y le a�adimos un listener
        if (boton != null)
        {
            boton.onClick.AddListener(IrAlMenu);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n en el inspector.");
        }
    }

    public void IrAlMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
