#if UNITY_EDITOR
using RiverRush.Gameplay;
using RiverRush.Graphics;
using RiverRush.World;
using UnityEditor;
using UnityEngine;

namespace RiverRush.Editor
{
    /// <summary>Repeatable Unity-side integration checks; run before publishing.</summary>
    public static class RiverRushEditorValidator
    {
        [MenuItem("River Rush/Validate Prototype Geometry")]
        public static void Validate()
        {
            var terrain = Object.FindFirstObjectByType<ProceduralTerrain>();
            var route = Object.FindFirstObjectByType<RiverRoute>();
            var flow = Object.FindFirstObjectByType<RiverFlowField>();
            var water = Object.FindFirstObjectByType<RiverWaterSurface>();
            var quality = Object.FindFirstObjectByType<AdaptiveQualityController>();
            var runner = Object.FindFirstObjectByType<RiverRunner>();
            var bootstrap = Object.FindFirstObjectByType<RiverRaceBootstrap>();
            int failures = 0;
            if (terrain == null || route == null || flow == null || water == null
                || quality == null || runner == null || bootstrap == null)
            {
                Debug.LogError("Missing components. Use River Rush/Create Prototype Scene first.");
                return;
            }

            // Pass 1: deterministic heights and strictly decreasing channel centerline.
            terrain.Generate();
            route.Rebuild();
            var points = route.Points;
            if (points == null || points.Count < 2) { Debug.LogError("Empty route."); return; }
            float previous = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 world = route.transform.TransformPoint(points[i]);
                float height = world.y;
                if (!IsFinite(height) || height > previous + 0.005f) failures++;
                previous = height;
            }
            if (route.TotalLength < terrain.WorldSize * 0.9f) failures++;
            float[] before = new float[8];
            for (int i = 0; i < 8; i++)
            {
                float t = (i + 1f) / 10f;
                before[i] = terrain.SampleHeight(terrain.RiverCenter(t),
                    (t - 0.5f) * terrain.WorldSize);
            }
            terrain.Generate();
            for (int i = 0; i < 8; i++)
            {
                float t = (i + 1f) / 10f;
                float after = terrain.SampleHeight(terrain.RiverCenter(t),
                    (t - 0.5f) * terrain.WorldSize);
                if (Mathf.Abs(before[i] - after) > 0.00001f) failures++;
            }

            // Pass 2: physically directed flow, positive speed, slower near bank.
            float sampleDistance = route.TotalLength * 0.40f;
            bool core = flow.TrySample(sampleDistance, 0f, out Vector3 flowVelocity,
                out _, out float coreSpeed);
            bool shore = flow.TrySample(sampleDistance, route.Width * 0.47f,
                out _, out _, out float shoreSpeed);
            if (!core || !shore || coreSpeed <= 0f || shoreSpeed > coreSpeed ||
                !IsFinite(flowVelocity.x) || !IsFinite(flowVelocity.y) ||
                !IsFinite(flowVelocity.z)) failures++;

            // Pass 3: water mesh and URP shader.
            water.Rebuild();
            Mesh mesh = water.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null) failures++;
            else if (mesh.vertexCount != points.Count * 2 ||
                     mesh.triangles.Length != (points.Count - 1) * 6) failures++;
            var renderer = water.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null ||
                renderer.sharedMaterial.shader == null ||
                renderer.sharedMaterial.shader.name != "RiverRush/StylizedRiverURP" ||
                !renderer.sharedMaterial.shader.isSupported) failures++;
            var ocean = GameObject.Find("Ocean");
            if (ocean == null || ocean.GetComponent<MeshRenderer>() == null ||
                ocean.GetComponent<MeshRenderer>().sharedMaterial == null) failures++;

            // Pass 4: performance configuration and river physics component presence.
            if (Object.FindFirstObjectByType<WaterFloatBody>() == null ||
                Object.FindFirstObjectByType<RiverFinish>() == null) failures++;
            if (quality.EffectiveQualityLevel < 0 || quality.EffectiveQualityLevel > 3)
                failures++;

            // Pass 5: correct playable entity physics and terrain collider.
            var rigidbody = runner.GetComponent<Rigidbody>();
            if (rigidbody == null || !rigidbody.isKinematic ||
                terrain.GetComponent<MeshCollider>() == null) failures++;
            if (!route.SampleBank(0f, 0f, out Vector3 source) ||
                !route.SampleBank(route.TotalLength, 0f, out Vector3 mouth) ||
                source.y <= mouth.y) failures++;

            if (failures == 0)
                Debug.Log("PASS: 5 audit groups (terrain, flow, rendering, quality, integration). Still run on real devices.");
            else
                Debug.LogError("FAIL: " + failures + " prototype validation assertions. Fix before build.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
#endif
