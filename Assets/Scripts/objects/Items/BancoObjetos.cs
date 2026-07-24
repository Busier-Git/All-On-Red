using System.Collections.Generic;
using UnityEngine;

/// <summary>Definicion de un objeto (item) que mejora el disparo del jugador.</summary>
public class DefObjeto
{
    public string id;
    public string nombre;
    public Color color;
    public int peso;        // mas peso = mas comun. Los raros tienen peso bajo.
    public string descripcion;
}

/// <summary>
/// Banco de objetos del juego con sus pools por nivel.
/// - Nivel 1: objetos basicos + raros (Rayo Carmesi y Corazon Sagrado con poco peso).
/// - Nivel 2: "otros objetos": entra el Lanzagranadas (exclusivo) y suben un poco
///   los pesos de los raros; sale el Disparo Doble (exclusivo del nivel 1).
/// Puedes editar pesos y pools aqui mismo.
/// </summary>
public static class BancoObjetos
{
    static readonly DefObjeto[] TODOS =
    {
        new DefObjeto{ id="dano_up",     nombre="Daño+",           color=new Color(0.95f,0.45f,0.15f), peso=10, descripcion="+0.3 de daño" },
        new DefObjeto{ id="vel_up",      nombre="Balas Veloces",   color=new Color(0.25f,0.85f,0.95f), peso=10, descripcion="+3 velocidad de bala" },
        new DefObjeto{ id="cadencia_up", nombre="Lágrimas+",       color=new Color(0.45f,0.60f,0.95f), peso=10, descripcion="Disparas más seguido" },
        new DefObjeto{ id="doble",       nombre="Disparo Doble",   color=new Color(0.95f,0.90f,0.30f), peso=9,  descripcion="+1 bala por disparo" },
        new DefObjeto{ id="quad",        nombre="Disparo Cuádruple", color=new Color(0.40f,0.90f,0.40f), peso=7, descripcion="+3 balas por disparo" },
        new DefObjeto{ id="granadas",    nombre="Lanzagranadas",   color=new Color(0.45f,0.55f,0.30f), peso=7,  descripcion="Granadas explosivas (¡cuidado de cerca!)" },
        new DefObjeto{ id="brimstone",   nombre="Rayo Carmesí",    color=new Color(0.75f,0.10f,0.12f), peso=3,  descripcion="Rayo que atraviesa, +1.0 daño" },
        new DefObjeto{ id="sagrado",     nombre="Corazón Sagrado", color=new Color(1.00f,0.85f,0.90f), peso=2,  descripcion="Balas teledirigidas, +1.5 daño" },
    };

    // Pools por nivel (ids). Edita esto para cambiar que aparece en cada piso.
    static readonly string[] POOL_NIVEL_1 = { "dano_up", "vel_up", "cadencia_up", "doble", "quad", "brimstone", "sagrado" };
    static readonly string[] POOL_NIVEL_2 = { "dano_up", "vel_up", "cadencia_up", "quad", "granadas", "brimstone", "sagrado" };

    public static DefObjeto Def(string id)
    {
        foreach (var d in TODOS) if (d.id == id) return d;
        return null;
    }

    /// <summary>Elige un objeto al azar del pool del nivel, respetando los pesos (rareza).</summary>
    public static DefObjeto ElegirAlAzar(int nivel)
    {
        string[] pool = (nivel >= 2) ? POOL_NIVEL_2 : POOL_NIVEL_1;

        // En nivel 2 los raros pesan un poco mas (es mas profundo, mejor botin)
        int total = 0;
        List<DefObjeto> defs = new List<DefObjeto>();
        List<int> pesos = new List<int>();
        foreach (var id in pool)
        {
            DefObjeto d = Def(id);
            if (d == null) continue;
            int p = d.peso;
            if (nivel >= 2 && (id == "brimstone" || id == "sagrado")) p += 1;
            defs.Add(d); pesos.Add(p); total += p;
        }
        if (total <= 0) return null;

        int r = Random.Range(0, total);
        for (int i = 0; i < defs.Count; i++)
        {
            r -= pesos[i];
            if (r < 0) return defs[i];
        }
        return defs[defs.Count - 1];
    }
}
