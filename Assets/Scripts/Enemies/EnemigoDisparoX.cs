using UnityEngine;

/// <summary>
/// Enemigo que se mueve al azar (deambula y rebota de las paredes) y dispara en X
/// (4 diagonales) cada cierto tiempo. Necesita Rigidbody2D (Gravity 0) + Collider2D
/// no trigger, y el tag "enemy".
/// </summary>
public class EnemigoDisparoX : MonoBehaviour, IDanable
{
    [Header("Vida")]
    public float vidaMaxima = 4f;
    private float vidaActual;

    [Header("Movimiento aleatorio")]
    public float velocidad = 2.5f;
    public float tiempoCambioDireccion = 1.5f;
    private Vector2 direccion;
    private float tiempoSigCambio;

    [Header("Disparo en X (diagonales)")]
    public GameObject prefabProyectil;          // asigna EnemyBullet.prefab
    public float velocidadProyectil = 6f;
    public float cadencia = 1.5f;
    private float tiempoSigDisparo;

    [Header("Daño por contacto")]
    public float danoContacto = 1f;
    public float intervaloDano = 1f;
    private float tiempoUltimoDano = -999f;

    private Rigidbody2D rb;

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        ElegirNuevaDireccion();
    }

    void Update()
    {
        if (Time.time >= tiempoSigCambio) ElegirNuevaDireccion();

        if (Time.time >= tiempoSigDisparo)
        {
            DispararEnX();
            tiempoSigDisparo = Time.time + cadencia;
        }
    }

    void FixedUpdate()
    {
        if (rb != null) rb.velocity = direccion * velocidad;
    }

    void ElegirNuevaDireccion()
    {
        Vector2 d = Random.insideUnitCircle;
        direccion = (d == Vector2.zero) ? Vector2.right : d.normalized;
        tiempoSigCambio = Time.time + tiempoCambioDireccion;
    }

    void DispararEnX()
    {
        if (prefabProyectil == null) return;

        Vector2[] diagonales =
        {
            new Vector2( 1,  1).normalized,
            new Vector2(-1,  1).normalized,
            new Vector2( 1, -1).normalized,
            new Vector2(-1, -1).normalized
        };

        foreach (Vector2 d in diagonales)
        {
            GameObject p = Instantiate(prefabProyectil, transform.position, Quaternion.identity);
            Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
            if (prb != null) prb.velocity = d * velocidadProyectil;
        }
    }

    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0f)
        {
            GetComponent<Botin>()?.Soltar();
            GestorAudio.Efecto("enemigo_muere");
            Destroy(gameObject);
        }
    }

    // --- Contacto + rebote de paredes ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        IntentarDanar(collision.gameObject);
        if (!collision.gameObject.CompareTag("Player"))
            ElegirNuevaDireccion(); // rebota de paredes/obstaculos para no quedar pegado
    }
    private void OnCollisionStay2D(Collision2D collision) { IntentarDanar(collision.gameObject); }

    private void IntentarDanar(GameObject obj)
    {
        if (!obj.CompareTag("Player")) return;
        if (Time.time < tiempoUltimoDano + intervaloDano) return;

        Player jugador = obj.GetComponent<Player>();
        if (jugador != null)
        {
            jugador.RecibirDano(Mathf.RoundToInt(danoContacto));
            tiempoUltimoDano = Time.time;
        }
    }
}
