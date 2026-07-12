using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe: se acerca lento al jugador y dispara "sprays" tipo aerosol: una rafaga de
/// balas en abanico, una tras otra con separacion entre cada disparo (no muy rapidas),
/// y una pausa entre cada spray. Necesita Rigidbody2D (Gravity 0) + Collider2D no
/// trigger y el tag "enemy". Va en una sala grande con obstaculos para esquivar.
/// </summary>
public class Jefe : MonoBehaviour, IDanable
{
    [Header("Vida")]
    public float vidaMaxima = 12f;
    private float vidaActual;

    [Header("Movimiento")]
    public float velocidad = 1f;
    private Rigidbody2D rb;
    private Transform jugador;

    [Header("Evitar obstaculos")]
    public float distanciaSensor = 2.5f;
    private RaycastHit2D[] sensorHits = new RaycastHit2D[8];
    private float radio = 1f;

    [Header("Spray de disparos (aerosol)")]
    public GameObject prefabProyectil;          // asigna EnemyBullet.prefab
    public float velocidadProyectil = 4.5f;     // no muy rapido para poder esquivar
    public int balasPorSpray = 7;               // cuantas balas tiene cada abanico
    public float aperturaGrados = 90f;          // que tan abierto es el abanico
    public float separacionEntreBalas = 0.12f;  // pausa entre cada bala del spray
    public float tiempoEntreSprays = 2.5f;      // pausa entre un spray y el siguiente

    [Header("Daño por contacto")]
    public float danoContacto = 1f;
    public float intervaloDano = 1f;
    private float tiempoUltimoDano = -999f;

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) radio = col.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        GameObject pj = GameObject.FindWithTag("Player");
        if (pj != null) jugador = pj.transform;

        StartCoroutine(RutinaDisparo());
    }

    void FixedUpdate()
    {
        if (jugador == null || rb == null) return;
        Vector2 deseada = ((Vector2)jugador.position - rb.position).normalized;
        Vector2 mover = Navegacion.DireccionEvitando(rb.position, deseada, gameObject, radio, distanciaSensor, sensorHits);
        rb.MovePosition(rb.position + mover * velocidad * Time.fixedDeltaTime);
    }

    IEnumerator RutinaDisparo()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreSprays);

            float anguloBase = AnguloHaciaJugador();
            for (int i = 0; i < balasPorSpray; i++)
            {
                float t = (balasPorSpray <= 1) ? 0.5f : (float)i / (balasPorSpray - 1);
                float ang = anguloBase + Mathf.Lerp(-aperturaGrados / 2f, aperturaGrados / 2f, t);
                DispararEnAngulo(ang);
                yield return new WaitForSeconds(separacionEntreBalas);
            }
        }
    }

    float AnguloHaciaJugador()
    {
        if (jugador == null) return -90f;
        Vector2 dir = (Vector2)jugador.position - (Vector2)transform.position;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    void DispararEnAngulo(float grados)
    {
        if (prefabProyectil == null) return;
        float r = grados * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));

        GameObject p = Instantiate(prefabProyectil, transform.position, Quaternion.identity);
        Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
        if (prb != null) prb.velocity = dir * velocidadProyectil;
    }

    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log("Jefe recibió " + cantidad + " de daño. Vida restante: " + vidaActual);
        if (vidaActual <= 0f)
        {
            GetComponent<Botin>()?.Soltar();
            Destroy(gameObject);
            // (Mas adelante: aqui puedes abrir una escotilla para pasar de piso)
        }
    }

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
}
