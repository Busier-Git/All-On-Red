using UnityEngine;

/// <summary>
/// BANCO DE AUDIO: un unico lugar para asignar TODA la musica y los efectos.
///
/// Como usarlo:
///  1. En el Project: click derecho > Create > Roguelike > Banco de Audio.
///  2. Guarda el asset con el nombre "BancoAudio" dentro de una carpeta
///     llamada "Resources" (ej: Assets/Resources/BancoAudio.asset).
///  3. Arrastra tus clips a los campos. Lo que dejes vacio simplemente no suena.
///
/// No hay que tocar ninguna escena: se carga solo. La musica cambia sola segun
/// la escena (Menu / test / 2) y el volumen se ajusta desde el menu de pausa (ESC).
/// </summary>
[CreateAssetMenu(fileName = "BancoAudio", menuName = "Roguelike/Banco de Audio")]
public class BancoAudio : ScriptableObject
{
    [Header("Musica de fondo (se repite en bucle)")]
    public AudioClip musicaMenu;      // escena Menu
    public AudioClip musicaNivel1;    // escena test
    public AudioClip musicaNivel2;    // escena 2

    [Header("Efectos del jugador")]
    public AudioClip disparo;         // disparo normal
    public AudioClip disparoRayo;     // rayo Brimstone
    public AudioClip disparoGranada;  // lanzar granada
    public AudioClip explosion;       // boom de la granada
    public AudioClip jugadorDano;     // el jugador recibe daño

    [Header("Efectos de enemigos / jefes")]
    public AudioClip enemigoMuere;
    public AudioClip jefeRayo;        // el jefe del nivel 2 dispara su rayo
    public AudioClip jefeTeleport;    // el jefe del nivel 2 se teletransporta

    [Header("Pickups y puertas")]
    public AudioClip recogerMoneda;
    public AudioClip recogerObjeto;
    public AudioClip recogerCorazon;
    public AudioClip abrirPuerta;

    [Header("Efectos extra por id (opcional)")]
    public EntradaClip[] extra;

    [System.Serializable]
    public class EntradaClip { public string id; public AudioClip clip; }

    /// <summary>Devuelve el clip segun un id de texto (lo usan los scripts del juego).</summary>
    public AudioClip EfectoPorId(string id)
    {
        switch (id)
        {
            case "disparo":        return disparo;
            case "rayo":           return disparoRayo;
            case "granada":        return disparoGranada;
            case "explosion":      return explosion;
            case "jugador_dano":   return jugadorDano;
            case "enemigo_muere":  return enemigoMuere;
            case "jefe_rayo":      return jefeRayo;
            case "jefe_teleport":  return jefeTeleport;
            case "moneda":         return recogerMoneda;
            case "objeto":         return recogerObjeto;
            case "corazon":        return recogerCorazon;
            case "puerta":         return abrirPuerta;
        }
        if (extra != null)
            foreach (var e in extra)
                if (e != null && e.id == id) return e.clip;
        return null;
    }

    // ------------------ carga automatica ------------------
    static BancoAudio _instancia;
    static bool _buscado;

    /// <summary>Devuelve el banco (Assets/Resources/BancoAudio) o null si no existe.</summary>
    public static BancoAudio Cargar()
    {
        if (!_buscado)
        {
            _instancia = Resources.Load<BancoAudio>("BancoAudio");
            _buscado = true;
            if (_instancia == null)
                Debug.Log("BancoAudio: no hay asset en Resources/BancoAudio. El juego funciona sin sonido. " +
                          "Crea uno con Create > Roguelike > Banco de Audio dentro de Assets/Resources.");
        }
        return _instancia;
    }
}
