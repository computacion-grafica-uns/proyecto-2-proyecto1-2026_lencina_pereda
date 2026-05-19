using UnityEngine;

// Esto permite que el material aparezca en el menú "Click Derecho -> Create"
[CreateAssetMenu(fileName = "NuevoDatosMaterial", menuName = "Graficos/Datos de Material")]
public class DatosMaterial : ScriptableObject
{
    [Header("Propiedades Lumínicas (Blinn-Phong)")]
    public Color colorTinte = Color.white;
    public Color colorEspecular = Color.white;
    public float shininess = 32f;
    [Range(0f, 1f)] public float opacidad = 1f;

    [Header("Propiedades Físicas (Para Cook-Torrance más adelante)")]
    [Range(0f, 1f)] public float rugosidadPBR = 0.5f;
    [Range(0f, 1f)] public float metalicidadPBR = 0.0f;
}