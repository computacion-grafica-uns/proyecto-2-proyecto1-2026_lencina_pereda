using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewMatrix : MonoBehaviour
{
    public Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
	{
		Vector3 f = (target - eye).normalized; // Forward
		Vector3 r = Vector3.Cross(up, f).normalized; // Right (Cambio de orden en Cross)
		Vector3 u = Vector3.Cross(f, r).normalized; // Up

		Matrix4x4 v = Matrix4x4.identity;

		v[0, 0] = r.x;  v[0, 1] = r.y;  v[0, 2] = r.z;  v[0, 3] = -Vector3.Dot(r, eye);
		v[1, 0] = u.x;  v[1, 1] = u.y;  v[1, 2] = u.z;  v[1, 3] = -Vector3.Dot(u, eye);
		// FILA Z: Positiva para objetos al frente (Mano Izquierda)
		v[2, 0] = f.x;  v[2, 1] = f.y;  v[2, 2] = f.z;  v[2, 3] = -Vector3.Dot(f, eye);
		v[3, 0] = 0;    v[3, 1] = 0;    v[3, 2] = 0;    v[3, 3] = 1;

		return v;
	}
}