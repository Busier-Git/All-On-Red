using UnityEngine;

/// <summary>
/// Va en cada uno de los 4 huecos (puertas) de una sala.
/// Estados:
///   - Muro: es una pared solida permanente (no hay sala vecina por ese lado).
///   - Puerta abierta: se puede pasar (barrera desactivada).
///   - Puerta cerrada: bloquea el paso durante el combate (barrera activada).
/// </summary>
public class Puerta : MonoBehaviour
{
    [Header("Barrera fisica solida (Collider2D que bloquea el paso al cerrar)")]
    public Collider2D barrera;

    [Header("Visual (opcional). Si no asignas sprites igual funciona.)")]
    public SpriteRenderer sprite;
    public Sprite spriteAbierta;
    public Sprite spriteCerrada;
    public Sprite spriteMuro;

    public bool Abierta { get; private set; } = true;
    public bool EsMuro { get; private set; } = false;

    /// <summary>Convierte este hueco en una puerta normal (empieza abierta).</summary>
    public void VolverPuerta()
    {
        EsMuro = false;
        Abrir();
    }

    /// <summary>Convierte este hueco en una pared solida permanente.</summary>
    public void VolverMuro()
    {
        EsMuro = true;
        Abierta = false;
        if (barrera != null) barrera.enabled = true;
        if (sprite != null && spriteMuro != null) sprite.sprite = spriteMuro;
    }

    public void Abrir()
    {
        if (EsMuro) return;
        Abierta = true;
        if (barrera != null) barrera.enabled = false;
        if (sprite != null && spriteAbierta != null) sprite.sprite = spriteAbierta;
    }

    public void Cerrar()
    {
        if (EsMuro) return;
        Abierta = false;
        if (barrera != null) barrera.enabled = true;
        if (sprite != null && spriteCerrada != null) sprite.sprite = spriteCerrada;
    }
}
