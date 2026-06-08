using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform objetivo;
    public float velocidadSuavizado = 5f; // 0 = instantáneo, más alto = más fluido

    void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 posObjetivo = new Vector3(
            objetivo.position.x,
            objetivo.position.y,
            transform.position.z  // mantener el Z original de la cámara
        );

        transform.position = Vector3.Lerp(
            transform.position,
            posObjetivo,
            velocidadSuavizado * Time.deltaTime
        );
    }
}
