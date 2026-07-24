using UnityEngine;

/// <summary>
/// Pedestal con un objeto encima (sala del tesoro y tienda).
/// Al tocarlo el jugador lo recoge; si tiene precio, primero paga con monedas.
/// </summary>
public class PedestalObjeto : MonoBehaviour
{
    public string idObjeto;
    public int precio = 0;
    [HideInInspector] public TextMesh etiqueta;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player jugador = other.GetComponent<Player>();
        if (jugador == null) return;

        if (precio > 0)
        {
            if (GameManager.Instance == null || !GameManager.Instance.GastarMonedas(precio))
            {
                if (etiqueta != null) etiqueta.color = Color.red;   // feedback: no alcanza
                return;
            }
        }

        jugador.AplicarObjeto(idObjeto);
        GestorAudio.Efecto("objeto");
        Destroy(gameObject);   // la etiqueta es hija: se destruye junto
    }

    /// <summary>Crea un pedestal por codigo con el objeto indicado.</summary>
    public static PedestalObjeto Crear(Vector3 pos, DefObjeto def, int precio, Transform padre = null)
    {
        if (def == null) return null;

        BancoSprites banco = BancoSprites.Cargar();

        // Base gris del pedestal (o el sprite del Banco)
        GameObject baseGO = UtilJuego.CrearCuadro("Pedestal_" + def.id, pos, new Vector2(1.1f, 0.5f), new Color(0.5f, 0.5f, 0.55f), 5, null);
        if (banco != null && banco.pedestalBase != null)
            UtilJuego.AplicarSprite(baseGO, banco.pedestalBase, new Vector2(1.1f, 0.5f), false, false);
        if (padre != null) baseGO.transform.SetParent(padre, true);

        // El objeto en si flotando encima (sprite por id del Banco, o cuadro de color)
        GameObject item = UtilJuego.CrearCuadro("Objeto", pos + Vector3.up * 0.75f, new Vector2(0.8f, 0.8f), def.color, 6, null);
        Sprite sprItem = (banco != null) ? banco.ObjetoPorId(def.id) : null;
        if (sprItem != null)
            UtilJuego.AplicarSprite(item, sprItem, new Vector2(0.8f, 0.8f), true, false);
        item.transform.SetParent(baseGO.transform, true);

        // Nombre (+ precio si corresponde)
        string textoEtiqueta = def.nombre;
        if (precio > 0) textoEtiqueta += "\n" + precio + " monedas";
        TextMesh tm = UtilJuego.CrearTexto(textoEtiqueta, pos + Vector3.up * 1.8f, baseGO.transform, (precio > 0) ? new Color(1f, 0.85f, 0.3f) : Color.white, 2.6f);

        CircleCollider2D col = baseGO.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        // radio de recogida ~1.1 unidades en el mundo, sin importar la escala del sprite
        col.radius = 1.1f / Mathf.Max(baseGO.transform.localScale.x, baseGO.transform.localScale.y, 0.0001f);

        PedestalObjeto p = baseGO.AddComponent<PedestalObjeto>();
        p.idObjeto = def.id;
        p.precio = precio;
        p.etiqueta = tm;
        return p;
    }
}
