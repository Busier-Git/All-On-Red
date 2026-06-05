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
        // 1. Leemos exclusivamente las teclas W, A, S, D
        float movX = 0f;
        float movY = 0f;

        if (Input.GetKey(KeyCode.D)) movX += 1f;
        if (Input.GetKey(KeyCode.A)) movX -= 1f;
        if (Input.GetKey(KeyCode.W)) movY += 1f;
        if (Input.GetKey(KeyCode.S)) movY -= 1f;

        direccionMovimiento = new Vector2(movX, movY).normalized;
    }

    void FixedUpdate()
    {
        rb.velocity = direccionMovimiento * velocidad;
    }
}