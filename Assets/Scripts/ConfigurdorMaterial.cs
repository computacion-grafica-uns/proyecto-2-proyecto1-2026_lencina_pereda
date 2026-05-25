using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ConfiguradorMaterial : MonoBehaviour
{
    [Header("Datos del Material Personalizado (.asset)")]
    public DatosMaterial datosMaterial;
    public Shader shaderAUsar;

    [Header("Jerarquía de Texturas")]
    public Texture2D texturaBase; 
    public TexturaProcedural texturaProcedural; 
    public Texture2D mapaDeNormales; 

    void Start() 
    {
        if (datosMaterial == null || shaderAUsar == null)
        {
            Debug.LogWarning($"ConfiguradorMaterial: Falta asignar DatosMaterial o Shader en: {gameObject.name}");
            return;
        }

        Renderer rendererComponent = GetComponent<Renderer>();
        Material nuevoMaterial = new Material(shaderAUsar);

        Color tinte = datosMaterial.colorTinte;
        Color spec = datosMaterial.colorEspecular;
        float shininess = datosMaterial.shininess;
        float rugosidad = datosMaterial.rugosidadPBR;
        float metalicidad = datosMaterial.metalicidadPBR;
        float opacidad = datosMaterial.opacidad;

        // --- LÓGICA DE TEXTURAS ---
        Texture2D texturaFinal = texturaBase != null ? texturaBase : (texturaProcedural != null ? texturaProcedural.GenerarTexturaEnMemoria() : null);

        nuevoMaterial.SetColor("_MatColor", tinte);

        if (texturaFinal != null) {
            nuevoMaterial.SetTexture("_MainTex", texturaFinal);
            if (nuevoMaterial.HasProperty("_UseTexture")) nuevoMaterial.SetFloat("_UseTexture", 1f);
        } else {
            if (nuevoMaterial.HasProperty("_UseTexture")) nuevoMaterial.SetFloat("_UseTexture", 0f);
        }

        // --- LÓGICA DE FÍSICAS ---
        if (nuevoMaterial.HasProperty("_SpecColor")) nuevoMaterial.SetColor("_SpecColor", spec);
        if (nuevoMaterial.HasProperty("_Shininess")) nuevoMaterial.SetFloat("_Shininess", shininess);
        if (nuevoMaterial.HasProperty("_Roughness")) nuevoMaterial.SetFloat("_Roughness", rugosidad);
        if (nuevoMaterial.HasProperty("_Metallic")) nuevoMaterial.SetFloat("_Metallic", metalicidad);
        if (nuevoMaterial.HasProperty("_Opacidad")) nuevoMaterial.SetFloat("_Opacidad", opacidad);

        // --- LÓGICA DE NORMALES ---
        if (mapaDeNormales != null && nuevoMaterial.HasProperty("_NormalMap")) {
            nuevoMaterial.SetTexture("_NormalMap", mapaDeNormales);
            nuevoMaterial.SetFloat("_UseNormalMap", 1f);
        } else if (nuevoMaterial.HasProperty("_UseNormalMap")) {
            nuevoMaterial.SetFloat("_UseNormalMap", 0f);
        }

        // --- TRANSPARENCIA DINÁMICA ---
        if (opacidad < 1.0f) {
            nuevoMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            nuevoMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            nuevoMaterial.SetFloat("_ZWrite", 0f);
            nuevoMaterial.renderQueue = 3000; 
        } else {
            nuevoMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            nuevoMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            nuevoMaterial.SetFloat("_ZWrite", 1f);
            nuevoMaterial.renderQueue = 2000; 
        }

        // ==========================================
        // SOLUCIÓN DEFINITIVA: RED DE SEGURIDAD
        // ==========================================
        MeshFilter filtroMesh = GetComponent<MeshFilter>();
        
        // Consultamos geometría real primero, si falla usamos la lectura de slots
        int cantidadSubMallas = (filtroMesh != null && filtroMesh.sharedMesh != null) 
                                ? filtroMesh.sharedMesh.subMeshCount 
                                : rendererComponent.sharedMaterials.Length;

        // RED DE SEGURIDAD CRÍTICA: Impedir que sea 0
        if (cantidadSubMallas <= 0) cantidadSubMallas = 1;

        // Creamos NUESTRO propio arreglo de memoria
        Material[] nuevosMateriales = new Material[cantidadSubMallas];
        
        // Llenamos todos los huecos garantizados
        for (int i = 0; i < cantidadSubMallas; i++)
        {
            nuevosMateriales[i] = nuevoMaterial;
        }

        // Sobrescribimos el renderizador obligándolo a renderizar todas las caras
        rendererComponent.materials = nuevosMateriales;
    }
}