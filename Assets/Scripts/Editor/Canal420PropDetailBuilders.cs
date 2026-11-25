#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Canal420.Audio;
using Canal420.Effects;
using Canal420.Interaction;

namespace Canal420.EditorTools
{
    public static partial class Canal420GreyboxBuilder
    {
        private const string MeshesFolder = "Assets/Meshes/Greybox";

        private static Bounds ComputeRendererWorldBounds(Renderer r)
        {
            if (r == null) return new Bounds(Vector3.zero, Vector3.zero);
            MeshFilter mf = r.GetComponent<MeshFilter>();
            Matrix4x4 m = r.transform.localToWorldMatrix;
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertices != null && mf.sharedMesh.vertices.Length > 0)
            {
                Vector3[] verts = mf.sharedMesh.vertices;
                Bounds wb = new Bounds(m.MultiplyPoint3x4(verts[0]), Vector3.zero);
                for (int i = 1; i < verts.Length; i++)
                {
                    wb.Encapsulate(m.MultiplyPoint3x4(verts[i]));
                }
                return wb;
            }
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.bounds.size.sqrMagnitude > 0f)
            {
                Bounds lb = mf.sharedMesh.bounds;
                Vector3 c = lb.center;
                Vector3 e = lb.extents;
                Bounds wb = new Bounds(m.MultiplyPoint3x4(c), Vector3.zero);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        for (int sz = -1; sz <= 1; sz += 2)
                        {
                            wb.Encapsulate(m.MultiplyPoint3x4(c + new Vector3(e.x * sx, e.y * sy, e.z * sz)));
                        }
                    }
                }
                return wb;
            }
            return r.bounds;
        }

        private static Bounds ComputeHierarchyWorldBounds(GameObject root)
        {
            if (root == null) return new Bounds(Vector3.zero, Vector3.zero);
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            bool hasAny = false;
            Bounds total = new Bounds(root.transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled) continue;
                Bounds b = ComputeRendererWorldBounds(renderers[i]);
                if (!hasAny)
                {
                    total = b;
                    hasAny = true;
                }
                else
                {
                    total.Encapsulate(b);
                }
            }
            return total;
        }

        private static void SnapToSupportWorldY(GameObject obj, float targetMinWorldY)
        {
            if (obj == null) return;
            Bounds b = ComputeHierarchyWorldBounds(obj);
            float deltaY = targetMinWorldY - b.min.y;
            if (Mathf.Abs(deltaY) > 1e-6f)
            {
                Vector3 p = obj.transform.position;
                p.y += deltaY;
                obj.transform.position = p;
            }
        }

        private static void SnapToSupportTop(GameObject obj, GameObject supportObj, float extraOffsetY = 0f)
        {
            if (obj == null || supportObj == null) return;
            Bounds sup = ComputeHierarchyWorldBounds(supportObj);
            SnapToSupportWorldY(obj, sup.max.y + extraOffsetY);
        }

        private static GameObject CreatePrintedOverlayQuad(
            Transform parent, string name, Vector3 localPos, Vector2 sizeXY, Material mat, Vector3 localEuler)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.localPosition = localPos;
            q.transform.localRotation = Quaternion.Euler(localEuler);
            q.transform.localScale = new Vector3(sizeXY.x, sizeXY.y, 1f);
            if (q.TryGetComponent<Collider>(out Collider col))
                Object.DestroyImmediate(col);
            if (q.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
            {
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = true;
                if (mat != null) mr.sharedMaterial = mat;
            }
            return q;
        }

        private static GameObject CreateCylinderLabelSleeve(
            Transform parent, string name, Vector3 localPos, Vector3 scaleXYZ, Material mat, GreyboxPalette pal, Vector3 localEuler = default)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = scaleXYZ;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = pal.details.labelCylinderSleeveMesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            if (mat != null) mr.sharedMaterial = mat;
            return go;
        }

        private static void EnsureMeshFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
                AssetDatabase.CreateFolder("Assets", "Meshes");
            if (!AssetDatabase.IsValidFolder(MeshesFolder))
                AssetDatabase.CreateFolder("Assets/Meshes", "Greybox");
        }

        private static Mesh cachedLowPolyCylinder8Mesh;
        private static Mesh cachedLowPolyIcosphere1Mesh;

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(1e-5f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static void AssignProceduralVertexAO(Mesh m, bool symmetricCapsOnly = false)
        {
            if (m == null) return;
            Vector3[] verts = m.vertices;
            Vector3[] norms = m.normals;
            if (verts == null || verts.Length == 0) return;

            m.RecalculateBounds();
            Bounds b = m.bounds;
            Vector3 center = b.center;
            float minY = b.min.y;
            float sizeY = Mathf.Max(0.0005f, b.size.y);
            float maxExtent = Mathf.Max(0.0005f, b.extents.magnitude);

            Color32[] cols = new Color32[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                Vector3 n = (norms != null && i < norms.Length && norms[i].sqrMagnitude > 1e-6f)
                    ? norms[i].normalized
                    : Vector3.up;

                float ao;
                if (symmetricCapsOnly)
                {
                    float capDot = Mathf.Abs(n.y);
                    float capTint = Mathf.Lerp(0.96f, 0.86f, SmoothStep01(0.60f, 0.99f, capDot));
                    float ySym = Mathf.Abs((v.y - center.y) / Mathf.Max(0.001f, b.extents.y));
                    ao = Mathf.Lerp(capTint, 0.98f, 0.20f * (1f - ySym));
                }
                else
                {
                    float h01 = Mathf.Clamp01((v.y - minY) / sizeY);
                    float heightAO = Mathf.Lerp(0.78f, 1.00f, SmoothStep01(0.0f, 0.36f, h01));

                    float up01 = n.y * 0.5f + 0.5f;
                    float normalAO = Mathf.Lerp(0.80f, 1.00f, SmoothStep01(0.0f, 0.78f, up01));

                    Vector3 toVert = v - center;
                    float distNorm = Mathf.Clamp01(toVert.magnitude / maxExtent);
                    float outward = toVert.sqrMagnitude > 1e-8f ? Vector3.Dot(toVert.normalized, n) : 1f;
                    float cavityAO = outward < -0.22f
                        ? Mathf.Lerp(0.62f, 0.90f, Mathf.Clamp01((outward + 0.85f) / 0.63f))
                        : 1.00f;
                    float depthCrease = Mathf.Lerp(0.88f, 1.00f, SmoothStep01(0.15f, 0.75f, distNorm));

                    ao = heightAO * normalAO * cavityAO * depthCrease;
                }

                byte aoByte = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(ao, 0.55f, 1.0f) * 255f), 140, 255);
                cols[i] = new Color32(aoByte, aoByte, aoByte, 255);
            }

            m.colors32 = cols;
        }

        private static Mesh SaveOrUpdateMeshAsset(Mesh generated, string assetName)
        {
            EnsureMeshFolder();
            Color32[] existingColors = generated.colors32;
            if (existingColors == null || existingColors.Length != generated.vertexCount)
            {
                AssignProceduralVertexAO(generated);
            }

            string path = $"{MeshesFolder}/{assetName}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = generated.vertices;
                existing.triangles = generated.triangles;
                existing.uv = generated.uv;
                existing.normals = generated.normals;
                existing.colors32 = generated.colors32;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(generated);
                return existing;
            }
            generated.name = assetName;
            AssetDatabase.CreateAsset(generated, path);
            return generated;
        }

        private static Mesh GetOrBuildLowPolyCylinder8Mesh()
        {
            if (cachedLowPolyCylinder8Mesh != null && AssetDatabase.Contains(cachedLowPolyCylinder8Mesh))
                return cachedLowPolyCylinder8Mesh;

            const int sides = 8;
            const float r = 0.5f;
            const float yBot = -1.0f;
            const float yTop = 1.0f;

            var verts = new List<Vector3>(80);
            var tris = new List<int>(96);
            var uvs = new List<Vector2>(80);

            Vector3[] topRing = new Vector3[sides];
            Vector3[] botRing = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float ang = (2f * Mathf.PI * i) / sides;
                float cx = r * Mathf.Cos(ang);
                float cz = r * Mathf.Sin(ang);
                topRing[i] = new Vector3(cx, yTop, cz);
                botRing[i] = new Vector3(cx, yBot, cz);
            }

            // 8 flat-shaded side quads (16 tris) with proper cylindrical UVs
            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;
                float u0 = (float)i / sides;
                float u1 = (float)(i + 1) / sides;

                int idx = verts.Count;
                verts.Add(botRing[i]); verts.Add(topRing[i]); verts.Add(topRing[inext]);
                uvs.Add(new Vector2(u0, 0f)); uvs.Add(new Vector2(u0, 1f)); uvs.Add(new Vector2(u1, 1f));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

                verts.Add(botRing[i]); verts.Add(topRing[inext]); verts.Add(botRing[inext]);
                uvs.Add(new Vector2(u0, 0f)); uvs.Add(new Vector2(u1, 1f)); uvs.Add(new Vector2(u1, 0f));
                tris.Add(idx + 3); tris.Add(idx + 4); tris.Add(idx + 5);
            }

            // 8 top cap tris + 8 bottom cap tris (32 tris total, strictly closed watertight manifold)
            Vector3 cTop = new Vector3(0f, yTop, 0f);
            Vector3 cBot = new Vector3(0f, yBot, 0f);
            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;

                int topIdx = verts.Count;
                verts.Add(topRing[i]); verts.Add(cTop); verts.Add(topRing[inext]);
                uvs.Add(new Vector2(0.5f + topRing[i].x, 0.5f + topRing[i].z));
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f + topRing[inext].x, 0.5f + topRing[inext].z));
                tris.Add(topIdx); tris.Add(topIdx + 1); tris.Add(topIdx + 2);

                int botIdx = verts.Count;
                verts.Add(cBot); verts.Add(botRing[i]); verts.Add(botRing[inext]);
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f + botRing[i].x, 0.5f + botRing[i].z));
                uvs.Add(new Vector2(0.5f + botRing[inext].x, 0.5f + botRing[inext].z));
                tris.Add(botIdx); tris.Add(botIdx + 1); tris.Add(botIdx + 2);
            }

            Mesh m = new Mesh { name = "Mesh_LowPoly_Cylinder8" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            AssignProceduralVertexAO(m, symmetricCapsOnly: true);

            cachedLowPolyCylinder8Mesh = SaveOrUpdateMeshAsset(m, "Mesh_LowPoly_Cylinder8");
            return cachedLowPolyCylinder8Mesh;
        }

        private static Mesh GetOrBuildLowPolyIcosphere1Mesh()
        {
            if (cachedLowPolyIcosphere1Mesh != null && AssetDatabase.Contains(cachedLowPolyIcosphere1Mesh))
                return cachedLowPolyIcosphere1Mesh;

            const float radius = 0.5f;
            float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;

            var sphereVerts = new List<Vector3>(42)
            {
                new Vector3(-1f,  phi,  0f).normalized * radius,
                new Vector3( 1f,  phi,  0f).normalized * radius,
                new Vector3(-1f, -phi,  0f).normalized * radius,
                new Vector3( 1f, -phi,  0f).normalized * radius,
                new Vector3( 0f, -1f,  phi).normalized * radius,
                new Vector3( 0f,  1f,  phi).normalized * radius,
                new Vector3( 0f, -1f, -phi).normalized * radius,
                new Vector3( 0f,  1f, -phi).normalized * radius,
                new Vector3( phi,  0f, -1f).normalized * radius,
                new Vector3( phi,  0f,  1f).normalized * radius,
                new Vector3(-phi,  0f, -1f).normalized * radius,
                new Vector3(-phi,  0f,  1f).normalized * radius
            };

            int[] baseFaces =
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            var midpointCache = new Dictionary<long, int>(60);
            int GetMidpoint(int i0, int i1)
            {
                long key = i0 < i1 ? (((long)i0 << 32) | (uint)i1) : (((long)i1 << 32) | (uint)i0);
                if (midpointCache.TryGetValue(key, out int cachedIdx))
                    return cachedIdx;

                Vector3 mid = ((sphereVerts[i0] + sphereVerts[i1]) * 0.5f).normalized * radius;
                int idx = sphereVerts.Count;
                sphereVerts.Add(mid);
                midpointCache[key] = idx;
                return idx;
            }

            var subFaces = new List<(int a, int b, int c)>(80);
            for (int f = 0; f < baseFaces.Length; f += 3)
            {
                int i0 = baseFaces[f];
                int i1 = baseFaces[f + 1];
                int i2 = baseFaces[f + 2];

                int a = GetMidpoint(i0, i1);
                int b = GetMidpoint(i1, i2);
                int c = GetMidpoint(i2, i0);

                subFaces.Add((i0, a, c));
                subFaces.Add((i1, b, a));
                subFaces.Add((i2, c, b));
                subFaces.Add((a, b, c));
            }

            Vector2 SphereUV(Vector3 p)
            {
                Vector3 d = p / radius;
                float u = 0.5f + Mathf.Atan2(d.z, d.x) / (2f * Mathf.PI);
                float v = 0.5f + Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI;
                return new Vector2(u, v);
            }

            var verts = new List<Vector3>(240);
            var tris = new List<int>(240);
            var uvs = new List<Vector2>(240);
            for (int i = 0; i < subFaces.Count; i++)
            {
                var (ia, ib, ic) = subFaces[i];
                Vector3 p0 = sphereVerts[ia];
                Vector3 p1 = sphereVerts[ib];
                Vector3 p2 = sphereVerts[ic];
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), p0 + p1 + p2) < 0f)
                {
                    Vector3 tmp = p1;
                    p1 = p2;
                    p2 = tmp;
                }

                int vIdx = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2);
                uvs.Add(SphereUV(p0)); uvs.Add(SphereUV(p1)); uvs.Add(SphereUV(p2));
                tris.Add(vIdx); tris.Add(vIdx + 1); tris.Add(vIdx + 2);
            }

            Mesh m = new Mesh { name = "Mesh_LowPoly_Icosphere1" };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            AssignProceduralVertexAO(m, symmetricCapsOnly: true);

            cachedLowPolyIcosphere1Mesh = SaveOrUpdateMeshAsset(m, "Mesh_LowPoly_Icosphere1");
            return cachedLowPolyIcosphere1Mesh;
        }

        private static GameObject CreateMeshObject(
            Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, Vector3 localEuler = default, bool castShadows = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = Vector3.one;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        private static void AddFlatQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Dois triângulos independentes por quad para que quads curvos/torcidos não compartilhem nem sobrescrevam normais.
            int idx = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f));
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);

            verts.Add(p0); verts.Add(p2); verts.Add(p3);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f));
            tris.Add(idx + 3); tris.Add(idx + 4); tris.Add(idx + 5);
        }

        private static void AddFlatTri(List<Vector3> verts, List<int> tris, List<Vector2> uvs, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            int idx = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0.5f, 1f)); uvs.Add(new Vector2(1f, 0f));
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        }

        private static void AssignFlatFaceNormals(Mesh m, List<Vector3> verts, List<int> tris)
        {
            Vector3[] norms = new Vector3[verts.Count];
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                int i0 = tris[t];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];
                Vector3 e1 = (verts[i1] - verts[i0]) * 1000f;
                Vector3 e2 = (verts[i2] - verts[i0]) * 1000f;
                Vector3 n = Vector3.Cross(e1, e2);
                if (n.sqrMagnitude > 1e-12f)
                {
                    n = n.normalized;
                    norms[i0] = n;
                    norms[i1] = n;
                    norms[i2] = n;
                }
            }
            // Guarantee non-zero unit normal even if one triangle of an unshared quad was thin
            for (int i = 0; i < norms.Length; i++)
            {
                if (norms[i].sqrMagnitude < 1e-6f) norms[i] = Vector3.up;
            }
            m.normals = norms;
        }

        private static Mesh CreateFacetedLatheVesselMesh(string assetName, int sides, (float y, float r)[] profile)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 RingPt(int k, int i)
            {
                float ang = (2f * Mathf.PI * (i % sides)) / sides;
                float r = profile[k].r;
                return new Vector3(r * Mathf.Cos(ang), profile[k].y, r * Mathf.Sin(ang));
            }

            for (int k = 0; k < profile.Length - 1; k++)
            {
                float r0 = profile[k].r;
                float r1 = profile[k + 1].r;
                if (r0 <= 1e-6f && r1 <= 1e-6f) continue;

                for (int i = 0; i < sides; i++)
                {
                    int inext = (i + 1) % sides;
                    if (r0 <= 1e-6f)
                    {
                        Vector3 c = new Vector3(0f, profile[k].y, 0f);
                        AddFlatTri(verts, tris, uvs, c, RingPt(k + 1, i), RingPt(k + 1, inext));
                    }
                    else if (r1 <= 1e-6f)
                    {
                        Vector3 c = new Vector3(0f, profile[k + 1].y, 0f);
                        AddFlatTri(verts, tris, uvs, RingPt(k, i), c, RingPt(k, inext));
                    }
                    else
                    {
                        AddFlatQuad(verts, tris, uvs, RingPt(k, i), RingPt(k + 1, i), RingPt(k + 1, inext), RingPt(k, inext));
                    }
                }
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private static Mesh CreateFacetedCShapeHandleMesh(string assetName, Vector3[] spinePts, float halfThicknessY, float halfThicknessZ)
        {
            return CreateFacetedCShapeHandleMesh(assetName, spinePts, halfThicknessY, halfThicknessZ, Vector3.forward);
        }

        private static Mesh CreateFacetedCShapeHandleMesh(string assetName, Vector3[] spinePts, float halfThicknessY, float halfThicknessZ, Vector3 sideAxis)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            Vector3 sideZ = sideAxis.sqrMagnitude > 1e-6f ? sideAxis.normalized : Vector3.forward;

            Vector3[][] rings = new Vector3[spinePts.Length][];
            for (int i = 0; i < spinePts.Length; i++)
            {
                Vector3 tangent = i == 0 ? (spinePts[1] - spinePts[0]).normalized
                                : i == spinePts.Length - 1 ? (spinePts[i] - spinePts[i - 1]).normalized
                                : (spinePts[i + 1] - spinePts[i - 1]).normalized;
                Vector3 normalOut = Vector3.Cross(sideZ, tangent).normalized;
                if (normalOut.sqrMagnitude < 1e-5f) normalOut = Vector3.right;

                Vector3 o = normalOut * halfThicknessY;
                Vector3 w = sideZ * halfThicknessZ;
                Vector3 p = spinePts[i];
                rings[i] = new[]
                {
                    p - o - w,
                    p + o - w,
                    p + o + w,
                    p - o + w
                };
            }

            // Start cap
            AddFlatQuad(verts, tris, uvs, rings[0][3], rings[0][2], rings[0][1], rings[0][0]);
            // Segments (clockwise when viewed from outside -> outward Cross(p1-p0, p2-p0))
            for (int i = 0; i < spinePts.Length - 1; i++)
            {
                for (int e = 0; e < 4; e++)
                {
                    int en = (e + 1) % 4;
                    AddFlatQuad(verts, tris, uvs, rings[i][e], rings[i][en], rings[i + 1][en], rings[i + 1][e]);
                }
            }
            // End cap
            int last = spinePts.Length - 1;
            AddFlatQuad(verts, tris, uvs, rings[last][0], rings[last][1], rings[last][2], rings[last][3]);

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private static Mesh CreateCrumpledPaperMesh(string assetName, float radius, int seed)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3[] baseVerts =
            {
                new Vector3(0f, 1f, 0f),
                new Vector3(0.95f, 0.25f, 0.12f),
                new Vector3(0.28f, 0.20f, 0.92f),
                new Vector3(-0.85f, 0.30f, 0.38f),
                new Vector3(-0.42f, 0.18f, -0.88f),
                new Vector3(0.55f, 0.28f, -0.78f),
                new Vector3(0f, -0.95f, 0f)
            };

            float minY = float.MaxValue;
            for (int i = 0; i < baseVerts.Length; i++)
            {
                float jx = Mathf.Sin((i + 1) * 12.7f + seed * 3.1f) * 0.18f;
                float jy = Mathf.Cos((i + 2) * 7.3f + seed * 5.7f) * 0.14f;
                float jz = Mathf.Sin((i + 3) * 19.1f + seed * 2.3f) * 0.18f;
                baseVerts[i] = new Vector3(
                    (baseVerts[i].x + jx) * radius,
                    (baseVerts[i].y + jy) * radius * 0.72f,
                    (baseVerts[i].z + jz) * radius);
                if (baseVerts[i].y < minY) minY = baseVerts[i].y;
            }
            for (int i = 0; i < baseVerts.Length; i++)
            {
                baseVerts[i].y -= minY;
            }

            int[] idx =
            {
                0, 2, 1,  0, 3, 2,  0, 4, 3,  0, 5, 4,  0, 1, 5,
                6, 1, 2,  6, 2, 3,  6, 3, 4,  6, 4, 5,  6, 5, 1
            };
            for (int t = 0; t + 2 < idx.Length; t += 3)
            {
                AddFlatTri(verts, tris, uvs, baseVerts[idx[t]], baseVerts[idx[t + 1]], baseVerts[idx[t + 2]]);
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        /// <summary>
        /// Torus/anel fechado estanque no plano XZ com centro (0,0,0) e raio maior R.
        /// </summary>
        private static Mesh CreateClosedRingTubeMesh(
            string assetName,
            int radialSides,
            int tubeSides,
            float majorRadius,
            float minorRadiusRadial,
            float minorRadiusY)
        {
            radialSides = Mathf.Max(6, radialSides);
            tubeSides = Mathf.Max(4, tubeSides);

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 RingPt(int i, int j)
            {
                float theta = (2f * Mathf.PI * (i % radialSides)) / radialSides;
                float phi = (2f * Mathf.PI * (j % tubeSides)) / tubeSides;
                float r = majorRadius + minorRadiusRadial * Mathf.Cos(phi);
                return new Vector3(
                    r * Mathf.Cos(theta),
                    minorRadiusY * Mathf.Sin(phi),
                    r * Mathf.Sin(theta));
            }

            for (int i = 0; i < radialSides; i++)
            {
                int inext = (i + 1) % radialSides;
                for (int j = 0; j < tubeSides; j++)
                {
                    int jnext = (j + 1) % tubeSides;
                    AddFlatQuad(
                        verts, tris, uvs,
                        RingPt(i, j),
                        RingPt(i, jnext),
                        RingPt(inext, jnext),
                        RingPt(inext, j));
                }
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        /// <summary>
        /// Loft fechado em torno do eixo Y usando polígono 2D em estrela (X,Z) e camadas verticais com chanfro.
        /// Garante orientação CCW e tampas fechadas sem arestas abertas.
        /// </summary>
        private static Mesh CreateClosedPolygonLoftMesh(
            string assetName,
            Vector2[] polygonXZ,
            (float y, float sx, float sz, float ox, float oz)[] layers)
        {
            Vector2[] poly = (Vector2[])polygonXZ.Clone();
            double signedArea = 0.0;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Length];
                signedArea += (double)a.x * b.y - (double)b.x * a.y;
            }
            if (signedArea < 0.0)
            {
                System.Array.Reverse(poly);
            }

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 Pt(int layerIdx, int pIdx)
            {
                var l = layers[layerIdx];
                Vector2 v = poly[pIdx % poly.Length];
                return new Vector3(l.ox + v.x * l.sx, l.y, l.oz + v.y * l.sz);
            }

            Vector3 cBot = new Vector3(layers[0].ox, layers[0].y, layers[0].oz);
            for (int i = 0; i < poly.Length; i++)
            {
                int inext = (i + 1) % poly.Length;
                AddFlatTri(verts, tris, uvs, cBot, Pt(0, i), Pt(0, inext));
            }

            for (int k = 0; k < layers.Length - 1; k++)
            {
                for (int i = 0; i < poly.Length; i++)
                {
                    int inext = (i + 1) % poly.Length;
                    AddFlatQuad(verts, tris, uvs, Pt(k, i), Pt(k + 1, i), Pt(k + 1, inext), Pt(k, inext));
                }
            }

            int top = layers.Length - 1;
            Vector3 cTop = new Vector3(layers[top].ox, layers[top].y, layers[top].oz);
            for (int i = 0; i < poly.Length; i++)
            {
                int inext = (i + 1) % poly.Length;
                AddFlatTri(verts, tris, uvs, Pt(top, i), cTop, Pt(top, inext));
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private static Mesh CreateChamferedRectLoftMesh(
            string assetName,
            float sizeX,
            float sizeY,
            float sizeZ,
            float chamferXZ,
            float topScaleFactor = 0.92f,
            float bevelY = 0.008f)
        {
            float hx = sizeX * 0.5f;
            float hz = sizeZ * 0.5f;
            float cx = Mathf.Min(chamferXZ, hx * 0.42f);
            float cz = Mathf.Min(chamferXZ, hz * 0.42f);

            Vector2[] poly =
            {
                new Vector2(hx - cx, -hz),
                new Vector2(hx, -hz + cz),
                new Vector2(hx, hz - cz),
                new Vector2(hx - cx, hz),
                new Vector2(-hx + cx, hz),
                new Vector2(-hx, hz - cz),
                new Vector2(-hx, -hz + cz),
                new Vector2(-hx + cx, -hz)
            };

            float b = Mathf.Clamp(bevelY, 0.002f, sizeY * 0.30f);
            return CreateClosedPolygonLoftMesh(assetName, poly, new[]
            {
                (0f, topScaleFactor, topScaleFactor, 0f, 0f),
                (b, 1f, 1f, 0f, 0f),
                (sizeY - b, 1f, 1f, 0f, 0f),
                (sizeY, topScaleFactor, topScaleFactor, 0f, 0f)
            });
        }

        /// <summary>
        /// Monta o conjunto completo e padronizado de mostrador analógico voltado para -Z local:
        /// papel impresso 1..12, aro chanfrado vazado, 3 ponteiros pivotados no centro e vidro M_Clear_Glass.
        /// </summary>
        private static Mesh CreateClosedClockDialMesh(string assetName, int sides, float radius, float thickness = 0.002f)
        {
            sides = Mathf.Max(12, sides);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 cFront = Vector3.zero;
            Vector3 cBack = new Vector3(0f, 0f, thickness);
            Vector3 PtF(int i)
            {
                float a = (2f * Mathf.PI * (i % sides)) / sides;
                return new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), 0f);
            }
            Vector3 PtB(int i)
            {
                Vector3 f = PtF(i);
                return new Vector3(f.x, f.y, thickness);
            }
            Vector2 DialUV(Vector3 p)
            {
                return new Vector2(0.5f + p.x / (2f * radius), 0.5f + p.y / (2f * radius));
            }
            void AddDialTri(Vector3 a, Vector3 b, Vector3 c)
            {
                int idx = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                uvs.Add(DialUV(a)); uvs.Add(DialUV(b)); uvs.Add(DialUV(c));
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            }

            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;
                AddDialTri(cFront, PtF(inext), PtF(i));
                AddDialTri(cBack, PtB(i), PtB(inext));
                AddFlatQuad(verts, tris, uvs, PtF(i), PtF(inext), PtB(inext), PtB(i));
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private static void BuildAnalogClockDialAssembly(
            Transform parent,
            Vector3 localPos,
            float dialRadius,
            GreyboxPalette pal,
            out Transform hourPivot,
            out Transform minutePivot,
            out Transform secondPivot,
            float yRotDegrees = 0f)
        {
            GameObject dialGroup = CreateContainer(parent, "Grupo_Mostrador_Analogico");
            dialGroup.transform.localPosition = localPos;
            dialGroup.transform.localRotation = Quaternion.Euler(0f, yRotDegrees, 0f);

            // Disco circular fechado mapeado UV [0..1] encaixado perfeitamente dentro do aro vazado de latão
            Material dialMat = pal.details.analogClockDial != null ? pal.details.analogClockDial : pal.paperWhite;
            Mesh dialDiscMesh = CreateClosedClockDialMesh($"Mesh_ClockDial_{Mathf.RoundToInt(dialRadius * 1000f)}", 20, dialRadius * 0.99f, 0.002f);
            CreateMeshObject(dialGroup.transform, "Impressao_Mostrador_Relogio", dialDiscMesh, dialMat, Vector3.zero, castShadows: false);

            Mesh bezelMesh = CreateClosedRingTubeMesh(
                $"Mesh_ClockBezel_{Mathf.RoundToInt(dialRadius * 1000f)}",
                16,
                6,
                dialRadius,
                Mathf.Max(0.004f, dialRadius * 0.11f),
                Mathf.Max(0.004f, dialRadius * 0.10f));
            CreateMeshObject(dialGroup.transform, "Aro_Latao", bezelMesh, pal.metalBrass, new Vector3(0f, 0f, -0.003f), new Vector3(90f, 0f, 0f), castShadows: false);

            // Ponteiro das horas (+Y = 12h quando o disco aponta para -Z)
            hourPivot = CreateContainer(dialGroup.transform, "Pivo_Ponteiro_Hora").transform;
            hourPivot.localPosition = new Vector3(0f, 0f, -0.0022f);
            float hTip = dialRadius * 0.56f;
            float hTail = dialRadius * 0.14f;
            float hW = Mathf.Max(0.0020f, dialRadius * 0.068f);
            Mesh hourMesh = CreateClosedPolygonLoftMesh(
                $"Mesh_ClockHandH_{Mathf.RoundToInt(dialRadius * 1000f)}",
                new[]
                {
                    new Vector2(0f, -hTail),
                    new Vector2(hW, hTail * 0.35f),
                    new Vector2(hW * 0.42f, hTip * 0.85f),
                    new Vector2(0f, hTip),
                    new Vector2(-hW * 0.42f, hTip * 0.85f),
                    new Vector2(-hW, hTail * 0.35f)
                },
                new[] { (-0.0007f, 1f, 1f, 0f, 0f), (0.0007f, 1f, 1f, 0f, 0f) });
            CreateMeshObject(hourPivot, "Ponteiro_Hora", hourMesh, pal.shadowBlack, Vector3.zero, new Vector3(-90f, 0f, 0f), castShadows: false);

            minutePivot = CreateContainer(dialGroup.transform, "Pivo_Ponteiro_Minuto").transform;
            minutePivot.localPosition = new Vector3(0f, 0f, -0.0038f);
            float mTip = dialRadius * 0.80f;
            float mTail = dialRadius * 0.18f;
            float mW = Mathf.Max(0.0015f, dialRadius * 0.048f);
            Mesh minMesh = CreateClosedPolygonLoftMesh(
                $"Mesh_ClockHandM_{Mathf.RoundToInt(dialRadius * 1000f)}",
                new[]
                {
                    new Vector2(0f, -mTail),
                    new Vector2(mW, mTail * 0.25f),
                    new Vector2(mW * 0.38f, mTip * 0.88f),
                    new Vector2(0f, mTip),
                    new Vector2(-mW * 0.38f, mTip * 0.88f),
                    new Vector2(-mW, mTail * 0.25f)
                },
                new[] { (-0.0006f, 1f, 1f, 0f, 0f), (0.0006f, 1f, 1f, 0f, 0f) });
            CreateMeshObject(minutePivot, "Ponteiro_Minuto", minMesh, pal.shadowBlack, Vector3.zero, new Vector3(-90f, 0f, 0f), castShadows: false);

            secondPivot = CreateContainer(dialGroup.transform, "Pivo_Ponteiro_Segundo").transform;
            secondPivot.localPosition = new Vector3(0f, 0f, -0.0052f);
            float sTip = dialRadius * 0.86f;
            float sTail = dialRadius * 0.22f;
            float sW = Mathf.Max(0.0009f, dialRadius * 0.024f);
            Mesh secMesh = CreateClosedPolygonLoftMesh(
                $"Mesh_ClockHandS_{Mathf.RoundToInt(dialRadius * 1000f)}",
                new[]
                {
                    new Vector2(-sW, -sTail),
                    new Vector2(sW, -sTail),
                    new Vector2(sW * 0.5f, sTip),
                    new Vector2(-sW * 0.5f, sTip)
                },
                new[] { (-0.00045f, 1f, 1f, 0f, 0f), (0.00045f, 1f, 1f, 0f, 0f) });
            CreateMeshObject(secondPivot, "Ponteiro_Segundo", secMesh, pal.calendarRed, Vector3.zero, new Vector3(-90f, 0f, 0f), castShadows: false);

            CreateDetailCylinder(dialGroup.transform, "Pino_Central_Relogio", new Vector3(0f, 0f, -0.0062f),
                new Vector3(dialRadius * 0.14f, 0.0014f, dialRadius * 0.14f), pal.metalBrass, new Vector3(90f, 0f, 0f));

            // Vidro plano transparente protegendo mostrador e ponteiros (sem collider, sem sombras)
            CreateDetailCylinder(dialGroup.transform, "Vidro_Mostrador_Relogio", new Vector3(0f, 0f, -0.0080f),
                new Vector3(dialRadius * 1.94f, 0.0008f, dialRadius * 1.94f), pal.details.clearGlass, new Vector3(90f, 0f, 0f));
        }

        private static Mesh CreateAngularHeadMesh(string assetName, float rx, float ry, float rz)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 top = new Vector3(0f, ry, -rz * 0.08f);
            Vector3 chin = new Vector3(0f, -ry, rz * 0.18f);
            Vector3[] ring =
            {
                new Vector3(0f, 0.08f * ry, rz),
                new Vector3(-rx * 0.85f, 0.05f * ry, rz * 0.45f),
                new Vector3(-rx * 0.72f, 0.12f * ry, -rz * 0.75f),
                new Vector3(0f, 0.16f * ry, -rz),
                new Vector3(rx * 0.72f, 0.12f * ry, -rz * 0.75f),
                new Vector3(rx * 0.85f, 0.05f * ry, rz * 0.45f)
            };
            for (int i = 0; i < ring.Length; i++)
            {
                int inext = (i + 1) % ring.Length;
                AddFlatTri(verts, tris, uvs, top, ring[inext], ring[i]);
                AddFlatTri(verts, tris, uvs, chin, ring[i], ring[inext]);
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private enum SilhouetteArchetype
        {
            Margaret,
            SlenderWatcher,
            ShadowDash,
            SlumpedCian,
            ChildWatcher
        }

        private static Mesh CreateLoftedEllipticalMesh(
            string assetName, int sides, (float y, float cx, float cz, float rx, float rz)[] rings)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 RingPt(int k, int i)
            {
                float ang = (2f * Mathf.PI * (i % sides)) / sides;
                var r = rings[k];
                return new Vector3(
                    r.cx + r.rx * Mathf.Cos(ang),
                    r.y,
                    r.cz + r.rz * Mathf.Sin(ang));
            }

            Vector3 botCenter = new Vector3(rings[0].cx, rings[0].y, rings[0].cz);
            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;
                AddFlatTri(verts, tris, uvs, botCenter, RingPt(0, i), RingPt(0, inext));
            }

            for (int k = 0; k < rings.Length - 1; k++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int inext = (i + 1) % sides;
                    AddFlatQuad(verts, tris, uvs, RingPt(k, i), RingPt(k + 1, i), RingPt(k + 1, inext), RingPt(k, inext));
                }
            }

            int last = rings.Length - 1;
            Vector3 topCenter = new Vector3(rings[last].cx, rings[last].y, rings[last].cz);
            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;
                AddFlatTri(verts, tris, uvs, topCenter, RingPt(last, inext), RingPt(last, i));
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private static Mesh CreateSteppedBezelFrameMesh(
            string assetName,
            float innerHalfW, float innerHalfH,
            float stepHalfW, float stepHalfH,
            float outerHalfW, float outerHalfH,
            float zBack, float zScreenPlane, float zStep, float zFront)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector2[] signs =
            {
                new Vector2(1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(-1f, -1f),
                new Vector2(1f, -1f)
            };

            (float hw, float hh, float z)[] profile =
            {
                (outerHalfW, outerHalfH, zFront),
                (outerHalfW, outerHalfH, zBack),
                (innerHalfW, innerHalfH, zBack),
                (innerHalfW, innerHalfH, zScreenPlane),
                (stepHalfW,  stepHalfH,  zStep),
                (stepHalfW,  stepHalfH,  zFront)
            };

            Vector3 CornerPt(int c, int p)
            {
                Vector2 s = signs[c % 4];
                var prof = profile[p % profile.Length];
                return new Vector3(s.x * prof.hw, s.y * prof.hh, prof.z);
            }

            for (int c = 0; c < 4; c++)
            {
                int cnext = (c + 1) % 4;
                for (int p = 0; p < profile.Length; p++)
                {
                    int pnext = (p + 1) % profile.Length;
                    AddFlatQuad(verts, tris, uvs,
                        CornerPt(c, p), CornerPt(c, pnext), CornerPt(cnext, pnext), CornerPt(cnext, p));
                }
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        private static Mesh CreatePolylineCordMesh(string assetName, Vector3[] worldPts, float radius, int sides = 6)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            Vector3 origin = worldPts[0];
            int n = worldPts.Length;
            Vector3[] tangents = new Vector3[n];
            for (int k = 0; k < n; k++)
            {
                if (k == 0)
                    tangents[k] = (worldPts[1] - worldPts[0]).normalized;
                else if (k == n - 1)
                    tangents[k] = (worldPts[n - 1] - worldPts[n - 2]).normalized;
                else
                {
                    Vector3 tPrev = (worldPts[k] - worldPts[k - 1]).normalized;
                    Vector3 tNext = (worldPts[k + 1] - worldPts[k]).normalized;
                    Vector3 avg = tPrev + tNext;
                    tangents[k] = avg.sqrMagnitude > 1e-5f ? avg.normalized : tNext;
                }
            }

            Vector3[] uAxes = new Vector3[n];
            Vector3[] vAxes = new Vector3[n];

            Vector3 seedUp = Mathf.Abs(Vector3.Dot(tangents[0], Vector3.up)) < 0.92f ? Vector3.up : Vector3.forward;
            uAxes[0] = Vector3.Cross(seedUp, tangents[0]).normalized;
            vAxes[0] = Vector3.Cross(tangents[0], uAxes[0]).normalized;

            for (int k = 1; k < n; k++)
            {
                Quaternion rot = Quaternion.FromToRotation(tangents[k - 1], tangents[k]);
                uAxes[k] = (rot * uAxes[k - 1]).normalized;
                uAxes[k] = (uAxes[k] - tangents[k] * Vector3.Dot(uAxes[k], tangents[k])).normalized;
                vAxes[k] = Vector3.Cross(tangents[k], uAxes[k]).normalized;
            }

            Vector3 RingPt(int k, int i)
            {
                float ang = (2f * Mathf.PI * (i % sides)) / sides;
                Vector3 center = worldPts[k] - origin;
                return center + radius * (Mathf.Cos(ang) * uAxes[k] + Mathf.Sin(ang) * vAxes[k]);
            }

            Vector3 cStart = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;
                AddFlatTri(verts, tris, uvs, cStart, RingPt(0, inext), RingPt(0, i));
            }

            for (int k = 0; k < n - 1; k++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int inext = (i + 1) % sides;
                    AddFlatQuad(verts, tris, uvs, RingPt(k, i), RingPt(k, inext), RingPt(k + 1, inext), RingPt(k + 1, i));
                }
            }

            int last = n - 1;
            Vector3 cEnd = worldPts[last] - origin;
            for (int i = 0; i < sides; i++)
            {
                int inext = (i + 1) % sides;
                AddFlatTri(verts, tris, uvs, cEnd, RingPt(last, i), RingPt(last, inext));
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }
        private static GameObject CreateDetailBox(
            Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Vector3 localEuler = default, bool castShadows = false)
        {
            GameObject go = CreateBox(parent, name, localPos, localScale, mat, hasCollider: false, localEuler: localEuler);
            if (!castShadows && go.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
            {
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        private static GameObject CreateDetailCylinder(
            Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Vector3 localEuler = default, bool castShadows = false)
        {
            GameObject go = CreateCylinder(parent, name, localPos, localScale, mat, hasCollider: false, localEuler: localEuler);
            if (!castShadows && go.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
            {
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        private static GameObject CreateDetailSphere(
            Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Vector3 localEuler = default, bool castShadows = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrBuildLowPolyIcosphere1Mesh();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            AssignMaterial(go, mat);
            if (!castShadows)
            {
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        private static void DecoratePanelDoorLeaf(
            Transform doorPivot,
            GreyboxPalette pal,
            bool includeKeyhole = true,
            float leafWidth = 0.80f,
            float leafHeight = 2.092f,
            float leafCenterY = 1.050f,
            float leafThickness = 0.048f)
        {
            GameObject decor = CreateContainer(doorPivot, "Detalhes_Folha_Porta");
            decor.transform.localPosition = new Vector3(leafWidth * 0.5f, leafCenterY, 0f);

            float halfZ = leafThickness * 0.5f;
            float frameW = Mathf.Clamp(leafWidth - 0.22f, 0.36f, 0.72f);
            float railBar = 0.024f;
            float panelW = Mathf.Max(0.26f, frameW - railBar * 2f - 0.012f);
            float supH = leafHeight * 0.365f;
            float infH = leafHeight * 0.345f;
            float supY = leafHeight * 0.21f;
            float infY = -leafHeight * 0.225f;
            float handleX = leafWidth * 0.5f - 0.078f;
            float supInnerH = supH - railBar * 2f - 0.014f;
            float infInnerH = infH - railBar * 2f - 0.014f;

            string doorMeshTag = Mathf.RoundToInt(leafWidth * 1000f).ToString();
            Mesh panelSupMesh = CreateChamferedRectLoftMesh($"Mesh_DoorPanelSup_{doorMeshTag}", panelW, 0.008f, supInnerH, 0.012f, 0.82f, 0.004f);
            Mesh panelInfMesh = CreateChamferedRectLoftMesh($"Mesh_DoorPanelInf_{doorMeshTag}", panelW, 0.008f, infInnerH, 0.012f, 0.82f, 0.004f);

            float[] signs = { -1f, 1f };
            for (int s = 0; s < signs.Length; s++)
            {
                float sign = signs[s];
                string tag = s == 0 ? "A" : "B";
                float zFrame = sign * (halfZ + 0.0045f);
                float zRail = sign * (halfZ + 0.0040f);

                // Baguetes vazadas escalonadas ao redor de cada almofada chanfrada (sem sobrepor o centro)
                BuildOpenFrameTrimBars(decor.transform, $"Moldura_Sup_{tag}", new Vector3(0f, supY, zFrame), frameW, supH, railBar, 0.009f, pal.baseboardWood);
                CreateMeshObject(decor.transform, $"Painel_Sup_{tag}", panelSupMesh, pal.doorWood,
                    new Vector3(0f, supY, sign * halfZ), new Vector3(sign > 0f ? 90f : -90f, 0f, 0f), castShadows: false);

                BuildOpenFrameTrimBars(decor.transform, $"Moldura_Inf_{tag}", new Vector3(0f, infY, zFrame), frameW, infH, railBar, 0.009f, pal.baseboardWood);
                CreateMeshObject(decor.transform, $"Painel_Inf_{tag}", panelInfMesh, pal.doorWood,
                    new Vector3(0f, infY, sign * halfZ), new Vector3(sign > 0f ? 90f : -90f, 0f, 0f), castShadows: false);

                // Barra de reforço central e roda-pé metálico inferior discreto
                CreateDetailBox(decor.transform, $"Travessa_Reforco_{tag}", new Vector3(0f, -0.005f, zRail), new Vector3(panelW + 0.03f, 0.044f, 0.006f), pal.baseboardWood);
                CreateDetailBox(decor.transform, $"Barra_Reforco_Base_{tag}", new Vector3(0f, -leafHeight * 0.5f + 0.062f, zRail), new Vector3(leafWidth - 0.06f, 0.095f, 0.004f), pal.metalDark);

                // Espelho de fechadura em latão escalonado + miolo limpo
                CreateDetailBox(decor.transform, $"Espelho_Fechadura_{tag}", new Vector3(handleX, -0.056f, sign * (halfZ + 0.003f)), new Vector3(0.044f, 0.168f, 0.006f), pal.metalBrass);
                CreateDetailBox(decor.transform, $"Miolo_Espelho_{tag}", new Vector3(handleX, -0.056f, sign * (halfZ + 0.0075f)), new Vector3(0.032f, 0.146f, 0.003f), pal.woodMahogany);
                CreateDetailCylinder(decor.transform, $"Parafuso_Esp_Sup_{tag}", new Vector3(handleX, 0.015f, sign * (halfZ + 0.0095f)), new Vector3(0.006f, 0.0015f, 0.006f), pal.metalDark, new Vector3(90f, 0f, 0f));
                CreateDetailCylinder(decor.transform, $"Parafuso_Esp_Inf_{tag}", new Vector3(handleX, -0.126f, sign * (halfZ + 0.0095f)), new Vector3(0.006f, 0.0015f, 0.006f), pal.metalDark, new Vector3(90f, 0f, 0f));

                // Roseta, pescoço e alavanca L pivotada no eixo para animar no clique
                CreateDetailCylinder(decor.transform, $"Roseta_Macaneta_{tag}", new Vector3(handleX, -0.022f, sign * (halfZ + 0.0105f)), new Vector3(0.036f, 0.003f, 0.036f), pal.metalBrass, new Vector3(90f, 0f, 0f));
                CreateDetailCylinder(decor.transform, $"Eixo_Macaneta_{tag}", new Vector3(handleX, -0.022f, sign * (halfZ + 0.022f)), new Vector3(0.018f, 0.014f, 0.018f), pal.metalBrass, new Vector3(90f, 0f, 0f));

                GameObject leverPivot = CreateContainer(decor.transform, $"Alavanca_Pivo_{tag}");
                leverPivot.transform.localPosition = new Vector3(handleX, -0.022f, sign * (halfZ + 0.039f));
                CreateDetailBox(leverPivot.transform, $"Alavanca_Macaneta_{tag}", new Vector3(-0.048f, 0f, 0f), new Vector3(0.114f, 0.015f, 0.014f), pal.metalBrass);
                CreateDetailBox(leverPivot.transform, $"Ponta_Retorno_Macaneta_{tag}", new Vector3(-0.098f, 0f, -sign * 0.013f), new Vector3(0.010f, 0.012f, 0.012f), pal.metalBrass);

                if (includeKeyhole)
                {
                    CreateDetailCylinder(decor.transform, $"Miolo_Chave_{tag}", new Vector3(handleX, -0.094f, sign * (halfZ + 0.010f)), new Vector3(0.018f, 0.0022f, 0.018f), pal.metalBrass, new Vector3(90f, 0f, 0f));
                    CreateDetailBox(decor.transform, $"Fenda_Chave_{tag}", new Vector3(handleX, -0.096f, sign * (halfZ + 0.013f)), new Vector3(0.004f, 0.013f, 0.002f), pal.shadowBlack);
                }
            }

            float hingeLocalX = -leafWidth * 0.5f + 0.004f;
            float[] hingeYs = { leafHeight * 0.36f, 0.0f, -leafHeight * 0.36f };
            for (int i = 0; i < hingeYs.Length; i++)
            {
                CreateDetailCylinder(decor.transform, $"Dobradica_{i}", new Vector3(hingeLocalX, hingeYs[i], 0f), new Vector3(0.016f, 0.042f, 0.016f), pal.stainlessSteel);
                CreateDetailCylinder(decor.transform, $"Pino_Dobradica_Sup_{i}", new Vector3(hingeLocalX, hingeYs[i] + 0.045f, 0f), new Vector3(0.020f, 0.004f, 0.020f), pal.metalBrass);
                CreateDetailCylinder(decor.transform, $"Pino_Dobradica_Inf_{i}", new Vector3(hingeLocalX, hingeYs[i] - 0.045f, 0f), new Vector3(0.020f, 0.004f, 0.020f), pal.metalBrass);
            }
        }

        private static void BuildOpenFrameTrimBars(
            Transform parent,
            string prefix,
            Vector3 center,
            float width,
            float height,
            float barWidth,
            float depth,
            Material mat)
        {
            float innerH = Mathf.Max(0.04f, height - barWidth * 2f);
            CreateDetailBox(parent, $"{prefix}_Sup", new Vector3(center.x, center.y + (height - barWidth) * 0.5f, center.z), new Vector3(width, barWidth, depth), mat);
            CreateDetailBox(parent, $"{prefix}_Inf", new Vector3(center.x, center.y - (height - barWidth) * 0.5f, center.z), new Vector3(width, barWidth, depth), mat);
            CreateDetailBox(parent, $"{prefix}_Esq", new Vector3(center.x - (width - barWidth) * 0.5f, center.y, center.z), new Vector3(barWidth, innerH, depth), mat);
            CreateDetailBox(parent, $"{prefix}_Dir", new Vector3(center.x + (width - barWidth) * 0.5f, center.y, center.z), new Vector3(barWidth, innerH, depth), mat);
        }

        private static void BuildOpenFrameTrimBarsZ(
            Transform parent,
            string prefix,
            Vector3 center,
            float widthZ,
            float height,
            float barWidth,
            float depthX,
            Material mat)
        {
            float innerH = Mathf.Max(0.008f, height - barWidth * 2f);
            CreateDetailBox(parent, $"{prefix}_Sup", new Vector3(center.x, center.y + (height - barWidth) * 0.5f, center.z), new Vector3(depthX, barWidth, widthZ), mat);
            CreateDetailBox(parent, $"{prefix}_Inf", new Vector3(center.x, center.y - (height - barWidth) * 0.5f, center.z), new Vector3(depthX, barWidth, widthZ), mat);
            CreateDetailBox(parent, $"{prefix}_Sul", new Vector3(center.x, center.y, center.z - (widthZ - barWidth) * 0.5f), new Vector3(depthX, innerH, barWidth), mat);
            CreateDetailBox(parent, $"{prefix}_Norte", new Vector3(center.x, center.y, center.z + (widthZ - barWidth) * 0.5f), new Vector3(depthX, innerH, barWidth), mat);
        }

        private static void BuildOpenFrameTrimBarsXZ(
            Transform parent,
            string prefix,
            Vector3 center,
            float widthX,
            float depthZ,
            float barWidth,
            float heightY,
            Material mat,
            bool castShadows = false)
        {
            float innerD = Mathf.Max(0.006f, depthZ - barWidth * 2f);
            CreateDetailBox(parent, $"{prefix}_N", new Vector3(center.x, center.y, center.z + (depthZ - barWidth) * 0.5f), new Vector3(widthX, heightY, barWidth), mat, castShadows: castShadows);
            CreateDetailBox(parent, $"{prefix}_S", new Vector3(center.x, center.y, center.z - (depthZ - barWidth) * 0.5f), new Vector3(widthX, heightY, barWidth), mat, castShadows: castShadows);
            CreateDetailBox(parent, $"{prefix}_O", new Vector3(center.x - (widthX - barWidth) * 0.5f, center.y, center.z), new Vector3(barWidth, heightY, innerD), mat, castShadows: castShadows);
            CreateDetailBox(parent, $"{prefix}_L", new Vector3(center.x + (widthX - barWidth) * 0.5f, center.y, center.z), new Vector3(barWidth, heightY, innerD), mat, castShadows: castShadows);
        }

        private static GameObject BuildDetailedBallpointPen(
            Transform parent, string name, Vector3 surfacePos, float yaw, Material barrelMat, GreyboxPalette pal)
        {
            GameObject pen = CreateContainer(parent, name);
            pen.transform.localPosition = surfacePos;
            pen.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const float r = 0.0044f;
            CreateDetailCylinder(pen.transform, "Corpo_Caneta", new Vector3(0f, r, -0.008f), new Vector3(r * 2f, 0.056f, r * 2f), barrelMat, new Vector3(90f, 0f, 0f));
            Mesh tipMesh = CreateFacetedLatheVesselMesh("Mesh_Ponta_Caneta_Esfero", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, r),
                (0.015f, 0.0014f),
                (0.020f, 0.0007f),
                (0.020f, 0.000f)
            });
            CreateMeshObject(pen.transform, "Ponta_Cone_Caneta", tipMesh, pal.metalBrass, new Vector3(0f, r, 0.048f), new Vector3(90f, 0f, 0f), castShadows: false);
            CreateDetailCylinder(pen.transform, "Anel_Caneta", new Vector3(0f, r, 0.046f), new Vector3(r * 2.1f, 0.002f, r * 2.1f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(pen.transform, "Botao_Clique_Caneta", new Vector3(0f, r, -0.068f), new Vector3(0.0064f, 0.005f, 0.0064f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailBox(pen.transform, "Base_Clipe_Caneta", new Vector3(0f, r + 0.0038f, -0.054f), new Vector3(0.0024f, 0.0032f, 0.004f), pal.stainlessSteel);
            CreateDetailBox(pen.transform, "Clipe_Bolso_Caneta", new Vector3(0f, r + 0.0054f, -0.036f), new Vector3(0.0024f, 0.0020f, 0.036f), pal.stainlessSteel);
            return pen;
        }

        private static void BuildWallLightSwitch(
            Transform parent, string name, Vector3 pos, float yaw, GreyboxPalette pal, bool doubleGang = false)
        {
            GameObject sw = CreateContainer(parent, name);
            sw.transform.localPosition = pos;
            sw.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateDetailBox(sw.transform, "Moldura_Base_90s", new Vector3(0f, 0f, 0.0025f), new Vector3(0.084f, 0.124f, 0.005f), pal.formicaCream);
            CreateDetailBox(sw.transform, "Espelho_90s", new Vector3(0f, 0f, 0.0080f), new Vector3(0.076f, 0.114f, 0.006f), pal.plasticBeige90s);
            CreateDetailCylinder(sw.transform, "Parafuso_Sup", new Vector3(0f, 0.044f, 0.0118f), new Vector3(0.006f, 0.0016f, 0.006f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(sw.transform, "Parafuso_Inf", new Vector3(0f, -0.044f, 0.0118f), new Vector3(0.006f, 0.0016f, 0.006f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            if (doubleGang)
            {
                CreateDetailBox(sw.transform, "Tecla_1", new Vector3(0f, 0.016f, 0.013f), new Vector3(0.036f, 0.018f, 0.008f), pal.porcelainWhite, new Vector3(-8f, 0f, 0f));
                CreateDetailBox(sw.transform, "Tecla_2", new Vector3(0f, -0.016f, 0.013f), new Vector3(0.036f, 0.018f, 0.008f), pal.porcelainWhite, new Vector3(8f, 0f, 0f));
            }
            else
            {
                CreateDetailBox(sw.transform, "Tecla", new Vector3(0f, 0f, 0.013f), new Vector3(0.036f, 0.024f, 0.008f), pal.porcelainWhite, new Vector3(-8f, 0f, 0f));
            }
        }

        private static void BuildWallPowerOutlet(
            Transform parent, string name, Vector3 pos, float yaw, GreyboxPalette pal, bool hasPlugConnected = false, bool hasRoutedCable = true)
        {
            GameObject outlet = CreateContainer(parent, name);
            outlet.transform.localPosition = pos;
            outlet.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateDetailBox(outlet.transform, "Espelho_Tomada", new Vector3(0f, 0f, 0.0045f), new Vector3(0.078f, 0.116f, 0.009f), pal.plasticBeige90s);
            CreateDetailBox(outlet.transform, "Chanfro_Tomada", new Vector3(0f, 0f, 0.0102f), new Vector3(0.068f, 0.106f, 0.0024f), pal.plasticBeige90s);
            CreateDetailBox(outlet.transform, "Miolo_Tomada", new Vector3(0f, 0f, 0.0136f), new Vector3(0.042f, 0.028f, 0.0044f), pal.porcelainWhite);
            CreateDetailCylinder(outlet.transform, "Parafuso_Espelho_Sup", new Vector3(0f, 0.036f, 0.0118f), new Vector3(0.0045f, 0.0012f, 0.0045f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(outlet.transform, "Parafuso_Espelho_Inf", new Vector3(0f, -0.036f, 0.0118f), new Vector3(0.0045f, 0.0012f, 0.0045f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            if (hasPlugConnected)
            {
                CreateDetailBox(outlet.transform, "Plugue_Preto", new Vector3(0f, 0f, 0.0264f), new Vector3(0.032f, 0.024f, 0.0212f), pal.metalDark);
                CreateDetailBox(outlet.transform, "Colar_Plugue", new Vector3(0f, 0f, 0.0385f), new Vector3(0.020f, 0.016f, 0.003f), pal.shadowBlack);
                if (hasRoutedCable)
                {
                    CreateDetailCylinder(outlet.transform, "Bota_Plugue", new Vector3(0f, -0.018f, 0.0264f), new Vector3(0.012f, 0.008f, 0.012f), pal.metalDark);
                }
                else
                {
                    CreateDetailCylinder(outlet.transform, "Cabo_Descendo", new Vector3(0f, -0.10f, 0.0324f), new Vector3(0.008f, 0.095f, 0.008f), pal.metalDark);
                }
            }
            else
            {
                CreateDetailCylinder(outlet.transform, "Furo_Esq", new Vector3(-0.010f, 0f, 0.0162f), new Vector3(0.006f, 0.0012f, 0.006f), pal.shadowBlack, new Vector3(90f, 0f, 0f));
                CreateDetailCylinder(outlet.transform, "Furo_Dir", new Vector3(0.010f, 0f, 0.0162f), new Vector3(0.006f, 0.0012f, 0.006f), pal.shadowBlack, new Vector3(90f, 0f, 0f));
            }
        }

        private static void BuildTileWainscotingX(
            Transform parent, string name, float xMin, float xMax, float zWallFace, float height, GreyboxPalette pal)
        {
            float len = Mathf.Abs(xMax - xMin);
            if (len < 0.05f) return;
            float xCenter = (xMin + xMax) * 0.5f;
            GameObject root = CreateContainer(parent, name);

            // Azulejo encosta no rodapé (Y=0.099m), sobe até o filete decorativo e encosta no friso sem sobreposição em Y nem Z-fighting
            float bottomY = 0.099f;
            float frisoH = 0.022f;
            float fileteH = 0.014f;
            float fileteBottom = height - frisoH * 0.5f - fileteH;
            float tileH = Mathf.Max(0.10f, fileteBottom - bottomY);
            CreateDetailBox(root.transform, "Painel_Azulejo", new Vector3(xCenter, bottomY + tileH * 0.5f, zWallFace), new Vector3(len, tileH, 0.010f), pal.ceramicAzulejo);
            CreateDetailBox(root.transform, "Filete_Decorativo", new Vector3(xCenter, fileteBottom + fileteH * 0.5f, zWallFace), new Vector3(len, fileteH, 0.012f), pal.tileBorderBlue);
            CreateDetailBox(root.transform, "Friso_Superior", new Vector3(xCenter, height, zWallFace), new Vector3(len, frisoH, 0.014f), pal.baseboardWood);
        }

        private static void BuildTileWainscotingZ(
            Transform parent, string name, float xWallFace, float zMin, float zMax, float height, GreyboxPalette pal)
        {
            float len = Mathf.Abs(zMax - zMin);
            if (len < 0.05f) return;
            float zCenter = (zMin + zMax) * 0.5f;
            GameObject root = CreateContainer(parent, name);

            float bottomY = 0.099f;
            float frisoH = 0.022f;
            float fileteH = 0.014f;
            float fileteBottom = height - frisoH * 0.5f - fileteH;
            float tileH = Mathf.Max(0.10f, fileteBottom - bottomY);
            CreateDetailBox(root.transform, "Painel_Azulejo", new Vector3(xWallFace, bottomY + tileH * 0.5f, zCenter), new Vector3(0.010f, tileH, len), pal.ceramicAzulejo);
            CreateDetailBox(root.transform, "Filete_Decorativo", new Vector3(xWallFace, fileteBottom + fileteH * 0.5f, zCenter), new Vector3(0.012f, fileteH, len), pal.tileBorderBlue);
            CreateDetailBox(root.transform, "Friso_Superior", new Vector3(xWallFace, height, zCenter), new Vector3(0.014f, frisoH, len), pal.baseboardWood);
        }

        private static void BuildCrownMouldingRoom(
            Transform parent, string name, float xMin, float xMax, float zMin, float zMax, float ceilingY, Material mat)
        {
            GameObject grp = CreateContainer(parent, name);
            float y = ceilingY - 0.027f; // Topo em ceilingY + 0.001f (embutido 1mm no teto sem Z-fighting)
            float lenX = Mathf.Abs(xMax - xMin);
            float lenZ = Mathf.Max(0.04f, Mathf.Abs(zMax - zMin) - 0.082f); // Aparado nos 4 cantos para não sobrepor Norte/Sul
            float cx = (xMin + xMax) * 0.5f;
            float cz = (zMin + zMax) * 0.5f;

            CreateDetailBox(grp.transform, "Sanca_Norte", new Vector3(cx, y, zMax - 0.02f), new Vector3(lenX, 0.056f, 0.04f), mat);
            CreateDetailBox(grp.transform, "Sanca_Sul", new Vector3(cx, y, zMin + 0.02f), new Vector3(lenX, 0.056f, 0.04f), mat);
            CreateDetailBox(grp.transform, "Sanca_Oeste", new Vector3(xMin + 0.02f, y, cz), new Vector3(0.04f, 0.056f, lenZ), mat);
            CreateDetailBox(grp.transform, "Sanca_Leste", new Vector3(xMax - 0.02f, y, cz), new Vector3(0.04f, 0.056f, lenZ), mat);
        }

        private static void BuildFloorDrain(Transform parent, string name, Vector3 floorPos, GreyboxPalette pal)
        {
            GameObject dr = CreateContainer(parent, name);
            dr.transform.localPosition = floorPos;
            // Aro quadrado em 4 barras sem Z-fighting, fundo recuado e grelha nivelada
            CreateDetailBox(dr.transform, "Moldura_Inox_N", new Vector3(0f, 0.0020f,  0.057f), new Vector3(0.132f, 0.004f, 0.018f), pal.stainlessSteel);
            CreateDetailBox(dr.transform, "Moldura_Inox_S", new Vector3(0f, 0.0020f, -0.057f), new Vector3(0.132f, 0.004f, 0.018f), pal.stainlessSteel);
            CreateDetailBox(dr.transform, "Moldura_Inox_O", new Vector3(-0.057f, 0.0020f, 0f), new Vector3(0.018f, 0.004f, 0.096f), pal.stainlessSteel);
            CreateDetailBox(dr.transform, "Moldura_Inox_L", new Vector3( 0.057f, 0.0020f, 0f), new Vector3(0.018f, 0.004f, 0.096f), pal.stainlessSteel);
            CreateDetailCylinder(dr.transform, "Fundo_Escuro_Ralo", new Vector3(0f, 0.0010f, 0f), new Vector3(0.094f, 0.0010f, 0.094f), pal.shadowBlack);
            for (int i = -1; i <= 1; i++)
            {
                CreateDetailBox(dr.transform, $"Grade_{i + 2}", new Vector3(0f, 0.0031f, i * 0.024f), new Vector3(0.094f, 0.0018f, 0.008f), pal.stainlessSteel);
            }
            CreateDetailCylinder(dr.transform, "Parafuso_Ralo_1", new Vector3(-0.056f, 0.0042f, 0f), new Vector3(0.006f, 0.0005f, 0.006f), pal.metalDark);
            CreateDetailCylinder(dr.transform, "Parafuso_Ralo_2", new Vector3( 0.056f, 0.0042f, 0f), new Vector3(0.006f, 0.0005f, 0.006f), pal.metalDark);
        }

        private static GameObject BuildDetailedRemoteControl(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject rc = CreateContainer(parent, name);
            rc.transform.localPosition = surfacePos;
            rc.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Material powerMat = pal.magnetCoral ?? pal.bookBurgundy ?? pal.calendarRed;
            Material chMat = pal.bookSlateBlue ?? pal.sofaFabric;
            Material volMat = pal.bookMustard ?? pal.metalBrass;
            Material vcrRecMat = pal.bookBurgundy ?? pal.calendarRed;
            Material vcrPlayMat = pal.bookTeal ?? pal.sofaFabric;
            Material muteMat = pal.bookSage ?? pal.sofaFabric;

            Mesh rcBodyMesh = CreateChamferedRectLoftMesh("Mesh_Controle_Remoto_90s", 0.056f, 0.018f, 0.184f, 0.008f, 0.90f, 0.004f);
            CreateMeshObject(rc.transform, "Corpo_Controle", rcBodyMesh, pal.metalDark, Vector3.zero, castShadows: true);
            CreateDetailBox(rc.transform, "Chanfro_Frontal", new Vector3(0f, 0.013f, 0.088f), new Vector3(0.044f, 0.010f, 0.010f), pal.shadowBlack);
            CreateDetailBox(rc.transform, "Painel_Superior", new Vector3(0f, 0.0195f, 0f), new Vector3(0.048f, 0.0028f, 0.166f), pal.shadowBlack);
            CreateDetailBox(rc.transform, "Friso_Lat_Controle_E", new Vector3(-0.0282f, 0.009f, -0.010f), new Vector3(0.0016f, 0.004f, 0.140f), pal.shadowBlack);
            CreateDetailBox(rc.transform, "Friso_Lat_Controle_D", new Vector3( 0.0282f, 0.009f, -0.010f), new Vector3(0.0016f, 0.004f, 0.140f), pal.shadowBlack);
            CreateDetailBox(rc.transform, "Botao_Power_Vermelho", new Vector3(-0.015f, 0.0235f, 0.068f), new Vector3(0.013f, 0.0052f, 0.011f), powerMat);
            CreateDetailBox(rc.transform, "Botao_TV_AV", new Vector3(0.003f, 0.0232f, 0.068f), new Vector3(0.011f, 0.0048f, 0.010f), chMat);
            CreateDetailBox(rc.transform, "Botao_Mute", new Vector3(0.017f, 0.0232f, 0.068f), new Vector3(0.010f, 0.0048f, 0.010f), muteMat);
            CreateDetailCylinder(rc.transform, "LED_Infravermelho", new Vector3(0f, 0.012f, 0.094f), new Vector3(0.008f, 0.003f, 0.008f), pal.details.clearGlass, new Vector3(90f, 0f, 0f));

            for (int r = 0; r < 4; r++)
            {
                for (int c = -1; c <= 1; c++)
                {
                    CreateDetailBox(rc.transform, $"Btn_{r}_{c + 1}",
                        new Vector3(c * 0.013f, 0.0231f, 0.044f - r * 0.016f),
                        new Vector3(0.009f, 0.0046f, 0.0095f), pal.plasticBeige90s);
                }
            }

            CreateDetailBox(rc.transform, "Tecla_Canal", new Vector3(-0.013f, 0.0238f, -0.028f), new Vector3(0.015f, 0.0056f, 0.028f), chMat);
            CreateDetailBox(rc.transform, "Tecla_Volume", new Vector3(0.013f, 0.0238f, -0.028f), new Vector3(0.015f, 0.0056f, 0.028f), volMat);

            // Fileira inferior de teclas VCR (Play / Stop / Rec coloridas dessaturadas)
            CreateDetailBox(rc.transform, "Tecla_VCR_Play", new Vector3(-0.014f, 0.0232f, -0.064f), new Vector3(0.011f, 0.0046f, 0.012f), vcrPlayMat);
            CreateDetailBox(rc.transform, "Tecla_VCR_Stop", new Vector3(0.000f, 0.0232f, -0.064f), new Vector3(0.011f, 0.0046f, 0.012f), pal.plasticBeige90s);
            CreateDetailBox(rc.transform, "Tecla_VCR_Rec", new Vector3(0.014f, 0.0232f, -0.064f), new Vector3(0.011f, 0.0046f, 0.012f), vcrRecMat);

            return rc;
        }

        private static GameObject BuildDetailedCoffeeMug(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal, bool withSaucer = false)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = surfacePos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float mugBottomY = 0f;
            if (withSaucer)
            {
                Mesh saucerMesh = CreateFacetedLatheVesselMesh("Mesh_CoffeeSaucer", 12, new[]
                {
                    (0.000f, 0.000f),
                    (0.000f, 0.046f),
                    (0.005f, 0.056f),
                    (0.015f, 0.074f),
                    (0.016f, 0.071f),
                    (0.006f, 0.050f),
                    (0.006f, 0.000f)
                });
                CreateMeshObject(root.transform, "Pires_Caneca", saucerMesh, pal.porcelainWhite, Vector3.zero, castShadows: true);
                mugBottomY = 0.0060f;
            }

            Mesh mugMesh = CreateFacetedLatheVesselMesh("Mesh_CoffeeMug", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.032f),
                (0.005f, 0.038f),
                (0.010f, 0.039f),
                (0.088f, 0.040f),
                (0.090f, 0.038f),
                (0.090f, 0.034f),
                (0.087f, 0.032f),
                (0.014f, 0.030f),
                (0.014f, 0.000f)
            });
            CreateMeshObject(root.transform, "Corpo_Caneca_Mesh", mugMesh, pal.porcelainWhite, new Vector3(0f, mugBottomY, 0f), castShadows: true);

            Material coffeeMat = pal.coffeeDark != null ? pal.coffeeDark : pal.baseboardWood;
            CreateDetailCylinder(root.transform, "Cafe_Seco_Interno", new Vector3(0f, mugBottomY + 0.056f, 0f), new Vector3(0.058f, 0.0015f, 0.058f), coffeeMat);

            Mesh handleMesh = CreateFacetedCShapeHandleMesh("Mesh_CoffeeMugHandle", new[]
            {
                new Vector3(0.036f, 0.071f, 0f),
                new Vector3(0.053f, 0.069f, 0f),
                new Vector3(0.061f, 0.049f, 0f),
                new Vector3(0.053f, 0.029f, 0f),
                new Vector3(0.036f, 0.027f, 0f)
            }, 0.0045f, 0.0055f);
            CreateMeshObject(root.transform, "Alca_Caneca_Mesh", handleMesh, pal.porcelainWhite, new Vector3(0f, mugBottomY, 0f), castShadows: true);

            return root;
        }

        private static GameObject BuildDrinkingGlass(
            Transform parent, string name, Vector3 surfacePos, GreyboxPalette pal, bool upsideDown = false, bool hasWaterLeft = false)
        {
            GameObject gl = CreateContainer(parent, name);
            gl.transform.localPosition = surfacePos;
            if (upsideDown)
            {
                Mesh downMesh = CreateFacetedLatheVesselMesh("Mesh_DrinkingGlass_Down", 12, new[]
                {
                    (0.000f, 0.030f),
                    (0.000f, 0.034f),
                    (0.084f, 0.030f),
                    (0.096f, 0.028f),
                    (0.096f, 0.000f),
                    (0.082f, 0.000f),
                    (0.082f, 0.025f),
                    (0.000f, 0.030f)
                });
                CreateMeshObject(gl.transform, "Copo_Vidro_Invertido", downMesh, pal.details.clearGlass, Vector3.zero, castShadows: false);
            }
            else
            {
                Mesh upMesh = CreateFacetedLatheVesselMesh("Mesh_DrinkingGlass_Up", 12, new[]
                {
                    (0.000f, 0.000f),
                    (0.000f, 0.028f),
                    (0.012f, 0.030f),
                    (0.096f, 0.034f),
                    (0.096f, 0.030f),
                    (0.016f, 0.025f),
                    (0.016f, 0.000f)
                });
                CreateMeshObject(gl.transform, "Copo_Vidro_Mesh", upMesh, pal.details.clearGlass, Vector3.zero, castShadows: false);
                if (hasWaterLeft)
                {
                    CreateDetailCylinder(gl.transform, "Agua_Restante", new Vector3(0f, 0.032f, 0f), new Vector3(0.050f, 0.002f, 0.050f), pal.details.clearGlass);
                }
            }
            return gl;
        }

        private static GameObject BuildDetailedKeyWithTag(
            Transform parent, string name, Vector3 surfacePos, float yaw, Material keyMat, Material tagMat, GreyboxPalette pal)
        {
            GameObject key = CreateContainer(parent, name);
            key.transform.localPosition = surfacePos;
            key.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (tagMat != null)
            {
                Mesh tagMesh = CreateChamferedRectLoftMesh("Mesh_Tag_Chaveiro", 0.038f, 0.007f, 0.022f, 0.0045f, 0.90f, 0.0014f);
                CreateMeshObject(key.transform, "Etiqueta_Fita", tagMesh, tagMat, new Vector3(-0.044f, 0f, 0.004f), new Vector3(0f, -10f, 0f), castShadows: true);
                CreateDetailBox(key.transform, "Visor_Etiqueta", new Vector3(-0.044f, 0.0082f, 0.004f), new Vector3(0.024f, 0.0022f, 0.013f), pal.paperWhite, new Vector3(0f, -10f, 0f));
                Mesh ringMesh = CreateClosedRingTubeMesh("Mesh_Argola_Chaveiro", 12, 6, 0.0095f, 0.0014f, 0.0014f);
                CreateMeshObject(key.transform, "Argola_Chaveiro", ringMesh, pal.stainlessSteel, new Vector3(-0.023f, 0.0035f, 0.002f), Vector3.zero, castShadows: false);
            }

            Mesh keyBowMesh = CreateChamferedRectLoftMesh("Mesh_Cabeca_Chave_Oitavada", 0.024f, 0.0052f, 0.022f, 0.006f, 0.88f, 0.0012f);
            CreateMeshObject(key.transform, "Cabeca_Chave", keyBowMesh, keyMat, new Vector3(-0.006f, 0.0008f, 0f), Vector3.zero, castShadows: true);
            CreateDetailCylinder(key.transform, "Furo_Cabeca", new Vector3(-0.011f, 0.0036f, 0f), new Vector3(0.0065f, 0.0030f, 0.0065f), pal.shadowBlack);
            CreateDetailBox(key.transform, "Haste_Chave", new Vector3(0.022f, 0.0034f, 0f), new Vector3(0.042f, 0.0028f, 0.007f), keyMat);
            CreateDetailBox(key.transform, "Friso_Haste", new Vector3(0.022f, 0.0052f, 0.001f), new Vector3(0.036f, 0.0012f, 0.0022f), pal.shadowBlack);
            CreateDetailBox(key.transform, "Dente_1", new Vector3(0.024f, 0.0034f, -0.0050f), new Vector3(0.005f, 0.0026f, 0.004f), keyMat);
            CreateDetailBox(key.transform, "Dente_2", new Vector3(0.032f, 0.0034f, -0.0058f), new Vector3(0.006f, 0.0026f, 0.0055f), keyMat);
            CreateDetailBox(key.transform, "Dente_3", new Vector3(0.040f, 0.0034f, -0.0045f), new Vector3(0.005f, 0.0026f, 0.004f), keyMat);

            return key;
        }

        private static GameObject BuildDetailedAABattery(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject bat = CreateContainer(parent, name);
            bat.transform.localPosition = surfacePos;
            bat.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float r = 0.009f;
            CreateCylinderLabelSleeve(bat.transform, "Corpo_Pilha_AA", new Vector3(0f, r, 0f), new Vector3(r * 2.02f, 0.046f, r * 2.02f), pal.details.aaBatteryWrap, pal, new Vector3(0f, 90f, 90f));
            CreateDetailCylinder(bat.transform, "Polo_Positivo", new Vector3(0f, r, 0.0255f), new Vector3(0.007f, 0.0025f, 0.007f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(bat.transform, "Polo_Negativo", new Vector3(0f, r, -0.0245f), new Vector3(0.013f, 0.0015f, 0.013f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            return bat;
        }

        private static GameObject BuildDetailedCaplessFlashlight(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject fl = CreateContainer(parent, name);
            fl.transform.localPosition = surfacePos;
            fl.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float axisY = 0.026f;
            CreateDetailCylinder(fl.transform, "Tubo_Metalico", new Vector3(0f, axisY, -0.01f), new Vector3(0.036f, 0.072f, 0.036f), pal.metalDark, new Vector3(90f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(fl.transform, "Anel_Punho_Borracha", new Vector3(0f, axisY, -0.015f), new Vector3(0.0385f, 0.032f, 0.0385f), pal.shadowBlack, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fl.transform, "Colar_Bocal", new Vector3(0f, axisY, 0.054f), new Vector3(0.044f, 0.008f, 0.044f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fl.transform, "Bocal_Refletor", new Vector3(0f, axisY, 0.074f), new Vector3(0.052f, 0.020f, 0.052f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fl.transform, "Aro_Bocal_Lanterna", new Vector3(0f, axisY, 0.093f), new Vector3(0.054f, 0.004f, 0.054f), pal.metalDark, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fl.transform, "Refletor_Interno", new Vector3(0f, axisY, 0.088f), new Vector3(0.040f, 0.002f, 0.040f), pal.paperWhite, new Vector3(90f, 0f, 0f));
            CreateDetailSphere(fl.transform, "Bulbo_Lanterna", new Vector3(0f, axisY, 0.089f), new Vector3(0.012f, 0.012f, 0.012f), pal.porcelainWhite, castShadows: false);
            CreateDetailCylinder(fl.transform, "Lente_Vidro", new Vector3(0f, axisY, 0.096f), new Vector3(0.044f, 0.002f, 0.044f), pal.details.clearGlass, new Vector3(90f, 0f, 0f));
            CreateDetailBox(fl.transform, "Chave_Deslizante", new Vector3(0f, axisY + 0.0195f, 0.028f), new Vector3(0.012f, 0.006f, 0.024f), pal.calendarRed);
            CreateDetailCylinder(fl.transform, "Aro_Cavidade_Lanterna", new Vector3(0f, axisY, -0.083f), new Vector3(0.038f, 0.004f, 0.038f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fl.transform, "Cavidade_Sem_Tampa", new Vector3(0f, axisY, -0.086f), new Vector3(0.030f, 0.002f, 0.030f), pal.shadowBlack, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fl.transform, "Mola_Bateria_Lanterna", new Vector3(0f, axisY, -0.088f), new Vector3(0.016f, 0.002f, 0.016f), pal.metalBrass, new Vector3(90f, 0f, 0f));

            return fl;
        }

        private static GameObject BuildDetailedCassetteTape(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject tape = CreateContainer(parent, name);
            tape.transform.localPosition = surfacePos;
            tape.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh shellMesh = CreateChamferedRectLoftMesh("Mesh_Carcaca_Cassete", 0.104f, 0.012f, 0.066f, 0.004f, 0.95f, 0.002f);
            CreateMeshObject(tape.transform, "Carcaca_Cassete", shellMesh, pal.metalDark, Vector3.zero, castShadows: true);
            CreateDetailBox(tape.transform, "Aba_Inferior_Cabecote", new Vector3(0f, 0.0090f, -0.027f), new Vector3(0.072f, 0.013f, 0.019f), pal.metalDark);
            CreateDetailBox(tape.transform, "Fita_Magnetica_Borda", new Vector3(0f, 0.0060f, -0.0372f), new Vector3(0.064f, 0.005f, 0.002f), pal.shadowBlack);
            CreateDetailBox(tape.transform, "Etiqueta_Papel", new Vector3(0f, 0.0132f, 0.006f), new Vector3(0.088f, 0.0024f, 0.042f), pal.paperWhite);
            CreatePrintedOverlayQuad(tape.transform, "Impressao_Etiqueta_Cassete", new Vector3(0f, 0.0149f, 0.006f), new Vector2(0.086f, 0.040f), pal.details.cassetteLabel, new Vector3(90f, 0f, 0f));
            CreateDetailBox(tape.transform, "Janela_Visor_Fita", new Vector3(0f, 0.0160f, 0.002f), new Vector3(0.056f, 0.0024f, 0.018f), pal.details.clearGlass);

            float[] screwXs = { -0.046f, 0.046f };
            float[] screwZs = { -0.027f, 0.027f };
            for (int sx = 0; sx < 2; sx++)
                for (int sz = 0; sz < 2; sz++)
                    CreateDetailCylinder(tape.transform, $"Parafuso_Cassete_{sx}_{sz}", new Vector3(screwXs[sx], 0.0126f, screwZs[sz]), new Vector3(0.0036f, 0.0008f, 0.0036f), pal.stainlessSteel);

            CreateDetailCylinder(tape.transform, "Carretel_Esq", new Vector3(-0.020f, 0.0108f, 0.002f), new Vector3(0.015f, 0.0084f, 0.015f), pal.porcelainWhite);
            CreateDetailCylinder(tape.transform, "Furo_Esq", new Vector3(-0.020f, 0.0132f, 0.002f), new Vector3(0.007f, 0.0084f, 0.007f), pal.shadowBlack);
            CreateDetailCylinder(tape.transform, "Carretel_Dir", new Vector3(0.020f, 0.0108f, 0.002f), new Vector3(0.015f, 0.0084f, 0.015f), pal.porcelainWhite);
            CreateDetailCylinder(tape.transform, "Furo_Dir", new Vector3(0.020f, 0.0132f, 0.002f), new Vector3(0.007f, 0.0084f, 0.007f), pal.shadowBlack);

            return tape;
        }

        private static Mesh GetOrBuildPharmacyBottleMesh(bool openMouth)
        {
            if (openMouth)
            {
                return CreateFacetedLatheVesselMesh("Mesh_Frasco_Farmacia_Aberto", 12, new[]
                {
                    (0.000f, 0.000f),
                    (0.000f, 0.0240f),
                    (0.004f, 0.0270f),
                    (0.074f, 0.0270f),
                    (0.084f, 0.0205f),
                    (0.094f, 0.0205f),
                    (0.094f, 0.0165f),
                    (0.082f, 0.0165f),
                    (0.070f, 0.0225f),
                    (0.008f, 0.0225f),
                    (0.008f, 0.000f)
                });
            }

            return CreateFacetedLatheVesselMesh("Mesh_Frasco_Farmacia", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.0245f),
                (0.004f, 0.0270f),
                (0.074f, 0.0270f),
                (0.084f, 0.0205f),
                (0.095f, 0.0205f),
                (0.095f, 0.000f)
            });
        }

        private static Mesh GetOrBuildPharmacyCapMesh()
        {
            return CreateFacetedLatheVesselMesh("Mesh_Tampa_Farmacia", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.0285f),
                (0.004f, 0.0285f),
                (0.006f, 0.0265f),
                (0.019f, 0.0265f),
                (0.021f, 0.0240f),
                (0.021f, 0.000f)
            });
        }

        private static GameObject CreateDetailedPillBottle(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = surfacePos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateMeshObject(root.transform, "Corpo_Ambar", GetOrBuildPharmacyBottleMesh(openMouth: false), pal.pillBottleAmber, Vector3.zero, castShadows: true);
            CreateCylinderLabelSleeve(root.transform, "Rotulo_Farmacia", new Vector3(0f, 0.041f, 0f), new Vector3(0.0572f, 0.050f, 0.0572f), pal.details.pillBottleLabel, pal, new Vector3(0f, 90f, 0f));
            CreateMeshObject(root.transform, "Tampa_Seguranca", GetOrBuildPharmacyCapMesh(), pal.porcelainWhite, new Vector3(0f, 0.094f, 0f), castShadows: true);
            return root;
        }

        private static GameObject CreateSpilledPillBottle(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = surfacePos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float bandR = 0.0288f; // Rotulo_Caido radius defines lowest resting rim of lying bottle
            CreateMeshObject(root.transform, "Corpo_Caido", GetOrBuildPharmacyBottleMesh(openMouth: true), pal.pillBottleAmber, new Vector3(0f, bandR, -0.044f), new Vector3(90f, 0f, 0f), castShadows: true);
            CreateCylinderLabelSleeve(root.transform, "Rotulo_Caido", new Vector3(0f, bandR, -0.004f), new Vector3(bandR * 2f, 0.048f, bandR * 2f), pal.details.pillBottleLabel, pal, new Vector3(0f, 90f, 90f));
            CreateDetailCylinder(root.transform, "Boca_Aberta", new Vector3(0f, bandR, 0.036f), new Vector3(0.032f, 0.005f, 0.032f), pal.shadowBlack, new Vector3(90f, 0f, 0f));

            CreateMeshObject(root.transform, "Tampa_Solta", GetOrBuildPharmacyCapMesh(), pal.porcelainWhite, new Vector3(0.056f, 0f, 0.038f), castShadows: true);
            CreateDetailCylinder(root.transform, "Comprimido_1", new Vector3(-0.012f, 0.002f, 0.072f), new Vector3(0.011f, 0.002f, 0.011f), pal.porcelainWhite);
            CreateDetailCylinder(root.transform, "Comprimido_2", new Vector3(0.018f, 0.002f, 0.088f), new Vector3(0.011f, 0.002f, 0.011f), pal.porcelainWhite);
            CreateDetailCylinder(root.transform, "Comprimido_3", new Vector3(-0.028f, 0.002f, 0.102f), new Vector3(0.011f, 0.002f, 0.011f), pal.porcelainWhite);
            return root;
        }

        private static GameObject CreateSpilledStimulantBottle(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = surfacePos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float bandR = 0.0282f;
            CreateMeshObject(root.transform, "Corpo_Frasco_Manipulado", GetOrBuildPharmacyBottleMesh(openMouth: true), pal.pillBottleAmber, new Vector3(0f, bandR, -0.044f), new Vector3(90f, 0f, 0f), castShadows: true);
            CreateCylinderLabelSleeve(root.transform, "Rotulo_Manipulado_D3", new Vector3(0f, bandR, -0.004f), new Vector3(bandR * 2f, 0.048f, bandR * 2f), pal.details.stimulantBottleLabel, pal, new Vector3(0f, 90f, 90f));
            CreateDetailCylinder(root.transform, "Boca_Aberta_Manipulado", new Vector3(0f, bandR, 0.036f), new Vector3(0.032f, 0.005f, 0.032f), pal.shadowBlack, new Vector3(90f, 0f, 0f));
            CreateMeshObject(root.transform, "Tampa_Solta_Manipulado", GetOrBuildPharmacyCapMesh(), pal.porcelainWhite, new Vector3(0.056f, 0f, 0.036f), castShadows: true);
            return root;
        }

        private static GameObject BuildLooseStimulantCapsule(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject cap = CreateContainer(parent, name);
            cap.transform.localPosition = surfacePos;
            cap.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const float radius = 0.0046f;
            float diam = radius * 2f;
            Material capDarkHalf = pal.bookBurgundy ?? pal.calendarRed;
            Material capLightHalf = pal.paperWhite ?? pal.porcelainWhite;

            // Two-tone hard gelatin capsule resting horizontally on the tabletop (min.y = 0.000 local)
            CreateDetailCylinder(cap.transform, "Metade_Escura", new Vector3(0f, radius, -0.0046f), new Vector3(diam, 0.0046f, diam), capDarkHalf, new Vector3(90f, 0f, 0f), castShadows: true);
            CreateDetailSphere(cap.transform, "Ponta_Escura", new Vector3(0f, radius, -0.0092f), new Vector3(diam, diam, diam), capDarkHalf, castShadows: false);
            CreateDetailCylinder(cap.transform, "Metade_Clara", new Vector3(0f, radius, 0.0046f), new Vector3(diam * 0.96f, 0.0046f, diam * 0.96f), capLightHalf, new Vector3(90f, 0f, 0f), castShadows: true);
            CreateDetailSphere(cap.transform, "Ponta_Clara", new Vector3(0f, radius, 0.0092f), new Vector3(diam * 0.96f, diam * 0.96f, diam * 0.96f), capLightHalf, castShadows: false);
            return cap;
        }

        private static GameObject BuildDetailedLandlinePhone(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject tel = CreateContainer(parent, name);
            tel.transform.localPosition = surfacePos;
            tel.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Base principal chanfrada anos 90 + berço + painel inclinado para teclado
            Mesh phoneBaseMesh = CreateChamferedRectLoftMesh("Mesh_Base_Telefone_90s", 0.172f, 0.028f, 0.198f, 0.014f, 0.93f, 0.005f);
            CreateMeshObject(tel.transform, "Base_Inferior", phoneBaseMesh, pal.plasticBeige90s, Vector3.zero, castShadows: true);
            CreateDetailBox(tel.transform, "Berco_Suporte_Esq", new Vector3(-0.052f, 0.034f, 0.044f), new Vector3(0.054f, 0.016f, 0.046f), pal.plasticBeige90s);
            CreateDetailBox(tel.transform, "Berco_Suporte_Dir", new Vector3(-0.052f, 0.034f, -0.044f), new Vector3(0.054f, 0.016f, 0.046f), pal.plasticBeige90s);
            CreateDetailBox(tel.transform, "Gancho_Pino_0", new Vector3(-0.052f, 0.036f, 0.010f), new Vector3(0.012f, 0.016f, 0.008f), pal.porcelainWhite);
            CreateDetailBox(tel.transform, "Gancho_Pino_1", new Vector3(-0.052f, 0.036f, -0.010f), new Vector3(0.012f, 0.016f, 0.008f), pal.porcelainWhite);

            CreateDetailBox(tel.transform, "Painel_Inclinado", new Vector3(0.024f, 0.036f, -0.008f), new Vector3(0.102f, 0.024f, 0.170f), pal.plasticBeige90s, new Vector3(-14f, 0f, 0f));
            CreateDetailBox(tel.transform, "Etiqueta_Numero_Tel", new Vector3(0.024f, 0.054f, 0.056f), new Vector3(0.068f, 0.004f, 0.022f), pal.paperWhite, new Vector3(-14f, 0f, 0f));

            for (int r = 0; r < 4; r++)
            {
                for (int c = -1; c <= 1; c++)
                {
                    bool isRedial = (r == 3 && c == -1);
                    Material keyMat = isRedial ? pal.calendarRed : pal.metalDark;
                    CreateDetailBox(tel.transform, $"Tecla_Tel_{r}_{c + 1}",
                        new Vector3(0.024f + c * 0.022f, 0.047f - r * 0.0055f, 0.028f - r * 0.022f),
                        new Vector3(0.015f, 0.007f, 0.014f), keyMat, new Vector3(-14f, 0f, 0f));
                }
            }

            // Monofone ergonômico curvado descansando exatamente sobre os dois apoios do berço
            GameObject handset = CreateContainer(tel.transform, "Monofone");
            handset.transform.localPosition = new Vector3(-0.052f, 0.054f, 0f);
            Mesh handsetArchMesh = CreateFacetedCShapeHandleMesh("Mesh_Empunhadura_Monofone", new[]
            {
                new Vector3(0f, -0.009f, -0.072f),
                new Vector3(0f,  0.004f, -0.052f),
                new Vector3(0f,  0.008f,  0.000f),
                new Vector3(0f,  0.004f,  0.052f),
                new Vector3(0f, -0.009f,  0.072f)
            }, halfThicknessY: 0.010f, halfThicknessZ: 0.016f, sideAxis: Vector3.right);
            CreateMeshObject(handset.transform, "Empunhadura", handsetArchMesh, pal.plasticBeige90s, Vector3.zero, castShadows: true);

            Mesh cupMesh = CreateFacetedLatheVesselMesh("Mesh_Capsula_Monofone", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.023f),
                (0.010f, 0.026f),
                (0.022f, 0.017f),
                (0.022f, 0.000f)
            });
            CreateMeshObject(handset.transform, "Capsula_Ouvido", cupMesh, pal.plasticBeige90s, new Vector3(0f, -0.020f, 0.072f), castShadows: true);
            CreateMeshObject(handset.transform, "Capsula_Voz", cupMesh, pal.plasticBeige90s, new Vector3(0f, -0.020f, -0.072f), castShadows: true);

            // Fio espiralado contínuo ligando a cápsula de voz à lateral esquerda da base
            BuildRoutedWallCable(tel.transform, "Fio_Monofone", "Mesh_Fio_Monofone_Tel", new[]
            {
                new Vector3(-0.052f, 0.034f, -0.076f),
                new Vector3(-0.078f, 0.018f, -0.072f),
                new Vector3(-0.094f, 0.008f, -0.044f),
                new Vector3(-0.092f, 0.008f,  0.004f),
                new Vector3(-0.084f, 0.014f,  0.022f)
            }, pal.metalDark, radius: 0.0042f);
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                CreateDetailCylinder(tel.transform, $"Espiral_Fio_{i}",
                    new Vector3(-0.091f + Mathf.Sin(t * Mathf.PI) * 0.004f, 0.009f, -0.056f + t * 0.072f),
                    new Vector3(0.016f, 0.0035f, 0.016f), pal.shadowBlack, new Vector3(82f, 6f, 0f));
            }
            CreateDetailBox(tel.transform, "Jack_Traseiro_RJ11", new Vector3(-0.020f, 0.014f, 0.099f), new Vector3(0.018f, 0.014f, 0.008f), pal.shadowBlack);

            return tel;
        }

        private static GameObject BuildAnsweringMachine(
            Transform parent, string name, Vector3 surfacePos, GreyboxPalette pal, bool blinkingRedLed, bool hasTapeInside = true)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = surfacePos;

            // Quatro pés de borracha apoiados exatos em Y=0.000m (altura 4mm)
            float[] fx = { -0.104f, 0.104f };
            float[] fz = { -0.068f, 0.068f };
            for (int ix = 0; ix < 2; ix++)
                for (int iz = 0; iz < 2; iz++)
                    CreateDetailCylinder(root.transform, $"Pe_Secretaria_{ix}_{iz}", new Vector3(fx[ix], 0.002f, fz[iz]), new Vector3(0.018f, 0.002f, 0.018f), pal.shadowBlack);

            Mesh secBodyMesh = CreateChamferedRectLoftMesh("Mesh_Corpo_Secretaria", 0.242f, 0.052f, 0.172f, 0.010f, 0.95f, 0.006f);
            CreateMeshObject(root.transform, "Corpo_Secretaria", secBodyMesh, pal.metalDark, new Vector3(0f, 0.004f, 0f), castShadows: true);
            CreateDetailBox(root.transform, "Moldura_Frontal", new Vector3(0f, 0.031f, -0.086f), new Vector3(0.228f, 0.040f, 0.006f), pal.plasticBeige90s);
            CreateDetailBox(root.transform, "Placa_Modelo_Sec", new Vector3(0.072f, 0.031f, -0.090f), new Vector3(0.058f, 0.014f, 0.002f), pal.shadowBlack);
            CreateDetailBox(root.transform, "Conector_Traseiro_Sec", new Vector3(-0.015f, 0.024f, 0.088f), new Vector3(0.054f, 0.018f, 0.006f), pal.shadowBlack);

            CreateDetailBox(root.transform, "Cavidade_Deck", new Vector3(-0.046f, 0.0572f, 0.006f), new Vector3(0.114f, 0.0024f, 0.082f), pal.shadowBlack);
            // Pinos fixos vazios visíveis quando a secretária está sem microfita
            CreateDetailCylinder(root.transform, "Pino_Vazio_Esq", new Vector3(-0.066f, 0.0595f, 0.006f), new Vector3(0.009f, 0.0012f, 0.009f), pal.stainlessSteel);
            CreateDetailCylinder(root.transform, "Pino_Vazio_Dir", new Vector3(-0.026f, 0.0595f, 0.006f), new Vector3(0.009f, 0.0012f, 0.009f), pal.stainlessSteel);

            // Sub-conjunto completo da microfita inserida (revelado ao colocar a fita no Dia 2 e com rolos animados ao tocar)
            GameObject tapeInsert = CreateContainer(root.transform, "Microfita_Inserida");
            tapeInsert.transform.localPosition = new Vector3(-0.046f, 0.0616f, 0.006f);
            CreateDetailBox(tapeInsert.transform, "Microfita_Interna", Vector3.zero, new Vector3(0.092f, 0.0028f, 0.058f), pal.paperWhite);
            CreateDetailBox(tapeInsert.transform, "Janela_Fita", new Vector3(0f, 0.0025f, 0f), new Vector3(0.056f, 0.0022f, 0.022f), pal.shadowBlack);
            GameObject reelL = CreateContainer(tapeInsert.transform, "Rolo_Esq");
            reelL.transform.localPosition = new Vector3(-0.020f, 0.0038f, 0f);
            CreateDetailCylinder(reelL.transform, "Carretel_Esq", Vector3.zero, new Vector3(0.016f, 0.0012f, 0.016f), pal.metalDark);
            CreateDetailBox(reelL.transform, "Dente_Rolo_Esq", new Vector3(0f, 0.0008f, 0f), new Vector3(0.012f, 0.0008f, 0.003f), pal.porcelainWhite);
            GameObject reelR = CreateContainer(tapeInsert.transform, "Rolo_Dir");
            reelR.transform.localPosition = new Vector3(0.020f, 0.0038f, 0f);
            CreateDetailCylinder(reelR.transform, "Carretel_Dir", Vector3.zero, new Vector3(0.016f, 0.0012f, 0.016f), pal.metalDark);
            CreateDetailBox(reelR.transform, "Dente_Rolo_Dir", new Vector3(0f, 0.0008f, 0f), new Vector3(0.012f, 0.0008f, 0.003f), pal.porcelainWhite);
            tapeInsert.SetActive(hasTapeInside);

            CreateDetailBox(root.transform, "Borda_Deck_Esq", new Vector3(-0.106f, 0.0607f, 0.006f), new Vector3(0.006f, 0.0094f, 0.082f), pal.metalDark);
            CreateDetailBox(root.transform, "Borda_Deck_Dir", new Vector3(0.014f, 0.0607f, 0.006f), new Vector3(0.006f, 0.0094f, 0.082f), pal.metalDark);
            CreateDetailBox(root.transform, "Borda_Deck_Tras", new Vector3(-0.046f, 0.0607f, 0.050f), new Vector3(0.126f, 0.0094f, 0.006f), pal.metalDark);
            CreateDetailBox(root.transform, "Borda_Deck_Frente", new Vector3(-0.046f, 0.0607f, -0.038f), new Vector3(0.126f, 0.0094f, 0.006f), pal.metalDark);
            CreateDetailBox(root.transform, "Dobradica_Tampa_E", new Vector3(-0.084f, 0.0672f, 0.052f), new Vector3(0.016f, 0.003f, 0.006f), pal.stainlessSteel);
            CreateDetailBox(root.transform, "Dobradica_Tampa_D", new Vector3(-0.008f, 0.0672f, 0.052f), new Vector3(0.016f, 0.003f, 0.006f), pal.stainlessSteel);
            CreateDetailBox(root.transform, "Tampa_Fita", new Vector3(-0.046f, 0.0688f, 0.006f), new Vector3(0.124f, 0.0024f, 0.090f), pal.details.clearGlass);

            for (int g = 0; g < 4; g++)
            {
                CreateDetailBox(root.transform, $"Fenda_Audio_{g}", new Vector3(0.068f, 0.0572f, 0.036f - g * 0.012f), new Vector3(0.072f, 0.0024f, 0.005f), pal.shadowBlack);
            }
            CreateDetailBox(root.transform, "Visor_Contador", new Vector3(0.044f, 0.0580f, -0.042f), new Vector3(0.032f, 0.004f, 0.026f), pal.visorScreen);
            CreateDetailBox(root.transform, "Botao_Play", new Vector3(-0.078f, 0.060f, -0.056f), new Vector3(0.024f, 0.008f, 0.018f), pal.plasticBeige90s);
            CreateDetailBox(root.transform, "Botao_Stop", new Vector3(-0.046f, 0.060f, -0.056f), new Vector3(0.022f, 0.008f, 0.018f), pal.plasticSlate);
            CreateDetailBox(root.transform, "Botao_Rew", new Vector3(-0.016f, 0.060f, -0.056f), new Vector3(0.022f, 0.008f, 0.018f), pal.bookMustard);

            GameObject led = CreateBox(root.transform, "LED_Mensagens", new Vector3(0.084f, 0.060f, -0.042f), new Vector3(0.016f, 0.008f, 0.016f),
                blinkingRedLed ? pal.calendarRed : pal.metalDark, hasCollider: false);
            if (led.TryGetComponent<MeshRenderer>(out MeshRenderer ledMr))
            {
                ledMr.shadowCastingMode = ShadowCastingMode.Off;
            }

            BoxCollider col = GetOrAdd<BoxCollider>(root);
            col.isTrigger = false;
            col.center = new Vector3(0f, 0.045f, -0.01f);
            col.size = new Vector3(0.34f, 0.16f, 0.22f);
            return root;
        }

        private static GameObject BuildDetailedAlarmClock(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject clk = CreateContainer(parent, name);
            clk.transform.localPosition = surfacePos;
            clk.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Pés torneados dianteiros/traseiros em latão (apoio exato em Y=0.000m)
            Mesh footMesh = CreateFacetedLatheVesselMesh("Mesh_Pe_Despertador", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.0055f),
                (0.006f, 0.0085f),
                (0.012f, 0.0065f),
                (0.015f, 0.0050f),
                (0.015f, 0.000f)
            });
            float[] legXs = { -0.036f, 0.036f };
            float[] legZs = { -0.020f, 0.020f };
            for (int lx = 0; lx < legXs.Length; lx++)
            {
                for (int lz = 0; lz < legZs.Length; lz++)
                {
                    CreateMeshObject(clk.transform, $"Pe_Despertador_{lx}_{lz}", footMesh, pal.metalBrass,
                        new Vector3(legXs[lx], 0f, legZs[lz]), Vector3.zero, castShadows: false);
                }
            }

            // Corpo arredondado estilo despertador clássico anos 90
            CreateDetailCylinder(clk.transform, "Carcaca_Relogio_Cil", new Vector3(0f, 0.068f, 0f),
                new Vector3(0.112f, 0.026f, 0.112f), pal.woodMahogany, new Vector3(90f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(clk.transform, "Tampa_Tras_Metal", new Vector3(0f, 0.068f, 0.027f),
                new Vector3(0.104f, 0.004f, 0.104f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));

            // Botões de ajuste e pino de parada atrás
            CreateDetailCylinder(clk.transform, "Botao_Acerto_Hora", new Vector3(-0.024f, 0.056f, 0.036f),
                new Vector3(0.016f, 0.006f, 0.016f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(clk.transform, "Botao_Corda_Alarme", new Vector3(0.024f, 0.056f, 0.036f),
                new Vector3(0.016f, 0.006f, 0.016f), pal.metalBrass, new Vector3(90f, 0f, 0f));

            // Campainhas duplas superiores + haste + martelo + alça em arco de latão
            Mesh bellMesh = CreateFacetedLatheVesselMesh("Mesh_Despertador_Sino", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.021f),
                (0.012f, 0.018f),
                (0.020f, 0.010f),
                (0.022f, 0.000f)
            });
            CreateDetailCylinder(clk.transform, "Haste_Sino_E", new Vector3(-0.036f, 0.120f, -0.004f),
                new Vector3(0.005f, 0.014f, 0.005f), pal.metalBrass, new Vector3(0f, 0f, 24f));
            CreateMeshObject(clk.transform, "Sino_Alarme_Esq", bellMesh, pal.metalBrass,
                new Vector3(-0.042f, 0.125f, -0.004f), new Vector3(0f, 0f, 28f), castShadows: true);

            CreateDetailCylinder(clk.transform, "Haste_Sino_D", new Vector3(0.036f, 0.120f, -0.004f),
                new Vector3(0.005f, 0.014f, 0.005f), pal.metalBrass, new Vector3(0f, 0f, -24f));
            CreateMeshObject(clk.transform, "Sino_Alarme_Dir", bellMesh, pal.metalBrass,
                new Vector3(0.042f, 0.125f, -0.004f), new Vector3(0f, 0f, -28f), castShadows: true);

            Mesh clockArchMesh = CreateFacetedCShapeHandleMesh("Mesh_Alca_Despertador", new[]
            {
                new Vector3(-0.046f, 0.138f, -0.004f),
                new Vector3(-0.034f, 0.158f, -0.004f),
                new Vector3( 0.000f, 0.165f, -0.004f),
                new Vector3( 0.034f, 0.158f, -0.004f),
                new Vector3( 0.046f, 0.138f, -0.004f)
            }, halfThicknessY: 0.0024f, halfThicknessZ: 0.0024f);
            CreateMeshObject(clk.transform, "Alca_Superior_Despertador", clockArchMesh, pal.metalBrass, Vector3.zero, castShadows: false);

            CreateDetailCylinder(clk.transform, "Haste_Martelo", new Vector3(0f, 0.124f, -0.004f),
                new Vector3(0.004f, 0.014f, 0.004f), pal.stainlessSteel);
            CreateDetailSphere(clk.transform, "Martelo_Alarme", new Vector3(0f, 0.140f, -0.004f),
                new Vector3(0.011f, 0.011f, 0.011f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(clk.transform, "Botao_Desliga_Alarme", new Vector3(0f, 0.120f, 0.014f),
                new Vector3(0.008f, 0.010f, 0.008f), pal.stainlessSteel);

            // Mostrador analógico real e vivo sincronizado pelo DayManager (5:40 no Dia 1)
            BuildAnalogClockDialAssembly(
                clk.transform,
                new Vector3(0f, 0.068f, -0.027f),
                dialRadius: 0.046f,
                pal: pal,
                out Transform hourPivot,
                out Transform minutePivot,
                out Transform secondPivot);

            AnalogClockController clockCtrl = clk.AddComponent<AnalogClockController>();
            clockCtrl.ConfigureClock(hourPivot, minutePivot, secondPivot, 17, 40, 0f, running: true, discreteSecond: true);
            return clk;
        }

        private static GameObject BuildDetailedDeskCalculator(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject calc = CreateContainer(parent, name);
            calc.transform.localPosition = surfacePos;
            calc.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh calcBaseMesh = CreateChamferedRectLoftMesh("Mesh_Calculadora_Corpo", 0.185f, 0.036f, 0.245f, 0.012f, 0.94f, 0.005f);
            CreateMeshObject(calc.transform, "Base_Calculadora", calcBaseMesh, pal.plasticBeige90s, Vector3.zero, castShadows: true);
            Mesh printerHoodMesh = CreateChamferedRectLoftMesh("Mesh_Capo_Impressora_Calc", 0.175f, 0.032f, 0.092f, 0.010f, 0.90f, 0.005f);
            CreateMeshObject(calc.transform, "Bloco_Impressora", printerHoodMesh, pal.plasticBeige90s, new Vector3(0f, 0.028f, 0.068f), castShadows: true);
            CreateDetailBox(calc.transform, "Fenda_Papel_Calc", new Vector3(-0.02f, 0.0608f, 0.098f), new Vector3(0.072f, 0.0024f, 0.012f), pal.shadowBlack);
            CreateDetailBox(calc.transform, "Lamina_Corte_Papel", new Vector3(-0.02f, 0.0630f, 0.090f), new Vector3(0.072f, 0.0026f, 0.003f), pal.stainlessSteel);
            CreateDetailBox(calc.transform, "Moldura_Visor_Calc", new Vector3(0f, 0.039f, 0.008f), new Vector3(0.158f, 0.012f, 0.040f), pal.shadowBlack, new Vector3(-16f, 0f, 0f));
            CreateDetailBox(calc.transform, "Visor_Verde_Fluorescente", new Vector3(0f, 0.043f, 0.008f), new Vector3(0.146f, 0.008f, 0.030f), pal.ledGreenDim, new Vector3(-16f, 0f, 0f));

            CreateDetailCylinder(calc.transform, "Rolo_Bobina", new Vector3(-0.02f, 0.064f, 0.088f), new Vector3(0.048f, 0.032f, 0.048f), pal.paperWhite, new Vector3(0f, 0f, 90f));
            CreateDetailBox(calc.transform, "Fita_Impressa_Saindo", new Vector3(-0.02f, 0.082f, 0.118f), new Vector3(0.062f, 0.002f, 0.085f), pal.paperWhite, new Vector3(28f, 0f, 0f));

            for (int r = 0; r < 4; r++)
            {
                for (int c = -1; c <= 1; c++)
                {
                    bool isClearKey = (r == 0 && c == -1);
                    CreateDetailBox(calc.transform, $"Tecla_Calc_{r}_{c + 1}",
                        new Vector3(-0.022f + c * 0.024f, 0.038f, -0.028f - r * 0.021f),
                        new Vector3(0.017f, 0.007f, 0.015f), isClearKey ? pal.calendarRed : pal.metalDark);
                }
            }
            CreateDetailBox(calc.transform, "Tecla_Soma_Azul", new Vector3(0.056f, 0.039f, -0.078f), new Vector3(0.024f, 0.008f, 0.038f), pal.bookSlateBlue ?? pal.sofaFabric);
            CreateDetailBox(calc.transform, "Tecla_Sub_Calc", new Vector3(0.056f, 0.039f, -0.040f), new Vector3(0.024f, 0.008f, 0.022f), pal.plasticBeige90s);

            return calc;
        }

        private static GameObject BuildDetailedPaperSheet(
            Transform parent, string name, Vector3 localPos, float yaw, Vector2 sizeXZ, GreyboxPalette pal,
            bool hasRedHeader = false, bool hasHeavyScratchBottom = false, bool isNewspaper = false,
            Material printedFaceMat = null, float printAngleY = 0f)
        {
            GameObject sheet = CreateContainer(parent, name);
            sheet.transform.localPosition = localPos;
            sheet.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Material baseMat = isNewspaper ? pal.folderKraft : pal.paperWhite;
            CreateDetailBox(sheet.transform, "Folha_Papel", new Vector3(0f, 0.0012f, 0f), new Vector3(sizeXZ.x, 0.0024f, sizeXZ.y), baseMat);

            if (printedFaceMat != null)
            {
                Vector2 quadSize = (Mathf.Abs(Mathf.DeltaAngle(printAngleY, 90f)) < 15f || Mathf.Abs(Mathf.DeltaAngle(printAngleY, -90f)) < 15f)
                    ? new Vector2(sizeXZ.y * 0.985f, sizeXZ.x * 0.985f)
                    : new Vector2(sizeXZ.x * 0.985f, sizeXZ.y * 0.985f);
                CreatePrintedOverlayQuad(sheet.transform, "Impressao_Folha", new Vector3(0f, 0.0030f, 0f), quadSize, printedFaceMat, new Vector3(90f, printAngleY, 0f));
                return sheet;
            }

            if (hasRedHeader)
            {
                CreateDetailBox(sheet.transform, "Cabecalho_Timbrado", new Vector3(0f, 0.0035f, sizeXZ.y * 0.36f),
                    new Vector3(sizeXZ.x * 0.82f, 0.0022f, sizeXZ.y * 0.10f), pal.calendarRed);
            }
            else if (isNewspaper)
            {
                CreateDetailBox(sheet.transform, "Manchete_Jornal", new Vector3(0f, 0.0035f, sizeXZ.y * 0.34f),
                    new Vector3(sizeXZ.x * 0.86f, 0.0022f, sizeXZ.y * 0.13f), pal.metalDark);
                CreateDetailBox(sheet.transform, "Coluna_Esq", new Vector3(-sizeXZ.x * 0.21f, 0.0035f, -sizeXZ.y * 0.08f),
                    new Vector3(sizeXZ.x * 0.36f, 0.0022f, sizeXZ.y * 0.56f), pal.metalDark);
                CreateDetailBox(sheet.transform, "Coluna_Dir", new Vector3(sizeXZ.x * 0.21f, 0.0035f, -sizeXZ.y * 0.08f),
                    new Vector3(sizeXZ.x * 0.36f, 0.0022f, sizeXZ.y * 0.56f), pal.metalDark);
                return sheet;
            }

            int lines = 4;
            for (int i = 0; i < lines; i++)
            {
                float z = sizeXZ.y * (0.18f - i * 0.13f);
                float w = sizeXZ.x * (i == lines - 1 ? 0.56f : 0.76f);
                CreateDetailBox(sheet.transform, $"Linha_Tinta_{i}", new Vector3(-sizeXZ.x * 0.04f, 0.0035f, z),
                    new Vector3(w, 0.0022f, 0.004f), pal.metalDark);
            }

            if (hasHeavyScratchBottom)
            {
                CreateDetailBox(sheet.transform, "Risco_Forte_Caneta", new Vector3(0f, 0.0036f, -sizeXZ.y * 0.28f),
                    new Vector3(sizeXZ.x * 0.80f, 0.0024f, 0.012f), pal.shadowBlack, new Vector3(0f, -2f, 0f));
            }

            return sheet;
        }

        private static GameObject BuildDetailedCeramicFuse(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject fuse = CreateContainer(parent, name);
            fuse.transform.localPosition = surfacePos;
            fuse.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const float r = 0.0170f;
            Mesh fuseBodyMesh = CreateFacetedLatheVesselMesh("Mesh_Fusivel_Porcelana", 10, new[]
            {
                (-0.026f, 0.000f),
                (-0.026f, 0.0110f),
                (-0.022f, 0.0148f),
                (-0.010f, 0.0148f),
                (-0.007f, 0.0132f),
                (-0.004f, 0.0170f),
                ( 0.004f, 0.0170f),
                ( 0.007f, 0.0132f),
                ( 0.010f, 0.0148f),
                ( 0.022f, 0.0148f),
                ( 0.026f, 0.0132f),
                ( 0.026f, 0.000f)
            });
            CreateMeshObject(fuse.transform, "Corpo_Porcelana", fuseBodyMesh, pal.porcelainWhite, new Vector3(0f, r, 0f), new Vector3(90f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(fuse.transform, "Ponteira_Latao_A", new Vector3(0f, r, 0.029f), new Vector3(0.026f, 0.006f, 0.026f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fuse.transform, "Ponteira_Latao_B", new Vector3(0f, r, -0.029f), new Vector3(0.021f, 0.006f, 0.021f), pal.metalBrass, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fuse.transform, "Selo_Amperagem", new Vector3(0f, r, 0.0365f), new Vector3(0.008f, 0.0014f, 0.008f), pal.calendarRed, new Vector3(90f, 0f, 0f));

            return fuse;
        }

        private static GameObject BuildDetailedPliers(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject pl = CreateContainer(parent, name);
            pl.transform.localPosition = surfacePos;
            pl.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh leftHandle = CreateFacetedCShapeHandleMesh("Mesh_Cabo_Alicate_Esq", new[]
            {
                new Vector3(-0.006f, 0.0068f,  0.016f),
                new Vector3(-0.016f, 0.0068f, -0.010f),
                new Vector3(-0.024f, 0.0068f, -0.046f),
                new Vector3(-0.022f, 0.0068f, -0.082f)
            }, halfThicknessY: 0.0060f, halfThicknessZ: 0.0068f, sideAxis: Vector3.up);
            Mesh rightHandle = CreateFacetedCShapeHandleMesh("Mesh_Cabo_Alicate_Dir", new[]
            {
                new Vector3( 0.006f, 0.0068f,  0.016f),
                new Vector3( 0.016f, 0.0068f, -0.010f),
                new Vector3( 0.024f, 0.0068f, -0.046f),
                new Vector3( 0.022f, 0.0068f, -0.082f)
            }, halfThicknessY: 0.0060f, halfThicknessZ: 0.0068f, sideAxis: Vector3.up);
            CreateMeshObject(pl.transform, "Cabo_Isolado_Esq", leftHandle, pal.plasticBeige90s, Vector3.zero, castShadows: true);
            CreateMeshObject(pl.transform, "Cabo_Isolado_Dir", rightHandle, pal.plasticBeige90s, Vector3.zero, castShadows: true);

            CreateDetailCylinder(pl.transform, "Pivo_Aco", new Vector3(0f, 0.0075f, 0.022f), new Vector3(0.028f, 0.0070f, 0.028f), pal.metalDark);
            CreateDetailCylinder(pl.transform, "Rebite_Pivo", new Vector3(0f, 0.0152f, 0.022f), new Vector3(0.010f, 0.0012f, 0.010f), pal.stainlessSteel);

            Mesh jawMesh = CreateClosedPolygonLoftMesh("Mesh_Bico_Alicate", new[]
            {
                new Vector2(-0.006f, -0.022f),
                new Vector2( 0.006f, -0.022f),
                new Vector2( 0.003f,  0.024f),
                new Vector2(-0.003f,  0.024f)
            }, new[]
            {
                (0.002f, 0.92f, 1.00f, 0f, 0f),
                (0.012f, 1.00f, 1.00f, 0f, 0f),
                (0.014f, 0.86f, 0.96f, 0f, 0f)
            });
            CreateMeshObject(pl.transform, "Bico_Aco_Esq", jawMesh, pal.stainlessSteel, new Vector3(-0.006f, 0f, 0.052f), new Vector3(0f, 5f, 0f), castShadows: true);
            CreateMeshObject(pl.transform, "Bico_Aco_Dir", jawMesh, pal.stainlessSteel, new Vector3( 0.006f, 0f, 0.052f), new Vector3(0f, -5f, 0f), castShadows: true);
            return pl;
        }

        private static void BuildDetailedDiningChair(
            Transform parent, string name, Vector3 localPos, float yaw, GreyboxPalette pal)
        {
            GameObject c = CreateContainer(parent, name);
            c.transform.localPosition = localPos;
            c.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float lx = 0.18f, lz = 0.18f;
            Mesh frontLegMesh = CreateChamferedRectLoftMesh("Mesh_Perna_Cadeira_Frente", 0.038f, 0.425f, 0.038f, 0.005f, 0.94f, 0.004f);
            Mesh backLegMesh = CreateChamferedRectLoftMesh("Mesh_Perna_Cadeira_Tras", 0.038f, 0.875f, 0.038f, 0.005f, 0.94f, 0.004f);
            CreateMeshObject(c.transform, "Perna_FE", frontLegMesh, pal.furnitureWood, new Vector3(-lx, 0.005f, lz), castShadows: true);
            CreateMeshObject(c.transform, "Perna_FD", frontLegMesh, pal.furnitureWood, new Vector3( lx, 0.005f, lz), castShadows: true);
            CreateMeshObject(c.transform, "Perna_TE", backLegMesh, pal.furnitureWood, new Vector3(-lx, 0.005f, -lz), castShadows: true);
            CreateMeshObject(c.transform, "Perna_TD", backLegMesh, pal.furnitureWood, new Vector3( lx, 0.005f, -lz), castShadows: true);
            CreateDetailCylinder(c.transform, "Sapata_Pe_FE", new Vector3(-lx, 0.003f, lz), new Vector3(0.042f, 0.003f, 0.042f), pal.metalDark);
            CreateDetailCylinder(c.transform, "Sapata_Pe_FD", new Vector3(lx, 0.003f, lz), new Vector3(0.042f, 0.003f, 0.042f), pal.metalDark);
            CreateDetailCylinder(c.transform, "Sapata_Pe_TE", new Vector3(-lx, 0.003f, -lz), new Vector3(0.042f, 0.003f, 0.042f), pal.metalDark);
            CreateDetailCylinder(c.transform, "Sapata_Pe_TD", new Vector3(lx, 0.003f, -lz), new Vector3(0.042f, 0.003f, 0.042f), pal.metalDark);

            CreateDetailBox(c.transform, "Travessa_Esq", new Vector3(-lx, 0.14f, 0f), new Vector3(0.024f, 0.026f, 0.34f), pal.furnitureWood);
            CreateDetailBox(c.transform, "Travessa_Dir", new Vector3(lx, 0.14f, 0f), new Vector3(0.024f, 0.026f, 0.34f), pal.furnitureWood);
            CreateDetailBox(c.transform, "Travessa_Frente", new Vector3(0f, 0.14f, lz), new Vector3(0.34f, 0.026f, 0.024f), pal.furnitureWood);
            CreateDetailBox(c.transform, "Travessa_Tras", new Vector3(0f, 0.14f, -lz), new Vector3(0.34f, 0.026f, 0.024f), pal.furnitureWood);

            Mesh seatFrameMesh = CreateChamferedRectLoftMesh("Mesh_Quadro_Assento_Cadeira", 0.424f, 0.036f, 0.424f, 0.016f, 0.96f, 0.005f);
            CreateMeshObject(c.transform, "Quadro_Assento", seatFrameMesh, pal.furnitureWood, new Vector3(0f, 0.412f, 0f), castShadows: true);
            Mesh cushionMesh = CreateChamferedRectLoftMesh("Mesh_Cadeira_Assento_Chamfer", 0.382f, 0.030f, 0.382f, 0.022f, 0.90f, 0.008f);
            CreateMeshObject(c.transform, "Estofado_Assento", cushionMesh, pal.fabricOlive, new Vector3(0f, 0.4465f, 0.01f), castShadows: true);
            CreateDetailBox(c.transform, "Debrum_Assento", new Vector3(0f, 0.451f, 0.01f), new Vector3(0.386f, 0.006f, 0.386f), pal.fabricAsh);

            CreateDetailBox(c.transform, "Travessa_Inf_Encosto", new Vector3(0f, 0.488f, -lz), new Vector3(0.340f, 0.028f, 0.024f), pal.furnitureWood, castShadows: true);
            Mesh crestRailMesh = CreateChamferedRectLoftMesh("Mesh_Topo_Encosto_Cadeira", 0.428f, 0.068f, 0.034f, 0.010f, 0.90f, 0.008f);
            CreateMeshObject(c.transform, "Travessa_Topo_Encosto", crestRailMesh, pal.furnitureWood, new Vector3(0f, 0.826f, -lz), castShadows: true);
            for (int s = -1; s <= 1; s++)
            {
                CreateDetailBox(c.transform, $"Ripa_Encosto_{s + 2}", new Vector3(s * 0.085f, 0.65f, -lz), new Vector3(0.036f, 0.36f, 0.018f), pal.furnitureWood);
            }
        }

        private static GameObject BuildDetailedChildChair(
            Transform parent, string name, Vector3 localPos, float yaw, Material seatMat, GreyboxPalette pal)
        {
            GameObject ch = CreateContainer(parent, name);
            ch.transform.localPosition = localPos;
            ch.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const float lx = 0.122f;
            const float lz = 0.122f;
            Mesh frontLeg = CreateChamferedRectLoftMesh("Mesh_Perna_Cadeira_Inf_F", 0.028f, 0.275f, 0.028f, 0.004f, 0.94f, 0.003f);
            Mesh backLeg = CreateChamferedRectLoftMesh("Mesh_Perna_Cadeira_Inf_T", 0.028f, 0.545f, 0.028f, 0.004f, 0.94f, 0.003f);
            CreateMeshObject(ch.transform, "Perna_Inf_FE", frontLeg, pal.furnitureWood, new Vector3(-lx, 0.005f,  lz), castShadows: true);
            CreateMeshObject(ch.transform, "Perna_Inf_FD", frontLeg, pal.furnitureWood, new Vector3( lx, 0.005f,  lz), castShadows: true);
            CreateMeshObject(ch.transform, "Perna_Inf_TE", backLeg, pal.furnitureWood,  new Vector3(-lx, 0.005f, -lz), castShadows: true);
            CreateMeshObject(ch.transform, "Perna_Inf_TD", backLeg, pal.furnitureWood,  new Vector3( lx, 0.005f, -lz), castShadows: true);

            CreateDetailCylinder(ch.transform, "Sapata_Inf_FE", new Vector3(-lx, 0.0025f,  lz), new Vector3(0.030f, 0.0025f, 0.030f), pal.metalDark);
            CreateDetailCylinder(ch.transform, "Sapata_Inf_FD", new Vector3( lx, 0.0025f,  lz), new Vector3(0.030f, 0.0025f, 0.030f), pal.metalDark);
            CreateDetailCylinder(ch.transform, "Sapata_Inf_TE", new Vector3(-lx, 0.0025f, -lz), new Vector3(0.030f, 0.0025f, 0.030f), pal.metalDark);
            CreateDetailCylinder(ch.transform, "Sapata_Inf_TD", new Vector3( lx, 0.0025f, -lz), new Vector3(0.030f, 0.0025f, 0.030f), pal.metalDark);

            CreateDetailBox(ch.transform, "Travessa_Inf_E", new Vector3(-lx, 0.095f, 0f), new Vector3(0.018f, 0.020f, 0.220f), pal.furnitureWood);
            CreateDetailBox(ch.transform, "Travessa_Inf_D", new Vector3( lx, 0.095f, 0f), new Vector3(0.018f, 0.020f, 0.220f), pal.furnitureWood);
            CreateDetailBox(ch.transform, "Travessa_Inf_F", new Vector3(0f, 0.095f,  lz), new Vector3(0.220f, 0.020f, 0.018f), pal.furnitureWood);
            CreateDetailBox(ch.transform, "Travessa_Inf_T", new Vector3(0f, 0.095f, -lz), new Vector3(0.220f, 0.020f, 0.018f), pal.furnitureWood);

            Mesh seatMesh = CreateChamferedRectLoftMesh("Mesh_Assento_Cadeira_Infantil", 0.294f, 0.026f, 0.294f, 0.018f, 0.94f, 0.005f);
            CreateMeshObject(ch.transform, "Assento_Cadeira_Infantil", seatMesh, seatMat, new Vector3(0f, 0.276f, 0.004f), castShadows: true);

            Mesh crestMesh = CreateChamferedRectLoftMesh("Mesh_Topo_Cadeira_Infantil", 0.296f, 0.056f, 0.026f, 0.008f, 0.90f, 0.006f);
            CreateMeshObject(ch.transform, "Encosto_Topo_Infantil", crestMesh, seatMat, new Vector3(0f, 0.506f, -lz), castShadows: true);
            CreateDetailBox(ch.transform, "Ripa_Inf_E", new Vector3(-0.048f, 0.404f, -lz), new Vector3(0.028f, 0.205f, 0.014f), pal.furnitureWood);
            CreateDetailBox(ch.transform, "Ripa_Inf_D", new Vector3( 0.048f, 0.404f, -lz), new Vector3(0.028f, 0.205f, 0.014f), pal.furnitureWood);

            BoxCollider col = ch.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.285f, 0f);
            col.size = new Vector3(0.30f, 0.57f, 0.30f);
            return ch;
        }

        private static GameObject BuildPottedWiltedPlant(
            Transform parent, string name, Vector3 floorPos, float scale, GreyboxPalette pal)
        {
            GameObject pot = CreateContainer(parent, name);
            pot.transform.localPosition = floorPos;
            pot.transform.localScale = Vector3.one * scale;

            Mesh saucerMesh = CreateFacetedLatheVesselMesh("Mesh_PlantSaucer", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.125f),
                (0.008f, 0.138f),
                (0.022f, 0.142f),
                (0.022f, 0.134f),
                (0.009f, 0.124f),
                (0.009f, 0.000f)
            });
            CreateMeshObject(pot.transform, "Prato_Vaso", saucerMesh, pal.terracotta, Vector3.zero, castShadows: true);

            Mesh potMesh = CreateFacetedLatheVesselMesh("Mesh_PlantPot", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.094f),
                (0.214f, 0.122f),
                (0.218f, 0.134f),
                (0.252f, 0.136f),
                (0.254f, 0.120f),
                (0.228f, 0.114f),
                (0.228f, 0.000f)
            });
            CreateMeshObject(pot.transform, "Corpo_Vaso", potMesh, pal.terracotta, new Vector3(0f, 0.009f, 0f), castShadows: true);
            CreateDetailCylinder(pot.transform, "Terra_Seca", new Vector3(0f, 0.241f, 0f), new Vector3(0.222f, 0.004f, 0.222f), pal.baseboardWood);

            // Anel interno de hastes secas arqueadas + anel externo de frondes murchas pendentes (silhueta de samambaia)
            for (int i = 0; i < 6; i++)
            {
                float ang = i * 60f + 12f + (i % 2 == 0 ? -4f : 5f);
                float yOff = (i % 2 == 0) ? 0.245f : 0.249f;
                GameObject innerStem = CreateContainer(pot.transform, $"Ramo_Interno_{i}");
                innerStem.transform.localPosition = new Vector3(0f, yOff, 0f);
                innerStem.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                CreateDetailBox(innerStem.transform, "Folha_Arqueada", new Vector3(0f, 0.032f, 0.068f), new Vector3(0.036f, 0.010f, 0.132f), pal.foliageDead, new Vector3(-28f, 0f, 0f), castShadows: true);
            }

            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f + 22f;
                GameObject outerStem = CreateContainer(pot.transform, $"Frond_Caida_{i}");
                outerStem.transform.localPosition = new Vector3(0f, 0.243f, 0f);
                outerStem.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                CreateDetailBox(outerStem.transform, "Segmento_Base", new Vector3(0f, 0.006f, 0.074f), new Vector3(0.044f, 0.010f, 0.145f), pal.foliageDead, new Vector3(-10f, 0f, 0f), castShadows: true);
                CreateDetailBox(outerStem.transform, "Ponta_Pendente", new Vector3(0f, -0.030f, 0.174f), new Vector3(0.034f, 0.009f, 0.125f), pal.foliageDead, new Vector3(52f, 0f, 0f), castShadows: true);
            }

            // Duas folhas secas caídas fora do prato (sem sobreposição/Z-fighting)
            CreateDetailBox(pot.transform, "Folha_Caida_01", new Vector3(-0.195f, 0.004f, 0.130f), new Vector3(0.032f, 0.008f, 0.085f), pal.foliageDead, new Vector3(0f, 38f, 0f));
            CreateDetailBox(pot.transform, "Folha_Caida_02", new Vector3(0.185f, 0.004f, -0.135f), new Vector3(0.028f, 0.008f, 0.072f), pal.foliageDead, new Vector3(0f, -52f, 0f));

            return pot;
        }

        private static GameObject BuildStackedBooks(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal, int count = 3)
        {
            GameObject stack = CreateContainer(parent, name);
            stack.transform.localPosition = surfacePos;
            stack.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Material[] covers =
            {
                pal.bookBurgundy ?? pal.woodMahogany,
                pal.bookSlateBlue ?? pal.sofaFabric,
                pal.bookTeal ?? pal.baseboardWood,
                pal.bookMustard ?? pal.sofaFabric
            };
            float curY = 0f;
            float[] yaws = { 0f, -6f, 8f, -3f };
            float[] widths = { 0.21f, 0.19f, 0.20f, 0.175f };
            float[] depths = { 0.145f, 0.138f, 0.140f, 0.125f };
            float[] thicks = { 0.030f, 0.026f, 0.028f, 0.022f };

            int safeCount = Mathf.Clamp(count, 1, 4);
            for (int i = 0; i < safeCount; i++)
            {
                float h = thicks[i];
                GameObject bk = CreateContainer(stack.transform, $"Livro_{i + 1}");
                bk.transform.localPosition = new Vector3(0f, curY, 0f);
                bk.transform.localRotation = Quaternion.Euler(0f, yaws[i], 0f);
                CreateDetailBox(bk.transform, "Capa", new Vector3(0f, h * 0.5f, 0f), new Vector3(widths[i], h, depths[i]), covers[i % covers.Length], castShadows: true);
                CreateDetailBox(bk.transform, "Miolo_Paginas", new Vector3(0.006f, h * 0.5f, 0f), new Vector3(widths[i] - 0.006f, h - 0.006f, depths[i] - 0.008f), pal.paperWhite);
                CreateDetailBox(bk.transform, "Faixa_Lombada", new Vector3(-widths[i] * 0.5f - 0.001f, h * 0.5f, 0f), new Vector3(0.002f, h * 0.34f, depths[i] * 0.42f), pal.bookTaupe ?? pal.paperWhite);
                curY += h;
            }
            return stack;
        }

        private static GameObject BuildStoppedMantleClock(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject clk = CreateContainer(parent, name);
            clk.transform.localPosition = surfacePos;
            clk.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh plinthMesh = CreateChamferedRectLoftMesh("Mesh_ClockMantle_Plinth", 0.186f, 0.024f, 0.088f, 0.008f, 0.90f, 0.005f);
            CreateMeshObject(clk.transform, "Plinto_Base", plinthMesh, pal.baseboardWood, Vector3.zero, castShadows: true);

            Mesh bodyMesh = CreateChamferedRectLoftMesh("Mesh_ClockMantle_Body", 0.160f, 0.128f, 0.072f, 0.014f, 0.94f, 0.006f);
            CreateMeshObject(clk.transform, "Caixa_Relogio", bodyMesh, pal.woodMahogany, new Vector3(0f, 0.024f, 0f), castShadows: true);

            Mesh crestMesh = CreateChamferedRectLoftMesh("Mesh_ClockMantle_Crest", 0.174f, 0.014f, 0.080f, 0.010f, 0.88f, 0.004f);
            CreateMeshObject(clk.transform, "Friso_Superior", crestMesh, pal.baseboardWood, new Vector3(0f, 0.152f, 0f), castShadows: false);

            CreateDetailCylinder(clk.transform, "Coluna_Lateral_E", new Vector3(-0.066f, 0.088f, 0.036f), new Vector3(0.010f, 0.060f, 0.010f), pal.metalBrass);
            CreateDetailCylinder(clk.transform, "Coluna_Lateral_D", new Vector3(0.066f, 0.088f, 0.036f), new Vector3(0.010f, 0.060f, 0.010f), pal.metalBrass);

            // Mesmo conjunto analógico compartilhado, rotacionado +180° em Y para olhar para +Z local
            BuildAnalogClockDialAssembly(
                clk.transform,
                new Vector3(0f, 0.092f, 0.037f),
                dialRadius: 0.038f,
                pal: pal,
                out Transform hourPivot,
                out Transform minutePivot,
                out Transform secondPivot,
                yRotDegrees: 180f);

            AnalogClockController clockCtrl = clk.AddComponent<AnalogClockController>();
            clockCtrl.ConfigureClock(hourPivot, minutePivot, secondPivot, 23, 40, 18f, running: false, discreteSecond: true);

            return clk;
        }

        private static GameObject BuildUnopenedMailBundle(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal, int count = 4, Material envelopePrintMat = null, float printAngleY = 0f)
        {
            GameObject mail = CreateContainer(parent, name);
            mail.transform.localPosition = surfacePos;
            mail.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            int safeCount = Mathf.Clamp(count, 1, 5);
            for (int i = 0; i < safeCount; i++)
            {
                float y = 0.0022f + i * 0.0048f;
                float offX = (i - 1.5f) * 0.012f;
                float offZ = (i % 2 == 0 ? 1f : -1f) * 0.010f * i;
                float ang = (i - 1.5f) * 11f;
                Material envMat = (i % 2 == 0) ? pal.folderKraft : pal.paperWhite;

                CreateDetailBox(mail.transform, $"Envelope_{i + 1}", new Vector3(offX, y, offZ), new Vector3(0.215f, 0.0044f, 0.105f), envMat, new Vector3(0f, ang, 0f), castShadows: true);
                if (i == safeCount - 1)
                {
                    if (envelopePrintMat != null)
                    {
                        CreatePrintedOverlayQuad(mail.transform, $"Impressao_Envelope_{i + 1}", new Vector3(offX, y + 0.0028f, offZ), new Vector2(0.212f, 0.102f), envelopePrintMat, new Vector3(90f, ang + printAngleY, 0f));
                    }
                    else
                    {
                        CreateDetailBox(mail.transform, $"Carimbo_Vencido_{i + 1}", new Vector3(offX + 0.045f, y + 0.0033f, offZ + 0.018f), new Vector3(0.046f, 0.0022f, 0.016f), pal.calendarRed, new Vector3(0f, ang + 8f, 0f));
                    }
                }
            }
            return mail;
        }

        private static GameObject BuildBlisterPack(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal, int poppedCount = 5, float printAngleY = 0f, Material customBackMat = null)
        {
            GameObject pack = CreateContainer(parent, name);
            pack.transform.localPosition = surfacePos;
            pack.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateDetailBox(pack.transform, "Folha_Aluminio", new Vector3(0f, 0.0012f, 0f), new Vector3(0.088f, 0.0024f, 0.048f), pal.stainlessSteel, castShadows: true);
            CreatePrintedOverlayQuad(pack.transform, "Impressao_Cartela", new Vector3(0f, 0.0029f, 0f), new Vector2(0.086f, 0.046f), customBackMat ?? pal.details.blisterPackBack, new Vector3(90f, printAngleY, 0f));
            int total = 6;
            for (int i = 0; i < total; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float px = -0.026f + col * 0.026f;
                float pz = -0.011f + row * 0.022f;
                bool isEmpty = i < poppedCount;
                float h = isEmpty ? 0.003f : 0.006f;
                Material m = isEmpty ? pal.metalDark : pal.porcelainWhite;
                CreateDetailCylinder(pack.transform, $"Bolha_{i + 1}", new Vector3(px, 0.0024f + h * 0.5f, pz), new Vector3(0.014f, h * 0.5f, 0.011f), m);
            }
            return pack;
        }

        private static GameObject BuildDirtyDishAndCutlery(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject dish = CreateContainer(parent, name);
            dish.transform.localPosition = surfacePos;
            dish.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh plateMesh = CreateFacetedLatheVesselMesh("Mesh_DirtyPlate", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.068f),
                (0.006f, 0.074f),
                (0.026f, 0.114f),
                (0.029f, 0.112f),
                (0.012f, 0.074f),
                (0.010f, 0.000f)
            });
            CreateMeshObject(dish.transform, "Prato_Fundo_Mesh", plateMesh, pal.porcelainWhite, Vector3.zero, castShadows: true);

            Material brothMat = pal.coffeeDark != null ? pal.coffeeDark : pal.baseboardWood;
            CreateDetailCylinder(dish.transform, "Resto_Caldo", new Vector3(0f, 0.0115f, 0f), new Vector3(0.132f, 0.0020f, 0.132f), brothMat);
            CreateDetailBox(dish.transform, "Miojo_Fio_1", new Vector3(-0.018f, 0.0155f, 0.012f), new Vector3(0.048f, 0.004f, 0.006f), pal.bookMustard, new Vector3(0f, 34f, 0f));
            CreateDetailBox(dish.transform, "Miojo_Fio_2", new Vector3(0.014f, 0.0155f, -0.020f), new Vector3(0.042f, 0.004f, 0.006f), pal.bookMustard, new Vector3(0f, -26f, 0f));
            CreateDetailBox(dish.transform, "Miojo_Fio_3", new Vector3(0.006f, 0.0160f, 0.026f), new Vector3(0.034f, 0.004f, 0.006f), pal.bookMustard, new Vector3(0f, 72f, 0f));

            GameObject forkGrp = CreateContainer(dish.transform, "Garfo_Prato_Grp");
            forkGrp.transform.localPosition = new Vector3(0.046f, 0.026f, 0.010f);
            forkGrp.transform.localRotation = Quaternion.Euler(0f, 18f, 11f);
            CreateDetailBox(forkGrp.transform, "Garfo_Cabo", new Vector3(0.032f, 0f, 0f), new Vector3(0.095f, 0.003f, 0.009f), pal.stainlessSteel);
            CreateDetailBox(forkGrp.transform, "Garfo_Cabeca", new Vector3(-0.020f, 0f, 0f), new Vector3(0.018f, 0.003f, 0.018f), pal.stainlessSteel);
            for (int t = -1; t <= 1; t++)
            {
                CreateDetailBox(forkGrp.transform, $"Garfo_Dente_{t + 1}", new Vector3(-0.041f, 0f, t * 0.0065f), new Vector3(0.028f, 0.0028f, 0.003f), pal.stainlessSteel);
            }

            Mesh napkinMesh = CreateCrumpledPaperMesh("Mesh_CrumpledNapkin", 0.036f, 7);
            CreateMeshObject(dish.transform, "Guardanapo_Usado", napkinMesh, pal.paperWhite, new Vector3(-0.145f, 0.000f, -0.055f), new Vector3(0f, 24f, 0f), castShadows: true);

            return dish;
        }

        private static GameObject BuildSoakingPot(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject pot = CreateContainer(parent, name);
            pot.transform.localPosition = surfacePos;
            pot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh potMesh = CreateFacetedLatheVesselMesh("Mesh_SoakingPot", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.090f),
                (0.004f, 0.094f),
                (0.088f, 0.096f),
                (0.088f, 0.090f),
                (0.006f, 0.088f),
                (0.006f, 0.000f)
            });
            CreateMeshObject(pot.transform, "Panela_Inox_Mesh", potMesh, pal.stainlessSteel, Vector3.zero, castShadows: true);

            Material murkyWater = pal.wallDampStain != null ? pal.wallDampStain : pal.baseboardWood;
            CreateDetailCylinder(pot.transform, "Agua_Suja", new Vector3(0f, 0.048f, 0f), new Vector3(0.168f, 0.002f, 0.168f), murkyWater);
            CreateDetailBox(pot.transform, "Colar_Cabo_Panela", new Vector3(0.096f, 0.068f, 0f), new Vector3(0.016f, 0.024f, 0.026f), pal.stainlessSteel);
            CreateDetailBox(pot.transform, "Cabo_Baquelite", new Vector3(0.160f, 0.077f, 0f), new Vector3(0.126f, 0.015f, 0.022f), pal.shadowBlack, new Vector3(0f, 0f, -8f));
            CreateDetailCylinder(pot.transform, "Argola_Cabo_Panela", new Vector3(0.222f, 0.086f, 0f), new Vector3(0.016f, 0.006f, 0.016f), pal.stainlessSteel);

            // Colher metálica esquecida de molho dentro da panela
            GameObject ladle = CreateContainer(pot.transform, "Concha_Molho_Panela");
            ladle.transform.localPosition = new Vector3(-0.025f, 0.056f, 0.010f);
            ladle.transform.localRotation = Quaternion.Euler(14f, -32f, 44f);
            CreateDetailCylinder(ladle.transform, "Bojo_Concha", new Vector3(0f, -0.052f, 0f), new Vector3(0.044f, 0.012f, 0.044f), pal.stainlessSteel);
            CreateDetailBox(ladle.transform, "Haste_Concha", new Vector3(0f, 0.028f, 0f), new Vector3(0.012f, 0.165f, 0.005f), pal.stainlessSteel);

            return pot;
        }

        private static GameObject BuildRubberDoorstop(
            Transform parent, string name, Vector3 floorPos, GreyboxPalette pal)
        {
            GameObject stop = CreateContainer(parent, name);
            stop.transform.localPosition = floorPos;
            CreateDetailCylinder(stop.transform, "Base_Metal", new Vector3(0f, 0.011f, 0f), new Vector3(0.036f, 0.011f, 0.036f), pal.metalBrass);
            CreateDetailCylinder(stop.transform, "Anel_Borracha", new Vector3(0f, 0.026f, 0f), new Vector3(0.044f, 0.004f, 0.044f), pal.shadowBlack);
            return stop;
        }

        private static GameObject BuildDetailedRagDoll(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject doll = CreateContainer(parent, name);
            doll.transform.localPosition = surfacePos;
            doll.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Estética madura e melancólica: tecido desbotado (cinza-bege/kraft) e cabeça facetada caída de lado (zero vermelho vibrante/esferas fofas)
            Material dressMat = pal.childNightgownDusty != null ? pal.childNightgownDusty : pal.metalDark;
            Material clothMat = pal.folderKraft != null ? pal.folderKraft : pal.paperWhite;

            Mesh dressMesh = CreateFacetedLatheVesselMesh("Mesh_Vestido_Boneca", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.044f),
                (0.022f, 0.040f),
                (0.056f, 0.030f),
                (0.082f, 0.024f),
                (0.088f, 0.014f),
                (0.088f, 0.000f)
            });
            CreateMeshObject(doll.transform, "Vestido_Boneca", dressMesh, dressMat, Vector3.zero, castShadows: true);

            Mesh limbMesh = CreateLoftedEllipticalMesh("Mesh_Membro_Boneca", 6, new[]
            {
                (0.000f, 0f, 0f, 0.0090f, 0.0090f),
                (0.030f, 0f, 0f, 0.0105f, 0.0105f),
                (0.060f, 0f, 0f, 0.0080f, 0.0080f)
            });
            CreateMeshObject(doll.transform, "Perna_Esq", limbMesh, clothMat, new Vector3(-0.020f, 0.0105f, 0.018f), new Vector3(90f, -12f, 0f), castShadows: true);
            CreateMeshObject(doll.transform, "Perna_Dir", limbMesh, clothMat, new Vector3( 0.020f, 0.0105f, 0.016f), new Vector3(90f,  16f, 0f), castShadows: true);
            CreateMeshObject(doll.transform, "Braco_Esq", limbMesh, clothMat, new Vector3(-0.030f, 0.074f, 0.006f), new Vector3(24f, 0f, 150f), castShadows: true);
            CreateMeshObject(doll.transform, "Braco_Dir", limbMesh, clothMat, new Vector3( 0.030f, 0.072f, 0.008f), new Vector3(18f, 0f, -148f), castShadows: true);

            Mesh headMesh = CreateAngularHeadMesh("Mesh_RagDollHead", 0.028f, 0.032f, 0.026f);
            GameObject head = CreateMeshObject(doll.transform, "Cabeca_Pano", headMesh, clothMat, new Vector3(0f, 0.108f, 0f), new Vector3(12f, 0f, -24f), castShadows: true);
            Mesh dollHairMesh = CreateLoftedEllipticalMesh("Mesh_Cabelo_Boneca", 8, new[]
            {
                (-0.014f, 0f, -0.008f, 0.031f, 0.026f),
                ( 0.010f, 0f, -0.006f, 0.032f, 0.027f),
                ( 0.030f, 0f, -0.002f, 0.026f, 0.023f),
                ( 0.036f, 0f,  0.000f, 0.014f, 0.014f)
            });
            CreateMeshObject(head.transform, "Cabelo_Fios_Caidos", dollHairMesh, pal.metalDark, Vector3.zero, new Vector3(-8f, 0f, 0f), castShadows: false);
            CreateDetailBox(head.transform, "Costura_Olho_Esq", new Vector3(-0.012f, 0.002f, 0.0185f), new Vector3(0.009f, 0.0025f, 0.003f), pal.shadowBlack, new Vector3(0f, -31f, 18f));
            CreateDetailBox(head.transform, "Costura_Olho_Dir", new Vector3(0.012f, 0.001f, 0.0185f), new Vector3(0.009f, 0.0025f, 0.003f), pal.shadowBlack, new Vector3(0f, 31f, -15f));

            return doll;
        }

        private static GameObject BuildRoutedWallCable(
            Transform parent, string name, string meshAssetName, Vector3[] worldPts, Material cableMat, float radius = 0.0046f)
        {
            Mesh cordMesh = CreatePolylineCordMesh(meshAssetName, worldPts, radius, sides: 6);
            return CreateMeshObject(parent, name, cordMesh, cableMat, worldPts[0], Vector3.zero, castShadows: false);
        }

        private static GameObject BuildDetailedAshtray(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject tray = CreateContainer(parent, name);
            tray.transform.localPosition = surfacePos;
            tray.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh trayMesh = CreateFacetedLatheVesselMesh("Mesh_CeramicAshtray", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.056f),
                (0.024f, 0.066f),
                (0.025f, 0.052f),
                (0.008f, 0.046f),
                (0.008f, 0.000f)
            });
            Material ceramicMat = pal.bookTaupe ?? pal.porcelainWhite;
            CreateMeshObject(tray.transform, "Corpo_Cinzeiro", trayMesh, ceramicMat, Vector3.zero, Vector3.zero, castShadows: true);
            CreateDetailCylinder(tray.transform, "Cinza_Fundo", new Vector3(0f, 0.010f, 0f), new Vector3(0.072f, 0.002f, 0.072f), pal.fabricAsh ?? pal.metalDark);
            for (int notch = 0; notch < 2; notch++)
            {
                float ang = notch * 90f;
                GameObject nHolder = CreateContainer(tray.transform, $"Apoio_Cigarro_{notch}");
                nHolder.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                CreateDetailBox(nHolder.transform, "Batente_A", new Vector3(-0.011f, 0.025f, 0.058f), new Vector3(0.008f, 0.005f, 0.018f), ceramicMat);
                CreateDetailBox(nHolder.transform, "Batente_B", new Vector3(0.011f, 0.025f, 0.058f), new Vector3(0.008f, 0.005f, 0.018f), ceramicMat);
            }

            // Bituca apoiada no sulco do cinzeiro
            GameObject cig = CreateContainer(tray.transform, "Bituca_Cigarro");
            cig.transform.localPosition = new Vector3(0.004f, 0.022f, 0.040f);
            cig.transform.localRotation = Quaternion.Euler(-16f, 6f, 0f);
            CreateDetailCylinder(cig.transform, "Filtro_Cortica", new Vector3(0f, 0f, 0.014f), new Vector3(0.008f, 0.010f, 0.008f), pal.folderKraft, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(cig.transform, "Papel_Cigarro", new Vector3(0f, 0f, -0.008f), new Vector3(0.0076f, 0.012f, 0.0076f), pal.paperWhite, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(cig.transform, "Ponta_Apagada", new Vector3(0f, 0f, -0.022f), new Vector3(0.0074f, 0.003f, 0.0074f), pal.shadowBlack, new Vector3(90f, 0f, 0f));

            return tray;
        }

        private static GameObject BuildOpenBook(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal, Material coverMat = null)
        {
            GameObject book = CreateContainer(parent, name);
            book.transform.localPosition = surfacePos;
            book.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Material cover = coverMat ?? pal.bookSlateBlue ?? pal.sofaFabric;
            Material ribbon = pal.bookBurgundy ?? pal.woodMahogany;

            CreateDetailBox(book.transform, "Lombada_Aberta", new Vector3(0f, 0.004f, 0f), new Vector3(0.024f, 0.008f, 0.218f), cover, castShadows: true);
            CreateDetailBox(book.transform, "Capa_Esq", new Vector3(-0.072f, 0.010f, 0f), new Vector3(0.144f, 0.005f, 0.218f), cover, new Vector3(0f, 0f, -7f), castShadows: true);
            CreateDetailBox(book.transform, "Capa_Dir", new Vector3(0.072f, 0.010f, 0f), new Vector3(0.144f, 0.005f, 0.218f), cover, new Vector3(0f, 0f, 7f), castShadows: true);

            CreateDetailBox(book.transform, "Paginas_Esq", new Vector3(-0.068f, 0.018f, 0f), new Vector3(0.130f, 0.013f, 0.204f), pal.paperWhite, new Vector3(0f, 0f, -6f), castShadows: true);
            CreateDetailBox(book.transform, "Paginas_Dir", new Vector3(0.068f, 0.018f, 0f), new Vector3(0.130f, 0.013f, 0.204f), pal.paperWhite, new Vector3(0f, 0f, 6f), castShadows: true);
            CreateDetailBox(book.transform, "Folha_Curva_Esq", new Vector3(-0.056f, 0.027f, 0f), new Vector3(0.116f, 0.003f, 0.200f), pal.paperWhite, new Vector3(0f, 0f, -12f));
            CreateDetailBox(book.transform, "Folha_Curva_Dir", new Vector3(0.056f, 0.027f, 0f), new Vector3(0.116f, 0.003f, 0.200f), pal.paperWhite, new Vector3(0f, 0f, 12f));
            CreateDetailBox(book.transform, "Marcador_Fita_Livro", new Vector3(0.004f, 0.015f, -0.022f), new Vector3(0.010f, 0.002f, 0.240f), ribbon, new Vector3(0f, 6f, 0f));

            return book;
        }

        private static GameObject BuildPourOverCoffeeSet(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject root = CreateContainer(parent, name);
            root.transform.localPosition = surfacePos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Jarra de vidro de café passado apoiada na base
            Mesh carafeMesh = CreateFacetedLatheVesselMesh("Mesh_CoffeeCarafe", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.064f),
                (0.032f, 0.072f),
                (0.105f, 0.058f),
                (0.138f, 0.044f),
                (0.150f, 0.048f),
                (0.150f, 0.043f),
                (0.104f, 0.053f),
                (0.010f, 0.062f),
                (0.010f, 0.000f)
            });
            CreateMeshObject(root.transform, "Jarra_Cafe_Vidro", carafeMesh, pal.details.clearGlass, Vector3.zero, Vector3.zero, castShadows: false);
            CreateDetailCylinder(root.transform, "Fundo_Cafe_Jarra", new Vector3(0f, 0.034f, 0f), new Vector3(0.108f, 0.020f, 0.108f), pal.coffeeDark);
            CreateDetailCylinder(root.transform, "Cinta_Cabo_Jarra", new Vector3(0f, 0.136f, 0f), new Vector3(0.096f, 0.007f, 0.096f), pal.metalDark);
            Mesh carafeHandle = CreateFacetedCShapeHandleMesh("Mesh_CarafeHandle", new[]
            {
                new Vector3(0.047f, 0.136f, 0f),
                new Vector3(0.088f, 0.124f, 0f),
                new Vector3(0.092f, 0.075f, 0f),
                new Vector3(0.068f, 0.034f, 0f)
            }, 0.007f, 0.008f);
            CreateMeshObject(root.transform, "Alca_Jarra_Cafe", carafeHandle, pal.metalDark, Vector3.zero, Vector3.zero, castShadows: true);

            // Coador cônico apoiado limpo sobre a boca da jarra (Y=0.151 > 0.150) + filtro de papel oco e borra recuada sem faces coplanares
            Mesh dripperMesh = CreateFacetedLatheVesselMesh("Mesh_CoffeeDripper", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.054f),
                (0.010f, 0.054f),
                (0.010f, 0.020f),
                (0.106f, 0.068f),
                (0.106f, 0.061f),
                (0.016f, 0.016f),
                (0.016f, 0.000f)
            });
            CreateMeshObject(root.transform, "Coador_Plastico_Cone", dripperMesh, pal.plasticBeige90s, new Vector3(0f, 0.151f, 0f), Vector3.zero, castShadows: true);
            Mesh filterConeMesh = CreateFacetedLatheVesselMesh("Mesh_CoffeeFilterPaper", 12, new[]
            {
                (0.020f, 0.000f),
                (0.020f, 0.014f),
                (0.106f, 0.056f),
                (0.118f, 0.062f),
                (0.118f, 0.058f),
                (0.025f, 0.011f),
                (0.025f, 0.000f)
            });
            CreateMeshObject(root.transform, "Filtro_Papel_Cone", filterConeMesh, pal.folderKraft, new Vector3(0f, 0.151f, 0f), Vector3.zero, castShadows: true);
            CreateDetailCylinder(root.transform, "Borra_Cafe_Filtro", new Vector3(0f, 0.223f, 0f), new Vector3(0.058f, 0.005f, 0.058f), pal.coffeeDark);

            // Pote de café de louça ao lado com tampa de madeira selada e colher dosadora aterrada em Y=0
            CreateDetailCylinder(root.transform, "Pote_Cafe_Bancada", new Vector3(-0.14f, 0.065f, 0.03f), new Vector3(0.096f, 0.065f, 0.096f), pal.porcelainWhite, castShadows: true);
            CreateDetailCylinder(root.transform, "Faixa_Pote_Cafe", new Vector3(-0.14f, 0.072f, 0.03f), new Vector3(0.099f, 0.024f, 0.099f), pal.bookMustard);
            CreateDetailCylinder(root.transform, "Colar_Tampa_Pote_Cafe", new Vector3(-0.14f, 0.128f, 0.03f), new Vector3(0.098f, 0.004f, 0.098f), pal.metalDark);
            CreateDetailCylinder(root.transform, "Tampa_Pote_Cafe", new Vector3(-0.14f, 0.136f, 0.03f), new Vector3(0.104f, 0.010f, 0.104f), pal.woodLight);
            CreateDetailCylinder(root.transform, "Puxador_Pote_Cafe", new Vector3(-0.14f, 0.152f, 0.03f), new Vector3(0.024f, 0.008f, 0.024f), pal.woodMahogany);

            GameObject scoop = CreateContainer(root.transform, "Colher_Dosadora_Cafe");
            scoop.transform.localPosition = new Vector3(-0.08f, 0f, 0.09f);
            scoop.transform.localRotation = Quaternion.Euler(0f, -24f, 0f);
            CreateDetailBox(scoop.transform, "Cabo_Colher_Cafe", new Vector3(0f, 0.003f, -0.022f), new Vector3(0.014f, 0.006f, 0.086f), pal.stainlessSteel);
            CreateDetailCylinder(scoop.transform, "Bojo_Colher_Cafe", new Vector3(0f, 0.005f, 0.036f), new Vector3(0.030f, 0.005f, 0.036f), pal.stainlessSteel);
            CreateDetailCylinder(scoop.transform, "Po_Colher_Cafe", new Vector3(0f, 0.009f, 0.036f), new Vector3(0.022f, 0.003f, 0.022f), pal.coffeeDark);

            return root;
        }

        private static GameObject BuildDeskStapler(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject st = CreateContainer(parent, name);
            st.transform.localPosition = surfacePos;
            st.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Material shellMat = pal.bookSlateBlue ?? pal.metalDark;
            Mesh staplerBaseMesh = CreateChamferedRectLoftMesh("Mesh_Grampeador_Base", 0.036f, 0.010f, 0.148f, 0.006f, 0.90f, 0.002f);
            CreateMeshObject(st.transform, "Base_Borracha_Grampeador", staplerBaseMesh, pal.shadowBlack, Vector3.zero, castShadows: true);
            CreateDetailBox(st.transform, "Chapa_Anvil_Metal", new Vector3(0f, 0.0115f, 0.042f), new Vector3(0.028f, 0.003f, 0.048f), pal.stainlessSteel);
            CreateDetailBox(st.transform, "Dobradica_Grampeador", new Vector3(0f, 0.020f, -0.062f), new Vector3(0.032f, 0.022f, 0.022f), pal.metalDark);
            Mesh staplerBodyMesh = CreateChamferedRectLoftMesh("Mesh_Grampeador_Corpo", 0.032f, 0.018f, 0.132f, 0.006f, 0.88f, 0.004f);
            CreateMeshObject(st.transform, "Corpo_Articulado_Grampeador", staplerBodyMesh, shellMat, new Vector3(0f, 0.023f, 0.002f), new Vector3(-3.5f, 0f, 0f), castShadows: true);
            Mesh staplerTopMesh = CreateChamferedRectLoftMesh("Mesh_Grampeador_Tampa", 0.034f, 0.009f, 0.112f, 0.007f, 0.84f, 0.003f);
            CreateMeshObject(st.transform, "Tampa_Prensa_Grampeador", staplerTopMesh, shellMat, new Vector3(0f, 0.039f, 0.010f), new Vector3(-3.5f, 0f, 0f), castShadows: false);
            CreateDetailBox(st.transform, "Trilho_Grampos_Aco", new Vector3(0f, 0.023f, 0.018f), new Vector3(0.022f, 0.008f, 0.092f), pal.stainlessSteel, new Vector3(-3.5f, 0f, 0f));
            CreateDetailBox(st.transform, "Bico_Saida_Grampo", new Vector3(0f, 0.024f, 0.062f), new Vector3(0.026f, 0.010f, 0.014f), pal.stainlessSteel);

            return st;
        }

        private static GameObject BuildDeskNotepad(
            Transform parent, string name, Vector3 surfacePos, float yaw, GreyboxPalette pal)
        {
            GameObject pad = CreateContainer(parent, name);
            pad.transform.localPosition = surfacePos;
            pad.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Material bindMat = pal.bookBurgundy ?? pal.baseboardWood;
            CreateDetailBox(pad.transform, "Base_Cartao_Bloco", new Vector3(0f, 0.0018f, 0f), new Vector3(0.128f, 0.0036f, 0.172f), pal.folderKraft, castShadows: true);
            CreateDetailBox(pad.transform, "Paginas_Bloco_Notas", new Vector3(0f, 0.0092f, 0f), new Vector3(0.124f, 0.011f, 0.168f), pal.paperWhite, castShadows: true);
            CreateDetailBox(pad.transform, "Faixa_Encadernacao_Bloco", new Vector3(0f, 0.0155f, 0.074f), new Vector3(0.126f, 0.004f, 0.020f), bindMat);
            for (int r = -2; r <= 2; r++)
            {
                CreateDetailCylinder(pad.transform, $"Espiral_Bloco_{r + 2}", new Vector3(r * 0.022f, 0.016f, 0.084f), new Vector3(0.006f, 0.005f, 0.006f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            }
            CreateDetailBox(pad.transform, "Folha_Topo_Solta", new Vector3(0.002f, 0.0158f, -0.008f), new Vector3(0.118f, 0.0018f, 0.144f), pal.paperWhite, new Vector3(0f, 2.5f, 0f));
            CreateDetailCylinder(pad.transform, "Caneta_Bloco_Desk", new Vector3(0.032f, 0.021f, -0.010f), new Vector3(0.008f, 0.068f, 0.008f), pal.bookSlateBlue ?? pal.sofaFabric, new Vector3(90f, -22f, 0f));

            return pad;
        }

        // =====================================================================
        // PROPS DE ALTO ACABAMENTO DO QUARTO, PORTAS, JANELAS E COMPARTILHADOS
        // =====================================================================

        private static GameObject BuildDetailedClosedBook(
            Transform parent,
            string name,
            Vector3 surfacePos,
            float yaw,
            Vector3 sizeXYZ,
            Material coverMat,
            Material ribbonMat,
            GreyboxPalette pal)
        {
            GameObject bk = CreateContainer(parent, name);
            bk.transform.localPosition = surfacePos;
            bk.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = sizeXYZ.x;
            float h = sizeXYZ.y;
            float d = sizeXYZ.z;
            float boardT = Mathf.Clamp(h * 0.14f, 0.003f, 0.0055f);

            // Capas superior e inferior com beiral sutil sobre o miolo de páginas
            CreateDetailBox(bk.transform, "Capa_Inferior", new Vector3(0f, boardT * 0.5f, 0f), new Vector3(w, boardT, d), coverMat, castShadows: true);
            CreateDetailBox(bk.transform, "Capa_Superior", new Vector3(0f, h - boardT * 0.5f, 0f), new Vector3(w, boardT, d), coverMat, castShadows: true);
            CreateDetailBox(bk.transform, "Lombada_Livro", new Vector3(-w * 0.5f + boardT * 0.7f, h * 0.5f, 0f), new Vector3(boardT * 1.4f, h, d), coverMat, castShadows: true);
            CreateDetailBox(bk.transform, "Miolo_Paginas", new Vector3(boardT * 0.6f, h * 0.5f, 0f), new Vector3(w - boardT * 1.8f, h - boardT * 2.02f, d - 0.008f), pal.paperWhite);
            CreateDetailBox(bk.transform, "Faixa_Lombada", new Vector3(-w * 0.5f - 0.0008f, h * 0.5f, 0f), new Vector3(0.0016f, h * 0.55f, d * 0.44f), pal.metalBrass);
            if (ribbonMat != null)
            {
                CreateDetailBox(bk.transform, "Fita_Marcador", new Vector3(w * 0.14f, boardT + 0.002f, d * 0.5f + 0.010f), new Vector3(0.008f, 0.002f, 0.026f), ribbonMat, new Vector3(0f, 12f, 0f));
            }
            return bk;
        }

        private static GameObject BuildBedroomSlipper(
            Transform parent,
            string name,
            Vector3 floorPos,
            float yaw,
            bool isRightFoot,
            GreyboxPalette pal)
        {
            GameObject sl = CreateContainer(parent, name);
            sl.transform.localPosition = floorPos;
            sl.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float sx = isRightFoot ? 1f : -1f;
            Vector2[] solePoly =
            {
                new Vector2(0.000f * sx, -0.128f),
                new Vector2(0.036f * sx, -0.114f),
                new Vector2(0.042f * sx, -0.055f),
                new Vector2(0.035f * sx,  0.000f),
                new Vector2(0.048f * sx,  0.064f),
                new Vector2(0.044f * sx,  0.110f),
                new Vector2(0.014f * sx,  0.130f),
                new Vector2(-0.022f * sx, 0.125f),
                new Vector2(-0.044f * sx, 0.092f),
                new Vector2(-0.040f * sx, 0.020f),
                new Vector2(-0.036f * sx,-0.048f),
                new Vector2(-0.035f * sx,-0.106f)
            };
            Mesh soleMesh = CreateClosedPolygonLoftMesh(
                $"Mesh_Solado_Chinelo_{(isRightFoot ? "Dir" : "Esq")}",
                solePoly,
                new[]
                {
                    (0.000f, 0.94f, 0.96f, 0f, 0f),
                    (0.005f, 1.00f, 1.00f, 0f, 0f),
                    (0.016f, 1.00f, 1.00f, 0f, 0f),
                    (0.019f, 0.94f, 0.96f, 0f, 0f)
                });
            CreateMeshObject(sl.transform, "Solado_Chinelo", soleMesh, pal.baseboardWood, Vector3.zero, castShadows: true);

            Mesh strapMesh = CreateFacetedCShapeHandleMesh(
                "Mesh_Tira_Chinelo",
                new[]
                {
                    new Vector3(-0.040f, 0.014f, 0.028f),
                    new Vector3(-0.034f, 0.036f, 0.030f),
                    new Vector3( 0.000f, 0.046f, 0.034f),
                    new Vector3( 0.034f, 0.036f, 0.030f),
                    new Vector3( 0.040f, 0.014f, 0.028f)
                },
                halfThicknessY: 0.0045f,
                halfThicknessZ: 0.032f);
            CreateMeshObject(sl.transform, "Tira_Chinelo", strapMesh, pal.metalDark, Vector3.zero, castShadows: true);
            return sl;
        }

        private static GameObject BuildReadingGlasses(
            Transform parent,
            string name,
            Vector3 surfacePos,
            float yaw,
            GreyboxPalette pal)
        {
            GameObject gl = CreateContainer(parent, name);
            gl.transform.localPosition = surfacePos;
            gl.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Mesh rimMesh = CreateClosedRingTubeMesh("Mesh_Aro_Oculos", 8, 4, 0.0185f, 0.0022f, 0.0024f);
            float lensY = 0.021f;

            // Aros abertos com lentes transparentes em M_Clear_Glass (sem sombras nem colisor)
            CreateMeshObject(gl.transform, "Oculos_Aro_Esq", rimMesh, pal.metalBrass,
                new Vector3(-0.025f, lensY, -0.024f), new Vector3(90f, 0f, 0f), castShadows: false);
            CreateDetailCylinder(gl.transform, "Lente_Oculos_Esq",
                new Vector3(-0.025f, lensY, -0.024f), new Vector3(0.034f, 0.0006f, 0.034f), pal.details.clearGlass, new Vector3(90f, 0f, 0f));

            CreateMeshObject(gl.transform, "Oculos_Aro_Dir", rimMesh, pal.metalBrass,
                new Vector3(0.025f, lensY, -0.024f), new Vector3(90f, 0f, 0f), castShadows: false);
            CreateDetailCylinder(gl.transform, "Lente_Oculos_Dir",
                new Vector3(0.025f, lensY, -0.024f), new Vector3(0.034f, 0.0006f, 0.034f), pal.details.clearGlass, new Vector3(90f, 0f, 0f));

            // Ponte nasal arqueada + plaquetas + dobradiças + hastes apoiadas até Y=0.000
            CreateDetailBox(gl.transform, "Oculos_Ponte", new Vector3(0f, lensY + 0.006f, -0.024f), new Vector3(0.016f, 0.003f, 0.003f), pal.metalBrass);
            CreateDetailBox(gl.transform, "Plaqueta_Nasal_E", new Vector3(-0.007f, lensY - 0.002f, -0.020f), new Vector3(0.003f, 0.006f, 0.002f), pal.porcelainWhite);
            CreateDetailBox(gl.transform, "Plaqueta_Nasal_D", new Vector3( 0.007f, lensY - 0.002f, -0.020f), new Vector3(0.003f, 0.006f, 0.002f), pal.porcelainWhite);
            CreateDetailBox(gl.transform, "Dobradica_Oculos_E", new Vector3(-0.045f, lensY + 0.004f, -0.021f), new Vector3(0.004f, 0.004f, 0.006f), pal.metalBrass);
            CreateDetailBox(gl.transform, "Dobradica_Oculos_D", new Vector3( 0.045f, lensY + 0.004f, -0.021f), new Vector3(0.004f, 0.004f, 0.006f), pal.metalBrass);

            // Hastes levemente inclinadas para tocar o tampo na traseira e frente da lente em Y=0.000
            CreateDetailBox(gl.transform, "Oculos_Haste_Esq", new Vector3(-0.045f, 0.013f, 0.020f), new Vector3(0.003f, 0.004f, 0.086f), pal.metalDark, new Vector3(14f, 3f, 0f));
            CreateDetailBox(gl.transform, "Oculos_Haste_Dir", new Vector3( 0.045f, 0.013f, 0.020f), new Vector3(0.003f, 0.004f, 0.086f), pal.metalDark, new Vector3(14f, -3f, 0f));
            CreateDetailBox(gl.transform, "Ponteira_Haste_E", new Vector3(-0.043f, 0.002f, 0.064f), new Vector3(0.0035f, 0.004f, 0.022f), pal.baseboardWood, new Vector3(-4f, 3f, 0f));
            CreateDetailBox(gl.transform, "Ponteira_Haste_D", new Vector3( 0.043f, 0.002f, 0.064f), new Vector3(0.0035f, 0.004f, 0.022f), pal.baseboardWood, new Vector3(-4f, -3f, 0f));

            // Base de apoio das lentes (ponto mais baixo da lente tocando o tampo em Y=0.000)
            SnapToSupportWorldY(gl, parent.TransformPoint(surfacePos).y);
            return gl;
        }

        private static GameObject BuildPorcelainRingDishAndRing(
            Transform parent,
            Vector3 dishPos,
            GreyboxPalette pal)
        {
            Mesh dishMesh = CreateFacetedLatheVesselMesh("Mesh_Pratinho_Porcelana", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.032f),
                (0.005f, 0.046f),
                (0.013f, 0.052f),
                (0.014f, 0.048f),
                (0.006f, 0.038f),
                (0.005f, 0.000f)
            });
            GameObject dish = CreateMeshObject(parent, "Pratinho_Joia_Margaret", dishMesh, pal.porcelainWhite, dishPos, castShadows: true);

            Mesh ringMesh = CreateClosedRingTubeMesh("Mesh_Alianca_Ouro", 12, 6, 0.010f, 0.0018f, 0.0022f);
            GameObject ring = CreateMeshObject(parent, "Alianca_Guardada", ringMesh, pal.metalBrass,
                new Vector3(dishPos.x + 0.006f, dishPos.y + 0.0072f, dishPos.z - 0.004f),
                new Vector3(4f, 18f, -5f), castShadows: false);
            return dish;
        }

        private static GameObject BuildBedroomTableLamp(
            Transform parent,
            Vector3 localOffset,
            GreyboxPalette pal)
        {
            GameObject lampMarg = CreateContainer(parent, "Abajur_Margaret");

            // Urna torneada apoiada em Y=0.560m (tampo do criado-mudo), mantendo bulbo exato em +0.745f!
            Mesh urnMesh = CreateFacetedLatheVesselMesh("Mesh_Abajur_Urna", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.052f),
                (0.014f, 0.056f),
                (0.038f, 0.064f),
                (0.072f, 0.052f),
                (0.095f, 0.022f),
                (0.108f, 0.024f),
                (0.108f, 0.000f)
            });
            CreateMeshObject(lampMarg.transform, "Abajur_Margaret_Base", urnMesh, pal.porcelainWhite, localOffset, castShadows: false);
            CreateDetailCylinder(lampMarg.transform, "Abajur_Margaret_Haste",
                localOffset + new Vector3(0f, 0.132f, 0f),
                new Vector3(0.020f, 0.030f, 0.020f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(lampMarg.transform, "Soquete_Abajur_Margaret",
                localOffset + new Vector3(0f, 0.165f, 0f),
                new Vector3(0.030f, 0.014f, 0.030f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(lampMarg.transform, "Correntinha_Abajur",
                localOffset + new Vector3(-0.026f, 0.145f, -0.014f),
                new Vector3(0.003f, 0.026f, 0.003f), pal.metalBrass, castShadows: false);

            Mesh shadeMesh = CreateFacetedLatheVesselMesh("Mesh_Abajur_Cupula_Conica", 14, new[]
            {
                (0.000f, 0.114f),
                (0.118f, 0.068f),
                (0.118f, 0.064f),
                (0.000f, 0.110f),
                (0.000f, 0.114f)
            });
            Vector3 shadeOrigin = localOffset + new Vector3(0f, 0.134f, 0f);
            Vector3 bulbPos = localOffset + new Vector3(0f, 0.185f, 0f); // y = 0.560 + 0.185 = 0.745 (invariante do Teste de Iluminação!)

            GameObject abajurOn = CreateContainer(lampMarg.transform, "Estado_Ligado");
            CreateMeshObject(abajurOn.transform, "Abajur_Margaret_Cupula", shadeMesh, pal.lampEmissive, shadeOrigin, castShadows: false);
            CreateDetailSphere(abajurOn.transform, "Bulbo_Abajur_Aceso", bulbPos, new Vector3(0.052f, 0.072f, 0.052f), pal.lampBulbCore, castShadows: false);

            GameObject abajurOff = CreateContainer(lampMarg.transform, "Estado_Desligado");
            CreateMeshObject(abajurOff.transform, "Abajur_Margaret_Cupula_Apagada", shadeMesh, pal.lampUnlitGlass, shadeOrigin, castShadows: false);
            CreateDetailSphere(abajurOff.transform, "Bulbo_Abajur_Apagado", bulbPos, new Vector3(0.052f, 0.072f, 0.052f), pal.metalDark, castShadows: false);
            abajurOff.SetActive(false);

            return lampMarg;
        }

        private static void BuildWardrobeInteriorProps(Transform wardrobeRoot, GreyboxPalette pal)
        {
            // Cabide triangular de madeira + gancho curvado ao redor do varão em Y=1.68
            Mesh hangerBodyMesh = CreateFacetedCShapeHandleMesh(
                "Mesh_Cabide_Madeira",
                new[]
                {
                    new Vector3(-0.185f, -0.056f, 0f),
                    new Vector3(-0.088f, -0.018f, 0f),
                    new Vector3( 0.000f,  0.006f, 0f),
                    new Vector3( 0.088f, -0.018f, 0f),
                    new Vector3( 0.185f, -0.056f, 0f)
                },
                halfThicknessY: 0.008f,
                halfThicknessZ: 0.007f);
            Mesh hookMesh = CreateFacetedCShapeHandleMesh(
                "Mesh_Cabide_Gancho",
                new[]
                {
                    new Vector3(0f, 0.004f,  0.000f),
                    new Vector3(0f, 0.050f,  0.000f),
                    new Vector3(0f, 0.072f, -0.015f),
                    new Vector3(0f, 0.083f,  0.000f),
                    new Vector3(0f, 0.073f,  0.015f),
                    new Vector3(0f, 0.060f,  0.016f)
                },
                halfThicknessY: 0.0028f,
                halfThicknessZ: 0.0028f,
                sideAxis: Vector3.right);

            float[] hangerZs = { -0.54f, -0.28f, 0.44f };
            float[] hangerYaws = { 4f, -3f, -6f };
            for (int h = 0; h < hangerZs.Length; h++)
            {
                GameObject hRoot = CreateContainer(wardrobeRoot, $"Cabide_{h}");
                hRoot.transform.localPosition = new Vector3(-0.03f, 1.612f, hangerZs[h]);
                hRoot.transform.localRotation = Quaternion.Euler(0f, hangerYaws[h], 0f);
                CreateMeshObject(hRoot.transform, $"Gancho_Cabide_{h}", hookMesh, pal.stainlessSteel, Vector3.zero, castShadows: false);
                CreateMeshObject(hRoot.transform, $"Ombro_Cabide_{h}", hangerBodyMesh, pal.woodLight, Vector3.zero, castShadows: false);
            }

            // Paletós e casaco de Margaret modelados com silhueta de ombros e caimento
            Mesh suitMesh = CreateClosedPolygonLoftMesh(
                "Mesh_Paleto_Cabide",
                new[]
                {
                    new Vector2( 0.19f, -0.07f),
                    new Vector2( 0.21f,  0.00f),
                    new Vector2( 0.19f,  0.07f),
                    new Vector2(-0.19f,  0.07f),
                    new Vector2(-0.21f,  0.00f),
                    new Vector2(-0.19f, -0.07f)
                },
                new[]
                {
                    (0.000f, 0.88f, 0.82f, 0f, 0f),
                    (0.460f, 0.94f, 0.96f, 0f, 0f),
                    (0.760f, 1.00f, 1.00f, 0f, 0f),
                    (0.865f, 0.92f, 0.88f, 0f, 0f),
                    (0.890f, 0.42f, 0.56f, 0f, 0f)
                });
            CreateMeshObject(wardrobeRoot, "Terno_Escuro_1", suitMesh, pal.metalDark, new Vector3(-0.03f, 0.720f, -0.54f), new Vector3(0f, 4f, 0f), castShadows: true);
            CreateDetailBox(wardrobeRoot, "Lapela_Terno_1", new Vector3(0.055f, 1.46f, -0.54f), new Vector3(0.025f, 0.24f, 0.082f), pal.shadowBlack, new Vector3(0f, 4f, -10f));

            CreateMeshObject(wardrobeRoot, "Terno_Escuro_2", suitMesh, pal.cianTrousersDark, new Vector3(-0.03f, 0.730f, -0.28f), new Vector3(0f, -3f, 0f), castShadows: true);
            CreateDetailBox(wardrobeRoot, "Lapela_Terno_2", new Vector3(0.055f, 1.47f, -0.28f), new Vector3(0.025f, 0.24f, 0.082f), pal.metalDark, new Vector3(0f, -3f, -10f));

            Mesh coatMesh = CreateClosedPolygonLoftMesh(
                "Mesh_Casaco_Longo",
                new[]
                {
                    new Vector2( 0.18f, -0.10f),
                    new Vector2( 0.20f,  0.00f),
                    new Vector2( 0.18f,  0.10f),
                    new Vector2(-0.18f,  0.10f),
                    new Vector2(-0.20f,  0.00f),
                    new Vector2(-0.18f, -0.10f)
                },
                new[]
                {
                    (0.000f, 1.08f, 1.12f, 0f, 0f),
                    (0.480f, 0.94f, 1.00f, 0f, 0f),
                    (0.800f, 1.00f, 0.98f, 0f, 0f),
                    (0.910f, 0.90f, 0.86f, 0f, 0f),
                    (0.945f, 0.42f, 0.54f, 0f, 0f)
                });
            CreateMeshObject(wardrobeRoot, "Casaco_Cinza_Margaret_Mesh", coatMesh, pal.margaretCoatWool, new Vector3(-0.03f, 0.665f, 0.44f), new Vector3(0f, -6f, 0f), castShadows: true);
            CreateDetailBox(wardrobeRoot, "Gola_Casaco_Margaret", new Vector3(0.045f, 1.545f, 0.44f), new Vector3(0.11f, 0.12f, 0.18f), pal.porcelainWhite, new Vector3(0f, -6f, -12f));
            CreateDetailBox(wardrobeRoot, "Bolso_Casaco_Margaret", new Vector3(0.098f, 1.05f, 0.51f), new Vector3(0.065f, 0.095f, 0.016f), pal.fabricAsh, new Vector3(0f, -6f, 0f));
            for (int cb = 0; cb < 3; cb++)
            {
                CreateDetailCylinder(wardrobeRoot, $"Botao_Casaco_Margaret_{cb}", new Vector3(0.155f, 1.38f - cb * 0.14f, 0.44f), new Vector3(0.016f, 0.004f, 0.016f), pal.metalDark, new Vector3(0f, 0f, 90f));
            }

            // Sapatos sociais masculinos esculpidos sobre Base_GuardaRoupa (topo em Y=0.120m)
            Mesh shoeMesh = CreateClosedPolygonLoftMesh(
                "Mesh_Sapato_Social",
                new[]
                {
                    new Vector2(-0.130f, -0.040f),
                    new Vector2(-0.065f, -0.044f),
                    new Vector2( 0.025f, -0.040f),
                    new Vector2( 0.095f, -0.038f),
                    new Vector2( 0.138f, -0.016f),
                    new Vector2( 0.138f,  0.016f),
                    new Vector2( 0.095f,  0.038f),
                    new Vector2( 0.025f,  0.040f),
                    new Vector2(-0.065f,  0.044f),
                    new Vector2(-0.130f,  0.040f)
                },
                new[]
                {
                    (0.000f, 1.00f, 1.00f, 0f, 0f),
                    (0.014f, 1.02f, 1.02f, 0f, 0f),
                    (0.044f, 0.92f, 0.88f, -0.012f, 0f),
                    (0.070f, 0.52f, 0.72f, -0.055f, 0f)
                });
            CreateMeshObject(wardrobeRoot, "Sapato_Social_E", shoeMesh, pal.shadowBlack, new Vector3(0.02f, 0.120f, -0.48f), new Vector3(0f, 8f, 0f), castShadows: true);
            CreateMeshObject(wardrobeRoot, "Sapato_Social_D", shoeMesh, pal.shadowBlack, new Vector3(0.02f, 0.120f, -0.35f), new Vector3(0f, -4f, 0f), castShadows: true);

            // Mala de couro antiga com cantoneiras, cintas e alça no maleiro (topo em Y=1.793m)
            GameObject suitcase = CreateContainer(wardrobeRoot, "Mala_Couro_Antiga");
            suitcase.transform.localPosition = new Vector3(-0.02f, 1.793f, -0.44f);
            suitcase.transform.localRotation = Quaternion.Euler(0f, 4f, 0f);
            Mesh suitCaseMesh = CreateChamferedRectLoftMesh("Mesh_Mala_Couro", 0.38f, 0.175f, 0.56f, 0.024f, 0.96f, 0.012f);
            CreateMeshObject(suitcase.transform, "Corpo_Mala", suitCaseMesh, pal.woodMahogany, Vector3.zero, castShadows: true);
            CreateDetailBox(suitcase.transform, "Cinta_Mala_Sul", new Vector3(0f, 0.090f, -0.15f), new Vector3(0.386f, 0.180f, 0.026f), pal.baseboardWood);
            CreateDetailBox(suitcase.transform, "Cinta_Mala_Norte", new Vector3(0f, 0.090f, 0.15f), new Vector3(0.386f, 0.180f, 0.026f), pal.baseboardWood);
            CreateDetailBox(suitcase.transform, "Fivela_Sul", new Vector3(0.196f, 0.105f, -0.15f), new Vector3(0.008f, 0.026f, 0.032f), pal.metalBrass);
            CreateDetailBox(suitcase.transform, "Fivela_Norte", new Vector3(0.196f, 0.105f, 0.15f), new Vector3(0.008f, 0.026f, 0.032f), pal.metalBrass);
            CreateDetailBox(suitcase.transform, "Alca_Mala", new Vector3(0.205f, 0.095f, 0f), new Vector3(0.022f, 0.022f, 0.110f), pal.metalDark);

            // Caixa de papelão com tampa separada e etiqueta
            GameObject storBox = CreateContainer(wardrobeRoot, "Caixa_Guardada_1");
            storBox.transform.localPosition = new Vector3(-0.02f, 1.793f, 0.36f);
            storBox.transform.localRotation = Quaternion.Euler(0f, -6f, 0f);
            CreateDetailBox(storBox.transform, "Corpo_Caixa", new Vector3(0f, 0.056f, 0f), new Vector3(0.31f, 0.112f, 0.41f), pal.folderKraft, castShadows: true);
            CreateDetailBox(storBox.transform, "Tampa_Caixa", new Vector3(0f, 0.114f, 0f), new Vector3(0.328f, 0.024f, 0.428f), pal.woodLight, castShadows: true);
            CreateDetailBox(storBox.transform, "Etiqueta_Caixa", new Vector3(0.158f, 0.062f, 0f), new Vector3(0.003f, 0.044f, 0.095f), pal.paperWhite);
        }

        private static void BuildDresserTopAndDrawerCraft(
            Transform dresserRoot,
            Transform dresserTop,
            Transform drTop,
            Transform drBottom,
            GreyboxPalette pal)
        {
            // Bandeja espelhada com borda horizontal moldurada em latão (topo Comoda em Y=0.860f)
            GameObject mirroredTray = CreateContainer(dresserRoot, "Bandeja_Espelhada");
            mirroredTray.transform.localPosition = new Vector3(0.24f, 0.860f, 0.02f);
            CreateDetailBox(mirroredTray.transform, "Fundo_Bandeja", new Vector3(0f, 0.004f, 0f), new Vector3(0.30f, 0.008f, 0.18f), pal.stainlessSteel);
            BuildOpenFrameTrimBarsXZ(mirroredTray.transform, "Borda_Bandeja", new Vector3(0f, 0.0075f, 0f), 0.324f, 0.204f, 0.012f, 0.015f, pal.metalBrass);

            // Perfume cristalino em vidro facetado com colarinho e borrifador
            Mesh perfume1Mesh = CreateFacetedLatheVesselMesh("Mesh_Perfume_Octagonal", 8, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.024f),
                (0.052f, 0.026f),
                (0.066f, 0.014f),
                (0.072f, 0.014f),
                (0.072f, 0.000f)
            });
            GameObject perf1 = CreateMeshObject(mirroredTray.transform, "Perfume_1", perfume1Mesh, pal.details.clearGlass, new Vector3(-0.06f, 0.008f, 0.02f), castShadows: false);
            CreateDetailCylinder(perf1.transform, "Liquido_Perfume_1", new Vector3(0f, 0.024f, 0f), new Vector3(0.038f, 0.020f, 0.038f), pal.tileBorderBlue, castShadows: false);
            CreateDetailCylinder(perf1.transform, "Colar_Perfume_1", new Vector3(0f, 0.074f, 0f), new Vector3(0.022f, 0.004f, 0.022f), pal.metalBrass);
            CreateDetailCylinder(perf1.transform, "Tampa_Perfume_1", new Vector3(0f, 0.088f, 0f), new Vector3(0.024f, 0.010f, 0.024f), pal.metalBrass);

            Mesh perfume2Mesh = CreateChamferedRectLoftMesh("Mesh_Perfume_Frasco2", 0.044f, 0.056f, 0.032f, 0.006f, 0.90f, 0.006f);
            GameObject perf2 = CreateMeshObject(mirroredTray.transform, "Perfume_2", perfume2Mesh, pal.soapPastelPink, new Vector3(0.04f, 0.008f, -0.02f), new Vector3(0f, -18f, 0f), castShadows: false);
            CreateDetailCylinder(perf2.transform, "Bocal_Perfume_2", new Vector3(0f, 0.060f, 0f), new Vector3(0.016f, 0.006f, 0.016f), pal.metalBrass);
            CreateDetailCylinder(perf2.transform, "Tampa_Perfume_2", new Vector3(0f, 0.072f, 0f), new Vector3(0.020f, 0.008f, 0.020f), pal.porcelainWhite);

            // Porta-jóias com pezinho, caixa chanfrada, tampa chanfrada, cantoneiras de latão e fecho
            GameObject jewelBox = CreateContainer(dresserRoot, "Porta_Joias_Madeira");
            jewelBox.transform.localPosition = new Vector3(-0.26f, 0.860f, 0.04f);
            jewelBox.transform.localRotation = Quaternion.Euler(0f, 7f, 0f);
            Mesh jewelBaseMesh = CreateChamferedRectLoftMesh("Mesh_Base_PortaJoias", 0.214f, 0.052f, 0.144f, 0.008f, 0.96f, 0.005f);
            CreateMeshObject(jewelBox.transform, "Base_PortaJoias", jewelBaseMesh, pal.woodMahogany, Vector3.zero, castShadows: true);
            Mesh lidMesh = CreateChamferedRectLoftMesh("Mesh_Tampa_PortaJoias", 0.224f, 0.022f, 0.154f, 0.008f, 0.92f, 0.006f);
            CreateMeshObject(jewelBox.transform, "Tampa_PortaJoias", lidMesh, pal.baseboardWood, new Vector3(0f, 0.052f, 0f), castShadows: true);
            float[] jbCx = { -0.105f, 0.105f };
            float[] jbCz = { -0.070f, 0.070f };
            for (int jx = 0; jx < 2; jx++)
                for (int jz = 0; jz < 2; jz++)
                    CreateDetailBox(jewelBox.transform, $"Cantoneira_PortaJoias_{jx}_{jz}", new Vector3(jbCx[jx], 0.026f, jbCz[jz]), new Vector3(0.012f, 0.046f, 0.012f), pal.metalBrass);
            CreateDetailBox(jewelBox.transform, "Fecho_PortaJoias", new Vector3(0f, 0.052f, -0.075f), new Vector3(0.032f, 0.020f, 0.006f), pal.metalBrass);
            CreateDetailCylinder(jewelBox.transform, "Miolo_PortaJoias", new Vector3(0f, 0.048f, -0.0785f), new Vector3(0.008f, 0.0015f, 0.008f), pal.shadowBlack, new Vector3(90f, 0f, 0f));

            // Escova de cabelo ergonômica com almofada e tufos de cerdas
            GameObject hairBrush = CreateContainer(dresserRoot, "Escova_Cabelo");
            hairBrush.transform.localPosition = new Vector3(-0.02f, 0.860f, -0.06f);
            hairBrush.transform.localRotation = Quaternion.Euler(0f, 32f, 0f);
            Mesh brushHeadMesh = CreateChamferedRectLoftMesh("Mesh_Cabeca_Escova", 0.068f, 0.012f, 0.112f, 0.022f, 0.86f, 0.004f);
            CreateMeshObject(hairBrush.transform, "Escova_Cabelo_Base", brushHeadMesh, pal.furnitureWood, Vector3.zero, castShadows: false);
            CreateDetailBox(hairBrush.transform, "Escova_Cabelo_Cabo", new Vector3(0f, 0.006f, -0.095f), new Vector3(0.020f, 0.010f, 0.096f), pal.furnitureWood);
            CreateDetailCylinder(hairBrush.transform, "Almofada_Cerdas", new Vector3(0f, 0.0135f, 0f), new Vector3(0.054f, 0.003f, 0.094f), pal.metalDark);
            for (int bz = -2; bz <= 2; bz++)
            {
                for (int bx = -1; bx <= 1; bx++)
                {
                    CreateDetailCylinder(hairBrush.transform, $"Tufo_Cerda_{bx + 1}_{bz + 2}",
                        new Vector3(bx * 0.013f, 0.020f, bz * 0.016f),
                        new Vector3(0.0025f, 0.007f, 0.0025f), pal.shadowBlack);
                }
            }

            // Gaveta superior: camisas dobradas com colarinho e carcela + meia solta curvada
            Mesh shirtMesh = CreateChamferedRectLoftMesh("Mesh_Camisa_Dobrada", 0.295f, 0.034f, 0.235f, 0.026f, 0.90f, 0.010f);
            GameObject sh1 = CreateMeshObject(drTop, "Camisa_Dobrada_1", shirtMesh, pal.cianShirtWrinkled, new Vector3(-0.20f, -0.077f, -0.06f), castShadows: false);
            CreateDetailBox(sh1.transform, "Colarinho_1", new Vector3(0f, 0.036f, 0.082f), new Vector3(0.11f, 0.012f, 0.044f), pal.cianShirtWrinkled);
            GameObject sh2 = CreateMeshObject(drTop, "Camisa_Dobrada_2", shirtMesh, pal.porcelainWhite, new Vector3(-0.18f, -0.043f, -0.05f), new Vector3(0f, 5f, 0f), castShadows: false);
            CreateDetailBox(sh2.transform, "Colarinho_2", new Vector3(0f, 0.036f, 0.082f), new Vector3(0.11f, 0.012f, 0.044f), pal.porcelainWhite);
            CreateDetailBox(sh2.transform, "Carcela_Botoes_2", new Vector3(0f, 0.0355f, -0.005f), new Vector3(0.022f, 0.003f, 0.145f), pal.paperWhite);

            Mesh sockMesh = CreateClosedPolygonLoftMesh(
                "Mesh_Meia_Solta",
                new[]
                {
                    new Vector2(-0.055f, -0.032f),
                    new Vector2( 0.015f, -0.034f),
                    new Vector2( 0.058f, -0.010f),
                    new Vector2( 0.064f,  0.048f),
                    new Vector2( 0.026f,  0.058f),
                    new Vector2( 0.004f,  0.012f),
                    new Vector2(-0.055f,  0.008f)
                },
                new[]
                {
                    (0.000f, 0.88f, 0.88f, 0f, 0f),
                    (0.008f, 1.00f, 1.00f, 0f, 0f),
                    (0.018f, 1.00f, 1.00f, 0f, 0f),
                    (0.024f, 0.85f, 0.85f, 0f, 0f)
                });
            CreateMeshObject(drTop, "Meia_Sem_Par_Mesh", sockMesh, pal.paperWhite, new Vector3(0.22f, -0.077f, -0.07f), new Vector3(0f, 22f, 0f), castShadows: false);

            // Gaveta inferior: cobertor dobrado em duas camadas com vinco visível
            Mesh blanketMesh = CreateChamferedRectLoftMesh("Mesh_Cobertor_Dobrado", 0.72f, 0.034f, 0.26f, 0.028f, 0.93f, 0.012f);
            CreateMeshObject(drBottom, "Cobertor_Guardado_Inferior", blanketMesh, pal.bedBlanket, new Vector3(0f, -0.077f, -0.05f), castShadows: false);
            CreateMeshObject(drBottom, "Cobertor_Guardado", blanketMesh, pal.bedPastelBlue, new Vector3(0.01f, -0.045f, -0.048f), new Vector3(0f, 1.5f, 0f), castShadows: false);
        }

        private static GameObject BuildPleatedCurtainPanel(
            Transform parent,
            string name,
            Vector3 localPos,
            float width,
            float height,
            Material clothMat)
        {
            const float halfD = 0.024f;
            float hw = width * 0.5f;
            // Contorno sanfonado simples mantendo polígono estritamente em estrela para malha fechada
            Vector2[] cleanPleats =
            {
                new Vector2(-hw, -halfD * 0.5f),
                new Vector2(-hw * 0.5f, -halfD),
                new Vector2( 0f,        -halfD * 0.35f),
                new Vector2( hw * 0.5f, -halfD),
                new Vector2( hw,        -halfD * 0.5f),
                new Vector2( hw,         halfD * 0.6f),
                new Vector2( hw * 0.5f,  halfD * 0.2f),
                new Vector2( 0f,         halfD * 0.85f),
                new Vector2(-hw * 0.5f,  halfD * 0.2f),
                new Vector2(-hw,         halfD * 0.6f)
            };

            string key = $"{Mathf.RoundToInt(width * 1000f)}_{Mathf.RoundToInt(height * 1000f)}";
            Mesh curtainMesh = CreateClosedPolygonLoftMesh(
                $"Mesh_Cortina_{key}",
                cleanPleats,
                new[]
                {
                    (-height * 0.5f, 1.03f, 1.08f, 0f, 0f),
                    ( 0.00f,         0.98f, 1.00f, 0f, 0f),
                    ( height * 0.5f, 0.94f, 0.88f, 0f, 0f)
                });
            return CreateMeshObject(parent, name, curtainMesh, clothMat, localPos, castShadows: true);
        }

        private static GameObject BuildStyledDoorStop(
            Transform parent, string name, Vector3 floorPos, GreyboxPalette pal)
        {
            GameObject stop = CreateContainer(parent, name);
            stop.transform.localPosition = floorPos;
            CreateDetailCylinder(stop.transform, "Base_Batedor", new Vector3(0f, 0.004f, 0f), new Vector3(0.044f, 0.004f, 0.044f), pal.metalBrass);
            CreateDetailCylinder(stop.transform, "Corpo_Batedor", new Vector3(0f, 0.020f, 0f), new Vector3(0.032f, 0.012f, 0.032f), pal.metalBrass);
            CreateDetailCylinder(stop.transform, "Borracha_Batedor", new Vector3(0f, 0.020f, 0f), new Vector3(0.038f, 0.006f, 0.038f), pal.shadowBlack);
            CreateDetailSphere(stop.transform, "Topo_Batedor", new Vector3(0f, 0.032f, 0f), new Vector3(0.024f, 0.012f, 0.024f), pal.metalBrass, castShadows: false);
            return stop;
        }

        private static Mesh CreateClosedThickGridShellMesh(
            string assetName,
            Vector3[,] outerPts,
            Vector3[,] innerPts)
        {
            int nu = outerPts.GetLength(0) - 1;
            int nv = outerPts.GetLength(1) - 1;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            for (int i = 0; i < nu; i++)
            {
                for (int j = 0; j < nv; j++)
                {
                    // Superfície externa (+Y / para fora) e superfície interna (-Y / para dentro)
                    AddFlatQuad(verts, tris, uvs, outerPts[i, j], outerPts[i, j + 1], outerPts[i + 1, j + 1], outerPts[i + 1, j]);
                    AddFlatQuad(verts, tris, uvs, innerPts[i, j], innerPts[i + 1, j], innerPts[i + 1, j + 1], innerPts[i, j + 1]);
                }
            }

            // Fechamento estanque das 4 bordas perimetrais
            for (int i = 0; i < nu; i++)
            {
                AddFlatQuad(verts, tris, uvs, outerPts[i, 0], outerPts[i + 1, 0], innerPts[i + 1, 0], innerPts[i, 0]);
                AddFlatQuad(verts, tris, uvs, outerPts[i + 1, nv], outerPts[i, nv], innerPts[i, nv], innerPts[i + 1, nv]);
            }

            for (int j = 0; j < nv; j++)
            {
                AddFlatQuad(verts, tris, uvs, outerPts[0, j + 1], outerPts[0, j], innerPts[0, j], innerPts[0, j + 1]);
                AddFlatQuad(verts, tris, uvs, outerPts[nu, j], outerPts[nu, j + 1], innerPts[nu, j + 1], innerPts[nu, j]);
            }

            Mesh m = new Mesh { name = assetName };
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs.ToArray();
            AssignFlatFaceNormals(m, verts, tris);
            m.RecalculateBounds();
            return SaveOrUpdateMeshAsset(m, assetName);
        }

        /// <summary>
        /// Constrói uma coberta única contínua e anatômica + vira única de lençol seguindo exatamente
        /// uma meia-parábola suave do lado mais baixo (onde Cian dormiu, Oeste) até o lado alto (Margaret, Leste).
        /// </summary>
        private static void BuildUnifiedBedBlanketAndSheet(Transform bedTransform, GreyboxPalette pal)
        {
            // Seção transversal contínua abraçando lateral oeste, topo do colchão e lateral leste (17 estações)
            float[] profX =
            {
                -0.784f, -0.770f, -0.756f, -0.732f, -0.680f,
                -0.510f, -0.340f, -0.170f,  0.000f,  0.170f,  0.340f,  0.510f,
                 0.680f,  0.732f,  0.756f,  0.770f,  0.784f
            };
            float[] profY =
            {
                 0.264f,  0.352f,  0.432f,  0.456f,  0.462f,
                 0.462f,  0.462f,  0.462f,  0.462f,  0.462f,  0.462f,  0.462f,
                 0.462f,  0.456f,  0.432f,  0.352f,  0.264f
            };
            Vector2[] profNormXY =
            {
                new Vector2(-0.98f, 0.20f).normalized,
                new Vector2(-0.99f, 0.14f).normalized,
                new Vector2(-0.82f, 0.57f).normalized,
                new Vector2(-0.42f, 0.91f).normalized,
                Vector2.up, Vector2.up, Vector2.up, Vector2.up, Vector2.up, Vector2.up, Vector2.up, Vector2.up, Vector2.up,
                new Vector2( 0.42f, 0.91f).normalized,
                new Vector2( 0.82f, 0.57f).normalized,
                new Vector2( 0.99f, 0.14f).normalized,
                new Vector2( 0.98f, 0.20f).normalized
            };

            const int nu = 16; // profX.Length - 1
            const int nv = 12;

            var blanketOut = new Vector3[nu + 1, nv + 1];
            var blanketIn = new Vector3[nu + 1, nv + 1];

            for (int i = 0; i <= nu; i++)
            {
                float xBase = profX[i];
                float yBase = profY[i];
                Vector2 nxy = profNormXY[i];
                Vector3 normal = new Vector3(nxy.x, nxy.y, 0f);

                // Meia-parábola suave ligando lado baixo (Cian, tX=0 -> Z=0.185) ao lado alto (Margaret, tX=1 -> Z=0.505)
                float tX = Mathf.Clamp01((xBase + 0.735f) / 1.470f);
                float halfParabola = 1f - (1f - tX) * (1f - tX);
                float headZ = Mathf.Lerp(0.185f, 0.505f, halfParabola);

                // Recorte anatômico sutil do corpo de Cian dormindo no lado oeste + espessura orgânica
                float sleepGaussian = Mathf.Exp(-Mathf.Pow((xBase + 0.36f) / 0.24f, 2f));
                bool isSkirt = (i <= 1 || i >= nu - 1);

                for (int j = 0; j <= nv; j++)
                {
                    float v = (float)j / nv;
                    float x = xBase;
                    float y = yBase;
                    float z;
                    Vector3 jNormal = normal;

                    if (j == 0)
                    {
                        // Borda tombada na peseira com folga milimétrica entre o colchão (-0.970) e a peseira (-0.982)
                        z = -0.971f;
                        if (i == 0 || i == nu)
                        {
                            x = Mathf.Sign(xBase) * 0.764f;
                            y = 0.286f;
                        }
                        else if (i == 1 || i == nu - 1)
                        {
                            x = Mathf.Sign(xBase) * 0.762f;
                            y = 0.342f;
                        }
                        else if (i == 2 || i == nu - 2)
                        {
                            x = Mathf.Sign(xBase) * 0.742f;
                            y = 0.376f;
                        }
                        else
                        {
                            y = 0.384f;
                        }
                        jNormal = new Vector3(normal.x * 0.35f, normal.y * 0.15f, -0.92f).normalized;
                    }
                    else if (j == 1)
                    {
                        // Arredondamento suave sobre o chanfro frontal do colchão (Z=-0.970..0.932)
                        z = -0.964f;
                        if (i == 0 || i == nu)
                        {
                            x = Mathf.Sign(xBase) * 0.775f;
                            y = 0.284f;
                        }
                        else if (i == 1 || i == nu - 1)
                        {
                            x = Mathf.Sign(xBase) * 0.770f;
                            y = 0.352f;
                        }
                        else
                        {
                            y = yBase - 0.010f;
                        }
                        Vector3 cornerN = new Vector3(normal.x * 0.55f, normal.y * 0.65f, -0.62f).normalized;
                        jNormal = Vector3.Slerp(cornerN, normal, 0.35f).normalized;
                    }
                    else
                    {
                        float vTop = (j - 2f) / (nv - 2f);
                        z = Mathf.Lerp(-0.928f, headZ, vTop);
                    }

                    // Achatamento sutil no lugar onde Cian deitou (meio-alto do lado esquerdo) e leve ondulação no tecido
                    float longitudinalBelly = Mathf.Sin(Mathf.Clamp01((v - 0.15f) / 0.85f) * Mathf.PI);
                    float flatten = sleepGaussian * longitudinalBelly;
                    float baseThick = isSkirt ? 0.013f : Mathf.Lerp(0.022f, 0.015f, flatten * 0.85f);
                    float edgeTaper = j == 0 ? 0.009f : (1f - 0.22f * Mathf.Pow(Mathf.Abs(2f * v - 1f), 4f)) * baseThick;
                    float foldWave = (1f - tX) * 0.0025f * Mathf.Sin(v * Mathf.PI * 3.0f) * (j <= 1 || isSkirt ? 0f : 1f);
                    float thick = (j == 0 ? 0.009f : edgeTaper) + Mathf.Max(0f, foldWave);

                    Vector3 inPt = new Vector3(x, y, z);
                    blanketIn[i, j] = inPt;
                    blanketOut[i, j] = inPt + jNormal * thick;
                }
            }

            Mesh blanketMesh = CreateClosedThickGridShellMesh("Mesh_Coberta_Casal_Unica", blanketOut, blanketIn);
            CreateMeshObject(bedTransform, "Coberta_Casal_Unica", blanketMesh, pal.bedBlanket, Vector3.zero, castShadows: true);

            // Vira contínua do lençol superior acompanhando a mesma meia-parábola da borda da coberta
            const int sNu = 14; // i = 1..15 (cobre todo o topo e vira suavemente nos ombros laterais)
            const int sNv = 5;
            var sheetOut = new Vector3[sNu + 1, sNv + 1];
            var sheetIn = new Vector3[sNu + 1, sNv + 1];

            for (int si = 0; si <= sNu; si++)
            {
                int srcI = si + 1;
                float xBase = profX[srcI];
                float yBase = profY[srcI];
                Vector2 nxy = profNormXY[srcI];
                Vector3 normal = new Vector3(nxy.x, nxy.y, 0f);

                float tX = Mathf.Clamp01((xBase + 0.735f) / 1.470f);
                float halfParabola = 1f - (1f - tX) * (1f - tX);
                float headZ = Mathf.Lerp(0.185f, 0.505f, halfParabola);

                // A borda externa da coberta tem espessura em headZ
                Vector3 duvetTopAtEdge = blanketOut[srcI, nv];
                float cuffDepth = 0.145f;

                for (int sj = 0; sj <= sNv; sj++)
                {
                    float sv = (float)sj / sNv; // 0 = borda inferior da vira sobre a coberta, 1 = dobra frontal na cabeceira
                    Vector3 anchorPt;
                    if (sv <= 0.80f)
                    {
                        float backT = sv / 0.80f;
                        float z = Mathf.Lerp(headZ - cuffDepth, headZ, backT);
                        float duvetThickAtZ = Mathf.Lerp(
                            (blanketOut[srcI, nv - 1] - blanketIn[srcI, nv - 1]).magnitude,
                            (duvetTopAtEdge - blanketIn[srcI, nv]).magnitude,
                            backT);
                        float clearance = 0.0025f;
                        anchorPt = new Vector3(xBase, yBase, z) + normal * (Mathf.Max(0.012f, duvetThickAtZ) + clearance);
                        sheetIn[si, sj] = anchorPt;
                        sheetOut[si, sj] = anchorPt + normal * 0.009f;
                    }
                    else
                    {
                        // A última faixa faz a dobra arredondada descendo da borda da coberta até a face superior do colchão
                        float lipT = (sv - 0.80f) / 0.20f;
                        float z = headZ + lipT * 0.020f;
                        float overH = Mathf.Lerp(
                            Mathf.Max(0.013f, (duvetTopAtEdge - blanketIn[srcI, nv]).magnitude) + 0.0025f,
                            0.0025f,
                            lipT);
                        anchorPt = new Vector3(xBase, yBase, z) + normal * overH;
                        sheetIn[si, sj] = anchorPt;
                        sheetOut[si, sj] = anchorPt + normal * Mathf.Lerp(0.009f, 0.006f, lipT);
                    }
                }
            }

            Mesh sheetCuffMesh = CreateClosedThickGridShellMesh("Mesh_Dobra_Lencol_Unica", sheetOut, sheetIn);
            CreateMeshObject(bedTransform, "Dobra_Lencol_Unica", sheetCuffMesh, pal.porcelainWhite, Vector3.zero, castShadows: true);
        }

        private static void BuildHallwayCoatRackProps(Transform rackTransform, GreyboxPalette pal)
        {
            // Coluna esculpida com 4 pés chanfrados, colar intermediário, pomo torneado e 4 ganchos curvos em S
            for (int f = 0; f < 4; f++)
            {
                float ang = f * 90f + 45f;
                GameObject foot = CreateContainer(rackTransform, $"Pe_Cabideiro_{f}");
                foot.transform.localPosition = Vector3.zero;
                foot.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                CreateDetailBox(foot.transform, "Braco_Pe_Inf", new Vector3(0f, 0.012f, 0.095f), new Vector3(0.032f, 0.024f, 0.095f), pal.furnitureWood);
                CreateDetailBox(foot.transform, "Braco_Pe_Diag", new Vector3(0f, 0.046f, 0.056f), new Vector3(0.028f, 0.068f, 0.042f), pal.woodMahogany, new Vector3(34f, 0f, 0f));
                CreateDetailBox(foot.transform, "Sapata_Pe_Cabideiro", new Vector3(0f, 0.005f, 0.132f), new Vector3(0.036f, 0.010f, 0.028f), pal.metalBrass);
            }

            CreateDetailCylinder(rackTransform, "Base_Disco_Cabideiro", new Vector3(0f, 0.042f, 0f), new Vector3(0.115f, 0.016f, 0.115f), pal.woodMahogany);
            CreateCylinder(rackTransform, "Base_Torneada", new Vector3(0f, 0.072f, 0f), new Vector3(0.076f, 0.018f, 0.076f), pal.furnitureWood, hasCollider: true);
            CreateCylinder(rackTransform, "Haste", new Vector3(0f, 0.875f, 0f), new Vector3(0.042f, 0.785f, 0.042f), pal.furnitureWood, hasCollider: true);
            CreateDetailCylinder(rackTransform, "Colar_Haste", new Vector3(0f, 1.06f, 0f), new Vector3(0.060f, 0.014f, 0.060f), pal.woodMahogany);
            CreateDetailCylinder(rackTransform, "Anel_Coroa_Ganchos", new Vector3(0f, 1.56f, 0f), new Vector3(0.056f, 0.018f, 0.056f), pal.metalBrass);

            Vector2[] octUnit = new Vector2[8];
            for (int oi = 0; oi < 8; oi++)
            {
                float a = (2f * Mathf.PI * oi) / 8f;
                octUnit[oi] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            Mesh finialMesh = CreateClosedPolygonLoftMesh("Mesh_Pomo_Cabideiro", octUnit, new[]
            {
                (0.000f, 0.024f, 0.024f, 0f, 0f),
                (0.016f, 0.032f, 0.032f, 0f, 0f),
                (0.048f, 0.038f, 0.038f, 0f, 0f),
                (0.080f, 0.022f, 0.022f, 0f, 0f),
                (0.106f, 0.008f, 0.008f, 0f, 0f)
            });
            CreateMeshObject(rackTransform, "Pomo_Topo", finialMesh, pal.woodMahogany, new Vector3(0f, 1.658f, 0f), castShadows: true);

            Mesh rackHookMesh = CreateFacetedCShapeHandleMesh(
                "Mesh_Gancho_Cabideiro_S",
                new[]
                {
                    new Vector3(0f,  0.000f, 0.024f),
                    new Vector3(0f,  0.018f, 0.064f),
                    new Vector3(0f,  0.004f, 0.102f),
                    new Vector3(0f,  0.028f, 0.126f),
                    new Vector3(0f,  0.056f, 0.118f)
                },
                halfThicknessY: 0.006f,
                halfThicknessZ: 0.006f,
                sideAxis: Vector3.right);

            for (int hk = 0; hk < 4; hk++)
            {
                float ang = hk * 90f + 45f;
                GameObject hook = CreateContainer(rackTransform, $"Gancho_{hk}");
                hook.transform.localPosition = new Vector3(0f, 1.56f, 0f);
                hook.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                CreateDetailCylinder(hook.transform, "Roseta_Gancho", new Vector3(0f, 0f, 0.026f), new Vector3(0.020f, 0.004f, 0.020f), pal.metalBrass, new Vector3(90f, 0f, 0f));
                CreateMeshObject(hook.transform, "Haste_Gancho_S", rackHookMesh, pal.metalBrass, Vector3.zero, castShadows: true);
            }

            // Casaco pendurado exatamente sobre Gancho_0 (ang = 45 deg -> +X, +Z local)
            Mesh coatRackMesh = CreateClosedPolygonLoftMesh(
                "Mesh_Casaco_Cabideiro",
                new[]
                {
                    new Vector2( 0.095f, -0.055f),
                    new Vector2( 0.115f,  0.000f),
                    new Vector2( 0.090f,  0.055f),
                    new Vector2(-0.090f,  0.055f),
                    new Vector2(-0.115f,  0.000f),
                    new Vector2(-0.095f, -0.055f)
                },
                new[]
                {
                    (0.000f, 1.24f, 1.28f, 0f, 0f),
                    (0.280f, 1.06f, 1.08f, 0f, 0f),
                    (0.510f, 0.78f, 0.82f, 0f, 0f),
                    (0.610f, 0.36f, 0.42f, 0f, 0f)
                });
            CreateMeshObject(rackTransform, "Casaco_Masculino", coatRackMesh, pal.metalDark, new Vector3(0.095f, 0.985f, 0.095f), new Vector3(4f, 45f, -3f), castShadows: true);
            CreateDetailBox(rackTransform, "Gola_Casaco_Entrada", new Vector3(0.088f, 1.585f, 0.088f), new Vector3(0.068f, 0.032f, 0.056f), pal.shadowBlack, new Vector3(6f, 45f, 0f));

            // Guarda-chuva vermelho pendurado EXATAMENTE no gancho oposto (Gancho_2, ang = 225 deg -> -X, -Z local em Y=1.586m)
            GameObject umbrella = CreateContainer(rackTransform, "Guarda_Chuva");
            umbrella.transform.localPosition = new Vector3(-0.084f, 1.592f, -0.084f);
            umbrella.transform.localRotation = Quaternion.Euler(3f, 45f, -4f);

            Mesh jHandleMesh = CreateFacetedCShapeHandleMesh(
                "Mesh_Cabo_GuardaChuva_J",
                new[]
                {
                    new Vector3(0f, -0.095f, 0.000f),
                    new Vector3(0f, -0.015f, 0.000f),
                    new Vector3(0f,  0.012f, -0.018f),
                    new Vector3(0f,  0.008f, -0.042f),
                    new Vector3(0f, -0.020f, -0.050f)
                },
                halfThicknessY: 0.007f,
                halfThicknessZ: 0.007f,
                sideAxis: Vector3.right);
            CreateMeshObject(umbrella.transform, "Cabo_J_Curvo", jHandleMesh, pal.woodMahogany, Vector3.zero, castShadows: true);

            Mesh umbrellaConeMesh = CreateClosedPolygonLoftMesh(
                "Mesh_Tecido_GuardaChuva",
                new[]
                {
                    new Vector2( 0.036f,  0.000f),
                    new Vector2( 0.025f,  0.025f),
                    new Vector2( 0.000f,  0.036f),
                    new Vector2(-0.025f,  0.025f),
                    new Vector2(-0.036f,  0.000f),
                    new Vector2(-0.025f, -0.025f),
                    new Vector2( 0.000f, -0.036f),
                    new Vector2( 0.025f, -0.025f)
                },
                new[]
                {
                    (-0.680f, 0.24f, 0.24f, 0f, 0f),
                    (-0.520f, 0.75f, 0.75f, 0f, 0f),
                    (-0.340f, 1.00f, 1.00f, 0f, 0f),
                    (-0.175f, 0.72f, 0.72f, 0f, 0f),
                    (-0.150f, 0.35f, 0.35f, 0f, 0f)
                });
            CreateMeshObject(umbrella.transform, "Tecido_Fechado", umbrellaConeMesh, pal.calendarRed, Vector3.zero, castShadows: true);
            CreateDetailCylinder(umbrella.transform, "Fita_Prendedor_GuardaChuva", new Vector3(0f, -0.36f, 0f), new Vector3(0.075f, 0.014f, 0.075f), pal.metalDark);
            CreateDetailCylinder(umbrella.transform, "Haste_Guarda_Chuva", new Vector3(0f, -0.125f, 0f), new Vector3(0.012f, 0.034f, 0.012f), pal.stainlessSteel);
            CreateDetailCylinder(umbrella.transform, "Ponteira_Guarda_Chuva", new Vector3(0f, -0.735f, 0f), new Vector3(0.012f, 0.058f, 0.012f), pal.metalBrass);

            // Sapatos sociais esculpidos reutilizando Mesh_Sapato_Social apoiados exatamente no piso (Y=0.000m)
            Mesh shoeMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/Mesh_Sapato_Social.asset");
            if (shoeMesh != null)
            {
                GameObject sL = CreateMeshObject(rackTransform, "Sapato_Entrada_E", shoeMesh, pal.shadowBlack, new Vector3(-0.26f, 0f, -0.02f), new Vector3(0f, -78f, 0f), castShadows: true);
                sL.transform.localScale = new Vector3(1.04f, 1.04f, 1.04f);
                SnapToSupportWorldY(sL, 0f);

                GameObject sR = CreateMeshObject(rackTransform, "Sapato_Entrada_D", shoeMesh, pal.shadowBlack, new Vector3(-0.42f, 0f, -0.01f), new Vector3(0f, -96f, 0f), castShadows: true);
                sR.transform.localScale = new Vector3(1.04f, 1.04f, 1.04f);
                SnapToSupportWorldY(sR, 0f);
            }
            else
            {
                CreateDetailBox(rackTransform, "Sapato_Entrada_E", new Vector3(-0.26f, 0.04f, -0.02f), new Vector3(0.10f, 0.08f, 0.26f), pal.shadowBlack, new Vector3(0f, 12f, 0f));
                CreateDetailBox(rackTransform, "Sapato_Entrada_D", new Vector3(-0.43f, 0.04f, -0.01f), new Vector3(0.10f, 0.08f, 0.26f), pal.shadowBlack, new Vector3(0f, -6f, 0f));
            }
        }

        private static Mesh GetOrBuildCrossHandleMesh()
        {
            const float armLen = 0.024f;
            const float armW = 0.0045f;
            Vector2[] crossPoly =
            {
                new Vector2(-armW, -armLen),
                new Vector2( armW, -armLen),
                new Vector2( armW, -armW),
                new Vector2( armLen, -armW),
                new Vector2( armLen,  armW),
                new Vector2( armW,  armW),
                new Vector2( armW,  armLen),
                new Vector2(-armW,  armLen),
                new Vector2(-armW,  armW),
                new Vector2(-armLen,  armW),
                new Vector2(-armLen, -armW),
                new Vector2(-armW, -armW)
            };
            return CreateClosedPolygonLoftMesh("Mesh_Cruzeta_Registro", crossPoly, new[]
            {
                (0.000f, 0.90f, 0.90f, 0f, 0f),
                (0.002f, 1.00f, 1.00f, 0f, 0f),
                (0.008f, 1.00f, 1.00f, 0f, 0f),
                (0.010f, 0.85f, 0.85f, 0f, 0f)
            });
        }

        private static GameObject BuildToiletPaperRoll(
            Transform parent, string name, Vector3 localPos, Vector3 localEuler, GreyboxPalette pal, bool withHangingSheet = false)
        {
            GameObject roll = CreateContainer(parent, name);
            roll.transform.localPosition = localPos;
            roll.transform.localRotation = Quaternion.Euler(localEuler);

            CreateDetailCylinder(roll.transform, "Corpo_Papel", new Vector3(0f, 0.052f, 0f), new Vector3(0.098f, 0.052f, 0.098f), pal.paperWhite, castShadows: true);
            CreateDetailCylinder(roll.transform, "Tubo_Papelao", new Vector3(0f, 0.052f, 0f), new Vector3(0.038f, 0.0528f, 0.038f), pal.folderKraft);
            CreateDetailCylinder(roll.transform, "Miolo_Vazio", new Vector3(0f, 0.052f, 0f), new Vector3(0.032f, 0.0534f, 0.032f), pal.shadowBlack);

            if (withHangingSheet)
            {
                // Rolo_Papel_Uso é rotacionado +90° em X na papeleira, portanto +Z local aponta para baixo (-Y no mundo).
                CreateDetailBox(roll.transform, "Folha_Pendente_Papel", new Vector3(0.0485f, 0.052f, 0.048f), new Vector3(0.0020f, 0.098f, 0.096f), pal.paperWhite);
            }
            return roll;
        }

        private static GameObject BuildBleachJug(
            Transform parent, string name, Vector3 localPos, float yaw, GreyboxPalette pal)
        {
            GameObject jug = CreateContainer(parent, name);
            jug.transform.localPosition = localPos;
            jug.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateDetailCylinder(jug.transform, "Corpo_Galao", new Vector3(0f, 0.105f, 0f), new Vector3(0.104f, 0.105f, 0.092f), pal.porcelainWhite, castShadows: true);
            CreateCylinderLabelSleeve(jug.transform, "Rotulo_AguaSanitaria", new Vector3(0f, 0.105f, 0f), new Vector3(0.1065f, 0.115f, 0.0945f), pal.details.bleachLabel, pal, new Vector3(0f, 90f, 0f));
            CreateDetailCylinder(jug.transform, "Ombro_Galao", new Vector3(0.010f, 0.222f, 0f), new Vector3(0.064f, 0.014f, 0.058f), pal.porcelainWhite);
            CreateDetailCylinder(jug.transform, "Gargalo_Galao", new Vector3(0.016f, 0.244f, 0f), new Vector3(0.034f, 0.010f, 0.034f), pal.porcelainWhite);
            CreateDetailCylinder(jug.transform, "Tampa_Agua_Sanitaria", new Vector3(0.016f, 0.264f, 0f), new Vector3(0.040f, 0.012f, 0.040f), pal.calendarRed);

            Mesh handleMesh = CreateFacetedCShapeHandleMesh("Mesh_Alca_Galao_Sanitaria", new[]
            {
                new Vector3(-0.012f, 0.232f, 0f),
                new Vector3(-0.048f, 0.226f, 0f),
                new Vector3(-0.062f, 0.188f, 0f),
                new Vector3(-0.052f, 0.148f, 0f),
                new Vector3(-0.032f, 0.142f, 0f)
            }, halfThicknessY: 0.008f, halfThicknessZ: 0.009f);
            CreateMeshObject(jug.transform, "Alca_Galao", handleMesh, pal.porcelainWhite, Vector3.zero, castShadows: true);
            return jug;
        }

        private static GameObject BuildFoldedClothMeshObject(
            Transform parent, string name, string meshAssetName, Vector3 localPos, Vector3 localEuler, Vector3 scale, Material mainMat, Material trimMat = null)
        {
            Mesh clothMesh = CreateClosedPolygonLoftMesh(meshAssetName, new[]
            {
                new Vector2(-0.14f, -0.025f),
                new Vector2( 0.14f, -0.025f),
                new Vector2( 0.15f,  0.000f),
                new Vector2( 0.14f,  0.025f),
                new Vector2(-0.14f,  0.025f),
                new Vector2(-0.15f,  0.000f)
            }, new[]
            {
                (0.000f, 0.96f, 0.92f, 0f, 0f),
                (0.012f, 1.00f, 1.02f, 0f, 0f),
                (0.048f, 1.00f, 1.02f, 0f, 0f),
                (0.060f, 0.92f, 0.88f, 0f, 0f)
            });

            GameObject container = CreateContainer(parent, name);
            container.transform.localPosition = localPos;
            container.transform.localRotation = Quaternion.Euler(localEuler);
            GameObject body = CreateMeshObject(container.transform, $"{name}_Corpo", clothMesh, mainMat, Vector3.zero, castShadows: true);
            body.transform.localScale = scale;

            if (trimMat != null)
            {
                CreateDetailBox(container.transform, $"{name}_Barrado", new Vector3(0f, 0.016f * scale.y, -0.0245f * scale.z), new Vector3(0.27f * scale.x, 0.014f * scale.y, 0.004f), trimMat);
            }
            return container;
        }

        private static void BuildBathroomTowelRack(Transform root, GreyboxPalette pal)
        {
            GameObject towelBar = CreateContainer(root, "Porta_Toalha_Rosto");
            towelBar.transform.localPosition = new Vector3(-3.5345f, 1.28f, 5.42f);
            towelBar.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Espelhos de fixação encostados no azulejo + suportes + barra inox
            CreateDetailCylinder(towelBar.transform, "Roseta_Toalha_Esq", new Vector3(-0.18f, 0f, -0.004f), new Vector3(0.044f, 0.004f, 0.044f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(towelBar.transform, "Roseta_Toalha_Dir", new Vector3( 0.18f, 0f, -0.004f), new Vector3(0.044f, 0.004f, 0.044f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(towelBar.transform, "Suporte_Esq", new Vector3(-0.18f, 0f, -0.032f), new Vector3(0.020f, 0.028f, 0.020f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(towelBar.transform, "Suporte_Dir", new Vector3( 0.18f, 0f, -0.032f), new Vector3(0.020f, 0.028f, 0.020f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(towelBar.transform, "Barra_Inox", new Vector3(0f, 0f, -0.056f), new Vector3(0.016f, 0.21f, 0.016f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));

            // Toalha contínua dobrada sobre a barra em U invertido sem interpenetrar a haste metálica
            const int nu = 6, nv = 10;
            float[] profU = { -0.140f, -0.088f, -0.030f, 0.000f, 0.030f, 0.088f, 0.140f };
            float[] profV_Y = { -0.340f, -0.250f, -0.140f, -0.045f, 0.004f, 0.016f, 0.004f, -0.045f, -0.130f, -0.220f, -0.295f };
            float[] profV_Z = { -0.072f, -0.074f, -0.071f, -0.068f, -0.066f, -0.056f, -0.046f, -0.044f, -0.042f, -0.040f, -0.042f };
            Vector2[] normYZ =
            {
                new Vector2( 0.00f, -1.00f),
                new Vector2( 0.00f, -1.00f),
                new Vector2( 0.00f, -1.00f),
                new Vector2( 0.00f, -1.00f),
                new Vector2( 0.65f, -0.76f).normalized,
                new Vector2( 1.00f,  0.00f),
                new Vector2( 0.65f,  0.76f).normalized,
                new Vector2( 0.00f,  1.00f),
                new Vector2( 0.00f,  1.00f),
                new Vector2( 0.00f,  1.00f),
                new Vector2( 0.00f,  1.00f)
            };

            var towelOut = new Vector3[nu + 1, nv + 1];
            var towelIn = new Vector3[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
            {
                float x = profU[i];
                for (int j = 0; j <= nv; j++)
                {
                    float foldWave = Mathf.Sin(((float)i / nu) * Mathf.PI * 3f) * 0.004f * (1f - Mathf.Clamp01(Mathf.Abs(j - 5) / 5f));
                    Vector3 inPt = new Vector3(x, profV_Y[j], profV_Z[j]);
                    Vector3 n = new Vector3(0f, normYZ[j].x, normYZ[j].y);
                    towelIn[i, j] = inPt;
                    towelOut[i, j] = inPt + n * (0.0085f + Mathf.Abs(foldWave));
                }
            }
            Mesh drapedTowelMesh = CreateClosedThickGridShellMesh("Mesh_Toalha_Toalheiro", towelOut, towelIn);
            CreateMeshObject(towelBar.transform, "Toalha_Pendurada", drapedTowelMesh, pal.paperWhite, Vector3.zero, castShadows: true);
            CreateDetailBox(towelBar.transform, "Barra_Bordada_Toalha", new Vector3(0f, -0.305f, -0.0825f), new Vector3(0.282f, 0.028f, 0.003f), pal.bedBlanket);
        }

        private static FaucetWaterController BuildBathroomSinkDetails(Transform sinkTop, GameObject sinkRimW, GameObject sinkRimE, GreyboxPalette pal)
        {
            GameObject bathFaucet = CreateContainer(sinkTop.transform, "Torneira_Banheiro");
            CreateDetailCylinder(bathFaucet.transform, "Canopia_Torneira", new Vector3(-4.02f, 0.896f, 5.91f), new Vector3(0.050f, 0.006f, 0.050f), pal.stainlessSteel);
            CreateDetailCylinder(bathFaucet.transform, "Torneira_Base", new Vector3(-4.02f, 0.925f, 5.91f), new Vector3(0.036f, 0.028f, 0.036f), pal.stainlessSteel, castShadows: true);
            Mesh spoutMesh = CreateFacetedCShapeHandleMesh("Mesh_Torneira_Pia", new[]
            {
                new Vector3(0f, 0.945f, 5.910f),
                new Vector3(0f, 1.005f, 5.902f),
                new Vector3(0f, 1.032f, 5.855f),
                new Vector3(0f, 1.012f, 5.800f),
                new Vector3(0f, 0.985f, 5.788f)
            }, halfThicknessY: 0.011f, halfThicknessZ: 0.011f, sideAxis: Vector3.right);
            CreateMeshObject(bathFaucet.transform, "Torneira_Bica_Curva", spoutMesh, pal.stainlessSteel, new Vector3(-4.02f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(bathFaucet.transform, "Aerador_Torneira", new Vector3(-4.02f, 0.982f, 5.788f), new Vector3(0.026f, 0.007f, 0.026f), pal.metalDark, new Vector3(-16f, 0f, 0f));

            GameObject waterOrigin = CreateContainer(bathFaucet.transform, "Ponto_Saida_Agua");
            waterOrigin.transform.localPosition = new Vector3(-4.02f, 0.976f, 5.786f);

            Mesh crossMesh = GetOrBuildCrossHandleMesh();
            float[] regXs = { -4.15f, -3.89f };
            Transform coldCrossTransform = null;
            for (int r = 0; r < 2; r++)
            {
                string side = r == 0 ? "Quente" : "Frio";
                Material dotMat = r == 0 ? pal.calendarRed : pal.bookSlateBlue;
                CreateDetailCylinder(bathFaucet.transform, $"Canopia_Registro_{side}", new Vector3(regXs[r], 0.895f, 5.91f), new Vector3(0.044f, 0.005f, 0.044f), pal.stainlessSteel);
                CreateDetailCylinder(bathFaucet.transform, $"Registro_{side}", new Vector3(regXs[r], 0.914f, 5.91f), new Vector3(0.020f, 0.014f, 0.020f), pal.stainlessSteel);
                GameObject crossObj = CreateMeshObject(bathFaucet.transform, $"Cruzeta_Registro_{side}", crossMesh, pal.stainlessSteel, new Vector3(regXs[r], 0.928f, 5.91f), new Vector3(0f, 45f, 0f), castShadows: true);
                if (r == 1) coldCrossTransform = crossObj.transform;
                CreateDetailCylinder(bathFaucet.transform, $"Indicador_{side}", new Vector3(regXs[r], 0.939f, 5.91f), new Vector3(0.009f, 0.0015f, 0.009f), dotMat);
            }

            FaucetWaterController faucetCtrl = GetOrAdd<FaucetWaterController>(bathFaucet);
            faucetCtrl.ConfigureReferences(
                nozzle: waterOrigin.transform,
                coldHandle: coldCrossTransform,
                sinkBasinFloorY: 0.774f,
                forwardArcZ: -0.028f,
                duration: 2.15f
            );
            EditorUtility.SetDirty(faucetCtrl);

            // Saboneteira côncava oval com sabonete anatômico chanfrado
            Mesh dishMesh = CreateClosedPolygonLoftMesh("Mesh_Saboneteira_Oval", new[]
            {
                new Vector2( 0.056f,  0.000f),
                new Vector2( 0.040f,  0.032f),
                new Vector2( 0.000f,  0.044f),
                new Vector2(-0.040f,  0.032f),
                new Vector2(-0.056f,  0.000f),
                new Vector2(-0.040f, -0.032f),
                new Vector2( 0.000f, -0.044f),
                new Vector2( 0.040f, -0.032f)
            }, new[]
            {
                (0.000f, 0.78f, 0.78f, 0f, 0f),
                (0.006f, 0.98f, 0.98f, 0f, 0f),
                (0.013f, 1.00f, 1.00f, 0f, 0f)
            });
            GameObject soapDish = CreateMeshObject(sinkTop.transform, "Saboneteira_Louca", dishMesh, pal.porcelainWhite, new Vector3(-4.40f, 0.890f, 5.60f), new Vector3(0f, 82f, 0f), castShadows: true);
            SnapToSupportTop(soapDish, sinkRimW);

            Mesh soapMesh = CreateClosedPolygonLoftMesh("Mesh_Sabonete_Oval", new[]
            {
                new Vector2( 0.036f,  0.000f),
                new Vector2( 0.026f,  0.020f),
                new Vector2( 0.000f,  0.026f),
                new Vector2(-0.026f,  0.020f),
                new Vector2(-0.036f,  0.000f),
                new Vector2(-0.026f, -0.020f),
                new Vector2( 0.000f, -0.026f),
                new Vector2( 0.026f, -0.020f)
            }, new[]
            {
                (0.000f, 0.72f, 0.72f, 0f, 0f),
                (0.005f, 1.00f, 1.00f, 0f, 0f),
                (0.014f, 1.00f, 1.00f, 0f, 0f),
                (0.018f, 0.70f, 0.70f, 0f, 0f)
            });
            GameObject soapBar = CreateMeshObject(sinkTop.transform, "Sabonete_Usado", soapMesh, pal.soapPastelPink, new Vector3(-4.40f, 0.903f, 5.60f), new Vector3(0f, 70f, 0f), castShadows: true);
            SnapToSupportTop(soapBar, soapDish);

            // Copo com escova de dentes apoiada exato no fundo interno do copo (Y=0.906)
            GameObject brushGlass = BuildDrinkingGlass(sinkTop.transform, "Copo_Escova_Cian", new Vector3(-4.41f, 0.890f, 5.84f), pal);
            SnapToSupportTop(brushGlass, sinkRimW);
            GameObject toothbrushGrp = CreateContainer(sinkTop.transform, "Escova_Dentes_Grp");
            toothbrushGrp.transform.localPosition = new Vector3(-4.406f, 0.986f, 5.842f);
            toothbrushGrp.transform.localRotation = Quaternion.Euler(14f, 12f, -12f);
            CreateDetailBox(toothbrushGrp.transform, "Escova_Dentes_Unica", new Vector3(0f, -0.020f, 0f), new Vector3(0.010f, 0.115f, 0.010f), pal.plasticSlate);
            CreateDetailBox(toothbrushGrp.transform, "Pescoco_Escova", new Vector3(0f, 0.052f, 0f), new Vector3(0.007f, 0.032f, 0.007f), pal.plasticSlate);
            CreateDetailBox(toothbrushGrp.transform, "Cabeca_Escova", new Vector3(0f, 0.078f, 0f), new Vector3(0.011f, 0.026f, 0.009f), pal.plasticSlate);
            CreateDetailBox(toothbrushGrp.transform, "Cerdas_Escova", new Vector3(0f, 0.078f, 0.012f), new Vector3(0.0096f, 0.022f, 0.012f), pal.porcelainWhite);

            // Bisnaga de pasta de dente achatada + tampinha rosqueada e barbeador apoiados em sinkRimE
            GameObject pasteGrp = CreateContainer(sinkTop.transform, "Pasta_Dente_Grp");
            pasteGrp.transform.localPosition = new Vector3(-3.635f, 0.890f, 5.87f);
            pasteGrp.transform.localRotation = Quaternion.Euler(0f, -16f, 0f);
            Mesh pasteTubeMesh = CreateClosedPolygonLoftMesh("Mesh_Bisnaga_Pasta", new[]
            {
                new Vector2( 0.014f,  0.000f),
                new Vector2( 0.010f,  0.010f),
                new Vector2( 0.000f,  0.014f),
                new Vector2(-0.010f,  0.010f),
                new Vector2(-0.014f,  0.000f),
                new Vector2(-0.010f, -0.010f),
                new Vector2( 0.000f, -0.014f),
                new Vector2( 0.010f, -0.010f)
            }, new[]
            {
                (0.000f, 1.15f, 0.28f, 0f, 0f),
                (0.055f, 1.00f, 0.70f, 0f, 0f),
                (0.095f, 0.92f, 0.92f, 0f, 0f),
                (0.110f, 0.36f, 0.36f, 0f, 0f)
            });
            GameObject tubeBody = CreateMeshObject(pasteGrp.transform, "Tubo_Pasta_Dente", pasteTubeMesh, pal.porcelainWhite, new Vector3(0.050f, 0.014f, 0f), new Vector3(0f, 0f, 90f), castShadows: true);
            SnapToSupportTop(tubeBody, sinkRimE);
            CreateDetailCylinder(pasteGrp.transform, "Tampa_Pasta_Dente", new Vector3(-0.064f, tubeBody.transform.localPosition.y, 0f), new Vector3(0.012f, 0.007f, 0.012f), pal.calendarRed, new Vector3(0f, 0f, 90f));

            GameObject razorGrp = CreateContainer(sinkTop.transform, "Barbeador_Grp");
            razorGrp.transform.localPosition = new Vector3(-3.655f, 0.890f, 5.735f);
            razorGrp.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            CreateDetailCylinder(razorGrp.transform, "Barbeador_Cabo", new Vector3(-0.022f, 0.006f, 0f), new Vector3(0.010f, 0.044f, 0.010f), pal.metalDark, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(razorGrp.transform, "Barbeador_Gripo", new Vector3(-0.010f, 0.006f, 0f), new Vector3(0.012f, 0.018f, 0.012f), pal.shadowBlack, new Vector3(0f, 0f, 90f));
            CreateDetailBox(razorGrp.transform, "Barbeador_Cabeca", new Vector3(0.028f, 0.008f, 0f), new Vector3(0.014f, 0.014f, 0.038f), pal.stainlessSteel, new Vector3(0f, 0f, -15f));
            CreateDetailBox(razorGrp.transform, "Barbeador_Lamina", new Vector3(0.033f, 0.011f, 0f), new Vector3(0.004f, 0.004f, 0.034f), pal.porcelainWhite, new Vector3(0f, 0f, -15f));
            SnapToSupportTop(razorGrp, sinkRimE);
            return faucetCtrl;
        }

        private static void BuildBathroomToiletAndFixtures(Transform root, GreyboxPalette pal)
        {
            GameObject toilet = CreateContainer(root, "Vaso_Sanitario");
            toilet.transform.localPosition = new Vector3(-6.08f, 0f, 3.64f);

            Mesh bowlMesh = CreateClosedPolygonLoftMesh("Mesh_Vaso_Base_Bacia", new[]
            {
                new Vector2( 0.24f,  0.00f),
                new Vector2( 0.16f,  0.14f),
                new Vector2( 0.00f,  0.19f),
                new Vector2(-0.16f,  0.16f),
                new Vector2(-0.25f,  0.09f),
                new Vector2(-0.25f, -0.09f),
                new Vector2(-0.16f, -0.16f),
                new Vector2( 0.00f, -0.19f),
                new Vector2( 0.16f, -0.14f)
            }, new[]
            {
                (0.000f, 0.62f, 0.74f, -0.01f, 0f),
                (0.140f, 0.56f, 0.68f, -0.01f, 0f),
                (0.285f, 0.72f, 0.84f,  0.02f, 0f),
                (0.365f, 1.00f, 1.02f,  0.03f, 0f),
                (0.405f, 1.02f, 1.04f,  0.03f, 0f)
            });
            CreateMeshObject(toilet.transform, "Pedestal_Bacia_Vaso", bowlMesh, pal.porcelainWhite, Vector3.zero, castShadows: true);
            BoxCollider bowlCol = toilet.AddComponent<BoxCollider>();
            bowlCol.center = new Vector3(0.04f, 0.21f, 0f);
            bowlCol.size = new Vector3(0.44f, 0.42f, 0.38f);

            Mesh lidMesh = CreateClosedPolygonLoftMesh("Mesh_Tampa_Assento_Vaso", new[]
            {
                new Vector2( 0.22f,  0.00f),
                new Vector2( 0.15f,  0.13f),
                new Vector2( 0.00f,  0.18f),
                new Vector2(-0.15f,  0.15f),
                new Vector2(-0.19f,  0.00f),
                new Vector2(-0.15f, -0.15f),
                new Vector2( 0.00f, -0.18f),
                new Vector2( 0.15f, -0.13f)
            }, new[]
            {
                (0.000f, 1.00f, 1.00f, 0f, 0f),
                (0.014f, 0.98f, 0.98f, 0f, 0f),
                (0.022f, 0.90f, 0.90f, 0f, 0f)
            });
            CreateMeshObject(toilet.transform, "Assento_Vaso", lidMesh, pal.paperWhite, new Vector3(0.055f, 0.405f, 0f), castShadows: false);
            CreateMeshObject(toilet.transform, "Tampa_Assento_Fechada", lidMesh, pal.porcelainWhite, new Vector3(0.055f, 0.427f, 0f), castShadows: true);
            CreateDetailCylinder(toilet.transform, "Dobradica_Assento_S", new Vector3(-0.155f, 0.422f, -0.075f), new Vector3(0.018f, 0.012f, 0.018f), pal.stainlessSteel);
            CreateDetailCylinder(toilet.transform, "Dobradica_Assento_N", new Vector3(-0.155f, 0.422f,  0.075f), new Vector3(0.018f, 0.012f, 0.018f), pal.stainlessSteel);

            CreateBox(toilet.transform, "Apoio_Caixa_Vaso", new Vector3(-0.285f, 0.405f, 0f), new Vector3(0.16f, 0.05f, 0.28f), pal.porcelainWhite, hasCollider: false);
            Mesh cisternMesh = CreateChamferedRectLoftMesh("Mesh_Vaso_CaixaAcoplada", 0.19f, 0.37f, 0.41f, 0.018f, 0.96f, 0.010f);
            GameObject cisternObj = CreateMeshObject(toilet.transform, "Vaso_CaixaAcoplada", cisternMesh, pal.porcelainWhite, new Vector3(-0.340f, 0.430f, 0f), castShadows: true);
            BoxCollider cisternCol = cisternObj.AddComponent<BoxCollider>();
            cisternCol.center = new Vector3(0f, 0.185f, 0f);
            cisternCol.size = new Vector3(0.19f, 0.37f, 0.41f);
            Mesh cisternLidMesh = CreateChamferedRectLoftMesh("Mesh_Tampa_CaixaAcoplada", 0.214f, 0.032f, 0.434f, 0.020f, 0.92f, 0.008f);
            CreateMeshObject(toilet.transform, "Tampa_CaixaAcoplada", cisternLidMesh, pal.porcelainWhite, new Vector3(-0.335f, 0.800f, 0f), castShadows: true);
            CreateDetailCylinder(toilet.transform, "Parafuso_Pe_Vaso_S", new Vector3(-0.04f, 0.016f, -0.145f), new Vector3(0.016f, 0.012f, 0.016f), pal.porcelainWhite);
            CreateDetailCylinder(toilet.transform, "Parafuso_Pe_Vaso_N", new Vector3(-0.04f, 0.016f,  0.145f), new Vector3(0.016f, 0.012f, 0.016f), pal.porcelainWhite);
            CreateDetailCylinder(toilet.transform, "Aro_Botao_Descarga", new Vector3(-0.335f, 0.834f, 0f), new Vector3(0.044f, 0.004f, 0.044f), pal.stainlessSteel);
            CreateDetailCylinder(toilet.transform, "Botao_Descarga_Half", new Vector3(-0.335f, 0.839f, -0.007f), new Vector3(0.022f, 0.003f, 0.012f), pal.stainlessSteel);
            CreateDetailCylinder(toilet.transform, "Botao_Descarga_Full", new Vector3(-0.335f, 0.839f,  0.007f), new Vector3(0.022f, 0.003f, 0.012f), pal.stainlessSteel);

            // Engate flexível cromado ligando a parede Oeste (-6.530 -> local -0.450) ao fundo da caixa acoplada
            CreateDetailCylinder(toilet.transform, "Canopia_Engate_Vaso", new Vector3(-0.448f, 0.25f, -0.14f), new Vector3(0.038f, 0.004f, 0.038f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            BuildRoutedWallCable(toilet.transform, "Engate_Flexivel_Vaso", "Mesh_Engate_Flexivel_Vaso", new[]
            {
                new Vector3(-0.445f, 0.250f, -0.14f),
                new Vector3(-0.380f, 0.250f, -0.14f),
                new Vector3(-0.340f, 0.310f, -0.13f),
                new Vector3(-0.340f, 0.435f, -0.12f)
            }, pal.stainlessSteel, radius: 0.0065f);

            // Papeleira de parede detalhada com suporte, eixo cromado e rolo com folha solta
            GameObject holder = CreateContainer(root, "Papeleira_Parede");
            holder.transform.localPosition = new Vector3(-6.530f, 0.72f, 4.02f);
            CreateDetailBox(holder.transform, "Base_Papeleira", new Vector3(0.010f, 0f, 0f), new Vector3(0.020f, 0.11f, 0.15f), pal.porcelainWhite);
            CreateDetailBox(holder.transform, "Braco_Papeleira_Sul", new Vector3(0.044f, 0f, -0.064f), new Vector3(0.068f, 0.018f, 0.016f), pal.stainlessSteel);
            CreateDetailBox(holder.transform, "Braco_Papeleira_Norte", new Vector3(0.044f, 0f,  0.064f), new Vector3(0.068f, 0.018f, 0.016f), pal.stainlessSteel);
            CreateDetailCylinder(holder.transform, "Eixo_Papeleira", new Vector3(0.064f, 0f, 0f), new Vector3(0.016f, 0.062f, 0.016f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            BuildToiletPaperRoll(holder.transform, "Rolo_Papel_Uso", new Vector3(0.064f, 0f, -0.052f), new Vector3(90f, 0f, 0f), pal, withHangingSheet: true);

            // Lixeira com pedal frontal cromado, base antiderrapante, aro superior e tampa inox
            GameObject bin = CreateContainer(root, "Lixeira_Pedal_Banheiro");
            bin.transform.localPosition = new Vector3(-6.30f, 0f, 4.16f);
            CreateDetailCylinder(bin.transform, "Base_Lixeira_Borracha", new Vector3(0f, 0.008f, 0f), new Vector3(0.208f, 0.008f, 0.208f), pal.shadowBlack);
            CreateDetailBox(bin.transform, "Pedal_Lixeira", new Vector3(0.112f, 0.014f, 0f), new Vector3(0.044f, 0.010f, 0.058f), pal.stainlessSteel, new Vector3(0f, 0f, -12f));
            CreateDetailBox(bin.transform, "Haste_Pedal_Tras", new Vector3(-0.106f, 0.145f, 0f), new Vector3(0.008f, 0.260f, 0.010f), pal.stainlessSteel);
            CreateDetailCylinder(bin.transform, "Corpo_Lixeira", new Vector3(0f, 0.148f, 0f), new Vector3(0.198f, 0.132f, 0.198f), pal.porcelainWhite, castShadows: true);
            CreateDetailCylinder(bin.transform, "Aro_Lixeira_Inox", new Vector3(0f, 0.282f, 0f), new Vector3(0.208f, 0.006f, 0.208f), pal.metalDark);
            CreateDetailCylinder(bin.transform, "Tampa_Lixeira_Inox", new Vector3(0f, 0.296f, 0f), new Vector3(0.212f, 0.009f, 0.212f), pal.stainlessSteel, castShadows: true);
            CreateDetailCylinder(bin.transform, "Dobradica_Tampa_Lixeira", new Vector3(-0.104f, 0.290f, 0f), new Vector3(0.016f, 0.012f, 0.044f), pal.stainlessSteel);
        }

        private static void BuildBathroomShowerDetails(Transform showerTransform, GreyboxPalette pal)
        {
            Mesh crossMesh = GetOrBuildCrossHandleMesh();

            // Coluna hidráulica, cotovelos, ducha elétrica cônica anos 90 e chuveirinho manual com mangueira conectada
            CreateDetailCylinder(showerTransform, "Canopla_Base_Chuveiro", new Vector3(-0.557f, 0.855f, 0.48f), new Vector3(0.056f, 0.004f, 0.056f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailBox(showerTransform, "Cotovelo_Inf_Chuveiro", new Vector3(-0.528f, 0.855f, 0.48f), new Vector3(0.060f, 0.024f, 0.024f), pal.stainlessSteel);
            CreateDetailCylinder(showerTransform, "Cano_Chuveiro", new Vector3(-0.50f, 1.56f, 0.48f), new Vector3(0.026f, 0.715f, 0.026f), pal.stainlessSteel);
            CreateDetailBox(showerTransform, "Abracadeira_Sup_Chuveiro", new Vector3(-0.533f, 2.18f, 0.48f), new Vector3(0.074f, 0.028f, 0.036f), pal.stainlessSteel);
            CreateDetailCylinder(showerTransform, "Canopla_Topo_Chuveiro", new Vector3(-0.556f, 2.275f, 0.48f), new Vector3(0.064f, 0.006f, 0.064f), pal.porcelainWhite, new Vector3(0f, 0f, 90f));

            Mesh armMesh = CreateFacetedCShapeHandleMesh("Mesh_Braco_Chuveiro", new[]
            {
                new Vector3(-0.552f, 2.275f, 0.48f),
                new Vector3(-0.380f, 2.285f, 0.48f),
                new Vector3(-0.240f, 2.270f, 0.48f),
                new Vector3(-0.200f, 2.238f, 0.48f)
            }, halfThicknessY: 0.012f, halfThicknessZ: 0.012f);
            CreateMeshObject(showerTransform, "Braco_Chuveiro", armMesh, pal.porcelainWhite, Vector3.zero, castShadows: true);

            Vector2[] octUnit = new Vector2[8];
            for (int oi = 0; oi < 8; oi++)
            {
                float a = (2f * Mathf.PI * oi) / 8f;
                octUnit[oi] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            Mesh showerHeadMesh = CreateClosedPolygonLoftMesh("Mesh_Ducha_Cone", octUnit, new[]
            {
                (0.000f, 0.095f, 0.095f, 0f, 0f),
                (0.018f, 0.098f, 0.098f, 0f, 0f),
                (0.068f, 0.056f, 0.056f, 0f, 0f),
                (0.088f, 0.024f, 0.024f, 0f, 0f)
            });
            CreateMeshObject(showerTransform, "Ducha", showerHeadMesh, pal.porcelainWhite, new Vector3(-0.20f, 2.155f, 0.48f), castShadows: true);
            CreateDetailCylinder(showerTransform, "Crivo_Ducha", new Vector3(-0.20f, 2.154f, 0.48f), new Vector3(0.16f, 0.003f, 0.16f), pal.metalDark);
            CreateDetailBox(showerTransform, "Seletor_Temp_Chuveiro", new Vector3(-0.20f, 2.238f, 0.424f), new Vector3(0.026f, 0.014f, 0.020f), pal.calendarRed);

            // Chuveirinho manual no suporte da parede + mangueira contínua conectada à ducha
            CreateDetailBox(showerTransform, "Suporte_Ducha_Manual", new Vector3(-0.545f, 1.48f, 0.34f), new Vector3(0.032f, 0.044f, 0.032f), pal.porcelainWhite);
            CreateDetailCylinder(showerTransform, "Ducha_Manual_Cabo", new Vector3(-0.505f, 1.46f, 0.34f), new Vector3(0.020f, 0.055f, 0.020f), pal.porcelainWhite, new Vector3(0f, 0f, -18f));
            CreateDetailCylinder(showerTransform, "Ducha_Manual_Bocal", new Vector3(-0.488f, 1.525f, 0.34f), new Vector3(0.046f, 0.012f, 0.046f), pal.porcelainWhite, new Vector3(0f, 0f, -35f));
            BuildRoutedWallCable(showerTransform, "Mangueira_Chuveiro_Curva", "Mesh_Mangueira_Chuveiro", new[]
            {
                new Vector3(-0.265f, 2.175f, 0.455f),
                new Vector3(-0.340f, 1.980f, 0.420f),
                new Vector3(-0.435f, 1.720f, 0.375f),
                new Vector3(-0.495f, 1.490f, 0.340f),
                new Vector3(-0.520f, 1.405f, 0.340f)
            }, pal.porcelainWhite, radius: 0.007f);

            // Registro de pressão com cruzeta
            CreateDetailCylinder(showerTransform, "Canopla_Registro", new Vector3(-0.556f, 1.12f, 0.22f), new Vector3(0.074f, 0.004f, 0.074f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(showerTransform, "Eixo_Registro", new Vector3(-0.534f, 1.12f, 0.22f), new Vector3(0.026f, 0.022f, 0.026f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(showerTransform, "Base_Cruzeta_Chuveiro", new Vector3(-0.512f, 1.12f, 0.22f), new Vector3(0.028f, 0.012f, 0.028f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateMeshObject(showerTransform, "Registro_Chuveiro", crossMesh, pal.stainlessSteel, new Vector3(-0.502f, 1.12f, 0.22f), new Vector3(0f, 0f, -90f), castShadows: true);

            // Cantoneira porta-shampoo com barra de proteção, shampoo, condicionador e sabonete de banho
            GameObject shampooShelf = CreateDetailBox(showerTransform, "Porta_Shampoo_Canto", new Vector3(-0.460f, 1.24f, 0.460f), new Vector3(0.200f, 0.022f, 0.200f), pal.porcelainWhite);
            CreateDetailBox(showerTransform, "Suporte_Shampoo_Oeste", new Vector3(-0.552f, 1.219f, 0.445f), new Vector3(0.016f, 0.022f, 0.140f), pal.stainlessSteel);
            CreateDetailBox(showerTransform, "Suporte_Shampoo_Norte", new Vector3(-0.445f, 1.219f, 0.552f), new Vector3(0.140f, 0.022f, 0.016f), pal.stainlessSteel);
            CreateDetailBox(showerTransform, "Gradinha_Shampoo_Sul", new Vector3(-0.460f, 1.272f, 0.365f), new Vector3(0.194f, 0.008f, 0.008f), pal.stainlessSteel);
            CreateDetailBox(showerTransform, "Gradinha_Shampoo_Leste", new Vector3(-0.365f, 1.272f, 0.460f), new Vector3(0.008f, 0.008f, 0.184f), pal.stainlessSteel);

            GameObject shampooGrp = CreateContainer(showerTransform, "Frasco_Shampoo_Grp");
            shampooGrp.transform.localPosition = new Vector3(-0.405f, 1.251f, 0.415f);
            shampooGrp.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            CreateDetailCylinder(shampooGrp.transform, "Frasco_Shampoo", new Vector3(0f, 0.080f, 0f), new Vector3(0.055f, 0.080f, 0.055f), pal.tileBorderBlue, castShadows: true);
            CreateCylinderLabelSleeve(shampooGrp.transform, "Rotulo_Shampoo", new Vector3(0f, 0.080f, 0f), new Vector3(0.0575f, 0.096f, 0.0575f), pal.details.shampooLabel, pal);
            CreateDetailCylinder(shampooGrp.transform, "Tampa_Shampoo", new Vector3(0f, 0.168f, 0f), new Vector3(0.026f, 0.010f, 0.026f), pal.porcelainWhite);
            SnapToSupportTop(shampooGrp, shampooShelf);

            GameObject condBottle = CreateDetailCylinder(showerTransform, "Frasco_Condicionador", new Vector3(-0.495f, 1.318f, 0.425f), new Vector3(0.048f, 0.067f, 0.048f), pal.paperWhite, castShadows: true);
            SnapToSupportTop(condBottle, shampooShelf);
            CreateDetailCylinder(showerTransform, "Tampa_Condicionador", new Vector3(-0.495f, condBottle.transform.localPosition.y + 0.075f, 0.425f), new Vector3(0.022f, 0.009f, 0.022f), pal.bookTeal);

            Mesh soapBarMesh = CreateChamferedRectLoftMesh("Mesh_Sabonete_Banho", 0.068f, 0.018f, 0.044f, 0.012f, 0.82f, 0.005f);
            GameObject bathSoap = CreateMeshObject(showerTransform, "Sabonete_Box", soapBarMesh, pal.soapPastelPink, new Vector3(-0.495f, 1.251f, 0.505f), new Vector3(0f, -24f, 0f), castShadows: false);
            SnapToSupportTop(bathSoap, shampooShelf);
        }

        private static Mesh GetOrBuildCurtainRingMesh()
        {
            return CreateFacetedLatheVesselMesh("Mesh_Argola_Cortina", 12, new[]
            {
                (-0.003f, 0.0155f),
                ( 0.003f, 0.0155f),
                ( 0.003f, 0.0115f),
                (-0.003f, 0.0115f),
                (-0.003f, 0.0155f)
            });
        }

        private static Mesh GetOrBuildTurnedSofaFootMesh()
        {
            return CreateFacetedLatheVesselMesh("Mesh_Pe_Torneado_Sofa", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.022f),
                (0.012f, 0.024f),
                (0.024f, 0.033f),
                (0.036f, 0.026f),
                (0.044f, 0.031f),
                (0.044f, 0.000f)
            });
        }

        private static void BuildLivingRoomSofaUpgrades(Transform sofaRoot, GreyboxPalette pal)
        {
            // Pés torneados esculpidos com ponteira em latão sob o rodapé contínuo do sofá
            Mesh sofaFootMesh = GetOrBuildTurnedSofaFootMesh();
            float[] footXs = { -0.90f, 0.90f };
            float[] footZs = { -0.35f, 0.31f };
            for (int fx = 0; fx < 2; fx++)
            {
                for (int fz = 0; fz < 2; fz++)
                {
                    CreateDetailCylinder(sofaRoot, $"Sapata_Pe_Sofa_{fx}_{fz}", new Vector3(footXs[fx], 0.003f, footZs[fz]), new Vector3(0.048f, 0.003f, 0.048f), pal.metalBrass);
                    CreateMeshObject(sofaRoot, $"Pe_Sofa_{fx}_{fz}", sofaFootMesh, pal.baseboardWood, new Vector3(footXs[fx], 0.005f, footZs[fz]), Vector3.zero, castShadows: true);
                }
            }

            CreateDetailBox(sofaRoot, "Rodape_Madeira_Sofa", new Vector3(0f, 0.057f, -0.02f), new Vector3(1.98f, 0.026f, 0.80f), pal.baseboardWood);
            CreateDetailBox(sofaRoot, "Base_Sofa", new Vector3(0f, 0.1375f, -0.070f), new Vector3(1.64f, 0.135f, 0.720f), pal.sofaFabric, castShadows: true);
            CreateDetailBox(sofaRoot, "Costas_Estrutura", new Vector3(0f, 0.495f, 0.35f), new Vector3(1.64f, 0.85f, 0.12f), pal.sofaFabric, castShadows: true);
            CreateDetailBox(sofaRoot, "Friso_Topo_Costas_Sofa", new Vector3(0f, 0.927f, 0.35f), new Vector3(1.63f, 0.014f, 0.10f), pal.baseboardWood);

            Mesh seatMesh = CreateChamferedRectLoftMesh("Mesh_Sofa_Assento", 0.546f, 0.180f, 0.720f, 0.026f, 0.94f, 0.018f);
            Mesh backMesh = CreateChamferedRectLoftMesh("Mesh_Sofa_Encosto", 0.540f, 0.450f, 0.175f, 0.024f, 0.91f, 0.018f);
            for (int c = -1; c <= 1; c++)
            {
                float seatBaseY = (c == 0) ? 0.195f : 0.205f;
                CreateMeshObject(sofaRoot, $"Almofada_Assento_{c + 2}", seatMesh, pal.sofaFabric, new Vector3(c * 0.57f, seatBaseY, -0.06f), Vector3.zero, castShadows: true);
                CreateDetailBox(sofaRoot, $"Vivo_Assento_{c + 2}", new Vector3(c * 0.57f, seatBaseY + 0.160f, -0.415f), new Vector3(0.50f, 0.008f, 0.010f), pal.furnitureWood);
                GameObject backCushion = CreateMeshObject(sofaRoot, $"Almofada_Encosto_{c + 2}", backMesh, pal.sofaFabric, new Vector3(c * 0.57f, 0.375f, 0.260f), new Vector3(-8f, 0f, 0f), castShadows: true);
                CreateDetailCylinder(backCushion.transform, $"Botao_Encosto_Sofa_{c + 2}_E", new Vector3(-0.11f, 0.23f, -0.088f), new Vector3(0.018f, 0.004f, 0.018f), pal.baseboardWood, new Vector3(90f, 0f, 0f));
                CreateDetailCylinder(backCushion.transform, $"Botao_Encosto_Sofa_{c + 2}_D", new Vector3( 0.11f, 0.23f, -0.088f), new Vector3(0.018f, 0.004f, 0.018f), pal.baseboardWood, new Vector3(90f, 0f, 0f));
            }

            Mesh armCapMesh = CreateChamferedRectLoftMesh("Mesh_Sofa_Braco_Topo", 0.184f, 0.120f, 0.880f, 0.022f, 0.89f, 0.020f);
            for (int side = -1; side <= 1; side += 2)
            {
                string tag = side < 0 ? "Esq" : "Dir";
                float x = side * 0.91f;
                CreateDetailBox(sofaRoot, $"Sofa_Braco_{tag}", new Vector3(x, 0.295f, -0.02f), new Vector3(0.18f, 0.450f, 0.88f), pal.sofaFabric, castShadows: true);
                CreateMeshObject(sofaRoot, $"Sofa_Braco_Topo_{tag}", armCapMesh, pal.sofaFabric, new Vector3(x, 0.520f, -0.02f), Vector3.zero, castShadows: true);
                CreateDetailBox(sofaRoot, $"Painel_Frontal_Braco_{tag}", new Vector3(x, 0.285f, -0.464f), new Vector3(0.132f, 0.360f, 0.008f), pal.baseboardWood);
            }

            Mesh throwPillowMesh = CreateChamferedRectLoftMesh("Mesh_Sofa_Almofada_Canto", 0.32f, 0.30f, 0.12f, 0.032f, 0.78f, 0.022f);
            CreateMeshObject(sofaRoot, "Almofada_Canto", throwPillowMesh, pal.fabricOlive, new Vector3(-0.70f, 0.340f, 0.08f), new Vector3(-14f, 24f, 12f), castShadows: true);

            // Manta contínua dobrada sobre o assento direito e tombando suavemente pela borda frontal
            const int nu = 8;
            const int nv = 8;
            var outPts = new Vector3[nu + 1, nv + 1];
            var inPts = new Vector3[nu + 1, nv + 1];
            Vector3[] profilePts =
            {
                new Vector3(0f, 0.220f, -0.434f),
                new Vector3(0f, 0.310f, -0.432f),
                new Vector3(0f, 0.368f, -0.426f),
                new Vector3(0f, 0.388f, -0.398f),
                new Vector3(0f, 0.388f, -0.310f),
                new Vector3(0f, 0.388f, -0.215f),
                new Vector3(0f, 0.388f, -0.120f),
                new Vector3(0f, 0.388f, -0.025f),
                new Vector3(0f, 0.388f,  0.070f)
            };
            Vector3[] profileNormals =
            {
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0.45f, -0.89f).normalized,
                new Vector3(0f, 0.85f, -0.52f).normalized,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            for (int i = 0; i <= nu; i++)
            {
                float u = (float)i / nu;
                float x = Mathf.Lerp(0.36f, 0.80f, u);
                float edgeSway = 0.008f * Mathf.Sin(u * Mathf.PI * 2.5f);
                for (int j = 0; j <= nv; j++)
                {
                    float v = (float)j / nv;
                    float ripple = 0.0035f * Mathf.Sin(u * Mathf.PI * 3.0f + v * 2.2f);
                    float zSkew = Mathf.Lerp(-0.018f, 0.022f, u);
                    Vector3 basePt = profilePts[j] + new Vector3(x, (j == 0 ? edgeSway : 0f) + (j >= 4 ? ripple : 0f), (j >= 3 ? zSkew : 0f));
                    Vector3 n = profileNormals[j];
                    float thick = (j == 0 || j == nv) ? 0.011f : 0.017f;
                    inPts[i, j] = basePt;
                    outPts[i, j] = basePt + n * thick;
                }
            }

            Mesh sofaThrowMesh = CreateClosedThickGridShellMesh("Mesh_Manta_Sofa", outPts, inPts);
            CreateMeshObject(sofaRoot, "Manta_Amarrotada_Cian", sofaThrowMesh, pal.throwBlanket, Vector3.zero, Vector3.zero, castShadows: true);
        }

        private static void BuildLivingRoomArmchairUpgrades(Transform armchairRoot, GreyboxPalette pal)
        {
            Mesh chairFootMesh = CreateFacetedLatheVesselMesh("Mesh_Pe_Torneado_Poltrona", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.020f),
                (0.016f, 0.022f),
                (0.032f, 0.028f),
                (0.048f, 0.023f),
                (0.056f, 0.027f),
                (0.056f, 0.000f)
            });
            for (int px = -1; px <= 1; px += 2)
            {
                for (int pz = -1; pz <= 1; pz += 2)
                {
                    CreateDetailCylinder(armchairRoot, $"Sapata_Pe_Polt_{px}_{pz}", new Vector3(px * 0.32f, 0.003f, pz * 0.30f), new Vector3(0.044f, 0.003f, 0.044f), pal.metalBrass);
                    CreateMeshObject(armchairRoot, $"Pe_Polt_{px}_{pz}", chairFootMesh, pal.furnitureWood, new Vector3(px * 0.32f, 0.004f, pz * 0.30f), Vector3.zero, castShadows: true);
                }
            }

            CreateDetailBox(armchairRoot, "Base_Madeira_Poltrona", new Vector3(0f, 0.072f, 0f), new Vector3(0.76f, 0.028f, 0.74f), pal.baseboardWood);
            CreateDetailBox(armchairRoot, "Base_Poltrona", new Vector3(0f, 0.168f, 0f), new Vector3(0.78f, 0.164f, 0.76f), pal.sofaFabric, castShadows: true);

            Mesh seatMesh = CreateChamferedRectLoftMesh("Mesh_Poltrona_Almofada", 0.600f, 0.145f, 0.640f, 0.026f, 0.94f, 0.016f);
            CreateMeshObject(armchairRoot, "Almofada_Poltrona", seatMesh, pal.fabricOlive, new Vector3(0f, 0.250f, -0.03f), Vector3.zero, castShadows: true);

            CreateDetailBox(armchairRoot, "Costas_Estrutura_Poltrona", new Vector3(0f, 0.440f, 0.340f), new Vector3(0.78f, 0.710f, 0.08f), pal.sofaFabric, castShadows: true);
            Mesh backMesh = CreateChamferedRectLoftMesh("Mesh_Poltrona_Encosto", 0.590f, 0.500f, 0.145f, 0.024f, 0.91f, 0.018f);
            GameObject chairBack = CreateMeshObject(armchairRoot, "Encosto_Poltrona", backMesh, pal.sofaFabric, new Vector3(0f, 0.380f, 0.285f), new Vector3(-8f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(chairBack.transform, "Botao_Encosto_Poltrona_E", new Vector3(-0.11f, 0.25f, -0.073f), new Vector3(0.018f, 0.004f, 0.018f), pal.baseboardWood, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(chairBack.transform, "Botao_Encosto_Poltrona_D", new Vector3( 0.11f, 0.25f, -0.073f), new Vector3(0.018f, 0.004f, 0.018f), pal.baseboardWood, new Vector3(90f, 0f, 0f));

            Mesh armCapMesh = CreateChamferedRectLoftMesh("Mesh_Poltrona_Braco_Topo", 0.144f, 0.080f, 0.740f, 0.018f, 0.89f, 0.016f);
            for (int side = -1; side <= 1; side += 2)
            {
                string tag = side < 0 ? "Esq" : "Dir";
                float x = side * 0.35f;
                CreateDetailBox(armchairRoot, $"Braco_{tag}_Poltrona", new Vector3(x, 0.313f, 0f), new Vector3(0.14f, 0.454f, 0.74f), pal.sofaFabric, castShadows: true);
                CreateMeshObject(armchairRoot, $"Braco_{tag}_Topo_Poltrona", armCapMesh, pal.sofaFabric, new Vector3(x, 0.540f, 0f), Vector3.zero, castShadows: true);
                CreateDetailBox(armchairRoot, $"Friso_Braco_Polt_{tag}", new Vector3(x, 0.320f, -0.374f), new Vector3(0.104f, 0.320f, 0.008f), pal.baseboardWood);
            }

            // Manta contínua em crochê abraçando o topo e a lateral externa do braço esquerdo sem quebras
            const int nu = 8;
            const int nv = 8;
            var outPts = new Vector3[nu + 1, nv + 1];
            var inPts = new Vector3[nu + 1, nv + 1];
            Vector3[] armCurvePts =
            {
                new Vector3(-0.426f, 0.300f, 0f),
                new Vector3(-0.425f, 0.435f, 0f),
                new Vector3(-0.424f, 0.555f, 0f),
                new Vector3(-0.415f, 0.614f, 0f),
                new Vector3(-0.375f, 0.622f, 0f),
                new Vector3(-0.325f, 0.622f, 0f),
                new Vector3(-0.285f, 0.614f, 0f),
                new Vector3(-0.276f, 0.565f, 0f),
                new Vector3(-0.275f, 0.475f, 0f)
            };
            Vector3[] armCurveNormals =
            {
                Vector3.left,
                Vector3.left,
                Vector3.left,
                new Vector3(-0.62f, 0.78f, 0f).normalized,
                Vector3.up,
                Vector3.up,
                new Vector3(0.62f, 0.78f, 0f).normalized,
                Vector3.right,
                Vector3.right
            };

            for (int i = 0; i <= nu; i++)
            {
                for (int j = 0; j <= nv; j++)
                {
                    float v = (float)j / nv;
                    float z = Mathf.Lerp(-0.26f, 0.22f, v);
                    float wave = 0.003f * Mathf.Sin(v * Mathf.PI * 3.0f);
                    Vector3 basePt = armCurvePts[i] + new Vector3((i <= 2 ? -Mathf.Abs(wave) : 0f), (i == 0 ? wave * 1.4f : 0f), z);
                    Vector3 n = armCurveNormals[i];
                    float thick = (i == 0 || i == nu) ? 0.010f : 0.015f;
                    inPts[i, j] = basePt;
                    outPts[i, j] = basePt + n * thick;
                }
            }

            Mesh chairThrowMesh = CreateClosedThickGridShellMesh("Mesh_Manta_Poltrona", outPts, inPts);
            CreateMeshObject(armchairRoot, "Manta_Croche_Poltrona", chairThrowMesh, pal.calendarRed, Vector3.zero, Vector3.zero, castShadows: true);
        }

        private static void BuildLivingRoomFloorLamp(Transform floorLamp, GreyboxPalette pal)
        {
            Mesh baseMesh = CreateFacetedLatheVesselMesh("Mesh_Base_Coluna_Sala", 14, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.148f),
                (0.014f, 0.145f),
                (0.022f, 0.108f),
                (0.032f, 0.056f),
                (0.042f, 0.034f),
                (0.042f, 0.000f)
            });
            CreateMeshObject(floorLamp, "Base_Coluna", baseMesh, pal.metalBrass, Vector3.zero, castShadows: true);
            CreateDetailCylinder(floorLamp, "Colar_Base_Coluna", new Vector3(0f, 0.056f, 0f), new Vector3(0.044f, 0.014f, 0.044f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(floorLamp, "Haste_Coluna", new Vector3(0f, 0.770f, 0f), new Vector3(0.026f, 0.730f, 0.026f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(floorLamp, "Colar_Meio_Coluna", new Vector3(0f, 0.860f, 0f), new Vector3(0.040f, 0.014f, 0.040f), pal.metalBrass, castShadows: false);
            CreateDetailCylinder(floorLamp, "Soquete_Coluna", new Vector3(0f, 1.494f, 0f), new Vector3(0.036f, 0.024f, 0.036f), pal.metalBrass, castShadows: false);

            // Três hastes da aranha superior ligando o soquete à cúpula sem flutuar
            for (int sp = 0; sp < 3; sp++)
            {
                float a = (sp * 120f + 20f) * Mathf.Deg2Rad;
                Vector3 pInner = new Vector3(Mathf.Cos(a) * 0.016f, 1.500f, Mathf.Sin(a) * 0.016f);
                Vector3 pOuter = new Vector3(Mathf.Cos(a) * 0.140f, 1.675f, Mathf.Sin(a) * 0.140f);
                Vector3 mid = (pInner + pOuter) * 0.5f;
                Vector3 dir = pOuter - pInner;
                float halfLen = dir.magnitude * 0.5f;
                GameObject spoke = CreateDetailCylinder(floorLamp, $"Haste_Armacao_Coluna_{sp}", mid, new Vector3(0.005f, halfLen, 0.005f), pal.metalBrass, castShadows: false);
                spoke.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }

            CreateDetailCylinder(floorLamp, "Cordao_Interruptor_Luminaria", new Vector3(-0.042f, 1.385f, -0.024f), new Vector3(0.004f, 0.095f, 0.004f), pal.metalBrass, castShadows: false);
            CreateDetailSphere(floorLamp, "Pingente_Cordao_Coluna", new Vector3(-0.042f, 1.285f, -0.024f), new Vector3(0.016f, 0.020f, 0.016f), pal.metalBrass, castShadows: false);

            Mesh drumShadeMesh = CreateFacetedLatheVesselMesh("Mesh_Cupula_Coluna_Tecido", 16, new[]
            {
                (-0.140f, 0.194f),
                ( 0.140f, 0.146f),
                ( 0.140f, 0.140f),
                (-0.140f, 0.188f),
                (-0.140f, 0.194f)
            });

            GameObject floorLampOn = CreateContainer(floorLamp, "Estado_Ligado");
            CreateMeshObject(floorLampOn.transform, "Cupula_Tecido", drumShadeMesh, pal.lampEmissive, new Vector3(0f, 1.56f, 0f), Vector3.zero, castShadows: false);
            CreateDetailSphere(floorLampOn.transform, "Bulbo_Coluna_Aceso", new Vector3(0f, 1.56f, 0f), new Vector3(0.068f, 0.094f, 0.068f), pal.lampBulbCore, castShadows: false);

            GameObject floorLampOff = CreateContainer(floorLamp, "Estado_Desligado");
            CreateMeshObject(floorLampOff.transform, "Cupula_Tecido_Apagada", drumShadeMesh, pal.lampUnlitGlass, new Vector3(0f, 1.56f, 0f), Vector3.zero, castShadows: false);
            CreateDetailSphere(floorLampOff.transform, "Bulbo_Coluna_Apagado", new Vector3(0f, 1.56f, 0f), new Vector3(0.068f, 0.094f, 0.068f), pal.metalDark, castShadows: false);
            floorLampOff.SetActive(false);
        }

        private static Mesh GetOrBuildLivingRoomCeramicVaseMesh()
        {
            return CreateFacetedLatheVesselMesh("Mesh_Vaso_Ceramica_Sala", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.044f),
                (0.016f, 0.050f),
                (0.072f, 0.074f),
                (0.132f, 0.062f),
                (0.162f, 0.036f),
                (0.190f, 0.046f),
                (0.194f, 0.042f),
                (0.190f, 0.034f),
                (0.160f, 0.028f),
                (0.055f, 0.042f),
                (0.015f, 0.022f),
                (0.015f, 0.000f)
            });
        }

        private static void BuildKitchenFridgeDetails(Transform fridge, GreyboxPalette pal)
        {
            // Quatro pés niveladores apoiados no piso Y=0 e corpo branco anos 90 com quinas chanfradas
            float[] fx = { -0.295f, 0.295f };
            float[] fz = { -0.270f, 0.240f };
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    int idx = ix * 2 + iz;
                    CreateDetailCylinder(fridge, $"Pe_Geladeira_{idx}", new Vector3(fx[ix], 0.014f, fz[iz]), new Vector3(0.044f, 0.014f, 0.044f), pal.metalDark);
                    CreateDetailCylinder(fridge, $"Bucha_Pe_Geladeira_{idx}", new Vector3(fx[ix], 0.003f, fz[iz]), new Vector3(0.050f, 0.003f, 0.050f), pal.shadowBlack);
                }
            }

            Mesh bodyMesh = CreateChamferedRectLoftMesh("Mesh_Corpo_Geladeira", 0.70f, 1.696f, 0.60f, 0.028f, 0.985f, 0.014f);
            GameObject fridgeBody = CreateMeshObject(fridge, "Corpo_Geladeira", bodyMesh, pal.porcelainWhite, new Vector3(0f, 0.024f, -0.02f), castShadows: true);
            BoxCollider bodyCol = fridgeBody.AddComponent<BoxCollider>();
            bodyCol.center = new Vector3(0f, 0.848f, 0f);
            bodyCol.size = new Vector3(0.70f, 1.696f, 0.60f);

            // Grade inferior recuada com aletas de ventilação
            CreateDetailBox(fridge, "Grade_Rodape_Geladeira", new Vector3(0f, 0.056f, 0.281f), new Vector3(0.65f, 0.068f, 0.008f), pal.shadowBlack);
            for (int l = 0; l < 3; l++)
            {
                CreateDetailBox(fridge, $"Aleta_Grade_Geladeira_{l}", new Vector3(0f, 0.036f + l * 0.020f, 0.289f), new Vector3(0.62f, 0.008f, 0.010f), pal.metalDark, new Vector3(18f, 0f, 0f));
            }

            // Vedações magnéticas de borracha entre corpo e portas + duas portas chanfradas + 3 dobradiças à esquerda
            CreateDetailBox(fridge, "Borracha_Divisoria", new Vector3(0f, 1.196f, 0.288f), new Vector3(0.672f, 0.026f, 0.018f), pal.metalDark);
            CreateDetailBox(fridge, "Vedacao_Inferior", new Vector3(0f, 0.640f, 0.282f), new Vector3(0.674f, 1.055f, 0.012f), pal.metalDark);
            CreateDetailBox(fridge, "Vedacao_Freezer", new Vector3(0f, 1.460f, 0.282f), new Vector3(0.674f, 0.478f, 0.012f), pal.metalDark);

            Mesh doorLowerMesh = CreateChamferedRectLoftMesh("Mesh_Porta_Inferior_Gel", 0.692f, 1.080f, 0.036f, 0.018f, 0.96f, 0.010f);
            Mesh doorUpperMesh = CreateChamferedRectLoftMesh("Mesh_Porta_Freezer_Gel", 0.692f, 0.500f, 0.036f, 0.018f, 0.96f, 0.010f);
            CreateMeshObject(fridge, "Porta_Inferior", doorLowerMesh, pal.porcelainWhite, new Vector3(0f, 0.100f, 0.303f), castShadows: true);
            CreateMeshObject(fridge, "Porta_Freezer", doorUpperMesh, pal.porcelainWhite, new Vector3(0f, 1.210f, 0.303f), castShadows: true);

            float[] hingeYs = { 0.135f, 1.196f, 1.678f };
            for (int hi = 0; hi < hingeYs.Length; hi++)
            {
                CreateDetailBox(fridge, $"Suporte_Dobradica_Gel_{hi}", new Vector3(-0.340f, hingeYs[hi], 0.299f), new Vector3(0.024f, 0.016f, 0.022f), pal.stainlessSteel);
                CreateDetailCylinder(fridge, $"Pino_Dobradica_Gel_{hi}", new Vector3(-0.346f, hingeYs[hi], 0.308f), new Vector3(0.014f, 0.014f, 0.014f), pal.stainlessSteel);
            }

            // Puxadores ergonômicos curvos em C + colares cromados + plaqueta de marca anos 90
            Mesh upperHandleMesh = CreateFacetedCShapeHandleMesh("Mesh_Puxador_Freezer", new[]
            {
                new Vector3(0f,  0.105f, -0.010f),
                new Vector3(0f,  0.105f,  0.022f),
                new Vector3(0f,  0.075f,  0.030f),
                new Vector3(0f, -0.075f,  0.030f),
                new Vector3(0f, -0.105f,  0.022f),
                new Vector3(0f, -0.105f, -0.010f)
            }, halfThicknessY: 0.010f, halfThicknessZ: 0.011f, sideAxis: Vector3.right);
            CreateMeshObject(fridge, "Puxador_Freezer", upperHandleMesh, pal.plasticBeige90s, new Vector3(0.272f, 1.380f, 0.321f), castShadows: true);
            CreateDetailBox(fridge, "Colar_Puxador_F_Sup", new Vector3(0.272f, 1.485f, 0.324f), new Vector3(0.028f, 0.024f, 0.010f), pal.stainlessSteel);
            CreateDetailBox(fridge, "Colar_Puxador_F_Inf", new Vector3(0.272f, 1.275f, 0.324f), new Vector3(0.028f, 0.024f, 0.010f), pal.stainlessSteel);

            Mesh lowerHandleMesh = CreateFacetedCShapeHandleMesh("Mesh_Puxador_Geladeira", new[]
            {
                new Vector3(0f,  0.185f, -0.010f),
                new Vector3(0f,  0.185f,  0.022f),
                new Vector3(0f,  0.145f,  0.030f),
                new Vector3(0f, -0.145f,  0.030f),
                new Vector3(0f, -0.185f,  0.022f),
                new Vector3(0f, -0.185f, -0.010f)
            }, halfThicknessY: 0.010f, halfThicknessZ: 0.011f, sideAxis: Vector3.right);
            CreateMeshObject(fridge, "Puxador_Geladeira", lowerHandleMesh, pal.plasticBeige90s, new Vector3(0.272f, 0.900f, 0.321f), castShadows: true);
            CreateDetailBox(fridge, "Colar_Puxador_G_Sup", new Vector3(0.272f, 1.085f, 0.324f), new Vector3(0.028f, 0.024f, 0.010f), pal.stainlessSteel);
            CreateDetailBox(fridge, "Colar_Puxador_G_Inf", new Vector3(0.272f, 0.715f, 0.324f), new Vector3(0.028f, 0.024f, 0.010f), pal.stainlessSteel);

            CreateDetailBox(fridge, "Placa_Marca_Geladeira", new Vector3(0f, 1.635f, 0.323f), new Vector3(0.110f, 0.022f, 0.006f), pal.stainlessSteel);
            CreateDetailBox(fridge, "Friso_Marca_Geladeira", new Vector3(0f, 1.635f, 0.327f), new Vector3(0.090f, 0.008f, 0.004f), pal.metalDark);

            // Topo da geladeira: toalhinha dobrada, caixa de cereal aterrada e lata de biscoitos
            GameObject topCloth = BuildFoldedClothMeshObject(fridge, "Pano_Topo_Geladeira", "Mesh_Pano_Topo_Geladeira",
                new Vector3(-0.08f, 1.720f, 0.02f), new Vector3(0f, 6f, 0f), new Vector3(1.1f, 0.45f, 1.1f), pal.paperWhite, pal.bookSlateBlue);
            SnapToSupportTop(topCloth, fridgeBody);

            GameObject cerealGrp = CreateContainer(fridge, "Caixa_Cereal_Topo");
            cerealGrp.transform.localPosition = new Vector3(-0.15f, 1.720f, 0.02f);
            cerealGrp.transform.localRotation = Quaternion.Euler(0f, 16f, 0f);
            CreateDetailBox(cerealGrp.transform, "Corpo_Caixa_Cereal", new Vector3(0f, 0.110f, 0f), new Vector3(0.20f, 0.220f, 0.080f), pal.folderKraft, castShadows: true);
            CreateDetailBox(cerealGrp.transform, "Aba_Caixa_Cereal", new Vector3(0f, 0.223f, 0f), new Vector3(0.194f, 0.006f, 0.072f), pal.paperWhite);
            CreateDetailBox(cerealGrp.transform, "Faixa_Caixa_Cereal", new Vector3(0f, 0.125f, 0.0418f), new Vector3(0.172f, 0.092f, 0.004f), pal.calendarRed);
            SnapToSupportTop(cerealGrp, topCloth);

            GameObject biscuitTin = CreateContainer(fridge, "Lata_Biscoito_Geladeira");
            biscuitTin.transform.localPosition = new Vector3(0.17f, 1.720f, -0.02f);
            biscuitTin.transform.localRotation = Quaternion.Euler(0f, -12f, 0f);
            CreateDetailBox(biscuitTin.transform, "Corpo_Lata_Biscoito", new Vector3(0f, 0.060f, 0f), new Vector3(0.19f, 0.120f, 0.19f), pal.bookMustard, castShadows: true);
            CreateDetailBox(biscuitTin.transform, "Tampa_Lata_Biscoito", new Vector3(0f, 0.126f, 0f), new Vector3(0.20f, 0.016f, 0.20f), pal.stainlessSteel, castShadows: true);
            CreateDetailCylinder(biscuitTin.transform, "Puxador_Lata_Biscoito", new Vector3(0f, 0.141f, 0f), new Vector3(0.024f, 0.007f, 0.024f), pal.woodMahogany);
            SnapToSupportTop(biscuitTin, fridgeBody);

            // Papéis na porta do freezer e 3 ímãs coloridos (nomes preservados para o validador)
            GameObject fridgeNotes = CreateContainer(fridge, "Recado_Geladeira_Grp");
            GameObject fridgeListGrp = CreateContainer(fridgeNotes.transform, "Papel_Lista_Grp");
            fridgeListGrp.transform.localPosition = new Vector3(-0.13f, 1.44f, 0.3235f);
            fridgeListGrp.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            CreateDetailBox(fridgeListGrp.transform, "Papel_Lista", Vector3.zero, new Vector3(0.16f, 0.21f, 0.005f), pal.paperWhite);
            CreatePrintedOverlayQuad(fridgeListGrp.transform, "Impressao_Lista_Geladeira", new Vector3(0f, 0f, 0.0052f), new Vector2(0.156f, 0.206f), pal.details.fridgeList, new Vector3(0f, 180f, 0f));

            GameObject fridgeBillGrp = CreateContainer(fridgeNotes.transform, "Aviso_Conta_Grp");
            fridgeBillGrp.transform.localPosition = new Vector3(0.08f, 1.38f, 0.3235f);
            fridgeBillGrp.transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
            CreateDetailBox(fridgeBillGrp.transform, "Aviso_Conta", Vector3.zero, new Vector3(0.15f, 0.19f, 0.005f), pal.paperWhite);
            CreatePrintedOverlayQuad(fridgeBillGrp.transform, "Impressao_Aviso_Conta", new Vector3(0f, 0f, 0.0052f), new Vector2(0.146f, 0.186f), pal.details.fridgeBillNotice, new Vector3(0f, 180f, 0f));

            CreateDetailCylinder(fridgeNotes.transform, "Ima_Geladeira_Coral", new Vector3(-0.14f, 1.522f, 0.3315f), new Vector3(0.028f, 0.005f, 0.028f), pal.magnetCoral, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fridgeNotes.transform, "Anel_Ima_Coral", new Vector3(-0.14f, 1.522f, 0.3392f), new Vector3(0.015f, 0.0025f, 0.015f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fridgeNotes.transform, "Ima_Geladeira_Azul", new Vector3(0.08f, 1.454f, 0.3315f), new Vector3(0.026f, 0.005f, 0.026f), pal.magnetBlue, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(fridgeNotes.transform, "Ima_Geladeira_Ocre", new Vector3(-0.08f, 1.352f, 0.3315f), new Vector3(0.024f, 0.005f, 0.024f), pal.magnetOchre, new Vector3(90f, 0f, 0f));
        }

        private static void BuildKitchenFaucetAndDetergentDetails(Transform counterTransform, GreyboxPalette pal)
        {
            GameObject faucet = CreateContainer(counterTransform, "Torneira_Cozinha");
            CreateDetailCylinder(faucet.transform, "Canopla_Torneira", new Vector3(-5.13f, 0.896f, -5.76f), new Vector3(0.056f, 0.006f, 0.056f), pal.stainlessSteel);
            CreateDetailCylinder(faucet.transform, "Torneira_Coluna", new Vector3(-5.13f, 0.935f, -5.76f), new Vector3(0.036f, 0.034f, 0.036f), pal.stainlessSteel, castShadows: true);
            CreateDetailCylinder(faucet.transform, "Anel_Articulacao_Torneira", new Vector3(-5.13f, 0.972f, -5.76f), new Vector3(0.042f, 0.008f, 0.042f), pal.metalDark);

            Mesh spoutMesh = CreateFacetedCShapeHandleMesh("Mesh_Torneira_Cozinha", new[]
            {
                new Vector3(0f, 0.975f, -5.760f),
                new Vector3(0f, 1.075f, -5.752f),
                new Vector3(0f, 1.120f, -5.695f),
                new Vector3(0f, 1.095f, -5.622f),
                new Vector3(0f, 1.032f, -5.588f)
            }, halfThicknessY: 0.011f, halfThicknessZ: 0.011f, sideAxis: Vector3.right);
            CreateMeshObject(faucet.transform, "Torneira_Pescoco", spoutMesh, pal.stainlessSteel, new Vector3(-5.13f, 0f, 0f), castShadows: true);
            CreateDetailCylinder(faucet.transform, "Arejador_Bica", new Vector3(-5.13f, 1.028f, -5.588f), new Vector3(0.028f, 0.008f, 0.028f), pal.metalDark, new Vector3(14f, 0f, 0f));

            Mesh crossMesh = GetOrBuildCrossHandleMesh();
            float[] regXs = { -5.255f, -5.005f };
            for (int r = 0; r < 2; r++)
            {
                string side = r == 0 ? "Esq" : "Dir";
                Material dotMat = r == 0 ? pal.calendarRed : pal.bookSlateBlue;
                CreateDetailCylinder(faucet.transform, $"Canopla_Reg_{side}", new Vector3(regXs[r], 0.896f, -5.76f), new Vector3(0.044f, 0.005f, 0.044f), pal.stainlessSteel);
                CreateDetailCylinder(faucet.transform, $"Registro_{side}", new Vector3(regXs[r], 0.916f, -5.76f), new Vector3(0.022f, 0.016f, 0.022f), pal.stainlessSteel);
                CreateMeshObject(faucet.transform, $"Cruzeta_Reg_{side}", crossMesh, pal.stainlessSteel, new Vector3(regXs[r], 0.932f, -5.76f), new Vector3(0f, 45f, 0f), castShadows: true);
                CreateDetailCylinder(faucet.transform, $"Indicador_Reg_{side}", new Vector3(regXs[r], 0.942f, -5.76f), new Vector3(0.010f, 0.002f, 0.010f), dotMat);
            }

            // Frasco de detergente com ombro chanfrado, pescoço e bico push-pull
            GameObject detBottleGrp = CreateContainer(counterTransform, "Frasco_Detergente_Pia_Grp");
            detBottleGrp.transform.localPosition = new Vector3(-5.59f, 0.890f, -5.72f);
            CreateDetailCylinder(detBottleGrp.transform, "Frasco_Detergente_Pia", new Vector3(0f, 0.072f, 0f), new Vector3(0.062f, 0.072f, 0.062f), pal.porcelainWhite, castShadows: true);
            CreateCylinderLabelSleeve(detBottleGrp.transform, "Rotulo_Detergente", new Vector3(0f, 0.070f, 0f), new Vector3(0.064f, 0.088f, 0.064f), pal.details.detergentLabel, pal, new Vector3(0f, 90f, 0f));
            CreateDetailCylinder(detBottleGrp.transform, "Ombro_Detergente", new Vector3(0f, 0.150f, 0f), new Vector3(0.048f, 0.010f, 0.048f), pal.porcelainWhite);
            CreateDetailCylinder(detBottleGrp.transform, "Gargalo_Detergente", new Vector3(0f, 0.166f, 0f), new Vector3(0.026f, 0.008f, 0.026f), pal.porcelainWhite);
            CreateDetailCylinder(detBottleGrp.transform, "Tampa_PushPull_Detergente", new Vector3(0f, 0.180f, 0f), new Vector3(0.028f, 0.008f, 0.028f), pal.paperWhite);
            CreateDetailCylinder(detBottleGrp.transform, "Bico_Detergente", new Vector3(0f, 0.194f, 0f), new Vector3(0.014f, 0.008f, 0.014f), pal.calendarRed);
        }

        private static void BuildKitchenDishDrainerDetails(Transform drainer, GameObject drainerTray, GreyboxPalette pal)
        {
            // Aba de escoamento para a cuba + grade metálica tubular em 4 lados e arcos divisores
            CreateDetailBox(drainer.transform, "Bico_Escoamento_Escorredor", new Vector3(-0.24f, 0.003f, 0f), new Vector3(0.070f, 0.006f, 0.16f), pal.stainlessSteel, new Vector3(0f, 0f, -4f));
            CreateDetailBox(drainer.transform, "Trilho_Sup_Oeste", new Vector3(-0.200f, 0.095f, 0f), new Vector3(0.008f, 0.008f, 0.324f), pal.stainlessSteel);
            CreateDetailBox(drainer.transform, "Trilho_Sup_Leste", new Vector3( 0.200f, 0.095f, 0f), new Vector3(0.008f, 0.008f, 0.324f), pal.stainlessSteel);
            CreateDetailBox(drainer.transform, "Trilho_Sup_Norte", new Vector3(0f, 0.095f,  0.158f), new Vector3(0.392f, 0.008f, 0.008f), pal.stainlessSteel);
            CreateDetailBox(drainer.transform, "Trilho_Sup_Sul",   new Vector3(0f, 0.095f, -0.158f), new Vector3(0.392f, 0.008f, 0.008f), pal.stainlessSteel);

            float[] postX = { -0.200f, 0.200f };
            float[] postZ = { -0.158f, 0.158f };
            for (int px = 0; px < 2; px++)
            {
                for (int pz = 0; pz < 2; pz++)
                {
                    CreateDetailCylinder(drainer.transform, $"Pilar_Escorredor_{px}_{pz}", new Vector3(postX[px], 0.053f, postZ[pz]), new Vector3(0.009f, 0.045f, 0.009f), pal.stainlessSteel);
                }
            }

            for (int d = 0; d < 4; d++)
            {
                float dx = -0.132f + d * 0.055f;
                CreateDetailBox(drainer.transform, $"Divisor_Prato_{d}", new Vector3(dx, 0.050f, 0.01f), new Vector3(0.006f, 0.072f, 0.24f), pal.stainlessSteel);
            }

            Mesh dinnerPlateMesh = CreateFacetedLatheVesselMesh("Mesh_FlatDinnerPlate", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.068f),
                (0.004f, 0.074f),
                (0.016f, 0.112f),
                (0.019f, 0.110f),
                (0.009f, 0.072f),
                (0.006f, 0.000f)
            });
            for (int p = 0; p < 3; p++)
            {
                GameObject dryingPlate = CreateMeshObject(drainer.transform, $"Prato_Secando_{p}", dinnerPlateMesh, pal.porcelainWhite,
                    new Vector3(-0.104f + p * 0.055f, 0.118f, 0.01f), new Vector3(0f, 0f, 78f), castShadows: true);
                SnapToSupportTop(dryingPlate, drainerTray);
            }

            GameObject drainerGlass = BuildDrinkingGlass(drainer.transform, "Copo_Emborcado", new Vector3(0.115f, 0.012f, -0.075f), pal, upsideDown: true);
            SnapToSupportTop(drainerGlass, drainerTray);

            GameObject utensilCup = CreateContainer(drainer.transform, "Porta_Talheres_Escorredor");
            utensilCup.transform.localPosition = new Vector3(0.125f, 0.012f, 0.075f);
            Mesh cupMesh = CreateFacetedLatheVesselMesh("Mesh_Copo_Talheres", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.036f),
                (0.100f, 0.042f),
                (0.100f, 0.037f),
                (0.008f, 0.032f),
                (0.008f, 0.000f)
            });
            CreateMeshObject(utensilCup.transform, "Cesto_Talheres_Mesh", cupMesh, pal.plasticBeige90s, Vector3.zero, castShadows: true);
            CreateDetailBox(utensilCup.transform, "Colher_Secando_1", new Vector3(-0.012f, 0.078f, 0.006f), new Vector3(0.010f, 0.130f, 0.004f), pal.stainlessSteel, new Vector3(12f, 0f, 10f));
            CreateDetailBox(utensilCup.transform, "Faca_Secando_2", new Vector3(0.010f, 0.082f, -0.008f), new Vector3(0.012f, 0.138f, 0.003f), pal.stainlessSteel, new Vector3(-10f, 18f, -8f));
        }

        private static void BuildKitchenFramedDoorFront(
            Transform doorPivot, string sideTag, float centerLocalX, float width, float height, float hingeLocalX, float handleLocalX, GreyboxPalette pal)
        {
            BuildOpenFrameTrimBars(doorPivot, $"Moldura_Porta_Pia_{sideTag}", new Vector3(centerLocalX, 0f, 0.0145f), width - 0.044f, height - 0.066f, 0.035f, 0.006f, pal.woodLight);
            CreateDetailBox(doorPivot, $"Miolo_Formica_{sideTag}", new Vector3(centerLocalX, 0f, 0.0135f), new Vector3(width - 0.114f, height - 0.136f, 0.003f), pal.formicaCream);

            CreateDetailCylinder(doorPivot, $"Puxador_{sideTag}", new Vector3(handleLocalX, 0.16f, 0.035f), new Vector3(0.012f, 0.064f, 0.012f), pal.stainlessSteel);
            CreateDetailBox(doorPivot, $"Suporte_Sup_Pux_{sideTag}", new Vector3(handleLocalX, 0.214f, 0.023f), new Vector3(0.014f, 0.012f, 0.020f), pal.stainlessSteel);
            CreateDetailBox(doorPivot, $"Suporte_Inf_Pux_{sideTag}", new Vector3(handleLocalX, 0.106f, 0.023f), new Vector3(0.014f, 0.012f, 0.020f), pal.stainlessSteel);

            float[] hy = { -0.20f, 0.20f };
            for (int i = 0; i < 2; i++)
                CreateDetailCylinder(doorPivot, $"Dobradica_Balcao_{sideTag}_{i}", new Vector3(hingeLocalX, hy[i], 0.012f), new Vector3(0.012f, 0.018f, 0.012f), pal.stainlessSteel);
        }

        private static void BuildKitchenFramedDrawerFront(
            Transform parent, string tag, Vector3 centerLocal, float width, float height, GreyboxPalette pal)
        {
            BuildOpenFrameTrimBars(parent, $"Moldura_Gaveta_{tag}", centerLocal + new Vector3(0f, 0f, 0.0145f), width - 0.056f, height - 0.042f, 0.024f, 0.005f, pal.woodLight);
            CreateDetailCylinder(parent, $"Puxador_Gaveta_{tag}", centerLocal + new Vector3(0f, 0f, 0.036f), new Vector3(0.012f, 0.076f, 0.012f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailBox(parent, $"Suporte_E_Gav_{tag}", centerLocal + new Vector3(-0.064f, 0f, 0.023f), new Vector3(0.012f, 0.014f, 0.022f), pal.stainlessSteel);
            CreateDetailBox(parent, $"Suporte_D_Gav_{tag}", centerLocal + new Vector3( 0.064f, 0f, 0.023f), new Vector3(0.012f, 0.014f, 0.022f), pal.stainlessSteel);
        }

        private static void BuildKitchenCutleryDrawerUtensils(Transform cutleryDrawerRoot, GameObject cutleryBottom, GreyboxPalette pal)
        {
            // Garfos com dentes reais
            for (int k = 0; k < 3; k++)
            {
                GameObject fork = CreateContainer(cutleryDrawerRoot, $"Garfo_{k}");
                fork.transform.localPosition = new Vector3(-0.23f + k * 0.034f, 0f, 0.09f);
                fork.transform.localRotation = Quaternion.Euler(0f, k * 4f, 0f);
                CreateDetailBox(fork.transform, "Cabo_Garfo", new Vector3(0f, 0.0022f, -0.042f), new Vector3(0.010f, 0.0044f, 0.095f), pal.stainlessSteel);
                CreateDetailBox(fork.transform, "Cabeca_Garfo", new Vector3(0f, 0.0022f, 0.015f), new Vector3(0.019f, 0.0040f, 0.020f), pal.stainlessSteel);
                for (int t = -1; t <= 1; t++)
                    CreateDetailBox(fork.transform, $"Dente_Garfo_{t + 1}", new Vector3(t * 0.0065f, 0.0022f, 0.042f), new Vector3(0.003f, 0.0034f, 0.034f), pal.stainlessSteel);
                SnapToSupportTop(fork, cutleryBottom);

                // Facas de mesa com cabo + lâmina mais larga
                GameObject knife = CreateContainer(cutleryDrawerRoot, $"Faca_{k}");
                knife.transform.localPosition = new Vector3(-0.068f + k * 0.030f, 0f, 0.08f);
                CreateDetailBox(knife.transform, "Cabo_Faca", new Vector3(0f, 0.0035f, -0.052f), new Vector3(0.013f, 0.007f, 0.096f), pal.stainlessSteel);
                CreateDetailBox(knife.transform, "Lamina_Faca", new Vector3(0f, 0.0020f, 0.048f), new Vector3(0.016f, 0.003f, 0.105f), pal.stainlessSteel);
                SnapToSupportTop(knife, cutleryBottom);

                // Colheres de chá na terceira divisão
                GameObject spoon = CreateContainer(cutleryDrawerRoot, $"Colher_{k}");
                spoon.transform.localPosition = new Vector3(0.082f + k * 0.034f, 0f, 0.07f);
                spoon.transform.localRotation = Quaternion.Euler(0f, -3f + k * 2f, 0f);
                CreateDetailBox(spoon.transform, "Cabo_Colher", new Vector3(0f, 0.0022f, -0.040f), new Vector3(0.009f, 0.004f, 0.095f), pal.stainlessSteel);
                CreateDetailCylinder(spoon.transform, "Concha_Colher", new Vector3(0f, 0.0035f, 0.022f), new Vector3(0.024f, 0.0035f, 0.032f), pal.stainlessSteel);
                SnapToSupportTop(spoon, cutleryBottom);
            }

            // Colher de pau esculpida com cabo cilíndrico e bojo ovalado
            GameObject woodSpoon = CreateContainer(cutleryDrawerRoot, "Colher_Pau");
            woodSpoon.transform.localPosition = new Vector3(0.215f, 0f, 0.07f);
            woodSpoon.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
            CreateDetailCylinder(woodSpoon.transform, "Cabo_Colher_Pau", new Vector3(0f, 0.006f, -0.036f), new Vector3(0.013f, 0.095f, 0.013f), pal.furnitureWood, new Vector3(90f, 0f, 0f));
            CreateDetailCylinder(woodSpoon.transform, "Bojo_Colher_Pau", new Vector3(0f, 0.005f, 0.082f), new Vector3(0.046f, 0.005f, 0.068f), pal.furnitureWood);
            SnapToSupportTop(woodSpoon, cutleryBottom);
        }

        private static void BuildKitchenUpperCabinetAndSpiceDetails(Transform upperCab, GreyboxPalette pal)
        {
            // Cornija chanfrada superior e cantoneiras ancorando o armário aéreo rente à parede sul (-5.84)
            Mesh crownMesh = CreateChamferedRectLoftMesh("Mesh_Cornija_Aereo_Cozinha", 2.00f, 0.030f, 0.36f, 0.012f, 0.96f, 0.006f);
            CreateMeshObject(upperCab, "Cornija_Aereo_Cozinha", crownMesh, pal.woodMahogany, new Vector3(0f, 0.290f, 0.012f), castShadows: true);
            CreateDetailBox(upperCab, "Suporte_Parede_Aereo_E", new Vector3(-0.76f, 0.25f, -0.165f), new Vector3(0.055f, 0.055f, 0.012f), pal.stainlessSteel);
            CreateDetailBox(upperCab, "Suporte_Parede_Aereo_D", new Vector3( 0.76f, 0.25f, -0.165f), new Vector3(0.055f, 0.055f, 0.012f), pal.stainlessSteel);

            // Três portas superiores mantendo Porta_Aerea_2 com M_Frosted_Glass e moldura ao redor sem obstrução
            for (int d = -1; d <= 1; d++)
            {
                float doorX = d * 0.645f;
                if (d == 0)
                {
                    CreateDetailBox(upperCab, "Fundo_Escuro_Aereo_2", new Vector3(doorX, 0f, 0.166f), new Vector3(0.520f, 0.440f, 0.006f), pal.shadowBlack);
                    CreateDetailBox(upperCab, "Porta_Aerea_2", new Vector3(doorX, 0f, 0.178f), new Vector3(0.535f, 0.450f, 0.012f), pal.details.frostedGlass, castShadows: false);
                    BuildOpenFrameTrimBars(upperCab, "Moldura_Porta_Aerea_2", new Vector3(doorX, 0f, 0.178f), 0.625f, 0.54f, 0.048f, 0.018f, pal.formicaCream);
                }
                else
                {
                    string tag = d < 0 ? "Esq" : "Dir";
                    CreateDetailBox(upperCab, $"Porta_Aerea_{d + 2}", new Vector3(doorX, 0f, 0.170f), new Vector3(0.625f, 0.54f, 0.020f), pal.formicaCream, castShadows: true);
                    BuildOpenFrameTrimBars(upperCab, $"Moldura_Aerea_{tag}", new Vector3(doorX, 0f, 0.183f), 0.55f, 0.46f, 0.032f, 0.006f, pal.woodLight);
                }

                float handleX = doorX + (d == 0 ? 0.24f : (d < 0 ? 0.215f : -0.215f));
                CreateDetailCylinder(upperCab, $"Puxador_Aereo_{d + 2}", new Vector3(handleX, -0.175f, 0.202f), new Vector3(0.011f, 0.048f, 0.011f), pal.stainlessSteel);
                CreateDetailBox(upperCab, $"Suporte_Sup_Aereo_{d + 2}", new Vector3(handleX, -0.132f, 0.192f), new Vector3(0.012f, 0.010f, 0.016f), pal.stainlessSteel);
                CreateDetailBox(upperCab, $"Suporte_Inf_Aereo_{d + 2}", new Vector3(handleX, -0.218f, 0.192f), new Vector3(0.012f, 0.010f, 0.016f), pal.stainlessSteel);

                float hingeX = doorX + (d < 0 ? -0.304f : (d > 0 ? 0.304f : -0.304f));
                CreateDetailCylinder(upperCab, $"Dobradica_Aerea_Sup_{d + 2}", new Vector3(hingeX,  0.175f, 0.170f), new Vector3(0.012f, 0.016f, 0.012f), pal.stainlessSteel);
                CreateDetailCylinder(upperCab, $"Dobradica_Aerea_Inf_{d + 2}", new Vector3(hingeX, -0.175f, 0.170f), new Vector3(0.012f, 0.016f, 0.012f), pal.stainlessSteel);
            }

            // Prateleira de temperos recuada +15mm para longe do friso do azulejo (-5.828) + mão-francesa angular
            GameObject spiceShelf = CreateDetailBox(upperCab, "Prateleira_Temperos", new Vector3(0.38f, -0.472f, -0.045f), new Vector3(0.96f, 0.024f, 0.20f), pal.furnitureWood, castShadows: true);
            CreateDetailBox(upperCab, "Suporte_Esq_Temperos", new Vector3(-0.055f, -0.378f, -0.045f), new Vector3(0.028f, 0.178f, 0.180f), pal.furnitureWood, castShadows: true);
            CreateDetailBox(upperCab, "Suporte_Dir_Temperos", new Vector3( 0.815f, -0.378f, -0.045f), new Vector3(0.028f, 0.178f, 0.180f), pal.furnitureWood, castShadows: true);

            GameObject coffeeTin = CreateDetailCylinder(upperCab, "Lata_Cafe", new Vector3(0.08f, -0.385f, -0.045f), new Vector3(0.10f, 0.075f, 0.10f), pal.calendarRed, castShadows: true);
            SnapToSupportTop(coffeeTin, spiceShelf);
            CreateCylinderLabelSleeve(upperCab, "Rotulo_Lata_Cafe", coffeeTin.transform.localPosition, new Vector3(0.102f, 0.110f, 0.102f), pal.details.coffeeTinLabel, pal, new Vector3(0f, 90f, 0f));
            GameObject coffeeLid = CreateDetailCylinder(upperCab, "Tampa_Lata_Cafe", coffeeTin.transform.localPosition + new Vector3(0f, 0.079f, 0f), new Vector3(0.106f, 0.008f, 0.106f), pal.metalDark);
            SnapToSupportTop(coffeeLid, coffeeTin);

            GameObject sugarJar = CreateDetailCylinder(upperCab, "Pote_Acucar", new Vector3(0.26f, -0.395f, -0.045f), new Vector3(0.11f, 0.065f, 0.11f), pal.porcelainWhite, castShadows: true);
            SnapToSupportTop(sugarJar, spiceShelf);
            CreateCylinderLabelSleeve(upperCab, "Rotulo_Pote_Acucar", sugarJar.transform.localPosition, new Vector3(0.112f, 0.095f, 0.112f), pal.details.sugarJarLabel, pal, new Vector3(0f, 90f, 0f));
            GameObject sugarLid = CreateDetailCylinder(upperCab, "Tampa_Pote_Acucar", sugarJar.transform.localPosition + new Vector3(0f, 0.070f, 0f), new Vector3(0.116f, 0.009f, 0.116f), pal.woodLight);
            SnapToSupportTop(sugarLid, sugarJar);
            CreateDetailCylinder(upperCab, "Puxador_Pote_Acucar", sugarLid.transform.localPosition + new Vector3(0f, 0.015f, 0f), new Vector3(0.022f, 0.007f, 0.022f), pal.woodMahogany);

            GameObject saltShaker = CreateDetailCylinder(upperCab, "Saleiro", new Vector3(0.42f, -0.420f, -0.045f), new Vector3(0.065f, 0.040f, 0.065f), pal.porcelainWhite, castShadows: true);
            SnapToSupportTop(saltShaker, spiceShelf);
            GameObject saltCap = CreateDetailCylinder(upperCab, "Tampa_Saleiro", saltShaker.transform.localPosition + new Vector3(0f, 0.044f, 0f), new Vector3(0.062f, 0.008f, 0.062f), pal.stainlessSteel);
            SnapToSupportTop(saltCap, saltShaker);

            // Garrafa de óleo vegetal âmbar/vidro com rolha dosadora equilibrando a extremidade leste
            GameObject oilGrp = CreateContainer(upperCab, "Garrafa_Oleo_Temperos");
            oilGrp.transform.localPosition = new Vector3(0.62f, -0.460f, -0.045f);
            Mesh oilBody = CreateFacetedLatheVesselMesh("Mesh_Garrafa_Oleo", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.036f),
                (0.130f, 0.036f),
                (0.172f, 0.014f),
                (0.205f, 0.014f),
                (0.205f, 0.010f),
                (0.168f, 0.010f),
                (0.125f, 0.031f),
                (0.008f, 0.031f),
                (0.008f, 0.000f)
            });
            CreateMeshObject(oilGrp.transform, "Vidro_Garrafa_Oleo", oilBody, pal.details.clearGlass, Vector3.zero, castShadows: false);
            CreateDetailCylinder(oilGrp.transform, "Oleo_Interno", new Vector3(0f, 0.060f, 0f), new Vector3(0.056f, 0.052f, 0.056f), pal.bookMustard);
            CreateDetailCylinder(oilGrp.transform, "Rolha_Garrafa_Oleo", new Vector3(0f, 0.212f, 0f), new Vector3(0.022f, 0.010f, 0.022f), pal.folderKraft);
            SnapToSupportTop(oilGrp, spiceShelf);

            // Moedor de pimenta torneado em madeira escura com parafuso em latão
            GameObject pepperMill = CreateContainer(upperCab, "Moedor_Pimenta");
            pepperMill.transform.localPosition = new Vector3(0.76f, -0.460f, -0.045f);
            Mesh pepperMesh = CreateFacetedLatheVesselMesh("Mesh_Moedor_Pimenta", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.024f),
                (0.018f, 0.026f),
                (0.056f, 0.016f),
                (0.088f, 0.021f),
                (0.098f, 0.016f),
                (0.118f, 0.023f),
                (0.128f, 0.015f),
                (0.130f, 0.000f)
            });
            CreateMeshObject(pepperMill.transform, "Corpo_Moedor_Pimenta", pepperMesh, pal.woodMahogany, Vector3.zero, castShadows: true);
            CreateDetailCylinder(pepperMill.transform, "Pino_Moedor_Pimenta", new Vector3(0f, 0.134f, 0f), new Vector3(0.009f, 0.004f, 0.009f), pal.metalBrass);
            SnapToSupportTop(pepperMill, spiceShelf);
        }

        private static void BuildKitchenStoveDetails(Transform stoveObj, GreyboxPalette pal)
        {
            // Quatro pés niveladores apoiados em Y=0 + corpo esmaltado acima dos pés + mesa inox
            float[] sx = { -0.245f, 0.245f };
            float[] sz = { -0.255f, 0.255f };
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    int idx = ix * 2 + iz;
                    CreateDetailCylinder(stoveObj, $"Pe_Fogao_{idx}", new Vector3(sx[ix], 0.018f, sz[iz]), new Vector3(0.036f, 0.018f, 0.036f), pal.metalDark);
                    CreateDetailCylinder(stoveObj, $"Sapata_Fogao_{idx}", new Vector3(sx[ix], 0.003f, sz[iz]), new Vector3(0.042f, 0.003f, 0.042f), pal.shadowBlack);
                }
            }

            CreateBox(stoveObj, "Corpo_Fogao", new Vector3(0f, 0.448f, 0f), new Vector3(0.56f, 0.824f, 0.58f), pal.porcelainWhite);
            CreateDetailBox(stoveObj, "Mesa_Inox_Fogao", new Vector3(0f, 0.869f, 0f), new Vector3(0.57f, 0.018f, 0.59f), pal.stainlessSteel);

            // Tampa de vidro temperado com friso frontal metálico + dobradiças traseiras
            GameObject lidPivot = CreateContainer(stoveObj, "Conjunto_Tampa_Fogao");
            lidPivot.transform.localPosition = new Vector3(0f, 0.884f, -0.278f);
            lidPivot.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            CreateDetailBox(lidPivot.transform, "Tampa_Vidro_levantada", new Vector3(0f, 0.240f, 0f), new Vector3(0.56f, 0.48f, 0.014f), pal.details.clearGlass);
            CreateDetailBox(lidPivot.transform, "Puxador_Tampa_Fogao", new Vector3(0f, 0.486f, 0.002f), new Vector3(0.562f, 0.016f, 0.022f), pal.metalDark);
            CreateDetailBox(lidPivot.transform, "Perfil_Base_Tampa", new Vector3(0f, 0.006f, 0f), new Vector3(0.562f, 0.016f, 0.020f), pal.metalDark);
            CreateDetailBox(stoveObj, "Dobradica_Tampa_Fogao_E", new Vector3(-0.22f, 0.884f, -0.278f), new Vector3(0.028f, 0.026f, 0.028f), pal.metalDark);
            CreateDetailBox(stoveObj, "Dobradica_Tampa_Fogao_D", new Vector3( 0.22f, 0.884f, -0.278f), new Vector3(0.028f, 0.026f, 0.028f), pal.metalDark);

            // Porta do forno em alto relevo + moldura chanfrada do visor + cavidade escura e grade interna
            CreateDetailBox(stoveObj, "Porta_Forno_Painel", new Vector3(0f, 0.395f, 0.296f), new Vector3(0.532f, 0.575f, 0.012f), pal.porcelainWhite, castShadows: true);
            BuildOpenFrameTrimBars(stoveObj, "Moldura_Visor_Forno", new Vector3(0f, 0.380f, 0.307f), 0.436f, 0.336f, 0.024f, 0.014f, pal.metalDark);
            CreateDetailBox(stoveObj, "Cavidade_Forno_Interna", new Vector3(0f, 0.380f, 0.302f), new Vector3(0.388f, 0.288f, 0.004f), pal.shadowBlack);
            CreateDetailBox(stoveObj, "Grade_Forno_Interna", new Vector3(0f, 0.340f, 0.305f), new Vector3(0.350f, 0.006f, 0.002f), pal.stainlessSteel);
            CreateDetailBox(stoveObj, "Visor_Forno", new Vector3(0f, 0.380f, 0.3095f), new Vector3(0.388f, 0.288f, 0.003f), pal.details.clearGlass);
            CreateDetailCylinder(stoveObj, "Puxador_Forno", new Vector3(0f, 0.625f, 0.332f), new Vector3(0.018f, 0.236f, 0.018f), pal.metalDark, new Vector3(0f, 0f, 90f));
            CreateDetailBox(stoveObj, "Suporte_Puxador_Forno_E", new Vector3(-0.20f, 0.625f, 0.315f), new Vector3(0.020f, 0.020f, 0.026f), pal.metalDark);
            CreateDetailBox(stoveObj, "Suporte_Puxador_Forno_D", new Vector3( 0.20f, 0.625f, 0.315f), new Vector3(0.020f, 0.020f, 0.026f), pal.metalDark);

            // Painel de botões + botoeira elétrica + 5 manípulos de gás
            CreateDetailBox(stoveObj, "Painel_Botoes_Fogao", new Vector3(0f, 0.78f, 0.294f), new Vector3(0.556f, 0.072f, 0.008f), pal.plasticBeige90s);
            CreateDetailBox(stoveObj, "Botao_Acendedor_Fogao", new Vector3(-0.242f, 0.78f, 0.303f), new Vector3(0.016f, 0.026f, 0.012f), pal.magnetCoral ?? pal.bookBurgundy, new Vector3(8f, 0f, 0f));
            CreateDetailBox(stoveObj, "Botao_Forno_Luz", new Vector3( 0.242f, 0.78f, 0.303f), new Vector3(0.016f, 0.026f, 0.012f), pal.bookMustard, new Vector3(-8f, 0f, 0f));
            for (int k = -2; k <= 2; k++)
            {
                float knobX = k * 0.090f;
                float knobRot = (k == -2) ? 28f : 0f;
                CreateDetailCylinder(stoveObj, $"Espelho_Botao_Gas_{k + 2}", new Vector3(knobX, 0.78f, 0.2995f), new Vector3(0.042f, 0.002f, 0.042f), pal.stainlessSteel, new Vector3(90f, 0f, 0f));
                CreateDetailCylinder(stoveObj, $"Botao_Gas_{k + 2}", new Vector3(knobX, 0.78f, 0.3075f), new Vector3(0.032f, 0.009f, 0.032f), pal.metalDark, new Vector3(90f, 0f, 0f));
                CreateDetailBox(stoveObj, $"Pega_Botao_Gas_{k + 2}", new Vector3(knobX, 0.78f, 0.318f), new Vector3(0.008f, 0.030f, 0.008f), pal.porcelainWhite, new Vector3(0f, 0f, knobRot));
            }

            // Quatro queimadores centralizados simetricamente na mesa inox + trempes aranha aterradas com 4 pés e hastes radiais
            float[] bx = { -0.132f, 0.132f };
            float[] bz = { -0.105f, 0.135f };
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    Vector3 bCenter = new Vector3(bx[ix], 0.878f, bz[iz]);
                    CreateDetailCylinder(stoveObj, $"Bacia_Boca_{ix}_{iz}", bCenter + new Vector3(0f, 0.003f, 0f), new Vector3(0.145f, 0.003f, 0.145f), pal.metalDark);
                    CreateDetailCylinder(stoveObj, $"Boca_{ix}_{iz}", bCenter + new Vector3(0f, 0.009f, 0f), new Vector3(0.105f, 0.006f, 0.105f), pal.metalDark);
                    CreateDetailCylinder(stoveObj, $"Espalhador_Chama_{ix}_{iz}", bCenter + new Vector3(0f, 0.0165f, 0f), new Vector3(0.070f, 0.003f, 0.070f), pal.metalBrass);

                    BuildOpenFrameTrimBarsXZ(stoveObj, $"Aro_Trempe_{ix}_{iz}", bCenter + new Vector3(0f, 0.022f, 0f), 0.192f, 0.192f, 0.011f, 0.008f, pal.shadowBlack);
                    for (int arm = 0; arm < 4; arm++)
                    {
                        float deg = arm * 90f + 45f;
                        float ang = deg * Mathf.Deg2Rad;
                        float cx = Mathf.Sign(Mathf.Cos(ang)) * 0.090f;
                        float cz = Mathf.Sign(Mathf.Sin(ang)) * 0.090f;
                        // Pé aterrado conectando cada canto da trempe até a Mesa_Inox_Fogao (Y = 0.878..0.898)
                        CreateDetailBox(stoveObj, $"Pe_Trempe_{ix}_{iz}_{arm}", bCenter + new Vector3(cx, 0.010f, cz), new Vector3(0.010f, 0.020f, 0.010f), pal.shadowBlack);
                        // Haste apontando radialmente do canto para o centro da boca (Euler(0, -deg, 0) alinha +X em (cos, 0, +sin) no sistema LH do Unity)
                        Vector3 armPos = bCenter + new Vector3(Mathf.Cos(ang) * 0.082f, 0.0255f, Mathf.Sin(ang) * 0.082f);
                        CreateDetailBox(stoveObj, $"Braco_Trempe_{ix}_{iz}_{arm}", armPos, new Vector3(0.082f, 0.006f, 0.010f), pal.shadowBlack, new Vector3(0f, -deg, 0f));
                    }
                }
            }

            // Bule de café em malha torno fechada com bico curvado, alça em C e tampa com puxador apoiado na boca traseira esquerda
            GameObject kettle = CreateContainer(stoveObj, "Bule_Cafe");
            kettle.transform.localPosition = new Vector3(-0.132f, 0.9065f, -0.105f);
            Mesh kettleMesh = CreateFacetedLatheVesselMesh("Mesh_Bule_Cafe_Corpo", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.062f),
                (0.012f, 0.066f),
                (0.135f, 0.048f),
                (0.145f, 0.052f),
                (0.145f, 0.046f),
                (0.012f, 0.058f),
                (0.008f, 0.000f)
            });
            CreateMeshObject(kettle.transform, "Bule_Cafe_Corpo", kettleMesh, pal.stainlessSteel, Vector3.zero, castShadows: true);
            CreateDetailCylinder(kettle.transform, "Bule_Base_Termica", new Vector3(0f, 0.003f, 0f), new Vector3(0.126f, 0.003f, 0.126f), pal.metalDark);
            CreateDetailCylinder(kettle.transform, "Bule_Tampa", new Vector3(0f, 0.149f, 0f), new Vector3(0.098f, 0.006f, 0.098f), pal.stainlessSteel);
            CreateDetailSphere(kettle.transform, "Bule_Puxador_Preto", new Vector3(0f, 0.164f, 0f), new Vector3(0.026f, 0.020f, 0.026f), pal.shadowBlack);

            // Bico frontal curvado apontando para +X e alça de baquelite em -X
            Mesh spoutMesh = CreateFacetedCShapeHandleMesh("Mesh_Bico_Bule", new[]
            {
                new Vector3(0.052f, 0.046f, 0f),
                new Vector3(0.072f, 0.076f, 0f),
                new Vector3(0.088f, 0.118f, 0f),
                new Vector3(0.104f, 0.134f, 0f)
            }, halfThicknessY: 0.009f, halfThicknessZ: 0.009f);
            CreateMeshObject(kettle.transform, "Bule_Bico", spoutMesh, pal.stainlessSteel, Vector3.zero, castShadows: true);
            Mesh kettleHandle = CreateFacetedCShapeHandleMesh("Mesh_Alca_Bule", new[]
            {
                new Vector3(-0.049f, 0.132f, 0f),
                new Vector3(-0.094f, 0.124f, 0f),
                new Vector3(-0.102f, 0.072f, 0f),
                new Vector3(-0.088f, 0.032f, 0f),
                new Vector3(-0.060f, 0.026f, 0f)
            }, halfThicknessY: 0.008f, halfThicknessZ: 0.009f);
            CreateMeshObject(kettle.transform, "Bule_Cabo_Baquelite", kettleHandle, pal.shadowBlack, Vector3.zero, castShadows: true);

            // Pano de prato contínuo dobrado anatomicamente sobre o puxador do forno (+Z fora do puxador sem atravessar)
            const int twU = 6;
            const int twV = 8;
            var twOut = new Vector3[twU + 1, twV + 1];
            var twIn = new Vector3[twU + 1, twV + 1];
            Vector3[] twProfile =
            {
                new Vector3(0f, 0.455f, 0.319f),
                new Vector3(0f, 0.545f, 0.319f),
                new Vector3(0f, 0.620f, 0.320f),
                new Vector3(0f, 0.638f, 0.325f),
                new Vector3(0f, 0.640f, 0.332f),
                new Vector3(0f, 0.638f, 0.340f),
                new Vector3(0f, 0.616f, 0.345f),
                new Vector3(0f, 0.525f, 0.346f),
                new Vector3(0f, 0.435f, 0.348f)
            };
            Vector3[] twNormals =
            {
                Vector3.back,
                Vector3.back,
                Vector3.back,
                new Vector3(0f, 0.72f, -0.69f).normalized,
                Vector3.up,
                new Vector3(0f, 0.72f,  0.69f).normalized,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };
            for (int i = 0; i <= twU; i++)
            {
                float u = (float)i / twU;
                float x = Mathf.Lerp(-0.182f, -0.008f, u);
                float waveZ = 0.0025f * Mathf.Sin(u * Mathf.PI * 2.5f);
                for (int j = 0; j <= twV; j++)
                {
                    float sway = (j >= 7) ? waveZ : 0f;
                    Vector3 p = twProfile[j] + new Vector3(x, (j == 0 || j == twV) ? 0.003f * Mathf.Sin(u * Mathf.PI * 2f) : 0f, sway);
                    float thick = (j == 0 || j == twV) ? 0.006f : 0.008f;
                    twIn[i, j] = p;
                    twOut[i, j] = p + twNormals[j] * thick;
                }
            }
            Mesh ovenTowelMesh = CreateClosedThickGridShellMesh("Mesh_Pano_Forno", twOut, twIn);
            CreateMeshObject(stoveObj, "Pano_Forno_Frente", ovenTowelMesh, pal.paperWhite, Vector3.zero, castShadows: true);
            CreateDetailBox(stoveObj, "Pano_Forno_Listra_1", new Vector3(-0.095f, 0.474f, 0.3575f), new Vector3(0.170f, 0.011f, 0.0022f), pal.bookSlateBlue);
            CreateDetailBox(stoveObj, "Pano_Forno_Listra_2", new Vector3(-0.095f, 0.456f, 0.3578f), new Vector3(0.170f, 0.008f, 0.0022f), pal.bookBurgundy);
        }

        private static void BuildKitchenSideboardDetails(Transform sideboard, GreyboxPalette pal)
        {
            // Aparador elevado em 4 pés afunilados com ponteiras de latão mantendo tampo exatamente em Y = 0.840f
            float[] lx = { -0.185f, 0.175f };
            float[] lz = { -0.575f, 0.575f };
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    int idx = ix * 2 + iz;
                    CreateDetailCylinder(sideboard, $"Pe_Aparador_{idx}", new Vector3(lx[ix], 0.070f, lz[iz]), new Vector3(0.042f, 0.062f, 0.042f), pal.woodMahogany, castShadows: true);
                    CreateDetailCylinder(sideboard, $"Ponteira_Pe_Aparador_{idx}", new Vector3(lx[ix], 0.009f, lz[iz]), new Vector3(0.045f, 0.009f, 0.045f), pal.metalBrass);
                }
            }

            CreateBox(sideboard, "Corpo_Aparador", new Vector3(0f, 0.470f, 0f), new Vector3(0.44f, 0.680f, 1.24f), pal.furnitureWood);
            Mesh topMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Aparador_Copa", 0.48f, 0.030f, 1.30f, 0.012f, 0.985f, 0.005f);
            GameObject topObj = CreateMeshObject(sideboard, "Tampo_Aparador", topMesh, pal.woodMahogany, new Vector3(-0.01f, 0.810f, 0f), castShadows: true);
            BoxCollider topCol = topObj.AddComponent<BoxCollider>();
            topCol.center = new Vector3(0f, 0.015f, 0f);
            topCol.size = new Vector3(0.48f, 0.030f, 1.30f);

            // Faixa de gaveta superior decorativa + duas portas inferiores com moldura vazada, dobradiças e puxadores em C
            CreateDetailBox(sideboard, "Frente_Gaveta_Aparador", new Vector3(-0.228f, 0.725f, 0f), new Vector3(0.016f, 0.120f, 1.16f), pal.baseboardWood);
            BuildOpenFrameTrimBarsZ(sideboard, "Moldura_Gav_Ap", new Vector3(-0.239f, 0.725f, 0f), 1.08f, 0.088f, 0.014f, 0.006f, pal.woodMahogany);
            CreateDetailCylinder(sideboard, "Puxador_Gav_Ap_S", new Vector3(-0.244f, 0.725f, -0.30f), new Vector3(0.022f, 0.008f, 0.022f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(sideboard, "Puxador_Gav_Ap_N", new Vector3(-0.244f, 0.725f,  0.30f), new Vector3(0.022f, 0.008f, 0.022f), pal.metalBrass, new Vector3(0f, 0f, 90f));

            float[] doorZs = { -0.295f, 0.295f };
            float[] hingeZs = { -0.562f, 0.562f };
            string[] doorTags = { "Sul", "Norte" };
            for (int d = 0; d < 2; d++)
            {
                CreateDetailBox(sideboard, $"Porta_Decor_{doorTags[d]}", new Vector3(-0.228f, 0.405f, doorZs[d]), new Vector3(0.016f, 0.48f, 0.54f), pal.baseboardWood);
                BuildOpenFrameTrimBarsZ(sideboard, $"Moldura_Porta_Ap_{doorTags[d]}", new Vector3(-0.239f, 0.405f, doorZs[d]), 0.46f, 0.40f, 0.022f, 0.006f, pal.woodMahogany);
                CreateDetailBox(sideboard, $"Painel_Porta_Ap_{doorTags[d]}", new Vector3(-0.238f, 0.405f, doorZs[d]), new Vector3(0.004f, 0.356f, 0.416f), pal.furnitureWood);
                CreateDetailCylinder(sideboard, $"Dobradica_Ap_{doorTags[d]}_Sup", new Vector3(-0.236f, 0.545f, hingeZs[d]), new Vector3(0.012f, 0.018f, 0.012f), pal.metalBrass);
                CreateDetailCylinder(sideboard, $"Dobradica_Ap_{doorTags[d]}_Inf", new Vector3(-0.236f, 0.265f, hingeZs[d]), new Vector3(0.012f, 0.018f, 0.012f), pal.metalBrass);
            }
            Mesh apHandleMesh = CreateFacetedCShapeHandleMesh("Mesh_Puxador_Aparador_Copa", new[]
            {
                new Vector3(-0.236f,  0.044f, 0f),
                new Vector3(-0.256f,  0.036f, 0f),
                new Vector3(-0.260f,  0.000f, 0f),
                new Vector3(-0.256f, -0.036f, 0f),
                new Vector3(-0.236f, -0.044f, 0f)
            }, halfThicknessY: 0.005f, halfThicknessZ: 0.005f);
            CreateMeshObject(sideboard, "Puxador_Ap_S", apHandleMesh, pal.metalBrass, new Vector3(0f, 0.46f, -0.075f), Vector3.zero, castShadows: false);
            CreateMeshObject(sideboard, "Puxador_Ap_N", apHandleMesh, pal.metalBrass, new Vector3(0f, 0.46f,  0.075f), Vector3.zero, castShadows: false);
        }

        private static GameObject BuildKitchenPadlockWithShackle(Transform parent, string name, Vector3 pos, float yaw, GreyboxPalette pal)
        {
            GameObject pd = CreateContainer(parent, name);
            pd.transform.localPosition = pos;
            pd.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CreateDetailBox(pd.transform, "Corpo_Cadeado", new Vector3(0f, 0.005f, 0f), new Vector3(0.026f, 0.010f, 0.028f), pal.metalBrass);
            Mesh shackle = CreateFacetedCShapeHandleMesh("Mesh_Argola_Cadeado", new[]
            {
                new Vector3(-0.008f, 0.005f,  0.012f),
                new Vector3(-0.008f, 0.005f,  0.024f),
                new Vector3( 0.000f, 0.005f,  0.027f),
                new Vector3( 0.008f, 0.005f,  0.024f),
                new Vector3( 0.008f, 0.005f,  0.012f)
            }, halfThicknessY: 0.0028f, halfThicknessZ: 0.0028f);
            CreateMeshObject(pd.transform, "Argola_Cadeado", shackle, pal.stainlessSteel, Vector3.zero, castShadows: true);
            return pd;
        }

        private static GameObject BuildKitchenNapkinHolder(Transform parent, string name, Vector3 pos, float yaw, GreyboxPalette pal)
        {
            GameObject nh = CreateContainer(parent, name);
            nh.transform.localPosition = pos;
            nh.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateDetailBox(nh.transform, "Base_Porta_Guardanapo", new Vector3(0f, 0.003f, 0f), new Vector3(0.056f, 0.006f, 0.132f), pal.stainlessSteel);
            Mesh wireMesh = CreateFacetedCShapeHandleMesh("Mesh_Arco_Guardanapo", new[]
            {
                new Vector3(0f, 0.004f, -0.058f),
                new Vector3(0f, 0.068f, -0.054f),
                new Vector3(0f, 0.082f,  0.000f),
                new Vector3(0f, 0.068f,  0.054f),
                new Vector3(0f, 0.004f,  0.058f)
            }, halfThicknessY: 0.003f, halfThicknessZ: 0.003f, sideAxis: Vector3.right);
            CreateMeshObject(nh.transform, "Aba_Esq_Guardanapo", wireMesh, pal.stainlessSteel, new Vector3(-0.022f, 0f, 0f), castShadows: true);
            CreateMeshObject(nh.transform, "Aba_Dir_Guardanapo", wireMesh, pal.stainlessSteel, new Vector3( 0.022f, 0f, 0f), castShadows: true);

            for (int n = -2; n <= 2; n++)
            {
                float nx = n * 0.0055f;
                CreateDetailBox(nh.transform, $"Guardanapo_Dobr_{n + 2}", new Vector3(nx, 0.044f, 0f), new Vector3(0.003f, 0.074f, 0.106f), pal.paperWhite);
            }
            return nh;
        }

        private static void BuildKitchenBreakerBoxEnclosures(Transform breaker, GreyboxPalette pal)
        {
            // Colar perimetral metálico fechando a fresta entre parede (-6.54) e folha da porta (+0.058) sem sobreposição nos cantos
            CreateDetailBox(breaker, "Caixa_Colar_N", new Vector3(0.027f, 0f,  0.215f), new Vector3(0.062f, 0.520f, 0.020f), pal.metalDark);
            CreateDetailBox(breaker, "Caixa_Colar_S", new Vector3(0.027f, 0f, -0.215f), new Vector3(0.062f, 0.520f, 0.020f), pal.metalDark);
            CreateDetailBox(breaker, "Caixa_Colar_T", new Vector3(0.027f,  0.270f, 0f), new Vector3(0.062f, 0.020f, 0.450f), pal.metalDark);
            CreateDetailBox(breaker, "Caixa_Colar_B", new Vector3(0.027f, -0.270f, 0f), new Vector3(0.062f, 0.020f, 0.450f), pal.metalDark);

            // Chicotes coloridos interligando os 3 disjuntores e o bloco de fusível cerâmico aos eletrodutos
            CreateDetailCylinder(breaker, "Fio_Quadro_Vermelho", new Vector3(0.018f, 0.21f, -0.08f), new Vector3(0.007f, 0.075f, 0.007f), pal.calendarRed);
            CreateDetailCylinder(breaker, "Fio_Quadro_Azul",     new Vector3(0.018f, 0.21f,  0.00f), new Vector3(0.007f, 0.075f, 0.007f), pal.bookSlateBlue);
            CreateDetailCylinder(breaker, "Fio_Quadro_Preto",    new Vector3(0.018f, 0.21f,  0.08f), new Vector3(0.007f, 0.075f, 0.007f), pal.shadowBlack);
            CreateDetailCylinder(breaker, "Fio_Quadro_Fusivel",  new Vector3(0.018f, -0.01f, -0.02f), new Vector3(0.007f, 0.080f, 0.007f), pal.shadowBlack);

            // Trilho DIN metálico + parafusos de barramento + clipes de cobre chamuscados no soquete do fusível + dobradiças expostas na porta
            CreateDetailBox(breaker, "Trilho_DIN_Disjuntores", new Vector3(0.030f, 0.10f, 0f), new Vector3(0.005f, 0.032f, 0.30f), pal.stainlessSteel);
            for (int t = -1; t <= 1; t++)
            {
                CreateDetailCylinder(breaker, $"Parafuso_Terminal_Sup_{t + 1}", new Vector3(0.044f, 0.146f, t * 0.08f), new Vector3(0.009f, 0.003f, 0.009f), pal.metalBrass, new Vector3(0f, 0f, 90f));
                CreateDetailCylinder(breaker, $"Parafuso_Terminal_Inf_{t + 1}", new Vector3(0.044f, 0.054f, t * 0.08f), new Vector3(0.009f, 0.003f, 0.009f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            }
            CreateDetailBox(breaker, "Mancha_Queimado_Fusivel", new Vector3(0.0175f, -0.095f, 0f), new Vector3(0.003f, 0.125f, 0.135f), pal.shadowBlack);
            CreateDetailBox(breaker, "Clipe_Cobre_Sup", new Vector3(0.036f, -0.078f, 0f), new Vector3(0.010f, 0.006f, 0.024f), pal.metalBrass);
            CreateDetailBox(breaker, "Clipe_Cobre_Inf", new Vector3(0.036f, -0.122f, 0f), new Vector3(0.010f, 0.006f, 0.024f), pal.metalBrass);
            CreateDetailCylinder(breaker, "Dobradica_Quadro_Sup", new Vector3(0.068f,  0.18f, -0.195f), new Vector3(0.012f, 0.020f, 0.012f), pal.stainlessSteel);
            CreateDetailCylinder(breaker, "Dobradica_Quadro_Inf", new Vector3(0.068f, -0.18f, -0.195f), new Vector3(0.012f, 0.020f, 0.012f), pal.stainlessSteel);
        }

        private static void BuildKitchenPedalBin(Transform root, Vector3 floorPos, GreyboxPalette pal)
        {
            GameObject bin = CreateContainer(root, "Lixeira_Cozinha");
            bin.transform.localPosition = floorPos;
            CreateDetailCylinder(bin.transform, "Base_Lixeira_Cozinha", new Vector3(0f, 0.008f, 0f), new Vector3(0.264f, 0.008f, 0.264f), pal.shadowBlack);
            CreateDetailBox(bin.transform, "Pedal_Lixeira_Cozinha", new Vector3(0.142f, 0.015f, 0f), new Vector3(0.052f, 0.011f, 0.068f), pal.stainlessSteel, new Vector3(0f, 0f, -12f));
            CreateDetailBox(bin.transform, "Haste_Pedal_Cozinha", new Vector3(-0.132f, 0.195f, 0f), new Vector3(0.009f, 0.365f, 0.011f), pal.stainlessSteel);
            GameObject body = CreateDetailCylinder(bin.transform, "Corpo_Lixeira_Cozinha", new Vector3(0f, 0.196f, 0f), new Vector3(0.252f, 0.182f, 0.252f), pal.formicaCream, castShadows: true);
            BoxCollider binCol = body.AddComponent<BoxCollider>();
            binCol.size = new Vector3(0.255f, 0.380f, 0.255f);
            CreateDetailCylinder(bin.transform, "Aro_Lixeira_Cozinha", new Vector3(0f, 0.381f, 0f), new Vector3(0.262f, 0.007f, 0.262f), pal.metalDark);
            CreateDetailCylinder(bin.transform, "Tampa_Lixeira_Cozinha", new Vector3(0f, 0.396f, 0f), new Vector3(0.266f, 0.010f, 0.266f), pal.stainlessSteel, castShadows: true);
            CreateDetailCylinder(bin.transform, "Dobradica_Lixeira_Cozinha", new Vector3(-0.130f, 0.390f, 0f), new Vector3(0.016f, 0.012f, 0.052f), pal.stainlessSteel);
        }

        private static void BuildOfficeRugDetails(Transform root, GreyboxPalette pal)
        {
            GameObject rug = CreateContainer(root, "Tapete_Escritorio");
            rug.transform.localPosition = new Vector3(4.68f, 0f, -2.55f);

            // Base com moldura escura e campo central em Y exato = 0.0100f para apoiar mesa e cadeira
            CreateDetailBox(rug.transform, "Borda_Tapete_Esc", new Vector3(0f, 0.0038f, 0f), new Vector3(3.12f, 0.0076f, 2.60f), pal.rugBorderNavy);
            CreateDetailBox(rug.transform, "Campo_Tapete_Esc", new Vector3(0f, 0.0088f, 0f), new Vector3(2.88f, 0.0024f, 2.36f), pal.rugFabric);

            for (int f = -6; f <= 6; f++)
            {
                float fx = f * 0.22f;
                CreateDetailBox(rug.transform, $"Franja_Esc_N_{f + 6}", new Vector3(fx, 0.0020f,  1.318f), new Vector3(0.052f, 0.004f, 0.038f), pal.paperWhite);
                CreateDetailBox(rug.transform, $"Franja_Esc_S_{f + 6}", new Vector3(fx, 0.0020f, -1.318f), new Vector3(0.052f, 0.004f, 0.038f), pal.paperWhite);
            }
        }

        private static GameObject BuildOfficeDeskStructureAndLamp(Transform desk, GreyboxPalette pal)
        {
            // Tampo em mogno chanfrado mantendo topo exato em Y = 0.780f e colisor integral
            Mesh deskTopMesh = CreateChamferedRectLoftMesh("Mesh_Tampo_Mesa_Escritorio", 0.84f, 0.060f, 1.72f, 0.018f, 0.985f, 0.006f);
            GameObject deskTop = CreateMeshObject(desk, "Tampo_Mesa", deskTopMesh, pal.woodMahogany, new Vector3(0f, 0.720f, 0f), castShadows: true);
            BoxCollider topCol = deskTop.AddComponent<BoxCollider>();
            topCol.center = new Vector3(0f, 0.030f, 0f);
            topCol.size = new Vector3(0.84f, 0.060f, 1.72f);

            // Quatro pés chanfrados sobre ponteiras de latão apoiadas exatamente no tapete (Y = 0.010f)
            Mesh deskLegMesh = CreateChamferedRectLoftMesh("Mesh_Pe_Mesa_Escritorio", 0.058f, 0.696f, 0.058f, 0.008f, 0.96f, 0.004f);
            float[] lx = { -0.34f, 0.34f, -0.34f, 0.34f };
            float[] lz = { -0.78f, -0.78f, 0.78f, 0.78f };
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = CreateMeshObject(desk, $"Pe_Mesa_{i + 1}", deskLegMesh, pal.furnitureWood, new Vector3(lx[i], 0.024f, lz[i]), castShadows: true);
                BoxCollider legCol = leg.AddComponent<BoxCollider>();
                legCol.center = new Vector3(0f, 0.348f, 0f);
                legCol.size = new Vector3(0.058f, 0.696f, 0.058f);
                CreateDetailBox(desk, $"Ponteira_Pe_Mesa_{i + 1}", new Vector3(lx[i], 0.019f, lz[i]), new Vector3(0.064f, 0.018f, 0.064f), pal.metalBrass);
            }

            // Painel traseiro estrutural contínuo entre os pés leste + saias norte/sul
            CreateDetailBox(desk, "Painel_Frontal_Mesa", new Vector3(0.34f, 0.520f, 0f), new Vector3(0.024f, 0.400f, 1.50f), pal.furnitureWood, castShadows: true);
            CreateDetailBox(desk, "Friso_Painel_Mesa", new Vector3(0.324f, 0.520f, 0f), new Vector3(0.008f, 0.330f, 1.42f), pal.woodMahogany);
            CreateDetailBox(desk, "Travessa_Sul_Mesa", new Vector3(0f, 0.680f, 0.78f), new Vector3(0.62f, 0.070f, 0.024f), pal.furnitureWood);
            CreateDetailBox(desk, "Travessa_Norte_Mesa", new Vector3(0f, 0.680f, -0.78f), new Vector3(0.62f, 0.070f, 0.024f), pal.furnitureWood);

            // Luminária de banqueiro clássica preservando os nomes exatos usados por DayManager.SyncBankerLampVisual
            Mesh lampBaseMesh = CreateFacetedLatheVesselMesh("Mesh_Base_Luminaria_Bancaria", 12, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.074f),
                (0.012f, 0.070f),
                (0.020f, 0.044f),
                (0.028f, 0.024f),
                (0.028f, 0.000f)
            });
            GameObject deskLampBase = CreateMeshObject(desk, "Luminaria_Base", lampBaseMesh, pal.metalBrass, new Vector3(0.22f, 0.780f, -0.64f), castShadows: false);
            SnapToSupportTop(deskLampBase, deskTop);

            Mesh neckMesh = CreateFacetedCShapeHandleMesh("Mesh_Haste_Luminaria_Bancaria", new[]
            {
                new Vector3(0.245f, 0.804f, -0.64f),
                new Vector3(0.268f, 0.880f, -0.64f),
                new Vector3(0.258f, 0.968f, -0.64f),
                new Vector3(0.228f, 1.018f, -0.64f),
                new Vector3(0.212f, 1.008f, -0.64f)
            }, halfThicknessY: 0.009f, halfThicknessZ: 0.009f);
            CreateMeshObject(desk, "Luminaria_Haste", neckMesh, pal.metalBrass, Vector3.zero, castShadows: false);

            CreateDetailCylinder(desk, "Porca_Lateral_Cupula_S", new Vector3(0.212f, 0.985f, -0.786f), new Vector3(0.016f, 0.010f, 0.016f), pal.metalBrass, new Vector3(90f, 0f, 0f), castShadows: false);
            CreateDetailCylinder(desk, "Porca_Lateral_Cupula_N", new Vector3(0.212f, 0.985f, -0.494f), new Vector3(0.016f, 0.010f, 0.016f), pal.metalBrass, new Vector3(90f, 0f, 0f), castShadows: false);
            CreateDetailCylinder(desk, "Cordao_Luminaria_Esc", new Vector3(0.244f, 0.892f, -0.602f), new Vector3(0.004f, 0.056f, 0.004f), pal.metalBrass, castShadows: false);
            CreateDetailSphere(desk, "Pingente_Luminaria_Esc", new Vector3(0.244f, 0.834f, -0.602f), new Vector3(0.014f, 0.018f, 0.014f), pal.metalBrass, castShadows: false);

            // Cúpula em arco verde chanfrado espesso e fechado (watertight)
            const int arcU = 6;
            const int lenV = 2;
            var outerPts = new Vector3[arcU + 1, lenV + 1];
            var innerPts = new Vector3[arcU + 1, lenV + 1];
            for (int i = 0; i <= arcU; i++)
            {
                float t = (float)i / arcU;
                float ang = Mathf.Lerp(196f, -16f, t) * Mathf.Deg2Rad;
                float cx = Mathf.Cos(ang);
                float cy = Mathf.Sin(ang);
                for (int j = 0; j <= lenV; j++)
                {
                    float z = Mathf.Lerp(-0.142f, 0.142f, (float)j / lenV);
                    outerPts[i, j] = new Vector3(cx * 0.068f, cy * 0.068f, z);
                    innerPts[i, j] = new Vector3(cx * 0.058f, cy * 0.058f, z);
                }
            }
            Mesh hoodMesh = CreateClosedThickGridShellMesh("Mesh_Cupula_Luminaria_Esc", outerPts, innerPts);

            Vector3 hoodPos = new Vector3(0.212f, 0.972f, -0.64f);
            Vector3 hoodRot = new Vector3(0f, 0f, 14f);

            GameObject deskLampOn = CreateContainer(desk, "Estado_Ligado_Luminaria");
            CreateMeshObject(deskLampOn.transform, "Luminaria_Cupula", hoodMesh, pal.lampGreenGlass, hoodPos, hoodRot, castShadows: false);
            CreateDetailBox(deskLampOn.transform, "Luminaria_Vidro_Verde", new Vector3(0.212f, 0.962f, -0.64f), new Vector3(0.104f, 0.004f, 0.272f), pal.lampBulbCore, hoodRot, castShadows: false);
            CreateDetailCylinder(deskLampOn.transform, "Bulbo_Luminaria_Escritorio", new Vector3(0.205f, 0.976f, -0.64f), new Vector3(0.022f, 0.092f, 0.022f), pal.lampBulbCore, new Vector3(90f, 0f, 0f), castShadows: false);

            GameObject deskLampOff = CreateContainer(desk, "Estado_Desligado_Luminaria");
            CreateMeshObject(deskLampOff.transform, "Luminaria_Cupula_Apagada", hoodMesh, pal.lampMetalOlive, hoodPos, hoodRot, castShadows: false);
            CreateDetailBox(deskLampOff.transform, "Luminaria_Vidro_Apagado", new Vector3(0.212f, 0.962f, -0.64f), new Vector3(0.104f, 0.004f, 0.272f), pal.metalDark, hoodRot, castShadows: false);
            deskLampOff.SetActive(false);

            return deskTop;
        }

        private static void BuildOfficeDeskAccessories(Transform desk, GameObject deskTop, GreyboxPalette pal)
        {
            // Mata-borrão mantendo o centro exatamente em Y = 0.783f para papéis dos Dias 1 e 3
            GameObject blotter = CreateDetailBox(desk, "Mata_Borrao", new Vector3(-0.04f, 0.7815f, 0.04f), new Vector3(0.48f, 0.003f, 0.60f), pal.shadowBlack);
            SnapToSupportTop(blotter, deskTop);
            float[] cx = { -0.252f, 0.172f };
            float[] cz = { -0.232f, 0.312f };
            for (int ix = 0; ix < 2; ix++)
                for (int iz = 0; iz < 2; iz++)
                    CreateDetailBox(desk, $"Canto_MataBorrao_{ix}_{iz}", new Vector3(cx[ix], 0.7845f, cz[iz]), new Vector3(0.048f, 0.003f, 0.048f), pal.woodMahogany);

            GameObject calc = BuildDetailedDeskCalculator(desk, "Calculadora_Contabil", new Vector3(-0.22f, 0.780f, 0.60f), 4f, pal);
            SnapToSupportTop(calc, deskTop);

            // Porta-canetas metálico oco real + canetas com clipe + lápis apontado + régua graduada
            GameObject penCup = CreateContainer(desk, "Porta_Canetas");
            penCup.transform.localPosition = new Vector3(0.29f, 0.780f, -0.18f);
            Mesh cupMesh = CreateFacetedLatheVesselMesh("Mesh_Porta_Canetas_Escritorio", 10, new[]
            {
                (0.000f, 0.000f),
                (0.000f, 0.038f),
                (0.095f, 0.038f),
                (0.095f, 0.034f),
                (0.008f, 0.034f),
                (0.008f, 0.000f)
            });
            CreateMeshObject(penCup.transform, "Copo_Metalico", cupMesh, pal.metalDark, Vector3.zero, castShadows: true);
            GameObject bluePenGrp = CreateContainer(penCup.transform, "Caneta_Azul_Grp");
            bluePenGrp.transform.localPosition = new Vector3(0.012f, 0.072f, 0f);
            bluePenGrp.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            CreateDetailCylinder(bluePenGrp.transform, "Caneta_Azul", Vector3.zero, new Vector3(0.009f, 0.068f, 0.009f), pal.plasticSlate);
            CreateDetailBox(bluePenGrp.transform, "Clipe_Caneta_Azul", new Vector3(0.0055f, 0.044f, 0f), new Vector3(0.003f, 0.026f, 0.003f), pal.stainlessSteel);

            CreateDetailCylinder(penCup.transform, "Caneta_Preta", new Vector3(-0.012f, 0.070f, 0.009f), new Vector3(0.009f, 0.064f, 0.009f), pal.shadowBlack, new Vector3(6f, 0f, 10f));
            GameObject pencilGrp = CreateContainer(penCup.transform, "Lapis_Amarelo_Grp");
            pencilGrp.transform.localPosition = new Vector3(-0.004f, 0.074f, -0.014f);
            pencilGrp.transform.localRotation = Quaternion.Euler(-8f, 0f, 4f);
            CreateDetailCylinder(pencilGrp.transform, "Lapis_Amarelo", Vector3.zero, new Vector3(0.008f, 0.066f, 0.008f), pal.bookMustard);
            CreateDetailCylinder(pencilGrp.transform, "Ferrola_Lapis", new Vector3(0f, 0.068f, 0f), new Vector3(0.0082f, 0.005f, 0.0082f), pal.stainlessSteel);
            CreateDetailCylinder(pencilGrp.transform, "Borracha_Lapis", new Vector3(0f, 0.075f, 0f), new Vector3(0.0080f, 0.004f, 0.0080f), pal.soapPastelPink);
            CreateDetailBox(penCup.transform, "Regua_Plastica", new Vector3(0.004f, 0.088f, -0.006f), new Vector3(0.028f, 0.165f, 0.004f), pal.details.clearGlass, new Vector3(-6f, 12f, 0f));
            SnapToSupportTop(penCup, deskTop);

            // Bandeja dupla aberta na frente (-X) com 4 hastes de sustentação
            GameObject inbox = CreateContainer(desk, "Bandeja_Entrada_2_Andares");
            inbox.transform.localPosition = new Vector3(0.16f, 0.780f, 0.58f);
            float[] trayYs = { 0f, 0.076f };
            for (int t = 0; t < 2; t++)
            {
                float ty = trayYs[t];
                CreateDetailBox(inbox.transform, $"Bandeja_Fundo_{t}", new Vector3(0f, ty + 0.004f, 0f), new Vector3(0.240f, 0.008f, 0.320f), pal.metalDark);
                CreateDetailBox(inbox.transform, $"Bandeja_Tras_{t}", new Vector3(0.112f, ty + 0.024f, 0f), new Vector3(0.010f, 0.032f, 0.294f), pal.metalDark);
                CreateDetailBox(inbox.transform, $"Bandeja_Sul_{t}", new Vector3(0f, ty + 0.024f, -0.152f), new Vector3(0.234f, 0.032f, 0.010f), pal.metalDark);
                CreateDetailBox(inbox.transform, $"Bandeja_Norte_{t}", new Vector3(0f, ty + 0.024f,  0.152f), new Vector3(0.234f, 0.032f, 0.010f), pal.metalDark);
            }
            float[] postX = { -0.110f, 0.110f };
            float[] postZ = { -0.148f, 0.148f };
            for (int px = 0; px < 2; px++)
                for (int pz = 0; pz < 2; pz++)
                    CreateDetailCylinder(inbox.transform, $"Pilar_Bandeja_{px}_{pz}", new Vector3(postX[px], 0.044f, postZ[pz]), new Vector3(0.009f, 0.040f, 0.009f), pal.stainlessSteel);

            CreateDetailBox(inbox.transform, "Papeis_Bandeja_Baixa", new Vector3(-0.006f, 0.016f, 0f), new Vector3(0.20f, 0.016f, 0.27f), pal.paperWhite, new Vector3(0f, 4f, 0f));
            CreateDetailBox(inbox.transform, "Pasta_Bandeja_Alta", new Vector3(-0.004f, 0.088f, 0f), new Vector3(0.21f, 0.008f, 0.28f), pal.folderKraft, new Vector3(0f, -3f, 0f));
            CreateDetailBox(inbox.transform, "Papeis_Bandeja_Alta", new Vector3(-0.008f, 0.097f, 0.005f), new Vector3(0.195f, 0.010f, 0.26f), pal.paperWhite, new Vector3(0f, -6f, 0f));
            SnapToSupportTop(inbox, deskTop);

            GameObject deskBooks = BuildStackedBooks(desk, "Livros_Contabeis_Mesa", new Vector3(0.30f, 0.780f, 0.18f), -82f, pal, count: 2);
            SnapToSupportTop(deskBooks, deskTop);
            GameObject deskStapler = BuildDeskStapler(desk, "Grampeador_Escritorio", new Vector3(0.12f, 0.780f, -0.39f), -28f, pal);
            SnapToSupportTop(deskStapler, deskTop);
            GameObject deskPad = BuildDeskNotepad(desk, "Bloco_Notas_Escritorio", new Vector3(-0.06f, 0.780f, -0.45f), 14f, pal);
            SnapToSupportTop(deskPad, deskTop);
            GameObject deskMug = BuildDetailedCoffeeMug(desk, "Caneca_Cafe_Escritorio", new Vector3(-0.25f, 0.780f, -0.56f), 24f, pal, withSaucer: true);
            SnapToSupportTop(deskMug, deskTop);
        }

        private static void BuildOfficeDeskDrawerPull(Transform drawerFrontParent, GreyboxPalette pal)
        {
            BuildOpenFrameTrimBarsZ(drawerFrontParent, "Moldura_Gaveta_Mesa", new Vector3(-0.257f, 0f, 0f), 0.36f, 0.094f, 0.018f, 0.005f, pal.woodMahogany);
            CreateDetailBox(drawerFrontParent, "Espelho_Puxador_Gaveta", new Vector3(-0.257f, 0f, 0f), new Vector3(0.005f, 0.026f, 0.148f), pal.metalBrass);
            Mesh pullMesh = CreateFacetedCShapeHandleMesh("Mesh_Puxador_Gaveta_Esc", new[]
            {
                new Vector3(-0.258f, 0f, -0.056f),
                new Vector3(-0.282f, 0f, -0.048f),
                new Vector3(-0.286f, 0f,  0.000f),
                new Vector3(-0.282f, 0f,  0.048f),
                new Vector3(-0.258f, 0f,  0.056f)
            }, halfThicknessY: 0.006f, halfThicknessZ: 0.006f, sideAxis: Vector3.up);
            CreateMeshObject(drawerFrontParent, "Puxador_Gaveta", pullMesh, pal.metalBrass, Vector3.zero, castShadows: true);
        }

        private static void BuildOfficeChairDetails(Transform root, GreyboxPalette pal)
        {
            GameObject chair = CreateContainer(root, "Cadeira_Escritorio");
            chair.transform.localPosition = new Vector3(4.72f, 0.010f, -2.55f);
            chair.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);

            // Assento e encosto estofados chanfrados + concha rígida traseira + mecanismo de alavanca
            Mesh seatMesh = CreateChamferedRectLoftMesh("Mesh_Assento_Cadeira_Esc", 0.48f, 0.062f, 0.48f, 0.036f, 0.90f, 0.012f);
            CreateMeshObject(chair.transform, "Assento", seatMesh, pal.sofaFabric, new Vector3(0f, 0.420f, 0f), castShadows: true);
            CreateDetailBox(chair.transform, "Mecanismo_Assento", new Vector3(0f, 0.402f, 0f), new Vector3(0.22f, 0.028f, 0.22f), pal.metalDark);
            CreateDetailBox(chair.transform, "Alavanca_Pistao", new Vector3(0.06f, 0.402f, -0.17f), new Vector3(0.016f, 0.010f, 0.14f), pal.metalDark, new Vector3(0f, -18f, 0f));
            CreateDetailBox(chair.transform, "Pega_Alavanca_Pistao", new Vector3(0.08f, 0.402f, -0.235f), new Vector3(0.024f, 0.016f, 0.040f), pal.shadowBlack, new Vector3(0f, -18f, 0f));

            Mesh backMesh = CreateChamferedRectLoftMesh("Mesh_Encosto_Cadeira_Esc", 0.060f, 0.52f, 0.46f, 0.020f, 0.92f, 0.014f);
            CreateMeshObject(chair.transform, "Encosto", backMesh, pal.sofaFabric, new Vector3(-0.23f, 0.51f, 0f), castShadows: true);
            CreateDetailBox(chair.transform, "Concha_Tras_Encosto", new Vector3(-0.264f, 0.77f, 0f), new Vector3(0.016f, 0.47f, 0.42f), pal.metalDark);
            CreateDetailBox(chair.transform, "Coluna_Encosto_V", new Vector3(-0.278f, 0.59f, 0f), new Vector3(0.020f, 0.36f, 0.056f), pal.metalDark);
            CreateDetailBox(chair.transform, "Coluna_Encosto_H", new Vector3(-0.135f, 0.415f, 0f), new Vector3(0.280f, 0.022f, 0.065f), pal.metalDark);
            CreateDetailCylinder(chair.transform, "Manopla_Encosto", new Vector3(-0.294f, 0.59f, 0f), new Vector3(0.036f, 0.012f, 0.036f), pal.shadowBlack, new Vector3(0f, 0f, 90f));

            CreateCylinder(chair.transform, "Cubo_Estrela", new Vector3(0f, 0.065f, 0f), new Vector3(0.088f, 0.025f, 0.088f), pal.metalDark);
            CreateDetailCylinder(chair.transform, "Capa_Pistao_Inf", new Vector3(0f, 0.165f, 0f), new Vector3(0.072f, 0.085f, 0.072f), pal.shadowBlack);
            CreateCylinder(chair.transform, "Pistao", new Vector3(0f, 0.255f, 0f), new Vector3(0.056f, 0.155f, 0.056f), pal.metalDark);

            Mesh armPadMesh = CreateChamferedRectLoftMesh("Mesh_Apoio_Braco_Esc", 0.30f, 0.030f, 0.054f, 0.012f, 0.90f, 0.008f);
            CreateDetailBox(chair.transform, "Barra_Bracos_Base", new Vector3(0f, 0.398f, 0f), new Vector3(0.036f, 0.022f, 0.54f), pal.metalDark);
            CreateMeshObject(chair.transform, "Apoio_Braco_Sul", armPadMesh, pal.metalDark, new Vector3(0f, 0.595f, -0.26f), castShadows: true);
            CreateMeshObject(chair.transform, "Apoio_Braco_Norte", armPadMesh, pal.metalDark, new Vector3(0f, 0.595f, 0.26f), castShadows: true);
            CreateDetailBox(chair.transform, "Suporte_Braco_Sul", new Vector3(0f, 0.498f, -0.26f), new Vector3(0.025f, 0.195f, 0.025f), pal.metalDark);
            CreateDetailBox(chair.transform, "Suporte_Braco_Norte", new Vector3(0f, 0.498f,  0.26f), new Vector3(0.025f, 0.195f, 0.025f), pal.metalDark);

            // Estrela de 5 pontas com garfo e rodinhas verticais duplas aterradas exatamente em Y = 0.000f local (0.010f mundo)
            for (int arm = 0; arm < 5; arm++)
            {
                GameObject star = CreateContainer(chair.transform, $"Pata_Estrela_{arm}");
                star.transform.localRotation = Quaternion.Euler(0f, arm * 72f, 0f);
                CreateDetailBox(star.transform, "Braco_Pata", new Vector3(0f, 0.062f, 0.162f), new Vector3(0.042f, 0.028f, 0.258f), pal.metalDark);
                CreateDetailCylinder(star.transform, "Pino_Rodizio", new Vector3(0f, 0.046f, 0.290f), new Vector3(0.014f, 0.016f, 0.014f), pal.stainlessSteel);
                CreateDetailBox(star.transform, "Garfo_Rodizio", new Vector3(0f, 0.032f, 0.290f), new Vector3(0.034f, 0.022f, 0.036f), pal.metalDark);
                CreateDetailCylinder(star.transform, "Rodizio_Esq", new Vector3(-0.014f, 0.021f, 0.290f), new Vector3(0.042f, 0.006f, 0.042f), pal.shadowBlack, new Vector3(0f, 0f, 90f));
                CreateDetailCylinder(star.transform, "Rodizio_Dir", new Vector3( 0.014f, 0.021f, 0.290f), new Vector3(0.042f, 0.006f, 0.042f), pal.shadowBlack, new Vector3(0f, 0f, 90f));
            }
        }

        private static void BuildOfficeBookshelfDetails(Transform shelf, float[] binderBaseYs, GreyboxPalette pal)
        {
            // Friso superior de coroa e cantoneiras de livro + caixas arquivo reais com tampa sobreposta
            CreateDetailBox(shelf, "Friso_Topo_Estante_Esc", new Vector3(0f, 1.936f, 0.006f), new Vector3(1.76f, 0.016f, 0.37f), pal.baseboardWood);
            for (int r = 0; r < binderBaseYs.Length; r++)
            {
                float bracketX = (r == 2) ? 0.725f : 0.781f;
                CreateDetailBox(shelf, $"Apoio_Livros_V_{r}", new Vector3(bracketX, binderBaseYs[r] + 0.075f, 0f), new Vector3(0.006f, 0.150f, 0.14f), pal.metalDark);
                CreateDetailBox(shelf, $"Apoio_Livros_B_{r}", new Vector3(bracketX + 0.021f, binderBaseYs[r] + 0.003f, 0f), new Vector3(0.036f, 0.006f, 0.14f), pal.metalDark);
            }

            float[] boxXs = { -0.44f, 0.06f };
            float[] boxRots = { 0f, 3f };
            for (int b = 0; b < 2; b++)
            {
                GameObject boxGrp = CreateContainer(shelf, $"Caixa_Arquivo_{b + 1}");
                boxGrp.transform.localPosition = new Vector3(boxXs[b], 1.513f, 0.02f);
                boxGrp.transform.localRotation = Quaternion.Euler(0f, boxRots[b], 0f);
                CreateDetailBox(boxGrp.transform, "Corpo_Caixa_Arquivo", new Vector3(0f, 0.115f, 0f), new Vector3(0.35f, 0.230f, 0.26f), pal.folderKraft, castShadows: true);
                CreateDetailBox(boxGrp.transform, "Tampa_Caixa_Arquivo", new Vector3(0f, 0.236f, 0f), new Vector3(0.37f, 0.036f, 0.28f), pal.folderKraft, castShadows: true);
                CreateDetailBox(boxGrp.transform, $"Etiqueta_Caixa_{b + 1}", new Vector3(0f, 0.145f, 0.1325f), new Vector3(0.15f, 0.068f, 0.004f), pal.paperWhite);
                CreateDetailBox(boxGrp.transform, $"Recorte_Mao_Caixa_{b + 1}", new Vector3(0f, 0.080f, 0.1320f), new Vector3(0.092f, 0.024f, 0.004f), pal.shadowBlack);
            }
        }

        private static void BuildOfficeFilingCabinet(Transform root, GreyboxPalette pal)
        {
            GameObject cabinet = CreateContainer(root, "Arquivo_Metalico");
            cabinet.transform.localPosition = new Vector3(6.08f, 0f, -4.65f);

            // Quatro pés niveladores apoiados em Y = 0.000f levantando a base de aço para visual real
            float[] fx = { -0.20f, 0.20f };
            float[] fz = { -0.24f, 0.24f };
            for (int ix = 0; ix < 2; ix++)
                for (int iz = 0; iz < 2; iz++)
                    CreateDetailBox(cabinet.transform, $"Pe_Arquivo_{ix * 2 + iz}", new Vector3(fx[ix], 0.008f, fz[iz]), new Vector3(0.044f, 0.016f, 0.044f), pal.metalDark);

            CreateBox(cabinet.transform, "Base_Arquivo", new Vector3(0f, 0.048f, 0f), new Vector3(0.48f, 0.064f, 0.58f), pal.metalDark);
            CreateBox(cabinet.transform, "Topo_Arquivo", new Vector3(0f, 1.30f, 0f), new Vector3(0.48f, 0.04f, 0.58f), pal.metalDark);
            CreateBox(cabinet.transform, "Fundo_Arquivo", new Vector3(0.22f, 0.68f, 0f), new Vector3(0.04f, 1.20f, 0.58f), pal.metalDark);
            CreateBox(cabinet.transform, "Lat_Sul_Arquivo", new Vector3(-0.02f, 0.68f, -0.27f), new Vector3(0.44f, 1.20f, 0.04f), pal.metalDark);
            CreateBox(cabinet.transform, "Lat_Norte_Arquivo", new Vector3(-0.02f, 0.68f, 0.27f), new Vector3(0.44f, 1.20f, 0.04f), pal.metalDark);
            CreateDetailBox(cabinet.transform, "Sombra_Interna_Arquivo", new Vector3(0.195f, 0.68f, 0f), new Vector3(0.010f, 1.18f, 0.49f), pal.shadowBlack);

            // Gaveta baixa fixa com porta-etiqueta emoldurado e puxador metálico em C (aço pintado chapado natural, sem azulejo)
            CreateBox(cabinet.transform, "Gaveta_Baixa_Fixa", new Vector3(-0.03f, 0.276f, 0f), new Vector3(0.42f, 0.392f, 0.50f), pal.officeCabinetMetal);
            BuildOfficeFileCabinetFrontDecor(cabinet.transform, "Baixa", -0.2400f, 0.276f, pal);

            // Gaveta do meio com trilhos suspensos para pastas e pastas suspensas com ganchos
            GameObject midDrawer = CreateContainer(cabinet.transform, "Gaveta_Media_Arquivo");
            midDrawer.transform.localPosition = new Vector3(-0.01f, 0.680f, 0f);
            CreateBox(midDrawer.transform, "Fundo_Gaveta", new Vector3(-0.008f, -0.14f, 0f), new Vector3(0.400f, 0.02f, 0.456f), pal.metalDark, hasCollider: true);
            CreateDetailBox(midDrawer.transform, "Lateral_Sul_Gav", new Vector3(-0.004f, -0.04f, -0.234f), new Vector3(0.420f, 0.22f, 0.012f), pal.metalDark);
            CreateDetailBox(midDrawer.transform, "Lateral_Norte_Gav", new Vector3(-0.004f, -0.04f, 0.234f), new Vector3(0.420f, 0.22f, 0.012f), pal.metalDark);
            CreateDetailBox(midDrawer.transform, "Traseira_Gav", new Vector3(0.198f, -0.04f, 0f), new Vector3(0.012f, 0.22f, 0.456f), pal.metalDark);
            CreateBox(midDrawer.transform, "Frente_Gaveta", new Vector3(-0.23f, 0f, 0f), new Vector3(0.025f, 0.392f, 0.50f), pal.officeCabinetMetal, hasCollider: true);
            BuildOfficeFileCabinetFrontDecor(midDrawer.transform, "Media", -0.2425f, 0f, pal);

            CreateDetailCylinder(midDrawer.transform, "Trilho_Pasta_Sul", new Vector3(0f, 0.068f, -0.220f), new Vector3(0.008f, 0.205f, 0.008f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            CreateDetailCylinder(midDrawer.transform, "Trilho_Pasta_Norte", new Vector3(0f, 0.068f, 0.220f), new Vector3(0.008f, 0.205f, 0.008f), pal.stainlessSteel, new Vector3(0f, 0f, 90f));
            for (int fd = 0; fd < 6; fd++)
            {
                float fxPos = -0.15f + fd * 0.06f;
                CreateDetailBox(midDrawer.transform, $"Pasta_Suspensa_{fd}", new Vector3(fxPos, -0.03f, 0f), new Vector3(0.012f, 0.20f, 0.40f), pal.folderKraft, new Vector3(0f, 0f, fd % 2 == 0 ? 2f : -2f));
                CreateDetailBox(midDrawer.transform, $"Haste_Pasta_{fd}", new Vector3(fxPos, 0.072f, 0f), new Vector3(0.006f, 0.006f, 0.450f), pal.stainlessSteel);
                CreateDetailBox(midDrawer.transform, $"Aba_Pasta_{fd}", new Vector3(fxPos, 0.082f, 0f), new Vector3(0.014f, 0.016f, 0.10f), pal.paperWhite);
            }

            InspectableObject midDrawerInsp = MakeSpot(midDrawer.transform, "Pastas_Imposto_Arquivo", new Vector3(-0.06f, 0.02f, 0f), new Vector3(0.30f, 0.18f, 0.42f));
            midDrawerInsp.ConfigureTask(
                id: "arquivo_gaveta_media_opc",
                prompt: "Folhear as pastas",
                subtitle: "Papéis de 92 e 93. Tá tudo em ordem aqui.",
                repeatSubtitle: "Só faltam as coisas de 94.",
                linear: false,
                sound: FoleySoundId.PaperRustle
            );

            AnimatedContainerInteractable midDrawerAnim = midDrawer.AddComponent<AnimatedContainerInteractable>();
            midDrawerAnim.ConfigureSlideDrawer(
                id: "arquivo_gaveta_media",
                drawerTransform: midDrawer.transform,
                slideOffset: new Vector3(-0.32f, 0f, 0f),
                promptOpen: "Abrir a gaveta do meio do arquivo",
                promptClose: "Fechar a gaveta do meio",
                slideSfx: FoleySoundId.FileCabinetDrawer
            );
            midDrawerAnim.RegisterInteriorObject(midDrawerInsp);
            EditorUtility.SetDirty(midDrawerAnim);

            // Gaveta alta trancada com fechadura arranhada + envelope manila + recorte de jornal de 1994
            GameObject topDrawer = CreateContainer(cabinet.transform, "Gaveta_Alta_Arquivo");
            topDrawer.transform.localPosition = new Vector3(-0.01f, 1.084f, 0f);
            GameObject topCabinetBottom = CreateBox(topDrawer.transform, "Fundo_Gaveta", new Vector3(-0.008f, -0.14f, 0f), new Vector3(0.400f, 0.02f, 0.456f), pal.metalDark, hasCollider: true);
            CreateDetailBox(topDrawer.transform, "Lateral_Sul_Gav", new Vector3(-0.004f, -0.04f, -0.234f), new Vector3(0.420f, 0.22f, 0.012f), pal.metalDark);
            CreateDetailBox(topDrawer.transform, "Lateral_Norte_Gav", new Vector3(-0.004f, -0.04f, 0.234f), new Vector3(0.420f, 0.22f, 0.012f), pal.metalDark);
            CreateDetailBox(topDrawer.transform, "Traseira_Gav", new Vector3(0.198f, -0.04f, 0f), new Vector3(0.012f, 0.22f, 0.456f), pal.metalDark);
            CreateBox(topDrawer.transform, "Frente_Gaveta", new Vector3(-0.23f, 0f, 0f), new Vector3(0.025f, 0.392f, 0.50f), pal.officeCabinetMetal, hasCollider: true);
            BuildOfficeFileCabinetFrontDecor(topDrawer.transform, "Alta", -0.2425f, 0f, pal);

            CreateDetailBox(topDrawer.transform, "Espelho_Fechadura_Alta", new Vector3(-0.2485f, 0.16f, 0.14f), new Vector3(0.004f, 0.050f, 0.040f), pal.stainlessSteel);
            CreateDetailCylinder(topDrawer.transform, "Fechadura_Gaveta_Alta", new Vector3(-0.2535f, 0.16f, 0.14f), new Vector3(0.020f, 0.005f, 0.020f), pal.metalBrass, new Vector3(0f, 0f, 90f));
            CreateDetailBox(topDrawer.transform, "Furo_Fechadura_Alta", new Vector3(-0.2598f, 0.16f, 0.14f), new Vector3(0.003f, 0.011f, 0.003f), pal.shadowBlack);
            CreateDetailBox(topDrawer.transform, "Riscos_Fechadura_1", new Vector3(-0.2442f, 0.172f, 0.095f), new Vector3(0.0034f, 0.0024f, 0.026f), pal.stainlessSteel, new Vector3(25f, 0f, 0f));
            CreateDetailBox(topDrawer.transform, "Riscos_Fechadura_2", new Vector3(-0.2490f, 0.146f, 0.105f), new Vector3(0.0030f, 0.0024f, 0.026f), pal.stainlessSteel, new Vector3(-30f, 0f, 0f));

            GameObject manilaEnv = CreateDetailBox(topDrawer.transform, "Envelope_Recorte_Jornal", new Vector3(-0.06f, -0.128f, -0.02f), new Vector3(0.30f, 0.004f, 0.24f), pal.folderKraft, new Vector3(0f, 4f, 0f));
            SnapToSupportTop(manilaEnv, topCabinetBottom);

            GameObject clippingObj = CreateContainer(topDrawer.transform, "Recorte_Jornal_1994");
            clippingObj.transform.localPosition = new Vector3(-0.06f, -0.125f, 0f);
            clippingObj.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
            BuildDetailedPaperSheet(clippingObj.transform, "Folha_Jornal_Mesh", Vector3.zero, 0f, new Vector2(0.28f, 0.22f), pal, isNewspaper: true, printedFaceMat: pal.details.newspaper1994, printAngleY: 90f);
            SnapToSupportTop(clippingObj, manilaEnv);
            BoxCollider clipCol = clippingObj.AddComponent<BoxCollider>();
            clipCol.center = new Vector3(0f, 0.04f, 0f);
            clipCol.size = new Vector3(0.30f, 0.12f, 0.36f);

            InspectableObject cabinetDoc = clippingObj.AddComponent<InspectableObject>();
            cabinetDoc.ConfigureDocument(
                id: "arquivo_metalico_opc",
                prompt: "Ler o recorte de jornal guardado",
                docTitle: "JORNAL DA BAIXADA — Domingo, 11 de setembro de 1994",
                docBody:
                    "MÃE E DUAS FILHAS SÃO ENCONTRADAS MORTAS NA RUA DAS ACÁCIAS\n\n" +
                    "A Polícia Civil investiga a morte de M. N., de 32 anos, e das duas\n" +
                    "filhas, de 6 e 8 anos, na noite de sábado (10), em uma casa no fim\n" +
                    "da Rua das Acácias.\n\n" +
                    "Vizinhos chamaram a PM por volta das 22h30 depois de ouvirem gritos\n" +
                    "e barulho vindo de dentro. Quando os policiais arrombaram a porta do\n" +
                    "quarto dos fundos, que estava trancada por fora, as três já estavam\n" +
                    "mortas. A polícia ainda não informou a causa.\n\n" +
                    "O marido, contador de 34 anos, estava sentado na sala com a TV alta.\n" +
                    "Ele foi levado na madrugada para...\n\n" +
                    "//////////////////////////////",
                afterReadThought: "O final foi arrancado.",
                repeatThought: "...",
                linear: false,
                hudLabel: "",
                style: DocumentStyle.NewspaperClipping,
                extraSound: FoleySoundId.PaperRustle
            );

            AnimatedContainerInteractable topDrawerAnim = topDrawer.AddComponent<AnimatedContainerInteractable>();
            topDrawerAnim.ConfigureSlideDrawer(
                id: "arquivo_gaveta_alta",
                drawerTransform: topDrawer.transform,
                slideOffset: new Vector3(-0.35f, 0f, 0f),
                promptOpen: "Abrir a gaveta alta do arquivo",
                promptClose: "Fechar a gaveta alta do arquivo",
                slideSfx: FoleySoundId.FileCabinetDrawer
            );
            topDrawerAnim.ConfigureKeyLock(
                neededKeyId: "chave_arquivo_pessoal",
                promptWhenLocked: "Tentar abrir a gaveta alta do arquivo",
                promptWhenHasKey: "Destrancar e abrir a gaveta alta",
                lockedLine: "Não abre. A fechadura tá toda riscada.",
                unlockedLine: "A chave do bolso dela serviu."
            );
            topDrawerAnim.RegisterInteriorObject(cabinetDoc);
            EditorUtility.SetDirty(topDrawerAnim);

            // Tampo do arquivo metálico: pastas de balancete + carimbo de madeira e almofada de tinta
            CreateDetailBox(cabinet.transform, "Pasta_Topo_Arquivo_1", new Vector3(-0.02f, 1.329f, 0.08f), new Vector3(0.32f, 0.018f, 0.24f), pal.folderKraft, new Vector3(0f, -6f, 0f));
            CreateDetailBox(cabinet.transform, "Pasta_Topo_Arquivo_2", new Vector3(-0.01f, 1.345f, 0.08f), new Vector3(0.30f, 0.014f, 0.22f), pal.plasticSlate, new Vector3(0f, 4f, 0f));
            CreateDetailBox(cabinet.transform, "Almofada_Carimbo", new Vector3(-0.06f, 1.330f, -0.14f), new Vector3(0.085f, 0.020f, 0.115f), pal.metalDark, new Vector3(0f, 12f, 0f));
            CreateDetailBox(cabinet.transform, "Base_Carimbo_Esc", new Vector3(-0.05f, 1.326f, -0.02f), new Vector3(0.062f, 0.012f, 0.036f), pal.furnitureWood, new Vector3(0f, -18f, 0f));
            CreateDetailCylinder(cabinet.transform, "Cabo_Carimbo_Esc", new Vector3(-0.05f, 1.356f, -0.02f), new Vector3(0.024f, 0.024f, 0.024f), pal.woodMahogany);
        }

        private static void BuildOfficeFileCabinetFrontDecor(
            Transform parent, string tag, float frontX, float baseY, GreyboxPalette pal)
        {
            BuildOpenFrameTrimBarsZ(parent, $"Moldura_Gav_Arq_{tag}", new Vector3(frontX - 0.002f, baseY, 0f), 0.44f, 0.31f, 0.016f, 0.006f, pal.metalDark);
            CreateDetailBox(parent, $"Etiqueta_Papel_{tag}", new Vector3(frontX - 0.0015f, baseY + 0.08f, 0f), new Vector3(0.003f, 0.036f, 0.148f), pal.paperWhite);
            BuildOpenFrameTrimBarsZ(parent, $"Porta_Etiqueta_{tag}", new Vector3(frontX - 0.0025f, baseY + 0.08f, 0f), 0.17f, 0.052f, 0.008f, 0.007f, pal.stainlessSteel);
            CreateDetailBox(parent, $"Espelho_Puxador_Arq_{tag}", new Vector3(frontX - 0.002f, baseY - 0.04f, 0f), new Vector3(0.006f, 0.032f, 0.18f), pal.stainlessSteel);

            Mesh pullMesh = CreateFacetedCShapeHandleMesh($"Mesh_Puxador_Arq_{tag}", new[]
            {
                new Vector3(frontX - 0.004f, baseY - 0.04f, -0.068f),
                new Vector3(frontX - 0.028f, baseY - 0.04f, -0.058f),
                new Vector3(frontX - 0.032f, baseY - 0.04f,  0.000f),
                new Vector3(frontX - 0.028f, baseY - 0.04f,  0.058f),
                new Vector3(frontX - 0.004f, baseY - 0.04f,  0.068f)
            }, halfThicknessY: 0.006f, halfThicknessZ: 0.006f, sideAxis: Vector3.up);
            CreateMeshObject(parent, $"Puxador_{tag}", pullMesh, pal.metalBrass, Vector3.zero, castShadows: true);
        }
    }
}
#endif
