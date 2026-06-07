using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour
{
    [Header("Sistema de Vida")]
    public float vidaMaxima = 3f;
    private float vidaActual;

    [Header("Movimiento")]
    public float velocidad = 1.5f;

    [Header("Disparo")]
    public GameObject prefabProyectil;
    public float velocidadProyectil = 6f;
    public float cadenciaDisparo = 2f;

    [Header("Detección")]
    public float rangoDeteccion = 10f;

    private Transform jugador;
    private float tiempoSiguienteDisparo = 0f;

    void Start()
    {
        // Igual que Enemy: inicializar vida y buscar jugador
        vidaActual = vidaMaxima;

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            jugador = obj.transform;
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        if (distancia <= rangoDeteccion)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                jugador.position,
                velocidad * Time.deltaTime
            );

            if (Time.time >= tiempoSiguienteDisparo)
            {
                Disparar();
                tiempoSiguienteDisparo = Time.time + cadenciaDisparo;
            }
        }
    }

    private void Disparar()
    {
        if (prefabProyectil == null) return;

        Vector2 direccion = (jugador.position - transform.position).normalized;
        GameObject proyectil = Instantiate(prefabProyectil, transform.position, Quaternion.identity);

        Rigidbody2D rb = proyectil.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = direccion * velocidadProyectil;
    }

    /// <summary>
    /// Igual que Enemy.RecibirDano — resta vida y destruye si llega a 0.
    /// </summary>
    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log("Enemigo recibió " + cantidad + " de daño. Vida restante: " + vidaActual);

        if (vidaActual <= 0)
            Morir();
    }

    private void Morir()
    {
        Debug.Log("Enemigo eliminado.");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}