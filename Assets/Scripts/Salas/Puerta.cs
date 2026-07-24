using UnityEngine;

/// <summary>
/// Va en cada uno de los 4 huecos (puertas) de una sala.
/// Estados:
///   - Muro: es una pared solida permanente (no hay sala vecina por ese lado).
///   - Puerta abierta: se puede pasar (barrera desactivada).
///   - Puerta cerrada: bloquea el paso durante el combate (barrera activada).
///   - Peaje: cerrada hasta que el jugador la toque teniendo monedas suficientes
///     (salas especiales del nivel 2).
///
/// El collider (barrera) esta en el objeto raiz y el SPRITE en un hijo, para que
/// intercambiar entre abierta/cerrada sea siempre confiable (modo Simple) y el
/// sprite se reescale para llenar exactamente el hueco de la puerta.
/// </summary>
public class Puerta : MonoBehaviour
{
    [Header("Barrera fisica solida (Collider2D que bloquea el paso al cerrar)")]
    public Collider2D barrera;

    [Header("Visual (SpriteRenderer del hijo). Si no asignas sprites igual funciona.)")]
    public SpriteRenderer sprite;
    public Sprite spriteAbierta;
    public Sprite spriteCerrada;
    public Sprite spriteMuro;
    public Sprite spritePeaje;

    [HideInInspector] public Vector2 tamHueco = Vector2.one;   // tamaño del hueco en el mundo
    [HideInInspector] public Color colorAbierta = Color.white;
    [HideInInspector] public Color colorCerrada = Color.white;
    [HideInInspector] public Color colorMuro = Color.white;

    public bool Abierta { get; private set; } = true;
    public bool EsMuro { get; private set; } = false;

    [Header("Peaje (0 = gratis). Lo configura el generador en el nivel 2")]
    public int costoEntrada = 0;
    private bool peajePagado = false;
    private TextMesh textoPeaje;

    /// <summary>Cambia el sprite y lo reescala para llenar el hueco (modo Simple).</summary>
    private void PonerSprite(Sprite s, Color color)
    {
        if (sprite == null || s == null) return;
        sprite.drawMode = SpriteDrawMode.Simple;
        sprite.sprite = s;
        sprite.color = color;

        Vector3 b = s.bounds.size;   // tamaño del sprite a escala 1
        if (b.x > 0.0001f && b.y > 0.0001f)
            sprite.transform.localScale = new Vector3(tamHueco.x / b.x, tamHueco.y / b.y, 1f);
    }

    /// <summary>Convierte este hueco en una puerta normal (empieza abierta).</summary>
    public void VolverPuerta()
    {
        EsMuro = false;
        Abrir();
    }

    /// <summary>Convierte este hueco en una pared solida permanente.</summary>
    public void VolverMuro()
    {
        EsMuro = true;
        Abierta = false;
        if (barrera != null) barrera.enabled = true;
        PonerSprite(spriteMuro != null ? spriteMuro : spriteCerrada, colorMuro);
    }

    /// <summary>
    /// Convierte la puerta en una puerta de PEAJE: queda cerrada, con un cartel
    /// con el precio. Se abre sola al tocarla con monedas suficientes.
    /// </summary>
    public void ConfigurarPeaje(int costo, Sprite spriteDorado, Vector3 posTexto)
    {
        costoEntrada = costo;
        peajePagado = false;
        if (spriteDorado != null) spritePeaje = spriteDorado;

        EsMuro = false;
        Abierta = false;
        if (barrera != null) barrera.enabled = true;
        PonerSprite(spritePeaje != null ? spritePeaje : spriteCerrada, Color.white);

        if (textoPeaje == null)
            textoPeaje = UtilJuego.CrearTexto(costo.ToString() + " monedas", posTexto, transform.parent, new Color(1f, 0.85f, 0.3f), 2.6f);
    }

    // El jugador choca contra la barrera del peaje -> intenta pagar
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (costoEntrada <= 0 || peajePagado) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (GameManager.Instance != null && GameManager.Instance.GastarMonedas(costoEntrada))
        {
            peajePagado = true;
            costoEntrada = 0;
            if (textoPeaje != null) Destroy(textoPeaje.gameObject);
            Abrir();
        }
        else if (textoPeaje != null)
        {
            textoPeaje.color = Color.red;   // feedback: no alcanzan las monedas
        }
    }

    public void Abrir()
    {
        if (EsMuro) return;
        if (costoEntrada > 0 && !peajePagado) return;   // el peaje no se abre gratis
        Abierta = true;
        if (barrera != null) barrera.enabled = false;
        PonerSprite(spriteAbierta, colorAbierta);
    }

    public void Cerrar()
    {
        if (EsMuro) return;
        Abierta = false;
        if (barrera != null) barrera.enabled = true;
        PonerSprite(spriteCerrada, colorCerrada);
    }
}
