using System.Collections.Generic;
using UnityEngine;

public enum TipoSala { Inicio, Normal, Jefe, Tesoro, Tienda }

/// <summary>
/// Va en la raiz de cada sala (la crea el GeneradorMapa por codigo en runtime).
/// Maneja el combate: al entrar con enemigos cierra las puertas; al limpiarla las abre.
/// </summary>
public class Habitacion : MonoBehaviour
{
    [HideInInspector] public Vector2Int origenCelda;
    [HideInInspector] public Vector2Int tamCeldas;
    [HideInInspector] public TipoSala tipo;
    [HideInInspector] public Vector2 tamMundo;   // tamaño fisico de la sala (ancho, alto)

    public bool Limpia { get; private set; }
    public bool Visitada { get; private set; }

    private List<Puerta> puertas = new List<Puerta>();
    private List<GameObject> enemigos = new List<GameObject>();
    private Transform contenedorEnemigos;
    private bool enCombate;

    /// <summary>La llama el generador cuando termina de construir la sala.</summary>
    public void Configurar(List<Puerta> puertas, Transform contenedorEnemigos, List<GameObject> enemigos)
    {
        this.puertas = puertas ?? new List<Puerta>();
        this.contenedorEnemigos = contenedorEnemigos;
        this.enemigos = enemigos ?? new List<GameObject>();

        if (contenedorEnemigos != null)
            contenedorEnemigos.gameObject.SetActive(false);  // enemigos dormidos hasta entrar

        Limpia = this.enemigos.Count == 0;
    }

    /// <summary>La llama ControladorSalas cuando el jugador entra a esta sala.</summary>
    public void AlEntrar()
    {
        Visitada = true;
        if (enCombate || Limpia) return;

        enCombate = true;
        if (contenedorEnemigos != null)
            contenedorEnemigos.gameObject.SetActive(true);  // despierta a los enemigos
        CambiarPuertas(false);                              // cierra
    }

    void Update()
    {
        if (!enCombate) return;

        enemigos.RemoveAll(e => e == null);   // los muertos se destruyen -> null
        if (enemigos.Count == 0)
        {
            enCombate = false;
            Limpia = true;
            CambiarPuertas(true);             // abre
        }
    }

    void CambiarPuertas(bool abrir)
    {
        foreach (var p in puertas)
        {
            if (p == null || p.EsMuro) continue;
            if (abrir) p.Abrir(); else p.Cerrar();
        }
    }

    public Vector3 CentroMundo => transform.position;
}
