using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Monedas")]
    public int monedas = 0;
    public TMP_Text textoMonedas; // Arrastra el TextMeshPro desde el Inspector

    [Header("Menú de Muerte")]
    public GameObject panelMuerte;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (panelMuerte != null)
            panelMuerte.SetActive(false);

        // Si venimos del nivel anterior, recuperamos las monedas guardadas
        if (EstadoPartida.enCurso)
            monedas = EstadoPartida.monedas;

        ActualizarUI();
    }

    public void AgregarMonedas(int cantidad)
    {
        monedas += cantidad;
        if (monedas < 0) monedas = 0;
        ActualizarUI();
    }

    /// <summary>Intenta pagar. Devuelve true si alcanzaban las monedas.</summary>
    public bool GastarMonedas(int cantidad)
    {
        if (monedas < cantidad) return false;
        monedas -= cantidad;
        ActualizarUI();
        return true;
    }

    private void ActualizarUI()
    {
        if (textoMonedas != null)
            textoMonedas.text = monedas.ToString("00");
    }

    public void MostrarMenuMuerte()
    {
        if (panelMuerte != null)
            panelMuerte.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Reiniciar()
    {
        // Reiniciar SIEMPRE manda al nivel 1 (escena "test"), aunque mueras en el nivel 2
        Time.timeScale = 1f;
        monedas = 0;
        EstadoPartida.Limpiar();
        SceneManager.LoadScene("test");
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        monedas = 0;
        EstadoPartida.Limpiar();
        SceneManager.LoadScene(0);
    }
}
