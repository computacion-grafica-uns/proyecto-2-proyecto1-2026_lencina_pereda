using UnityEngine;
using System.Collections.Generic;

public abstract class SceneManagerBase : MonoBehaviour
{
    [Header("Estructura Core (Heredada)")]
    public List<GameObject> objetosEscena = new List<GameObject>();

    [Header("Configuración Global de Luces (Heredada)")]
    public Vector3 rotSolInicial = new Vector3(45f, 45f, 0f);
    public Color colorSol = Color.white;
    public float intensidadSolInicial = 0.8f;
    public float velocidadSol = 50f;

    public Vector3 posPuntualInicial = new Vector3(0f, 4.0f, 0f);
    public Color colorPuntual = Color.yellow;
    public float intensidadPuntualInicial = 1.2f;
    public float radioPuntualInicial = 8f;

    public Vector3 posSpotInicial = new Vector3(-2.0f, 4.5f, 0f);
    public Vector3 rotSpotInicial = new Vector3(90f, 0f, 0f);
    public Color colorSpot = Color.cyan;
    public float intensidadSpotInicial = 1.5f;
    public float radioSpotInicial = 10f;
    [Range(0f, 90f)] public float aperturaSpotInicial = 30f;

    protected virtual void Start()
    {
        // 1. Llama al método de construcción específico de la escena hija (Escena A, B, etc.)
        ConstruirEscena();

        // 2. Sincroniza automáticamente las luces una vez que la escena está armada
        LuzController luzCtrl = Object.FindFirstObjectByType<LuzController>();
        if (luzCtrl != null)
        {
            luzCtrl.GeneralizarLuces(this);
        }
    }

    // MÉTODO ABSTRACTO: Obliga a cada escena hija a programar cómo se construye a sí misma
    protected abstract void ConstruirEscena();
}