using System.Collections.Generic;

/// <summary>
/// Estado de la partida que sobrevive al cambio de escena (nivel 1 -> nivel 2).
/// Es estatico: no vive en ninguna escena, asi que no se pierde al cargar.
/// Se guarda al tocar el portal del jefe y se limpia al morir / volver al menu.
/// </summary>
public static class EstadoPartida
{
    public static bool enCurso = false;
    public static int monedas = 0;
    public static int vida = 5;
    public static readonly List<string> objetos = new List<string>();

    public static void Guardar(int monedasActuales, int vidaActual, List<string> objetosRecogidos)
    {
        enCurso = true;
        monedas = monedasActuales;
        vida = vidaActual;
        objetos.Clear();
        if (objetosRecogidos != null) objetos.AddRange(objetosRecogidos);
    }

    public static void Limpiar()
    {
        enCurso = false;
        monedas = 0;
        vida = 5;
        objetos.Clear();
    }
}
