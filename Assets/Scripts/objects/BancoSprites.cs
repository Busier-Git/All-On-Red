using UnityEngine;

/// <summary>
/// BANCO DE SPRITES: un unico lugar para asignar TODOS los sprites del juego.
///
/// Como usarlo:
///  1. En el Project: click derecho > Create > Roguelike > Banco de Sprites.
///  2. Guarda el asset con el nombre "BancoSprites" dentro de una carpeta
///     llamada "Resources" (ej: Assets/Resources/BancoSprites.asset).
///  3. Arrastra tus sprites a los campos. Los que dejes vacios seguiran
///     usando los cuadros de color de siempre.
///
/// No hay que tocar ninguna escena: se carga solo (Resources.Load) y sirve
/// para el nivel 1 y el nivel 2 a la vez. El tamaño en el juego se ajusta
/// automaticamente sin importar la resolucion del sprite.
/// </summary>
[CreateAssetMenu(fileName = "BancoSprites", menuName = "Roguelike/Banco de Sprites")]
public class BancoSprites : ScriptableObject
{
    [Header("Enemigos generados por codigo")]
    public Sprite enemigoPerseguidor;   // rojo: te persigue
    public Sprite enemigoDisparador;    // naranjo: te sigue y dispara
    public Sprite enemigoDiagonal;      // morado: deambula y dispara en X
    public Sprite enemigoTorreta;       // celeste: fijo, dispara con linea de vision

    [Header("Jefes")]
    public Sprite jefeNivel1;           // jefe de los sprays
    public Sprite jefeNivel2;           // el Adversario (rayo + teletransporte)

    [Header("Obstaculos")]
    public Sprite roca;                 // indestructible
    public Sprite caja;                 // destructible

    [Header("Suelo, paredes y decoracion (NIVEL 1)")]
    public Sprite suelo;                // se repite (tiled) por toda la sala
    public Sprite pared;                // se repite en los muros
    [Tooltip("Detalles que se reparten al azar por el piso de cada sala (manchas, grietas, plantas...)")]
    public Sprite[] decoraciones;

    [Header("Suelo, paredes y decoracion (NIVEL 2) — vacio = usa los del nivel 1")]
    public Sprite sueloNivel2;
    public Sprite paredNivel2;
    public Sprite[] decoracionesNivel2;

    /// <summary>Suelo segun el nivel (nivel 2 usa el suyo; si no tiene, cae al del nivel 1).</summary>
    public Sprite SueloDeNivel(int nivel)
    {
        return (nivel >= 2 && sueloNivel2 != null) ? sueloNivel2 : suelo;
    }

    /// <summary>Pared segun el nivel (nivel 2 usa la suya; si no tiene, cae a la del nivel 1).</summary>
    public Sprite ParedDeNivel(int nivel)
    {
        return (nivel >= 2 && paredNivel2 != null) ? paredNivel2 : pared;
    }

    /// <summary>Decoraciones segun el nivel (nivel 2 usa las suyas; si no, las del nivel 1).</summary>
    public Sprite[] DecoracionesDeNivel(int nivel)
    {
        if (nivel >= 2 && decoracionesNivel2 != null && decoracionesNivel2.Length > 0)
            return decoracionesNivel2;
        return decoraciones;
    }

    [Header("Puertas")]
    public Sprite puertaAbierta;
    public Sprite puertaCerrada;
    public Sprite puertaPeaje;          // puerta dorada del nivel 2
    public Sprite muro;

    [Header("Pickups y otros")]
    public Sprite corazon;
    public Sprite granada;
    public Sprite explosion;
    public Sprite portal;               // portal al siguiente nivel
    public Sprite pedestalBase;         // la base donde flota el objeto

    [Header("Objetos (sprite por id: dano_up, vel_up, cadencia_up, doble, quad, granadas, brimstone, sagrado)")]
    public EntradaObjeto[] objetos;

    [System.Serializable]
    public class EntradaObjeto
    {
        public string id;
        public Sprite sprite;
    }

    public Sprite ObjetoPorId(string id)
    {
        if (objetos == null) return null;
        foreach (var e in objetos)
            if (e != null && e.id == id) return e.sprite;
        return null;
    }

    // ------------------ carga automatica ------------------
    static BancoSprites _instancia;
    static bool _buscado;

    /// <summary>Devuelve el banco (Assets/Resources/BancoSprites) o null si no existe.</summary>
    public static BancoSprites Cargar()
    {
        if (!_buscado)
        {
            _instancia = Resources.Load<BancoSprites>("BancoSprites");
            _buscado = true;
            if (_instancia == null)
                Debug.Log("BancoSprites: no hay asset en Resources/BancoSprites. Se usan los colores por defecto. " +
                          "Crea uno con Create > Roguelike > Banco de Sprites dentro de Assets/Resources.");
        }
        return _instancia;
    }
}
