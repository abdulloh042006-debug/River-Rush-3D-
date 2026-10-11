#if UNITY_EDITOR
using RiverRush.World;
using UnityEditor;
using UnityEngine;

namespace RiverRush.Editor
{
    /// <summary>Editor-only geometry checks. Does not replace a real Android/Windows build.</summary>
    public static class RiverRushEditorValidator
    {
        [MenuItem("River Rush/Validate Prototype Geometry")]
        public static void Validate()
        {
            var terrain = Object.FindFirstObjectByType<ProceduralTerrain>();
            var route = Object.FindFirstObjectByType<RiverRoute>();
            var surface = Object.FindFirstObjectByType<RiverWaterSurface>();
            if (terrain == null || route == null || surface == null)
            {
                Debug.LogError("Missing terrain, river route, or water surface. First use River Rush/Create Prototype Scene.");
                return;
            }

            terrain.Generate();
            route.Rebuild();
            surface.Rebuild();
            var points = route.Points;
            var waterMesh = surface.GetComponent<MeshFilter>().sharedMesh;
            var terrainMesh = terrain.GetComponent<MeshFilter>().sharedMesh;

            int failures = 0;
            if (terrainMesh == null || points == null || points.Count < 2 || waterMesh == null)
            {
                Debug.LogError("Geometry generation returned empty data.");
                return;
            }

            // Every centerline point should flow downhill in terrain-local coordinates.
            float previousHeight = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 world = route.transform.TransformPoint(points[i]);
                float height = terrain.transform.InverseTransformPoint(world).y;
                if (height > previousHeight + 0.005f) failures++;
                previousHeight = height;
            }

            // Deterministic sampling: same seed and location always return the same height.
            float x = terrain.RiverCenter(0.33f);
            float z = -0.17f * terrain.WorldSize;
            if (Mathf.Abs(terrain.SampleHeight(x, z) - terrain.SampleHeight(x, z)) > 0.00001f)
                failures++;

            int expectedVertices = points.Count * 2;
            int expectedTriangles = (points.Count - 1) * 6;
            if (waterMesh.vertexCount != expectedVertices || waterMesh.triangles.Length != expectedTriangles)
                failures++;

            var renderer = surface.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null ||
                renderer.sharedMaterial.shader.name != "RiverRush/StylizedRiverURP")
                failures++;

            if (failures == 0)
                Debug.Log("River Rush geometry checks passed: downhill channel, stable samples, ribbon topology, water shader assigned.");
            else
                Debug.LogError("River Rush geometry validation failed " + failures + " check(s). Inspect the generated river before building.");
        }
    }
}
#endif
