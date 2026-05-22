using UnityEngine;
using System.IO;

// Heredamos de la clase abstracta
public class SceneManagerEscenaA : SceneManagerBase
{
    [Header("Configuración Específica Escena A - Piso")]
    public Shader shaderPiso;
    public Texture2D texturaPiso; // 1. PNG Clásico
    public TexturaProcedural texturaProceduralPiso; // 2. Generador Matemático
    public Texture2D mapaDeNormalesPiso;
    public Color colorDefectoPiso = Color.white; // 3. Color Sólido
    public string nombreArchivoObj = "piso.obj";

    [Header("Configuración Específica Escena A - Pava")]
    public Shader shaderPava;
    public Texture2D texturaPava; // 1. PNG Clásico
    public TexturaProcedural texturaProceduralPava; // 2. Generador Matemático
    public Texture2D mapaDeNormalesPava;
    public Color colorDefectoPava = new Color(0.8f, 0.8f, 0.8f, 1f); // 3. Color Sólido
    public string nombreArchivoDae = "base_model.dae";

    // --- VARIABLES DE TRANSFORMACIÓN ---
    public Vector3 posicionPava = new Vector3(0f, 0f, 0f);
    public Vector3 rotacionPava = new Vector3(0f, 0f, 0f);
    public Vector3 escalaPava = new Vector3(0.03f, 0.03f, 0.03f);

    protected override void ConstruirEscena()
    {
        if (shaderPiso == null)
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

            Material matPiso = new Material(shaderPiso);

            // --- LÓGICA DE TEXTURAS (JERARQUÍA) ---
            Texture2D texturaFinalPiso = null;

            if (texturaPiso != null)
            {
                texturaFinalPiso = texturaPiso; // Gana el PNG
            }
            else if (texturaProceduralPiso != null)
            {
                // Si no hay PNG, ejecuta la matemática y crea la textura en RAM
                texturaFinalPiso = texturaProceduralPiso.GenerarTexturaEnMemoria();
            }

            // Inyectamos lo que haya ganado al Shader
            if (texturaFinalPiso != null)
            {
                matPiso.SetTexture("_MainTex", texturaFinalPiso);
                matPiso.SetColor("_MatColor", Color.white); // Blanco puro para no teñir la imagen
                if (matPiso.HasProperty("_UseTexture")) matPiso.SetFloat("_UseTexture", 1f);
            }
            else
            {
                matPiso.SetColor("_MatColor", colorDefectoPiso); // Gana el color sólido
                if (matPiso.HasProperty("_UseTexture")) matPiso.SetFloat("_UseTexture", 0f);
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

            Shader shaderAUsar = shaderPava != null ? shaderPava : shaderPiso;
            Material matPava = new Material(shaderAUsar);

            // --- LÓGICA DE TEXTURAS (JERARQUÍA) ---
            Texture2D texturaFinalPava = null;

            if (texturaPava != null)
            {
                texturaFinalPava = texturaPava; // Gana el PNG
            }
            else if (texturaProceduralPava != null)
            {
                // Si no hay PNG, ejecuta la matemática y crea la textura en RAM
                texturaFinalPava = texturaProceduralPava.GenerarTexturaEnMemoria();
            }

            // Inyectamos lo que haya ganado al Shader
            if (texturaFinalPava != null)
            {
                matPava.SetTexture("_MainTex", texturaFinalPava);
                matPava.SetColor("_MatColor", Color.white); // Blanco puro para no teñir la imagen
                if (matPava.HasProperty("_UseTexture")) matPava.SetFloat("_UseTexture", 1f);
            }
            else
            {
                matPava.SetColor("_MatColor", colorDefectoPava); // Gana el color sólido
                if (matPava.HasProperty("_UseTexture")) matPava.SetFloat("_UseTexture", 0f);
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