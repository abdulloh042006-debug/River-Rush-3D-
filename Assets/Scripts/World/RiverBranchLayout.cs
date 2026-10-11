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
            var authored = RiverBranchMath.Generate(
                seed, worldSize, mainRiverWidth, mainCenter);
            var branches = new Branch[authored.Length];
            for (int i = 0; i < authored.Length; i++)
            {
                var from = authored[i];
                var coordinates = new Vector2[from.Points.Length];
                for (int j = 0; j < coordinates.Length; j++)
                    coordinates[j] = new Vector2(from.Points[j].X, from.Points[j].Z);
                branches[i] = new Branch(coordinates, from.JunctionV, from.Width, from.Side);
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
