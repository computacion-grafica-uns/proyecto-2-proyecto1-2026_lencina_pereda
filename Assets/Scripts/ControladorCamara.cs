using UnityEngine;

public class ControladorCamara : MonoBehaviour
{
    public enum ModoCamara { OrbitalAuto, PrimeraPersona, OrbitalPasos }
    public ModoCamara modoActual = ModoCamara.OrbitalPasos;
    private ModoCamara ultimoModoOrbital = ModoCamara.OrbitalPasos;

    private ViewMatrix viewMath;
    private SceneManagerBase sceneManager;

    [Header("Parámetros de Vista")]
    public Vector3 eye;
    public Vector3 target = Vector3.zero;

    private float yaw = 0f;
    private float pitch = 15f;
    private float distancia = 20f;

    private Vector3 posInicialFPP;
    private float yawInicial, pitchInicial, distInicial;
    private Vector3 centroGlobal;

    public bool focoGlobal = true;
    public int indiceObjetoActual = 0;

    public void ConfigurarCamara(Vector3 centro, float dist, float inclinacion, Vector3 fppInicio)
    {
        this.centroGlobal = centro;
        this.target = centro;
        this.distancia = dist;
        this.pitch = inclinacion;
        this.posInicialFPP = fppInicio;

        this.yawInicial = 0f;
        this.pitchInicial = inclinacion;
        this.distInicial = dist;

        if (modoActual == ModoCamara.PrimeraPersona) eye = posInicialFPP;
        else CalcularPosicionOrbital();

        // Arranca en modo crudo (Sin AA) para que el usuario elija cuándo activar los filtros
        AplicarAntiAliasing(0, FilterMode.Point, 0);

        ActualizarTodo();
    }

    void Awake()
    {
        viewMath = GetComponent<ViewMatrix>();
        sceneManager = Object.FindFirstObjectByType<SceneManagerBase>();

        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = gameObject.AddComponent<Camera>();
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void Update()
    {
        ManejarTecladoEstado();

        // --- INTERRUPTORES DE DEPURACIÓN EN VIVO (PROGRESIÓN DIDÁCTICA) ---
        
        // Tecla 1: Solo Anti-Aliasing Geométrico (Suaviza la pava, piso sigue roto)
        if (Input.GetKeyDown(KeyCode.Alpha1)) AplicarAntiAliasing(8, FilterMode.Point, 0);
        
        // Tecla 2: AA Geométrico + Filtrado Trilineal (Suaviza el piso cerca, pero lejos sigue el Moiré)
        if (Input.GetKeyDown(KeyCode.Alpha2)) AplicarAntiAliasing(8, FilterMode.Trilinear, 0);
        
        // Tecla 3: AA Geométrico + Trilineal + Anisotrópico 16x (Perfección total hasta el horizonte)
        if (Input.GetKeyDown(KeyCode.Alpha3)) AplicarAntiAliasing(8, FilterMode.Trilinear, 16);
        
        // Tecla 0: Apagar todo (Vuelve al estado crudo inicial para comparar)
        if (Input.GetKeyDown(KeyCode.Alpha0)) AplicarAntiAliasing(0, FilterMode.Point, 0);

        if (modoActual == ModoCamara.PrimeraPersona) ControlFPP();
        else ControlOrbital();

        ActualizarTodo();
    }

    void ManejarTecladoEstado()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (modoActual == ModoCamara.PrimeraPersona) modoActual = ultimoModoOrbital;
            else modoActual = ModoCamara.PrimeraPersona;
            ResetearVista();
        }

        if (Input.GetKeyDown(KeyCode.Z) && modoActual != ModoCamara.PrimeraPersona)
        {
            modoActual = (modoActual == ModoCamara.OrbitalPasos) ? ModoCamara.OrbitalAuto : ModoCamara.OrbitalPasos;
            ultimoModoOrbital = modoActual;
        }

        if (Input.GetKeyDown(KeyCode.V) && modoActual != ModoCamara.PrimeraPersona)
        {
            focoGlobal = !focoGlobal;
            if (!focoGlobal && sceneManager.objetosEscena.Count > 0) indiceObjetoActual = 0;
            AplicarFoco();
        }

