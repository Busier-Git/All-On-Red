using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portal que aparece al morir el jefe. Al tocarlo, guarda el progreso
/// (monedas, vida y objetos) y carga la escena destino.
/// IMPORTANTE: la escena destino debe estar en File > Build Settings.
/// </summary>
public class PortalNivel : MonoBehaviour
{
    public string escenaDestino = "2";
    public bool guardarProgreso = true;   // true al pasar de nivel; false al salir al menu

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (guardarProgreso)
        {
            Player p = other.GetComponent<Player>();
            EstadoPartida.Guardar(
                GameManager.Instance != null ? GameManager.Instance.monedas : 0,
                p != null ? p.VidaActual : 5,
                p != null ? p.Objetos : null);
        }
        else
        {
            EstadoPartida.Limpiar();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaDestino);
    }

    /// <summary>Crea el portal por codigo (lo llama el generador al morir el jefe).</summary>
    public static PortalNivel Crear(Vector3 pos, string escena, string etiqueta, bool guardar)
    {
        GameObject go = UtilJuego.CrearCuadro("PortalNivel", pos, new Vector2(2.2f, 1.4f), new Color(0.45f, 0.20f, 0.65f), 6);

        // Sprite del Banco de Sprites (si hay)
        BancoSprites banco = BancoSprites.Cargar();
        if (banco != null && banco.portal != null)
            UtilJuego.AplicarSprite(go, banco.portal, new Vector2(2.2f, 1.4f), true, false);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // zona de entrada ~2.2 x 1.4 unidades en el mundo, sin importar la escala del sprite
        col.size = new Vector2(2.2f / Mathf.Max(go.transform.localScale.x, 0.0001f),
                               1.4f / Mathf.Max(go.transform.localScale.y, 0.0001f));

        UtilJuego.CrearTexto(etiqueta, pos + Vector3.up * 1.3f, go.transform, new Color(0.85f, 0.7f, 1f), 2.8f);

        PortalNivel portal = go.AddComponent<PortalNivel>();
        portal.escenaDestino = escena;
        portal.guardarProgreso = guardar;
        return portal;
    }
}
