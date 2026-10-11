using System.Collections.Generic;
using UnityEngine;
using RiverRush.Gameplay;

namespace RiverRush.World
{
    /// <summary>
    /// Visual branches plus confluence pickups. Branch topology is shared with
    /// ProceduralTerrain: seed -> same carved channels and water ribbons.
    /// No runtime GameObject is created for every river vertex.
    /// </summary>
    [RequireComponent(typeof(ProceduralTerrain), typeof(RiverRoute))]
    public sealed class RiverTributaryNetwork : MonoBehaviour
    {
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField] private RiverRoute route;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material pickupMaterial;
        [SerializeField, Range(0.65f, 1f)] private float ribbonWidthRatio = 0.88f;
        [SerializeField, Min(0.1f)] private float waterClearance = 0.70f;
        [SerializeField] private bool spawnConfluencePickups = true;

        private readonly List<Mesh> generatedMeshes = new List<Mesh>();
        private readonly List<MeshRenderer> branchRenderers = new List<MeshRenderer>();
        private MaterialPropertyBlock settings;
        private GameObject generatedRoot;
        private float visualDetail = 0.65f;
        private Color naturalDeep = new Color(0.025f, 0.20f, 0.39f, 1f);
        private Color naturalShallow = new Color(0.09f, 0.63f, 0.77f, 1f);

        public int BranchCount => terrain != null && terrain.Branches != null
            ? terrain.Branches.Branches.Length : 0;
        public int GeneratedWaterMeshCount => generatedMeshes.Count;
        public int GeneratedPickupCount { get; private set; }

        public void ConfigureMaterials(Material river, Material pickups)
        {
            waterMaterial = river;
            pickupMaterial = pickups;
        }

        private void Start() => Rebuild();

