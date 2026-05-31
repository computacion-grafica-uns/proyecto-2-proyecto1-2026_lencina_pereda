using UnityEngine;
using System.IO;

[RequireComponent(typeof(ModelMatrix))]
public class LuzController : MonoBehaviour
{
    [Header("Estados de Activación (Toggles - Teclas D, P, S)")]
    public bool dirActiva     = true;
    public bool puntualActiva = true;
    public bool spotActiva    = true;
	
    [Header("Configuración de Marcadores en Ejecución")]
    public bool mostrarMarcadorDir = true;     
    public bool mostrarMarcadorPuntual = true; 
    public bool mostrarMarcadorSpot = true;

    [Header("Modelos 3D (Carpeta Assets/Models)")]
    public string objSol = "Luz direccional - sol.obj";
    public string objLampara = "Luz Puntual - lampara.obj";
    public string objLinterna = "Luz Spot - Linterna.obj";

    [Header("Marcador Luz Direccional (Sol)")]
    public Shader shaderDir;
    public DatosMaterial matDataDir;
    public Texture2D texturaDir;
    public TexturaProcedural texturaProceduralDir; 
    public Texture2D normalDir;

    [Header("Marcador Luz Puntual")]
    public Shader shaderPuntual;
    public DatosMaterial matDataPuntual;
    public Texture2D texturaPuntual;
    public TexturaProcedural texturaProceduralPuntual; 
    public Texture2D normalPuntual;

    [Header("Marcador Luz Spot")]
    public Shader shaderSpot;
    public DatosMaterial matDataSpot;
    public Texture2D texturaSpot;
    public TexturaProcedural texturaProceduralSpot; 
    public Texture2D normalSpot;

    [HideInInspector] public Vector3 rotacionDireccional;
    [HideInInspector] public Color   dirColor;
    [HideInInspector] public float   intensidadDir;
    [HideInInspector] public float   velocidadRotacionSol;

    [HideInInspector] public Vector3 posPuntual;
    [HideInInspector] public Color   puntualColor;
    [HideInInspector] public float   intensidadPuntual;
    [HideInInspector] public float   radioPuntual;

    [HideInInspector] public Vector3 posSpot;
    [HideInInspector] public Vector3 rotacionSpot;
    [HideInInspector] public Color   spotColor;
    [HideInInspector] public float   intensidadSpot;
    [HideInInspector] public float   radioSpot;
    [HideInInspector] public float   aperturaAngulo;

    private ModelMatrix      modelMatrix;
    private SceneManagerBase sceneManager; 

    // --- MARCADORES VISUALES INTERNOS ---
    private GameObject marcadorPuntual;
    private GameObject marcadorSpot;
    private GameObject marcadorDir;
    private Material matPuntual;
    private Material matSpot;
    private Material matDir;

    public void Inicializar(SceneManagerBase manager)
    {
        sceneManager = manager;
    }

    private void Awake()
    {
        modelMatrix = GetComponent<ModelMatrix>();
    }

    private void Start()
    {
        if (sceneManager == null) sceneManager = Object.FindFirstObjectByType<SceneManagerBase>();
        Shader shaderPorDefecto = Shader.Find("Standard");

        // 1. Instanciar Marcador Luz Puntual (Lámpara por código)
        marcadorPuntual = CrearMarcadorDesdeOBJ(objLampara, "Marcador_Luz_Puntual", Vector3.one * 0.3f);
        Shader sPuntual = shaderPuntual != null ? shaderPuntual : shaderPorDefecto;
        matPuntual = new Material(sPuntual);
        marcadorPuntual.GetComponent<Renderer>().material = matPuntual;
        marcadorPuntual.SetActive(false);

        // 2. Instanciar Marcador Luz Spot (Linterna por código)
        marcadorSpot = CrearMarcadorDesdeOBJ(objLinterna, "Marcador_Luz_Spot", Vector3.one * 0.3f);
        Shader sSpot = shaderSpot != null ? shaderSpot : shaderPorDefecto;
        matSpot = new Material(sSpot);
        marcadorSpot.GetComponent<Renderer>().material = matSpot;
        marcadorSpot.SetActive(false);

        // 3. Instanciar Marcador Luz Direccional (Sol por código)
        marcadorDir = CrearMarcadorDesdeOBJ(objSol, "Marcador_Luz_Direccional", Vector3.one * 0.4f);
        Shader sDir = shaderDir != null ? shaderDir : shaderPorDefecto;
        matDir = new Material(sDir);
        marcadorDir.GetComponent<Renderer>().material = matDir;
        marcadorDir.SetActive(false);

        // Inyección automática en la lista de renderizado para recibir matrices de cámara
        if (sceneManager != null)
        {
            if (!sceneManager.objetosEscena.Contains(marcadorPuntual)) sceneManager.objetosEscena.Add(marcadorPuntual);
            if (!sceneManager.objetosEscena.Contains(marcadorSpot)) sceneManager.objetosEscena.Add(marcadorSpot);
            if (!sceneManager.objetosEscena.Contains(marcadorDir)) sceneManager.objetosEscena.Add(marcadorDir);
        }
    }

