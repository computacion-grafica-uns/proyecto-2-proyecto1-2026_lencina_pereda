using UnityEngine;

public class SceneManagerEscenaB : SceneManagerBase
{
    [Header("Raíz de los Modelos Estáticos")]
    public Transform raizModelos;

    [Header("Configuración Inicial")]
    public Vector3 centroCasa = new Vector3(0f, 1f, 0f);
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
            // Le pasamos (Target, Distancia Orbital, Inclinación, Posición FPP)
            camara.ConfigurarCamara(centroCasa, 25f, 30f, posInicioFPP);
        }

        // 3. CONFIGURAR LAS LUCES (Opcional, si tenés un método similar)
        // LuzController luces = Object.FindFirstObjectByType<LuzController>();
        // if (luces != null) { luces.ConfigurarLuces(...); }
    }
}