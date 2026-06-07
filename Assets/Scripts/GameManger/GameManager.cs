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

        ActualizarUI();
    }

    public void AgregarMonedas(int cantidad)
    {
        monedas += cantidad;
        ActualizarUI();
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
        Time.timeScale = 1f;
        monedas = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        monedas = 0;
        SceneManager.LoadScene(0);
    }
}