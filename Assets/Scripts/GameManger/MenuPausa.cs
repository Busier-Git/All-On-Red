using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa y reinicio rapido. Se crea SOLO por codigo (Player.Start llama
/// a MenuPausa.Asegurar()), no hay que agregar nada a las escenas.
/// - ESC: pausa el juego y muestra Continuar / Salir de la partida / Salir del juego.
/// - Mantener R por 3 segundos: reinicia la partida (con barra de progreso).
/// </summary>
public class MenuPausa : MonoBehaviour
{
    public static MenuPausa Instancia;

    [Header("Reinicio rapido")]
    public float segundosReinicio = 3f;

    private GameObject panel;
    private Text textoReinicio;
    private bool pausado = false;
    private float tiempoR = 0f;

    /// <summary>Crea el menu si aun no existe en la escena.</summary>
    public static void Asegurar()
    {
        if (Instancia != null) return;
        new GameObject("MenuPausa").AddComponent<MenuPausa>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        GestorAudio.Asegurar();   // asegura que exista el audio (y arranque la musica)
        ConstruirUI();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Update()
    {
        // ---- ESC: abrir/cerrar el menu de pausa (no sobre el menu de muerte) ----
        if (Input.GetKeyDown(KeyCode.Escape) && !MuerteActiva())
        {
            if (pausado) Continuar();
            else Pausar();
        }

        // ---- Mantener R 3 segundos: reiniciar la partida ----
        if (Input.GetKey(KeyCode.R))
        {
            tiempoR += Time.unscaledDeltaTime;   // funciona incluso en pausa o muerto
            if (textoReinicio != null)
            {
                textoReinicio.gameObject.SetActive(true);
                int pct = Mathf.Clamp(Mathf.RoundToInt(tiempoR / segundosReinicio * 100f), 0, 100);
                textoReinicio.text = "Reiniciando... " + pct + "%";
            }
            if (tiempoR >= segundosReinicio)
            {
                tiempoR = 0f;
                Reiniciar();
            }
        }
        else
        {
            tiempoR = 0f;
            if (textoReinicio != null && textoReinicio.gameObject.activeSelf)
                textoReinicio.gameObject.SetActive(false);
        }
    }

    bool MuerteActiva()
    {
        return GameManager.Instance != null
            && GameManager.Instance.panelMuerte != null
            && GameManager.Instance.panelMuerte.activeSelf;
    }

    // ============================ ACCIONES ============================
    public void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
    }

    public void Continuar()
    {
        pausado = false;
        Time.timeScale = 1f;
        if (panel != null) panel.SetActive(false);
    }

    void Reiniciar()
    {
        pausado = false;
        if (panel != null) panel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.Reiniciar();
        else
        {
            Time.timeScale = 1f;
            EstadoPartida.Limpiar();
            SceneManager.LoadScene("test");   // reiniciar = volver al nivel 1
        }
    }

    void SalirDeLaPartida()
    {
        pausado = false;
        if (GameManager.Instance != null) GameManager.Instance.VolverAlMenu();
        else
        {
            Time.timeScale = 1f;
            EstadoPartida.Limpiar();
            SceneManager.LoadScene(0);
        }
    }

    void SalirDelJuego()
    {
#if UNITY_EDITOR
        Debug.Log("Salir del juego (en el editor no se cierra).");
#endif
        Application.Quit();
    }

    // ============================ UI POR CODIGO ============================
    void ConstruirUI()
    {
        // Canvas propio, encima de todo
        GameObject canvasGO = new GameObject("CanvasPausa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        // ----- Panel oscuro de pausa -----
        panel = new GameObject("PanelPausa", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        CrearTextoUI(panel.transform, "PAUSA", 44, new Vector2(0f, 185f), new Color(1f, 1f, 1f, 0.95f), true);

        // Sliders de volumen (Musica y Efectos)
        CrearFilaVolumen(panel.transform, "Música", 128f,
            (GestorAudio.Instancia != null) ? GestorAudio.Instancia.VolumenMusica : 0.7f,
            v => { if (GestorAudio.Instancia != null) GestorAudio.Instancia.SetVolumenMusica(v); });
        CrearFilaVolumen(panel.transform, "Efectos", 68f,
            (GestorAudio.Instancia != null) ? GestorAudio.Instancia.VolumenEfectos : 0.9f,
            v => { if (GestorAudio.Instancia != null) GestorAudio.Instancia.SetVolumenEfectos(v); });

        CrearBoton(panel.transform, "Continuar", new Vector2(0f, -5f), Continuar);
        CrearBoton(panel.transform, "Salir de la partida", new Vector2(0f, -65f), SalirDeLaPartida);
        CrearBoton(panel.transform, "Salir del juego", new Vector2(0f, -125f), SalirDelJuego);

        panel.SetActive(false);

        // ----- Texto de "Reiniciando..." (abajo al centro, fuera del panel) -----
        textoReinicio = CrearTextoUI(canvasGO.transform, "Reiniciando... 0%", 26, Vector2.zero, new Color(1f, 0.85f, 0.3f), false);
        RectTransform trt = textoReinicio.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 70f);
        textoReinicio.gameObject.SetActive(false);
    }

    Text CrearTextoUI(Transform padre, string contenido, int tamano, Vector2 pos, Color color, bool centradoEnPanel)
    {
        GameObject go = new GameObject("Texto_" + contenido, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 60f);
        if (centradoEnPanel)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        Text t = go.GetComponent<Text>();
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        Font f = UtilJuego.Fuente();
        if (f != null) t.font = f;
        return t;
    }

    // Una fila = etiqueta con % + barra deslizable
    void CrearFilaVolumen(Transform padre, string nombre, float y, float valorInicial, UnityEngine.Events.UnityAction<float> alCambiar)
    {
        Text etiqueta = CrearTextoUI(padre, nombre + "  " + Mathf.RoundToInt(valorInicial * 100f) + "%", 22,
                                     new Vector2(0f, y + 26f), Color.white, true);

        CrearSlider(padre, new Vector2(0f, y), valorInicial, v =>
        {
            etiqueta.text = nombre + "  " + Mathf.RoundToInt(v * 100f) + "%";
            alCambiar(v);
        });
    }

    // Slider funcional creado por codigo (estructura estandar de Unity: fondo + relleno + manija)
    Slider CrearSlider(Transform padre, Vector2 pos, float valorInicial, UnityEngine.Events.UnityAction<float> alCambiar)
    {
        GameObject go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(280f, 22f);
        rt.anchoredPosition = pos;

        // Fondo
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        RectTransform bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0f, 0.25f); bgrt.anchorMax = new Vector2(1f, 0.75f);
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        // Area de relleno + relleno
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fart = fillArea.GetComponent<RectTransform>();
        fart.anchorMin = new Vector2(0f, 0.25f); fart.anchorMax = new Vector2(1f, 0.75f);
        fart.offsetMin = new Vector2(6f, 0f); fart.offsetMax = new Vector2(-6f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 1f);
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        frt.sizeDelta = new Vector2(10f, 0f);
        fill.GetComponent<Image>().color = new Color(0.95f, 0.80f, 0.30f, 1f);

        // Area de la manija + manija
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform hart = handleArea.GetComponent<RectTransform>();
        hart.anchorMin = new Vector2(0f, 0f); hart.anchorMax = new Vector2(1f, 1f);
        hart.offsetMin = new Vector2(10f, 0f); hart.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(20f, 0f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = valorInicial;
        slider.onValueChanged.AddListener(alCambiar);
        return slider;
    }

    void CrearBoton(Transform padre, string etiqueta, Vector2 pos, UnityEngine.Events.UnityAction accion)
    {
        GameObject go = new GameObject("Boton_" + etiqueta, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 52f);
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.28f, 0.95f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(accion);

        Text t = CrearTextoUI(go.transform, etiqueta, 24, Vector2.zero, Color.white, true);
        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
    }
}
