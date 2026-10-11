using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Stable, low-cost, 1D damped shallow-water wave approximation.
    /// Physical displacements influence both render mesh and Rigidbody buoyancy.
    /// This models longitudinal surface waves, not full 3D volumetric water.
    /// </summary>
    [RequireComponent(typeof(RiverRoute))]
    public sealed class WaterWaveSimulator : MonoBehaviour
    {
        [SerializeField] private RiverRoute route;
        [SerializeField, Range(1f, 15f)] private float propagationSpeed = 6f;
        [SerializeField, Range(0.1f, 6f)] private float damping = 1.8f;
        [SerializeField, Range(0.05f, 2f)] private float maximumDisplacement = 0.65f;

        private float[] heights;
        private float[] velocities;
        private float[] nextVelocities;

        public int Count => heights != null ? heights.Length : 0;

        private void Awake() => Initialize();

        [ContextMenu("Initialize Waves")]
        public void Initialize()
        {
            if (route == null) route = GetComponent<RiverRoute>();
            if (route == null) return;
            if (route.Points == null || route.Points.Count < 2) route.Rebuild();
            if (route.Points == null || route.Points.Count < 3) return;
            int count = route.Points.Count;
            heights = new float[count];
            velocities = new float[count];
            nextVelocities = new float[count];
        }

        private void FixedUpdate()
        {
            if (heights == null || heights.Length < 3) return;
            float segment = route.TotalLength / Mathf.Max(1, heights.Length - 1);
            float spacing = Mathf.Max(0.2f, segment);
            // CFL stability: c*dt/dx <= 0.45. Substeps allow small river segments.
            int steps = Mathf.Clamp(Mathf.CeilToInt(
                propagationSpeed * Time.fixedDeltaTime / spacing / 0.45f), 1, 16);
            float dt = Time.fixedDeltaTime / steps;
            float c2 = propagationSpeed * propagationSpeed / (spacing * spacing);
            for (int step = 0; step < steps; step++)
            {
                nextVelocities[0] = 0f;
                nextVelocities[heights.Length - 1] = 0f;
                for (int i = 1; i < heights.Length - 1; i++)
                {
                    float curvature = heights[i - 1] - 2f * heights[i] + heights[i + 1];
                    nextVelocities[i] = velocities[i] +
                        (curvature * c2 - damping * velocities[i]) * dt;
                }
                for (int i = 1; i < heights.Length - 1; i++)
                {
                    velocities[i] = nextVelocities[i];
                    heights[i] = Mathf.Clamp(heights[i] + velocities[i] * dt,
                        -maximumDisplacement, maximumDisplacement);
                }
                heights[0] = heights[heights.Length - 1] = 0f;
            }
        }

        public void AddImpulse(float meters, float strength, float radiusMeters = 5f)
        {
            if (heights == null || route == null || route.TotalLength <= 0f) return;
            int count = heights.Length;
            float centerIndex = Mathf.Clamp01(meters / route.TotalLength) * (count - 1);
            float pointsPerMeter = (count - 1) / route.TotalLength;
            float radius = Mathf.Max(1f, radiusMeters * pointsPerMeter);
            int min = Mathf.Max(1, Mathf.FloorToInt(centerIndex - radius * 2f));
            int max = Mathf.Min(count - 2, Mathf.CeilToInt(centerIndex + radius * 2f));
            for (int i = min; i <= max; i++)
            {
                float x = (i - centerIndex) / radius;
                velocities[i] += strength * Mathf.Exp(-2.3f * x * x);
            }
        }

        public float GetHeight(float meters)
        {
            if (heights == null || heights.Length == 0 || route == null ||
                route.TotalLength <= 0f) return 0f;
            float index = Mathf.Clamp01(meters / route.TotalLength) * (heights.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(index), heights.Length - 2);
            return Mathf.Lerp(heights[i], heights[i + 1], index - i);
        }
    }
}
