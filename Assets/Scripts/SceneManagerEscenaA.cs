using UnityEngine;
using System.IO;

public class SceneManagerEscenaA : SceneManagerBase
{
    [Header("Configuración Específica Escena A - Piso")]
    public Shader shaderActividad11;
    public Texture2D texturaPiso;
    public Texture2D mapaDeNormalesPiso;
    public Color colorDefectoPiso = Color.white;
    public string nombreArchivoObj = "piso.obj";

    [Header("Configuración Específica Escena A - Pava")]
    public Shader shaderPava;
    public Texture2D texturaPava;
    public Texture2D mapaDeNormalesPava;
    public Color colorDefectoPava = new Color(0.8f, 0.8f, 0.8f, 1f);
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

            // --- GESTIÓN EXCLUSIVA DE COLOR VS TEXTURA ---
            if (texturaPiso != null)
            {
                matPiso.SetTexture("_MainTex", texturaPiso);
                matPiso.SetColor("_MatColor", Color.white); // Forzado a blanco para usar la textura pura
            }
            else
            {
                matPiso.SetColor("_MatColor", colorDefectoPiso); // Si no hay archivo, aplica el color plano del Inspector
            }

            // Normales
            if (mapaDeNormalesPiso != null && matPiso.HasProperty("_NormalMap"))
            {
                matPiso.SetTexture("_NormalMap", mapaDeNormalesPiso);
                matPiso.SetFloat("_UseNormalMap", 1f);
            }
            else if (matPiso.HasProperty("_UseNormalMap"))
            {
                matPiso.SetFloat("_UseNormalMap", 0f);
            }

            matPiso.SetColor("_SpecColor", new Color(0.2f, 0.2f, 0.2f, 1f));
            if (matPiso.HasProperty("_Shininess")) matPiso.SetFloat("_Shininess", 16f);

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
            rotacionPava = new Vector3(90f, 0f, 0f);

            pavaObj.transform.position = posicionPava;
            pavaObj.transform.eulerAngles = rotacionPava;
            pavaObj.transform.localScale = escalaPava;

            pavaObj.AddComponent<MeshFilter>().mesh = meshPava;
            pavaObj.AddComponent<ModelMatrix>();

            Shader shaderAUsar = shaderPava != null ? shaderPava : shaderActividad11;
            Material matPava = new Material(shaderAUsar);

            // --- GESTIÓN EXCLUSIVA DE COLOR VS TEXTURA ---
            if (texturaPava != null)
            {
                matPava.SetTexture("_MainTex", texturaPava);
                matPava.SetColor("_MatColor", Color.white); // Forzado a blanco para usar la textura pura
            }
            else
            {
                matPava.SetColor("_MatColor", colorDefectoPava); // Si no hay archivo, aplica el color plano del Inspector
            }

            // Normales
            if (mapaDeNormalesPava != null && matPava.HasProperty("_NormalMap"))
            {
                matPava.SetTexture("_NormalMap", mapaDeNormalesPava);
                matPava.SetFloat("_UseNormalMap", 1f);
            }
            else if (matPava.HasProperty("_UseNormalMap"))
            {
                matPava.SetFloat("_UseNormalMap", 0f);
            }

            matPava.SetColor("_SpecColor", Color.white);
            if (matPava.HasProperty("_Shininess")) matPava.SetFloat("_Shininess", 32f);

            pavaObj.AddComponent<MeshRenderer>().material = matPava;
            objetosEscena.Add(pavaObj);
        }
    }
}