#if UNITY_EDITOR
using RiverRush.Gameplay;
using RiverRush.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiverRush.Editor
{
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
            var terrain = world.AddComponent<ProceduralTerrain>();
            world.AddComponent<MeshCollider>();
            terrain.Generate();
            var route = world.AddComponent<RiverRoute>();
            route.Rebuild();

            terrainRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/Terrain.mat", "Universal Render Pipeline/Lit",
                new Color(0.28f, 0.49f, 0.30f));

            var river = new GameObject("Animated River Surface");
            river.AddComponent<MeshFilter>();
            var riverRenderer = river.AddComponent<MeshRenderer>();
            riverRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/StylizedRiver.mat", "RiverRush/StylizedRiverURP",
                new Color(0.07f, 0.68f, 0.78f));
            river.AddComponent<RiverWaterSurface>().Rebuild();

            var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player - River Droplet";
            player.transform.localScale = Vector3.one * 2.2f;
            if (route.Points != null && route.Points.Count > 0)
                player.transform.position = route.transform.TransformPoint(route.Points[0]);
            var playerRenderer = player.GetComponent<MeshRenderer>();
            playerRenderer.sharedMaterial = EnsureMaterial(
                "Assets/Materials/Player.mat", "Universal Render Pipeline/Lit",
                new Color(0.04f, 0.76f, 0.91f));

            var body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            player.AddComponent<RiverRunner>();
            player.AddComponent<RiverGrowth>();

            // Three simple pickup locations to demonstrate water volume growth.
            int[] pickupIndexes = { 24, 55, 95 };
            for (int p = 0; p < pickupIndexes.Length; p++)
            {
                if (route.Points == null || pickupIndexes[p] >= route.Points.Count) continue;
                var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pickup.name = "Tributary Pickup " + (p + 1);
                pickup.transform.position = route.transform.TransformPoint(route.Points[pickupIndexes[p]]) + Vector3.up;
                pickup.transform.localScale = Vector3.one * 3.5f;
                pickup.GetComponent<SphereCollider>().isTrigger = true;
                pickup.GetComponent<MeshRenderer>().sharedMaterial = playerRenderer.sharedMaterial;
                pickup.AddComponent<TributaryPickup>();
            }

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
            sun.intensity = 1.25f;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
                "Assets/Scenes/RiverRushPrototype.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("River Rush prototype created. Press Play. This is not yet fluid simulation.");
        }

        private static Material EnsureMaterial(string path, string shaderName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("Shader not found: " + shaderName + ". Create a Unity 6 Universal 3D (URP) project.");
                return null;
            }

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_ShallowColor")) material.SetColor("_ShallowColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
