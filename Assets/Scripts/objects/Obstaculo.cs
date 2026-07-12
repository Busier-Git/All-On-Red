using UnityEngine;

/// <summary>
/// Obstaculo dentro de una sala. Necesita un Collider2D NO trigger (solido) para
/// bloquear el paso. Si es destructible, los proyectiles del jugador lo rompen.
/// </summary>
public class Obstaculo : MonoBehaviour, IDanable
{
    public bool destructible = false;
    public float vida = 1f;
    [Tooltip("Opcional: soltar monedas al romperse")]
    public Botin botin;

    public void RecibirDano(float cantidad)
    {
        if (!destructible) return;   // los indestructibles ignoran el daño (solo frenan la bala)
        vida -= cantidad;
        if (vida <= 0f)
        {
            if (botin != null) botin.Soltar();
            Destroy(gameObject);
        }
    }
}