        [ContextMenu("Rebuild Tributary Network")]
        public void Rebuild()
        {
            if (terrain == null) terrain = GetComponent<ProceduralTerrain>();
            if (route == null) route = GetComponent<RiverRoute>();
            if (terrain == null || terrain.Branches == null) return;

            ClearGenerated();
            generatedRoot = new GameObject("Generated Tributaries - Seed " + terrain.Seed);
            generatedRoot.transform.SetParent(transform, false);
            var branches = terrain.Branches.Branches;
            for (int index = 0; index < branches.Length; index++)
            {
                var branch = branches[index];
                Mesh mesh = BuildRibbon(branch, index);
                if (mesh == null) continue;
                generatedMeshes.Add(mesh);
                var child = new GameObject("Tributary Water " + (index + 1));
                child.transform.SetParent(generatedRoot.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = waterMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                branchRenderers.Add(renderer);

                if (spawnConfluencePickups)
                {
                    CreateConfluencePickup(branch, index);
                }
            }
            ApplyMaterialProperties();
        }

        private Mesh BuildRibbon(RiverBranchLayout.Branch branch, int id)
        {
            var points = branch.Points;
            if (points == null || points.Length < 2) return null;
            var vertices = new Vector3[points.Length * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(points.Length - 1) * 6];
            float traveled = 0f;
            float halfWidth = branch.Width * 0.5f * ribbonWidthRatio;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 current = points[i];
                Vector2 before = points[Mathf.Max(0, i - 1)];
                Vector2 after = points[Mathf.Min(points.Length - 1, i + 1)];
                Vector2 tangent = (after - before).normalized;
                if (tangent.sqrMagnitude < 0.001f) tangent = Vector2.up;
                Vector2 right = new Vector2(tangent.y, -tangent.x);
                if (i > 0) traveled += Vector2.Distance(points[i - 1], current);
                Vector2 leftPosition = current - right * halfWidth;
                Vector2 rightPosition = current + right * halfWidth;

                // Sampling each bank avoids a floating strip on uneven slopes.
                vertices[i * 2] = new Vector3(leftPosition.x,
                    terrain.SampleHeight(leftPosition.x, leftPosition.y) + waterClearance,
                    leftPosition.y);
                vertices[i * 2 + 1] = new Vector3(rightPosition.x,
                    terrain.SampleHeight(rightPosition.x, rightPosition.y) + waterClearance,
                    rightPosition.y);
                uv[i * 2] = new Vector2(0f, traveled / 5f);
                uv[i * 2 + 1] = new Vector2(1f, traveled / 5f);

                if (i == points.Length - 1) continue;
                int t = i * 6;
                int p = i * 2;
                triangles[t] = p;
                triangles[t + 1] = p + 2;
                triangles[t + 2] = p + 1;
                triangles[t + 3] = p + 1;
                triangles[t + 4] = p + 2;
                triangles[t + 5] = p + 3;
            }
            Mesh mesh = new Mesh { name = "Tributary " + (id + 1) + " Water" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateConfluencePickup(RiverBranchLayout.Branch branch, int index)
        {
            var end = branch.Points[branch.Points.Length - 1];
            var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = "Tributary Pickup " + (index + 1);
            pickup.transform.SetParent(generatedRoot.transform, false);
            pickup.transform.localPosition = new Vector3(end.x,
                terrain.SampleHeight(end.x, end.y) + waterClearance + 0.5f, end.y);
            pickup.transform.localScale = Vector3.one * 2.6f;
            var collider = pickup.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            var renderer = pickup.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = pickupMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pickup.AddComponent<TributaryPickup>();
            GeneratedPickupCount++;
        }

        /// <summary>
        /// Physical surface/current sampling for floating objects in smaller
        /// tributaries. The player's race route remains on the main channel.
        /// </summary>
        public bool TrySampleSurface(Vector3 position, out Vector3 surface, out Vector3 current)
        {
            surface = Vector3.zero;
            current = Vector3.zero;
            if (terrain == null || terrain.Branches == null) return false;
            Vector3 local = transform.InverseTransformPoint(position);
            Vector2 planar = new Vector2(local.x, local.z);
            float bestSquared = float.PositiveInfinity;
            RiverBranchLayout.Branch closest = null;
            Vector2 closestPoint = Vector2.zero;
            Vector2 tangent = Vector2.up;

            foreach (var branch in terrain.Branches.Branches)
            {
                float limit = branch.Width * ribbonWidthRatio * 0.5f;
                if (planar.y < branch.Points[0].y - limit ||
                    planar.y > branch.Points[branch.Points.Length - 1].y + limit)
                    continue;
                for (int i = 1; i < branch.Points.Length; i++)
                {
                    Vector2 a = branch.Points[i - 1];
                    Vector2 b = branch.Points[i];
                    Vector2 ab = b - a;
                    float denominator = ab.sqrMagnitude;
                    if (denominator <= 0.0001f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(planar - a, ab) / denominator);
                    Vector2 nearest = a + ab * t;
                    float d2 = (nearest - planar).sqrMagnitude;
                    if (d2 >= bestSquared || d2 > limit * limit) continue;
                    bestSquared = d2;
                    closest = branch;
                    closestPoint = nearest;
                    tangent = ab.normalized;
                }
            }
            if (closest == null) return false;
            float waterHeight = terrain.SampleHeight(closestPoint.x, closestPoint.y) +
                                waterClearance;
            surface = transform.TransformPoint(new Vector3(planar.x, waterHeight, planar.y));
            Vector3 fromDirection = new Vector3(tangent.x, 0f, tangent.y);
            float v = Mathf.Clamp01(closestPoint.y / terrain.WorldSize + 0.5f);
            // The channel bed steepness sets an approximate downhill drift.
            float bedBefore = terrain.RiverBedAt(Mathf.Max(0f, v - 0.01f));
            float bedAfter = terrain.RiverBedAt(Mathf.Min(1f, v + 0.01f));
            float slope = Mathf.Clamp01((bedBefore - bedAfter) /
                Mathf.Max(0.01f, terrain.WorldSize * 0.02f));
            float speed = Mathf.Lerp(3f, 13f, slope * 7f);
            current = transform.TransformDirection(fromDirection.normalized) * speed;
            return true;
        }

        public void SetDetail(float detail)
        {
            visualDetail = Mathf.Clamp01(detail);
            ApplyMaterialProperties();
        }

        public void SetNaturalColors(Color deep, Color shallow)
        {
            naturalDeep = deep;
            naturalShallow = shallow;
            ApplyMaterialProperties();
        }

        private void ApplyMaterialProperties()
        {
            if (settings == null) settings = new MaterialPropertyBlock();
            foreach (var renderer in branchRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(settings);
                settings.SetFloat("_Detail", visualDetail);
                settings.SetColor("_DeepColor", naturalDeep);
                settings.SetColor("_ShallowColor", naturalShallow);
                renderer.SetPropertyBlock(settings);
                settings.Clear();
            }
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
            foreach (var mesh in generatedMeshes)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            generatedMeshes.Clear();
            branchRenderers.Clear();
            GeneratedPickupCount = 0;
        }

        private void OnDestroy() => ClearGenerated();
    }
}
