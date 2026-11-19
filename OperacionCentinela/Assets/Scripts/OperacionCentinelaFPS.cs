// OPERACION CENTINELA - FPS de tres niveles (Unity 6).
// Jeffrey Fernandez - Matricula 1-21-1476.
// Se coloca en el GameObject "Director del juego" de la escena
// "01 Galeria de disparos". Todos los escenarios y personajes se
// construyen automaticamente al pulsar Play.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OperacionCentinelaFPS : MonoBehaviour
{
    private enum Estado { Menu, Jugando, Pausa, Victoria, Derrota }

    private class Objetivo
    {
        public GameObject objeto;
        public int puntos;
        public int vida;
        public int vidaMaxima;
        public bool enemigo;
        public bool jefe;
        public float disparoEn;
        public float fase;
        public float velocidad;
        public Vector3 centroInicial;
        public Transform modeloVisual;
        public Vector3 modeloPosicionBase;
        public Vector3 modeloEscalaBase;
        public Quaternion modeloRotacionBase;
        public float ultimoAtaque;
    }

    private class Proyectil
    {
        public GameObject objeto;
        public Vector3 direccion;
        public Vector3 posicion;
        public GameObject dueño;
        public float restante;
        public float velocidad;
        public bool enemigo;
    }

    private class Efecto
    {
        public GameObject objeto;
        public float restante;
        public float total;
        public Vector3 velocidad;
    }

    private readonly List<Objetivo> objetivos = new List<Objetivo>();
    private readonly List<Proyectil> proyectiles = new List<Proyectil>();
    private readonly List<Efecto> efectos = new List<Efecto>();

    private Estado estado = Estado.Menu;
    private GameObject escenario;
    private GameObject jugador;
    private CharacterController controlador;
    private Camera camara;
    private Transform arma;
    private GameObject armaRifleImportada;
    private GameObject armaBlasterImportada;
    private string nombreArma = "RIFLE SCI-FI";
    private GameObject fogonazo;
    private AudioSource musica;
    private AudioSource efectosSonido;

    private Material suelo, hormigon, metal, metalOscuro, rojo, naranja, cian;
    private Material oro, blanco, cristal, verde, cielo, madera;
    private Material materialEstelas;
    private Texture2D blancoGUI;
    private float giroHorizontal;
    private float giroVertical;
    private float velocidadVertical;
    private float sacudidaArma;
    private float flashDisparoHasta;
    private float heridoHasta;
    private float siguienteTiro;
    private float ultimoDisparo;
    private float avisoHasta;
    private string aviso = "";
    private int nivel = 1;
    private int vidas = 5;
    private int municion = 18;
    private int puntos;
    private int aciertos;
    private int disparos;
    private float tiempoNivel;
    private float finNivelHasta = -1f;

    private const float Sensibilidad = 2.4f;
    private const float Rapidez = 7f;

    private void Awake()
    {
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
        PrepararMateriales();
        PrepararJugador();
        PrepararAudio();
        CargarNivel(1);
        estado = Estado.Menu;
        LiberarRaton();
    }

    private void OnDisable()
    {
        LiberarRaton();
    }

    private void Update()
    {
        ActualizarEfectos();
        if (estado == Estado.Jugando)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                estado = Estado.Pausa;
                LiberarRaton();
                return;
            }

            tiempoNivel += Time.deltaTime;
            MirarConRaton();
            MoverJugador();
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquiparArma(false);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquiparArma(true);
            ActualizarArma();
            if (Input.GetMouseButtonDown(0)) Disparar();
            ActualizarProyectiles();
            ActualizarObjetivos();
            ComprobarFinNivel();
        }
        else if (estado == Estado.Pausa && Input.GetKeyDown(KeyCode.Escape))
        {
            estado = Estado.Jugando;
            CapturarRaton();
        }
    }

    // -----------------------------------------------------------------
    // CONFIGURACION GENERAL
    // -----------------------------------------------------------------
    private Material MaterialColor(string nombre, Color color, bool brillo = false)
    {
        Shader shader = null;
        if (GraphicsSettings.currentRenderPipeline != null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        Material material = new Material(shader);
        material.name = nombre;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", brillo ? 0.68f : 0.08f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", brillo ? 0.7f : 0.22f);
        if (brillo)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.3f);
        }
        return material;
    }

    private void PrepararMateriales()
    {
        suelo = MaterialColor("Arena gris azulada", new Color(.17f, .22f, .27f));
        hormigon = MaterialColor("Hormigon", new Color(.36f, .40f, .45f));
        metal = MaterialColor("Acero claro", new Color(.57f, .64f, .69f), true);
        metalOscuro = MaterialColor("Metal tactico", new Color(.09f, .13f, .19f), true);
        rojo = MaterialColor("Alerta roja", new Color(.9f, .13f, .13f), true);
        naranja = MaterialColor("Senal naranja", new Color(.98f, .52f, .14f), true);
        cian = MaterialColor("Energia cian", new Color(.1f, .84f, .98f), true);
        oro = MaterialColor("Titanio dorado", new Color(.73f, .48f, .19f), true);
        blanco = MaterialColor("Blanco", new Color(.9f, .91f, .89f));
        cristal = MaterialColor("Cristal", new Color(.18f, .44f, .54f), true);
        verde = MaterialColor("Oliva", new Color(.20f, .32f, .28f));
        cielo = MaterialColor("Azul profundo", new Color(.06f, .10f, .16f));
        madera = MaterialColor("Madera", new Color(.45f, .32f, .23f));
        Shader shaderEstela = Shader.Find("Sprites/Default");
        if (shaderEstela == null) shaderEstela = Shader.Find("Unlit/Color");
        materialEstelas = new Material(shaderEstela);
        blancoGUI = Texture2D.whiteTexture;
        TexturaIndustrial(suelo, 17, .75f);
        TexturaIndustrial(hormigon, 43, .88f);
        TexturaIndustrial(metalOscuro, 67, .90f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.52f, .58f, .66f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(.13f, .20f, .26f);
        RenderSettings.fogStartDistance = 22;
        RenderSettings.fogEndDistance = 90;
    }

    private void TexturaIndustrial(Material m, int semilla, float contraste)
    {
        const int n = 128;
        Texture2D imagen = new Texture2D(n, n, TextureFormat.RGB24, true);
        imagen.name = "Textura industrial generada " + semilla;
        imagen.wrapMode = TextureWrapMode.Repeat;
        imagen.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float ruido = Mathf.PerlinNoise((x + semilla * 9f) * .085f,
                    (y + semilla * 5f) * .085f);
                float raya = ((x + semilla) % 41 < 2 ||
                    (y + semilla) % 51 < 2) ? .77f : 1f;
                float intensidad = Mathf.Lerp(.73f, 1.17f, ruido) * raya *
                    contraste;
                pixels[y * n + x] = new Color(intensidad, intensidad,
                    intensidad, 1f);
            }
        }
        imagen.SetPixels(pixels);
        imagen.Apply(true);
        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", imagen);
            m.SetTextureScale("_BaseMap", new Vector2(2, 2));
        }
        else if (m.HasProperty("_MainTex"))
        {
            m.SetTexture("_MainTex", imagen);
            m.SetTextureScale("_MainTex", new Vector2(2, 2));
        }
    }

    private GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre,
        Vector3 posicion, Vector3 escala, Material material, bool solido = false)
    {
        GameObject o = GameObject.CreatePrimitive(tipo);
        o.name = nombre;
        o.transform.SetParent(padre, false);
        o.transform.localPosition = posicion;
        o.transform.localScale = escala;
        Renderer r = o.GetComponent<Renderer>();
        if (r != null && material != null) r.sharedMaterial = material;
        if (!solido)
        {
            Collider c = o.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }
        return o;
    }

    private GameObject Cubo(string nombre, Transform padre, Vector3 posicion,
        Vector3 escala, Material material, bool solido = false)
    {
        return Pieza(PrimitiveType.Cube, nombre, padre, posicion, escala, material, solido);
    }

    private GameObject Cilindro(string nombre, Transform padre, Vector3 posicion,
        Vector3 escala, Material material, bool solido = false)
    {
        return Pieza(PrimitiveType.Cylinder, nombre, padre, posicion, escala, material, solido);
    }

    private GameObject Esfera(string nombre, Transform padre, Vector3 posicion,
        Vector3 escala, Material material, bool solido = false)
    {
        return Pieza(PrimitiveType.Sphere, nombre, padre, posicion, escala, material, solido);
    }

    private void PrepararJugador()
    {
        jugador = new GameObject("JUGADOR - Jeffrey Fernandez");
        controlador = jugador.AddComponent<CharacterController>();
        controlador.height = 1.8f;
        controlador.radius = .36f;
        controlador.center = new Vector3(0f, .93f, 0f);
        controlador.stepOffset = .35f;
        controlador.slopeLimit = 48f;

        GameObject oCamara = new GameObject("Camara FPS");
        oCamara.transform.SetParent(jugador.transform, false);
        oCamara.transform.localPosition = new Vector3(0f, 1.56f, 0f);
        oCamara.tag = "MainCamera";
        camara = oCamara.AddComponent<Camera>();
        camara.fieldOfView = 68f;
        camara.nearClipPlane = .04f;
        camara.farClipPlane = 110;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(.07f, .13f, .20f);
        oCamara.AddComponent<AudioListener>();
        CrearArma();
    }

    private void CrearArma()
    {
        GameObject soporte = new GameObject("Armas 3D - primera persona");
        soporte.transform.SetParent(camara.transform, false);
        arma = soporte.transform;
        arma.localPosition = new Vector3(.33f, -.32f, .62f);

        // Dos armas realmente importadas desde los ZIP de Quaternius y Kenney.
        // La geometria antigua permanece como alternativa si faltan los FBX.
        armaRifleImportada = PaquetesVisualesFPS.Arma(
            "ThirdParty/SciFi/Gun_Rifle", arma,
            new Vector3(0, -.02f, .33f), 1.08f, "SciFiArma",
            new Vector3(0, -90, 0));
        armaBlasterImportada = PaquetesVisualesFPS.Arma(
            "ThirdParty/Kenney/BlasterLargo", arma,
            new Vector3(0, -.02f, .32f), 1.05f, "Kenney",
            new Vector3(90, 0, 0));
        if (armaRifleImportada != null || armaBlasterImportada != null)
        {
            Cubo("Antebrazo tactico", arma, new Vector3(.23f, -.28f, -.19f),
                new Vector3(.19f, .22f, .51f), verde);
            Cubo("Guante blindado", arma, new Vector3(.10f, -.18f, .04f),
                new Vector3(.18f, .13f, .23f), metalOscuro);
            fogonazo = Esfera("Fogonazo de disparo", arma,
                new Vector3(0, .015f, .95f),
                new Vector3(.22f, .22f, .28f), naranja);
            fogonazo.SetActive(false);
            EquiparArma(armaRifleImportada == null);
            return;
        }

        Cubo("Cuerpo del rifle", arma, new Vector3(0, 0, .22f),
            new Vector3(.20f, .19f, .66f), metalOscuro);
        Cubo("Recubrimiento superior", arma, new Vector3(0, .125f, .24f),
            new Vector3(.17f, .07f, .42f), metal);
        Cubo("Riel", arma, new Vector3(0, .175f, .17f),
            new Vector3(.12f, .045f, .35f), cian);
        Cubo("Culata", arma, new Vector3(0, -.025f, -.27f),
            new Vector3(.18f, .18f, .28f), metalOscuro);
        Cubo("Cargador", arma, new Vector3(0, -.18f, .12f),
            new Vector3(.15f, .25f, .21f), metal);
        Cubo("Empunadura", arma, new Vector3(.035f, -.22f, -.10f),
            new Vector3(.11f, .27f, .12f), metalOscuro);
        GameObject canion = Cilindro("Canon", arma,
            new Vector3(0, -.02f, .69f), new Vector3(.058f, .27f, .058f), metal);
        canion.transform.localRotation = Quaternion.Euler(90, 0, 0);
        fogonazo = Esfera("Fogonazo de disparo", arma,
            new Vector3(0, -.02f, .97f), new Vector3(.22f, .22f, .25f), naranja);
        fogonazo.SetActive(false);
        // Brazos estilizados que aparecen en pantalla.
        Cubo("Brazo derecho", arma, new Vector3(.23f, -.30f, -.11f),
            new Vector3(.18f, .19f, .54f), verde);
        Cubo("Guante", arma, new Vector3(.13f, -.20f, .04f),
            new Vector3(.18f, .12f, .23f), metalOscuro);
        // Detalles de rifle tipo carabina: mirilla, empuñadura,
        // compartimento de municion, tubos y placas luminosas.
        Cubo("Armadura del guardamanos", arma, new Vector3(0, 0, .59f),
            new Vector3(.23f, .19f, .37f), metalOscuro);
        Cubo("Riel superior segmentado", arma, new Vector3(0, .16f, .49f),
            new Vector3(.18f, .035f, .49f), metal);
        for (int i = 0; i < 5; i++)
            Cubo("Acanaladura superior", arma,
                new Vector3(0, .185f, .31f + i * .085f),
                new Vector3(.18f, .025f, .032f), metalOscuro);
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Cubo("Panel lateral rifle", arma,
                new Vector3(lado * .118f, -.02f, .49f),
                new Vector3(.028f, .145f, .33f), metal);
            Cubo("Ranura ventilacion", arma,
                new Vector3(lado * .139f, .02f, .48f),
                new Vector3(.015f, .035f, .17f), cian);
            Cubo("Anillo abrazadera", arma,
                new Vector3(lado * .055f, -.03f, .78f),
                new Vector3(.055f, .12f, .035f), metalOscuro);
        }
        Cubo("Base visor holografico", arma,
            new Vector3(0, .25f, .29f), new Vector3(.17f, .09f, .22f), metal);
        Cubo("Pantalla visor", arma,
            new Vector3(0, .37f, .29f), new Vector3(.06f, .21f, .05f), cian);
        Cubo("Perno de cerrojo", arma,
            new Vector3(-.13f, .085f, .09f), new Vector3(.12f, .06f, .15f), metal);
        Cubo("Caja del cargador", arma, new Vector3(0, -.30f, .13f),
            new Vector3(.17f, .20f, .24f), metalOscuro);
        Cubo("Guardamonte", arma, new Vector3(0, -.12f, -.10f),
            new Vector3(.17f, .06f, .19f), metal);
    }

    private void EquiparArma(bool blaster)
    {
        if (armaRifleImportada == null && armaBlasterImportada == null)
        {
            nombreArma = "CARABINA";
            return;
        }
        if (blaster && armaBlasterImportada == null) blaster = false;
        if (!blaster && armaRifleImportada == null) blaster = true;
        if (armaRifleImportada != null) armaRifleImportada.SetActive(!blaster);
        if (armaBlasterImportada != null) armaBlasterImportada.SetActive(blaster);
        nombreArma = blaster ? "BLASTER KENNEY" : "RIFLE SCI-FI";
        if (estado == Estado.Jugando)
        {
            aviso = "ARMA EQUIPADA: " + nombreArma;
            avisoHasta = Time.time + 1.25f;
        }
    }

    private void PrepararAudio()
    {
        musica = gameObject.AddComponent<AudioSource>();
        musica.playOnAwake = false;
        musica.loop = true;
        musica.volume = .32f;
        efectosSonido = gameObject.AddComponent<AudioSource>();
        efectosSonido.playOnAwake = false;
        efectosSonido.volume = .75f;
        CambiarMusica(false);
    }

    private void Sonar(string nombre, float volumen = 1f)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + nombre);
        if (clip != null) efectosSonido.PlayOneShot(clip, volumen);
    }

    private void CambiarMusica(bool jefe)
    {
        AudioClip clip = Resources.Load<AudioClip>(jefe ? "Audio/Boss" : "Audio/Ambiente");
        if (clip == musica.clip && musica.isPlaying) return;
        musica.Stop();
        musica.clip = clip;
        musica.volume = jefe ? .36f : .30f;
        if (clip != null) musica.Play();
    }

    private void CapturarRaton()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LiberarRaton()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // -----------------------------------------------------------------
    // CREACION DE LOS TRES NIVELES
    // -----------------------------------------------------------------
    private void CargarNivel(int nuevoNivel)
    {
        nivel = nuevoNivel;
        if (escenario != null) Destroy(escenario);
        for (int i = 0; i < proyectiles.Count; i++)
            if (proyectiles[i].objeto != null) Destroy(proyectiles[i].objeto);
        proyectiles.Clear();
        for (int i = 0; i < efectos.Count; i++)
            if (efectos[i].objeto != null) Destroy(efectos[i].objeto);
        efectos.Clear();
        objetivos.Clear();

        escenario = new GameObject("ESCENARIO - NIVEL " + nivel);
        RenderSettings.fogColor = nivel == 3 ?
            new Color(.18f, .09f, .12f) :
            (nivel == 2 ? new Color(.20f, .16f, .14f) :
            new Color(.12f, .19f, .27f));
        camara.backgroundColor = RenderSettings.fogColor;
        ConstruirBase();
        if (nivel == 1)
        {
            municion = 18;
            ConstruirGaleria();
        }
        else if (nivel == 2)
        {
            municion = 36;
            ConstruirPatioEnemigos();
        }
        else
        {
            municion = 45;
            ConstruirArenaBoss();
        }
        velocidadVertical = -1f;
        controlador.enabled = false;
        jugador.transform.position = new Vector3(0f, .12f, -18f);
        jugador.transform.rotation = Quaternion.identity;
        controlador.enabled = true;
        giroHorizontal = 0f;
        giroVertical = 0f;
        camara.transform.localRotation = Quaternion.identity;
        tiempoNivel = 0f;
        finNivelHasta = -1f;
        aviso = nivel == 1 ? "DESTRUYE LAS DIANAS" :
            nivel == 2 ? "ELIMINA LOS CENTINELAS Y LAS DIANAS" : "DERROTA AL TITAN-01";
        avisoHasta = Time.time + 4f;
        CambiarMusica(nivel == 3);
    }

    private void ConstruirBase()
    {
        Transform r = escenario.transform;
        // Suelo y muros exteriores, todos con colision real.
        Cubo("Suelo blindado", r, new Vector3(0, -.28f, 11),
            new Vector3(44, .56f, 70), suelo, true);
        Cubo("Muro izquierda", r, new Vector3(-22.3f, 3, 11),
            new Vector3(.8f, 6, 70), hormigon, true);
        Cubo("Muro derecha", r, new Vector3(22.3f, 3, 11),
            new Vector3(.8f, 6, 70), hormigon, true);
        Cubo("Muro al fondo", r, new Vector3(0, 3, 45.8f),
            new Vector3(45, 6, .8f), hormigon, true);
        Cubo("Muro entrada", r, new Vector3(0, 3, -24),
            new Vector3(45, 6, .8f), hormigon, true);

        // Señalizacion, baldosas, luces y mobiliario industrial.
        for (int z = -21; z < 44; z += 6)
        {
            for (int x = -18; x <= 18; x += 6)
            {
                Cubo("Junta de pavimento", r, new Vector3(x, .015f, z),
                    new Vector3(5.7f, .025f, 5.7f), hormigon);
            }
            Cubo("Carril naranja lateral", r, new Vector3(-18.3f, .037f, z),
                new Vector3(.13f, .012f, 4.0f), naranja);
            Cubo("Carril cian lateral", r, new Vector3(18.3f, .037f, z),
                new Vector3(.13f, .012f, 4.0f), cian);
        }

        for (int z = -17; z < 44; z += 12)
        {
            PosteIluminado(-19.6f, z);
            PosteIluminado(19.6f, z);
            Barril(-20.3f, z + 4.1f);
            Barril(20.3f, z + 4.1f);
        }

        // Cajas y edificios separados del eje central de disparo.
        for (int z = -8; z < 38; z += 13)
        {
            Contenedor(-18.0f, z, z % 2 == 0 ? verde : metalOscuro);
            Contenedor(18.0f, z + 3, z % 2 == 0 ? metalOscuro : verde);
        }
        Torre(-19f, 34f);
        Torre(19f, 34f);
        DecorarComplejo();
        DecorarConPacks();

        GameObject sol = new GameObject("Luz ambiental del complejo");
        sol.transform.SetParent(r, false);
        sol.transform.rotation = Quaternion.Euler(44, -32, 0);
        Light l = sol.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.3f;
        l.color = new Color(.84f, .89f, 1f);
        l.shadows = LightShadows.Soft;
    }

    private void DecorarComplejo()
    {
        Transform r = escenario.transform;
        Material luces = nivel == 3 ? rojo : (nivel == 2 ? naranja : cian);
        // Fachadas interiores sobre las paredes reales: paneles
        // industriales, contrafuertes, ventanas, barandillas y vigas.
        for (int lado = -1; lado <= 1; lado += 2)
        {
            float px = lado * 20.70f;
            for (int z = -17; z <= 40; z += 11)
            {
                Cubo("Modulo arquitectura industrial", r,
                    new Vector3(px, 4.30f, z),
                    new Vector3(2.2f, 7.2f, 9.5f), metalOscuro);
                Cubo("Revestimiento vertical", r,
                    new Vector3(lado * 19.50f, 4.1f, z),
                    new Vector3(.11f, 5.6f, 8.2f), hormigon);
                for (int i = -1; i <= 1; i++)
                {
                    Cubo("Ventana blindada", r,
                        new Vector3(lado * 19.40f, 5.30f, z + i * 2.6f),
                        new Vector3(.055f, 1.08f, 1.72f), cristal);
                    Cubo("Zocalo de ventana", r,
                        new Vector3(lado * 19.32f, 4.66f, z + i * 2.6f),
                        new Vector3(.07f, .10f, 1.88f), metal);
                    Cubo("Franja luminosa", r,
                        new Vector3(lado * 19.28f, 3.15f, z + i * 2.6f),
                        new Vector3(.07f, .07f, 2.13f), luces);
                }
                Cubo("Pilar modular", r, new Vector3(lado * 19.22f, 3.5f, z + 4.7f),
                    new Vector3(.42f, 7f, .55f), metal);
                Cubo("Luz vertical pilar", r,
                    new Vector3(lado * 18.98f, 3.7f, z + 4.7f),
                    new Vector3(.07f, 2.6f, .15f), luces);
                Cubo("Alero superior", r,
                    new Vector3(lado * 18.6f, 7.3f, z),
                    new Vector3(4.2f, .35f, 9.2f), hormigon);
                // Conductos de refrigeracion de la pared.
                for (int p = 0; p < 2; p++)
                {
                    Cilindro("Tubo refrigeracion", r,
                        new Vector3(lado * (20.15f + p * .37f),
                        7.65f, z), new Vector3(.10f, 4.1f, .10f), metal)
                        .transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
            }
            // Protección de las zonas laterales.
            for (int z = -16; z < 39; z += 8)
            {
                Cubo("Barrera militar baja", r,
                    new Vector3(lado * 16.1f, .40f, z),
                    new Vector3(.29f, .80f, 2.60f), hormigon, true);
                Cubo("Pintura de seguridad", r,
                    new Vector3(lado * 15.92f, .58f, z),
                    new Vector3(.03f, .12f, 2.60f), luces);
            }
        }
        // Techos parciales y refuerzos del hangar: el jugador puede
        // seguir viendo el fondo y disparar a todos los blancos.
        for (int z = -15; z < 44; z += 12)
        {
            Cubo("Viga transversal elevada", r,
                new Vector3(0, 8.2f, z),
                new Vector3(42.0f, .46f, .63f), metal);
            Cubo("Luminaria central", r,
                new Vector3(0, 7.9f, z),
                new Vector3(5.2f, .14f, .23f), luces);
            for (int lado = -1; lado <= 1; lado += 2)
                Cubo("Cercha diagonal", r,
                    new Vector3(lado * 11.8f, 7.60f, z),
                    new Vector3(9f, .14f, .34f), metalOscuro);
        }
        for (int z = -17; z < 42; z += 4)
        {
            Cubo("Baliza centro izquierda", r,
                new Vector3(-2.4f, .047f, z),
                new Vector3(.10f, .02f, 2.1f), luces);
            Cubo("Baliza centro derecha", r,
                new Vector3(2.4f, .047f, z),
                new Vector3(.10f, .02f, 2.1f), luces);
        }
    }

    // Escenografia autentica: modelos FBX de los paquetes gratuitos suministrados.
    // Se coloca a los lados para mantener visibles las dianas y el eje de juego.
    private void DecorarConPacks()
    {
        Transform r = escenario.transform;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                PaquetesVisualesFPS.Decoracion(
                    "ThirdParty/Modular/Column_1", r,
                    new Vector3(lado * 18.9f, 0, -13f + i * 12f),
                    4.65f, "Modular", lado == 1 ? 0 : 180);
                PaquetesVisualesFPS.Decoracion(
                    "ThirdParty/SciFi/Prop_Crate", r,
                    new Vector3(lado * 16.8f, 0, -9f + i * 11f),
                    1.23f, "SciFiProp", 28f + i * 18f);
            }
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/Modular/Props_Computer", r,
                new Vector3(lado * 18.3f, 0, -10f),
                2.1f, "Modular", lado == 1 ? -90 : 90);
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/Modular/Props_Laser", r,
                new Vector3(lado * 17.65f, 0, 15f),
                1.5f, "Modular", lado == 1 ? 90 : -90);
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/Modular/Props_Shelf", r,
                new Vector3(lado * 18.1f, 0, 33f),
                2.3f, "Modular", lado == 1 ? 90 : -90);
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/Kenney/CajaKenney", r,
                new Vector3(lado * 16.8f, 0, 26.5f),
                1.05f, "Kenney", 20f);
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/SciFi/Prop_Barrel1", r,
                new Vector3(lado * 16.7f, 0, -1f),
                1.2f, "SciFiProp");
        }
        PaquetesVisualesFPS.Decoracion(
            "ThirdParty/Modular/Door_Double", r,
            new Vector3(0, 0, 44.65f), 6f, "Modular", 180f);
        PaquetesVisualesFPS.Decoracion(
            "ThirdParty/SciFi/Prop_SatelliteDish", r,
            new Vector3(-18.2f, 0, 38.5f), 3.6f, "SciFiProp", 135f);
        if (nivel == 2)
        {
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/SciFi/Prop_Locker", r,
                new Vector3(17.5f, 0, -15f), 2.7f, "SciFiProp", 90f);
        }
        if (nivel == 3)
        {
            PaquetesVisualesFPS.Decoracion(
                "ThirdParty/Modular/Props_Teleporter_1", r,
                new Vector3(0, 0, 38.4f), 5.5f, "Modular", 180f);
            for (int lado = -1; lado <= 1; lado += 2)
                PaquetesVisualesFPS.Decoracion(
                    "ThirdParty/Modular/Props_Vessel_Tall", r,
                    new Vector3(lado * 11f, 0, 35f), 4.2f, "Modular");
        }
    }

    private void PosteIluminado(float x, float z)
    {
        Transform r = escenario.transform;
        Cilindro("Poste de acero", r, new Vector3(x, 2.7f, z),
            new Vector3(.13f, 2.7f, .13f), metal);
        Cubo("Faro", r, new Vector3(x, 5.3f, z),
            new Vector3(.8f, .20f, .7f), cian);
    }

    private void Contenedor(float x, float z, Material pintura)
    {
        Transform r = escenario.transform;
        Cubo("Contenedor industrial", r, new Vector3(x, 1.3f, z),
            new Vector3(3.2f, 2.6f, 4.2f), pintura, true);
        for (int i = -2; i <= 2; i++)
            Cubo("Nervadura", r, new Vector3(x + i * .51f, 1.4f, z - 2.13f),
                new Vector3(.09f, 2.3f, .06f), metal);
        Cubo("Placa amarilla", r, new Vector3(x, 2.1f, z - 2.18f),
            new Vector3(1.1f, .2f, .04f), naranja);
    }

    private void Barril(float x, float z)
    {
        Transform r = escenario.transform;
        Cilindro("Bidon metalico", r, new Vector3(x, .64f, z),
            new Vector3(.48f, .64f, .48f), metal, true);
        Cilindro("Banda de seguridad", r, new Vector3(x, .78f, z),
            new Vector3(.492f, .08f, .492f), naranja);
        Cilindro("Tapa", r, new Vector3(x, 1.30f, z),
            new Vector3(.49f, .035f, .49f), metalOscuro);
    }

    private void Torre(float x, float z)
    {
        Transform r = escenario.transform;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Cubo("Pata torre", r, new Vector3(x + sx * .9f, 2, z + sz * .9f),
                    new Vector3(.16f, 4f, .16f), metal);
        Cubo("Plataforma superior", r, new Vector3(x, 4.1f, z),
            new Vector3(2.4f, .22f, 2.4f), hormigon);
        Cubo("Panel luminoso", r, new Vector3(x, 4.5f, z),
            new Vector3(1.2f, .3f, .12f), cian);
    }

    private void ConstruirGaleria()
    {
        // 12 dianas con valores diferentes. Destruirlas TODAS abre el nivel 2.
        int[] recompensas = { 50, 100, 150, 200 };
        Material[] pinturas = { blanco, cian, naranja, oro };
        for (int fila = 0; fila < 3; fila++)
        {
            for (int columna = 0; columna < 4; columna++)
            {
                float x = (columna - 1.5f) * 8.4f;
                float z = 7.5f + fila * 13.0f;
                int tipo = (fila + columna) % 4;
                CrearDiana(new Vector3(x, 1.8f + fila * .15f, z),
                    recompensas[tipo], pinturas[tipo]);
                Cubo("Peana de tiro", escenario.transform,
                    new Vector3(x, .52f, z + .6f),
                    new Vector3(1.1f, 1.04f, 1.1f), metalOscuro);
            }
        }
        Cubo("Cabecera galeria", escenario.transform, new Vector3(0, 5.5f, 41f),
            new Vector3(15f, 1f, .4f), metalOscuro);
        for (int i = -6; i <= 6; i += 3)
            Cubo("Marcador luminoso", escenario.transform,
                new Vector3(i, 5.52f, 40.7f), new Vector3(1.5f, .24f, .1f), cian);
    }

    private void CrearDiana(Vector3 posicion, int recompensa, Material zonaCentral)
    {
        GameObject root = new GameObject("DIANA - " + recompensa + " puntos");
        root.transform.SetParent(escenario.transform, false);
        root.transform.localPosition = posicion;
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(2.16f, 2.16f, .38f);
        collider.center = new Vector3(0, 0, .08f);

        Disco("Disco exterior", root.transform, 0, 2.12f, metalOscuro);
        Disco("Circulo blanco", root.transform, -.13f, 1.75f, blanco);
        Disco("Circulo rojo", root.transform, -.21f, 1.30f, rojo);
        Disco("Centro puntuable", root.transform, -.29f, .68f, zonaCentral);
        Cubo("Columna diana", root.transform, new Vector3(0, -1f, .36f),
            new Vector3(.17f, 1.4f, .17f), metal);

        objetivos.Add(new Objetivo {
            objeto = root, puntos = recompensa, vida = 1, vidaMaxima = 1,
            centroInicial = root.transform.position
        });
    }

    private void Disco(string nombre, Transform padre, float z, float diametro, Material material)
    {
        GameObject circulo = Cilindro(nombre, padre, new Vector3(0, 0, z),
            new Vector3(diametro * .5f, .07f, diametro * .5f), material);
        circulo.transform.localRotation = Quaternion.Euler(90f, 0, 0);
    }

    private void ConstruirPatioEnemigos()
    {
        for (int i = 0; i < 6; i++)
        {
            int columna = i % 3;
            int fila = i / 3;
            CrearDiana(new Vector3((columna - 1) * 10, 2f,
                fila == 0 ? 11f : 31f), fila == 0 ? 100 : 200,
                fila == 0 ? naranja : cian);
        }
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 7.0f;
            float z = (i % 2 == 0) ? 20f : 27f;
            CrearCentinela(new Vector3(x, 0f, z), i);
        }
        Cubo("Muralla de operaciones", escenario.transform,
            new Vector3(0, 4.3f, 42f), new Vector3(14, 4f, .6f), metalOscuro);
    }

    private void CrearCentinela(Vector3 posicion, int indice)
    {
        int tipoEnemigo = indice % 3;
        string nombreTipo = tipoEnemigo == 0 ? "EyeDrone" :
            tipoEnemigo == 1 ? "QuadShell" : "Trilobite";
        GameObject root = new GameObject("ENEMIGO " + (indice + 1) +
            " - " + nombreTipo);
        root.transform.SetParent(escenario.transform, false);
        root.transform.localPosition = posicion;
        CapsuleCollider c = root.AddComponent<CapsuleCollider>();
        // Hitbox adaptada a cada silueta, para no obligar al jugador
        // a acertar al centro exacto de los robots mas anchos.
        c.center = new Vector3(0, tipoEnemigo == 0 ? 1.38f : 1.2f, 0);
        c.height = tipoEnemigo == 0 ? 1.48f : 2.4f;
        c.radius = tipoEnemigo == 0 ? .69f :
            tipoEnemigo == 1 ? 1.11f : 1.05f;
        // Variantes distintas de enemigos: dron volador, caparazon y trilobite.
        string ruta = tipoEnemigo == 0 ? "ThirdParty/SciFi/Enemy_EyeDrone" :
            tipoEnemigo == 1 ? "ThirdParty/SciFi/Enemy_QuadShell" :
                "ThirdParty/SciFi/Enemy_Trilobite";
        GameObject modeloCentinela = PaquetesVisualesFPS.Personaje(
            ruta, root.transform,
            tipoEnemigo == 0 ? 1.35f : tipoEnemigo == 1 ? 2.0f : 2.4f,
            tipoEnemigo == 0 ? .69f : 0f, "SciFiEnemigo", 180f);
        if (modeloCentinela == null)
            modeloCentinela = InstanciarModeloDetallado(
                "CentinelaRobot", root.transform, 1.03f);
        if (modeloCentinela == null)
        {
        Cubo("Torso mecanico", root.transform, new Vector3(0, 1.28f, 0),
            new Vector3(.9f, .88f, .65f), metalOscuro);
        Cubo("Placa pecho", root.transform, new Vector3(0, 1.35f, -.35f),
            new Vector3(.67f, .40f, .10f), rojo);
        Esfera("Cabeza", root.transform, new Vector3(0, 2.02f, 0),
            new Vector3(.65f, .53f, .57f), metal);
        Cubo("Visor rojo", root.transform, new Vector3(0, 2.06f, -.29f),
            new Vector3(.52f, .12f, .1f), rojo);
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Cubo("Hombro angular", root.transform,
                new Vector3(lado * .63f, 1.58f, 0),
                new Vector3(.35f, .36f, .63f), rojo);
            Cubo("Brazo", root.transform,
                new Vector3(lado * .67f, .99f, 0),
                new Vector3(.22f, .75f, .25f), metal);
            Cubo("Pierna", root.transform,
                new Vector3(lado * .24f, .37f, 0),
                new Vector3(.27f, .75f, .32f), metalOscuro);
            Cubo("Bota", root.transform,
                new Vector3(lado * .24f, .08f, -.15f),
                new Vector3(.35f, .17f, .56f), metal);
        }
        }
        objetivos.Add(new Objetivo {
            objeto = root, puntos = 300, vida = 2, vidaMaxima = 2,
            enemigo = true, disparoEn = Time.time + 2.2f + indice * .32f,
            velocidad = 1.1f + indice * .07f,
            fase = indice, centroInicial = posicion,
            modeloVisual = modeloCentinela == null ? null : modeloCentinela.transform,
            modeloPosicionBase = modeloCentinela == null ? Vector3.zero :
                modeloCentinela.transform.localPosition,
            modeloEscalaBase = modeloCentinela == null ? Vector3.one :
                modeloCentinela.transform.localScale,
            modeloRotacionBase = modeloCentinela == null ? Quaternion.identity :
                modeloCentinela.transform.localRotation
        });
    }

    private void ConstruirArenaBoss()
    {
        Transform r = escenario.transform;
        // Ultimo nivel: SOLO el boss. Los elementos decorativos no son enemigos.
        Cubo("Puerta de reactor", r, new Vector3(0, 5.8f, 44f),
            new Vector3(17f, 11f, 1.3f), metalOscuro);
        Cilindro("Nucleo ambiental", r, new Vector3(0, 5, 43.1f),
            new Vector3(3f, .25f, 3f), rojo).transform.localRotation =
            Quaternion.Euler(90, 0, 0);
        for (int i = -3; i <= 3; i++)
        {
            Cubo("Pasarela iluminada", r,
                new Vector3(i * 5f, .04f, 10f),
                new Vector3(.2f, .04f, 30f), i % 2 == 0 ? naranja : rojo);
        }
        for (int i = -1; i <= 1; i += 2)
        {
            Cubo("Columna energetica", r,
                new Vector3(i * 16f, 3, 29f),
                new Vector3(1.7f, 6f, 1.7f), metalOscuro, true);
            Cilindro("Nucleo del pilar", r,
                new Vector3(i * 16f, 3f, 29f),
                new Vector3(.52f, 3.1f, .52f), cian);
        }
        // Portón circular con halo rojo y escalones. Enmarca al jefe
        // cuando la cámara apunta hacia el fondo.
        for (int anillo = 0; anillo < 3; anillo++)
        {
            GameObject pieza = Cilindro("Reactor circular nivel " + anillo,
                r, new Vector3(0, 5.1f, 40.9f - anillo * .12f),
                new Vector3(5.0f - anillo * .68f, .08f,
                    5.0f - anillo * .68f),
                anillo == 1 ? rojo : metalOscuro);
            pieza.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        for (int i = 0; i < 8; i++)
        {
            float angulo = 2f * Mathf.PI * i / 8f;
            Cubo("Cerrojo del reactor", r, new Vector3(
                Mathf.Cos(angulo) * 4.70f,
                5.1f + Mathf.Sin(angulo) * 4.70f, 40.5f),
                new Vector3(.63f, .73f, .35f), oro)
                .transform.localRotation = Quaternion.Euler(0, 0, -angulo * Mathf.Rad2Deg);
        }
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Cubo("Monolito alarma", r, new Vector3(lado * 8.2f, 2.4f, 17f),
                new Vector3(2.2f, 4.8f, 2.2f), metalOscuro, true);
            Cubo("Nervio rojo monolito", r,
                new Vector3(lado * 8.2f, 3.4f, 15.85f),
                new Vector3(1.5f, .17f, .07f), rojo);
            Cubo("Conduccion energia", r,
                new Vector3(lado * 8.2f, .055f, 27f),
                new Vector3(.16f, .045f, 21f), rojo);
        }
        // Acercamos al jefe para que su silueta domine la pantalla.
        CrearBoss(new Vector3(0, 0, 15));
    }

    private void CrearBoss(Vector3 posicion)
    {
        GameObject root = new GameObject("BOSS FINAL - TITAN 01");
        root.transform.SetParent(escenario.transform, false);
        root.transform.localPosition = posicion;
        CapsuleCollider c = root.AddComponent<CapsuleCollider>();
        c.center = new Vector3(0, 2.86f, 0);
        c.height = 5.70f;
        c.radius = 1.63f;

        GameObject modeloTitan = PaquetesVisualesFPS.Personaje(
            "ThirdParty/Mechas/Mecha_George", root.transform,
            5.6f, 0f, "Mecha", 180f);
        if (modeloTitan == null)
            modeloTitan = InstanciarModeloDetallado(
                "Titan01", root.transform, 1.08f);
        if (modeloTitan == null)
        {
        Cubo("Torso armadura", root.transform, new Vector3(0, 2.5f, 0),
            new Vector3(2.8f, 2.0f, 1.4f), oro);
        Cubo("Pecheras negras", root.transform, new Vector3(0, 2.55f, -.73f),
            new Vector3(2.1f, 1.13f, .17f), metalOscuro);
        Cilindro("Nucleo vital", root.transform, new Vector3(0, 2.67f, -.87f),
            new Vector3(.45f, .15f, .45f), rojo).transform.localRotation =
            Quaternion.Euler(90, 0, 0);
        Cubo("Casco", root.transform, new Vector3(0, 3.85f, 0),
            new Vector3(1.18f, .95f, 1.04f), metalOscuro);
        Cubo("Visor de ojos", root.transform, new Vector3(0, 3.93f, -.54f),
            new Vector3(.82f, .15f, .09f), rojo);
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Cubo("Hombrera gigante", root.transform,
                new Vector3(lado * 1.65f, 3.5f, 0),
                new Vector3(1.48f, .9f, 1.8f), oro);
            Cubo("Brazo mecanico", root.transform,
                new Vector3(lado * 1.78f, 2.2f, 0),
                new Vector3(.9f, 1.7f, 1.03f), metalOscuro);
            Cubo("Antebrazo", root.transform,
                new Vector3(lado * 1.78f, 1.45f, -.25f),
                new Vector3(1.05f, .9f, 1.15f), oro);
            Cubo("Muslo", root.transform,
                new Vector3(lado * .73f, .90f, 0),
                new Vector3(.98f, 1.55f, 1.14f), metalOscuro);
            Cubo("Bota mecanica", root.transform,
                new Vector3(lado * .73f, .24f, -.25f),
                new Vector3(1.18f, .48f, 1.7f), oro);
            Cubo("Energia lateral", root.transform,
                new Vector3(lado * 1.78f, 1.50f, -.84f),
                new Vector3(.65f, .18f, .07f), rojo);
        }
        }
        objetivos.Add(new Objetivo {
            objeto = root, puntos = 2500, vida = 20, vidaMaxima = 20,
            enemigo = true, jefe = true, disparoEn = Time.time + 2f,
            velocidad = 1.25f, centroInicial = posicion,
            modeloVisual = modeloTitan == null ? null : modeloTitan.transform,
            modeloPosicionBase = modeloTitan == null ? Vector3.zero :
                modeloTitan.transform.localPosition,
            modeloEscalaBase = modeloTitan == null ? Vector3.one :
                modeloTitan.transform.localScale,
            modeloRotacionBase = modeloTitan == null ? Quaternion.identity :
                modeloTitan.transform.localRotation
        });
    }

    // Las mallas FBX son originales y se incluyen en Resources/Modelos.
    // En caso de que Unity no importe un FBX, conservamos el modelo
    // geometrico antiguo como alternativa funcional sin perder el nivel.
    private GameObject InstanciarModeloDetallado(string nombre, Transform padre,
        float escala)
    {
        GameObject prefab = Resources.Load<GameObject>("Modelos/" + nombre);
        if (prefab == null) return null;
        GameObject visual = Instantiate(prefab, padre, false);
        visual.name = nombre + " - malla FBX";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * escala;
        visual.transform.localRotation = Quaternion.Euler(0, 180, 0);

        foreach (Collider c in visual.GetComponentsInChildren<Collider>(true))
            Destroy(c);
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] lista = r.sharedMaterials;
            for (int i = 0; i < lista.Length; i++)
            {
                string n = lista[i] == null ? r.gameObject.name.ToLowerInvariant() :
                    lista[i].name.ToLowerInvariant();
                string pieza = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("roja") || n.Contains("naranja") ||
                    pieza.Contains("visor") || pieza.Contains("bocaca") ||
                    pieza.Contains("luz") || pieza.Contains("led") ||
                    pieza.Contains("reactorrojo") || pieza.Contains("interiorreactor") ||
                    pieza.Contains("puntosensor") || pieza.Contains("protectorrojo"))
                    lista[i] = n.Contains("naranja") || pieza.Contains("interiorreactor")
                        ? naranja : rojo;
                else if (n.Contains("cian") || pieza.Contains("escape") ||
                    pieza.Contains("cian"))
                    lista[i] = cian;
                else if (n.Contains("dorado") || pieza.Contains("hombreratitan") ||
                    pieza.Contains("armadura") || pieza.Contains("puntera") ||
                    pieza.Contains("casco") || pieza.Contains("pechera"))
                    lista[i] = oro;
                else if (n.Contains("plata") || n.Contains("pulido") ||
                    pieza.Contains("articulacion") || pieza.Contains("acero") ||
                    pieza.Contains("rodilla") || pieza.Contains("servo"))
                    lista[i] = metal;
                else
                    lista[i] = metalOscuro;
            }
            r.sharedMaterials = lista;
        }
        return visual;
    }

    // -----------------------------------------------------------------
    // JUGADOR, DISPAROS, ENEMIGOS Y COLISIONES
    // -----------------------------------------------------------------
    private void MirarConRaton()
    {
        giroHorizontal += Input.GetAxis("Mouse X") * Sensibilidad;
        giroVertical = Mathf.Clamp(giroVertical - Input.GetAxis("Mouse Y") *
            Sensibilidad, -76f, 76f);
        jugador.transform.rotation = Quaternion.Euler(0, giroHorizontal, 0);
        camara.transform.localRotation = Quaternion.Euler(giroVertical, 0, 0);
    }

    private void MoverJugador()
    {
        float x = 0, z = 0;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x--;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x++;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) z++;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) z--;
        Vector3 plano = (jugador.transform.right * x + jugador.transform.forward * z)
            .normalized * Rapidez;
        if (controlador.isGrounded && velocidadVertical < 0) velocidadVertical = -2f;
        if (controlador.isGrounded && Input.GetKeyDown(KeyCode.Space))
            velocidadVertical = 5.8f;
        velocidadVertical -= 18f * Time.deltaTime;
        controlador.Move((plano + Vector3.up * velocidadVertical) * Time.deltaTime);
    }

    private void ActualizarArma()
    {
        sacudidaArma = Mathf.MoveTowards(sacudidaArma, 0, Time.deltaTime * 3.0f);
        float caminar = controlador.velocity.magnitude > .4f ?
            Mathf.Sin(Time.time * 10f) * .012f : 0f;
        arma.localPosition = new Vector3(.33f, -.32f - sacudidaArma * .10f + caminar,
            .62f - sacudidaArma * .20f);
        arma.localRotation = Quaternion.Euler(-sacudidaArma * 15f, 0, 0);
        fogonazo.SetActive(Time.time < flashDisparoHasta);
    }

    private void Disparar()
    {
        if (Time.time < siguienteTiro || municion <= 0) return;
        siguienteTiro = Time.time + .23f;
        ultimoDisparo = Time.time;
        municion--;
        disparos++;
        sacudidaArma = .85f;
        flashDisparoHasta = Time.time + .09f;
        Sonar("Disparo");

        Vector3 direccion = camara.transform.forward;
        Vector3 inicio = camara.transform.position + direccion * .51f +
            camara.transform.right * .15f - camara.transform.up * .12f;
        // El proyectil es REAL y visible; viaja desde el arma a la mira.
        RaycastHit impacto;
        if (Physics.Raycast(camara.transform.position, direccion,
            out impacto, 100f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
            direccion = (impacto.point - inicio).normalized;
        CrearProyectil(inicio, direccion, false, jugador, 56f);
    }

    private void CrearProyectil(Vector3 inicio, Vector3 direccion, bool hostil,
        GameObject dueño, float rapidez)
    {
        GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        objeto.name = hostil ? "Proyectil enemigo rojo" : "Bala del jugador";
        objeto.layer = 2; // Ignore Raycast: un proyectil no tapa a otro.
        objeto.transform.position = inicio;
        objeto.transform.localScale = hostil ?
            new Vector3(.27f, .27f, .27f) : new Vector3(.12f, .12f, .28f);
        objeto.GetComponent<Renderer>().sharedMaterial = hostil ? rojo : cian;
        TrailRenderer estela = objeto.AddComponent<TrailRenderer>();
        estela.time = hostil ? .36f : .19f;
        estela.startWidth = hostil ? .18f : .095f;
        estela.endWidth = .008f;
        estela.minVertexDistance = .06f;
        estela.material = materialEstelas;
        estela.startColor = hostil ?
            new Color(1f, .15f, .10f, 1f) : new Color(.2f, 1f, 1f, 1f);
        estela.endColor = hostil ?
            new Color(1f, .1f, .04f, 0f) : new Color(.3f, 1f, 1f, 0f);
        Destroy(objeto.GetComponent<Collider>());
        objeto.transform.rotation = Quaternion.LookRotation(direccion);
        proyectiles.Add(new Proyectil {
            objeto = objeto, posicion = inicio, direccion = direccion.normalized,
            dueño = dueño, velocidad = rapidez, restante = hostil ? 5f : 2f,
            enemigo = hostil
        });
    }

    private void ActualizarProyectiles()
    {
        for (int i = proyectiles.Count - 1; i >= 0; i--)
        {
            Proyectil b = proyectiles[i];
            if (b.objeto == null)
            {
                proyectiles.RemoveAt(i);
                continue;
            }
            float avanzar = b.velocidad * Time.deltaTime;
            Vector3 destino = b.posicion + b.direccion * avanzar;
            // SphereCastAll permite ignorar al dueño sin ignorar al jugador.
            RaycastHit[] impactos = Physics.SphereCastAll(b.posicion,
                b.enemigo ? .14f : .075f, b.direccion, avanzar,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            Collider colision = null;
            Vector3 punto = destino;
            for (int j = 0; j < impactos.Length; j++)
            {
                RaycastHit hit = impactos[j];
                if (hit.collider == null) continue;
                if (b.dueño != null &&
                    (hit.collider.gameObject == b.dueño ||
                    hit.collider.transform.IsChildOf(b.dueño.transform))) continue;
                if (hit.distance < menor)
                {
                    menor = hit.distance;
                    colision = hit.collider;
                    punto = hit.point;
                }
            }

            if (colision != null)
            {
                if (b.enemigo)
                {
                    if (colision.gameObject == jugador ||
                        colision.transform.IsChildOf(jugador.transform))
                        DañarJugador();
                }
                else
                {
                    Objetivo o = BuscarObjetivo(colision.transform);
                    if (o != null) GolpearObjetivo(o);
                }
                CrearImpacto(punto, b.enemigo ? rojo : cian);
                Destroy(b.objeto);
                proyectiles.RemoveAt(i);
                continue;
            }

            b.posicion = destino;
            b.objeto.transform.position = destino;
            b.restante -= Time.deltaTime;
            if (b.restante <= 0)
            {
                Destroy(b.objeto);
                proyectiles.RemoveAt(i);
            }
        }
    }

    private Objetivo BuscarObjetivo(Transform t)
    {
        for (int i = 0; i < objetivos.Count; i++)
        {
            GameObject go = objetivos[i].objeto;
            if (go != null && (t == go.transform || t.IsChildOf(go.transform)))
                return objetivos[i];
        }
        return null;
    }

    private void GolpearObjetivo(Objetivo objetivo)
    {
        objetivo.vida--;
        aciertos++;
        Sonar("Impacto", .76f);
        if (objetivo.vida <= 0)
        {
            puntos += objetivo.puntos;
            aviso = objetivo.jefe ? "TITAN-01 DESTRUIDO +2500" :
                "+" + objetivo.puntos + " PUNTOS";
            avisoHasta = Time.time + 1.4f;
            Destroy(objetivo.objeto);
            objetivos.Remove(objetivo);
        }
        else if (objetivo.jefe)
        {
            aviso = "IMPACTO EN EL TITAN";
            avisoHasta = Time.time + .55f;
        }
    }

    private void ActualizarObjetivos()
    {
        for (int i = 0; i < objetivos.Count; i++)
        {
            Objetivo o = objetivos[i];
            if (!o.enemigo || o.objeto == null) continue;
            if (o.modeloVisual != null)
            {
                float a = Time.time * (o.jefe ? 1.6f : 4.8f) + o.fase;
                float oscilar = Mathf.Sin(a) * (o.jefe ? .045f : .065f);
                o.modeloVisual.localPosition = o.modeloPosicionBase +
                    new Vector3(0, oscilar, 0);
                o.modeloVisual.localRotation = o.modeloRotacionBase *
                    Quaternion.Euler(
                        Mathf.Sin(a * .65f) * (o.jefe ? 1.8f : 2.5f),
                        0f, Mathf.Sin(a) * (o.jefe ? 2.3f : 1.4f));
                float retroceso = Mathf.Clamp01(1f - (Time.time - o.ultimoAtaque) * 4f);
                o.modeloVisual.localScale = o.modeloEscalaBase *
                    (1f + retroceso * .022f);
            }
            Vector3 diferencia = jugador.transform.position - o.objeto.transform.position;
            diferencia.y = 0;
            float distancia = diferencia.magnitude;
            if (distancia > .1f)
                // El visor modelado mira al eje local -Z.
                o.objeto.transform.rotation = Quaternion.LookRotation(-diferencia.normalized);

            if (distancia > (o.jefe ? 8f : 3f) && distancia < 29f)
            {
                Vector3 mover = diferencia.normalized * o.velocidad *
                    Time.deltaTime * (o.jefe && o.vida <= 10 ? 1.55f : 1f);
                Vector3 sig = o.objeto.transform.position + mover;
                sig.x = Mathf.Clamp(sig.x, -16.0f, 16.0f);
                sig.z = Mathf.Clamp(sig.z, -11.0f, 40.0f);
                o.objeto.transform.position = sig;
            }
            else if (!o.jefe)
            {
                Vector3 p = o.objeto.transform.position;
                p.x += Mathf.Sin(Time.time * 1.4f + o.fase) * .009f;
                o.objeto.transform.position = p;
            }

            // Contacto del jugador con el enemigo causa perdida de vida.
            if (distancia < (o.jefe ? 2.0f : 1.05f))
                DañarJugador();

            if (distancia < (o.jefe ? 35f : 20f) && Time.time >= o.disparoEn)
            {
                DisparoEnemigo(o, diferencia);
                float demora = o.jefe ? (o.vida <= 10 ? 1.12f : 1.9f) :
                    2.2f + Mathf.Repeat(o.fase * .3f, .6f);
                o.disparoEn = Time.time + demora;
            }
        }
    }

    private void DisparoEnemigo(Objetivo o, Vector3 diferencia)
    {
        o.ultimoAtaque = Time.time;
        Vector3 inicio = o.objeto.transform.position + Vector3.up *
            (o.jefe ? 2.9f : 1.7f);
        Vector3 destino = jugador.transform.position + Vector3.up * 1.25f;
        Vector3 dir = (destino - inicio).normalized;
        CrearProyectil(inicio, dir, true, o.objeto, o.jefe ? 17f : 13f);
        if (o.jefe && o.vida <= 10)
        {
            // Segunda fase: salva lateral, sin balas invisibles.
            CrearProyectil(inicio + Vector3.right * .6f,
                Quaternion.Euler(0, -12, 0) * dir, true, o.objeto, 18f);
            CrearProyectil(inicio - Vector3.right * .6f,
                Quaternion.Euler(0, 12, 0) * dir, true, o.objeto, 18f);
        }
        CrearImpacto(inicio, rojo);
        Sonar("Disparo", .18f);
    }

    private void DañarJugador()
    {
        if (Time.time < heridoHasta || estado != Estado.Jugando) return;
        vidas--;
        heridoHasta = Time.time + 1.25f;
        Sonar("Herido");
        aviso = "¡IMPACTO! QUEDAN " + vidas + " VIDAS";
        avisoHasta = Time.time + 1.45f;
        if (vidas <= 0) Terminar(false, "Te has quedado sin vidas.");
    }

    private void CrearImpacto(Vector3 posicion, Material material)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = "Destello de impacto";
        o.layer = 2;
        o.transform.position = posicion;
        o.transform.localScale = Vector3.one * .32f;
        o.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(o.GetComponent<Collider>());
        efectos.Add(new Efecto {
            objeto = o, restante = .24f, total = .24f, velocidad = Vector3.up * .9f
        });
    }

    private void ActualizarEfectos()
    {
        for (int i = efectos.Count - 1; i >= 0; i--)
        {
            Efecto e = efectos[i];
            if (e.objeto == null) { efectos.RemoveAt(i); continue; }
            e.restante -= Time.deltaTime;
            if (e.restante <= 0)
            {
                Destroy(e.objeto);
                efectos.RemoveAt(i);
            }
            else
            {
                e.objeto.transform.localScale = Vector3.one * (.1f +
                    .50f * e.restante / e.total);
                e.objeto.transform.position += e.velocidad * Time.deltaTime;
            }
        }
    }

    private void ComprobarFinNivel()
    {
        if (estado != Estado.Jugando) return;
        if (objetivos.Count == 0)
        {
            if (finNivelHasta < 0f)
            {
                finNivelHasta = Time.time + 1.5f;
                aviso = "¡NIVEL COMPLETADO!";
                avisoHasta = finNivelHasta;
            }
            if (Time.time >= finNivelHasta)
            {
                if (nivel >= 3) Terminar(true, "Has destruido el Titan-01.");
                else CargarNivel(nivel + 1);
            }
            return;
        }

        // La ultima bala puede estar en vuelo: esperamos a su impacto.
        bool quedanBalasEnVuelo = false;
        for (int i = 0; i < proyectiles.Count; i++)
        {
            if (!proyectiles[i].enemigo) { quedanBalasEnVuelo = true; break; }
        }
        if (municion == 0 && !quedanBalasEnVuelo &&
            Time.time > ultimoDisparo + .30f)
            Terminar(false, "Municion agotada. No completaste el objetivo.");
    }

    private void IniciarPartida()
    {
        puntos = 0;
        aciertos = 0;
        disparos = 0;
        vidas = 5;
        heridoHasta = 0f;
        ultimoDisparo = 0f;
        siguienteTiro = 0f;
        CargarNivel(1);
        estado = Estado.Jugando;
        CapturarRaton();
    }

    private void Terminar(bool victoria, string motivo)
    {
        if (estado != Estado.Jugando) return;
        estado = victoria ? Estado.Victoria : Estado.Derrota;
        aviso = motivo;
        avisoHasta = float.MaxValue;
        LiberarRaton();
        Sonar(victoria ? "Victoria" : "Derrota");
        if (victoria) musica.Stop();
        Debug.Log((victoria ? "VICTORIA" : "DERROTA") + " | Puntos: " + puntos +
            " | Vidas: " + vidas + " | Aciertos: " + aciertos +
            " | Disparos: " + disparos);
    }

    // -----------------------------------------------------------------
    // INTERFAZ RESPONSIVA (sin Canvas ni prefabs externos).
    // -----------------------------------------------------------------
    private void Rectangulo(float x, float y, float ancho, float alto, Color color)
    {
        Color anterior = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, ancho, alto), blancoGUI);
        GUI.color = anterior;
    }

    private GUIStyle Texto(int tamaño, Color color, TextAnchor alineacion =
        TextAnchor.MiddleLeft, bool negrita = false)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = tamaño;
        estilo.normal.textColor = color;
        estilo.alignment = alineacion;
        estilo.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
        estilo.wordWrap = true;
        return estilo;
    }

    private void Etiqueta(Rect r, string valor, int tamaño, Color color,
        TextAnchor alineacion = TextAnchor.MiddleLeft, bool negrita = false)
    {
        GUI.Label(r, valor, Texto(tamaño, color, alineacion, negrita));
    }

    private bool Boton(Rect r, string texto)
    {
        Rectangulo(r.x, r.y, r.width, r.height, new Color(.07f, .61f, .70f, .95f));
        Rectangulo(r.x + 3, r.y + 3, r.width - 6, r.height - 6,
            new Color(.075f, .15f, .21f, .97f));
        GUIStyle boton = new GUIStyle(GUI.skin.button);
        boton.fontSize = 22;
        boton.fontStyle = FontStyle.Bold;
        boton.normal.textColor = Color.white;
        boton.hover.textColor = new Color(.26f, 1f, 1f);
        boton.active.textColor = Color.white;
        return GUI.Button(r, texto, boton);
    }

    private void OnGUI()
    {
        float escala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        if (escala < .1f) return;
        Matrix4x4 anterior = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(
            (Screen.width - 1280f * escala) * .5f,
            (Screen.height - 720f * escala) * .5f, 0),
            Quaternion.identity, new Vector3(escala, escala, 1f));

        if (estado == Estado.Jugando)
        {
            HUD();
        }
        else if (estado == Estado.Menu)
        {
            PantallaMenu();
        }
        else if (estado == Estado.Pausa)
        {
            HUD();
            Rectangulo(0, 0, 1280, 720, new Color(.01f, .02f, .06f, .73f));
            PanelCentral("PARTIDA EN PAUSA", "Mueve el raton para apuntar.", false);
            if (Boton(new Rect(474, 441, 332, 61), "CONTINUAR"))
            {
                estado = Estado.Jugando;
                CapturarRaton();
            }
            if (Boton(new Rect(474, 518, 332, 55), "MENU PRINCIPAL"))
            {
                estado = Estado.Menu;
                CargarNivel(1);
            }
        }
        else
        {
            Rectangulo(0, 0, 1280, 720, new Color(.01f, .025f, .05f, .83f));
            PanelCentral(estado == Estado.Victoria ? "MISION COMPLETADA" :
                "MISION FALLIDA", aviso, estado == Estado.Victoria);
            Etiqueta(new Rect(345, 369, 590, 57),
                "PUNTOS: " + puntos + "     VIDAS: " + vidas +
                "     ACIERTOS: " + aciertos,
                23, new Color(.92f, .91f, .84f), TextAnchor.MiddleCenter, true);
            if (Boton(new Rect(473, 465, 334, 63), "VOLVER A JUGAR"))
                IniciarPartida();
            if (Boton(new Rect(473, 545, 334, 58), "MENU PRINCIPAL"))
            {
                estado = Estado.Menu;
                CargarNivel(1);
            }
        }
        GUI.matrix = anterior;
    }

    private void HUD()
    {
        Rectangulo(17, 16, 374, 178, new Color(.02f, .05f, .10f, .84f));
        Rectangulo(17, 16, 374, 5, new Color(.13f, .84f, .95f));
        Etiqueta(new Rect(30, 25, 340, 32), "OPERACION CENTINELA", 20,
            new Color(.71f, .94f, 1f), negrita: true);
        Etiqueta(new Rect(30, 58, 325, 31), "PUNTOS   " + puntos, 23,
            Color.white, negrita: true);
        Etiqueta(new Rect(30, 90, 340, 27),
            "VIDAS  " + new string('♥', Mathf.Max(0, vidas)),
            21, new Color(1f, .43f, .43f), negrita: true);
        Etiqueta(new Rect(30, 120, 350, 26), "MUNICION   " + municion,
            19, new Color(1f, .87f, .46f), negrita: true);
        Etiqueta(new Rect(30, 153, 345, 27), "ARMA  " + nombreArma,
            15, new Color(.57f, .86f, 1f), negrita: true);

        string titulo = nivel == 1 ? "01  GALERIA DE DISPAROS" :
            nivel == 2 ? "02  PATIO DE CENTINELAS" : "03  NUCLEO DEL TITAN";
        Rectangulo(426, 15, 427, 56, new Color(.02f, .05f, .10f, .87f));
        Etiqueta(new Rect(435, 19, 407, 45), titulo, 20, Color.white,
            TextAnchor.MiddleCenter, true);
        Rectangulo(948, 15, 315, 72, new Color(.02f, .05f, .10f, .85f));
        Etiqueta(new Rect(962, 23, 292, 24), "JEFFREY FERNANDEZ", 17,
            Color.white, TextAnchor.MiddleRight, true);
        Etiqueta(new Rect(962, 49, 292, 22), "MATRICULA: 1-21-1476", 14,
            new Color(.63f, .85f, .91f), TextAnchor.MiddleRight);

        // Mira reticular.
        float centroX = 640, centroY = 360;
        Color mira = new Color(.36f, 1f, .95f, .95f);
        Rectangulo(centroX - 22, centroY - 1, 15, 2, mira);
        Rectangulo(centroX + 7, centroY - 1, 15, 2, mira);
        Rectangulo(centroX - 1, centroY - 22, 2, 15, mira);
        Rectangulo(centroX - 1, centroY + 7, 2, 15, mira);
        Rectangulo(centroX - 2, centroY - 2, 4, 4, Color.white);

        Rectangulo(23, 675, 590, 30, new Color(.01f, .05f, .08f, .7f));
        Etiqueta(new Rect(32, 677, 574, 25),
            "FLECHAS/WASD: MOVER | RATON: APUNTAR | CLIC: DISPARAR | 1/2: ARMA",
            12, Color.white);

        if (nivel == 3 && objetivos.Count > 0)
        {
            Objetivo jefe = null;
            for (int i = 0; i < objetivos.Count; i++)
                if (objetivos[i].jefe) { jefe = objetivos[i]; break; }
            if (jefe != null)
            {
                Rectangulo(355, 614, 570, 65, new Color(.02f, .02f, .04f, .9f));
                Etiqueta(new Rect(369, 617, 539, 23), "TITAN-01   JEFE FINAL",
                    19, Color.white, TextAnchor.MiddleCenter, true);
                Rectangulo(370, 649, 540, 14, new Color(.18f, .17f, .21f));
                Rectangulo(370, 649, 540f * jefe.vida / jefe.vidaMaxima, 14,
                    new Color(.96f, .18f, .19f));
            }
        }
        if (Time.time < avisoHasta)
        {
            Rectangulo(358, 94, 564, 48, new Color(.02f, .05f, .09f, .83f));
            Etiqueta(new Rect(365, 98, 550, 41), aviso, 21,
                new Color(.98f, .86f, .46f), TextAnchor.MiddleCenter, true);
        }
        if (Time.time < heridoHasta)
        {
            float alpha = Mathf.Clamp01((heridoHasta - Time.time) * .2f);
            Rectangulo(0, 0, 1280, 720, new Color(.9f, .05f, .02f, alpha));
        }
    }

    private void PantallaMenu()
    {
        Rectangulo(0, 0, 1280, 720, new Color(.02f, .035f, .07f, .84f));
        Rectangulo(142, 68, 996, 573, new Color(.04f, .10f, .15f, .92f));
        Rectangulo(142, 68, 996, 7, new Color(.13f, .88f, .94f));
        Etiqueta(new Rect(178, 106, 925, 85), "OPERACION CENTINELA", 52,
            Color.white, TextAnchor.MiddleCenter, true);
        Etiqueta(new Rect(256, 194, 765, 52),
            "FPS TACTICO  •  TRES MISIONES  •  UN JEFE FINAL",
            20, new Color(.45f, .95f, .99f), TextAnchor.MiddleCenter, true);
        Etiqueta(new Rect(230, 265, 822, 113),
            "01  GALERIA: Destruye las 12 dianas.\n" +
            "02  ASALTO: Acaba con los robots y dianas.\n" +
            "03  TITAN-01: Vence al jefe de la base.",
            21, Color.white, TextAnchor.MiddleCenter);
        Etiqueta(new Rect(224, 379, 830, 36),
            "FLECHAS / WASD: MOVER   ·   RATON: MIRAR   ·   CLIC: DISPARAR",
            17, new Color(.88f, .92f, .95f), TextAnchor.MiddleCenter);
        if (Boton(new Rect(430, 442, 420, 76), "INICIAR MISION"))
            IniciarPartida();
        Etiqueta(new Rect(252, 563, 780, 27),
            "Jeffrey Fernandez   ·   1-21-1476", 18,
            new Color(.68f, .83f, .91f), TextAnchor.MiddleCenter);
    }

    private void PanelCentral(string titulo, string subtitulo, bool triunfo)
    {
        Rectangulo(285, 164, 710, 290, new Color(.05f, .12f, .19f, .94f));
        Rectangulo(285, 164, 710, 6, triunfo ?
            new Color(.15f, .91f, .66f) : new Color(.91f, .20f, .19f));
        Etiqueta(new Rect(305, 207, 670, 75), titulo, 39, Color.white,
            TextAnchor.MiddleCenter, true);
        Etiqueta(new Rect(331, 295, 618, 58), subtitulo, 20,
            new Color(.79f, .87f, .92f), TextAnchor.MiddleCenter);
    }
}
