using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spike : MonoBehaviour
{
    // OnCollisionEnter2D se activa cuando dos objetos sólidos chocan
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Verificamos que el objeto con el que chocamos sea el Jugador
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. Extraemos el script 'Player' del objeto que nos chocó
            Player scriptJugador = collision.gameObject.GetComponent<Player>();
            
            // 3. Si el script existe, llamamos a la función de daño
            if (scriptJugador != null)
            {
                scriptJugador.RecibirDano(1);
            }
        }
    }
}