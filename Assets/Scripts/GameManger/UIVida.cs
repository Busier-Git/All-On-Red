using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVida : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite vidaLlena;
    public Sprite vidaVacia;

    [Header("Contenedor")]
    public Image[] iconosVida; // Arrastra aquí los 5 Image del Canvas

    /// <summary>
    /// Actualiza los iconos según la vida actual del jugador.
    /// </summary>
    public void ActualizarVidas(int vidaActual, int vidaMaxima)
    {
        for (int i = 0; i < iconosVida.Length; i++)
        {
            if (iconosVida[i] != null)
            {
                if (i < vidaActual)
                    iconosVida[i].sprite = vidaLlena;
                else
                    iconosVida[i].sprite = vidaVacia;
            }
        }
    }
}