        if (!focoGlobal && sceneManager.objetosEscena.Count > 0 && modoActual != ModoCamara.PrimeraPersona)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                indiceObjetoActual = (indiceObjetoActual + 1) % sceneManager.objetosEscena.Count;
                AplicarFoco();
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                indiceObjetoActual = (indiceObjetoActual - 1 + sceneManager.objetosEscena.Count) % sceneManager.objetosEscena.Count;
                AplicarFoco();
            }
        }

        if (Input.GetKeyDown(KeyCode.R)) ResetearVista();
    }

    void AplicarFoco()
    {
        yaw = 0f;
        if (focoGlobal)
        {
            target = centroGlobal;
            distancia = distInicial;
            pitch = pitchInicial;
        }
        else
        {
            EnfocarObjetoActual();
        }
        CalcularPosicionOrbital();
    }

    void EnfocarObjetoActual()
    {
        if (sceneManager.objetosEscena.Count == 0) return;

        GameObject obj = sceneManager.objetosEscena[indiceObjetoActual];
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            target = b.center;
            distancia = b.extents.magnitude * 1.0f;
            if (distancia < 0.6f) distancia = 0.6f;
        }
        else
        {
            target = obj.transform.position;
            distancia = 3f;
        }
        pitch = 25f; 
    }

    void ResetearVista()
    {
        if (modoActual == ModoCamara.PrimeraPersona)
        {
            eye = posInicialFPP;
            yaw = 0f;
            pitch = 0f;
            target = eye + new Vector3(0, 0, 1);
        }
        else
        {
            AplicarFoco();
        }
    }

    void ControlOrbital()
    {
        if (Input.GetKey(KeyCode.UpArrow)) distancia -= 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) distancia += 20f * Time.deltaTime;

        distancia = Mathf.Clamp(distancia, 1f, 45f);

        if (modoActual == ModoCamara.OrbitalAuto)
        {
            yaw += 20f * Time.deltaTime;
        }
        else
        {
            yaw += Input.GetAxis("Mouse X") * 5f;
            pitch += Input.GetAxis("Mouse Y") * 5f;
            if (Input.GetKey(KeyCode.LeftArrow)) yaw -= 60f * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow)) yaw += 60f * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, -89f, 89f);
        CalcularPosicionOrbital();
    }

    void ControlFPP()
    {
        float speed = 6f;
        float rotationSpeed = 60f;

        yaw += Input.GetAxis("Mouse X") * 3f;
        pitch += Input.GetAxis("Mouse Y") * 3f;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        if (Input.GetKey(KeyCode.T)) yaw += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) yaw -= rotationSpeed * Time.deltaTime;

        Vector3 dir = new Vector3(
            Mathf.Cos(pitch * Mathf.Deg2Rad) * Mathf.Sin(yaw * Mathf.Deg2Rad),
            Mathf.Sin(pitch * Mathf.Deg2Rad),
            Mathf.Cos(pitch * Mathf.Deg2Rad) * Mathf.Cos(yaw * Mathf.Deg2Rad)
        ).normalized;

        if (Input.GetKey(KeyCode.W)) eye += Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.X)) eye -= Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.UpArrow)) eye += dir * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) eye -= dir * speed * Time.deltaTime;

        Vector3 sideDir = Vector3.Cross(Vector3.up, dir).normalized;
        if (Input.GetKey(KeyCode.RightArrow)) eye += sideDir * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow)) eye -= sideDir * speed * Time.deltaTime;

        target = eye + dir;
    }

    void CalcularPosicionOrbital()
    {
        float rY = yaw * Mathf.Deg2Rad;
        float rP = pitch * Mathf.Deg2Rad;
        eye.x = target.x + distancia * Mathf.Cos(rP) * Mathf.Sin(rY);
        eye.y = target.y + distancia * Mathf.Sin(rP);
        eye.z = target.z - distancia * Mathf.Cos(rP) * Mathf.Cos(rY);
    }

    void ActualizarTodo()
    {
        if (sceneManager == null || viewMath == null) return;
        if (sceneManager.objetosEscena == null || sceneManager.objetosEscena.Count == 0) return;

        ProjectionMatrix projMath = GetComponent<ProjectionMatrix>();
        if (projMath == null) projMath = gameObject.AddComponent<ProjectionMatrix>();

        float aspectoReal = (float)Screen.width / Screen.height;
        Matrix4x4 pMatRaw = projMath.CalculatePerspectiveProjectionMatrix(90f, aspectoReal, 0.1f, 1000f);
        Matrix4x4 pMatGPU = GL.GetGPUProjectionMatrix(pMatRaw, true);
        Matrix4x4 vMat = viewMath.CreateViewMatrix(eye, target, Vector3.up);

        transform.position = eye;
        transform.LookAt(target);

        foreach (GameObject obj in sceneManager.objetosEscena)
        {
            if (obj == null) continue;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                Material[] mats = r.sharedMaterials;

                foreach (Material m in mats)
                {
                    if (m == null) continue;
                    m.SetMatrix("_ProjectionMatrix", pMatGPU);
                    m.SetMatrix("_ViewMatrix", vMat);
                    m.SetVector("_CameraPos", new Vector4(eye.x, eye.y, eye.z, 1f));
                    m.SetMatrix("_ModelMatrix", r.transform.localToWorldMatrix);
                }
            }
        }
    }

    private void AplicarAntiAliasing(int nivelMSAA, FilterMode modoFiltro, int nivelAniso)
    {
        if (sceneManager == null) return;

        // 1. Modificación de hardware de rasterización de bordes geométricos
        QualitySettings.antiAliasing = nivelMSAA;

        // 2. Modificación de los estados de muestreo de texturas en VRAM
        foreach (GameObject obj in sceneManager.objetosEscena)
        {
            if (obj == null) continue;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m.HasProperty("_MainTex"))
                    {
                        Texture tex = m.GetTexture("_MainTex");
                        if (tex != null && tex is Texture2D)
                        {
                            Texture2D t2d = (Texture2D)tex;
                            t2d.filterMode = modoFiltro;
                            t2d.anisoLevel = nivelAniso;
                        }
                    }
                }
            }
        }
        Debug.Log($"[Módulo Teórico] MSAA: {nivelMSAA}x | Filtro: {modoFiltro} | Aniso: {nivelAniso}x");
    }
}