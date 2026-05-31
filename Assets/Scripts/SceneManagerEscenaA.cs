using UnityEngine;
using System.IO;

public class SceneManagerEscenaA : SceneManagerBase
{
    [Header("Configuración Específica Escena A - Piso")]
    public Shader shaderPiso;
    public Texture2D texturaPiso; 
    public TexturaProcedural texturaProceduralPiso; 
    public Texture2D mapaDeNormalesPiso;
    public DatosMaterial materialPiso; 
    public string nombreArchivoObj = "piso.obj";

    [Header("Configuración Específica Escena A - 18 Pavas")]
    public string nombreArchivoDae = "base_model.dae";

    public Shader[] shadersPavas = new Shader[18];
    public Texture2D[] texturasPavas = new Texture2D[18];
    public TexturaProcedural[] texturasProceduralesPavas = new TexturaProcedural[18];
    public Texture2D[] mapasDeNormalesPavas = new Texture2D[18];
    public DatosMaterial[] materialesPavas = new DatosMaterial[18];

    private Vector3 posicionPava = new Vector3(0f, 0f, 0f); 
    private Vector3 rotacionPava = new Vector3(0f, 0f, 0f);
    private Vector3 escalaPava = new Vector3(0.03f, 0.03f, 0.03f);

    protected override void ConstruirEscena()
    {
        if (shaderPiso == null) return;

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

            Material matPiso = new Material(shaderPiso);
            AplicarMaterialDinamico(matPiso, materialPiso, texturaPiso, texturaProceduralPiso, mapaDeNormalesPiso);
            pisoObj.AddComponent<MeshRenderer>().material = matPiso;
            objetosEscena.Add(pisoObj);
        }

        // ==========================================
        // 2. CARGA DE LAS 18 PAVAS
        // ==========================================
        string rutaPava = Path.Combine(Application.dataPath, "Models/Escena A", nombreArchivoDae);
        // Parseamos la malla una sola vez por rendimiento
        Mesh meshPava = File.Exists(rutaPava) ? DaeParser.Parse(rutaPava) : null;

        if (meshPava != null)
        {
            // Parámetros físicos harcodeados internamente para limpiar el Inspector
            Vector3 escalaPava = new Vector3(0.02f, 0.02f, 0.02f);
            Vector3 rotacionPava = new Vector3(0f, 0f, 0f);
            float alturaBase = 0.6f;

            for (int i = 0; i < 18; i++)
            {
                int col = i % 6;
                int fila = i / 6;

                // Grilla centrada exactamente en el origen (0,0)
                float posX = -2.5f + (col * 1.0f);
                float posZ = -1.25f + (fila * 1.25f);
                
                Vector3 posicionIndividual = new Vector3(posX, alturaBase, posZ);

                // Extracción segura del Inspector para esta pava en específico
                Shader shaderActual = (shadersPavas != null && i < shadersPavas.Length && shadersPavas[i] != null) ? shadersPavas[i] : shaderPiso;
                DatosMaterial datosActual = (materialesPavas != null && i < materialesPavas.Length) ? materialesPavas[i] : null;
                Texture2D texActual = (texturasPavas != null && i < texturasPavas.Length) ? texturasPavas[i] : null;
                TexturaProcedural procActual = (texturasProceduralesPavas != null && i < texturasProceduralesPavas.Length) ? texturasProceduralesPavas[i] : null;
                Texture2D normalActual = (mapasDeNormalesPavas != null && i < mapasDeNormalesPavas.Length) ? mapasDeNormalesPavas[i] : null;

                // --- MAGIA DEL RENOMBRADO DINÁMICO ---
                string nMat = datosActual != null ? datosActual.name : "SinMat";
                
                string nTex = "SinTex";
                if (texActual != null) nTex = texActual.name;
                else if (procActual != null) nTex = procActual.name;
                
                string nNorm = normalActual != null ? normalActual.name : "SinNorm";

                string nShader = shaderActual != null ? shaderActual.name : "SinShader";
                if (nShader.Contains("/")) {
                    string[] partes = nShader.Split('/');
                    nShader = partes[partes.Length - 1];
                }

                // Generamos el nombre descriptivo con el índice al principio para mantener el orden
                string nombreGenerado = $"Pava_{i:00}_{nMat}_{nTex}_{nNorm}_{nShader}";

                // Instanciamos el objeto usando el nombre dinámico en lugar de "Pava_EscenaA_i"
                GameObject pavaObj = new GameObject(nombreGenerado);
                pavaObj.transform.SetParent(this.transform);
                
                pavaObj.transform.localPosition = posicionIndividual;
                pavaObj.transform.localEulerAngles = rotacionPava;
                pavaObj.transform.localScale = escalaPava;
                
                // Reutilizamos la misma malla parseada para ahorrar RAM
                pavaObj.AddComponent<MeshFilter>().mesh = meshPava;
                pavaObj.AddComponent<ModelMatrix>();

                // Creación de material aislado en memoria de video
                Material matPava = new Material(shaderActual);
                
                if (datosActual != null)
                {
                    AplicarMaterialDinamico(matPava, datosActual, texActual, procActual, normalActual);
                }
                
                pavaObj.AddComponent<MeshRenderer>().material = matPava;
                objetosEscena.Add(pavaObj);
            }
        }

        ControladorCamara camara = Object.FindFirstObjectByType<ControladorCamara>();
        if (camara != null)
        {
            camara.ConfigurarCamara(new Vector3(0f, 0.7f, 0f), 6.5f, 25f, new Vector3(0f, 1.5f, -4f));
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

            luces.posPuntual = posPuntualInicial;
            luces.puntualColor = colorPuntual;
            luces.intensidadPuntual = intensidadPuntualInicial;
            luces.radioPuntual = radioPuntualInicial;

            luces.posSpot = posSpotInicial;
            luces.rotacionSpot = rotSpotInicial;
            luces.spotColor = colorSpot;
            luces.intensidadSpot = intensidadSpotInicial;
            luces.radioSpot = radioSpotInicial;
            luces.aperturaAngulo = aperturaSpotInicial;
        }
    }

    // --- EL CEREBRO DEL MATERIAL DINÁMICO ---
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