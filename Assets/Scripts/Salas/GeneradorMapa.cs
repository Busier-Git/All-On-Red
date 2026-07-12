using System.Collections.Generic;
using UnityEngine;

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
    public GameObject prefabJefe;          // enemigo jefe

    [Header("Sprites opcionales (vacio = usa colores)")]
    public Sprite spriteSuelo;
    public Sprite spritePared;
    public Sprite spriteObstaculoRoca;
    public Sprite spriteObstaculoCaja;
    public Color colorSuelo = new Color(0.13f, 0.10f, 0.13f);
    public Color colorPared = new Color(0.33f, 0.12f, 0.14f);

    [Header("Jugador (se autocompleta por tag)")]
    public Transform jugador;

    int[,] idCelda;                         // -1 vacio, si no el id de la sala
    readonly List<Sala> salas = new List<Sala>();
    Vector2Int inicio;

    Sprite _blanco, _verde, _rojo;

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

    void Awake() { Instancia = this; }

    void Start()
    {
        if (jugador == null)
        {
            GameObject pj = GameObject.FindWithTag("Player");
            if (pj != null) jugador = pj.transform;
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

        // Bordes: pared o puerta segun el vecino de cada celda-borde
        List<Puerta> puertas = new List<Puerta>();
        foreach (var c in s.Celdas())
            foreach (var d in DIRS)
            {
                Vector2Int n = c + d;
                if (EnRango(n) && idCelda[n.x, n.y] == s.id) continue;   // borde interno
                bool esPuerta = EnRango(n) && idCelda[n.x, n.y] >= 0;    // vecino de OTRA sala
                Vector2 bordeMundo = CeldaAMundo(new Vector2(c.x, c.y)) + new Vector2(d.x * tamCelda.x / 2f, d.y * tamCelda.y / 2f);
                CrearBorde(raiz.transform, bordeMundo - centro, d, esPuerta, puertas);
            }

        // Trigger de deteccion
        GameObject zona = new GameObject("ZonaSala");
        zona.transform.SetParent(raiz.transform, false);
        BoxCollider2D trig = zona.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = tamMundo - Vector2.one * (grosorPared * 3f);
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
        }
        // Inicio / Tesoro / Tienda: vacias (luego pones items o la tienda)

        hab.Configurar(puertas, contEnem, enemigos);
    }

    Transform NuevoContenedor(string nombre, Transform padre)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = Vector3.zero;
        return t;
    }

    void CrearBorde(Transform padre, Vector2 centroBorde, Vector2Int dir, bool esPuerta, List<Puerta> puertas)
    {
        bool horizontal = dir.y != 0;       // borde arriba/abajo = segmento horizontal
        float largo = horizontal ? tamCelda.x : tamCelda.y;

        if (!esPuerta)
        {
            Vector2 size = horizontal ? new Vector2(largo, grosorPared) : new Vector2(grosorPared, largo);
            CrearCuadro("Pared", padre, centroBorde, size, spritePared, colorPared, true, 0);
            return;
        }

        // con puerta: 2 trozos de pared + 1 puerta en el centro
        float gap = Mathf.Min(anchoPuerta, largo * 0.6f);
        float stub = (largo - gap) / 2f;

        if (horizontal)
        {
            if (stub > 0.05f)
            {
                CrearCuadro("Pared", padre, centroBorde + new Vector2(-(gap / 2f + stub / 2f), 0f), new Vector2(stub, grosorPared), spritePared, colorPared, true, 0);
                CrearCuadro("Pared", padre, centroBorde + new Vector2( (gap / 2f + stub / 2f), 0f), new Vector2(stub, grosorPared), spritePared, colorPared, true, 0);
            }
            puertas.Add(CrearPuerta(padre, centroBorde, new Vector2(gap, grosorPared)));
        }
        else
        {
            if (stub > 0.05f)
            {
                CrearCuadro("Pared", padre, centroBorde + new Vector2(0f, -(gap / 2f + stub / 2f)), new Vector2(grosorPared, stub), spritePared, colorPared, true, 0);
                CrearCuadro("Pared", padre, centroBorde + new Vector2(0f,  (gap / 2f + stub / 2f)), new Vector2(grosorPared, stub), spritePared, colorPared, true, 0);
            }
            puertas.Add(CrearPuerta(padre, centroBorde, new Vector2(grosorPared, gap)));
        }
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
        GameObject go = new GameObject("Puerta");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = local;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco();
        sr.sortingOrder = 1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();   // 1x1 * escala = size

        Puerta p = go.AddComponent<Puerta>();
        p.barrera = col;
        p.sprite = sr;
        p.spriteAbierta = Verde();
        p.spriteCerrada = Rojo();
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

        Botin botin = e.AddComponent<Botin>();
        botin.prefabMoneda = prefabMoneda; botin.probabilidad = probMoneda;

        // Torreta estatica: SIN Rigidbody dinamico (queda fija) y dispara por linea de vision
        if (tipo == 3)
        {
            sr.color = new Color(0.30f, 0.80f, 0.85f);
            EnemigoTorreta torreta = e.AddComponent<EnemigoTorreta>();
            torreta.prefabProyectil = prefabBala;
            return e;
        }

        Rigidbody2D rb = e.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (tipo == 0)
        {
            sr.color = new Color(0.85f, 0.22f, 0.22f);
            e.AddComponent<Enemy>();
        }
        else if (tipo == 1)
        {
            sr.color = new Color(0.92f, 0.55f, 0.20f);
            Enemigo disp = e.AddComponent<Enemigo>();
            disp.prefabProyectil = prefabBala;
        }
        else
        {
            sr.color = new Color(0.62f, 0.32f, 0.82f);
            EnemigoDisparoX x = e.AddComponent<EnemigoDisparoX>();
            x.prefabProyectil = prefabBala;
        }
        return e;
    }

    void SpawnJefe(Transform cont, List<GameObject> lista, Vector2 centro)
    {
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

        Jefe jefe = e.AddComponent<Jefe>();
        jefe.prefabProyectil = prefabBala;

        Botin botin = e.AddComponent<Botin>();
        botin.prefabMoneda = prefabMoneda; botin.probabilidad = 1f; botin.minMonedas = 3; botin.maxMonedas = 6;

        lista.Add(e);
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
        GameObject o = new GameObject(destructible ? "Caja" : "Roca");
        o.transform.position = worldPos;
        o.transform.SetParent(cont, true);
        o.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        SpriteRenderer sr = o.AddComponent<SpriteRenderer>();
        Sprite sp = destructible ? spriteObstaculoCaja : spriteObstaculoRoca;
        if (sp != null) sr.sprite = sp;
        else { sr.sprite = Blanco(); sr.color = destructible ? new Color(0.55f, 0.4f, 0.22f) : new Color(0.45f, 0.45f, 0.5f); }
        sr.sortingOrder = 4;

        o.AddComponent<BoxCollider2D>();

        Obstaculo obs = o.AddComponent<Obstaculo>();
        obs.destructible = destructible;
        obs.vida = destructible ? 2f : 1f;
        if (destructible)
        {
            Botin b = o.AddComponent<Botin>();
            b.prefabMoneda = prefabMoneda; b.probabilidad = 0.5f;
            obs.botin = b;
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
