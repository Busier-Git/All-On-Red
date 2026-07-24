using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rayo del jugador (estilo Brimstone): laser que ATRAVIESA a los enemigos y se
/// corta en la primera pared u obstaculo.
/// Sinergias:
/// - Disparo Doble / Cuadruple: se disparan VARIOS rayos en abanico (Player.Disparar).
/// - Corazon Sagrado: el rayo se vuelve blanco, ONDULADO y persigue a los enemigos
///   curvandose hacia ellos (estilo The Hanged Man de Isaac).
/// </summary>
public class RayoJugador : MonoBehaviour
{
    private LineRenderer lr;

    public static void Disparar(Vector3 origen, Vector2 direccion, float dano, float ancho, bool autoApuntar)
    {
        if (direccion == Vector2.zero) direccion = Vector2.down;
        direccion = direccion.normalized;

        if (autoApuntar)
            DispararSerpiente(origen, direccion, dano, ancho);   // sagrado: ondulado y persigue
        else
            DispararRecto(origen, direccion, dano, ancho);       // normal: recto y rojo
    }

    // ============================ RAYO RECTO (rojo) ============================
    static void DispararRecto(Vector3 origen, Vector2 direccion, float dano, float ancho)
    {
        float distancia = 30f;
        List<IDanable> aDanar = new List<IDanable>();

        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, direccion, 30f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            Collider2D c = h.collider;
            if (c == null || c.isTrigger) continue;
            if (c.CompareTag("Player")) continue;

            Obstaculo obst = c.GetComponent<Obstaculo>();
            if (obst != null) { aDanar.Add(obst); distancia = h.distance; break; }  // daña la caja/roca y se corta

            IDanable d = c.GetComponent<IDanable>();
            if (d != null) { aDanar.Add(d); continue; }   // enemigo: lo atraviesa

            distancia = h.distance;                       // pared: se corta
            break;
        }

        foreach (var d in aDanar) d.RecibirDano(dano);

        List<Vector3> puntos = new List<Vector3> { origen, origen + (Vector3)(direccion * distancia) };
        CrearVisual(puntos, ancho,
            new Color(0.85f, 0.10f, 0.12f, 0.95f),
            new Color(0.55f, 0.05f, 0.08f, 0.95f));
    }

    // ================= RAYO SERPIENTE (sagrado: blanco, ondulado, persigue) =================
    static void DispararSerpiente(Vector3 origen, Vector2 direccion, float dano, float ancho)
    {
        const float PASO = 0.45f;         // largo de cada segmento
        const int MAX_PASOS = 70;         // ~30 unidades de alcance
        const float GIRO_POR_PASO = 9f;   // cuanto se curva hacia el enemigo (grados)
        const float ONDA = 16f;           // amplitud del zigzag (grados)

        List<Vector3> puntos = new List<Vector3>();
        HashSet<IDanable> danados = new HashSet<IDanable>();

        Vector2 pos = origen;
        Vector2 dir = direccion;
        puntos.Add(pos);
        bool cortado = false;

        for (int i = 0; i < MAX_PASOS && !cortado; i++)
        {
            // Se curva hacia el enemigo mas cercano (teledirigido)
            Transform obj = Granada.BuscarEnemigoCercano(pos, 12f);
            if (obj != null)
            {
                Vector2 haciaEl = ((Vector2)obj.position - pos).normalized;
                dir = ((Vector2)Vector3.RotateTowards(dir, haciaEl, GIRO_POR_PASO * Mathf.Deg2Rad, 0f)).normalized;
            }

            // Ondulacion: zigzag suave alrededor de la direccion real
            Vector2 dirPaso = Rotar(dir, Mathf.Sin(i * 0.55f) * ONDA);

            // Una pared u obstaculo corta el rayo
            float avance = PASO;
            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, dirPaso, PASO);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                Collider2D c = h.collider;
                if (c == null || c.isTrigger) continue;
                if (c.CompareTag("Player")) continue;

                Obstaculo obst = c.GetComponent<Obstaculo>();
                if (obst != null) { danados.Add(obst); avance = h.distance; cortado = true; break; }

                IDanable d = c.GetComponent<IDanable>();
                if (d != null) { danados.Add(d); continue; }   // enemigo: lo atraviesa

                avance = h.distance;                            // pared
                cortado = true;
                break;
            }

            pos += dirPaso * avance;
            puntos.Add(pos);

            // Daña a los enemigos que quedan pegados al camino del rayo
            foreach (var c in Physics2D.OverlapCircleAll(pos, ancho * 0.5f + 0.2f))
            {
                if (c == null || c.isTrigger || c.CompareTag("Player")) continue;
                if (c.GetComponent<Obstaculo>() != null) continue;   // las cajas solo de frente
                IDanable d = c.GetComponent<IDanable>();
                if (d != null) danados.Add(d);
            }
        }

        foreach (var d in danados) d.RecibirDano(dano);

        // Blanco con un toque rosado, como el Corazon Sagrado
        CrearVisual(puntos, ancho,
            new Color(1f, 0.97f, 0.98f, 0.95f),
            new Color(0.95f, 0.80f, 0.88f, 0.9f));
    }

    static Vector2 Rotar(Vector2 v, float grados)
    {
        float r = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ============================ VISUAL ============================
    static void CrearVisual(List<Vector3> puntos, float ancho, Color c0, Color c1)
    {
        GameObject go = new GameObject("RayoJugador");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = puntos.Count;
        for (int i = 0; i < puntos.Count; i++)
            lr.SetPosition(i, puntos[i]);
        lr.startWidth = ancho;
        lr.endWidth = ancho * 0.85f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = c0;
        lr.endColor = c1;
        lr.sortingOrder = 60;

        RayoJugador r = go.AddComponent<RayoJugador>();
        r.lr = lr;
        r.StartCoroutine(r.Desvanecer());
    }

    IEnumerator Desvanecer()
    {
        float dur = 0.22f, t = 0f;
        Color c0 = lr.startColor, c1 = lr.endColor;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = 1f - (t / dur);
            lr.startColor = new Color(c0.r, c0.g, c0.b, c0.a * a);
            lr.endColor = new Color(c1.r, c1.g, c1.b, c1.a * a);
            yield return null;
        }
        Destroy(gameObject);
    }
}
