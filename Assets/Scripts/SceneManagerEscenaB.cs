using UnityEngine;

public class SceneManagerEscenaB : SceneManagerBase
{
    [Header("Raíz de los Modelos Estáticos")]
    public Transform raizModelos;

    [Header("Configuración Inicial")]
    public Vector3 centroCasa = new Vector3(0f, 1f, 0f);
    public float distanciaOrbitalInicial = 10f; 
    public float inclinacionOrbitalInicial = 30f;
    public Vector3 posInicioFPP = new Vector3(0f, 1.5f, -5f); 

    protected override void ConstruirEscena()
    {
        if (raizModelos == null)
        {
            Debug.LogWarning("Falta asignar la Raíz de los Modelos");
            return;
        }

        // ====================================================================
        // 1. INYECTAR MATRICES, REGISTRAR OBJETOS Y RENOMBRAR DINÁMICAMENTE
        // ====================================================================
        Renderer[] todosLosRenderers = raizModelos.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in todosLosRenderers)
        {
            if (r.GetComponent<ModelMatrix>() == null)
            {
                r.gameObject.AddComponent<ModelMatrix>();
            }
            if (!objetosEscena.Contains(r.gameObject))
            {
                objetosEscena.Add(r.gameObject);
            }

            // --- RENOMBRADO BASADO EN LA SEPARACIÓN DE RESPONSABILIDADES ---
            ConfiguradorMaterial config = r.GetComponent<ConfiguradorMaterial>();

            if (config != null)
            {
                // PRIORIDAD 1: Lectura directa desde tu ConfiguradorMaterial (Inspector)
                string nMat = config.datosMaterial != null ? config.datosMaterial.name : "SinMat";
                
                string nTex = "SinTex";
                if (config.texturaBase != null) nTex = config.texturaBase.name;
                else if (config.texturaProcedural != null) nTex = config.texturaProcedural.name;

                string nNorm = config.mapaDeNormales != null ? config.mapaDeNormales.name : "SinNorm";

                string nShader = config.shaderAUsar != null ? config.shaderAUsar.name : "SinShader";
                if (nShader.Contains("/")) {
                    string[] partes = nShader.Split('/');
                    nShader = partes[partes.Length - 1];
                }

                r.gameObject.name = $"{r.gameObject.name}_{nMat}_{nTex}_{nNorm}_{nShader}";
            }
            else
            {
                // PRIORIDAD 2 (FALLBACK): Lectura del material genérico importado por defecto
                Material mat = r.sharedMaterial;
                if (mat != null)
                {
                    string nMat = mat.name.Replace(" (Instance)", "");
                    
                    string nTex = "SinTex";
                    Texture texPrincipal = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : mat.mainTexture;
                    if (texPrincipal != null) nTex = texPrincipal.name;

                    string nNorm = "SinNorm";
                    if (mat.HasProperty("_NormalMap") && mat.GetTexture("_NormalMap") != null)
                        nNorm = mat.GetTexture("_NormalMap").name;
                    else if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
                        nNorm = mat.GetTexture("_BumpMap").name;

                    string nShader = mat.shader != null ? mat.shader.name : "SinShader";
                    if (nShader.Contains("/")) {
                        string[] partes = nShader.Split('/');
                        nShader = partes[partes.Length - 1];
                    }

                    r.gameObject.name = $"{r.gameObject.name}_{nMat}_{nTex}_{nNorm}_{nShader}";
                }
            }
        }

        // ==========================================
        // 2. CONFIGURAR LA CÁMARA
        // ==========================================
        CamaraController camara = Object.FindFirstObjectByType<CamaraController>();
        if (camara != null)
        {
            camara.ConfigurarCamara(centroCasa, distanciaOrbitalInicial, inclinacionOrbitalInicial, posInicioFPP);
        }

        // ==========================================
        // 3. CONFIGURAR LAS LUCES (ID4587 e ID4227)
        // ==========================================
        LuzController luces = Object.FindFirstObjectByType<LuzController>();
        if (luces != null)
        {
            luces.rotacionDireccional = rotSolInicial;
            luces.dirColor = colorSol; 
            luces.intensidadDir = 2.5f;     
            luces.velocidadRotacionSol = velocidadSol;
            luces.puntualColor = colorPuntual;
            luces.spotColor = colorSpot;
            luces.aperturaAngulo = aperturaSpotInicial;

            Vector3 posID4587 = centroCasa;
            Vector3 posID4227 = centroCasa;

            // Escaneo forense de nodos por identificador
            Transform[] todosLosTransforms = raizModelos.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in todosLosTransforms)
            {
                if (t.name.Contains("ID4587")) posID4587 = t.position;
                if (t.name.Contains("ID4227")) posID4227 = t.position;
            }

            // --- SETEO DE PARÁMETROS ESPECÍFICOS DE LA CASA ---
            luces.posPuntual = posID4587 + new Vector3(0f, 2.0f, 0f);
            luces.radioPuntual = 3.5f;
            luces.intensidadPuntual = 4.0f;

            luces.posSpot = posID4227 + new Vector3(0f, 3f, 4.0f);
            luces.rotacionSpot = new Vector3(15f, 180f, 0f);
            luces.radioSpot = 18f;
            luces.intensidadSpot = 5.0f;
        }
    }
}