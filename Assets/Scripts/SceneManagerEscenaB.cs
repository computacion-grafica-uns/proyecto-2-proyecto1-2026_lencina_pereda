using UnityEngine;

public class SceneManagerEscenaB : SceneManagerBase
{
    [Header("Raíz de los Modelos Estáticos")]
    public Transform raizModelos;

    [Header("Configuración Inicial")]
    public Vector3 centroCasa = new Vector3(0f, 1f, 0f);
    public float distanciaOrbitalInicial = 10f; 
    public float inclinacionOrbitalInicial = 30f;
    public Vector3 posInicioFPP = new Vector3(0f, 1.5f, -5f); // Donde arranca la primera persona

    protected override void ConstruirEscena()
    {
        if (raizModelos == null)
        {
            Debug.LogWarning("Falta asignar la Raíz de los Modelos");
            return;
        }

        // 1. INYECTAR MATRICES Y REGISTRAR OBJETOS DE LA CASA
        Renderer[] todosLosRenderers = raizModelos.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in todosLosRenderers)
        {
            if (r.GetComponent<ModelMatrix>() == null)
            {
                r.gameObject.AddComponent<ModelMatrix>();
            }
            if (!objetosEscena.Contains(r.gameObject))
            {
                objetosEscena.Add(r.gameObject);
            }
        }

        // 2. CONFIGURAR LA CÁMARA
        // Buscamos el objeto "Camaras" que tiene tu CamaraController
        CamaraController camara = Object.FindFirstObjectByType<CamaraController>();
        if (camara != null)
        {
            // Le pasamos las variables expuestas en el Inspector en lugar de números fijos
            camara.ConfigurarCamara(centroCasa, distanciaOrbitalInicial, inclinacionOrbitalInicial, posInicioFPP);
        }

        /// ==========================================
        // 3. CONFIGURAR LAS LUCES (ID4587 e ID4227)
        // ==========================================
        LuzController luces = Object.FindFirstObjectByType<LuzController>();
        if (luces != null)
        {
            // Inicializamos los estados compartidos heredados de la base
            luces.rotacionDireccional = rotSolInicial;
            luces.dirColor = colorSol; // Corregido a dirColor
            luces.intensidadDir = 2.5f;     // Sol potenciado para la casa
            luces.velocidadRotacionSol = velocidadSol;
            luces.puntualColor = colorPuntual;
            luces.spotColor = colorSpot;
            luces.aperturaAngulo = aperturaSpotInicial;

            Vector3 posID4587 = centroCasa;
            Vector3 posID4227 = centroCasa;

            // Escaneo forense de nodos por identificador
            Transform[] todosLosTransforms = raizModelos.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in todosLosTransforms)
            {
                if (t.name.Contains("ID4587")) posID4587 = t.position;
                if (t.name.Contains("ID4227")) posID4227 = t.position;
            }

            // --- SETEO DE PARÁMETROS ESPECÍFICOS DE LA CASA ---
            luces.posPuntual = posID4587 + new Vector3(0f, 2.0f, 0f);
            luces.radioPuntual = 3.5f;
            luces.intensidadPuntual = 4.0f;

            luces.posSpot = posID4227 + new Vector3(0f, 3f, 4.0f);
            luces.rotacionSpot = new Vector3(15f, 180f, 0f);
            luces.radioSpot = 18f;
            luces.intensidadSpot = 5.0f;
        }
    }
}