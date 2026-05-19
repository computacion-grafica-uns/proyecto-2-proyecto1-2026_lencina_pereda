using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class ObjParser
{
    public static Mesh Parse(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        List<Vector3> temp_v = new List<Vector3>();
        List<Vector2> temp_uv = new List<Vector2>();
        List<Vector3> final_v = new List<Vector3>();
        List<Vector2> final_uv = new List<Vector2>();
        List<int> final_t = new List<int>();

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines) {
            string l = line.Trim();
            if (string.IsNullOrEmpty(l) || l.StartsWith("#")) continue;
            string[] p = l.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (l.StartsWith("v ")) {
                temp_v.Add(new Vector3(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture)));
            } else if (l.StartsWith("vt ")) {
                temp_uv.Add(new Vector2(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture)));
            } else if (l.StartsWith("f ")) {
                // Triangulamos automáticamente: soporta caras de 3, 4 o más lados
                for (int i = 1; i < p.Length - 2; i++) {
                    int[] faceIndices = { 1, i + 1, i + 2 };
                    foreach (int idx in faceIndices) {
                        string[] sub = p[idx].Split('/');
                        int vIdx = int.Parse(sub[0]) - 1;
                        final_v.Add(temp_v[vIdx]);

                        if (sub.Length > 1 && !string.IsNullOrEmpty(sub[1])) {
                            int uvIdx = int.Parse(sub[1]) - 1;
                            final_uv.Add(temp_uv[uvIdx]);
                        } else {
                            final_uv.Add(Vector2.zero);
                        }
                        final_t.Add(final_v.Count - 1);
                    }
                }
            }
        }
		
		CentrarVertices(final_v);
        Mesh m = new Mesh();
        m.vertices = final_v.ToArray();
        m.uv = final_uv.ToArray();
        m.triangles = final_t.ToArray();
        m.RecalculateNormals();
        return m;
    }
	
	private static void CentrarVertices(List<Vector3> vertices)
	{
		if (vertices.Count == 0) return;

		// 1. Encontramos los límites (Bounding Box)
		Vector3 min = vertices[0];
		Vector3 max = vertices[0];

		foreach (Vector3 v in vertices)
		{
			if (v.x < min.x) min.x = v.x; if (v.x > max.x) max.x = v.x;
			if (v.y < min.y) min.y = v.y; if (v.y > max.y) max.y = v.y;
			if (v.z < min.z) min.z = v.z; if (v.z > max.z) max.z = v.z;
		}

		// 2. Calculamos el centro exacto
		Vector3 centro = (min + max) / 2f;

		// 3. Restamos el centro a cada vértice para que el nuevo centro sea (0,0,0)
		for (int i = 0; i < vertices.Count; i++)
		{
			vertices[i] -= centro;
		}
	}
}