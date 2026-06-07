using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public float velocidad = 5f;
    
    private Rigidbody2D rb;
    private Vector2 direccionMovimiento;
    public Animator animator;

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

        ActualizarAnimacion(movX, movY);
    }

   void ActualizarAnimacion(float movX, float movY)
    {
        animator.SetBool("up",        false);
        animator.SetBool("down",      false);
        animator.SetBool("left",      false);
        animator.SetBool("right",     false);
        animator.SetBool("leftUp",    false);
        animator.SetBool("rightUp",   false);
        animator.SetBool("leftDown",  false);
        animator.SetBool("rightDown", false);

        if      (movX > 0 && movY > 0) animator.SetBool("rightUp",   true);
        else if (movX < 0 && movY > 0) animator.SetBool("leftUp",    true);
        else if (movX > 0 && movY < 0) animator.SetBool("rightDown", true);
        else if (movX < 0 && movY < 0) animator.SetBool("leftDown",  true);
        else if (movX > 0)             animator.SetBool("right",     true);
        else if (movX < 0)             animator.SetBool("left",      true);
        else if (movY > 0)             animator.SetBool("up",        true);
        else if (movY < 0)             animator.SetBool("down",      true);
        // todos en false → idle
    }

    void FixedUpdate()
    {
        rb.velocity = direccionMovimiento * velocidad;
    }
}