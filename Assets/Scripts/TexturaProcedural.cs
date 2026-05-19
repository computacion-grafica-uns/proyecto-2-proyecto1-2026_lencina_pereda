using UnityEngine;

[CreateAssetMenu(fileName = "NuevaTexturaProcedural", menuName = "Graficos/Generador Textura Procedural")]
public class TexturaProcedural : ScriptableObject
{
    [Header("Configuración de la Imagen")]
    public int resolucion = 256;       // Tamaño en píxeles (ej: 256x256)
    public int cantidadCeldas = 8;     // Cantidad de cuadrados del tablero

    [Header("Colores del Patrón")]
    public Color colorA = Color.white;
    public Color colorB = Color.black;

    /// <summary>
    /// Genera un mapa de bits en tiempo de ejecución de forma matemática.
    /// </summary>
    public Texture2D GenerarTexturaEnMemoria()
    {
        // 1. Instanciamos el contenedor de la textura en memoria RAM
        Texture2D texturaGpu = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, true);
        
        // Configuramos para que se repita correctamente si las UV exceden el rango 0-1
        texturaGpu.wrapMode = TextureWrapMode.Repeat;
        
        // REQUISITO TÉCNICO: Usamos FilterMode.Point para que los bordes del ajedrez 
        // se vean perfectamente nítidos y no borrosos.
        texturaGpu.filterMode = FilterMode.Point; 

        // Calculamos cuántos píxeles mide cada celda del tablero
        int pixelesPorCelda = resolucion / cantidadCeldas;

        // 2. Recorremos la matriz bidimensional de píxeles
        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                // Evaluamos matemáticamente en qué celda lógica estamos
                int celdaX = x / pixelesPorCelda;
                int celdaY = y / pixelesPorCelda;

                // Si la suma de las posiciones de las celdas es par, pintamos Color A, si no Color B
                if ((celdaX + celdaY) % 2 == 0)
                {
                    texturaGpu.SetPixel(x, y, colorA);
                }
                else
                {
                    texturaGpu.SetPixel(x, y, colorB);
                }
            }
        }

        // 3. Comando crítico: Sube los píxeles calculados de la RAM a la memoria de la placa de video
        texturaGpu.Apply(); 

        return texturaGpu;
    }
}