using System;
using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Pure, seeded river tributary geometry. All paths run monotonically toward +Z
    /// and join the main channel. Shared by terrain carving and water rendering.
    /// </summary>
    public sealed class RiverBranchLayout
    {
        public sealed class Branch
        {
            public readonly Vector2[] Points;
            public readonly float JunctionV;
            public readonly float Width;
            public readonly int Side;

            public Branch(Vector2[] points, float junctionV, float width, int side)
            {
                Points = points;
                JunctionV = junctionV;
                Width = width;
                Side = side;
            }
        }

        public readonly Branch[] Branches;
        private RiverBranchLayout(Branch[] branches) { Branches = branches; }

        public static RiverBranchLayout Build(int seed, float worldSize,
            float mainRiverWidth, Func<float, float> mainCenter)
        {
            var random = new System.Random(unchecked(seed ^ 0x45631B2D));
            const int count = 5;
            var branches = new Branch[count];
            for (int i = 0; i < count; i++)
            {
                // Separated confluences avoid all pickups piling into the same spot.
                float joinV = 0.19f + i * 0.145f +
                    ((float)random.NextDouble() - 0.5f) * 0.055f;
                joinV = Mathf.Clamp(joinV, 0.16f, 0.87f);
                float startV = Mathf.Max(0.02f, joinV -
                    (0.14f + (float)random.NextDouble() * 0.10f));
                int side = i % 2 == 0 ? 1 : -1;
                // 18 samples keep carving costs predictable at 96x96 terrain vertices.
                const int pointsCount = 18;
                var points = new Vector2[pointsCount];
                float displacement = worldSize * (0.17f + 0.045f *
                    (float)random.NextDouble());
                float wigglePhase = (float)random.NextDouble() * Mathf.PI * 2f;
                for (int j = 0; j < pointsCount; j++)
                {
                    float t = j / (float)(pointsCount - 1);
                    float v = Mathf.Lerp(startV, joinV, t);
                    float fade = 1f - Mathf.SmoothStep(0f, 1f, t);
                    float x = mainCenter(v) + side * displacement * fade;
                    x += Mathf.Sin(t * 12f + wigglePhase) *
                         displacement * 0.09f * fade * Mathf.Sin(t * Mathf.PI);
                    // Never exceed the half-map boundary, even at sharp bends.
                    x = Mathf.Clamp(x, -worldSize * 0.46f, worldSize * 0.46f);
                    float z = (v - 0.5f) * worldSize;
                    points[j] = new Vector2(x, z);
                }
                // At the confluence the tributary joins the exact main centerline.
                points[pointsCount - 1] =
                    new Vector2(mainCenter(joinV), (joinV - 0.5f) * worldSize);
                branches[i] = new Branch(points, joinV,
                    Mathf.Max(2.5f, mainRiverWidth * (0.44f + 0.12f *
                    (float)random.NextDouble())), side);
            }
            return new RiverBranchLayout(branches);
        }

        public float DistanceToBranch(Vector2 point, Branch branch)
        {
            float best = float.PositiveInfinity;
            var vertices = branch.Points;
            if (point.y < vertices[0].y - branch.Width * 2f ||
                point.y > vertices[vertices.Length - 1].y + branch.Width * 2f)
                return best;

            for (int i = 1; i < vertices.Length; i++)
            {
                Vector2 a = vertices[i - 1];
                Vector2 b = vertices[i];
                Vector2 ab = b - a;
                float denominator = ab.sqrMagnitude;
                if (denominator < 0.00001f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denominator);
                float distanceSquared = (point - (a + ab * t)).sqrMagnitude;
                if (distanceSquared < best) best = distanceSquared;
            }
            return Mathf.Sqrt(best);
        }

        public float DistanceToNearestBranch(Vector2 point)
        {
            float nearest = float.PositiveInfinity;
            foreach (var branch in Branches)
                nearest = Mathf.Min(nearest, DistanceToBranch(point, branch));
            return nearest;
        }

        public bool Verify(out string reason)
        {
            foreach (var branch in Branches)
            {
                if (branch.Points == null || branch.Points.Length < 2)
                {
                    reason = "Empty tributary.";
                    return false;
                }
                for (int i = 1; i < branch.Points.Length; i++)
                {
                    if (branch.Points[i].y <= branch.Points[i - 1].y)
                    {
                        reason = "A tributary moves uphill in Z.";
                        return false;
                    }
                }
            }
            reason = null;
            return true;
        }
    }
}
