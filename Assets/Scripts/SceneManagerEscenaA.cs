using UnityEngine;
using System.IO;

// Heredamos de la clase abstracta
public class SceneManagerEscenaA : SceneManagerBase 
{
    [Header("Configuración Específica Escena A - Piso")]
    public Shader shaderActividad11;
    public Texture2D texturaPiso;
    public string nombreArchivoObj = "piso.obj";

    [Header("Configuración Específica Escena A - Pava")]
    public Texture2D texturaPava;
    public string nombreArchivoDae = "base_model.dae";
    
    // --- VARIABLES DE TRANSFORMACIÓN ---
    [Tooltip("La pava ahora descansa perfectamente en Y = 0 gracias al centrado de base")]
    public Vector3 posicionPava = new Vector3(0f, 0f, 0f); 
    
    [Tooltip("Rotación limpia: sin compensación, la cámara ya está correctamente posicionada")]
    public Vector3 rotacionPava = new Vector3(0f, 0f, 0f); 
    
    [Tooltip("Escala corregida (3%)")]
    public Vector3 escalaPava = new Vector3(0.03f, 0.03f, 0.03f);

    protected override void ConstruirEscena()
    {
        if (shaderActividad11 == null)
        {
            Debug.LogError("Falta asignar el Shader de la Actividad 11 en Escena A.");
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
            if (texturaPiso != null) matPiso.SetTexture("_MainTex", texturaPiso);
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
            
            // --- FORZADO ABSOLUTO POR CÓDIGO (IGNORA EL INSPECTOR) ---
            // Al reasignar explícitamente los valores aquí adentro, destruimos 
            // la memoria de Unity y garantizamos que tome el 0.03 matemático.
            escalaPava = new Vector3(0.03f, 0.03f, 0.03f);
            posicionPava = new Vector3(0f, 0.7f, 0f);
            rotacionPava = new Vector3(90f, 0f, 0f);

            // --- APLICAMOS LAS TRANSFORMACIONES ---
            pavaObj.transform.position = posicionPava;
            pavaObj.transform.eulerAngles = rotacionPava;
            pavaObj.transform.localScale = escalaPava;

            pavaObj.AddComponent<MeshFilter>().mesh = meshPava;
            pavaObj.AddComponent<ModelMatrix>();

            Material matPava = new Material(shaderActividad11);
            if (texturaPava != null) matPava.SetTexture("_MainTex", texturaPava);
            
            matPava.SetColor("_MatColor", new Color(0.8f, 0.8f, 0.8f, 1f));
            matPava.SetColor("_SpecColor", Color.white);
            matPava.SetFloat("_Shininess", 32f); 

            pavaObj.AddComponent<MeshRenderer>().material = matPava;

            objetosEscena.Add(pavaObj);
        }
    }
}