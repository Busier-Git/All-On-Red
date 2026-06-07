using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class Player : MonoBehaviour
{
    [Header("Sistema de Vida")]
    public int vidaMaxima = 5;
    private int vidaActual;

    [Header("Sistema de Disparo")]
    public GameObject prefabProyectil;
    public float velocidadProyectil = 12f;
    public float cadenciaDisparo = 0.3f;
    private float tiempoSiguienteDisparo = 0f;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

   void Update()
    {
        // 2. DISPARO CONTINUO CON FLECHAS
        if (Time.time >= tiempoSiguienteDisparo)
        {
            // Cambiamos GetKeyDown por GetKey
            if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.RightArrow))
            {
                Disparar(new Vector2(1, 1).normalized);
            }
            
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                Disparar(Vector2.up);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                Disparar(Vector2.down);
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                Disparar(Vector2.left);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                Disparar(Vector2.right);
            }
        }
    }

    /// <summary>
    /// Instancia el cubo proyectil y le asigna velocidad física en la dirección de la flecha presionada.
    /// </summary>
    private void Disparar(Vector2 direccion)
    {
        if (prefabProyectil != null)
        {
            // Creamos el proyectil
            GameObject proyectil = Instantiate(prefabProyectil, transform.position, Quaternion.identity);
            
            // Accedemos a su Rigidbody2D para impulsarlo en la dirección indicada
            Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
            if (rbProyectil != null)
            {
                rbProyectil.velocity = direccion * velocidadProyectil;
            }

            tiempoSiguienteDisparo = Time.time + cadenciaDisparo;
        }
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log("¡Jugador recibió " + cantidad + " de daño! Vida restante: " + vidaActual);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        Debug.Log("El jugador ha sido eliminado. Reiniciando nivel...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}