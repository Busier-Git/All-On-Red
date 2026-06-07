using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour
{
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
        // Busca al jugador automáticamente
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
            // Seguir al jugador lentamente
            transform.position = Vector2.MoveTowards(
                transform.position,
                jugador.position,
                velocidad * Time.deltaTime
            );

            // Disparar al jugador
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

    // Visualizar el rango en el Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}