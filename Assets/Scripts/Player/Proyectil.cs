using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [Header("Configuración")]
    /// <summary>Daño decimal que inflige a los enemigos.</summary>
    public float dano = 0.5f;
    /// <summary>Tiempo de vida máximo antes de desaparecer si no impacta nada.</summary>
    public float tiempoVida = 3f;

    [Header("Teledirigido (lo activa el Corazón Sagrado)")]
    public bool teledirigido = false;
    public float fuerzaGiro = 240f;      // grados/seg al curvarse hacia el enemigo

    private Rigidbody2D rb;
    private Transform objetivo;
    private float tiempoBusqueda;
    private Vector2 posAnterior;
    private bool muerto = false;   // evita procesar dos veces en el mismo frame

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        posAnterior = transform.position;
        // Se destruye solo tras unos segundos para no acumular basura en la escena
        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        if (!teledirigido || rb == null) return;

        // Busca al enemigo mas cercano cada 0.15 s (barato y suficiente)
        if (Time.time >= tiempoBusqueda)
        {
            tiempoBusqueda = Time.time + 0.15f;
            objetivo = Granada.BuscarEnemigoCercano(transform.position, 14f);
        }
        if (objetivo == null) return;

        Vector2 deseada = ((Vector2)objetivo.position - (Vector2)transform.position).normalized;
        Vector2 actual = rb.velocity.normalized;
        float rapidez = rb.velocity.magnitude;
        Vector2 nueva = Vector3.RotateTowards(actual, deseada, fuerzaGiro * Mathf.Deg2Rad * Time.deltaTime, 0f);
        rb.velocity = nueva.normalized * rapidez;
    }

    void FixedUpdate()
    {
        if (muerto) return;

        // Barrido anti-atravesado: revisa todo lo que hay entre donde estaba y donde esta.
        // Con esto la bala NUNCA cruza una pared, aunque vaya rapida.
        Vector2 actual = transform.position;
        Vector2 delta = actual - posAnterior;
        float dist = delta.magnitude;
        if (dist > 0.0001f)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(posAnterior, delta / dist, dist);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
                if (Procesar(h.collider)) return;
        }
        posAnterior = actual;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Procesar(collision);
    }

    /// <summary>Devuelve true si el proyectil se destruyo con este contacto.</summary>
    bool Procesar(Collider2D collision)
    {
        if (muerto) return true;
        // Ignoramos otros triggers (monedas, zonas de sala, otras balas) y al propio jugador
        if (collision == null || collision.isTrigger) return false;
        if (collision.CompareTag("Player")) return false;

        // Si lo que tocamos puede recibir daño (enemigos, jefe, obstaculos destructibles), se lo hacemos
        IDanable danable = collision.GetComponent<IDanable>();
        if (danable != null)
            danable.RecibirDano(dano);

        // Choco contra algo solido (enemigo, obstaculo o pared) -> el proyectil se destruye
        muerto = true;
        Destroy(gameObject);
        return true;
    }
}
