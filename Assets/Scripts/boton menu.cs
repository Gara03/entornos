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
        // Asegúrate de que el botón está asignado y le añadimos un listener
        if (boton != null)
        {
            boton.onClick.AddListener(IrAlMenu);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el botón en el inspector.");
        }
    }

    public void IrAlMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
