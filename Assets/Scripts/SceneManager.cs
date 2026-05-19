using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SceneManager : MonoBehaviour
{
    [Header("Configuración Actividad 11 - Iluminación")]
    public Shader shaderActividad11;
    public Texture2D texturaPiso;
    public string nombreArchivoObj = "piso.obj";

    [Header("Configuración Global de Luces (Data-Driven)")]
    [Header("1. Luz Direccional (Sol)")]
    public Vector3 rotSolInicial = new Vector3(45f, 45f, 0f);
    public Color colorSol = Color.white;
    public float intensidadSolInicial = 0.8f;
    public float velocidadSol = 50f;

    [Header("2. Luz Puntual (Lámpara)")]
    public Vector3 posPuntualInicial = new Vector3(0f, 4.0f, 0f);
    public Color colorPuntual = Color.yellow;
    public float intensidadPuntualInicial = 1.2f;
    public float radioPuntualInicial = 8f;

    [Header("3. Luz Spot (Reflector Cenital Desplazado)")]
    public Vector3 posSpotInicial = new Vector3(-2.0f, 4.5f, 0f);
    public Vector3 rotSpotInicial = new Vector3(90f, 0f, 0f);
    public Color colorSpot = Color.cyan;
    public float intensidadSpotInicial = 1.5f;
    public float radioSpotInicial = 10f;
    [Range(0f, 90f)] public float aperturaSpotInicial = 30f;

    [Header("Control de Objetos")]
    public List<GameObject> objetosEscena = new List<GameObject>();

    void Start()
    {
        // =======================================================
        // SOLUCIÓN: FORZADO EXPLÍCITO POR CÓDIGO (Ignora el Inspector)
        // =======================================================
        posPuntualInicial = new Vector3(0f, 4.0f, 0f); // Asegura el centro
        posSpotInicial = new Vector3(-2.0f, 4.5f, 0f);    // Fuerza el desplazamiento a la izquierda
        rotSpotInicial = new Vector3(90f, 0f, 0f);     // Fuerza que mire recto hacia abajo

        if (shaderActividad11 == null)
        {
            Debug.LogError("Falta asignar el Shader de la Actividad 11 en el Inspector.");
            return;
        }

        // Carga y parseo del OBJ usando tu parser manual
        string rutaCompleta = Path.Combine(Application.dataPath, "Models", nombreArchivoObj);
        Mesh meshCargado = ObjParser.Parse(rutaCompleta);

        if (meshCargado == null)
        {
            Debug.LogError("Error: No se pudo cargar o parsear el archivo en: " + rutaCompleta);
            return;
        }

        // Construcción del GameObject geométrico
        GameObject pisoObj = new GameObject("Piso_Madera_Iluminado");
        MeshFilter mf = pisoObj.AddComponent<MeshFilter>();
        MeshRenderer mr = pisoObj.AddComponent<MeshRenderer>();
        mf.mesh = meshCargado;

        pisoObj.AddComponent<ModelMatrix>();
        objetosEscena.Add(pisoObj);

        // Creamos el material inyectando el shader corregido
        Material matIluminado = new Material(shaderActividad11);
        if (texturaPiso != null) matIluminado.SetTexture("_MainTex", texturaPiso);
        mr.material = matIluminado;

        // Reset de transformaciones físicas iniciales
        pisoObj.transform.position = Vector3.zero;
        pisoObj.transform.eulerAngles = Vector3.zero;
        pisoObj.transform.localScale = Vector3.one;

        // =======================================================
        // INYECCIÓN DE ARQUITECTURA HACIA EL LUZCONTROLLER
        // =======================================================
        LuzController luzCtrl = Object.FindFirstObjectByType<LuzController>();
        if (luzCtrl != null)
        {
            luzCtrl.GeneralizarLuces(this); // Setea el estado inicial forzado
        }
    }
}