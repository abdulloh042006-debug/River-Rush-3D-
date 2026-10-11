#if UNITY_EDITOR
using RiverRush.Gameplay;
using RiverRush.Graphics;
using RiverRush.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiverRush.Editor
{
    /// <summary>Creates a runnable URP prototype using geometry and materials, no imported models.</summary>
    public static class RiverRushSceneBuilder
    {
        [MenuItem("River Rush/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var world = new GameObject("Procedural World");
            world.AddComponent<MeshFilter>();
            var terrainRenderer = world.AddComponent<MeshRenderer>();
            world.AddComponent<MeshCollider>();
            var terrain = world.AddComponent<ProceduralTerrain>();
            terrainRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/Terrain.mat", "Universal Render Pipeline/Lit",
                new Color(0.26f, 0.52f, 0.29f));
            terrain.Generate();
            var route = world.AddComponent<RiverRoute>();
            route.Rebuild();
            world.AddComponent<RiverFlowField>();
            world.AddComponent<WaterWaveSimulator>();

            var river = new GameObject("Animated River Surface");
            river.AddComponent<MeshFilter>();
            var riverRenderer = river.AddComponent<MeshRenderer>();
            riverRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/StylizedRiver.mat", "RiverRush/StylizedRiverURP",
                new Color(0.07f, 0.68f, 0.78f));
            river.AddComponent<RiverWaterSurface>().Rebuild();

            // Large low-cost opaque ocean beginning at the river mouth (positive Z).
            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Ocean";
            float size = terrain.WorldSize;
            ocean.transform.position = world.transform.TransformPoint(
                new Vector3(0f, terrain.SeaLevel + 0.04f, size * 1.2f));
            ocean.transform.localScale = new Vector3(size * 0.2f, 1f, size * 0.14f);
            ocean.GetComponent<MeshRenderer>().sharedMaterial = EnsureMaterial(
                "Assets/Materials/Ocean.mat", "RiverRush/StylizedOceanURP",
                new Color(0.015f, 0.22f, 0.39f));
            Object.DestroyImmediate(ocean.GetComponent<MeshCollider>());

            var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player - River Droplet";
            player.transform.localScale = Vector3.one * 1.7f;
            if (route.Points != null && route.Points.Count > 0)
                player.transform.position = route.transform.TransformPoint(route.Points[0]);
            var playerRenderer = player.GetComponent<MeshRenderer>();
            playerRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/Player.mat", "Universal Render Pipeline/Lit",
                new Color(0.045f, 0.78f, 0.94f));

            var body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            player.AddComponent<RiverRunner>();
            player.AddComponent<RiverGrowth>();
            player.AddComponent<RiverFinish>();

            // Cheap persistent streak replaces GPU-heavy particle emitters on mobile.
            var trail = player.AddComponent<TrailRenderer>();
            trail.time = 0.75f;
            trail.minVertexDistance = 0.35f;
            trail.startWidth = 2f;
            trail.endWidth = 0.05f;
            trail.sharedMaterial = EnsureMaterial("Assets/Materials/Wake.mat",
                "Universal Render Pipeline/Unlit", new Color(0.57f, 0.95f, 0.99f));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            int[] pickupIndexes = { 24, 65, 120, 177, 226 };
            for (int p = 0; p < pickupIndexes.Length; p++)
            {
                if (route.Points == null || pickupIndexes[p] >= route.Points.Count) continue;
                var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pickup.name = "Tributary Pickup " + (p + 1);
                pickup.transform.position =
                    route.transform.TransformPoint(route.Points[pickupIndexes[p]]) + Vector3.up * 0.4f;
                pickup.transform.localScale = Vector3.one * 2.6f;
                pickup.GetComponent<SphereCollider>().isTrigger = true;
                pickup.GetComponent<MeshRenderer>().sharedMaterial = playerRenderer.sharedMaterial;
                pickup.AddComponent<TributaryPickup>();
            }

            // Free-floating object demonstrates water drag/buoyancy without full fluid simulation.
            var floater = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floater.name = "Floating Crate (Water Physics Test)";
            if (route.SampleBank(route.TotalLength * 0.55f, 1.4f, out Vector3 floatSpot))
                floater.transform.position = floatSpot + Vector3.up * 0.5f;
            floater.transform.localScale = Vector3.one * 1.5f;
            var floaterBody = floater.AddComponent<Rigidbody>();
            floaterBody.mass = 2f;
            floater.AddComponent<WaterFloatBody>();

            world.AddComponent<RiverRaceBootstrap>();

            var cameraObject = new GameObject("River Follow Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 65f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<RiverFollowCamera>();

            var lightObject = new GameObject("Sun");
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;

            var settings = new GameObject("Performance & FPS");
            settings.AddComponent<AdaptiveQualityController>();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
                "Assets/Scenes/RiverRushPrototype.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Created river prototype with physics-based surface waves, floating crate, ocean and adaptive FPS.");
        }

        private static Material EnsureMaterial(string path, string shaderName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("Missing shader: " + shaderName + ". Use a Unity 6 Universal 3D (URP) project.");
                return null;
            }
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_ShallowColor")) material.SetColor("_ShallowColor", color);
            if (material.HasProperty("_OceanColor")) material.SetColor("_OceanColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
