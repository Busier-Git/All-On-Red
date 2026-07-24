using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de audio del juego. Vive entre escenas (DontDestroyOnLoad), reproduce
/// la musica que corresponde a cada escena y todos los efectos, y recuerda el
/// volumen (se guarda en PlayerPrefs).
///
/// Para reproducir un efecto desde cualquier script:
///     GestorAudio.Efecto("disparo");
/// (los ids estan en BancoAudio.EfectoPorId)
/// </summary>
public class GestorAudio : MonoBehaviour
{
    public static GestorAudio Instancia;

    private BancoAudio banco;
    private AudioSource fuenteMusica;
    private AudioSource[] fuentesEfecto;
    private int idxEfecto;

    public float VolumenMusica { get; private set; }
    public float VolumenEfectos { get; private set; }

    /// <summary>Crea el gestor si aun no existe (lo llama el menu de pausa al arrancar).</summary>
    public static void Asegurar()
    {
        if (Instancia != null) return;
        new GameObject("GestorAudio").AddComponent<GestorAudio>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        banco = BancoAudio.Cargar();
        VolumenMusica = PlayerPrefs.GetFloat("volMusica", 0.7f);
        VolumenEfectos = PlayerPrefs.GetFloat("volEfectos", 0.9f);

        // Una fuente para la musica (en bucle)
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.volume = VolumenMusica;

        // Varias fuentes para efectos, para que puedan sonar varios a la vez
        fuentesEfecto = new AudioSource[8];
        for (int i = 0; i < fuentesEfecto.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            fuentesEfecto[i] = s;
        }

        SceneManager.sceneLoaded += AlCargarEscena;
        AplicarMusicaDeEscena(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instancia == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    // ============================ MUSICA ============================
    void AlCargarEscena(Scene s, LoadSceneMode modo) { AplicarMusicaDeEscena(s.name); }

    void AplicarMusicaDeEscena(string nombre)
    {
        if (banco == null) return;
        string n = nombre.Trim();
        AudioClip clip;
        if (n == "2") clip = banco.musicaNivel2;
        else if (n == "Menu") clip = banco.musicaMenu;
        else clip = banco.musicaNivel1;   // "test" y cualquier otra
        CambiarMusica(clip);
    }

    public void CambiarMusica(AudioClip clip)
    {
        if (fuenteMusica == null) return;
        if (clip == null) { fuenteMusica.Stop(); return; }
        if (fuenteMusica.clip == clip && fuenteMusica.isPlaying) return;   // ya suena esa
        fuenteMusica.clip = clip;
        fuenteMusica.volume = VolumenMusica;
        fuenteMusica.Play();
    }

    // ============================ EFECTOS ============================
    public void ReproducirEfecto(AudioClip clip, float escalaVol = 1f)
    {
        if (clip == null || fuentesEfecto == null) return;
        AudioSource s = fuentesEfecto[idxEfecto];
        idxEfecto = (idxEfecto + 1) % fuentesEfecto.Length;   // rota entre las fuentes
        s.clip = clip;
        s.volume = Mathf.Clamp01(VolumenEfectos * escalaVol);
        s.Play();
    }

    /// <summary>Reproduce un efecto por su id (ej: "disparo"). Uso comodo desde otros scripts.</summary>
    public static void Efecto(string id, float escalaVol = 1f)
    {
        if (Instancia == null) Asegurar();
        if (Instancia == null || Instancia.banco == null) return;
        Instancia.ReproducirEfecto(Instancia.banco.EfectoPorId(id), escalaVol);
    }

    // ============================ VOLUMEN ============================
    public void SetVolumenMusica(float v)
    {
        VolumenMusica = Mathf.Clamp01(v);
        if (fuenteMusica != null) fuenteMusica.volume = VolumenMusica;
        PlayerPrefs.SetFloat("volMusica", VolumenMusica);
    }

    public void SetVolumenEfectos(float v)
    {
        VolumenEfectos = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("volEfectos", VolumenEfectos);
    }
}
