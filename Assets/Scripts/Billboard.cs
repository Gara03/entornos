using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camaraPrincipal;

    void Start()
    {
        // Encontramos la c�mara principal al iniciar
        if (Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }
    }

    // Usamos LateUpdate para asegurarnos de que se ejecuta despu�s de que la c�mara se haya movido
    void LateUpdate()
    {
        if (camaraPrincipal == null) return;

        // Hacemos que este objeto (el texto) mire en la misma direcci�n que la c�mara.
        // Esto evita que el texto se incline hacia arriba o abajo si la c�mara lo hace.
        transform.forward = camaraPrincipal.forward;
    }
}
