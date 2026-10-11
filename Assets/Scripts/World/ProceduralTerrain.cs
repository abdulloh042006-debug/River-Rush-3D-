using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Procedural 3D valley with a main river and five carved tributaries.
    /// The source, branches, trees and rocks use the same race seed.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralTerrain : MonoBehaviour
    {
        [Header("Seeded world")]
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool randomizeEachRace = true;
        [SerializeField, Range(32, 256)] private int resolution = 128;
        [SerializeField, Min(1f)] private float worldSize = 500f;
        [SerializeField, Min(0f)] private float mountainHeight = 95f;
        [SerializeField, Min(0.0001f)] private float noiseScale = 0.012f;
        [SerializeField, Min(1f)] private float riverWidth = 12f;
        [SerializeField, Min(0f)] private float seaLevel = 3f;

        private Mesh generatedMesh;
        private RiverBranchLayout branchLayout;
        private int cachedSeed = int.MinValue;
        private float cachedWorldSize = -1f;
        private float cachedWidth = -1f;
        private float offsetX, offsetZ;

        public int Seed => seed;
        public bool RandomizeEachRace => randomizeEachRace;
        public float WorldSize => worldSize;
        public float MountainHeight => mountainHeight;
        public float RiverWidth => riverWidth;
        public float SeaLevel => seaLevel;
        public RiverBranchLayout Branches { get { EnsureLayout(); return branchLayout; } }

        private void Awake()
        {
            if (Application.isPlaying && randomizeEachRace)
                seed = unchecked((int)(System.DateTime.UtcNow.Ticks ^
                                       System.Environment.TickCount));
            Generate();
        }

        private void EnsureLayout()
        {
            if (branchLayout != null && cachedSeed == seed &&
                cachedWorldSize == worldSize && cachedWidth == riverWidth) return;
            cachedSeed = seed;
            cachedWorldSize = worldSize;
            cachedWidth = riverWidth;
            var random = new System.Random(seed);
            offsetX = random.Next(-100000, 100000);
            offsetZ = random.Next(-100000, 100000);
            // RawRiverCenter avoids recursive EnsureLayout during branch generation.
            branchLayout = RiverBranchLayout.Build(seed, worldSize, riverWidth,
                v => RawRiverCenter(v, offsetX));
        }

        private float RawRiverCenter(float v, float noiseOffset)
        {
            v = Mathf.Clamp01(v);
            return Mathf.Sin(v * 7f + noiseOffset * 0.001f) * worldSize * 0.12f +
                   Mathf.Sin(v * 17f + noiseOffset * 0.002f) * worldSize * 0.035f;
        }

        public float RiverCenter(float v)
        {
            EnsureLayout();
            return RawRiverCenter(v, offsetX);
        }

        public float RiverBedAt(float normalizedZ)
        {
            return seaLevel + mountainHeight * 0.46f *
                   Mathf.Pow(1f - Mathf.Clamp01(normalizedZ), 1.4f);
        }

        public float SampleHeight(float x, float z)
        {
            EnsureLayout();
            return EvaluateHeight(x, z);
        }

        /// <summary>Distance in XZ metres to main channel or any carved tributary.</summary>
        public float WaterwayDistance(float x, float z)
        {
            EnsureLayout();
            float v = Mathf.Clamp01(z / worldSize + 0.5f);
            float mainDistance = Mathf.Abs(x - RawRiverCenter(v, offsetX));
            return Mathf.Min(mainDistance, branchLayout.DistanceToNearestBranch(new Vector2(x, z)));
        }

        public bool IsNearWaterway(float x, float z, float extraClearance)
        {
            return WaterwayDistance(x, z) < riverWidth * 0.68f +
                   Mathf.Max(0f, extraClearance);
        }

        private float EvaluateHeight(float x, float z)
        {
            float v = Mathf.Clamp01(z / worldSize + 0.5f);
            float downhill = Mathf.Pow(1f - v, 1.25f);
            float ridge = Mathf.PerlinNoise(x * noiseScale + offsetX, z * noiseScale + offsetZ);
            float detail = Mathf.PerlinNoise(x * noiseScale * 3f + offsetX, z * noiseScale * 3f + offsetZ);
            float highFrequency = Mathf.PerlinNoise(x * noiseScale * 6f + offsetZ,
                                                     z * noiseScale * 6f + offsetX);
            float jaggedRidge = 1f - Mathf.Abs(2f * ridge - 1f);
            float mountain = (0.40f + ridge * 0.38f + detail * 0.16f +
                              jaggedRidge * highFrequency * 0.06f);
            float terrainHeight = seaLevel + mountainHeight * downhill * mountain;
            float mainDistance = Mathf.Abs(x - RawRiverCenter(v, offsetX));
            float mask = 1f - Mathf.SmoothStep(riverWidth * 0.40f,
                                               riverWidth * 1.35f, mainDistance);

            // Carve the exact same branch paths the water renderer will draw.
            Vector2 location = new Vector2(x, z);
            foreach (var branch in branchLayout.Branches)
            {
                float distance = branchLayout.DistanceToBranch(location, branch);
                if (distance >= branch.Width * 1.4f) continue;
                float tributaryMask = 1f - Mathf.SmoothStep(branch.Width * 0.35f,
                                                            branch.Width * 1.4f, distance);
                mask = Mathf.Max(mask, tributaryMask);
            }
            return Mathf.Lerp(terrainHeight, RiverBedAt(v), mask);
        }

        [ContextMenu("Generate Terrain")]
        public void Generate()
        {
            if (worldSize <= 0f || noiseScale <= 0f) return;
            EnsureLayout();
            int n = Mathf.Clamp(resolution, 32, 256);
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
                vertices[index] = new Vector3(px, EvaluateHeight(px, pz), pz);
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
            generatedMesh = new Mesh { name = "River Rush Terrain and Tributaries" };
            generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            generatedMesh.vertices = vertices;
            generatedMesh.triangles = triangles;
            generatedMesh.uv = uvs;
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
            resolution = Mathf.Clamp(resolution, 32, 256);
            worldSize = Mathf.Max(1f, worldSize);
            riverWidth = Mathf.Max(1f, riverWidth);
            noiseScale = Mathf.Max(0.0001f, noiseScale);
            // Serialized inspector edits must not reuse stale river paths.
            branchLayout = null;
        }

        private void OnDestroy()
        {
            if (generatedMesh == null) return;
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
    }
}