    private void Update()
    {
        // =====================================================================
        // 1. CONTROLES DE TECLADO ORIGINALES
        // =====================================================================
        
        // Toggles de Encendido / Apagado
        if (Input.GetKeyDown(KeyCode.D)) dirActiva     = !dirActiva;
        if (Input.GetKeyDown(KeyCode.P)) puntualActiva = !puntualActiva;
        if (Input.GetKeyDown(KeyCode.S)) spotActiva    = !spotActiva;

        // Rotación orbital del Sol
        if (Input.GetKey(KeyCode.Period)) rotacionDireccional.x += velocidadRotacionSol * Time.deltaTime;
        if (Input.GetKey(KeyCode.Comma))  rotacionDireccional.x -= velocidadRotacionSol * Time.deltaTime;

        // Manejo de Intensidades
        if (Input.GetKey(KeyCode.I)) intensidadDir     += Time.deltaTime * 2f;
        if (Input.GetKey(KeyCode.K)) intensidadDir      = Mathf.Max(0f, intensidadDir     - Time.deltaTime * 2f);
        if (Input.GetKey(KeyCode.O)) intensidadPuntual += Time.deltaTime * 2f;
        if (Input.GetKey(KeyCode.L)) intensidadPuntual  = Mathf.Max(0f, intensidadPuntual - Time.deltaTime * 2f);
        if (Input.GetKey(KeyCode.U)) intensidadSpot    += Time.deltaTime * 2f;
        if (Input.GetKey(KeyCode.J)) intensidadSpot     = Mathf.Max(0f, intensidadSpot    - Time.deltaTime * 2f);

        // Manejo de Radios y Apertura del Cono
        if (Input.GetKey(KeyCode.Y)) radioPuntual   += Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.H)) radioPuntual    = Mathf.Max(0.1f, radioPuntual - Time.deltaTime * 5f);
        if (Input.GetKey(KeyCode.M)) radioSpot      += Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.N)) radioSpot       = Mathf.Max(0.1f, radioSpot    - Time.deltaTime * 5f);
        if (Input.GetKey(KeyCode.G)) aperturaAngulo  = Mathf.Min(90f,  aperturaAngulo + Time.deltaTime * 20f);
        if (Input.GetKey(KeyCode.F)) aperturaAngulo  = Mathf.Max(0f,   aperturaAngulo - Time.deltaTime * 20f);

        // =====================================================================
        // 2. SINCRONIZACIÓN MATEMÁTICA CON LA GPU
        // =====================================================================
        SincronizarShader();
        
        // =====================================================================
        // 3. AL FINAL DE TODO: ACTUALIZACIÓN DE LOS MODELOS 3D (.OBJ)
        // =====================================================================
        if (marcadorPuntual != null)
        {
            bool estadoPuntual = puntualActiva && mostrarMarcadorPuntual;
            marcadorPuntual.SetActive(estadoPuntual);
            if (estadoPuntual)
            {
                marcadorPuntual.transform.position = posPuntual;
                Renderer r = marcadorPuntual.GetComponentInChildren<Renderer>();
                if (r != null) AplicarPropiedadesMarcador(r.sharedMaterial, matDataPuntual, texturaPuntual, texturaProceduralPuntual, normalPuntual);
            }
        }

        if (marcadorSpot != null)
        {
            bool estadoSpot = spotActiva && mostrarMarcadorSpot;
            marcadorSpot.SetActive(estadoSpot);
            if (estadoSpot)
            {
                marcadorSpot.transform.position = posSpot;
                marcadorSpot.transform.rotation = Quaternion.Euler(rotacionSpot); // <--- Clave para que la linterna .obj gire correctamente
                Renderer r = marcadorSpot.GetComponentInChildren<Renderer>();
                if (r != null) AplicarPropiedadesMarcador(r.sharedMaterial, matDataSpot, texturaSpot, texturaProceduralSpot, normalSpot);
            }
        }

        if (marcadorDir != null)
        {
            bool estadoDir = dirActiva && mostrarMarcadorDir;
            marcadorDir.SetActive(estadoDir);
            if (estadoDir)
            {
                marcadorDir.transform.position = transform.position + Vector3.up * 3f; 
                marcadorDir.transform.rotation = Quaternion.Euler(rotacionDireccional); // El sol rota acompañando los ejes
                Renderer r = marcadorDir.GetComponentInChildren<Renderer>();
                if (r != null) AplicarPropiedadesMarcador(r.sharedMaterial, matDataDir, texturaDir, texturaProceduralDir, normalDir);
            }
        }
    }

    private void SincronizarShader()
    {
        if (sceneManager == null) return;

        // Mantenemos las direcciones como Vector4 con w=0 (implícito al castear)
		Vector4 dirMundo = Quaternion.Euler(rotacionDireccional) * Vector3.forward;
		Vector4 dirSpotMundo = Quaternion.Euler(rotacionSpot) * Vector3.forward;

		// Las posiciones DEBEN ser Vector4 con w=1f para que el shader las calcule bien
		Vector4 posPuntualMundo = new Vector4(posPuntual.x, posPuntual.y, posPuntual.z, 1f);
		Vector4 posSpotMundo    = new Vector4(posSpot.x,    posSpot.y,    posSpot.z,    1f);

        foreach (GameObject obj in sceneManager.objetosEscena)
        {
            if (obj == null) continue;
            Renderer rend = obj.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            // Extraemos con sharedMaterials para evitar duplicaciones intermitentes en memoria
            Material[] mats = rend.sharedMaterials;

            foreach (Material m in mats)
            {
                if (m == null) continue;

                m.SetFloat("_DirLightActive",   dirActiva     ? 1f : 0f);
                m.SetFloat("_PointLightActive", puntualActiva ? 1f : 0f);
                m.SetFloat("_SpotLightActive",  spotActiva    ? 1f : 0f);

                m.SetFloat("_DirIntensity",   intensidadDir);
                m.SetFloat("_PointIntensity", intensidadPuntual);
                m.SetFloat("_SpotIntensity",  intensidadSpot);

                m.SetVector("_LightDirWorld",  dirMundo);
                m.SetVector("_LightPosWorld",  posPuntualMundo);
                m.SetVector("_SpotPosWorld",   posSpotMundo);
                m.SetVector("_SpotDirWorld",   dirSpotMundo);

                m.SetColor("_DirLightColor",   dirColor);
                m.SetColor("_PointLightColor", puntualColor);
                m.SetColor("_SpotLightColor",  spotColor);

                m.SetFloat("_PointLightRadius", radioPuntual);
                m.SetFloat("_SpotLightRadius",  radioSpot);
               m.SetFloat("_Apertura", aperturaAngulo);
            }
        }
    }

    private GameObject CrearMarcadorDesdeOBJ(string nombreArchivo, string nombreObjeto, Vector3 escalaBase)
    {
        GameObject obj = new GameObject(nombreObjeto);
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        string ruta = Path.Combine(Application.dataPath, "Models", nombreArchivo);
        Mesh malla = ObjParser.Parse(ruta);

        if (malla != null)
        {
            mf.sharedMesh = malla;
        }
        else
        {
            Debug.LogWarning($"[LuzController] No se encontró {ruta}. Usando esfera de respaldo.");
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mf.sharedMesh = fallback.GetComponent<MeshFilter>().sharedMesh;
            Destroy(fallback); 
        }

        obj.transform.localScale = escalaBase;
        return obj;
    }

    private void AplicarPropiedadesMarcador(Material mat, DatosMaterial datos, Texture2D texPNG, TexturaProcedural texProc, Texture2D normalMap)
    {
        if (mat == null) return;

        Color tinte = datos != null ? datos.colorTinte : Color.white;
        Color spec = datos != null ? datos.colorEspecular : Color.white;
        float shininess = datos != null ? datos.shininess : 32f;
        float rugosidad = datos != null ? datos.rugosidadPBR : 0.5f;
        float metalicidad = datos != null ? datos.metalicidadPBR : 0.0f;
        float opacidad = datos != null ? datos.opacidad : 1.0f;

        mat.SetColor("_MatColor", tinte); 

        // Jerarquía lógica de texturas
        Texture2D texturaFinal = texPNG != null ? texPNG : (texProc != null ? texProc.GenerarTexturaEnMemoria() : null);

        if (texturaFinal != null) {
            mat.SetTexture("_MainTex", texturaFinal);
            if (mat.HasProperty("_UseTexture")) mat.SetFloat("_UseTexture", 1f);
        } else {
            if (mat.HasProperty("_UseTexture")) mat.SetFloat("_UseTexture", 0f);
        }

        if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", spec);
        if (mat.HasProperty("_Shininess")) mat.SetFloat("_Shininess", shininess);
        if (mat.HasProperty("_Roughness")) mat.SetFloat("_Roughness", rugosidad);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalicidad);
        if (mat.HasProperty("_Opacidad")) mat.SetFloat("_Opacidad", opacidad);

        if (normalMap != null && mat.HasProperty("_NormalMap")) {
            mat.SetTexture("_NormalMap", normalMap);
            mat.SetFloat("_UseNormalMap", 1f);
        } else if (mat.HasProperty("_UseNormalMap")) {
            mat.SetFloat("_UseNormalMap", 0f);
        }

        // Control dinámico de transparencias Blinn-Phong / PBR
        if (opacidad < 1.0f) {
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3000; 
        } else {
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.renderQueue = 2000; 
        }
    }
}