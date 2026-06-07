using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Sistema de Vida")]
    public int vidaMaxima = 5;
    private int vidaActual;

    [Header("UI Vidas")]
    public UIVida uiVidas;

    [Header("Sistema de Disparo")]
    public GameObject prefabProyectil;
    public float velocidadProyectil = 12f;
    public float cadenciaDisparo = 0.3f;
    private float tiempoSiguienteDisparo = 0f;

    void Start()
    {
        vidaActual = vidaMaxima;
        if (uiVidas != null)
            uiVidas.ActualizarVidas(vidaActual, vidaMaxima);
    }

    void Update()
    {
        if (Time.time >= tiempoSiguienteDisparo)
        {
            if (Input.GetKey(KeyCode.UpArrow)) Disparar(Vector2.up);
            else if (Input.GetKey(KeyCode.DownArrow)) Disparar(Vector2.down);
            else if (Input.GetKey(KeyCode.LeftArrow)) Disparar(Vector2.left);
            else if (Input.GetKey(KeyCode.RightArrow)) Disparar(Vector2.right);
        }
    }

    private void Disparar(Vector2 direccion)
    {
        if (prefabProyectil != null)
        {
            GameObject proyectil = Instantiate(prefabProyectil, transform.position, Quaternion.identity);
            Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
            if (rbProyectil != null)
                rbProyectil.velocity = direccion * velocidadProyectil;
            tiempoSiguienteDisparo = Time.time + cadenciaDisparo;
        }
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;
        if (uiVidas != null)
            uiVidas.ActualizarVidas(vidaActual, vidaMaxima);
        if (vidaActual <= 0)
            Morir();
    }

    private void Morir()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.MostrarMenuMuerte();
        gameObject.SetActive(false);
    }
}