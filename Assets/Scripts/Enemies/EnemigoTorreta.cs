using UnityEngine;

/// <summary>
/// Enemigo estatico (no se mueve). Cuando el jugador entra en su rango de vision
/// le dispara, PERO solo si tiene linea de vision despejada: si hay una pared u
/// obstaculo en medio, no dispara. Necesita Collider2D (NO trigger) y tag "enemy".
/// El generador lo crea SIN Rigidbody dinamico para que quede fijo.
/// </summary>
public class EnemigoTorreta : MonoBehaviour, IDanable
{
    [Header("Vida")]
    public float vidaMaxima = 4f;
    private float vidaActual;

    [Header("Vision / disparo")]
    public float rangoVision = 9f;
    public GameObject prefabProyectil;          // EnemyBullet
    public float velocidadProyectil = 7f;
    public float cadencia = 1.2f;
    private float tiempoSigDisparo;

    [Header("Daño por contacto")]
    public float danoContacto = 1f;
    public float intervaloDano = 1f;
    private float tiempoUltimoDano = -999f;

    private Transform jugador;

    void Start()
    {
        vidaActual = vidaMaxima;
        GameObject pj = GameObject.FindWithTag("Player");
        if (pj != null) jugador = pj.transform;
    }

    void Update()
    {
        if (jugador == null) return;
        if (Vector2.Distance(transform.position, jugador.position) > rangoVision) return;
        if (Time.time < tiempoSigDisparo) return;
        if (!TieneLineaDeVision()) return;       // detras de un obstaculo -> no dispara

        Disparar();
        tiempoSigDisparo = Time.time + cadencia;
    }

    // Lanza un rayo hacia el jugador; si choca con algo solido (pared/obstaculo) antes
    // de llegar, NO hay linea de vision. Ignora triggers, a si mismo, al jugador y a otros enemigos.
    bool TieneLineaDeVision()
    {
        Vector2 origen = transform.position;
        Vector2 dir = (Vector2)jugador.position - origen;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir.normalized, dist);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.gameObject == gameObject) continue;
            if (h.collider.CompareTag("Player") || h.collider.CompareTag("enemy")) continue;
            return false;   // pared u obstaculo en medio
        }
        return true;
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;
        Vector2 dir = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        GameObject p = Instantiate(prefabProyectil, transform.position, Quaternion.identity);
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = dir * velocidadProyectil;
    }

    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0f)
        {
            GetComponent<Botin>()?.Soltar();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) { IntentarDanar(collision.gameObject); }
    private void OnCollisionStay2D(Collision2D collision)  { IntentarDanar(collision.gameObject); }

    private void IntentarDanar(GameObject obj)
    {
        if (!obj.CompareTag("Player")) return;
        if (Time.time < tiempoUltimoDano + intervaloDano) return;
        Player p = obj.GetComponent<Player>();
        if (p != null) { p.RecibirDano(Mathf.RoundToInt(danoContacto)); tiempoUltimoDano = Time.time; }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoVision);
    }
}
