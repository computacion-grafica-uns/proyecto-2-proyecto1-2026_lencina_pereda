using UnityEngine;

public class CamaraController : MonoBehaviour
{
    public enum ModoCamara { OrbitalAuto, PrimeraPersona, OrbitalPasos }
    public ModoCamara modoActual = ModoCamara.OrbitalPasos;

    private ViewMatrix viewMath;
    private SceneManagerBase sceneManager;   // ← ahora apunta a la base

    [Header("Parámetros de Vista")]
    public Vector3 eye;
    public Vector3 target = Vector3.zero;

    private float yaw = 0f;
    private float pitch = 15f;
    private float distancia = 20f;

    private Vector3 posInicialFPP;
    private float yawInicial, pitchInicial, distInicial;

    public void ConfigurarCamara(Vector3 centro, float dist, float inclinacion, Vector3 fppInicio)
    {
        this.target        = centro;
        this.distancia     = dist;
        this.pitch         = inclinacion;
        this.posInicialFPP = fppInicio;

        this.yawInicial    = 0f;
        this.pitchInicial  = inclinacion;
        this.distInicial   = dist;

        if (modoActual == ModoCamara.PrimeraPersona) eye = posInicialFPP;
        else CalcularPosicionOrbital();

        ActualizarTodo();
    }

    void Awake()
    {
        viewMath     = GetComponent<ViewMatrix>();
        sceneManager = Object.FindFirstObjectByType<SceneManagerBase>(); // ← busca cualquier hijo

        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = gameObject.AddComponent<Camera>();
        cam.backgroundColor = Color.black;
        cam.clearFlags      = CameraClearFlags.SolidColor;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if      (modoActual == ModoCamara.OrbitalAuto)    modoActual = ModoCamara.PrimeraPersona;
            else if (modoActual == ModoCamara.PrimeraPersona) modoActual = ModoCamara.OrbitalPasos;
            else                                               modoActual = ModoCamara.OrbitalAuto;

            if (modoActual == ModoCamara.PrimeraPersona)
            {
                eye   = posInicialFPP;
                yaw   = 0f;
                pitch = 0f;
            }
            else CalcularPosicionOrbital();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (modoActual == ModoCamara.PrimeraPersona)
            {
                eye    = posInicialFPP;
                yaw    = 0f;
                pitch  = 0f;
                target = eye + new Vector3(0, 0, 1);
            }
            else
            {
                yaw      = yawInicial;
                pitch    = pitchInicial;
                distancia = distInicial;
                CalcularPosicionOrbital();
            }
        }

        if (modoActual == ModoCamara.PrimeraPersona) ControlFPP();
        else ControlOrbital();

        ActualizarTodo();
    }

    void ControlOrbital()
    {
        if (Input.GetKey(KeyCode.UpArrow))   distancia -= 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) distancia += 20f * Time.deltaTime;
        distancia = Mathf.Clamp(distancia, 4f, 45f);

        if (modoActual == ModoCamara.OrbitalAuto)
        {
            yaw += 20f * Time.deltaTime;
        }
        else
        {
            yaw   += Input.GetAxis("Mouse X") * 5f;
            pitch += Input.GetAxis("Mouse Y") * 5f;
            if (Input.GetKey(KeyCode.LeftArrow))  yaw -= 60f * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow)) yaw += 60f * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, -89f, 89f);
        CalcularPosicionOrbital();
    }

    void ControlFPP()
    {
        float speed         = 6f;
        float rotationSpeed = 60f;

        yaw   += Input.GetAxis("Mouse X") * 3f;
        pitch += Input.GetAxis("Mouse Y") * 3f;
        pitch  = Mathf.Clamp(pitch, -89f, 89f);

        if (Input.GetKey(KeyCode.T)) yaw   += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) yaw   -= rotationSpeed * Time.deltaTime;

        Vector3 dir = new Vector3(
            Mathf.Cos(pitch * Mathf.Deg2Rad) * Mathf.Sin(yaw * Mathf.Deg2Rad),
            Mathf.Sin(pitch * Mathf.Deg2Rad),
            Mathf.Cos(pitch * Mathf.Deg2Rad) * Mathf.Cos(yaw * Mathf.Deg2Rad)
        ).normalized;

        if (Input.GetKey(KeyCode.W)) eye += Vector3.up   * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.X)) eye -= Vector3.up   * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.UpArrow))   eye += dir * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) eye -= dir * speed * Time.deltaTime;

        Vector3 sideDir = Vector3.Cross(Vector3.up, dir).normalized;
        if (Input.GetKey(KeyCode.RightArrow)) eye += sideDir * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow))  eye -= sideDir * speed * Time.deltaTime;

        target = eye + dir;
    }

    void CalcularPosicionOrbital()
    {
        float rY = yaw   * Mathf.Deg2Rad;
        float rP = pitch * Mathf.Deg2Rad;
        eye.x = target.x + distancia * Mathf.Cos(rP) * Mathf.Sin(rY);
        eye.y = target.y + distancia * Mathf.Sin(rP);
        eye.z = target.z - distancia * Mathf.Cos(rP) * Mathf.Cos(rY);
    }

    void ActualizarTodo()
    {
        // 1. Guardas defensivas unificadas
        if (sceneManager == null || viewMath == null) return;
        if (sceneManager.objetosEscena == null || sceneManager.objetosEscena.Count == 0) return;

        // 2. Cálculo de Matrices (Una sola vez)
        ProjectionMatrix projMath = GetComponent<ProjectionMatrix>();
        if (projMath == null) projMath = gameObject.AddComponent<ProjectionMatrix>();

        float aspectoReal = (float)Screen.width / Screen.height;
        Matrix4x4 pMatRaw = projMath.CalculatePerspectiveProjectionMatrix(90f, aspectoReal, 0.1f, 1000f);
        Matrix4x4 pMatGPU = GL.GetGPUProjectionMatrix(pMatRaw, true);
        Matrix4x4 vMat    = viewMath.CreateViewMatrix(eye, target, Vector3.up);

        // =========================================================
        // EL FIX: SINCRONIZACIÓN FÍSICA DE LA CÁMARA NATIVA
        // Obligamos al Transform de Unity a seguir a nuestra matemática
        // =========================================================
        transform.position = eye;
        transform.LookAt(target);
		
        // 3. Inyección de datos a la GPU
        foreach (GameObject obj in sceneManager.objetosEscena)
        {
            if (obj == null) continue;
            
            // Recuperamos el barrido de jerarquías para que no ignore la casa
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;

                // Recuperamos sharedMaterials para evitar la fuga masiva de memoria
                Material[] mats = r.sharedMaterials;
                
                foreach (Material m in mats)
                {
                    if (m == null) continue;
                    
                    m.SetMatrix("_ProjectionMatrix", pMatGPU);
                    m.SetMatrix("_ViewMatrix",       vMat);
                    m.SetVector("_CameraPos", new Vector4(eye.x, eye.y, eye.z, 1f));

                    // Inyectamos la matriz local del sub-mesh específico
                    m.SetMatrix("_ModelMatrix", r.transform.localToWorldMatrix);
                }
            }
        }
    }
}
