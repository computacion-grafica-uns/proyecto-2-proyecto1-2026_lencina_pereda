using UnityEngine;

public class ProjectionMatrix : MonoBehaviour
{
    public Matrix4x4 CalculatePerspectiveProjectionMatrix(float fov, float aspect, float n, float f)
	{
		float tanHalfFOV = Mathf.Tan((fov * 0.5f) * Mathf.Deg2Rad);
		Matrix4x4 p = Matrix4x4.zero;

		p[0, 0] = 1.0f / (aspect * tanHalfFOV);
		p[1, 1] = 1.0f / tanHalfFOV;
		// Ajuste para Mano Izquierda y profundidad 0 a 1
		p[2, 2] = f / (f - n);
		p[2, 3] = -(f * n) / (f - n);
		p[3, 2] = 1.0f; // W ahora toma el valor de Z positivo

		return p;
	}
}
