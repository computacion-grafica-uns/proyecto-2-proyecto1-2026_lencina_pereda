using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SceneManager : MonoBehaviour
{
    [Header("Lógicas de Sombreado")]
    public Shader shaderBlinnPhong;
    public Shader shaderCookTorrance;
    public Shader shaderSuperToon;

    [Header("Materiales de Cátedra")]
    public DatosMaterial datosBarro;
    public DatosMaterial datosMetal;
    public DatosMaterial datosVidrio;

    [Header("Configuración de Luces")]
    public Vector3 rotSolInicial = new Vector3(50f, -30f, 0f);
    public Color colorSol = Color.white;
    public float intensidadSolInicial = 1.0f;
    public float velocidadSol = 10f;
    
    public Vector3 posPuntualInicial = new Vector3(0f, 4f, 0f);
    public Color colorPuntual = Color.red;
    public float intensidadPuntualInicial = 1.2f;
    public float radioPuntualInicial = 8f;
    
    public Vector3 posSpotInicial = new Vector3(-2f, 5f, -2f);
    public Vector3 rotSpotInicial = new Vector3(90f, 0f, 0f);
    public Color colorSpot = Color.blue;
    public float intensidadSpotInicial = 1.5f;
    public float radioSpotInicial = 10f;
    public float aperturaSpotInicial = 35f;

    [Header("Carga Geométrica")]
    public GameObject modeloPavaPrefab;

    [Header("Texturas e Inyección Procedural")]
    public Texture2D texturaPisoMadera;
    public Texture2D texturaPavaDifusa;
    public Texture2D texturaPavaNormal;
    public TexturaProcedural generadorProcedural;

    [Header("Configuración de Grilla")]
    public int filas = 3;
    public int columnas = 6;
    public float espaciadoX = 4.0f;
    public float espaciadoZ = 4.0f;
    public Vector3 escalaPava = new Vector3(0.6f, 0.6f, 0.6f);

    [Header("Estructura de Datos Global")]
    public List<GameObject> objetosEscena = new List<GameObject>();

    private Texture2D texturaProceduralCargada;

    void Start()
    {
        // Limpieza de seguridad ante reinicios
        objetosEscena.Clear();

        // 1. Inicialización de Textura Procedural si aplica
        if (generadorProcedural != null)
        {
            texturaProceduralCargada = generadorProcedural.GenerarTexturaEnMemoria();
        }

        // 2. Carga absoluta del piso .obj mediante File Stream / ObjParser
        string rutaAbsolutaPiso = "D:/Repositorios/Computación Grafica/Cursada 2026/proyecto-2-proyecto1-2026_lencina_pereda/Assets/Models/Escena A/piso.obj";
        
        if (File.Exists(rutaAbsolutaPiso))
        {
            Mesh meshPiso = ObjParser.Parse(rutaAbsolutaPiso);
            if (meshPiso != null)
            {
                ConstruirPiso(meshPiso);
            }
            else
            {
                Debug.LogError("ObjParser devolvió null al procesar el piso.");
            }
        }
        else
        {
            Debug.LogError("No se encontró el archivo .obj en la ruta especificada: " + rutaAbsolutaPiso);
        }

        // 3. Instanciación y combinatoria de las 18 pavas
        if (modeloPavaPrefab != null)
        {
            GenerarGrillaCombinatoriaPavas();
        }
        else
        {
            Debug.LogError("Falta asignar el Prefab de la pava en el Inspector.");
        }

        // 4. Sincronización del controlador de iluminación externo
        SincronizarEstructuraDeEscena();
    }

    void ConstruirPiso(Mesh meshPiso)
    {
        GameObject pisoGO = new GameObject("Piso_Escena_A");
        pisoGO.transform.SetParent(this.transform);
        pisoGO.AddComponent<MeshFilter>().mesh = meshPiso;
        
        Material matPiso = new Material(shaderBlinnPhong ? shaderBlinnPhong : Shader.Find("Diffuse"));
        
        // Prioriza la textura de madera si está asignada, sino usa la procedural
        if (texturaPisoMadera != null)
        {
            matPiso.SetTexture("_MainTex", texturaPisoMadera);
        }
        else if (texturaProceduralCargada != null)
        {
            matPiso.SetTexture("_MainTex", texturaProceduralCargada);
        }

        pisoGO.AddComponent<MeshRenderer>().material = matPiso;
        pisoGO.AddComponent<ModelMatrix>();
        objetosEscena.Add(pisoGO);
    }

    void GenerarGrillaCombinatoriaPavas()
    {
        float offsetX = ((columnas - 1) * espaciadoX) / 2f;
        float offsetZ = ((filas - 1) * espaciadoZ) / 2f;

        // Estructuras de datos locales para mapear la combinatoria matricial (3 Shaders x 3 Materiales)
        Shader[] sombreadores = { shaderBlinnPhong, shaderCookTorrance, shaderSuperToon };
        DatosMaterial[] materialesCatedra = { datosBarro, datosMetal, datosVidrio };

        for (int f = 0; f < filas; f++)
        {
            // Cada fila lógica adopta un material de cátedra único (F0: Barro, F1: Metal, F2: Vidrio)
            DatosMaterial materialActual = materialesCatedra[f % materialesCatedra.Length];

            for (int c = 0; c < columnas; c++)
            {
                // Instanciación limpia respetando transformaciones locales
                GameObject pavaGO = Instantiate(modeloPavaPrefab, this.transform);
                pavaGO.name = $"Pava_F{f}_C{c}";
                pavaGO.transform.position = new Vector3((c * espaciadoX) - offsetX, 0, (f * espaciadoZ) - offsetZ);
                pavaGO.transform.localScale = escalaPava;

                // Las columnas alternan cíclicamente entre los 3 shaders disponibles
                Shader shaderActual = sombreadores[c % sombreadores.Length];

                foreach (Renderer r in pavaGO.GetComponentsInChildren<Renderer>())
                {
                    if (shaderActual == null) continue;

                    Material matPava = new Material(shaderActual);

                    // Mapeo de Texturas
                    if (texturaPavaDifusa != null) matPava.SetTexture("_MainTex", texturaPavaDifusa);
                    if (texturaPavaNormal != null) matPava.SetTexture("_NormalMap", texturaPavaNormal);

                    // Inyección segura de DatosMaterial (Evita indeterminaciones matemáticas NaN)
                    if (materialActual != null)
                    {
                        Color tinteConOpacidad = materialActual.colorTinte;
                        tinteConOpacidad.a = materialActual.opacidad;

                        matPava.SetColor("_MatColor", tinteConOpacidad);
                        matPava.SetColor("_SpecColor", materialActual.colorEspecular);
                        
                        // Protección frente a exponentes nulos o negativos en pow()
                        matPava.SetFloat("_Shininess", Mathf.Max(0.001f, materialActual.shininess));
                        matPava.SetFloat("_UseNormalMap", (texturaPavaNormal != null) ? 1.0f : 0.0f);
                        
                        // Parámetros físicos opcionales para Cook-Torrance
                        matPava.SetFloat("_Rugosidad", materialActual.rugosidadPBR);
                        matPava.SetFloat("_Metalicidad", materialActual.metalicidadPBR);
                    }

                    r.material = matPava;

                    // Vinculación de la lógica de transformación por software custom
                    if (r.gameObject.GetComponent<ModelMatrix>() == null)
                    {
                        r.gameObject.AddComponent<ModelMatrix>();
                    }

                    objetosEscena.Add(r.gameObject);
                }
            }
        }
    }

    void SincronizarEstructuraDeEscena()
    {
        LuzController lc = Object.FindFirstObjectByType<LuzController>();
        if (lc != null)
        {
            lc.GeneralizarLuces(this);
        }
    }
}

