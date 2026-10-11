using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Fast 1-D hydraulic approximation along a sloping channel.
    /// A game flow model, not CFD/Navier-Stokes simulation.
    /// Distances and speeds are world-space metres / metres per second.
    /// </summary>
    [RequireComponent(typeof(RiverRoute))]
    public sealed class RiverFlowField : MonoBehaviour
    {
        [SerializeField] private RiverRoute route;
        [Header("Flow dynamics")]
        [SerializeField, Min(0.1f)] private float minimumSpeed = 2f;
        [SerializeField, Min(1f)] private float maximumSpeed = 23f;
        [SerializeField, Range(0.01f, 1f)] private float channelFriction = 0.18f;
        [SerializeField, Range(0f, 1f)] private float shoreDrag = 0.50f;
        [SerializeField, Range(0f, 2f)] private float eddyStrength = 0.35f;
        [SerializeField] private float waterDepth = 1.7f;

        public RiverRoute Route
        {
            get
            {
                if (route == null) route = GetComponent<RiverRoute>();
                return route;
            }
        }

        public float Depth => waterDepth;

        public bool TrySample(float meters, float lateral, out Vector3 velocity,
            out Vector3 surfacePosition, out float flowSpeed)
        {
            velocity = Vector3.zero;
            surfacePosition = Vector3.zero;
            flowSpeed = 0f;
            RiverRoute river = Route;
            if (river == null ||
                !river.Sample(meters, out Vector3 center, out Vector3 forward, out float slope))
                return false;

            float halfWidth = Mathf.Max(0.1f, river.Width * 0.5f);
            float shore = Mathf.Clamp01(Mathf.Abs(lateral) / halfWidth);
            float hydraulicSlope = Mathf.Max(0.008f, slope);
            // Energy balance: stronger gravity on steep gradients, friction loss along banks.
            float equilibrium = Mathf.Sqrt(2f * 9.81f * waterDepth * hydraulicSlope /
                                           Mathf.Max(0.01f, channelFriction));
            flowSpeed = Mathf.Clamp(equilibrium, minimumSpeed, maximumSpeed);
            flowSpeed *= 1f - shoreDrag * shore * shore;

            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.00001f) flatForward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            float eddy = Mathf.Sin(meters * 0.057f + shore * 9f + river.Terrain.Seed * 0.01f)
                         * eddyStrength * (0.25f + shore * 0.75f);
            velocity = forward * flowSpeed + right * eddy;
            surfacePosition = center + right * Mathf.Clamp(lateral, -halfWidth, halfWidth);
            return true;
        }

        public bool ClosestPoint(Vector3 worldPoint, out float meters, out float lateral,
            out Vector3 surfacePoint)
        {
            meters = 0f;
            lateral = 0f;
            surfacePoint = Vector3.zero;
            RiverRoute river = Route;
            var points = river != null ? river.Points : null;
            if (points == null || points.Count < 2) return false;

            float bestDistanceSquared = float.PositiveInfinity;
            float traveled = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = river.transform.TransformPoint(points[i - 1]);
                Vector3 b = river.transform.TransformPoint(points[i]);
                Vector3 ab = b - a;
                float segmentLengthSq = ab.sqrMagnitude;
                if (segmentLengthSq < 0.00001f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(worldPoint - a, ab) / segmentLengthSq);
                Vector3 nearest = a + ab * t;
                // Horizontal proximity should decide which river section is closest.
                Vector2 planar = new Vector2(worldPoint.x - nearest.x, worldPoint.z - nearest.z);
                float d2 = planar.sqrMagnitude;
                if (d2 < bestDistanceSquared)
                {
                    bestDistanceSquared = d2;
                    meters = traveled + Mathf.Sqrt(segmentLengthSq) * t;
                    Vector3 horizontal = Vector3.ProjectOnPlane(ab, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, horizontal).normalized;
                    lateral = Vector3.Dot(worldPoint - nearest, right);
                    surfacePoint = nearest + right * lateral;
                }
                traveled += Mathf.Sqrt(segmentLengthSq);
            }
            return bestDistanceSquared <= river.Width * river.Width * 0.25f;
        }
    }
}
