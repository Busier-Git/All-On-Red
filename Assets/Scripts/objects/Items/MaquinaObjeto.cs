using System.Collections;
using UnityEngine;

/// <summary>
/// Maquina tragamonedas de las salas especiales. Muestra el SPRITE de la maquina
/// (no el nombre ni un cuadro del item). Tiene cuerpo SOLIDO (choca con el jugador)
/// y una zona alrededor para interactuar: parandote cerca y presionando E pagas el
/// precio (5 monedas) y hay un 50/50 de que te de el objeto. Al ganar, la maquina
/// se APAGA. Si activas 'unSoloUso' es un solo intento.
/// </summary>
public class MaquinaObjeto : MonoBehaviour
{
    public string idObjeto;
    public int precio = 5;
    [Range(0f, 1f)] public float probabilidad = 0.5f;
    public bool unSoloUso = false;

    private bool gastada = false;
    private float proximoUso = -999f;
    private bool jugadorCerca = false;
    private Player jugador;

    private TextMesh etiqueta;
    private SpriteRenderer sr;
    private Sprite spriteApagada;

    void Update()
    {
        if (gastada || !jugadorCerca || jugador == null) return;
        if (Input.GetKeyDown(KeyCode.E))
            Jugar();
    }

    void Jugar()
    {
        if (Time.time < proximoUso) return;
        proximoUso = Time.time + 0.3f;

        // Cobrar
        if (GameManager.Instance == null || !GameManager.Instance.GastarMonedas(precio))
        {
            MostrarMensaje("Sin monedas", new Color(1f, 0.4f, 0.4f));
            if (etiqueta != null) etiqueta.color = Color.red;
            return;
        }
        if (etiqueta != null) etiqueta.color = new Color(1f, 0.85f, 0.3f);
        GestorAudio.Efecto("moneda");

        // Tirada 50/50
        if (Random.value < probabilidad)
        {
            jugador.AplicarObjeto(idObjeto);
            GestorAudio.Efecto("objeto");

            DefObjeto def = BancoObjetos.Def(idObjeto);
            string nombre = (def != null) ? def.nombre : "";
            MostrarMensaje("¡GANASTE!\n" + nombre, new Color(0.4f, 1f, 0.5f));
            Apagar();
        }
        else
        {
            GestorAudio.Efecto("jugador_dano");
            MostrarMensaje("Nada...", new Color(1f, 0.5f, 0.5f));
            if (unSoloUso) Apagar();
        }
    }

    // Deja la maquina apagada: cambia el sprite (o la oscurece) y ya no se puede jugar
    void Apagar()
    {
        gastada = true;
        if (sr != null)
        {
            if (spriteApagada != null) sr.sprite = spriteApagada;
            else sr.color = new Color(0.32f, 0.32f, 0.38f);   // oscurecida = apagada
        }
        if (etiqueta != null) Destroy(etiqueta.gameObject);
    }

    // Mensaje flotante que sube y desaparece (usa la tipografia del juego)
    void MostrarMensaje(string texto, Color color)
    {
        TextMesh tm = UtilJuego.CrearTexto(texto, transform.position + Vector3.up * 2.3f, transform.parent, color, 3.2f);
        if (tm != null) StartCoroutine(AnimarMensaje(tm));
    }

    IEnumerator AnimarMensaje(TextMesh tm)
    {
        float t = 0f, dur = 1.2f;
        Vector3 p0 = tm.transform.position;
        while (t < dur && tm != null)
        {
            t += Time.deltaTime;
            tm.transform.position = p0 + Vector3.up * (t * 0.9f);
            yield return null;
        }
        if (tm != null) Destroy(tm.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        jugador = other.GetComponent<Player>();
        jugadorCerca = (jugador != null);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = false;
    }

    /// <summary>Crea la maquina por codigo (cuerpo solido + zona de interaccion).</summary>
    public static MaquinaObjeto Crear(Vector3 pos, DefObjeto def, int precio, float probabilidad, Transform padre = null)
    {
        if (def == null) return null;

        BancoSprites banco = BancoSprites.Cargar();
        Sprite spr = (banco != null) ? (banco.maquina != null ? banco.maquina : banco.pedestalBase) : null;
        Sprite apagada = (banco != null) ? banco.maquinaApagada : null;

        // RAIZ (escala 1): colliders en unidades de mundo, independientes del sprite
        GameObject go = new GameObject("Maquina_" + def.id);
        go.transform.position = pos;
        if (padre != null) go.transform.SetParent(padre, true);

        BoxCollider2D solido = go.AddComponent<BoxCollider2D>();   // cuerpo SOLIDO: bloquea al jugador
        solido.isTrigger = false;
        solido.size = new Vector2(1.2f, 1.7f);

        CircleCollider2D zona = go.AddComponent<CircleCollider2D>();  // zona para pulsar E
        zona.isTrigger = true;
        zona.radius = 1.8f;

        // VISUAL (hijo): sprite del Banco o cuadro de respaldo
        GameObject vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        if (spr != null)
        {
            UtilJuego.AplicarSprite(vis, spr, new Vector2(1.7f, 2.3f), true, false);
        }
        else
        {
            sr.sprite = UtilJuego.Blanco();
            sr.color = new Color(0.5f, 0.42f, 0.2f);
            vis.transform.localScale = new Vector3(1.4f, 1.9f, 1f);
        }

        // Cartel: SOLO el precio y la tecla (nada de nombre del objeto ni cuadro del item)
        TextMesh tm = UtilJuego.CrearTexto("Jugar: E\n" + precio + " monedas", pos + Vector3.up * 1.8f, padre, new Color(1f, 0.85f, 0.3f), 2.6f);

        MaquinaObjeto m = go.AddComponent<MaquinaObjeto>();
        m.idObjeto = def.id;
        m.precio = precio;
        m.probabilidad = probabilidad;
        m.etiqueta = tm;
        m.sr = sr;
        m.spriteApagada = apagada;
        return m;
    }
}
