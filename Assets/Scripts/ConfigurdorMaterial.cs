using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ConfiguradorMaterial : MonoBehaviour
{
    [Header("Datos del Material Personalizado")]
    // ¡Aquí está el nombre real de tu clase conectada!
    public DatosMaterial datosMaterial;

    // La variable que pide el Shader desde el Inspector
    public Shader shaderAUsar;

    [Header("Textura (Opcional)")]
    public Texture2D texturaBase;

    void Start()
    {
        // Validación de seguridad
        if (datosMaterial == null || shaderAUsar == null)
        {
            Debug.LogWarning($"Faltan configurar datos o shader en el objeto: {gameObject.name}");
            return;
        }

        Renderer rendererComponent = GetComponent<Renderer>();

        // 1. Crea el material en memoria usando el shader arrastrado
        Material nuevoMaterial = new Material(shaderAUsar);

        // 2. Asigna la textura (si es que arrastraste alguna)
        if (texturaBase != null)
        {
            nuevoMaterial.SetTexture("_MainTex", texturaBase);
        }

        // 3. Combina el color de tinte con tu variable de opacidad
        Color colorFinal = datosMaterial.colorTinte;
        colorFinal.a = datosMaterial.opacidad;
        nuevoMaterial.SetColor("_MatColor", colorFinal);

        // 4. Configuración para Blinn-Phong o Toon
        if (nuevoMaterial.HasProperty("_Shininess"))
        {
            nuevoMaterial.SetFloat("_Shininess", datosMaterial.shininess);
            nuevoMaterial.SetColor("_SpecColor", datosMaterial.colorEspecular);
        }

        // 5. Configuración para Cook-Torrance (PBR)
        if (nuevoMaterial.HasProperty("_Roughness"))
        {
            nuevoMaterial.SetFloat("_Roughness", datosMaterial.rugosidadPBR);
        }
        if (nuevoMaterial.HasProperty("_Metallic"))
        {
            nuevoMaterial.SetFloat("_Metallic", datosMaterial.metalicidadPBR);
        }

        // 6. Le viste el material final al objeto
        rendererComponent.material = nuevoMaterial;

        // 7. Auto-registro en el pipeline de matrices y luces
        SceneManagerBase manager = Object.FindFirstObjectByType<SceneManagerBase>();
        if (manager != null && !manager.objetosEscena.Contains(gameObject))
        {
            manager.objetosEscena.Add(gameObject);
        }
    }
}