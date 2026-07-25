using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Sistema de Vida")]
    public int vidaMaxima = 5;
    private int vidaActual;

    [Header("UI Vidas")]
    public UIVida uiVidas;

    [Header("Sistema de Disparo")]
    public GameObject prefabProyectil;
    public float velocidadProyectil = 12f;
    public float cadenciaDisparo = 0.3f;
    private float tiempoSiguienteDisparo = 0f;

    [Header("Al recibir daño")]
    public float duracionInvulnerable = 1.5f;   // segundos sin poder recibir daño
    public float fuerzaEmpuje = 8f;             // empujon hacia atras al ser golpeado
    private float invulnerableHasta = -999f;
    private Vector2 empuje = Vector2.zero;
    private SpriteRenderer spritePropio;

    /// <summary>Empujon actual (lo suma Move.cs a la velocidad del Rigidbody).</summary>
    public Vector2 Empuje => empuje;
    public bool EsInvulnerable => Time.time < invulnerableHasta;

    // ---------- Estadisticas que dan los OBJETOS (se aplican al recogerlos) ----------
    [Header("Objetos (solo lectura, los cambia AplicarObjeto)")]
    public float danoExtra = 0f;          // Daño+ / raros
    public float multCadencia = 1f;       // Lágrimas+ (menor = dispara mas seguido)
    public int disparosExtra = 0;         // Doble (+1) / Cuádruple (+3)
    public bool teledirigido = false;     // Corazón Sagrado
    public bool rayoBrimstone = false;    // Rayo Carmesí
    public bool lanzaGranadas = false;    // Lanzagranadas

    private float danoBaseProyectil = 0.5f;
    private readonly List<string> objetosRecogidos = new List<string>();
    private string bufferTrucos = "";   // para los trucos de teclado (brim, sacred)

    public int VidaActual => vidaActual;
    public List<string> Objetos => objetosRecogidos;

    void Start()
    {
        vidaActual = vidaMaxima;
        spritePropio = GetComponentInChildren<SpriteRenderer>();
        MenuPausa.Asegurar();   // crea el menu de pausa (ESC) si no existe

        if (prefabProyectil != null)
        {
            Proyectil p = prefabProyectil.GetComponent<Proyectil>();
            if (p != null) danoBaseProyectil = p.dano;
        }

        // Si venimos del nivel anterior (portal del jefe), recuperamos vida y objetos
        if (EstadoPartida.enCurso)
        {
            foreach (string id in EstadoPartida.objetos)
                AplicarObjeto(id);
            vidaActual = Mathf.Clamp(EstadoPartida.vida, 1, vidaMaxima);
        }

        if (uiVidas != null)
            uiVidas.ActualizarVidas(vidaActual, vidaMaxima);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // pausado: no disparar

        // El empujon se desvanece solo (Move.cs lo suma a la velocidad)
        if (empuje != Vector2.zero)
            empuje = Vector2.MoveTowards(empuje, Vector2.zero, (fuerzaEmpuje / 0.25f) * Time.deltaTime);

        RevisarTrucos();

        if (Time.time >= tiempoSiguienteDisparo)
        {
            // Disparos diagonales
            if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.RightArrow))
                Disparar(new Vector2(1, 1).normalized);
            else if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftArrow))
                Disparar(new Vector2(-1, 1).normalized);
            else if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.RightArrow))
                Disparar(new Vector2(1, -1).normalized);
            else if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftArrow))
                Disparar(new Vector2(-1, -1).normalized);
            else if (Input.GetKey(KeyCode.UpArrow)) Disparar(Vector2.up);
            else if (Input.GetKey(KeyCode.DownArrow)) Disparar(Vector2.down);
            else if (Input.GetKey(KeyCode.LeftArrow)) Disparar(Vector2.left);
            else if (Input.GetKey(KeyCode.RightArrow)) Disparar(Vector2.right);
        }
    }

    // ============================ DISPARO ============================
    private void Disparar(Vector2 direccion)
    {
        int cantidad = 1 + disparosExtra;                 // Doble / Cuádruple
        float dano = danoBaseProyectil + danoExtra;
        float cadencia = cadenciaDisparo * multCadencia;

        if (rayoBrimstone)
        {
            // El rayo dispara mas lento pero pega fuerte y atraviesa.
            // Sinergias: Doble/Cuadruple -> VARIOS rayos en abanico (como Mutant Spider + Brimstone);
            // sagrado -> rayo blanco ondulado que persigue enemigos (como The Hanged Man).
            float danoRayo = dano * 2f;
            for (int i = 0; i < cantidad; i++)
                RayoJugador.Disparar(transform.position, DireccionConAbanico(direccion, i, cantidad), danoRayo, 0.9f, teledirigido);
            GestorAudio.Efecto("rayo");
            cadencia *= 2.6f;
        }
        else if (lanzaGranadas)
        {
            // Granadas de rango medio. Sinergias: multishot -> mas granadas; sagrado -> teledirigidas.
            for (int i = 0; i < cantidad; i++)
                Granada.Lanzar(transform.position, DireccionConAbanico(direccion, i, cantidad), dano * 1.6f, velocidadProyectil * 0.75f, teledirigido);
            GestorAudio.Efecto("granada");
            cadencia *= 1.7f;
        }
        else
        {
            if (prefabProyectil == null) return;
            GestorAudio.Efecto("disparo");
            for (int i = 0; i < cantidad; i++)
            {
                Vector2 dir = DireccionConAbanico(direccion, i, cantidad);
                GameObject proyectil = Instantiate(prefabProyectil, transform.position, Quaternion.identity);

                Proyectil pr = proyectil.GetComponent<Proyectil>();
                if (pr != null)
                {
                    pr.dano = dano;
                    pr.teledirigido = teledirigido;
                }

                Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
                if (rbProyectil != null)
                    rbProyectil.velocity = dir * velocidadProyectil;
            }
        }

        tiempoSiguienteDisparo = Time.time + cadencia;
    }

    /// <summary>Reparte 'total' disparos en un abanico chico alrededor de la direccion base.</summary>
    private Vector2 DireccionConAbanico(Vector2 baseDir, int indice, int total)
    {
        if (total <= 1) return baseDir;
        float apertura = Mathf.Min(9f * (total - 1), 36f);
        float ang = Mathf.Lerp(-apertura / 2f, apertura / 2f, (float)indice / (total - 1));
        float r = ang * Mathf.Deg2Rad;
        float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
        return new Vector2(baseDir.x * cos - baseDir.y * sin, baseDir.x * sin + baseDir.y * cos).normalized;
    }

    // ============================ TRUCOS ============================
    /// <summary>Trucos de teclado: escribe "brim" o "sacred" para recibir el objeto.</summary>
    private void RevisarTrucos()
    {
        string tecleado = Input.inputString;
        if (string.IsNullOrEmpty(tecleado)) return;

        foreach (char c in tecleado)
            if (char.IsLetter(c))
                bufferTrucos += char.ToLowerInvariant(c);

        if (bufferTrucos.Length > 12)
            bufferTrucos = bufferTrucos.Substring(bufferTrucos.Length - 12);

        // Los trucos siguen siendo "brim" y "sacred" y dan los mismos objetos;
        // solo el texto usa el nombre actual del banco de objetos (asi nunca se desincroniza).
        if (bufferTrucos.EndsWith("brim"))
        {
            bufferTrucos = "";
            AplicarObjeto("brimstone");
            AvisoTruco("¡" + NombreObjeto("brimstone") + "!");
        }
        else if (bufferTrucos.EndsWith("sacred"))
        {
            bufferTrucos = "";
            AplicarObjeto("sagrado");
            AvisoTruco("¡" + NombreObjeto("sagrado") + "!");
        }
    }

    private string NombreObjeto(string id)
    {
        DefObjeto d = BancoObjetos.Def(id);
        return (d != null) ? d.nombre : id;
    }

    private void AvisoTruco(string mensaje)
    {
        TextMesh tm = UtilJuego.CrearTexto(mensaje, transform.position + Vector3.up * 1.4f, null, new Color(1f, 0.85f, 0.3f), 3f);
        if (tm != null) Destroy(tm.gameObject, 1.5f);
    }

    // ============================ OBJETOS ============================
    /// <summary>
    /// Aplica el efecto de un objeto (lo llama el pedestal al recogerlo y
    /// tambien la carga del nivel 2 para re-aplicar lo que ya tenias).
    /// </summary>
    public void AplicarObjeto(string id)
    {
        switch (id)
        {
            case "dano_up":     danoExtra += 0.3f; break;
            case "vel_up":      velocidadProyectil += 3f; break;
            case "cadencia_up": multCadencia = Mathf.Max(0.3f, multCadencia * 0.75f); break;
            case "doble":       disparosExtra += 1; break;
            case "quad":        disparosExtra += 3; multCadencia *= 1.1f; break;
            case "brimstone":   rayoBrimstone = true; danoExtra += 1.0f; break;   // raro: mucho daño
            case "sagrado":     teledirigido = true; danoExtra += 1.5f; break;    // el mas raro: el que mas daño da
            case "granadas":    lanzaGranadas = true; danoExtra += 0.5f; break;
            default:
                Debug.LogWarning("Objeto desconocido: " + id);
                return;
        }
        objetosRecogidos.Add(id);
    }

    // ============================ VIDA ============================
    public void Curar(int cantidad)
    {
        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
        if (uiVidas != null)
            uiVidas.ActualizarVidas(vidaActual, vidaMaxima);
    }

    public void RecibirDano(int cantidad)
    {
        if (EsInvulnerable) return;   // 1.5 s de gracia tras cada golpe
        invulnerableHasta = Time.time + duracionInvulnerable;

        vidaActual -= cantidad;
        GestorAudio.Efecto("jugador_dano");
        if (uiVidas != null)
            uiVidas.ActualizarVidas(vidaActual, vidaMaxima);

        if (vidaActual <= 0)
        {
            Morir();
            return;
        }

        Empujar();
        StartCoroutine(ParpadearInvulnerable());
    }

    /// <summary>Empujon hacia atras: se aleja de lo que lo golpeo (enemigo, bala o pincho mas cercano).</summary>
    private void Empujar()
    {
        Transform origen = BuscarOrigenGolpe();
        Vector2 dir;
        if (origen != null)
            dir = ((Vector2)transform.position - (Vector2)origen.position).normalized;
        else
            dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.down;

        empuje = dir * fuerzaEmpuje;
    }

    private Transform BuscarOrigenGolpe()
    {
        Transform mejor = null;
        float mejorDist = 5f;   // solo cuenta lo que este cerca

        foreach (GameObject e in GameObject.FindGameObjectsWithTag("enemy"))
        {
            if (e == null || !e.activeInHierarchy) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < mejorDist) { mejorDist = d; mejor = e.transform; }
        }
        foreach (ProyectilEnemigo b in FindObjectsOfType<ProyectilEnemigo>())
        {
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < mejorDist) { mejorDist = d; mejor = b.transform; }
        }
        foreach (spike s in FindObjectsOfType<spike>())
        {
            float d = Vector2.Distance(transform.position, s.transform.position);
            if (d < mejorDist) { mejorDist = d; mejor = s.transform; }
        }
        return mejor;
    }

    private IEnumerator ParpadearInvulnerable()
    {
        while (EsInvulnerable)
        {
            if (spritePropio != null) spritePropio.enabled = !spritePropio.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        if (spritePropio != null) spritePropio.enabled = true;
    }

    private void Morir()
    {
        EstadoPartida.Limpiar();   // al morir, la proxima partida empieza de cero
        if (GameManager.Instance != null)
            GameManager.Instance.MostrarMenuMuerte();
        gameObject.SetActive(false);
    }
}
