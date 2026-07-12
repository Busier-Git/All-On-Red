using UnityEngine;

/// <summary>
/// Utilidad de navegacion para enemigos que persiguen al jugador.
/// Evita obstaculos/paredes con "sensores" (raycasts): si el camino directo esta
/// bloqueado, prueba angulos a ambos lados y devuelve el primero libre, de modo que
/// el enemigo RODEA el obstaculo en vez de quedarse atascado.
/// No es pathfinding completo (puede atascarse en trampas en U), pero funciona muy
/// bien para salas con obstaculos sueltos estilo Isaac.
/// </summary>
public static class Navegacion
{
    static readonly float[] ANGULOS = { 25f, -25f, 50f, -50f, 75f, -75f, 100f, -100f, 130f, -130f };

    /// <summary>
    /// Direccion (normalizada) hacia 'deseada' esquivando obstaculos.
    /// posicion = posicion del enemigo, propio = su GameObject (para ignorarse a si mismo),
    /// radio = radio aprox de su collider, distancia = que tan lejos mira el sensor,
    /// buffer = arreglo reutilizable de RaycastHit2D (p.ej. tamaño 8) para no generar basura.
    /// </summary>
    public static Vector2 DireccionEvitando(Vector2 posicion, Vector2 deseada, GameObject propio,
                                            float radio, float distancia, RaycastHit2D[] buffer)
    {
        if (CaminoLibre(posicion, deseada, propio, radio, distancia, buffer))
            return deseada;

        foreach (float a in ANGULOS)
        {
            Vector2 d = Rotar(deseada, a);
            if (CaminoLibre(posicion, d, propio, radio, distancia, buffer))
                return d;
        }
        return deseada;   // todo bloqueado: ultimo recurso, sigue directo
    }

    static bool CaminoLibre(Vector2 pos, Vector2 dir, GameObject propio, float radio, float dist, RaycastHit2D[] buffer)
    {
        Vector2 origen = pos + dir * (radio + 0.05f);   // empieza fuera del propio collider
        int n = Physics2D.RaycastNonAlloc(origen, dir, buffer, dist);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = buffer[i].collider;
            if (c == null || c.isTrigger) continue;                       // ignora monedas/zonas/balas
            if (c.gameObject == propio) continue;                         // ignora a si mismo
            if (c.CompareTag("Player") || c.CompareTag("enemy")) continue; // ni al jugador ni a otros enemigos
            return false;   // hay una pared u obstaculo en el camino
        }
        return true;
    }

    static Vector2 Rotar(Vector2 v, float grados)
    {
        float r = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
