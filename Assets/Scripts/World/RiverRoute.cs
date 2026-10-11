using UnityEngine;

namespace RiverRush.World
{
    /// <summary>Deterministic downhill route sampled from the same winding centerline as terrain.</summary>
    public sealed class RiverRoute : MonoBehaviour
    {
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField, Min(8)] private int samples = 128;
        [SerializeField] private float worldSize = 500f;
        [SerializeField] private float surfaceOffset = 1f;
        private Vector3[] points;
        public System.Collections.Generic.IReadOnlyList<Vector3> Points => points;

        [ContextMenu("Rebuild Route")]
        public void Rebuild()
        {
            if (terrain == null) terrain = GetComponent<ProceduralTerrain>();
            if (terrain == null) { Debug.LogError("Missing ProceduralTerrain"); return; }
            var mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null) { terrain.Generate(); mesh = terrain.GetComponent<MeshFilter>().sharedMesh; }
            if (mesh == null) return;
            samples = Mathf.Max(8, samples);
            points = new Vector3[samples];
            var rng = new System.Random(terrain.Seed);
            float offsetX = rng.Next(-100000, 100000);
            for (int i = 0; i < samples; i++)
            {
                float v = i / (float)(samples - 1);
                float x = Mathf.Sin(v * 7f + offsetX * 0.001f) * worldSize * 0.12f
                        + Mathf.Sin(v * 17f + offsetX * 0.002f) * worldSize * 0.035f;
                float z = (v - 0.5f) * worldSize;
                float y = 0f;
                // Approximate route height using a vertical ray against a temporary mesh collider.
                var collider = terrain.GetComponent<MeshCollider>();
                if (collider != null && collider.Raycast(new Ray(new Vector3(x, 10000f, z), Vector3.down), out var hit, 20000f))
                    y = hit.point.y;
                points[i] = new Vector3(x, y + surfaceOffset, z);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (points == null || points.Length < 2) return;
            Gizmos.color = Color.cyan;
            for (int i = 1; i < points.Length; i++)
                Gizmos.DrawLine(transform.TransformPoint(points[i - 1]), transform.TransformPoint(points[i]));
        }
    }
}
