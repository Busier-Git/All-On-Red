using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class coin : MonoBehaviour
{
    public int valor = 1;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            // Le avisa al GameManager que se recogió una moneda
            if (GameManager.Instance != null)
                GameManager.Instance.AgregarMonedas(valor);

            Destroy(gameObject);
        }
    }
}
