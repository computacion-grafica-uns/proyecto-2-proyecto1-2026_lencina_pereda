using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.IO;

/// <summary>
/// Parser manual para archivos COLLADA (.dae) - Proyecto 2 Computación Gráfica 2026.
/// Implementa conversión Right-Handed a Left-Handed (X = -X), Vertex Welding y generación
/// automática de Normales y Tangentes para Shaders PBR/Toon.
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

        XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");

        List<Vector3> finalVerts = new List<Vector3>();
        List<Vector2> finalUVs   = new List<Vector2>();
        List<int>     finalTris  = new List<int>();

        Dictionary<string, int> vertexCache = new Dictionary<string, int>();

        XmlNodeList geometries = doc.SelectNodes("//c:library_geometries/c:geometry", ns);

        if (geometries == null || geometries.Count == 0)
        {
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
            ParseGeometry(geo, ns, useNs, finalVerts, finalUVs, finalTris, vertexCache);
        }

        if (finalVerts.Count == 0)
        {
            Debug.LogError("DaeParser: No se extrajeron vértices.");
            return null;
        }

        CentrarVertices(finalVerts);

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices  = finalVerts.ToArray();
        mesh.triangles = finalTris.ToArray();
        
        if (finalUVs.Count > 0) 
            mesh.uv = finalUVs.ToArray();

        // Que Unity genere la matemática lumínica sobre la geometría ya reparada
        mesh.RecalculateNormals();

        if (mesh.uv != null && mesh.uv.Length > 0)
        {
            mesh.RecalculateTangents();
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    private static void ParseGeometry(
        XmlNode geo, XmlNamespaceManager ns, bool useNs,
        List<Vector3> outVerts, List<Vector2> outUVs, List<int> outTris, Dictionary<string, int> vertexCache)
    {
        System.Func<XmlNode, string, XmlNode> sel = (node, xpath) =>
            useNs ? node.SelectSingleNode(xpath.Replace("/", "/c:").TrimStart('/').Insert(0, "c:"), ns)
                  : node.SelectSingleNode(xpath);

        XmlNode mesh = sel(geo, "mesh");
        if (mesh == null) return;

        string geoId = geo.Attributes["id"]?.Value ?? "unknown";

        Dictionary<string, float[]> sources = new Dictionary<string, float[]>();
        XmlNodeList sourceNodes = useNs ? mesh.SelectNodes("c:source", ns) : mesh.SelectNodes("source");

        foreach (XmlNode src in sourceNodes)
        {
            string srcId = src.Attributes["id"]?.Value;
            if (srcId == null) continue;

            XmlNode arr = useNs ? src.SelectSingleNode("c:float_array", ns) : src.SelectSingleNode("float_array");
            if (arr == null) continue;

            string[] tokens = arr.InnerText.Split(new char[]{' ','\t','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries);
            float[] floats = new float[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                floats[i] = float.Parse(tokens[i], CultureInfo.InvariantCulture);

            sources["#" + srcId] = floats;
        }

        XmlNode verticesNode = useNs ? mesh.SelectSingleNode("c:vertices", ns) : mesh.SelectSingleNode("vertices");
        string positionSourceId = null;
        if (verticesNode != null)
        {
            string vtxId = verticesNode.Attributes["id"]?.Value;
            XmlNode posInput = useNs ? verticesNode.SelectSingleNode("c:input[@semantic='POSITION']", ns) : verticesNode.SelectSingleNode("input[@semantic='POSITION']");

            if (posInput != null) positionSourceId = posInput.Attributes["source"]?.Value;
            if (vtxId != null && positionSourceId != null && sources.ContainsKey(positionSourceId))
                sources["#" + vtxId] = sources[positionSourceId];
        }

        XmlNodeList trisList = useNs ? mesh.SelectNodes("c:triangles", ns) : mesh.SelectNodes("triangles");
        foreach (XmlNode tris in trisList)
            ParseTriangles(tris, ns, useNs, sources, geoId, outVerts, outUVs, outTris, vertexCache);

        XmlNodeList polyList = useNs ? mesh.SelectNodes("c:polylist", ns) : mesh.SelectNodes("polylist");
        foreach (XmlNode poly in polyList)
            ParsePolylist(poly, ns, useNs, sources, geoId, outVerts, outUVs, outTris, vertexCache);
    }

    private static void ParseTriangles(
        XmlNode trisNode, XmlNamespaceManager ns, bool useNs,
        Dictionary<string, float[]> sources, string geoId,
        List<Vector3> outVerts, List<Vector2> outUVs, List<int> outTris, Dictionary<string, int> vertexCache)
    {
        InputInfo posIn, normIn, uvIn, tangIn;
        ReadInputs(trisNode, ns, useNs, out posIn, out normIn, out uvIn, out tangIn);

        if (posIn.source == null) return;

        XmlNode pNode = useNs ? trisNode.SelectSingleNode("c:p", ns) : trisNode.SelectSingleNode("p");
        if (pNode == null) return;

        int[] indices = ParseIntArray(pNode.InnerText);
        int stride = GetMaxOffset(posIn, normIn, uvIn, tangIn) + 1;

        float[] posArr = GetSource(sources, posIn.source, geoId, "POSITION");
        float[] uvArr  = GetSource(sources, uvIn.source,  geoId, "TEXCOORD");

        int triCount = indices.Length / (stride * 3);

        for (int t = 0; t < triCount; t++)
        {
            int[] triIndices = new int[3];

            for (int v = 0; v < 3; v++)
            {
                int idx = (t * 3 + v) * stride;

                int pi = indices[idx + posIn.offset];
                int ni = normIn.source != null ? indices[idx + normIn.offset] : -1;
                int ui = uvIn.source != null ? indices[idx + uvIn.offset] : -1;
                int ti = tangIn.source != null ? indices[idx + tangIn.offset] : -1;

                string key = $"{geoId}_{pi}_{ni}_{ui}_{ti}";

                if (!vertexCache.TryGetValue(key, out int vertexIndex))
                {
                    vertexIndex = outVerts.Count;
                    
                    // --- CONVERSIÓN A LEFT-HANDED ---
                    // Negamos la X para des-espejar el modelo y que quede del lado correcto
                    outVerts.Add(new Vector3(-posArr[pi*3], posArr[pi*3+1], posArr[pi*3+2]));

                    if (uvArr != null && ui >= 0) 
                        outUVs.Add(new Vector2(uvArr[ui*2], 1f - uvArr[ui*2+1]));
                    else 
                        outUVs.Add(Vector2.zero);

                    vertexCache[key] = vertexIndex;
                }
                triIndices[v] = vertexIndex;
            }

            // --- CORRECCIÓN DE DIBUJO ---
            // Al negar X, forzamos a las caras a conectarse en el orden inverso (0, 2, 1)
            // para que miren hacia afuera.
            outTris.Add(triIndices[0]);
            outTris.Add(triIndices[2]);
            outTris.Add(triIndices[1]);
        }
    }

    private static void ParsePolylist(
        XmlNode polyNode, XmlNamespaceManager ns, bool useNs,
        Dictionary<string, float[]> sources, string geoId,
        List<Vector3> outVerts, List<Vector2> outUVs, List<int> outTris, Dictionary<string, int> vertexCache)
    {
        InputInfo posIn, normIn, uvIn, tangIn;
        ReadInputs(polyNode, ns, useNs, out posIn, out normIn, out uvIn, out tangIn);

        if (posIn.source == null) return;

        XmlNode vcountNode = useNs ? polyNode.SelectSingleNode("c:vcount", ns) : polyNode.SelectSingleNode("vcount");
        XmlNode pNode = useNs ? polyNode.SelectSingleNode("c:p", ns) : polyNode.SelectSingleNode("p");

        if (vcountNode == null || pNode == null) return;

        int[] vcounts = ParseIntArray(vcountNode.InnerText);
        int[] indices = ParseIntArray(pNode.InnerText);
        int stride = GetMaxOffset(posIn, normIn, uvIn, tangIn) + 1;

        float[] posArr = GetSource(sources, posIn.source, geoId, "POSITION");
        float[] uvArr  = GetSource(sources, uvIn.source, geoId, "TEXCOORD");

        int indexCursor = 0;

        foreach (int vc in vcounts)
        {
            List<int> polyIndices = new List<int>();

            for (int v = 0; v < vc; v++)
            {
                int idx = (indexCursor + v) * stride;

                int pi = indices[idx + posIn.offset];
                int ni = normIn.source != null ? indices[idx + normIn.offset] : -1;
                int ui = uvIn.source != null ? indices[idx + uvIn.offset] : -1;
                int ti = tangIn.source != null ? indices[idx + tangIn.offset] : -1;

                string key = $"{geoId}_{pi}_{ni}_{ui}_{ti}";

                if (!vertexCache.TryGetValue(key, out int vertexIndex))
                {
                    vertexIndex = outVerts.Count;
                    
                    outVerts.Add(new Vector3(-posArr[pi*3], posArr[pi*3+1], posArr[pi*3+2]));

                    if (uvArr != null && ui >= 0) 
                        outUVs.Add(new Vector2(uvArr[ui*2], 1f - uvArr[ui*2+1]));
                    else 
                        outUVs.Add(Vector2.zero);

                    vertexCache[key] = vertexIndex;
                }
                polyIndices.Add(vertexIndex);
            }

            for (int v = 1; v < vc - 1; v++)
            {
                outTris.Add(polyIndices[0]);
                outTris.Add(polyIndices[v + 1]);
                outTris.Add(polyIndices[v]);
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
        public int offset; 
    }

    private static void ReadInputs(XmlNode parent, XmlNamespaceManager ns, bool useNs,
        out InputInfo pos, out InputInfo norm, out InputInfo uv, out InputInfo tan)
    {
        pos  = new InputInfo { source = null, offset = 0 };
        norm = new InputInfo { source = null, offset = 0 };
        uv   = new InputInfo { source = null, offset = 0 };
        tan  = new InputInfo { source = null, offset = 0 };

        XmlNodeList inputs = useNs ? parent.SelectNodes("c:input", ns) : parent.SelectNodes("input");

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

    private static float[] GetSource(Dictionary<string, float[]> sources, string id, string geoId, string semantic)
    {
        if (id == null) return null;
        if (sources.TryGetValue(id, out float[] arr)) return arr;
        return null;
    }

    private static int[] ParseIntArray(string text)
    {
        string[] tokens = text.Split(new char[]{' ','\t','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries);
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
        Vector3 centro = (min + max) / 2f;
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] -= centro;
    }
}