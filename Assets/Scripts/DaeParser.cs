using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.IO;

/// <summary>
/// Parser manual para archivos COLLADA (.dae) - Proyecto 2 Computación Gráfica 2026.
/// Compatible con el formato exportado por el modelo base_model.dae provisto por la cátedra.
/// Combina todos los submeshes en un único Mesh de Unity.
/// </summary>
public static class DaeParser
{
    public static Mesh Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("DaeParser: No se encontró el archivo en: " + filePath);
            return null;
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);

        // Namespace del COLLADA 1.4
        XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");

        // Listas finales que combinarán todos los submeshes
        List<Vector3> finalVerts    = new List<Vector3>();
        List<Vector3> finalNormals  = new List<Vector3>();
        List<Vector2> finalUVs      = new List<Vector2>();
        List<Vector4> finalTangents = new List<Vector4>();
        List<int>     finalTris     = new List<int>();

        // Iterar sobre cada <geometry> en library_geometries
        XmlNodeList geometries = doc.SelectNodes("//c:library_geometries/c:geometry", ns);

        if (geometries == null || geometries.Count == 0)
        {
            // Fallback: intentar sin namespace (algunos exportadores lo omiten)
            ns = new XmlNamespaceManager(doc.NameTable);
            geometries = doc.SelectNodes("//library_geometries/geometry");
            if (geometries == null || geometries.Count == 0)
            {
                Debug.LogError("DaeParser: No se encontraron geometrías en el archivo.");
                return null;
            }
        }

        bool useNs = geometries[0].NamespaceURI.Length > 0;

        foreach (XmlNode geo in geometries)
        {
            ParseGeometry(geo, ns, useNs, finalVerts, finalNormals, finalUVs, finalTangents, finalTris);
        }

        if (finalVerts.Count == 0)
        {
            Debug.LogError("DaeParser: No se extrajeron vértices.");
            return null;
        }

        // Centrar el mesh resultante (igual que el ObjParser)
        CentrarVertices(finalVerts);

        Mesh mesh = new Mesh();
        // Unity por defecto limita a 65535 vértices; usamos 32-bit para modelos grandes
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices  = finalVerts.ToArray();
        mesh.normals   = finalNormals.Count == finalVerts.Count ? finalNormals.ToArray() : null;
        mesh.uv        = finalUVs.Count    == finalVerts.Count ? finalUVs.ToArray()     : null;
        mesh.tangents  = finalTangents.Count == finalVerts.Count ? finalTangents.ToArray() : null;
        mesh.triangles = finalTris.ToArray();

        if (finalNormals.Count != finalVerts.Count)
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Parseo de un nodo <geometry>
    // ─────────────────────────────────────────────────────────────────────────
    private static void ParseGeometry(
        XmlNode geo, XmlNamespaceManager ns, bool useNs,
        List<Vector3> outVerts, List<Vector3> outNormals,
        List<Vector2> outUVs,   List<Vector4> outTangents,
        List<int> outTris)
    {
        // Función helper para SelectSingleNode con o sin namespace
        System.Func<XmlNode, string, XmlNode> sel = (node, xpath) =>
            useNs ? node.SelectSingleNode(xpath.Replace("/", "/c:").TrimStart('/').Insert(0, "c:"), ns)
                  : node.SelectSingleNode(xpath);

        XmlNode mesh = sel(geo, "mesh");
        if (mesh == null) return;

        string geoId = geo.Attributes["id"]?.Value ?? "unknown";

        // 1. Leer todas las <source> del mesh (arrays de floats)
        Dictionary<string, float[]> sources = new Dictionary<string, float[]>();
        XmlNodeList sourceNodes = useNs
            ? mesh.SelectNodes("c:source", ns)
            : mesh.SelectNodes("source");

        foreach (XmlNode src in sourceNodes)
        {
            string srcId = src.Attributes["id"]?.Value;
            if (srcId == null) continue;

            XmlNode arr = useNs
                ? src.SelectSingleNode("c:float_array", ns)
                : src.SelectSingleNode("float_array");

            if (arr == null) continue;

            string[] tokens = arr.InnerText.Split(new char[]{' ','\t','\r','\n'},
                System.StringSplitOptions.RemoveEmptyEntries);

            float[] floats = new float[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                floats[i] = float.Parse(tokens[i], CultureInfo.InvariantCulture);

            sources["#" + srcId] = floats;
        }

        // 2. Resolver el <vertices> (indirección POSITION)
        XmlNode verticesNode = useNs
            ? mesh.SelectSingleNode("c:vertices", ns)
            : mesh.SelectSingleNode("vertices");

        string positionSourceId = null;
        if (verticesNode != null)
        {
            string vtxId = verticesNode.Attributes["id"]?.Value;
            XmlNode posInput = useNs
                ? verticesNode.SelectSingleNode("c:input[@semantic='POSITION']", ns)
                : verticesNode.SelectSingleNode("input[@semantic='POSITION']");

            if (posInput != null)
                positionSourceId = posInput.Attributes["source"]?.Value;

            // Alias: el id del nodo <vertices> apunta al mismo source que POSITION
            if (vtxId != null && positionSourceId != null && sources.ContainsKey(positionSourceId))
                sources["#" + vtxId] = sources[positionSourceId];
        }

        // 3. Parsear <triangles>
        XmlNodeList trisList = useNs
            ? mesh.SelectNodes("c:triangles", ns)
            : mesh.SelectNodes("triangles");

        foreach (XmlNode tris in trisList)
        {
            ParseTriangles(tris, ns, useNs, sources, geoId,
                outVerts, outNormals, outUVs, outTangents, outTris);
        }

        // 4. Parsear <polylist> (por si acaso el exportador usa polígonos en lugar de triángulos)
        XmlNodeList polyList = useNs
            ? mesh.SelectNodes("c:polylist", ns)
            : mesh.SelectNodes("polylist");

        foreach (XmlNode poly in polyList)
        {
            ParsePolylist(poly, ns, useNs, sources, geoId,
                outVerts, outNormals, outUVs, outTangents, outTris);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Parseo de <triangles>
    // El DAE de este proyecto usa offset compartido = 0 para todos los inputs
    // ─────────────────────────────────────────────────────────────────────────
    private static void ParseTriangles(
        XmlNode trisNode, XmlNamespaceManager ns, bool useNs,
        Dictionary<string, float[]> sources, string geoId,
        List<Vector3> outVerts, List<Vector3> outNormals,
        List<Vector2> outUVs,   List<Vector4> outTangents,
        List<int> outTris)
    {
        // Leer inputs y sus offsets
        InputInfo posIn, normIn, uvIn, tangIn;
        ReadInputs(trisNode, ns, useNs, out posIn, out normIn, out uvIn, out tangIn);

        if (posIn.source == null)
        {
            Debug.LogWarning($"DaeParser [{geoId}]: No se encontró input VERTEX/POSITION en <triangles>.");
            return;
        }

        // Leer el array de índices <p>
        XmlNode pNode = useNs
            ? trisNode.SelectSingleNode("c:p", ns)
            : trisNode.SelectSingleNode("p");

        if (pNode == null) return;

        int[] indices = ParseIntArray(pNode.InnerText);
        int stride = GetMaxOffset(posIn, normIn, uvIn, tangIn) + 1;
        int baseVertex = outVerts.Count;

        float[] posArr  = GetSource(sources, posIn.source,  geoId, "POSITION");
        float[] normArr = GetSource(sources, normIn.source,  geoId, "NORMAL");
        float[] uvArr   = GetSource(sources, uvIn.source,    geoId, "TEXCOORD");
        float[] tanArr  = GetSource(sources, tangIn.source,  geoId, "TANGENT");

        int triCount = indices.Length / (stride * 3);

        for (int t = 0; t < triCount; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int idx = (t * 3 + v) * stride;

                // Posición — Swizzle RH Z-Up → LH Y-Up: (X, Z, -Y)
                int pi = indices[idx + posIn.offset] * 3;
                outVerts.Add(new Vector3(posArr[pi], posArr[pi+2], -posArr[pi+1]));

                // Normal — Swizzle RH Z-Up → LH Y-Up: (X, Z, -Y)
                if (normArr != null)
                {
                    int ni = indices[idx + normIn.offset] * 3;
                    outNormals.Add(new Vector3(normArr[ni], normArr[ni+2], -normArr[ni+1]));
                }

                // UV (COLLADA: V invertida respecto a Unity)
                if (uvArr != null)
                {
                    int ui = indices[idx + uvIn.offset] * 2;
                    outUVs.Add(new Vector2(uvArr[ui], 1f - uvArr[ui+1]));
                }

                // Tangente — Swizzle RH Z-Up → LH Y-Up: (X, Z, -Y)
                if (tanArr != null)
                {
                    int ti2 = indices[idx + tangIn.offset] * 3;
                    outTangents.Add(new Vector4(tanArr[ti2], tanArr[ti2+2], -tanArr[ti2+1], 1f));
                }

                outTris.Add(baseVertex + (t * 3 + v));
            }

            // --- INVERSIÓN DE WINDING ORDER (RH→LH) ---
            // Intercambiamos el 2do y 3er índice del triángulo para que
            // las caras externas sean front-face en el sistema LH de Unity.
            int last = outTris.Count - 1;
            int tmp = outTris[last];
            outTris[last] = outTris[last - 1];
            outTris[last - 1] = tmp;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Parseo de <polylist> (triangulación en abanico)
    // ─────────────────────────────────────────────────────────────────────────
    private static void ParsePolylist(
        XmlNode polyNode, XmlNamespaceManager ns, bool useNs,
        Dictionary<string, float[]> sources, string geoId,
        List<Vector3> outVerts, List<Vector3> outNormals,
        List<Vector2> outUVs,   List<Vector4> outTangents,
        List<int> outTris)
    {
        InputInfo posIn, normIn, uvIn, tangIn;
        ReadInputs(polyNode, ns, useNs, out posIn, out normIn, out uvIn, out tangIn);

        if (posIn.source == null) return;

        XmlNode vcountNode = useNs
            ? polyNode.SelectSingleNode("c:vcount", ns)
            : polyNode.SelectSingleNode("vcount");
        XmlNode pNode = useNs
            ? polyNode.SelectSingleNode("c:p", ns)
            : polyNode.SelectSingleNode("p");

        if (vcountNode == null || pNode == null) return;

        int[] vcounts = ParseIntArray(vcountNode.InnerText);
        int[] indices = ParseIntArray(pNode.InnerText);
        int stride = GetMaxOffset(posIn, normIn, uvIn, tangIn) + 1;

        float[] posArr  = GetSource(sources, posIn.source,  geoId, "POSITION");
        float[] normArr = GetSource(sources, normIn.source,  geoId, "NORMAL");
        float[] uvArr   = GetSource(sources, uvIn.source,    geoId, "TEXCOORD");
        float[] tanArr  = GetSource(sources, tangIn.source,  geoId, "TANGENT");

        int indexCursor = 0;

        foreach (int vc in vcounts)
        {
            // Guardar los vértices del polígono temporalmente
            int polyBase = outVerts.Count;

            for (int v = 0; v < vc; v++)
            {
                int idx = (indexCursor + v) * stride;

                int pi = indices[idx + posIn.offset] * 3;
                outVerts.Add(new Vector3(posArr[pi], posArr[pi+2], -posArr[pi+1]));

                if (normArr != null)
                {
                    int ni = indices[idx + normIn.offset] * 3;
                    outNormals.Add(new Vector3(normArr[ni], normArr[ni+2], -normArr[ni+1]));
                }

                if (uvArr != null)
                {
                    int ui = indices[idx + uvIn.offset] * 2;
                    outUVs.Add(new Vector2(uvArr[ui], 1f - uvArr[ui+1]));
                }

                if (tanArr != null)
                {
                    int ti2 = indices[idx + tangIn.offset] * 3;
                    outTangents.Add(new Vector4(tanArr[ti2], tanArr[ti2+2], -tanArr[ti2+1], 1f));
                }
            }

            // Triangulación en abanico con winding invertido para LH (RH→LH)
            for (int v = 1; v < vc - 1; v++)
            {
                outTris.Add(polyBase);
                outTris.Add(polyBase + v + 1);  // Invertido: era v, ahora v+1
                outTris.Add(polyBase + v);       // Invertido: era v+1, ahora v
            }

            indexCursor += vc;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private struct InputInfo
    {
        public string source;
        public int    offset;
    }

    private static void ReadInputs(XmlNode parent, XmlNamespaceManager ns, bool useNs,
        out InputInfo pos, out InputInfo norm, out InputInfo uv, out InputInfo tan)
    {
        pos  = new InputInfo { source = null, offset = 0 };
        norm = new InputInfo { source = null, offset = 0 };
        uv   = new InputInfo { source = null, offset = 0 };
        tan  = new InputInfo { source = null, offset = 0 };

        XmlNodeList inputs = useNs
            ? parent.SelectNodes("c:input", ns)
            : parent.SelectNodes("input");

        foreach (XmlNode inp in inputs)
        {
            string sem    = inp.Attributes["semantic"]?.Value;
            string src    = inp.Attributes["source"]?.Value;
            int    offset = 0;
            int.TryParse(inp.Attributes["offset"]?.Value, out offset);

            switch (sem)
            {
                case "VERTEX":   pos  = new InputInfo { source = src, offset = offset }; break;
                case "POSITION": pos  = new InputInfo { source = src, offset = offset }; break;
                case "NORMAL":   norm = new InputInfo { source = src, offset = offset }; break;
                case "TEXCOORD": uv   = new InputInfo { source = src, offset = offset }; break;
                case "TANGENT":  tan  = new InputInfo { source = src, offset = offset }; break;
            }
        }
    }

    private static int GetMaxOffset(InputInfo a, InputInfo b, InputInfo c, InputInfo d)
    {
        int max = a.offset;
        if (b.source != null && b.offset > max) max = b.offset;
        if (c.source != null && c.offset > max) max = c.offset;
        if (d.source != null && d.offset > max) max = d.offset;
        return max;
    }

    private static float[] GetSource(Dictionary<string, float[]> sources, string id,
        string geoId, string semantic)
    {
        if (id == null) return null;
        float[] arr;
        if (sources.TryGetValue(id, out arr)) return arr;
        Debug.LogWarning($"DaeParser [{geoId}]: No se encontró source '{id}' para {semantic}.");
        return null;
    }

    private static int[] ParseIntArray(string text)
    {
        string[] tokens = text.Split(new char[]{' ','\t','\r','\n'},
            System.StringSplitOptions.RemoveEmptyEntries);
        int[] arr = new int[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
            arr[i] = int.Parse(tokens[i], CultureInfo.InvariantCulture);
        return arr;
    }
	
	private static void CentrarVertices(List<Vector3> vertices)
    {
        if (vertices.Count == 0) return;

        Vector3 min = vertices[0], max = vertices[0];
        foreach (Vector3 v in vertices)
        {
            if (v.x < min.x) min.x = v.x; if (v.x > max.x) max.x = v.x;
            if (v.y < min.y) min.y = v.y; if (v.y > max.y) max.y = v.y;
            if (v.z < min.z) min.z = v.z; if (v.z > max.z) max.z = v.z;
        }

        // Centramos X y Z para que no esté corrida hacia los costados.
        // En Y, usamos min.y para que la BASE de la pava quede a ras del piso (Y=0).
        Vector3 pivote = new Vector3((min.x + max.x) / 2f, min.y, (min.z + max.z) / 2f);

        for (int i = 0; i < vertices.Count; i++)
        {
            // Nota: En C# debemos reasignar el struct completo en la lista
            vertices[i] = vertices[i] - pivote;
        }
    }
}
