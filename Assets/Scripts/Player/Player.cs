using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clase principal que controla las acciones del Jugador.
/// Gestiona el movimiento en múltiples direcciones usando el sistema de físicas (Rigidbody2D),
/// el control de las animaciones visuales y el sistema de ataque (disparo de lagrimas).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Movimiento : MonoBehaviour
{
    [Header("Movimiento")]
    
    /// <summary>Velocidad de desplazamiento del jugador por el escenario.</summary>
    public float velocidad = 5f;
    
    /// <summary>Referencia al componente de fisicas del jugador.</summary>
    private Rigidbody2D rb;
    
    /// <summary>Referencia al componente que gestiona las transiciones de animación del sprite.</summary>
    private Animator animator;

    [Header("Disparo")]
    
    /// <summary>Referencia al Prefab del proyectil (lagrima) que el jugador instanciara al atacar.</summary>
    public GameObject prefabDisparo;
    
    /// <summary>Tiempo mínimo en segundos que debe transcurrir entre cada disparo (Cadencia de fuego).</summary>
    public float intervalDisparo = 0.4f;
    
    /// <summary>Efecto de sonido que se reproduce cada vez que el jugador dispara un proyectil.</summary>
    public AudioClip sonidoDisparo; 
    
    /// <summary>Temporizador interno para controlar el tiempo de recarga (cooldown) del disparo.</summary>
    private float timerDisparo = 0f;

    /// <summary>
    /// Método de inicializacion.
    /// Obtiene y almacena automaticamente las referencias a los componentes Rigidbody2D y Animator 
    /// adjuntos al mismo GameObject del jugador.
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Ciclo principal del jugador ejecutado en cada frame.
    /// Procesa de forma continua las fisicas de movimiento, actualiza el temporizador de ataque 
    /// y verifica si el jugador está intentando disparar.
    /// </summary>
    void Update()
    {
        Mover();
        timerDisparo += Time.deltaTime;
        Disparar();
    }

    /// <summary>
    /// Lee las entradas del teclado (teclas W, A, S, D) para calcular el vector de direccion del jugador.
    /// Aplica la velocidad al Rigidbody2D, ajusta la escala visual para voltear el sprite según la direccion 
    /// y actualiza los parametros del Animator para reflejar el movimiento.
    /// </summary>
    private void Mover()
    {
        float x = 0f;
        float y = 0f;

        // Lectura de inputs direccionales
        if (Input.GetKey(KeyCode.W)) y =  1f;
        if (Input.GetKey(KeyCode.S)) y = -1f;
        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x =  1f;

        // Aplicacion de la velocidad al motor de fisicas
        rb.velocity = new Vector2(x, y) * velocidad;

        // Volteo del sprite multiplicando la escala en X por -1 o 1
        if (x > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);   // Derecha → Escala normal
        else if (x < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);  // Izquierda → Escala invertida (espejo)

        // Sincronizacion de variables del Animator para transiciones de estado
        if (animator != null)
        {
            animator.SetBool("runningX", y != 0f);  
            animator.SetBool("runningY", x != 0f);  
        }
    }

    /// <summary>
    /// Lee las entradas del teclado (Flechas direccionales) para determinar si el jugador desea atacar.
    /// Si el tiempo de recarga ha finalizado y se presiona una tecla válida, instancia un proyectil,
    /// le asigna su valor de daño y direccion, reinicia el temporizador y reproduce el sonido de ataque.
    /// </summary>
    private void Disparar()
    {
        // Bloqueo por tiempo de recarga (Cooldown)
        if (timerDisparo < intervalDisparo) return;

        Vector2 dirección = Vector2.zero;
        
        // Determinacion de la direccion del ataque
        if (Input.GetKey(KeyCode.UpArrow))    dirección = Vector2.up;
        if (Input.GetKey(KeyCode.DownArrow))  dirección = Vector2.down;
        if (Input.GetKey(KeyCode.LeftArrow))  dirección = Vector2.left;
        if (Input.GetKey(KeyCode.RightArrow)) dirección = Vector2.right;

        // Si se registro una direccion de disparo valida
        if (dirección != Vector2.zero)
        {
            GameObject proy = Instantiate(prefabDisparo, transform.position, Quaternion.identity);
            /*
            Proyectil p = proy.GetComponent<Proyectil>();
            
            // Asignacion de estadisticas al proyectil instanciado
            p.daño = 3.5f;
            p.Inicializar(dirección, true); // true indica que es un proyectil aliado
            */
            timerDisparo = 0f; // Reinicio del cooldown

            // Reproduccion del efecto de sonido
            AudioSource audio = GetComponent<AudioSource>();
            if (audio != null && sonidoDisparo != null)
            {
                audio.PlayOneShot(sonidoDisparo);
            }
        }
    }
}