using System.Collections.Generic;
using UnityEngine;

namespace RiverRush.World
{
    /// <summary>Physical-distance sampled centerline. Coordinates are stored in route local space.</summary>
    public sealed class RiverRoute : MonoBehaviour
    {
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField, Range(32, 1024)] private int samples = 256;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.55f;

        private Vector3[] points;
        private float[] distances;
        public IReadOnlyList<Vector3> Points => points;
        public float TotalLength => distances != null && distances.Length > 0 ? distances[distances.Length - 1] : 0f;
        public float Width => terrain != null ? terrain.RiverWidth : 10f;
        public ProceduralTerrain Terrain => terrain;

        [ContextMenu("Rebuild Route")]
        public void Rebuild()
        {
            if (terrain == null) terrain = GetComponent<ProceduralTerrain>();
            if (terrain == null) terrain = FindFirstObjectByType<ProceduralTerrain>();
            if (terrain == null)
            {
                Debug.LogError("RiverRoute needs ProceduralTerrain.", this);
                points = null;
                distances = null;
                return;
            }

            int count = Mathf.Clamp(samples, 32, 1024);
            points = new Vector3[count];
            distances = new float[count];
            for (int i = 0; i < count; i++)
            {
                float v = i / (float)(count - 1);
                float x = terrain.RiverCenter(v);
                float z = (v - 0.5f) * terrain.WorldSize;
                float y = terrain.SampleHeight(x, z) + surfaceOffset;
                Vector3 world = terrain.transform.TransformPoint(new Vector3(x, y, z));
                points[i] = transform.InverseTransformPoint(world);
                if (i > 0)
                    distances[i] = distances[i - 1] + Vector3.Distance(
                        transform.TransformPoint(points[i - 1]), world);
            }
        }

        /// <summary>Distance in metres; returns center/tangent in world coordinates and vertical descent ratio.</summary>
        public bool Sample(float distance, out Vector3 center, out Vector3 forward, out float slope)
        {
            center = Vector3.zero;
            forward = Vector3.forward;
            slope = 0f;
            if (points == null || points.Length < 2 || distances == null) return false;

            float d = Mathf.Clamp(distance, 0f, TotalLength);
            int low = 0;
            int high = distances.Length - 1;
            while (high - low > 1)
            {
                int mid = (low + high) / 2;
                if (distances[mid] <= d) low = mid;
                else high = mid;
            }

            Vector3 from = transform.TransformPoint(points[low]);
            Vector3 to = transform.TransformPoint(points[low + 1]);
            float section = Mathf.Max(0.00001f, distances[low + 1] - distances[low]);
            center = Vector3.Lerp(from, to, (d - distances[low]) / section);
            Vector3 delta = to - from;
            forward = delta.sqrMagnitude > 0.00001f ? delta.normalized : transform.forward;
            slope = Mathf.Clamp01((from.y - to.y) / section);
            return true;
        }

        public bool SampleBank(float distance, float lateral, out Vector3 position)
        {
            position = Vector3.zero;
            if (!Sample(distance, out Vector3 center, out Vector3 forward, out _)) return false;
            Vector3 horizontal = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (horizontal.sqrMagnitude < 0.001f) horizontal = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, horizontal).normalized;
            position = center + right * Mathf.Clamp(lateral, -Width * 0.5f, Width * 0.5f);
            return true;
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
