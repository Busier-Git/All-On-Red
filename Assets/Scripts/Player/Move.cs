using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
   public float velocidad = 5f;
    
    private Rigidbody2D rb;
    private Vector2 direccionMovimiento;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float movX = Input.GetAxisRaw("Horizontal");
        float movY = Input.GetAxisRaw("Vertical");
        direccionMovimiento = new Vector2(movX, movY).normalized;
    }
    void FixedUpdate()
    {
        rb.velocity = direccionMovimiento * velocidad;
    }
}
