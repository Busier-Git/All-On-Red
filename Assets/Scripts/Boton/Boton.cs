using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boton : MonoBehaviour
{
   private AudioSource origenDeSonido;

    void Start()
    {
        origenDeSonido = GetComponent<AudioSource>();
    }
    void OnMouseDown()
    {
        if (origenDeSonido != null)
        {
            origenDeSonido.Play();
        }
    }
}
