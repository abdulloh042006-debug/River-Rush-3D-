using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Seeded, collision-free scenery for both Android and Windows.
    /// One combined mesh per detail layer instead of hundreds of GameObjects.
    /// No downloaded models or textures are required.
    /// </summary>
    [RequireComponent(typeof(ProceduralTerrain))]
    public sealed class ProceduralNature : MonoBehaviour
    {
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField] private Material natureMaterial;
        [Header("Maximum per-world population")]
        [SerializeField, Range(40, 500)] private int treeBudget = 270;
        [SerializeField, Range(20, 450)] private int rockBudget = 180;
        [SerializeField, Range(20, 600)] private int shrubBudget = 240;
        [SerializeField, Range(20, 500)] private int riverbankReedBudget = 160;
        [SerializeField, Range(4f, 28f)] private float bankClearance = 11f;

        private GameObject generatedRoot;
        private readonly List<Mesh> meshes = new List<Mesh>(4);
        private MeshRenderer essentialTrees, detailedTrees, rockRenderer, shrubRenderer;
        private Material fallbackMaterial;
        private float currentDetail = 0.65f;

        public int TreesCreated { get; private set; }
        public int RocksCreated { get; private set; }
        public int ShrubsCreated { get; private set; }
        public int ReedsCreated { get; private set; }
        public int MeshCount => meshes.Count;

        public void ConfigureMaterial(Material material) { natureMaterial = material; }
        private void Start() => Rebuild();

        public void SetDetail(float detail)
        {
            currentDetail = Mathf.Clamp01(detail);
            if (essentialTrees != null) essentialTrees.enabled = true;
            if (detailedTrees != null) detailedTrees.enabled = currentDetail >= 0.34f;
            if (rockRenderer != null) rockRenderer.enabled = true;
            if (shrubRenderer != null) shrubRenderer.enabled = currentDetail >= 0.55f;
        }

        [ContextMenu("Rebuild Procedural Nature")]
        public void Rebuild()
        {
            if (terrain == null) terrain = GetComponent<ProceduralTerrain>();
            if (terrain == null) return;
            ClearGenerated();
            var essential = new GeometryBuffer();
            var detailed = new GeometryBuffer();
            var rocks = new GeometryBuffer();
            var shrubs = new GeometryBuffer();

            var rng = new System.Random(unchecked(terrain.Seed ^ 0x7F3D912));
            float size = terrain.WorldSize;
            float half = size * 0.46f;
            TreesCreated = RocksCreated = ShrubsCreated = ReedsCreated = 0;

            // Alpine rockfields near the source; forests in sheltered valleys;
            // lush meadow shrubs and small stones toward the estuary.
            for (int attempt = 0; attempt < treeBudget * 9 && TreesCreated < treeBudget; attempt++)
            {
                float x = RandomRange(rng, -half, half);
                float z = RandomRange(rng, -half, half);
                float v = z / size + 0.5f;
                if (terrain.IsNearWaterway(x, z, bankClearance)) continue;
                float y = terrain.SampleHeight(x, z);
                float slope = Slope(x, z);
                if (slope > 0.65f) continue;
                float moisture = Mathf.PerlinNoise((x + terrain.Seed % 300) * 0.014f,
                                                  (z - terrain.Seed % 400) * 0.014f);
                float biome = (v < 0.18f ? 0.26f : v > 0.88f ? 0.52f : 0.88f);
                if (rng.NextDouble() > biome * (0.60f + moisture * 0.55f)) continue;

                bool alpine = v < 0.31f || slope > 0.32f;
                bool pine = alpine || rng.NextDouble() < 0.48f;
                float height = pine ? RandomRange(rng, 5f, 11f)
                                    : RandomRange(rng, 4.5f, 9.5f);
                height *= Mathf.Lerp(0.65f, 1.2f, moisture);
                float radius = height * (pine ? 0.30f : 0.24f);
                var buffer = TreesCreated % 2 == 0 ? essential : detailed;
                Color trunk = new Color(RandomRange(rng, 0.29f, 0.42f),
                                        RandomRange(rng, 0.18f, 0.27f), 0.12f, 1f);
                Color leaves = pine ?
                    new Color(RandomRange(rng, 0.09f, 0.23f),
                              RandomRange(rng, 0.31f, 0.52f),
                              RandomRange(rng, 0.15f, 0.25f), 1f) :
                    new Color(RandomRange(rng, 0.15f, 0.34f),
                              RandomRange(rng, 0.39f, 0.65f),
                              RandomRange(rng, 0.10f, 0.25f), 1f);

                Vector3 root = new Vector3(x, y - 0.1f, z);
                buffer.AddColumn(root, height * 0.065f, height * 0.39f, trunk);
                if (pine)
                {
                    buffer.AddCone(root + Vector3.up * (height * 0.20f),
                                   radius, height * 0.62f, leaves);
                    buffer.AddCone(root + Vector3.up * (height * 0.49f),
                                   radius * 0.72f, height * 0.47f, leaves * 1.12f);
                }
                else
                {
                    buffer.AddOctahedron(root + Vector3.up * (height * 0.75f),
                        new Vector3(radius * 1.2f, height * 0.34f, radius * 1.2f), leaves);
                }
                TreesCreated++;
            }

            for (int attempt = 0; attempt < rockBudget * 8 && RocksCreated < rockBudget; attempt++)
            {
                float x = RandomRange(rng, -half, half);
                float z = RandomRange(rng, -half, half);
                if (terrain.IsNearWaterway(x, z, 3.6f)) continue;
                float y = terrain.SampleHeight(x, z);
                float v = z / size + 0.5f;
                float slope = Slope(x, z);
                // Rockier high-altitude and steep regions, occasional shoreline boulders.
                float probability = 0.25f + (1f - v) * 0.43f + Mathf.Min(0.35f, slope * 0.5f);
                if (rng.NextDouble() > probability) continue;
                float scale = RandomRange(rng, 1.2f, 4f) * (slope > 0.5f ? 1.5f : 1f);
                Color rock = Color.Lerp(new Color(0.36f, 0.37f, 0.38f),
                    new Color(0.58f, 0.56f, 0.51f), (float)rng.NextDouble());
                rocks.AddOctahedron(new Vector3(x, y + scale * 0.32f, z),
                    new Vector3(scale * 0.93f, scale * 0.66f, scale * 0.72f), rock);
                RocksCreated++;
            }

            for (int attempt = 0; attempt < shrubBudget * 8 && ShrubsCreated < shrubBudget; attempt++)
            {
                float x = RandomRange(rng, -half, half);
                float z = RandomRange(rng, -half, half);
                float v = z / size + 0.5f;
                if (v < 0.20f || terrain.IsNearWaterway(x, z, 3.0f)) continue;
                float slope = Slope(x, z);
                if (slope > 0.47f) continue;
                float y = terrain.SampleHeight(x, z);
                float width = RandomRange(rng, 0.5f, 1.8f);
                float greens = RandomRange(rng, 0.32f, 0.67f);
                Color leaf = new Color(RandomRange(rng, 0.14f, 0.28f),
                                       greens, RandomRange(rng, 0.08f, 0.19f));
                shrubs.AddOctahedron(new Vector3(x, y + width * 0.33f, z),
                    new Vector3(width, width * 0.65f, width), leaf);
                ShrubsCreated++;
            }

            // Add narrow belts of cattails / river reeds. The candidate must be
            // close to a river but outside its playable water corridor.
            for (int attempt = 0;
                 attempt < riverbankReedBudget * 35 && ReedsCreated < riverbankReedBudget;
                 attempt++)
            {
                float x = RandomRange(rng, -half, half);
                float z = RandomRange(rng, -half, half);
                float v = z / size + 0.5f;
                if (v < 0.18f || v > 0.97f) continue;
                float shoreline = terrain.WaterwayDistance(x, z);
                float baseBank = terrain.RiverWidth * 0.55f;
                if (shoreline < baseBank + 2.5f ||
                    shoreline > baseBank + 8.5f) continue;
                float slope = Slope(x, z);
                if (slope > 0.40f) continue;
                float y = terrain.SampleHeight(x, z);
                float reedHeight = RandomRange(rng, 0.95f, 2.25f);
                Vector3 basePoint = new Vector3(x, y - 0.08f, z);
                Color stem = new Color(RandomRange(rng, 0.23f, 0.39f),
                    RandomRange(rng, 0.37f, 0.57f), RandomRange(rng, 0.13f, 0.22f));
                shrubs.AddColumn(basePoint, reedHeight * 0.034f,
                    reedHeight, stem);
                if (rng.NextDouble() < 0.40)
                {
                    shrubs.AddColumn(basePoint + Vector3.up * (reedHeight * 0.78f),
                        reedHeight * 0.052f, reedHeight * 0.23f,
                        new Color(0.36f, 0.25f, 0.14f));
                }
                ReedsCreated++;
            }

            if (natureMaterial == null && fallbackMaterial == null)
            {
                Shader shader = Shader.Find("RiverRush/NatureVertexColorURP");
                if (shader != null) fallbackMaterial = new Material(shader);
            }

            generatedRoot = new GameObject("Generated Forests, Rocks and Biomes");
            generatedRoot.transform.SetParent(transform, false);
            essentialTrees = CreateLayer("Forest - Essential", essential);
            detailedTrees = CreateLayer("Forest - Detail", detailed);
            rockRenderer = CreateLayer("Cliffs and Boulders", rocks);
            shrubRenderer = CreateLayer("Grass and Riverbank Shrubs", shrubs);
            SetDetail(currentDetail);
            Debug.Log("River Rush nature seed " + terrain.Seed +
                      ": " + TreesCreated + " trees, " + RocksCreated +
                      " rocks, " + ShrubsCreated + " shrubs, " + ReedsCreated +
                      " river reeds in " + MeshCount +
                      " mesh batches.");
        }

        private float Slope(float x, float z)
        {
            const float epsilon = 2f;
            float h = terrain.SampleHeight(x, z);
            float dx = Mathf.Abs(terrain.SampleHeight(x + epsilon, z) - h) / epsilon;
            float dz = Mathf.Abs(terrain.SampleHeight(x, z + epsilon) - h) / epsilon;
            return Mathf.Max(dx, dz);
        }

        private MeshRenderer CreateLayer(string label, GeometryBuffer buffer)
        {
            if (buffer.VertexCount == 0) return null;
            Mesh mesh = buffer.ToMesh(label);
            meshes.Add(mesh);
            var child = new GameObject(label);
            child.transform.SetParent(generatedRoot.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = natureMaterial != null ? natureMaterial : fallbackMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private void ClearGenerated()
        {
            if (generatedRoot != null)
            {
                generatedRoot.SetActive(false);
                if (Application.isPlaying) Destroy(generatedRoot);
                else DestroyImmediate(generatedRoot);
                generatedRoot = null;
            }
            foreach (var mesh in meshes)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            meshes.Clear();
            essentialTrees = detailedTrees = rockRenderer = shrubRenderer = null;
        }

        private void OnDestroy()
        {
            ClearGenerated();
            if (fallbackMaterial != null)
            {
                if (Application.isPlaying) Destroy(fallbackMaterial);
                else DestroyImmediate(fallbackMaterial);
            }
        }

        private sealed class GeometryBuffer
        {
            private readonly List<Vector3> vertices = new List<Vector3>(8000);
            private readonly List<int> triangles = new List<int>(14000);
            private readonly List<Color> colors = new List<Color>(8000);
            public int VertexCount => vertices.Count;

            private int Vertex(Vector3 position, Color color)
            {
                int index = vertices.Count;
                vertices.Add(position);
                colors.Add(color);
                return index;
            }

            private void Triangle(int a, int b, int c)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            public void AddColumn(Vector3 bottom, float radius, float height, Color color)
            {
                const int sides = 6;
                int start = vertices.Count;
                for (int i = 0; i < sides; i++)
                {
                    float angle = Mathf.PI * 2f * i / sides;
                    Vector3 radial = new Vector3(Mathf.Cos(angle) * radius, 0f,
                                                  Mathf.Sin(angle) * radius);
                    Vertex(bottom + radial, color * 0.79f);
                    Vertex(bottom + radial * 0.72f + Vector3.up * height, color);
                }
                for (int i = 0; i < sides; i++)
                {
                    int a = start + i * 2;
                    int b = start + ((i + 1) % sides) * 2;
                    Triangle(a, a + 1, b);
                    Triangle(b, a + 1, b + 1);
                }
            }

            public void AddCone(Vector3 baseCenter, float radius, float height, Color color)
            {
                const int sides = 7;
                int tip = Vertex(baseCenter + Vector3.up * height, color * 1.05f);
                int[] ring = new int[sides];
                for (int i = 0; i < sides; i++)
                {
                    float angle = Mathf.PI * 2f * i / sides;
                    ring[i] = Vertex(baseCenter + new Vector3(Mathf.Cos(angle) * radius,
                                        0f, Mathf.Sin(angle) * radius),
                                        color * (0.79f + i * 0.027f));
                }
                for (int i = 0; i < sides; i++)
                    Triangle(ring[i], tip, ring[(i + 1) % sides]);
            }

            public void AddOctahedron(Vector3 center, Vector3 radii, Color color)
            {
                int top = Vertex(center + Vector3.up * radii.y, color * 1.07f);
                int bottom = Vertex(center - Vector3.up * radii.y, color * 0.72f);
                int east = Vertex(center + Vector3.right * radii.x, color * 0.92f);
                int west = Vertex(center - Vector3.right * radii.x, color * 0.82f);
                int north = Vertex(center + Vector3.forward * radii.z, color * 0.98f);
                int south = Vertex(center - Vector3.forward * radii.z, color * 0.85f);
                Triangle(top, north, east);
                Triangle(top, west, north);
                Triangle(top, south, west);
                Triangle(top, east, south);
                Triangle(bottom, east, north);
                Triangle(bottom, north, west);
                Triangle(bottom, west, south);
                Triangle(bottom, south, east);
            }

            public Mesh ToMesh(string title)
            {
                var mesh = new Mesh { name = title };
                if (vertices.Count > 65000)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
