using UnityEngine;

/// <summary>
/// Va en un hijo de la sala con un Collider2D marcado como "Is Trigger",
/// del tamano del piso (sin tapar las puertas).
/// Cuando el jugador entra a esta zona, le avisa al ControladorSalas.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DetectorSala : MonoBehaviour
{
    [Tooltip("La sala a la que pertenece esta zona (normalmente el objeto padre)")]
    public Habitacion habitacion;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        // intenta autocompletar la referencia con el padre
        if (habitacion == null) habitacion = GetComponentInParent<Habitacion>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (ControladorSalas.Instancia != null && habitacion != null)
            ControladorSalas.Instancia.EntrarSala(habitacion);
    }
}
