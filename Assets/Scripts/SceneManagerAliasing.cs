using UnityEngine;
using System.IO;

public class SceneManagerAliasing : SceneManagerBase
{
    [Header("Configuración Específica Escena A - Piso")]
	public DatosMaterial materialPiso;
    public Shader shaderActividad11;
    public Texture2D texturaPiso;
	public TexturaProcedural texturaProceduralPiso;
    public Texture2D mapaDeNormalesPiso;
    public string nombreArchivoObj = "piso.obj";

    [Header("Configuración Específica Escena A - Pava")]
	public DatosMaterial materialPava;
    public Shader shaderPava;
    public Texture2D texturaPava;
	public TexturaProcedural texturaProceduralPava;
    public Texture2D mapaDeNormalesPava;
    public string nombreArchivoDae = "base_model.dae";

    // --- VARIABLES DE TRANSFORMACIÓN ---
    public Vector3 posicionPava = new Vector3(0f, 0f, 0f);
    public Vector3 rotacionPava = new Vector3(0f, 0f, 0f);
    public Vector3 escalaPava = new Vector3(0.03f, 0.03f, 0.03f);

    protected override void ConstruirEscena()
    {
        if (shaderActividad11 == null)
        {
            Debug.LogError("Falta asignar el Shader del Piso en Escena A.");
            return;
        }

        // ==========================================
        // 1. CARGA DEL PISO
        // ==========================================
        string rutaPiso = Path.Combine(Application.dataPath, "Models/Escena A", nombreArchivoObj);
        Mesh meshPiso = File.Exists(rutaPiso) ? ObjParser.Parse(rutaPiso) : null;

        if (meshPiso != null)
        {
            GameObject pisoObj = new GameObject("Piso_Madera_EscenaA");
            pisoObj.transform.SetParent(this.transform);

            pisoObj.AddComponent<MeshFilter>().mesh = meshPiso;
            pisoObj.AddComponent<ModelMatrix>();

            Material matPiso = new Material(shaderActividad11);
           
           AplicarMaterialDinamico(matPiso, materialPiso, texturaPiso, texturaProceduralPiso, mapaDeNormalesPiso);

            pisoObj.AddComponent<MeshRenderer>().material = matPiso;
            objetosEscena.Add(pisoObj);
        }

        // ==========================================
        // 2. CARGA DE LA PAVA (DAE)
        // ==========================================
        string rutaPava = Path.Combine(Application.dataPath, "Models/Escena A", nombreArchivoDae);
        Mesh meshPava = File.Exists(rutaPava) ? DaeParser.Parse(rutaPava) : null;

        if (meshPava != null)
        {
            GameObject pavaObj = new GameObject("Pava_EscenaA");
            pavaObj.transform.SetParent(this.transform);

            escalaPava = new Vector3(0.03f, 0.03f, 0.03f);
            posicionPava = new Vector3(0f, 0.7f, 0f);
            rotacionPava = new Vector3(0f, 0f, 0f);

            pavaObj.transform.position = posicionPava;
            pavaObj.transform.eulerAngles = rotacionPava;
            pavaObj.transform.localScale = escalaPava;

            pavaObj.AddComponent<MeshFilter>().mesh = meshPava;
            pavaObj.AddComponent<ModelMatrix>();

            Shader shaderAUsar = shaderPava != null ? shaderPava : shaderActividad11;
            Material matPava = new Material(shaderAUsar);
			
			AplicarMaterialDinamico(matPava, materialPava, texturaPava, texturaProceduralPava, mapaDeNormalesPava);
           
            pavaObj.AddComponent<MeshRenderer>().material = matPava;
            objetosEscena.Add(pavaObj);
        }
		
		// ==========================================================
        // 3. VINCULACIÓN DE LA CÁMARA (LUGAR DE LA MODIFICACIÓN)
        // ==========================================================
        ControladorCamara camara = Object.FindFirstObjectByType<ControladorCamara>();
        if (camara != null)
        {
            // Cambiado para apuntar a 'posicionPava', con una distancia de 3.5f para ver el aliasing de cerca
            camara.ConfigurarCamara(posicionPava, 3.5f, 25f, new Vector3(0f, 1.5f, -4f));
        }

        // ==========================================
        // 4. CONFIGURACIÓN PROPIA DE LUCES (ESCENA A)
        // ==========================================
        LuzController luces = Object.FindFirstObjectByType<LuzController>();
        if (luces != null)
        {
            luces.rotacionDireccional = rotSolInicial;
            luces.dirColor = colorSol; 
            luces.intensidadDir = intensidadSolInicial;
            luces.velocidadRotacionSol = velocidadSol;

            luces.posPuntual = new Vector3(0f, 1.5f, -2f);
            luces.puntualColor = colorPuntual;
            luces.intensidadPuntual = 5.0f; // Intesidad alta para quemar el metal
            luces.radioPuntual = 10f;       // Radio amplio para bañar la pava

            luces.posSpot = posSpotInicial;
            luces.rotacionSpot = rotSpotInicial;
            luces.spotColor = colorSpot;
            luces.intensidadSpot = intensidadSpotInicial;
            luces.radioSpot = radioSpotInicial;
            luces.aperturaAngulo = aperturaSpotInicial;
        }
    }
	
	// --- EL CEREBRO DEL MATERIAL DINÁMICO (TU CÓDIGO ORIGINAL) ---
    private void AplicarMaterialDinamico(Material mat, DatosMaterial datos, Texture2D texPNG, TexturaProcedural texProc, Texture2D normalMap)
    {
        Color tinte = datos != null ? datos.colorTinte : Color.white;
        Color spec = datos != null ? datos.colorEspecular : Color.white;
        float shininess = datos != null ? datos.shininess : 32f;
        float rugosidad = datos != null ? datos.rugosidadPBR : 0.5f;
        float metalicidad = datos != null ? datos.metalicidadPBR : 0.0f;
        float opacidad = datos != null ? datos.opacidad : 1.0f;

        // Jerarquía de Texturas
        Texture2D texturaFinal = texPNG != null ? texPNG : (texProc != null ? texProc.GenerarTexturaEnMemoria() : null);

        mat.SetColor("_MatColor", tinte); 

        if (texturaFinal != null) {
            mat.SetTexture("_MainTex", texturaFinal);
            if (mat.HasProperty("_UseTexture")) mat.SetFloat("_UseTexture", 1f);
        } else {
            if (mat.HasProperty("_UseTexture")) mat.SetFloat("_UseTexture", 0f);
        }

        // Propiedades Físicas
        if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", spec);
        if (mat.HasProperty("_Shininess")) mat.SetFloat("_Shininess", shininess);
        if (mat.HasProperty("_Roughness")) mat.SetFloat("_Roughness", rugosidad);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalicidad);
        if (mat.HasProperty("_Opacidad")) mat.SetFloat("_Opacidad", opacidad);

        // Normales
        if (normalMap != null && mat.HasProperty("_NormalMap")) {
            mat.SetTexture("_NormalMap", normalMap);
            mat.SetFloat("_UseNormalMap", 1f);
        } else if (mat.HasProperty("_UseNormalMap")) {
            mat.SetFloat("_UseNormalMap", 0f);
        }

        // --- MAGIA DE TRANSPARENCIA DINÁMICA ---
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