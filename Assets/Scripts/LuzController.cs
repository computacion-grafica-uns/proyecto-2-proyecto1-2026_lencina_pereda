using UnityEngine;

[RequireComponent(typeof(ModelMatrix))]
public class LuzController : MonoBehaviour
{
    [Header("Estados de Activación (Toggles - Teclas D, P, S)")]
    public bool dirActiva = true;
    public bool puntualActiva = true;
    public bool spotActiva = true;

    // Estados dinámicos en tiempo de ejecución (se ocultan en el Inspector para evitar ruido)
    [HideInInspector] public Vector3 rotacionDireccional;
    [HideInInspector] public Color dirColor;
    [HideInInspector] public float intensidadDir;
    [HideInInspector] public float velocidadRotacionSol;

    [HideInInspector] public Vector3 posPuntual;
    [HideInInspector] public Color puntualColor;
    [HideInInspector] public float intensidadPuntual;
    [HideInInspector] public float radioPuntual;

    [HideInInspector] public Vector3 posSpot;
    [HideInInspector] public Vector3 rotacionSpot;
    [HideInInspector] public Color spotColor;
    [HideInInspector] public float intensidadSpot;
    [HideInInspector] public float radioSpot;
    [HideInInspector] public float aperturaAngulo;

    private ModelMatrix modelMatrix;
    private SceneManager sceneManager;

    // Método de Inyección de Dependencias: el SceneManager puebla toda la configuración
    public void GeneralizarLuces(SceneManager manager)
    {
        // 1. Luz Direccional
        rotacionDireccional = manager.rotSolInicial;
        dirColor = manager.colorSol;
        intensidadDir = manager.intensidadSolInicial;
        velocidadRotacionSol = manager.velocidadSol;

        // 2. Luz Puntual
        posPuntual = manager.posPuntualInicial;
        puntualColor = manager.colorPuntual;
        intensidadPuntual = manager.intensidadPuntualInicial;
        radioPuntual = manager.radioPuntualInicial;

        // 3. Luz Spot
        posSpot = manager.posSpotInicial;
        rotacionSpot = manager.rotSpotInicial;
        spotColor = manager.colorSpot;
        intensidadSpot = manager.intensidadSpotInicial;
        radioSpot = manager.radioSpotInicial;
        aperturaAngulo = manager.aperturaSpotInicial;
    }

    void Start()
    {
        modelMatrix = GetComponent<ModelMatrix>();
        sceneManager = Object.FindFirstObjectByType<SceneManager>();
    }

    void Update()
    {
        // Controles por conmutación (Toggles)
        if (Input.GetKeyDown(KeyCode.D)) dirActiva = !dirActiva;
        if (Input.GetKeyDown(KeyCode.P)) puntualActiva = !puntualActiva;
        if (Input.GetKeyDown(KeyCode.S)) spotActiva = !spotActiva;

        // Rotación vertical del sol
        if (Input.GetKey(KeyCode.Period)) rotacionDireccional.x += velocidadRotacionSol * Time.deltaTime;
        if (Input.GetKey(KeyCode.Comma)) rotacionDireccional.x -= velocidadRotacionSol * Time.deltaTime;

        // Modificadores de intensidad en tiempo de ejecución
        if (Input.GetKey(KeyCode.I)) intensidadDir += Time.deltaTime * 2f;
        if (Input.GetKey(KeyCode.K)) intensidadDir = Mathf.Max(0f, intensidadDir - Time.deltaTime * 2f);
        if (Input.GetKey(KeyCode.O)) intensidadPuntual += Time.deltaTime * 2f;
        if (Input.GetKey(KeyCode.L)) intensidadPuntual = Mathf.Max(0f, intensidadPuntual - Time.deltaTime * 2f);
        if (Input.GetKey(KeyCode.U)) intensidadSpot += Time.deltaTime * 2f;
        if (Input.GetKey(KeyCode.J)) intensidadSpot = Mathf.Max(0f, intensidadSpot - Time.deltaTime * 2f);

        // Modificadores de radio y apertura
        if (Input.GetKey(KeyCode.Y)) radioPuntual += Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.H)) radioPuntual = Mathf.Max(0.1f, radioPuntual - Time.deltaTime * 5f);
        if (Input.GetKey(KeyCode.M)) radioSpot += Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.N)) radioSpot = Mathf.Max(0.1f, radioSpot - Time.deltaTime * 5f);
        if (Input.GetKey(KeyCode.G)) aperturaAngulo = Mathf.Min(90f, aperturaAngulo + Time.deltaTime * 20f);
        if (Input.GetKey(KeyCode.F)) aperturaAngulo = Mathf.Max(0f, aperturaAngulo - Time.deltaTime * 20f);

        SincronizarShader();
    }

    void SincronizarShader()
    {
        if (sceneManager == null || modelMatrix == null) return;

        Matrix4x4 matDir = modelMatrix.CreateModelMatrix(Vector3.zero, rotacionDireccional * Mathf.Deg2Rad, Vector3.one);
        Vector4 dirMundo = matDir.GetColumn(2);

        Matrix4x4 matSpot = modelMatrix.CreateModelMatrix(Vector3.zero, rotacionSpot * Mathf.Deg2Rad, Vector3.one);
        Vector4 dirSpotMundo = matSpot.GetColumn(2);

        Vector4 posPuntualMundo = new Vector4(posPuntual.x, posPuntual.y, posPuntual.z, 1.0f);
        Vector4 posSpotMundo = new Vector4(posSpot.x, posSpot.y, posSpot.z, 1.0f);

        foreach (GameObject obj in sceneManager.objetosEscena)
        {
            if (obj == null) continue;
            Renderer rend = obj.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.SetFloat("_DirLightActive", dirActiva ? 1f : 0f);
                rend.material.SetFloat("_PointLightActive", puntualActiva ? 1f : 0f);
                rend.material.SetFloat("_SpotLightActive", spotActiva ? 1f : 0f);

                rend.material.SetFloat("_DirIntensity", intensidadDir);
                rend.material.SetFloat("_PointIntensity", intensidadPuntual);
                rend.material.SetFloat("_SpotIntensity", intensidadSpot);

                rend.material.SetVector("_LightDirWorld", dirMundo);
                rend.material.SetVector("_LightPosWorld", posPuntualMundo);
                rend.material.SetVector("_SpotPosWorld", posSpotMundo);
                rend.material.SetVector("_SpotDirWorld", dirSpotMundo);

                rend.material.SetColor("_DirLightColor", dirColor);
                rend.material.SetColor("_PointLightColor", puntualColor);
                rend.material.SetColor("_SpotLightColor", spotColor);
                rend.material.SetFloat("_PointLightRadius", radioPuntual);
                rend.material.SetFloat("_SpotLightRadius", radioSpot);
                rend.material.SetFloat("_Apertura", aperturaAngulo);
            }
        }
    }
}