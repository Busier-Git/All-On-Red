using System.Collections;
using UnityEngine;

/// <summary>
/// Granada del Lanzagranadas (estilo Dr. Fetus): viaja un RANGO MEDIO y explota
/// (tambien explota si choca con algo). La explosion daña a enemigos y cajas,
/// y si el JUGADOR esta muy cerca le quita 1 de vida. Sinergia: con el
/// Corazon Sagrado las granadas son teledirigidas.
/// </summary>
public class Granada : MonoBehaviour
{
    public float dano = 1f;
    public float rango = 6.5f;            // rango medio antes de explotar sola
    public float radioExplosion = 2.1f;
    public bool teledirigida = false;
    public float fuerzaGiro = 160f;       // grados/seg al perseguir (si es teledirigida)

    private Rigidbody2D rb;
    private Vector3 origen;
    private float tiempoBusqueda;
    private Transform objetivo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        origen = transform.position;
        Destroy(gameObject, 6f);   // seguro por si algo falla
    }

    void Update()
    {
        // ¿Recorrio su rango medio? -> explota
        if (Vector3.Distance(origen, transform.position) >= rango)
        {
            Explotar();
            return;
        }

        if (!teledirigida || rb == null) return;

        // Busca al enemigo mas cercano cada 0.2 s
        if (Time.time >= tiempoBusqueda)
        {
            tiempoBusqueda = Time.time + 0.2f;
            objetivo = BuscarEnemigoCercano(transform.position, 14f);
        }
        if (objetivo == null) return;

        Vector2 deseada = ((Vector2)objetivo.position - (Vector2)transform.position).normalized;
        Vector2 actual = rb.velocity.normalized;
        float rapidez = rb.velocity.magnitude;
        Vector2 nueva = Vector3.RotateTowards(actual, deseada, fuerzaGiro * Mathf.Deg2Rad * Time.deltaTime, 0f);
        rb.velocity = nueva.normalized * rapidez;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.isTrigger) return;                 // monedas, zonas, etc.
        if (collision.CompareTag("Player")) return;      // no explota en el propio jugador
        Explotar();                                      // enemigo, pared u obstaculo
    }

    void Explotar()
    {
        Vector2 pos = transform.position;

        // Daño en area a todo lo dañable (enemigos, jefe, cajas)
        Collider2D[] alcanzados = Physics2D.OverlapCircleAll(pos, radioExplosion);
        foreach (var c in alcanzados)
        {
            if (c.CompareTag("Player")) continue;
            IDanable d = c.GetComponent<IDanable>();
            if (d != null) d.RecibirDano(dano);
        }

        // Si el jugador esta muy cerca de la explosion, pierde 1 de vida
        GameObject pj = GameObject.FindWithTag("Player");
        if (pj != null && Vector2.Distance(pj.transform.position, pos) <= radioExplosion)
        {
            Player p = pj.GetComponent<Player>();
            if (p != null) p.RecibirDano(1);
        }

        ExplosionVisual.Crear(pos, radioExplosion);
        GestorAudio.Efecto("explosion");
        Destroy(gameObject);
    }

    public static Transform BuscarEnemigoCercano(Vector2 desde, float rangoMax)
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("enemy");
        Transform mejor = null;
        float mejorDist = rangoMax;
        foreach (var e in enemigos)
        {
            if (e == null || !e.activeInHierarchy) continue;
            float d = Vector2.Distance(desde, e.transform.position);
            if (d < mejorDist) { mejorDist = d; mejor = e.transform; }
        }
        return mejor;
    }

    /// <summary>Lanza una granada por codigo (no necesita prefab).</summary>
    public static Granada Lanzar(Vector3 pos, Vector2 direccion, float dano, float velocidad, bool teledirigida)
    {
        GameObject go = UtilJuego.CrearCuadro("Granada", pos, new Vector2(0.45f, 0.45f), new Color(0.45f, 0.55f, 0.30f), 20);

        // Sprite del Banco de Sprites (si hay)
        BancoSprites banco = BancoSprites.Cargar();
        if (banco != null && banco.granada != null)
            UtilJuego.AplicarSprite(go, banco.granada, new Vector2(0.45f, 0.45f), true, false);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        // radio de contacto ~0.27 unidades en el mundo, sin importar la escala del sprite
        col.radius = 0.27f / Mathf.Max(go.transform.localScale.x, 0.0001f);

        Granada g = go.AddComponent<Granada>();
        g.dano = dano;
        g.teledirigida = teledirigida;
        rb.velocity = direccion.normalized * velocidad;
        return g;
    }
}

/// <summary>Circulo naranja (o el sprite del Banco) que crece y se desvanece: el "boom".</summary>
public class ExplosionVisual : MonoBehaviour
{
    private SpriteRenderer sr;
    private float radio;
    private float factorEscala = 1f;   // compensa el tamaño del sprite personalizado

    public static void Crear(Vector2 pos, float radio)
    {
        GameObject go = UtilJuego.CrearCuadro("Explosion", pos, new Vector2(0.3f, 0.3f), new Color(1f, 0.6f, 0.15f, 0.85f), 25);
        ExplosionVisual ev = go.AddComponent<ExplosionVisual>();
        ev.radio = radio;

        BancoSprites banco = BancoSprites.Cargar();
        if (banco != null && banco.explosion != null)
        {
            SpriteRenderer srGo = go.GetComponent<SpriteRenderer>();
            srGo.sprite = banco.explosion;
            srGo.color = new Color(1f, 1f, 1f, 0.85f);
            float lado = Mathf.Max(banco.explosion.bounds.size.x, banco.explosion.bounds.size.y);
            if (lado > 0.0001f) ev.factorEscala = 1f / lado;   // asi el sprite mide lo que diga la escala
        }
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        float dur = 0.28f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float tam = Mathf.Lerp(0.3f, radio * 2f, k) * factorEscala;
            transform.localScale = new Vector3(tam, tam, 1f);
            if (sr != null) { Color c = sr.color; c.a = 0.85f * (1f - k); sr.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }
}
