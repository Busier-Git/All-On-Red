using UnityEngine;

/// <summary>
/// Bala de los enemigos. Ahora:
/// - Choca con las paredes (ya no las atraviesa).
/// - Rompe los obstaculos destructibles (cajas) al pegarles.
/// - Sigue ignorando a otros enemigos (para no morir sobre el que dispara).
/// Usa un "barrido" con raycast entre la posicion anterior y la actual, asi
/// nunca se salta una pared aunque vaya rapida o falle el evento de trigger.
/// </summary>
public class ProyectilEnemigo : MonoBehaviour
{
    public float dano = 1f;
    public float tiempoVida = 3f;

    private Vector2 posAnterior;
    private bool muerto = false;   // evita procesar dos veces en el mismo frame

    void Start()
    {
        posAnterior = transform.position;
        Destroy(gameObject, tiempoVida);
    }

    void FixedUpdate()
    {
        if (muerto) return;

        // Barrido anti-atravesado: revisa todo lo que hay entre donde estaba y donde esta
        Vector2 actual = transform.position;
        Vector2 delta = actual - posAnterior;
        float dist = delta.magnitude;
        if (dist > 0.0001f)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(posAnterior, delta / dist, dist);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
                if (Procesar(h.collider)) return;
        }
        posAnterior = actual;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Procesar(collision);
    }

    /// <summary>Devuelve true si la bala se destruyo con este contacto.</summary>
    bool Procesar(Collider2D col)
    {
        if (muerto) return true;
        if (col == null || col.isTrigger) return false;   // monedas, zonas, otras balas
        if (col.CompareTag("enemy")) return false;        // no choca con otros enemigos

        if (col.CompareTag("Player"))
        {
            Player player = col.GetComponent<Player>();
            if (player != null)
                player.RecibirDano((int)dano);
            muerto = true;
            Destroy(gameObject);
            return true;
        }

        // Obstaculo: si es una caja destructible, la daña (las rocas solo frenan la bala)
        Obstaculo obst = col.GetComponent<Obstaculo>();
        if (obst != null)
            obst.RecibirDano(dano);

        // Pared u obstaculo: la bala muere aqui
        muerto = true;
        Destroy(gameObject);
        return true;
    }
}
