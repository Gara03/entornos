using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePrefab : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 50f; // Velocidad de rotación en grados por segundo

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
