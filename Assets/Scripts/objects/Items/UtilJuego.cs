using UnityEngine;

/// <summary>
/// Utilidades compartidas para crear cosas por codigo (sprites de color,
/// textos flotantes, etc). Lo usan el generador, los pedestales, corazones,
/// portales y las puertas con peaje.
/// </summary>
public static class UtilJuego
{
    static Sprite _blanco;
    static Font _fuente;

    /// <summary>Sprite blanco de 4x4 (se tiñe con SpriteRenderer.color).</summary>
    public static Sprite Blanco()
    {
        if (_blanco == null)
        {
            Texture2D t = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            t.SetPixels(px); t.Apply();
            t.filterMode = FilterMode.Point;
            _blanco = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        return _blanco;
    }

    /// <summary>
    /// Tipografia del juego. Primero busca una fuente pixel-art personalizada en
    /// Resources (arrastra un .ttf y renombralo "FuentePixel"); si no hay, usa la
    /// fuente por defecto de Unity. La usan todos los textos del mundo y el menu.
    /// </summary>
    public static Font Fuente()
    {
        if (_fuente == null)
        {
            _fuente = Resources.Load<Font>("FuentePixel");   // tu fuente pixel-art (opcional)
            if (_fuente == null)
            {
                try { _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_fuente == null)
                {
                    try { _fuente = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
            }
        }
        return _fuente;
    }

    /// <summary>Crea un cuadrito de color en el mundo.</summary>
    public static GameObject CrearCuadro(string nombre, Vector3 pos, Vector2 escala, Color color, int orden, Transform padre = null)
    {
        GameObject go = new GameObject(nombre);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        if (padre != null) go.transform.SetParent(padre, true);   // conserva tamaño en mundo
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco();
        sr.color = color;
        sr.sortingOrder = orden;
        return go;
    }

    /// <summary>
    /// Pone un sprite en un GameObject ajustando la ESCALA para que mida
    /// 'tamMundo' unidades en el juego, sin importar la resolucion o el
    /// Pixels Per Unit del sprite. Tambien reajusta los colliders que ya
    /// existan para que sigan midiendo lo mismo.
    /// </summary>
    public static void AplicarSprite(GameObject go, Sprite s, Vector2 tamMundo, bool mantenerProporcion = true, bool ajustarColliders = true)
    {
        if (go == null || s == null) return;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = Color.white;
        sr.drawMode = SpriteDrawMode.Simple;

        Vector3 b = s.bounds.size;   // tamaño del sprite a escala 1
        if (b.x <= 0.0001f || b.y <= 0.0001f) return;

        float sx = tamMundo.x / b.x;
        float sy = tamMundo.y / b.y;
        if (mantenerProporcion) { float k = Mathf.Min(sx, sy); sx = k; sy = k; }
        go.transform.localScale = new Vector3(sx, sy, 1f);

        if (ajustarColliders)
        {
            // Los colliders se escalan con el transform: los dejamos del tamaño del sprite
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (box != null) box.size = new Vector2(b.x, b.y);
            CircleCollider2D cir = go.GetComponent<CircleCollider2D>();
            if (cir != null) cir.radius = Mathf.Max(b.x, b.y) * 0.5f;
        }
    }

    /// <summary>Texto flotante en el mundo (TextMesh clasico, sin depender de TMP).</summary>
    public static TextMesh CrearTexto(string contenido, Vector3 pos, Transform padre, Color color, float tamano = 3.2f)
    {
        GameObject go = new GameObject("Texto");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.35f;
        if (padre != null) go.transform.SetParent(padre, true);   // conserva tamaño en mundo

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = contenido;
        tm.characterSize = tamano * 0.1f;
        tm.fontSize = 64;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        Font f = Fuente();
        if (f != null)
        {
            tm.font = f;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = f.material;
        }
        MeshRenderer rend = go.GetComponent<MeshRenderer>();
        if (rend != null) rend.sortingOrder = 30;
        return tm;
    }
}
