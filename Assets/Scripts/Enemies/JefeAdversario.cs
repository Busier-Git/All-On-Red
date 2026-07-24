using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe del NIVEL 2, inspirado en "The Adversary" de Isaac:
/// - Persigue al jugador (esquivando obstaculos).
/// - Cada cierto tiempo dispara un RAYO tipo Brimstone (con aviso previo).
/// - Cada cierto tiempo se TELETRANSPORTA cerca del jugador (con parpadeo).
/// Necesita Rigidbody2D (Gravity 0) + Collider2D no trigger y el tag "enemy".
/// El GeneradorMapa lo crea por codigo si no le asignas un prefab.
/// </summary>
public class JefeAdversario : MonoBehaviour, IDanable
{
    [Header("Vida")]
    public float vidaMaxima = 30f;
    private float vidaActual;

    [Header("Movimiento (te sigue)")]
    public float velocidad = 1.8f;
    public float distanciaSensor = 2.5f;
    private RaycastHit2D[] sensorHits = new RaycastHit2D[8];
    private float radio = 1f;

    [Header("Rayo (cada cierto tiempo)")]
    public float tiempoEntreRayos = 3.5f;

    [Header("Teletransporte (cada cierto tiempo, cerca del jugador)")]
    public float tiempoEntreTeleports = 6.5f;
    public float distanciaTeleportMin = 3.0f;
    public float distanciaTeleportMax = 4.5f;

    [Header("Daño por contacto")]
    public float danoContacto = 1f;
    public float intervaloDano = 1f;
    private float tiempoUltimoDano = -999f;

    private Rigidbody2D rb;
    private Transform jugador;
    private SpriteRenderer sr;
    private bool ocupado = false;   // true mientras dispara el rayo o se teletransporta

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) radio = col.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        GameObject pj = GameObject.FindWithTag("Player");
        if (pj != null) jugador = pj.transform;

        StartCoroutine(RutinaRayo());
        StartCoroutine(RutinaTeleport());
    }

    void FixedUpdate()
    {
        if (jugador == null || rb == null || ocupado) return;
        Vector2 deseada = ((Vector2)jugador.position - rb.position).normalized;
        Vector2 mover = Navegacion.DireccionEvitando(rb.position, deseada, gameObject, radio, distanciaSensor, sensorHits);
        rb.MovePosition(rb.position + mover * velocidad * Time.fixedDeltaTime);
    }

    // ============================ RAYO ============================
    IEnumerator RutinaRayo()
    {
        yield return new WaitForSeconds(1.5f);   // respiro inicial al entrar a la sala
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreRayos);
            while (ocupado) yield return null;
            if (jugador == null) continue;

            ocupado = true;                      // se queda quieto mientras apunta y dispara
            RayoJefe.Lanzar(transform, jugador);
            yield return new WaitForSeconds(RayoJefe.DURACION_AVISO + RayoJefe.DURACION_RAYO);
            ocupado = false;
        }
    }

    // ============================ TELETRANSPORTE ============================
    IEnumerator RutinaTeleport()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreTeleports);
            while (ocupado) yield return null;
            if (jugador == null) continue;

            ocupado = true;

            // Parpadeo de aviso antes de desaparecer
            yield return Parpadear(0.35f);

            Vector2 destino = ElegirDestinoCerca();
            GestorAudio.Efecto("jefe_teleport");
            ExplosionVisual.Crear(transform.position, 1.2f);
            if (rb != null) rb.position = destino; else transform.position = destino;
            ExplosionVisual.Crear(destino, 1.2f);

            // Parpadeo al aparecer (medio segundo de gracia para el jugador)
            yield return Parpadear(0.35f);

            ocupado = false;
        }
    }

    Vector2 ElegirDestinoCerca()
    {
        Vector2 pos = jugador.position;
        Habitacion sala = (ControladorSalas.Instancia != null) ? ControladorSalas.Instancia.SalaActual : null;

        for (int intento = 0; intento < 10; intento++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(distanciaTeleportMin, distanciaTeleportMax);
            Vector2 candidato = pos + offset;

            // No salirse de la sala del combate
            if (sala != null)
            {
                Vector2 centro = sala.CentroMundo;
                Vector2 mitad = sala.tamMundo * 0.5f - Vector2.one * 1.8f;
                candidato.x = Mathf.Clamp(candidato.x, centro.x - mitad.x, centro.x + mitad.x);
                candidato.y = Mathf.Clamp(candidato.y, centro.y - mitad.y, centro.y + mitad.y);
            }

            // Que no caiga encima de una pared/obstaculo
            bool libre = true;
            foreach (var c in Physics2D.OverlapCircleAll(candidato, radio + 0.3f))
            {
                if (c.isTrigger) continue;
                if (c.gameObject == gameObject) continue;
                if (c.CompareTag("Player")) continue;
                libre = false;
                break;
            }
            if (libre) return candidato;
        }
        return transform.position;   // no encontro lugar: se queda donde esta
    }

    IEnumerator Parpadear(float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += 0.08f;
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.08f);
        }
        if (sr != null) sr.enabled = true;
    }

    // ============================ VIDA / CONTACTO ============================
    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0f)
        {
            GetComponent<Botin>()?.Soltar();
            GestorAudio.Efecto("enemigo_muere");
            if (GeneradorMapa.Instancia != null)
                GeneradorMapa.Instancia.AlMorirJefe(transform.position);
            Destroy(gameObject);
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
