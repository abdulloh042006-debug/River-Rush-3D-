using System.Collections.Generic;
using UnityEngine;

namespace RiverRush.World
{
    /// <summary>Samples the exact same seeded channel as ProceduralTerrain, in local space.</summary>
    public sealed class RiverRoute : MonoBehaviour
    {
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField, Min(8)] private int samples = 128;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.45f;

        private Vector3[] points;
        public IReadOnlyList<Vector3> Points => points;

        [ContextMenu("Rebuild Route")]
        public void Rebuild()
        {
            if (terrain == null) terrain = GetComponent<ProceduralTerrain>();
            if (terrain == null) terrain = FindFirstObjectByType<ProceduralTerrain>();
            if (terrain == null)
            {
                Debug.LogError("RiverRoute needs a ProceduralTerrain.", this);
                points = null;
                return;
            }

            samples = Mathf.Max(8, samples);
            points = new Vector3[samples];
            for (int i = 0; i < samples; i++)
            {
                float v = i / (float)(samples - 1);
                float x = terrain.RiverCenter(v);
                float z = (v - 0.5f) * terrain.WorldSize;
                float y = terrain.SampleHeight(x, z) + surfaceOffset;
                Vector3 world = terrain.transform.TransformPoint(new Vector3(x, y, z));
                points[i] = transform.InverseTransformPoint(world);
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
