using UnityEngine;

public class Move : MonoBehaviour
{
    public float velocidad = 5f;

    private Rigidbody2D rb;
    private Vector2 direccionMovimiento;
    private Player player;   // para sumar el empujon al recibir daño
    public Animator animator;

    // Hacia donde "mira" el jugador. Empieza mirando hacia abajo.
    private Vector2 ultimaDir = Vector2.down;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
    }

    void Update()
    {
        // Movimiento: W A S D
        float movX = 0f, movY = 0f;
        if (Input.GetKey(KeyCode.D)) movX += 1f;
        if (Input.GetKey(KeyCode.A)) movX -= 1f;
        if (Input.GetKey(KeyCode.W)) movY += 1f;
        if (Input.GetKey(KeyCode.S)) movY -= 1f;

        direccionMovimiento = new Vector2(movX, movY).normalized;

        ActualizarAnimacion(movX, movY);
    }

    void ActualizarAnimacion(float movX, float movY)
    {
        if (animator == null) return;

        // Direccion de DISPARO (flechas) -> tiene prioridad para la orientacion
        Vector2 disparo = Vector2.zero;
        if (Input.GetKey(KeyCode.UpArrow))    disparo.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow))  disparo.y -= 1f;
        if (Input.GetKey(KeyCode.LeftArrow))  disparo.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) disparo.x += 1f;

        Vector2 mov = new Vector2(movX, movY);
        bool activo = (mov != Vector2.zero) || (disparo != Vector2.zero);

        // Orientacion: si disparo, miro hacia el disparo; si no, hacia donde me muevo;
        // si estoy quieto, conservo la ultima direccion (no vuelve a mirar arriba).
        if (disparo != Vector2.zero) ultimaDir = disparo;
        else if (mov != Vector2.zero) ultimaDir = mov;

        AplicarDireccion(ultimaDir);

        // Anima al moverse o disparar; al quedar quieto congela la pose mirando esa direccion.
        if (activo) animator.speed = 1f;
        else animator.speed = animator.IsInTransition(0) ? 1f : 0f;
    }

    // Activa SOLO el bool de la direccion indicada (los demas en false).
    void AplicarDireccion(Vector2 d)
    {
        animator.SetBool("up",        false);
        animator.SetBool("down",      false);
        animator.SetBool("left",      false);
        animator.SetBool("right",     false);
        animator.SetBool("leftUp",    false);
        animator.SetBool("rightUp",   false);
        animator.SetBool("leftDown",  false);
        animator.SetBool("rightDown", false);

        bool arriba = d.y > 0.5f, abajo = d.y < -0.5f, izq = d.x < -0.5f, der = d.x > 0.5f;

        if      (arriba && der) animator.SetBool("rightUp",   true);
        else if (arriba && izq) animator.SetBool("leftUp",    true);
        else if (abajo && der)  animator.SetBool("rightDown", true);
        else if (abajo && izq)  animator.SetBool("leftDown",  true);
        else if (der)           animator.SetBool("right",     true);
        else if (izq)           animator.SetBool("left",      true);
        else if (arriba)        animator.SetBool("up",        true);
        else if (abajo)         animator.SetBool("down",      true);
    }

    void FixedUpdate()
    {
        // Movimiento normal + empujon (knockback) que aporta Player al recibir daño
        Vector2 extra = (player != null) ? player.Empuje : Vector2.zero;
        rb.velocity = direccionMovimiento * velocidad + extra;
    }
}
