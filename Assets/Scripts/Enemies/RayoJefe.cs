using System.Collections;
using UnityEngine;

/// <summary>
/// Rayo del jefe del nivel 2 (estilo Brimstone enemigo):
/// 1) AVISO: una linea delgada que sigue al jugador un momento y luego se fija.
/// 2) RAYO: la linea se engrosa y hace daño al jugador que la toque.
/// Se crea por codigo con RayoJefe.Lanzar(...).
/// </summary>
public class RayoJefe : MonoBehaviour
{
    public const float DURACION_AVISO = 0.8f;
    public const float DURACION_RAYO = 0.55f;

    public float ancho = 1.3f;
    public int dano = 1;

    private Transform origen;      // el jefe
    private Transform objetivo;    // el jugador
    private LineRenderer lr;
    private float tiempoUltimoDano = -999f;

    public static RayoJefe Lanzar(Transform origen, Transform objetivo)
    {
        GameObject go = new GameObject("RayoJefe");
        RayoJefe r = go.AddComponent<RayoJefe>();
        r.origen = origen;
        r.objetivo = objetivo;

        r.lr = go.AddComponent<LineRenderer>();
        r.lr.positionCount = 2;
        r.lr.material = new Material(Shader.Find("Sprites/Default"));
        r.lr.sortingOrder = 55;

        r.StartCoroutine(r.Rutina());
        return r;
    }

    IEnumerator Rutina()
    {
        Vector2 direccion = Vector2.down;

        // ---- FASE 1: aviso (sigue al jugador el 60% del tiempo y despues se fija) ----
        float t = 0f;
        while (t < DURACION_AVISO)
        {
            if (origen == null) { Destroy(gameObject); yield break; }
            t += Time.deltaTime;

            if (t < DURACION_AVISO * 0.6f && objetivo != null)
                direccion = ((Vector2)objetivo.position - (Vector2)origen.position).normalized;

            float parpadeo = 0.35f + 0.25f * Mathf.PingPong(t * 6f, 1f);
            Dibujar(direccion, 0.12f, new Color(1f, 0.25f, 0.25f, parpadeo));
            yield return null;
        }

        // ---- FASE 2: rayo con daño ----
        GestorAudio.Efecto("jefe_rayo");
        t = 0f;
        while (t < DURACION_RAYO)
        {
            if (origen == null) { Destroy(gameObject); yield break; }
            t += Time.deltaTime;

            float dist = Dibujar(direccion, ancho, new Color(0.85f, 0.08f, 0.10f, 0.95f));
            IntentarDanar(direccion, dist);
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>Dibuja la linea desde el jefe hasta la primera pared. Devuelve el largo.</summary>
    float Dibujar(Vector2 direccion, float grosor, Color color)
    {
        Vector3 desde = origen.position;
        float dist = DistanciaHastaPared(desde, direccion);

        lr.SetPosition(0, desde);
        lr.SetPosition(1, desde + (Vector3)(direccion * dist));
        lr.startWidth = grosor;
        lr.endWidth = grosor;
        lr.startColor = color;
        lr.endColor = color;
        return dist;
    }

    float DistanciaHastaPared(Vector2 desde, Vector2 direccion)
    {
        float dist = 30f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(desde, direccion, 30f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            Collider2D c = h.collider;
            if (c == null || c.isTrigger) continue;
            if (c.CompareTag("Player") || c.CompareTag("enemy")) continue;   // atraviesa al jugador y enemigos
            dist = h.distance;   // pared u obstaculo
            break;
        }
        return dist;
    }

    void IntentarDanar(Vector2 direccion, float dist)
    {
        if (objetivo == null) return;
        if (Time.time < tiempoUltimoDano + 0.4f) return;   // un golpe cada 0.4 s como maximo

        // Distancia del jugador al segmento del rayo
        Vector2 a = origen.position;
        Vector2 b = a + direccion * dist;
        Vector2 p = objetivo.position;
        Vector2 ab = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        float distJugador = Vector2.Distance(p, a + ab * h);

        if (distJugador <= ancho * 0.5f + 0.35f)
        {
            Player jugador = objetivo.GetComponent<Player>();
            if (jugador != null)
            {
                jugador.RecibirDano(dano);
                tiempoUltimoDano = Time.time;
            }
        }
    }
}
