using System;
using UnityEngine;

namespace RiverRush.World
{
    /// <summary>Seeded mountains with a continuous downhill channel (prototype terrain, not fluid simulation).</summary>
    [DefaultExecutionOrder(-300)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralTerrain : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool randomizeEachRace = true;
        [SerializeField, Range(16, 256)] private int resolution = 96;
        [SerializeField, Min(1f)] private float worldSize = 500f;
        [SerializeField, Min(0f)] private float mountainHeight = 95f;
        [SerializeField, Min(0.0001f)] private float noiseScale = 0.012f;
        [SerializeField, Min(1f)] private float riverWidth = 12f;
        [SerializeField, Min(0f)] private float seaLevel = 3f;

        private Mesh generatedMesh;

        public int Seed => seed;
        public bool RandomizeEachRace => randomizeEachRace;
        public float WorldSize => worldSize;
        public float RiverWidth => riverWidth;
        public float SeaLevel => seaLevel;

        private void Awake()
        {
            if (Application.isPlaying && randomizeEachRace)
                seed = unchecked((int)(System.DateTime.UtcNow.Ticks ^
                                       System.Environment.TickCount));
            Generate();
        }

        private void NoiseOffsets(out float offsetX, out float offsetZ)
        {
            var random = new System.Random(seed);
            offsetX = random.Next(-100000, 100000);
            offsetZ = random.Next(-100000, 100000);
        }

        /// <summary>Normalized 0=mountain source, 1=sea. In this first prototype the river follows +Z.</summary>
        public float RiverCenter(float v)
        {
            NoiseOffsets(out float offsetX, out _);
            v = Mathf.Clamp01(v);
            return Mathf.Sin(v * 7f + offsetX * 0.001f) * worldSize * 0.12f
                 + Mathf.Sin(v * 17f + offsetX * 0.002f) * worldSize * 0.035f;
        }

        public float SampleHeight(float x, float z)
        {
            NoiseOffsets(out float offsetX, out float offsetZ);
            return EvaluateHeight(x, z, offsetX, offsetZ);
        }

        private float EvaluateHeight(float x, float z, float offsetX, float offsetZ)
        {
            float v = Mathf.Clamp01(z / worldSize + 0.5f);
            float downhill = Mathf.Pow(1f - v, 1.25f);
            float ridge = Mathf.PerlinNoise(x * noiseScale + offsetX, z * noiseScale + offsetZ);
            float detail = Mathf.PerlinNoise(x * noiseScale * 3f + offsetX, z * noiseScale * 3f + offsetZ);
            float terrainHeight = seaLevel + mountainHeight * downhill
                * (0.45f + 0.45f * ridge + 0.1f * detail);

            // An explicit monotonic channel bed keeps the river from climbing hills.
            float center = Mathf.Sin(v * 7f + offsetX * 0.001f) * worldSize * 0.12f
                         + Mathf.Sin(v * 17f + offsetX * 0.002f) * worldSize * 0.035f;
            float distance = Mathf.Abs(x - center);
            float mask = 1f - Mathf.SmoothStep(riverWidth * 0.4f, riverWidth * 1.3f, distance);
            float riverBed = seaLevel + mountainHeight * 0.46f * Mathf.Pow(1f - v, 1.4f);
            return Mathf.Lerp(terrainHeight, riverBed, mask);
        }

        [ContextMenu("Generate Terrain")]
        public void Generate()
        {
            if (worldSize <= 0f || noiseScale <= 0f) return;
            NoiseOffsets(out float offsetX, out float offsetZ);
            int n = Mathf.Clamp(resolution, 16, 256);
            var vertices = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[n * n * 6];

            for (int z = 0; z <= n; z++)
            for (int x = 0; x <= n; x++)
            {
                float u = x / (float)n;
                float v = z / (float)n;
                float px = (u - 0.5f) * worldSize;
                float pz = (v - 0.5f) * worldSize;
                int index = z * (n + 1) + x;
                vertices[index] = new Vector3(px, EvaluateHeight(px, pz, offsetX, offsetZ), pz);
                uvs[index] = new Vector2(u, v);
            }

            int t = 0;
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i = z * (n + 1) + x;
                triangles[t++] = i;
                triangles[t++] = i + n + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + n + 1;
                triangles[t++] = i + n + 2;
            }

            if (generatedMesh != null)
            {
                if (Application.isPlaying) Destroy(generatedMesh);
                else DestroyImmediate(generatedMesh);
            }

            generatedMesh = new Mesh { name = "River Rush Terrain" };
            generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            generatedMesh.vertices = vertices;
            generatedMesh.uv = uvs;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;

            var collider = GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = generatedMesh;
            }
        }

        private void OnValidate()
        {
            resolution = Mathf.Clamp(resolution, 16, 256);
            worldSize = Mathf.Max(1f, worldSize);
            riverWidth = Mathf.Max(1f, riverWidth);
            noiseScale = Mathf.Max(0.0001f, noiseScale);
        }

        private void OnDestroy()
        {
            if (generatedMesh == null) return;
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
    }
}
