using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Builds a light two-sided river ribbon along RiverRoute. No water physics.
    /// Add MeshFilter + MeshRenderer, assign a StylizedRiverURP material, then Rebuild.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RiverWaterSurface : MonoBehaviour
    {
        [SerializeField] private RiverRoute route;
        [SerializeField, Min(0.5f)] private float width = 10f;
        [SerializeField, Min(0.1f)] private float textureRepeatMeters = 5f;
        [SerializeField] private Material waterMaterial;
        private Mesh generatedMesh;
        private Material runtimeMaterial;
        private MaterialPropertyBlock waterProperties;

        public void SetDetail(float detail)
        {
            if (waterProperties == null) waterProperties = new MaterialPropertyBlock();
            var renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(waterProperties);
            waterProperties.SetFloat("_Detail", Mathf.Clamp01(detail));
            renderer.SetPropertyBlock(waterProperties);
        }

        private void Start() => Rebuild();

        [ContextMenu("Rebuild Water Surface")]
        public void Rebuild()
        {
            if (route == null) route = FindFirstObjectByType<RiverRoute>();
            if (route == null)
            {
                Debug.LogError("RiverWaterSurface requires a RiverRoute.", this);
                return;
            }

            if (route.Points == null || route.Points.Count < 2) route.Rebuild();
            if (route.Points == null || route.Points.Count < 2) return;

            int count = route.Points.Count;
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[count * 2];
            int[] triangles = new int[(count - 1) * 6];
            float traveled = 0f;
            Vector3 previous = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Vector3 center = route.transform.TransformPoint(route.Points[i]);
                Vector3 from = route.transform.TransformPoint(route.Points[Mathf.Max(0, i - 1)]);
                Vector3 to = route.transform.TransformPoint(route.Points[Mathf.Min(count - 1, i + 1)]);
                Vector3 forward = to - from;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.00001f ? forward.normalized : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                if (i > 0) traveled += Vector3.Distance(previous, center);
                previous = center;
                Vector3 left = center - right * width * 0.5f;
                Vector3 edge = center + right * width * 0.5f;
                vertices[2 * i] = transform.InverseTransformPoint(left);
                vertices[2 * i + 1] = transform.InverseTransformPoint(edge);
                float repeat = traveled / Mathf.Max(0.1f, textureRepeatMeters);
                uvs[2 * i] = new Vector2(0f, repeat);
                uvs[2 * i + 1] = new Vector2(1f, repeat);

                if (i == count - 1) continue;
                int t = i * 6;
                int p = i * 2;
                // Winding faces upward when the route moves from -Z to +Z.
                triangles[t] = p;
                triangles[t + 1] = p + 2;
                triangles[t + 2] = p + 1;
                triangles[t + 3] = p + 1;
                triangles[t + 4] = p + 2;
                triangles[t + 5] = p + 3;
            }

            if (generatedMesh != null)
            {
                if (Application.isPlaying) Destroy(generatedMesh);
                else DestroyImmediate(generatedMesh);
            }

            generatedMesh = new Mesh { name = "River Rush Water Ribbon" };
            generatedMesh.vertices = vertices;
            generatedMesh.uv = uvs;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;

            var renderer = GetComponent<MeshRenderer>();
            if (waterMaterial != null) renderer.sharedMaterial = waterMaterial;
            else if (renderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("RiverRush/StylizedRiverURP");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader) { name = "River Rush Water (Runtime)" };
                    renderer.sharedMaterial = runtimeMaterial;
                }
                else Debug.LogWarning("Create a material using Shader 'RiverRush/StylizedRiverURP' and assign it to the water surface.", this);
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (generatedMesh != null) Destroy(generatedMesh);
                if (runtimeMaterial != null) Destroy(runtimeMaterial);
            }
            else
            {
                if (generatedMesh != null) DestroyImmediate(generatedMesh);
                if (runtimeMaterial != null) DestroyImmediate(runtimeMaterial);
            }
        }
    }
}
