using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camaraPrincipal;

    void Start()
    {
        // Encontramos la cámara principal al iniciar
        if (Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }
    }

    // Usamos LateUpdate para asegurarnos de que se ejecuta después de que la cámara se haya movido
    void LateUpdate()
    {
        if (camaraPrincipal == null) return;

        // Hacemos que este objeto (el texto) mire en la misma dirección que la cámara.
        // Esto evita que el texto se incline hacia arriba o abajo si la cámara lo hace.
        transform.forward = camaraPrincipal.forward;
    }
}
