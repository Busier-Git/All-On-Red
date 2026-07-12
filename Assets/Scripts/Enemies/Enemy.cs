using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDanable
{
    public float vidaMaxima = 3f;
    public float velocidad = 3f;
    public float rangoDeteccion = 8f;
    public float distanciaParada = 0.5f;

    [Header("Evitar obstaculos")]
    public float distanciaSensor = 1.8f;       // que tan lejos mira para rodear
    private RaycastHit2D[] sensorHits = new RaycastHit2D[8];
    private float radio = 0.5f;

    [Header("Daño por contacto al jugador")]
    public float danoContacto = 1f;
    public float intervaloDano = 1f;   // segundos entre golpe y golpe (para no vaciar la vida de golpe)
    private float tiempoUltimoDano = -999f;

    private float vidaActual;
    private Transform jugador;
    private Rigidbody2D rb;

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) radio = col.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
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
        Vector2 deseada = ((Vector2)jugador.position - rb.position).normalized;
        Vector2 mover = Navegacion.DireccionEvitando(rb.position, deseada, gameObject, radio, distanciaSensor, sensorHits);
        rb.MovePosition(rb.position + mover * velocidad * Time.fixedDeltaTime);
    }

    void Detenerse()
    {
        rb.velocity = Vector2.zero;
    }

    // --- Daño por contacto: si el enemigo toca al jugador, le quita vida (con cooldown) ---
    private void OnCollisionEnter2D(Collision2D collision) { IntentarDanar(collision.gameObject); }
    private void OnCollisionStay2D(Collision2D collision)  { IntentarDanar(collision.gameObject); }
    private void OnTriggerEnter2D(Collider2D other)        { IntentarDanar(other.gameObject); }
    private void OnTriggerStay2D(Collider2D other)         { IntentarDanar(other.gameObject); }

    private void IntentarDanar(GameObject obj)
    {
        if (!obj.CompareTag("Player")) return;
        if (Time.time < tiempoUltimoDano + intervaloDano) return;

        Player jugadorScript = obj.GetComponent<Player>();
        if (jugadorScript != null)
        {
            jugadorScript.RecibirDano(Mathf.RoundToInt(danoContacto));
            tiempoUltimoDano = Time.time;
        }
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
        GetComponent<Botin>()?.Soltar();   // posibilidad de soltar monedas
        Destroy(gameObject);
    }
}
