using UnityEngine;

/// <summary>
/// Lleva el control de en que sala esta el jugador y mueve la camara.
/// Salas chicas (del tamaño de la pantalla): la camara se centra.
/// Salas grandes: SIGUE al jugador pero sin salirse de los limites de la sala
/// (scroll estilo Isaac en habitaciones grandes).
/// </summary>
public class ControladorSalas : MonoBehaviour
{
    public static ControladorSalas Instancia;

    [Header("Camara")]
    public Camera camara;
    public float suavizado = 8f;

    [Header("Jugador (se autocompleta por tag si lo dejas vacio)")]
    public Transform jugador;

    public Habitacion SalaActual { get; private set; }
    public System.Action<Habitacion> AlCambiarSala;   // lo usa el minimapa (Tanda 2b)

    private Vector2 centroSala;
    private Vector2 tamSala = new Vector2(20f, 12f);

    void Awake()
    {
        Instancia = this;
        if (camara == null) camara = Camera.main;
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject pj = GameObject.FindWithTag("Player");
            if (pj != null) jugador = pj.transform;
        }
    }

    public void EntrarSala(Habitacion sala, bool primera = false)
    {
        if (sala == null || sala == SalaActual) return;
        SalaActual = sala;
        centroSala = sala.CentroMundo;
        tamSala = sala.tamMundo;

        sala.AlEntrar();
        if (AlCambiarSala != null) AlCambiarSala(sala);

        if (primera && camara != null)
            camara.transform.position = CalcularPosCamara();
    }

    void LateUpdate()
    {
        if (camara == null || SalaActual == null) return;
        Vector3 destino = CalcularPosCamara();
        camara.transform.position = Vector3.Lerp(camara.transform.position, destino, suavizado * Time.deltaTime);
    }

    Vector3 CalcularPosCamara()
    {
        float z = camara.transform.position.z;
        Vector2 objetivo = (jugador != null) ? (Vector2)jugador.position : centroSala;

        float camHalfH = camara.orthographicSize;
        float camHalfW = camHalfH * camara.aspect;

        float salaHalfW = tamSala.x / 2f;
        float salaHalfH = tamSala.y / 2f;

        float x = (salaHalfW <= camHalfW)
            ? centroSala.x
            : Mathf.Clamp(objetivo.x, centroSala.x - (salaHalfW - camHalfW), centroSala.x + (salaHalfW - camHalfW));

        float y = (salaHalfH <= camHalfH)
            ? centroSala.y
            : Mathf.Clamp(objetivo.y, centroSala.y - (salaHalfH - camHalfH), centroSala.y + (salaHalfH - camHalfH));

        return new Vector3(x, y, z);
    }
}
