using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera el piso estilo The Binding of Isaac, ahora con salas de varios tamaños
/// (1x1, 2x1, 1x2, 2x2 celdas). Construye TODO por codigo en runtime: piso, paredes,
/// puertas, trigger, enemigos y obstaculos. La sala del jefe se agranda a 2x2.
///
/// Visual: si asignas sprites (suelo/pared) los usa en modo Tiled (se repiten); si no,
/// usa cuadros de color. Asigna prefabBala (EnemyBullet) y prefabMoneda (Coin).
/// </summary>
public class GeneradorMapa : MonoBehaviour
{
    public static GeneradorMapa Instancia;

    [Header("Grilla (en celdas)")]
    public int ancho = 9;
    public int alto = 9;
    public int minSalas = 8;
    public int maxSalas = 12;

    [Header("Tamaño de una celda (unidades de mundo)")]
    public Vector2 tamCelda = new Vector2(20f, 12f);
    public float grosorPared = 1f;
    public float anchoPuerta = 3f;

    [Header("Variedad de tamaños")]
    [Range(0, 100)] public int probSalaGrande = 30;

    [Header("Enemigos (cantidad al azar, no excesiva)")]
    public int enemigosMin = 2;
    public int enemigosMax = 4;
    [Range(0f, 1f)] public float probMoneda = 0.4f;

    [Header("Obstaculos")]
    public int obstaculosMin = 1;
    public int obstaculosMax = 3;

    [Header("Prefabs a reutilizar de tu proyecto")]
    public GameObject prefabBala;     // EnemyBullet.prefab (disparadores y jefe)
    public GameObject prefabMoneda;   // Coin.prefab

    [Header("Prefabs de enemigos (si los asignas se usan; si no, se crean por codigo)")]
    public GameObject[] prefabsEnemigos;   // pool de enemigos normales (usa el menu Roguelike > Crear prefabs de enemigos)
    public GameObject prefabJefe;          // enemigo jefe (nivel 1)
    public GameObject prefabJefeNivel2;    // jefe del nivel 2 (vacio = Adversario por codigo)

    [Header("Nivel (0 = auto por nombre de escena: 'test'=1, '2'=2)")]
    public int forzarNivel = 0;
    [HideInInspector] public int nivel = 1;

    [Header("Objetos y tienda")]
    public int precioCorazonTienda = 3;
    [Range(0f, 1f)] public float probObjetoEnTienda = 0.7f;      // si falla: la tienda solo tiene corazon
    public int costoSalaEspecialNivel2 = 5;                      // peaje de tesoro/tienda en nivel 2
    [Range(0f, 1f)] public float probSalaEspecialVacia = 0.25f;  // nivel 2: la sala puede estar vacia

    [Header("Maquina tragamonedas (tesoro y tienda)")]
    public int precioMaquina = 5;                               // cuesta jugar
    [Range(0f, 1f)] public float probMaquina = 0.5f;            // 50/50 que te de el objeto

    [Header("Sprites opcionales (vacio = usa el Banco de Sprites, y si no, colores)")]
    public Sprite spriteSuelo;
    public Sprite spritePared;
    public Sprite spriteObstaculoRoca;
    public Sprite spriteObstaculoCaja;

    [Header("Decoracion del piso (sprites en el Banco de Sprites > decoraciones)")]
    public int decoracionesMin = 2;   // por celda de sala
    public int decoracionesMax = 5;
    public Color colorSuelo = new Color(0.13f, 0.10f, 0.13f);
    public Color colorPared = new Color(0.33f, 0.12f, 0.14f);

    [Header("Jugador (se autocompleta por tag)")]
    public Transform jugador;

    int[,] idCelda;                         // -1 vacio, si no el id de la sala
    readonly List<Sala> salas = new List<Sala>();
    // Una sola puerta por pared compartida: la clave es la arista entre dos celdas.
    readonly Dictionary<long, Puerta> puertasCompartidas = new Dictionary<long, Puerta>();
    Vector2Int inicio;

    Sprite _blanco, _verde, _rojo, _dorado;
    Vector2 centroSalaJefe;   // para colocar el portal al morir el jefe

    static readonly Vector2Int[] DIRS = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    class Sala
    {
        public int id;
        public Vector2Int origen;   // celda inferior-izquierda
        public Vector2Int tam;      // (w,h) en celdas
        public TipoSala tipo = TipoSala.Normal;
        public Habitacion instancia;

        public IEnumerable<Vector2Int> Celdas()
        {
            for (int x = 0; x < tam.x; x++)
                for (int y = 0; y < tam.y; y++)
                    yield return new Vector2Int(origen.x + x, origen.y + y);
        }
        public Vector2 CentroCelda => new Vector2(origen.x + (tam.x - 1) / 2f, origen.y + (tam.y - 1) / 2f);
    }

    // ---- API publica para el minimapa (Tanda 2b) ----
    public struct InfoSala { public Vector2Int origen; public Vector2Int tam; public TipoSala tipo; public Habitacion hab; }
    public int Ancho => ancho;
    public int Alto => alto;
    public List<InfoSala> ObtenerSalas()
    {
        var lista = new List<InfoSala>();
        foreach (var s in salas)
            lista.Add(new InfoSala { origen = s.origen, tam = s.tam, tipo = s.tipo, hab = s.instancia });
        return lista;
    }

