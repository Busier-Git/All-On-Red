using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimapa estilo Isaac. Lo crea solo el GeneradorMapa al terminar de generar.
/// Dibuja la estructura del piso en la esquina superior derecha: cada sala es un
/// rectangulo (las grandes ocupan mas), colorea las especiales y resalta la sala actual.
/// </summary>
public class Minimapa : MonoBehaviour
{
    public static Minimapa Instancia;

    [Header("Aspecto (pixeles)")]
    public float paso = 24f;          // separacion entre celdas (subelo para un minimapa mas grande)
    public float gap = 4f;            // hueco entre salas
    public float margen = 16f;        // distancia a la esquina de la pantalla
    public float margenInterno = 5f;  // borde del fondo

    private readonly Dictionary<Habitacion, Image> imgs = new Dictionary<Habitacion, Image>();
    private readonly Dictionary<Habitacion, Color> colores = new Dictionary<Habitacion, Color>();

    void Awake() { Instancia = this; }

    public void Construir(GeneradorMapa gen)
    {
        int maxCx = gen.Ancho - 1;
        int maxCy = gen.Alto - 1;

        // Canvas propio en overlay (encima de todo)
        GameObject canvasGO = new GameObject("CanvasMinimapa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Fondo semitransparente, anclado arriba-derecha
        GameObject fondo = NuevaImagen("Fondo", canvasGO.transform, new Color(0f, 0f, 0f, 0.45f));
        RectTransform fr = fondo.GetComponent<RectTransform>();
        fr.anchorMin = fr.anchorMax = new Vector2(1f, 1f);
        fr.pivot = new Vector2(1f, 1f);
        fr.sizeDelta = new Vector2((maxCx + 1) * paso + margenInterno * 2f, (maxCy + 1) * paso + margenInterno * 2f);
        fr.anchoredPosition = new Vector2(-margen, -margen);

        imgs.Clear();
        colores.Clear();

        foreach (var info in gen.ObtenerSalas())
        {
            if (info.hab == null) continue;

            GameObject ri = NuevaImagen("sala", fondo.transform, Color.white);
            RectTransform rt = ri.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(info.tam.x * paso - gap, info.tam.y * paso - gap);

            // celda superior-derecha de la sala -> posicion (mirror del mundo: arriba=arriba, derecha=derecha)
            int trCx = info.origen.x + info.tam.x - 1;
            int trCy = info.origen.y + info.tam.y - 1;
            float x = -((maxCx - trCx) * paso) - margenInterno;
            float y = -((maxCy - trCy) * paso) - margenInterno;
            rt.anchoredPosition = new Vector2(x, y);

            Color c = ColorTipo(info.tipo);
            colores[info.hab] = c;
            Image img = ri.GetComponent<Image>();
            img.color = Atenuar(c);
            imgs[info.hab] = img;
        }

        if (ControladorSalas.Instancia != null)
        {
            ControladorSalas.Instancia.AlCambiarSala -= Resaltar;
            ControladorSalas.Instancia.AlCambiarSala += Resaltar;
            if (ControladorSalas.Instancia.SalaActual != null)
                Resaltar(ControladorSalas.Instancia.SalaActual);
        }
    }

    // Resalta la sala actual (color pleno) y atenua las demas
    void Resaltar(Habitacion actual)
    {
        foreach (var kv in imgs)
        {
            if (kv.Value == null) continue;
            Color c = colores[kv.Key];
            kv.Value.color = (kv.Key == actual) ? c : Atenuar(c);
        }
    }

    void OnDestroy()
    {
        if (ControladorSalas.Instancia != null)
            ControladorSalas.Instancia.AlCambiarSala -= Resaltar;
    }

    GameObject NuevaImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    Color ColorTipo(TipoSala t)
    {
        switch (t)
        {
            case TipoSala.Inicio: return new Color(0.90f, 0.90f, 0.95f);
            case TipoSala.Jefe:   return new Color(0.85f, 0.20f, 0.20f);
            case TipoSala.Tesoro: return new Color(0.95f, 0.80f, 0.25f);
            case TipoSala.Tienda: return new Color(0.30f, 0.75f, 0.40f);
            default:              return new Color(0.62f, 0.62f, 0.68f);
        }
    }

    Color Atenuar(Color c) => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 1f);
}
