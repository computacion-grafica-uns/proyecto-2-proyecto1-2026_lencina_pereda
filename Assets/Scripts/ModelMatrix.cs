using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelMatrix : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public Matrix4x4 CreateModelMatrix(Vector3 pos, Vector3 rot, Vector3 scale)
	{
		// Matriz de Traslación (Estándar Unity: Column-Major)
		Matrix4x4 T = new Matrix4x4(
			new Vector4(1, 0, 0, 0),
			new Vector4(0, 1, 0, 0),
			new Vector4(0, 0, 1, 0),
			new Vector4(pos.x, pos.y, pos.z, 1) // La posición va en la 4ta COLUMNA
		);

		// Rotaciones (Usando el formato de columna nativo)
		float sx = Mathf.Sin(rot.x); float cx = Mathf.Cos(rot.x);
		float sy = Mathf.Sin(rot.y); float cy = Mathf.Cos(rot.y);
		float sz = Mathf.Sin(rot.z); float cz = Mathf.Cos(rot.z);

		Matrix4x4 RX = new Matrix4x4(
			new Vector4(1, 0, 0, 0),
			new Vector4(0, cx, sx, 0),
			new Vector4(0, -sx, cx, 0),
			new Vector4(0, 0, 0, 1)
		);

		Matrix4x4 RY = new Matrix4x4(
			new Vector4(cy, 0, -sy, 0),
			new Vector4(0, 1, 0, 0),
			new Vector4(sy, 0, cy, 0),
			new Vector4(0, 0, 0, 1)
		);

		Matrix4x4 RZ = new Matrix4x4(
			new Vector4(cz, sz, 0, 0),
			new Vector4(-sz, cz, 0, 0),
			new Vector4(0, 0, 1, 0),
			new Vector4(0, 0, 0, 1)
		);

		Matrix4x4 S = new Matrix4x4(
			new Vector4(scale.x, 0, 0, 0),
			new Vector4(0, scale.y, 0, 0),
			new Vector4(0, 0, scale.z, 0),
			new Vector4(0, 0, 0, 1)
		);

		// Orden de multiplicación correcto: T * R * S
		return T * (RZ * RY * RX) * S;
	}
   
}
