using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour, IDanable
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

    [Header("Evitar obstaculos")]
    public float distanciaSensor = 1.8f;
    private RaycastHit2D[] sensorHits = new RaycastHit2D[8];
    private float radio = 0.5f;

    [Header("Daño por contacto")]
    public float danoContacto = 1f;
    public float intervaloDano = 1f;
    private float tiempoUltimoDano = -999f;

    private Transform jugador;
    private float tiempoSiguienteDisparo = 0f;

    void Start()
    {
        // Igual que Enemy: inicializar vida y buscar jugador
        vidaActual = vidaMaxima;

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            jugador = obj.transform;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) radio = col.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        if (distancia <= rangoDeteccion)
        {
            Vector2 deseada = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
            Vector2 mover = Navegacion.DireccionEvitando(transform.position, deseada, gameObject, radio, distanciaSensor, sensorHits);
            transform.position += (Vector3)(mover * velocidad * Time.deltaTime);

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

    // --- Daño por contacto al jugador (con cooldown) ---
    private void OnCollisionEnter2D(Collision2D collision) { IntentarDanar(collision.gameObject); }
    private void OnCollisionStay2D(Collision2D collision)  { IntentarDanar(collision.gameObject); }

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
        GetComponent<Botin>()?.Soltar();   // posibilidad de soltar monedas
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}