    void Awake()
    {
        Instancia = this;

        // Nivel automatico segun el nombre de la escena ("test" = 1, "2" = 2)
        if (forzarNivel > 0) nivel = forzarNivel;
        else nivel = (SceneManager.GetActiveScene().name.Trim() == "2") ? 2 : 1;
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject pj = GameObject.FindWithTag("Player");
            if (pj != null) jugador = pj.transform;
        }

        // Sprites centralizados: lo que no asignes aqui se toma del Banco de Sprites.
        // El suelo y la pared cambian segun el nivel (nivel 2 tiene los suyos).
        BancoSprites banco = BancoSprites.Cargar();
        if (banco != null)
        {
            if (spriteSuelo == null) spriteSuelo = banco.SueloDeNivel(nivel);
            if (spritePared == null) spritePared = banco.ParedDeNivel(nivel);
            if (spriteObstaculoRoca == null) spriteObstaculoRoca = banco.roca;
            if (spriteObstaculoCaja == null) spriteObstaculoCaja = banco.caja;
        }

        if (prefabBala == null)
            Debug.LogError("GeneradorMapa: falta 'Prefab Bala' (EnemyBullet). Corre el menu Roguelike > Preparar escena. Sin esto los enemigos a distancia y el jefe NO disparan.");

        Generar();
    }

    public void Generar()
    {
        int intentos = 0;
        while (intentos < 80 && !IntentarLayout()) intentos++;
        if (salas.Count < minSalas) { Debug.LogError("GeneradorMapa: no se pudo generar el piso."); return; }

        AsignarTipos();
        puertasCompartidas.Clear();   // una sola puerta por pared compartida entre dos salas
        foreach (var s in salas) ConstruirSala(s);
        ColocarJugador();
    }

    // ============================ LAYOUT ============================
    bool IntentarLayout()
    {
        idCelda = new int[ancho, alto];
        for (int x = 0; x < ancho; x++)
            for (int y = 0; y < alto; y++)
                idCelda[x, y] = -1;
        salas.Clear();

        int objetivo = Random.Range(minSalas, maxSalas + 1);
        inicio = new Vector2Int(ancho / 2, alto / 2);

        Sala s0 = ColocarSala(inicio, Vector2Int.one);
        Queue<int> cola = new Queue<int>();
        cola.Enqueue(s0.id);

        while (cola.Count > 0 && salas.Count < objetivo)
        {
            Sala actual = salas[cola.Dequeue()];

            // celdas-frontera: vecinas libres de cualquier celda de la sala
            List<KeyValuePair<Vector2Int, Vector2Int>> fronteras = new List<KeyValuePair<Vector2Int, Vector2Int>>();
            foreach (var c in actual.Celdas())
                foreach (var d in DIRS)
                {
                    Vector2Int n = c + d;
                    if (EnRango(n) && idCelda[n.x, n.y] == -1)
                        fronteras.Add(new KeyValuePair<Vector2Int, Vector2Int>(n, d));
                }
            Barajar(fronteras);

            foreach (var f in fronteras)
            {
                if (salas.Count >= objetivo) break;
                Vector2Int celda = f.Key;
                if (idCelda[celda.x, celda.y] != -1) continue;   // se ocupo entre medio
                if (Random.value < 0.5f) continue;               // 50% no expandir

                Vector2Int tam = ElegirTam();
                Sala nueva = IntentarColocar(celda, tam);
                if (nueva == null && tam != Vector2Int.one)
                    nueva = IntentarColocar(celda, Vector2Int.one);
                if (nueva != null) cola.Enqueue(nueva.id);
            }
        }
        return salas.Count >= minSalas;
    }

    Vector2Int ElegirTam()
    {
        if (Random.Range(0, 100) >= probSalaGrande) return Vector2Int.one;
        int r = Random.Range(0, 3);
        if (r == 0) return new Vector2Int(2, 1);
        if (r == 1) return new Vector2Int(1, 2);
        return new Vector2Int(2, 2);
    }

    // Intenta colocar un bloque 'tam' que contenga la celda 'ancla'
    Sala IntentarColocar(Vector2Int ancla, Vector2Int tam)
    {
        List<Vector2Int> origenes = new List<Vector2Int>();
        for (int ox = 0; ox < tam.x; ox++)
            for (int oy = 0; oy < tam.y; oy++)
                origenes.Add(new Vector2Int(ancla.x - ox, ancla.y - oy));
        Barajar(origenes);

        foreach (var origen in origenes)
            if (BloqueValido(origen, tam))
                return ColocarSala(origen, tam);
        return null;
    }

    bool BloqueValido(Vector2Int origen, Vector2Int tam)
    {
        for (int x = 0; x < tam.x; x++)
            for (int y = 0; y < tam.y; y++)
            {
                Vector2Int c = new Vector2Int(origen.x + x, origen.y + y);
                if (!EnRango(c) || idCelda[c.x, c.y] != -1) return false;
            }

        // que no toque mas de 1 sala existente (mantiene forma de arbol)
        HashSet<int> tocadas = new HashSet<int>();
        for (int x = 0; x < tam.x; x++)
            for (int y = 0; y < tam.y; y++)
            {
                Vector2Int c = new Vector2Int(origen.x + x, origen.y + y);
                foreach (var d in DIRS)
                {
                    Vector2Int n = c + d;
                    bool dentro = n.x >= origen.x && n.x < origen.x + tam.x && n.y >= origen.y && n.y < origen.y + tam.y;
                    if (dentro) continue;
                    if (EnRango(n) && idCelda[n.x, n.y] >= 0) tocadas.Add(idCelda[n.x, n.y]);
                }
            }
        return tocadas.Count <= 1;
    }

    Sala ColocarSala(Vector2Int origen, Vector2Int tam)
    {
        Sala s = new Sala { id = salas.Count, origen = origen, tam = tam };
        foreach (var c in s.Celdas()) idCelda[c.x, c.y] = s.id;
        salas.Add(s);
        return s;
    }

    // ============================ TIPOS ============================
    void AsignarTipos()
    {
        salas[0].tipo = TipoSala.Inicio;

        Dictionary<int, HashSet<int>> ady = Adyacencia();
        Dictionary<int, int> dist = BFS(0, ady);

        List<int> callejones = new List<int>();
        foreach (var s in salas)
            if (s.id != 0 && ady.ContainsKey(s.id) && ady[s.id].Count == 1)
                callejones.Add(s.id);

        List<int> candidatos = (callejones.Count > 0) ? callejones : TodosMenosInicio();
        int jefe = MasLejano(candidatos, dist);
        if (jefe >= 0)
        {
            salas[jefe].tipo = TipoSala.Jefe;
            AgrandarSala(salas[jefe], new Vector2Int(2, 2));   // sala de jefe grande
            callejones.Remove(jefe);
        }
        if (callejones.Count > 0) { salas[callejones[0]].tipo = TipoSala.Tesoro; callejones.RemoveAt(0); }
        if (callejones.Count > 0) { salas[callejones[0]].tipo = TipoSala.Tienda; callejones.RemoveAt(0); }
    }

    List<int> TodosMenosInicio()
    {
        var l = new List<int>();
        foreach (var s in salas) if (s.id != 0) l.Add(s.id);
        return l;
    }

    int MasLejano(List<int> ids, Dictionary<int, int> dist)
    {
        int mejor = -1, max = -1;
        foreach (int id in ids)
            if (dist.ContainsKey(id) && dist[id] > max) { max = dist[id]; mejor = id; }
        return mejor;
    }

    Dictionary<int, HashSet<int>> Adyacencia()
    {
        var ady = new Dictionary<int, HashSet<int>>();
        foreach (var s in salas) ady[s.id] = new HashSet<int>();
        for (int x = 0; x < ancho; x++)
            for (int y = 0; y < alto; y++)
            {
                int id = idCelda[x, y];
                if (id < 0) continue;
                foreach (var d in DIRS)
                {
                    Vector2Int n = new Vector2Int(x + d.x, y + d.y);
                    if (EnRango(n) && idCelda[n.x, n.y] >= 0 && idCelda[n.x, n.y] != id)
                        ady[id].Add(idCelda[n.x, n.y]);
                }
            }
        return ady;
    }

    Dictionary<int, int> BFS(int origen, Dictionary<int, HashSet<int>> ady)
    {
        var dist = new Dictionary<int, int> { { origen, 0 } };
        var cola = new Queue<int>();
        cola.Enqueue(origen);
        while (cola.Count > 0)
        {
            int a = cola.Dequeue();
            foreach (int v in ady[a])
                if (!dist.ContainsKey(v)) { dist[v] = dist[a] + 1; cola.Enqueue(v); }
        }
        return dist;
    }

    void AgrandarSala(Sala s, Vector2Int objetivo)
    {
        bool crecio = true;
        while (crecio && (s.tam.x < objetivo.x || s.tam.y < objetivo.y))
        {
            crecio = false;
            if (s.tam.x < objetivo.x)
            {
                if (PuedeCrecer(s, Vector2Int.right)) { Crecer(s, Vector2Int.right); crecio = true; }
                else if (PuedeCrecer(s, Vector2Int.left)) { Crecer(s, Vector2Int.left); crecio = true; }
            }
            if (s.tam.y < objetivo.y)
            {
                if (PuedeCrecer(s, Vector2Int.up)) { Crecer(s, Vector2Int.up); crecio = true; }
                else if (PuedeCrecer(s, Vector2Int.down)) { Crecer(s, Vector2Int.down); crecio = true; }
            }
        }
    }

    List<Vector2Int> FilaNueva(Sala s, Vector2Int dir)
    {
        var l = new List<Vector2Int>();
        if (dir == Vector2Int.right) for (int y = 0; y < s.tam.y; y++) l.Add(new Vector2Int(s.origen.x + s.tam.x, s.origen.y + y));
        else if (dir == Vector2Int.left) for (int y = 0; y < s.tam.y; y++) l.Add(new Vector2Int(s.origen.x - 1, s.origen.y + y));
        else if (dir == Vector2Int.up) for (int x = 0; x < s.tam.x; x++) l.Add(new Vector2Int(s.origen.x + x, s.origen.y + s.tam.y));
        else for (int x = 0; x < s.tam.x; x++) l.Add(new Vector2Int(s.origen.x + x, s.origen.y - 1));
        return l;
    }

    bool PuedeCrecer(Sala s, Vector2Int dir)
    {
        foreach (var c in FilaNueva(s, dir))
            if (!EnRango(c) || idCelda[c.x, c.y] != -1) return false;
        return true;
    }

    void Crecer(Sala s, Vector2Int dir)
    {
        foreach (var c in FilaNueva(s, dir)) idCelda[c.x, c.y] = s.id;
        if (dir == Vector2Int.left) { s.origen.x -= 1; s.tam.x += 1; }
        else if (dir == Vector2Int.right) { s.tam.x += 1; }
        else if (dir == Vector2Int.down) { s.origen.y -= 1; s.tam.y += 1; }
        else { s.tam.y += 1; }
    }

    // ============================ CONSTRUIR ============================
    void ConstruirSala(Sala s)
    {
        GameObject raiz = new GameObject($"Sala_{s.tipo}_{s.id}");
        raiz.transform.SetParent(transform);
        Vector2 centro = CeldaAMundo(s.CentroCelda);
        raiz.transform.position = centro;

        Vector2 tamMundo = new Vector2(s.tam.x * tamCelda.x, s.tam.y * tamCelda.y);

        Habitacion hab = raiz.AddComponent<Habitacion>();
        hab.origenCelda = s.origen; hab.tamCeldas = s.tam; hab.tipo = s.tipo; hab.tamMundo = tamMundo;
        s.instancia = hab;

        // Piso
        CrearCuadro("Piso", raiz.transform, Vector2.zero, tamMundo - Vector2.one * grosorPared, spriteSuelo, colorSuelo, false, -10);

        // Decoracion del piso (manchas, grietas, etc. del Banco de Sprites)
        CrearDecoraciones(s, raiz.transform, centro);

        // Bordes: pared o puerta segun el vecino de cada celda-borde
        List<Puerta> puertas = new List<Puerta>();
        foreach (var c in s.Celdas())
            foreach (var d in DIRS)
            {
                Vector2Int n = c + d;
                if (EnRango(n) && idCelda[n.x, n.y] == s.id) continue;   // borde interno
                bool esPuerta = EnRango(n) && idCelda[n.x, n.y] >= 0;    // vecino de OTRA sala
                Vector2 bordeMundo = CeldaAMundo(new Vector2(c.x, c.y)) + new Vector2(d.x * tamCelda.x / 2f, d.y * tamCelda.y / 2f);
                CrearBorde(raiz.transform, bordeMundo - centro, d, esPuerta, puertas, c, n);
            }

        // Trigger de deteccion.
        // ARREGLO PUERTAS: antes el trigger quedaba muy metido hacia adentro
        // (grosorPared * 3) y habia que entrar mucho en la sala para que contara.
        // Ahora llega casi hasta la pared: cruzas la puerta y cambia al tiro.
        GameObject zona = new GameObject("ZonaSala");
        zona.transform.SetParent(raiz.transform, false);
        BoxCollider2D trig = zona.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = tamMundo - Vector2.one * (grosorPared * 1.2f);
        zona.AddComponent<DetectorSala>().habitacion = hab;

        // Contenedores
        Transform contObst = NuevoContenedor("Obstaculos", raiz.transform);
        Transform contEnem = NuevoContenedor("Enemigos", raiz.transform);
        List<GameObject> enemigos = new List<GameObject>();
        List<Vector2> ocupados = new List<Vector2>();
        List<Vector2> puertasPos = new List<Vector2>();
        foreach (var pu in puertas) puertasPos.Add(pu.transform.position);

        if (s.tipo == TipoSala.Normal)
        {
            SpawnObstaculos(s, contObst, ocupados, puertasPos, 0f);
            SpawnEnemigos(s, contEnem, enemigos, centro, tamMundo, ocupados);
        }
        else if (s.tipo == TipoSala.Jefe)
        {
            SpawnObstaculos(s, contObst, ocupados, puertasPos, 6f);  // deja el centro libre para esquivar
            SpawnJefe(contEnem, enemigos, centro);
            centroSalaJefe = centro;
        }
        else if (s.tipo == TipoSala.Tesoro)
        {
            ConstruirTesoro(centro, raiz.transform);
        }
        else if (s.tipo == TipoSala.Tienda)
        {
            ConstruirTienda(centro, raiz.transform);
        }

        // NIVEL 2: entrar a las salas especiales cuesta monedas (puertas con peaje)
        if (nivel >= 2 && (s.tipo == TipoSala.Tesoro || s.tipo == TipoSala.Tienda))
        {
            BancoSprites bancoP = BancoSprites.Cargar();
            Sprite sprPeaje = (bancoP != null && bancoP.puertaPeaje != null) ? bancoP.puertaPeaje : Dorado();
            foreach (var pu in puertas)
            {
                Vector3 posPuerta = pu.transform.position;
                Vector3 haciaAfuera = (posPuerta - (Vector3)centro).normalized;
                pu.ConfigurarPeaje(costoSalaEspecialNivel2, sprPeaje, posPuerta + haciaAfuera * 1.6f);
            }
        }

        hab.Configurar(puertas, contEnem, enemigos);
    }

    // Reparte sprites de decoracion al azar por el piso de la sala (sin colliders)
    void CrearDecoraciones(Sala s, Transform raiz, Vector2 centro)
    {
        BancoSprites banco = BancoSprites.Cargar();
        if (banco == null) return;
        Sprite[] deco = banco.DecoracionesDeNivel(nivel);   // decoracion segun el nivel
        if (deco == null || deco.Length == 0) return;

        int area = s.tam.x * s.tam.y;
        int cantidad = Random.Range(decoracionesMin, decoracionesMax + 1) * area;
        Vector2 tamMundo = new Vector2(s.tam.x * tamCelda.x, s.tam.y * tamCelda.y);

        for (int i = 0; i < cantidad; i++)
        {
            Sprite spr = deco[Random.Range(0, deco.Length)];
            if (spr == null) continue;

            GameObject d = new GameObject("Decoracion");
            d.transform.SetParent(raiz, false);
            d.transform.position = centro + PosInterior(tamMundo);

            SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -9;                 // sobre el piso, debajo de todo lo demas
            sr.flipX = Random.value < 0.5f;       // variedad gratis

            float tam = Random.Range(0.8f, 1.6f);
            UtilJuego.AplicarSprite(d, spr, new Vector2(tam, tam), true, false);
        }
    }

    Transform NuevoContenedor(string nombre, Transform padre)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = Vector3.zero;
        return t;
    }

    void CrearBorde(Transform padre, Vector2 centroBorde, Vector2Int dir, bool esPuerta, List<Puerta> puertas, Vector2Int celda, Vector2Int vecino)
    {
        bool horizontal = dir.y != 0;       // borde arriba/abajo = segmento horizontal
        float largo = horizontal ? tamCelda.x : tamCelda.y;

        if (!esPuerta)
        {
            Vector2 size = horizontal ? new Vector2(largo, grosorPared) : new Vector2(grosorPared, largo);
            CrearCuadro("Pared", padre, centroBorde, size, spritePared, colorPared, true, 0);
            return;
        }

        // La pared entre dos salas se comparte: si la puerta ya la creo la OTRA sala,
        // la reutilizo (asi hay UNA sola puerta y no dos superpuestas con estados distintos).
        long clave = ClaveArista(celda, vecino);
        Puerta existente;
        if (puertasCompartidas.TryGetValue(clave, out existente) && existente != null)
        {
            puertas.Add(existente);
            return;   // no vuelvo a crear paredes ni puerta: ya estan puestas por la otra sala
        }

        // con puerta: 2 trozos de pared + 1 puerta en el centro
        float gap = Mathf.Min(anchoPuerta, largo * 0.6f);
        float stub = (largo - gap) / 2f;
        Puerta nueva;

        if (horizontal)
        {
            if (stub > 0.05f)
            {
                CrearCuadro("Pared", padre, centroBorde + new Vector2(-(gap / 2f + stub / 2f), 0f), new Vector2(stub, grosorPared), spritePared, colorPared, true, 0);
                CrearCuadro("Pared", padre, centroBorde + new Vector2( (gap / 2f + stub / 2f), 0f), new Vector2(stub, grosorPared), spritePared, colorPared, true, 0);
            }
            nueva = CrearPuerta(padre, centroBorde, new Vector2(gap, grosorPared));
        }
        else
        {
            if (stub > 0.05f)
            {
                CrearCuadro("Pared", padre, centroBorde + new Vector2(0f, -(gap / 2f + stub / 2f)), new Vector2(grosorPared, stub), spritePared, colorPared, true, 0);
                CrearCuadro("Pared", padre, centroBorde + new Vector2(0f,  (gap / 2f + stub / 2f)), new Vector2(grosorPared, stub), spritePared, colorPared, true, 0);
            }
            nueva = CrearPuerta(padre, centroBorde, new Vector2(grosorPared, gap));
        }

        puertasCompartidas[clave] = nueva;
        puertas.Add(nueva);
    }

    // Clave unica de la pared entre dos celdas (independiente del orden de las salas)
    long ClaveArista(Vector2Int a, Vector2Int b)
    {
        long codeA = a.x * (long)alto + a.y;
        long codeB = b.x * (long)alto + b.y;
        long lo = System.Math.Min(codeA, codeB);
        long hi = System.Math.Max(codeA, codeB);
        return lo * 100000L + hi;
    }

    GameObject CrearCuadro(string nombre, Transform padre, Vector2 local, Vector2 size, Sprite sprite, Color color, bool collider, int orden)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = local;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = orden;

        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;     // repite el tile para llenar el tamaño
            sr.size = size;
            if (collider) go.AddComponent<BoxCollider2D>().size = size;
        }
        else
        {
            sr.sprite = Blanco();
            sr.color = color;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            if (collider) go.AddComponent<BoxCollider2D>();   // 1x1 * escala = size
        }
        return go;
    }

    Puerta CrearPuerta(Transform padre, Vector2 local, Vector2 size)
    {
        // RAIZ: la barrera (collider) en unidades de mundo, escala 1.
        // El sprite va en un hijo aparte para poder intercambiarlo (abierta/cerrada)
        // sin depender del modo Tiled/Sliced, que no cambiaba bien el sprite.
        GameObject go = new GameObject("Puerta");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = local;
        go.transform.localScale = Vector3.one;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();   // hueco real de la puerta, en el mundo
        col.size = size;

        GameObject vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco();
        sr.sortingOrder = 1;

        Puerta p = go.AddComponent<Puerta>();
        p.barrera = col;
        p.sprite = sr;
        p.tamHueco = size;

        // Sprites de puerta desde el Banco de Sprites (si hay)
        BancoSprites banco = BancoSprites.Cargar();
        Sprite abierta = (banco != null) ? banco.puertaAbierta : null;
        Sprite cerrada = (banco != null) ? banco.puertaCerrada : null;

        if (abierta != null || cerrada != null)
        {
            p.spriteAbierta = (abierta != null) ? abierta : cerrada;
            if (cerrada != null)
            {
                p.spriteCerrada = cerrada;
                p.colorCerrada = Color.white;
            }
            else
            {
                // Solo asignaste la puerta abierta: reuso ese sprite tintado de rojo al cerrar
                p.spriteCerrada = abierta;
                p.colorCerrada = new Color(1f, 0.45f, 0.45f);
            }
            if (banco.muro != null) p.spriteMuro = banco.muro;
        }
        else
        {
            // Sin sprites: cuadros de color de siempre (verde = abierta, rojo = cerrada)
            p.spriteAbierta = Verde();
            p.spriteCerrada = Rojo();
        }

        p.VolverPuerta();   // empieza abierta
        return p;
    }

    // ============================ SPAWN ============================
    void SpawnEnemigos(Sala s, Transform cont, List<GameObject> lista, Vector2 centro, Vector2 tamMundo, List<Vector2> ocupados)
    {
        int area = s.tam.x * s.tam.y;
        int cantidad = Random.Range(enemigosMin, enemigosMax + 1) + (area - 1);
        cantidad = Mathf.Min(cantidad, enemigosMax + 3);   // tope: que no sea excesivo

        bool hayPrefabs = prefabsEnemigos != null && prefabsEnemigos.Length > 0;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 pos = PosInteriorLibre(centro, tamMundo, ocupados);
            GameObject e;
            if (hayPrefabs)
            {
                GameObject prefab = prefabsEnemigos[Random.Range(0, prefabsEnemigos.Length)];
                e = Instantiate(prefab, pos, Quaternion.identity, cont);
            }
            else
            {
                e = CrearEnemigo(Random.Range(0, 4), pos, cont);   // respaldo por codigo
            }
            lista.Add(e);
            ocupados.Add(pos);
        }
    }

    GameObject CrearEnemigo(int tipo, Vector2 worldPos, Transform cont)
    {
        GameObject e = new GameObject("Enemigo");
        e.transform.position = worldPos;
        e.transform.SetParent(cont, true);
        PonerTagEnemy(e);

        SpriteRenderer sr = e.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco();
        sr.sortingOrder = 5;
        e.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        e.AddComponent<CircleCollider2D>();

        // Sprite del Banco de Sprites segun el tipo (si no hay, queda el color de siempre)
        BancoSprites banco = BancoSprites.Cargar();
        Sprite sprEnemigo = null;
        if (banco != null)
            sprEnemigo = (tipo == 0) ? banco.enemigoPerseguidor
                       : (tipo == 1) ? banco.enemigoDisparador
                       : (tipo == 2) ? banco.enemigoDiagonal
                       : banco.enemigoTorreta;
        bool conSprite = sprEnemigo != null;
        if (conSprite) UtilJuego.AplicarSprite(e, sprEnemigo, new Vector2(1.1f, 1.1f));

        Botin botin = e.AddComponent<Botin>();
        botin.prefabMoneda = prefabMoneda; botin.probabilidad = probMoneda;

        // Torreta estatica: SIN Rigidbody dinamico (queda fija) y dispara por linea de vision
        if (tipo == 3)
        {
            if (!conSprite) sr.color = new Color(0.30f, 0.80f, 0.85f);
            EnemigoTorreta torreta = e.AddComponent<EnemigoTorreta>();
            torreta.prefabProyectil = prefabBala;
            return e;
        }

        Rigidbody2D rb = e.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (tipo == 0)
        {
            if (!conSprite) sr.color = new Color(0.85f, 0.22f, 0.22f);
            e.AddComponent<Enemy>();
        }
        else if (tipo == 1)
        {
            if (!conSprite) sr.color = new Color(0.92f, 0.55f, 0.20f);
            Enemigo disp = e.AddComponent<Enemigo>();
            disp.prefabProyectil = prefabBala;
        }
        else
        {
            if (!conSprite) sr.color = new Color(0.62f, 0.32f, 0.82f);
            EnemigoDisparoX x = e.AddComponent<EnemigoDisparoX>();
            x.prefabProyectil = prefabBala;
        }
        return e;
    }

    void SpawnJefe(Transform cont, List<GameObject> lista, Vector2 centro)
    {
        // NIVEL 2: jefe estilo "The Adversary" (rayo + persecucion + teletransporte)
        if (nivel >= 2)
        {
            if (prefabJefeNivel2 != null)
                lista.Add(Instantiate(prefabJefeNivel2, centro, Quaternion.identity, cont));
            else
                SpawnJefeAdversario(cont, lista, centro);
            return;
        }

        if (prefabJefe != null)
        {
            lista.Add(Instantiate(prefabJefe, centro, Quaternion.identity, cont));
            return;
        }

        GameObject e = new GameObject("Jefe");
        e.transform.position = centro;
        e.transform.SetParent(cont, true);
        PonerTagEnemy(e);
        e.transform.localScale = new Vector3(2.6f, 2.6f, 1f);

        SpriteRenderer sr = e.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco(); sr.color = new Color(0.6f, 0.1f, 0.42f); sr.sortingOrder = 5;

        Rigidbody2D rb = e.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        e.AddComponent<CircleCollider2D>();

        // Sprite del jefe desde el Banco de Sprites (si hay)
        BancoSprites bancoJ = BancoSprites.Cargar();
        if (bancoJ != null && bancoJ.jefeNivel1 != null)
            UtilJuego.AplicarSprite(e, bancoJ.jefeNivel1, new Vector2(2.6f, 2.6f));

        Jefe jefe = e.AddComponent<Jefe>();
        jefe.prefabProyectil = prefabBala;

        Botin botin = e.AddComponent<Botin>();
        botin.prefabMoneda = prefabMoneda; botin.probabilidad = 1f; botin.minMonedas = 3; botin.maxMonedas = 6;

        lista.Add(e);
    }

    // Jefe del nivel 2 construido por codigo (inspirado en The Adversary de Isaac)
    void SpawnJefeAdversario(Transform cont, List<GameObject> lista, Vector2 centro)
    {
        GameObject e = new GameObject("JefeAdversario");
        e.transform.position = centro;
        e.transform.SetParent(cont, true);
        PonerTagEnemy(e);
        e.transform.localScale = new Vector3(2.6f, 2.6f, 1f);

        SpriteRenderer sr = e.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco(); sr.color = new Color(0.15f, 0.15f, 0.18f); sr.sortingOrder = 5;

        Rigidbody2D rb = e.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        e.AddComponent<CircleCollider2D>();

        // Sprite del Adversario desde el Banco de Sprites. Si no hay, cuadro
        // oscuro con "ojos" rojos para que se note quien es.
        BancoSprites bancoA = BancoSprites.Cargar();
        if (bancoA != null && bancoA.jefeNivel2 != null)
        {
            UtilJuego.AplicarSprite(e, bancoA.jefeNivel2, new Vector2(2.6f, 2.6f));
        }
        else
        {
            GameObject ojos = new GameObject("Ojos");
            ojos.transform.SetParent(e.transform, false);
            ojos.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            ojos.transform.localScale = new Vector3(0.55f, 0.12f, 1f);
            SpriteRenderer srOjos = ojos.AddComponent<SpriteRenderer>();
            srOjos.sprite = Blanco(); srOjos.color = new Color(0.9f, 0.1f, 0.1f); srOjos.sortingOrder = 6;
        }

        e.AddComponent<JefeAdversario>();

        Botin botin = e.AddComponent<Botin>();
        botin.prefabMoneda = prefabMoneda; botin.probabilidad = 1f; botin.minMonedas = 5; botin.maxMonedas = 9;

        lista.Add(e);
    }

    // ============================ SALAS ESPECIALES ============================
    // Sala del TESORO: una MAQUINA tragamonedas (5 monedas por jugar, 50/50 que de el
    // objeto). En nivel 2 puede venir vacia.
    void ConstruirTesoro(Vector2 centro, Transform raiz)
    {
        if (nivel >= 2 && Random.value < probSalaEspecialVacia) return;   // mala suerte: vacia

        DefObjeto def = BancoObjetos.ElegirAlAzar(nivel);
        if (def != null)
            MaquinaObjeto.Crear(centro, def, precioMaquina, probMaquina, raiz);
    }

    // TIENDA: siempre hay un corazon en venta; ademas, con cierta probabilidad,
    // UNA maquina tragamonedas. A veces solo hay corazon.
    void ConstruirTienda(Vector2 centro, Transform raiz)
    {
        if (nivel >= 2 && Random.value < probSalaEspecialVacia) return;   // mala suerte: vacia

        bool hayObjeto = Random.value < probObjetoEnTienda;
        if (hayObjeto)
        {
            DefObjeto def = BancoObjetos.ElegirAlAzar(nivel);
            if (def != null)
                MaquinaObjeto.Crear(centro + Vector2.left * 2.5f, def, precioMaquina, probMaquina, raiz);
        }

        Vector2 posCorazon = hayObjeto ? centro + Vector2.right * 2.5f : centro;
        Corazon.Crear(posCorazon, precioCorazonTienda, raiz);
    }

    // ============================ MUERTE DEL JEFE ============================
    /// <summary>
    /// La llaman Jefe (nivel 1) y JefeAdversario (nivel 2) al morir.
    /// Suelta un corazon y crea el portal: nivel 1 -> escena "2"; nivel 2 -> menu.
    /// </summary>
    public void AlMorirJefe(Vector3 pos)
    {
        Corazon.Crear(pos + Vector3.right * 1.3f, 0, transform);

        if (nivel == 1)
        {
            PortalNivel.Crear(centroSalaJefe, "2", "NIVEL 2", true);
        }
        else
        {
            Corazon.Crear(pos + Vector3.left * 1.3f, 0, transform);   // premio extra
            PortalNivel.Crear(centroSalaJefe, "Menu", "SALIDA", false);
        }
    }

    // Coloca obstaculos por PATRONES (uno por celda). Nunca se enciman ni tapan puertas.
    void SpawnObstaculos(Sala s, Transform cont, List<Vector2> ocupados, List<Vector2> puertas, float centroLibre)
    {
        Vector2 centroRoom = CeldaAMundo(s.CentroCelda);
        float usableW = tamCelda.x - 5f;   // margen ~2.5 a cada lado para no pegar a la pared
        float usableH = tamCelda.y - 5f;

        foreach (var c in s.Celdas())
        {
            string[] patron = PATRONES[Random.Range(0, PATRONES.Length)];
            Vector2 cc = CeldaAMundo(new Vector2(c.x, c.y));

            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 5; col++)
                {
                    char ch = patron[row][col];
                    if (ch == '.') continue;

                    Vector2 pos = cc + new Vector2((col - 2) * (usableW / 4f), (1 - row) * (usableH / 2f));

                    if (centroLibre > 0f && Vector2.Distance(pos, centroRoom) < centroLibre) continue; // jefe: centro libre
                    if (CercaDeAlguno(pos, puertas, 2.5f)) continue;    // no tapar puertas
                    if (CercaDeAlguno(pos, ocupados, 1.6f)) continue;   // no encimar obstaculos

                    CrearObstaculo(ch == 'C', pos, cont);   // 'C' = caja destructible, 'R' = roca
                    ocupados.Add(pos);
                }
        }
    }

    GameObject CrearObstaculo(bool destructible, Vector2 worldPos, Transform cont)
    {
        // RAIZ: solo el collider SOLIDO (tamaño fijo en el mundo) + la logica.
        // El sprite va en un hijo aparte, asi el collider NUNCA depende del sprite
        // (esto evita que el jugador o las balas atraviesen el obstaculo).
        GameObject o = new GameObject(destructible ? "Caja" : "Roca");
        o.transform.position = worldPos;
        o.transform.SetParent(cont, true);
        o.transform.localScale = Vector3.one;   // el collider queda en unidades de mundo

        BoxCollider2D col = o.AddComponent<BoxCollider2D>();
        col.isTrigger = false;                  // SOLIDO: frena al jugador y a las balas
        col.size = new Vector2(1.4f, 1.4f);

        Obstaculo obs = o.AddComponent<Obstaculo>();
        obs.destructible = destructible;
        obs.vida = destructible ? 2f : 1f;
        if (destructible)
        {
            Botin b = o.AddComponent<Botin>();
            b.prefabMoneda = prefabMoneda; b.probabilidad = 0.5f;
            obs.botin = b;
        }

        // VISUAL (hijo): sprite del Banco o cuadro de color, escalado para llenar ~1.4
        GameObject vis = new GameObject("Visual");
        vis.transform.SetParent(o.transform, false);
        SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

        Sprite sp = destructible ? spriteObstaculoCaja : spriteObstaculoRoca;
        if (sp != null)
            UtilJuego.AplicarSprite(vis, sp, new Vector2(1.4f, 1.4f), true, false);   // solo escala el visual
        else
        {
            sr.sprite = Blanco();
            sr.color = destructible ? new Color(0.55f, 0.4f, 0.22f) : new Color(0.45f, 0.45f, 0.5f);
            vis.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        }
        return o;
    }

    void PonerTagEnemy(GameObject g)
    {
        try { g.tag = "enemy"; }
        catch { Debug.LogWarning("Crea el Tag 'enemy' en Project Settings > Tags and Layers."); }
    }

    Vector2 PosInterior(Vector2 tamMundo)
    {
        float mx = Mathf.Max(0.5f, tamMundo.x / 2f - grosorPared - 1.5f);
        float my = Mathf.Max(0.5f, tamMundo.y / 2f - grosorPared - 1.5f);
        return new Vector2(Random.Range(-mx, mx), Random.Range(-my, my));
    }

    // Busca una posicion interior que no este encima de un obstaculo (u otro enemigo) ya colocado.
    Vector2 PosInteriorLibre(Vector2 centro, Vector2 tamMundo, List<Vector2> ocupados)
    {
        for (int intento = 0; intento < 12; intento++)
        {
            Vector2 p = centro + PosInterior(tamMundo);
            if (!CercaDeAlguno(p, ocupados, 1.7f)) return p;
        }
        return centro + PosInterior(tamMundo);
    }

    bool CercaDeAlguno(Vector2 p, List<Vector2> lista, float dist)
    {
        foreach (var q in lista)
            if (Vector2.Distance(p, q) < dist) return true;
        return false;
    }

    // ============================ PATRONES DE OBSTACULOS ============================
    // 5 columnas x 3 filas POR CELDA. '.' vacio, 'R' roca (indestructible), 'C' caja (destructible).
    // La columna central (col 2) y la fila central (fila 1) van SIEMPRE vacias: dejan libre
    // el paso a las puertas. Puedes agregar tus propios patrones a esta lista.
    static readonly string[][] PATRONES =
    {
        new[] { ".....",
                ".....",
                "....." },   // vacia
        new[] { "R...R",
                ".....",
                "R...R" },   // 4 rocas en las esquinas
        new[] { ".C.C.",
                ".....",
                ".C.C." },   // 4 cajas
        new[] { "R...C",
                ".....",
                "C...R" },   // mixto en diagonal
        new[] { "RR.RR",
                ".....",
                "....." },   // hilera de rocas arriba
        new[] { ".....",
                ".....",
                "CC.CC" },   // hilera de cajas abajo
    };

    // ============================ UTILES ============================
    void ColocarJugador()
    {
        Sala s0 = salas[0];
        Vector2 centro = CeldaAMundo(s0.CentroCelda);
        if (jugador != null)
            jugador.position = new Vector3(centro.x, centro.y, jugador.position.z);

        // Minimapa (se crea solo si no existe y se dibuja con la estructura del piso)
        if (Minimapa.Instancia == null)
            new GameObject("Minimapa").AddComponent<Minimapa>();
        Minimapa.Instancia.Construir(this);

        if (ControladorSalas.Instancia != null && s0.instancia != null)
            ControladorSalas.Instancia.EntrarSala(s0.instancia, true);
    }

    Vector2 CeldaAMundo(Vector2 celda) => new Vector2(celda.x * tamCelda.x, celda.y * tamCelda.y);

    bool EnRango(Vector2Int c) => c.x >= 0 && c.x < ancho && c.y >= 0 && c.y < alto;

    void Barajar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }

    Sprite Blanco() { if (_blanco == null) _blanco = SpriteColor(Color.white); return _blanco; }
    Sprite Verde() { if (_verde == null) _verde = SpriteColor(new Color(0.30f, 0.75f, 0.35f)); return _verde; }
    Sprite Rojo() { if (_rojo == null) _rojo = SpriteColor(new Color(0.80f, 0.25f, 0.25f)); return _rojo; }
    Sprite Dorado() { if (_dorado == null) _dorado = SpriteColor(new Color(0.95f, 0.78f, 0.25f)); return _dorado; }

    Sprite SpriteColor(Color c)
    {
        Texture2D t = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        t.SetPixels(px); t.Apply();
        t.filterMode = FilterMode.Point;
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}
