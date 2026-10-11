#if UNITY_EDITOR
using RiverRush.Gameplay;
using RiverRush.Graphics;
using RiverRush.Audio;
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

            // Five upstream tributaries are carved into the terrain and visually
            // joined to this main river; their confluences replace old fake pickups.
            var tributaries = world.AddComponent<RiverTributaryNetwork>();
            tributaries.ConfigureMaterials(riverRenderer.sharedMaterial,
                EnsureMaterial("Assets/Materials/TributaryPickup.mat",
                    "RiverRush/WaterDropletURP", new Color(0.24f, 0.83f, 0.96f)));
            tributaries.Rebuild();

            // Grouped procedural nature meshes instead of one draw object per tree.
            var environment = world.AddComponent<ProceduralNature>();
            environment.ConfigureMaterial(EnsureMaterial(
                "Assets/Materials/Nature.mat", "RiverRush/NatureVertexColorURP",
                Color.white));
            environment.Rebuild();

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
            ocean.AddComponent<OceanWaterPhysics>();

            var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player - River Droplet";
            player.transform.localScale = Vector3.one * 1.7f;
            if (route.Points != null && route.Points.Count > 0)
                player.transform.position = route.transform.TransformPoint(route.Points[0]);
            var playerRenderer = player.GetComponent<MeshRenderer>();
            playerRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/Player.mat", "RiverRush/WaterDropletURP",
                new Color(0.045f, 0.78f, 0.94f));

            var body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            player.AddComponent<RiverRunner>();
            player.AddComponent<RiverGrowth>();
            // The race manager owns the finish screen and standings.

            // Five offline AI racers use the same flow model as the human.
            float[] startingLanes = { -4.2f, -2.7f, 1.7f, 3.0f, 4.2f };
            for (int i = 0; i < startingLanes.Length; i++)
            {
                var bot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bot.name = "River Bot " + (i + 1);
                bot.transform.localScale = Vector3.one * 1.5f;
                if (route.SampleBank(0f, startingLanes[i], out Vector3 botPosition))
                    bot.transform.position = botPosition;
                var visual = bot.GetComponent<MeshRenderer>();
                Color botColor = Color.HSVToRGB(0.505f + i * 0.018f, 0.55f, 0.94f);
                visual.sharedMaterial = EnsureMaterial(
                    "Assets/Materials/Bot_" + (i + 1) + ".mat",
                    "RiverRush/WaterDropletURP", botColor);
                var botBody = bot.AddComponent<Rigidbody>();
                botBody.isKinematic = true;
                botBody.useGravity = false;
                var botRunner = bot.AddComponent<RiverRunner>();
                botRunner.ConfigureBot(-0.35f + i * 0.32f, startingLanes[i]);
                var ai = bot.AddComponent<RiverBotAI>();
                ai.Configure(i + 1, terrain.Seed);
            }

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

            // Tributary pickups now come from RiverTributaryNetwork,
            // at actual confluences (no floating generic pickups).

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

            var race = new GameObject("Race Session & Leaderboard");
            race.AddComponent<RiverRaceManager>();

            // Natural cyan, glassy blue racer colors until >=8 real human participants.
            // Offline bot count does not trigger rainbow mode.
            var colorManager = new GameObject("Dynamic Water Colors");
            colorManager.AddComponent<RiverColorSystem>();

            // Generated, copyright-free ambient loops: water, mountain wind and birds/leaves.
            // A single AudioListener is already attached to the camera.
            var ambience = new GameObject("Ambient Water Wind Nature");
            ambience.AddComponent<RiverNatureAudio>();

            var settings = new GameObject("Performance & FPS");
            settings.AddComponent<AdaptiveQualityController>();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            const string scenePath = "Assets/Scenes/RiverRushPrototype.unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Created seeded branching river, batched forests and rocks, physics, AI, nature audio and adaptive FPS.");
        }

        private static Material EnsureMaterial(string path, string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("Missing shader: " + shaderName +
                    ". Create a Unity 6 Universal 3D (URP) project.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    AssetDatabase.CreateFolder("Assets", "Materials");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                // Upgrade old opaque Lit materials when reopening an older demo.
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_BaseTint")) material.SetColor("_BaseTint", color);
            if (material.HasProperty("_ShallowColor")) material.SetColor("_ShallowColor", color);
            if (material.HasProperty("_OceanColor")) material.SetColor("_OceanColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
