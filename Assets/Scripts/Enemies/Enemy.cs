using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float vidaMaxima = 3f;
    public float velocidad = 3f;
    public float rangoDeteccion = 8f;
    public float distanciaParada = 0.5f;

    private float vidaActual;
    private Transform jugador;
    private Rigidbody2D rb;

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        BuscarJugador();
    }

    void FixedUpdate()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        if (distancia <= rangoDeteccion && distancia > distanciaParada)
        {
            SeguirJugador();
        }
        else
        {
            Detenerse();
        }
    }

    void BuscarJugador()
    {
        GameObject objJugador = GameObject.FindWithTag("Player");
        if (objJugador != null)
            jugador = objJugador.transform;
    }

    void SeguirJugador()
    {
        Vector2 direccion = ((Vector2)jugador.position - rb.position).normalized;
        rb.MovePosition(rb.position + direccion * velocidad * Time.fixedDeltaTime);
    }

    void Detenerse()
    {
        rb.velocity = Vector2.zero;
    }

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
