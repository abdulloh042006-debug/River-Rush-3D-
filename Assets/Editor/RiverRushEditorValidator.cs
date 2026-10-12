#if UNITY_EDITOR
using RiverRush.Gameplay;
using RiverRush.Graphics;
using RiverRush.Audio;
using RiverRush.Diagnostics;
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
            RiverRunner runner = null;
            var allRunners = Object.FindObjectsByType<RiverRunner>(FindObjectsSortMode.None);
            foreach (var candidate in allRunners)
                if (candidate.IsHumanControlled) { runner = candidate; break; }
            var bootstrap = Object.FindFirstObjectByType<RiverRaceBootstrap>();
            var waves = Object.FindFirstObjectByType<WaterWaveSimulator>();
            var oceanPhysics = Object.FindFirstObjectByType<OceanWaterPhysics>();
            var tributaries = Object.FindFirstObjectByType<RiverTributaryNetwork>();
            var nature = Object.FindFirstObjectByType<ProceduralNature>();
            var waterColors = Object.FindFirstObjectByType<RiverColorSystem>();
            var natureSound = Object.FindFirstObjectByType<RiverNatureAudio>();
            var race = Object.FindFirstObjectByType<RiverRaceManager>();
            var benchmark = Object.FindFirstObjectByType<RiverPerformanceBenchmark>();
            var bots = Object.FindObjectsByType<RiverBotAI>(FindObjectsSortMode.None);
            int failures = 0;
            if (terrain == null || route == null || flow == null || water == null
                || quality == null || runner == null || bootstrap == null || waves == null ||
                oceanPhysics == null || race == null || bots.Length < 3 ||
                waterColors == null || natureSound == null ||
                tributaries == null || nature == null || benchmark == null)
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

            // Verify branch network geometry is seeded, monotonic and connects
            // precisely to the main river; all confluences yield a pickup.
            var layout = terrain.Branches;
            if (layout == null || layout.Branches.Length < 3 ||
                !layout.Verify(out string tributaryError))
                failures++;
            else
            {
                foreach (var branch in layout.Branches)
                {
                    Vector2 endpoint = branch.Points[branch.Points.Length - 1];
                    if (Mathf.Abs(endpoint.x - terrain.RiverCenter(branch.JunctionV)) > 0.1f ||
                        Mathf.Abs(endpoint.y - (branch.JunctionV - 0.5f) * terrain.WorldSize) > 0.1f)
                        failures++;
                    float heightBefore = float.PositiveInfinity;
                    foreach (var p in branch.Points)
                    {
                        float channelHeight = terrain.SampleHeight(p.x, p.y);
                        if (channelHeight > heightBefore + 0.01f) failures++;
                        heightBefore = channelHeight;
                    }
                }
            }

            tributaries.Rebuild();
            nature.Rebuild();

            // Repeat generation to catch stale serialized children and duplicated
            // scene roots that otherwise double GPU memory on 8 GB laptops.
            tributaries.Rebuild();
            nature.Rebuild();
            int natureRootCount = 0, tributaryRootCount = 0;
            for (int childIndex = 0; childIndex < terrain.transform.childCount; childIndex++)
            {
                string childName = terrain.transform.GetChild(childIndex).name;
                if (childName.StartsWith("Generated Forests, Rocks and Biomes",
                    System.StringComparison.Ordinal)) natureRootCount++;
                if (childName.StartsWith("Generated Tributaries - Seed ",
                    System.StringComparison.Ordinal)) tributaryRootCount++;
            }
            if (natureRootCount != 1 || tributaryRootCount != 1) failures++;
            if (tributaries.GeneratedWaterMeshCount != tributaries.BranchCount ||
                tributaries.GeneratedPickupCount != tributaries.BranchCount ||
                nature.MeshCount < 3 ||
                nature.TreesCreated < 25 ||
                nature.RocksCreated < 10 ||
                nature.ShrubsCreated < 10) failures++;
            var terrainRenderer = terrain.GetComponent<MeshRenderer>();
            if (terrainRenderer == null || terrainRenderer.sharedMaterial == null ||
                terrainRenderer.sharedMaterial.shader == null ||
                terrainRenderer.sharedMaterial.shader.name != "RiverRush/TerrainBiomeURP")
                failures++;

            // Pass 2: physically directed flow, positive speed, slower near bank.
            waves.Initialize();
            waves.AddImpulse(route.TotalLength * 0.5f, 1.2f, 4f);
            if (waves.Count != points.Count) failures++;
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
            if (ocean == null || ocean.GetComponent<OceanWaterPhysics>() == null ||
                ocean.GetComponent<MeshRenderer>() == null ||
                ocean.GetComponent<MeshRenderer>().sharedMaterial == null) failures++;

            // Pass 4: performance configuration and river physics component presence.
            if (Object.FindFirstObjectByType<WaterFloatBody>() == null ||
                allRunners.Length != bots.Length + 1) failures++;
            if (quality.EffectiveQualityLevel < 0 || quality.EffectiveQualityLevel > 3)
                failures++;

            // Audio and palette configuration must be present even if the new
            // laptop has no audio device connected at the time of validation.
            if (waterColors.CrowdThreshold < 2 || waterColors.ConnectedHumanCount < 1)
                failures++;
            // Palette materials are generated in Play Mode. Avoid assigning
            // transient runtime materials while validating an editor scene.
            var dropletRenderer = runner.GetComponent<Renderer>();
            if (dropletRenderer == null || dropletRenderer.sharedMaterial == null ||
                dropletRenderer.sharedMaterial.shader == null ||
                dropletRenderer.sharedMaterial.shader.name != "RiverRush/WaterDropletURP")
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
