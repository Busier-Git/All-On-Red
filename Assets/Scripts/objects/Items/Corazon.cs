using UnityEngine;

/// <summary>
/// Corazon que cura 1 de vida al jugador. Si tiene precio (tienda) hay que
/// pagarlo con monedas. No se puede recoger con la vida llena.
/// </summary>
public class Corazon : MonoBehaviour
{
    public int precio = 0;
    [HideInInspector] public TextMesh etiqueta;   // texto de precio (opcional)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player jugador = other.GetComponent<Player>();
        if (jugador == null) return;
        if (jugador.VidaActual >= jugador.vidaMaxima) return;   // vida llena: no se gasta

        if (precio > 0)
        {
            if (GameManager.Instance == null || !GameManager.Instance.GastarMonedas(precio))
            {
                if (etiqueta != null) etiqueta.color = Color.red;   // feedback: no alcanza
                return;
            }
        }

        jugador.Curar(1);
        GestorAudio.Efecto("corazon");
        if (etiqueta != null) Destroy(etiqueta.gameObject);
        Destroy(gameObject);
    }

    /// <summary>Crea un corazon por codigo en la posicion dada.</summary>
    public static Corazon Crear(Vector3 pos, int precio, Transform padre = null)
    {
        GameObject go = UtilJuego.CrearCuadro("Corazon", pos, new Vector2(0.7f, 0.7f), new Color(0.9f, 0.15f, 0.2f), 6, null);

        // Sprite del Banco de Sprites (si hay)
        BancoSprites banco = BancoSprites.Cargar();
        if (banco != null && banco.corazon != null)
            UtilJuego.AplicarSprite(go, banco.corazon, new Vector2(0.7f, 0.7f), true, false);
        if (padre != null) go.transform.SetParent(padre, true);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        // radio de recogida ~0.8 unidades en el mundo, sin importar la escala del sprite
        col.radius = 0.8f / Mathf.Max(go.transform.localScale.x, 0.0001f);

        Corazon c = go.AddComponent<Corazon>();
        c.precio = precio;
        if (precio > 0)
            c.etiqueta = UtilJuego.CrearTexto(precio + " monedas", pos + Vector3.up * 1.1f, padre, new Color(1f, 0.85f, 0.3f), 2.6f);
        return c;
    }
}
