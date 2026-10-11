using UnityEngine;

namespace RiverRush.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralTerrain : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private int seed = 12345;
        [SerializeField, Range(16, 256)] private int resolution = 96;
        [SerializeField] private float worldSize = 500f;
        [SerializeField] private float mountainHeight = 95f;
        [SerializeField] private float noiseScale = 0.012f;
        [SerializeField] private float riverWidth = 12f;
        [SerializeField] private float riverDepth = 9f;
        [SerializeField] private float seaLevel = 3f;

        private Mesh generatedMesh;
        public int Seed => seed;

        [ContextMenu("Generate Terrain")]
        public void Generate()
        {
            if (worldSize <= 0 || noiseScale <= 0) return;
            int n = resolution;
            var vertices = new Vector3[(n + 1) * (n + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[n * n * 6];
            var rng = new System.Random(seed);
            float offsetX = rng.Next(-100000, 100000);
            float offsetZ = rng.Next(-100000, 100000);

            for (int z = 0; z <= n; z++)
            for (int x = 0; x <= n; x++)
            {
                float u = x / (float)n;
                float v = z / (float)n;
                float px = (u - 0.5f) * worldSize;
                float pz = (v - 0.5f) * worldSize;
                float ridge = Mathf.PerlinNoise(px * noiseScale + offsetX, pz * noiseScale + offsetZ);
                float detail = Mathf.PerlinNoise(px * noiseScale * 3f + offsetX, pz * noiseScale * 3f + offsetZ);
                float downhill = Mathf.Pow(1f - v, 1.3f);
                float height = mountainHeight * downhill * (0.45f + 0.45f * ridge + 0.1f * detail);
                float center = RiverCenter(v, offsetX);
                float distance = Mathf.Abs(px - center);
                float riverMask = 1f - Mathf.SmoothStep(riverWidth * 0.5f, riverWidth * 1.5f, distance);
                height -= riverDepth * riverMask;
                height = Mathf.Max(0f, height);
                int index = z * (n + 1) + x;
                vertices[index] = new Vector3(px, height, pz);
                uv[index] = new Vector2(u, v);
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

            generatedMesh = new Mesh { name = "River Rush Procedural Terrain" };
            generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            generatedMesh.vertices = vertices;
            generatedMesh.triangles = triangles;
            generatedMesh.uv = uv;
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
            var collider = GetComponent<MeshCollider>();
            if (collider != null) collider.sharedMesh = generatedMesh;
        }

        private float RiverCenter(float v, float offset)
        {
            return Mathf.Sin(v * 7f + offset * 0.001f) * worldSize * 0.12f
                 + Mathf.Sin(v * 17f + offset * 0.002f) * worldSize * 0.035f;
        }

        private void Start() => Generate();
        private void OnValidate()
        {
            resolution = Mathf.Max(16, resolution);
            worldSize = Mathf.Max(1f, worldSize);
            noiseScale = Mathf.Max(0.0001f, noiseScale);
        }
    }
}
