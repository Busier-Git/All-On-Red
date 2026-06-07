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
        // Comprobamos si el proyectil impactó contra un enemigo (usando el tag correcto)
        if (collision.gameObject.CompareTag("enemy"))
        {
            // 1. Buscamos si el enemigo tiene el script original "Enemy"
            Enemy scriptEnemy = collision.gameObject.GetComponent<Enemy>();
            if (scriptEnemy != null)
            {
                scriptEnemy.RecibirDano(dano);
            }

            // 2. Buscamos si el enemigo tiene tu nuevo script "Enemigo" (Enemy2)
            Enemigo scriptEnemigo = collision.gameObject.GetComponent<Enemigo>();
            if (scriptEnemigo != null)
            {
                scriptEnemigo.RecibirDano(dano);
            }

            // Destruimos el proyectil tras el impacto para que no los atraviese
            Destroy(gameObject);
        }
    }
}