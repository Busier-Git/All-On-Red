using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Vida del Enemigo")]
    /// <summary>Puntos de vida totales del enemigo utilizando float.</summary>
    public float vidaMaxima = 3f;
    private float vidaActual;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    /// <summary>
    /// Resta vida flotante al enemigo. Se destruye si llega a 0.
    /// </summary>
    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log("Enemigo recibió " + cantidad + " de daño. Vida restante: " + vidaActual);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        Debug.Log("Enemigo eliminado.");
        Destroy(gameObject);
    }
}
