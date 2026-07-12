using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [Header("Configuración")]
    /// <summary>Daño decimal que inflige a los enemigos.</summary>
    public float dano = 0.5f;
    /// <summary>Tiempo de vida máximo antes de desaparecer si no impacta nada.</summary>
    public float tiempoVida = 3f;

    void Start()
    {
        // Se destruye solo tras unos segundos para no acumular basura en la escena
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignoramos otros triggers (monedas, zonas de sala, otras balas) y al propio jugador
        if (collision.isTrigger) return;
        if (collision.CompareTag("Player")) return;

        // Si lo que tocamos puede recibir daño (enemigos, jefe, obstaculos destructibles), se lo hacemos
        IDanable danable = collision.GetComponent<IDanable>();
        if (danable != null)
            danable.RecibirDano(dano);

        // Choco contra algo solido (enemigo, obstaculo o pared) -> el proyectil se destruye
        Destroy(gameObject);
    }
}