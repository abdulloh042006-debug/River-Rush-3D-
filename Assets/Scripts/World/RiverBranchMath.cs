using System;

namespace RiverRush.World
{
    /// <summary>
    /// Unity-independent deterministic tributary geometry.
    /// The same source file can be compiled by Unity and by GitHub .NET smoke tests.
    /// </summary>
    public static class RiverBranchMath
    {
        public struct Point
        {
            public readonly float X;
            public readonly float Z;
            public Point(float x, float z) { X = x; Z = z; }
        }

        public sealed class BranchData
        {
            public readonly Point[] Points;
            public readonly float JunctionV;
            public readonly float Width;
            public readonly int Side;

            public BranchData(Point[] points, float junctionV, float width, int side)
            {
                Points = points;
                JunctionV = junctionV;
                Width = width;
                Side = side;
            }
        }

        private static float Clamp(float value, float low, float high)
            => Math.Max(low, Math.Min(high, value));

        private static float Smooth(float value)
        {
            float t = Clamp(value, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        public static BranchData[] Generate(int seed, float worldSize,
            float mainRiverWidth, Func<float, float> mainCenter)
        {
            if (worldSize <= 0f) throw new ArgumentOutOfRangeException(nameof(worldSize));
            if (mainRiverWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(mainRiverWidth));
            if (mainCenter == null) throw new ArgumentNullException(nameof(mainCenter));

            var random = new Random(unchecked(seed ^ 0x45631B2D));
            const int branchCount = 5;
            const int pointsCount = 18;
            var branches = new BranchData[branchCount];

            for (int i = 0; i < branchCount; i++)
            {
                float joinV = Clamp(0.19f + i * 0.145f +
                    ((float)random.NextDouble() - 0.5f) * 0.055f, 0.16f, 0.87f);
                float startV = Math.Max(0.02f, joinV -
                    (0.14f + (float)random.NextDouble() * 0.10f));
                int side = i % 2 == 0 ? 1 : -1;
                var points = new Point[pointsCount];
                float displacement = worldSize * (0.17f + 0.045f *
                    (float)random.NextDouble());
                float wigglePhase = (float)random.NextDouble() * (float)Math.PI * 2f;

                for (int j = 0; j < pointsCount; j++)
                {
                    float t = j / (float)(pointsCount - 1);
                    float v = startV + (joinV - startV) * t;
                    float fade = 1f - Smooth(t);
                    float x = mainCenter(v) + side * displacement * fade;
                    x += (float)Math.Sin(t * 12f + wigglePhase) *
                         displacement * 0.09f * fade *
                         (float)Math.Sin(t * (float)Math.PI);
                    x = Clamp(x, -worldSize * 0.46f, worldSize * 0.46f);
                    points[j] = new Point(x, (v - 0.5f) * worldSize);
                }

                points[pointsCount - 1] =
                    new Point(mainCenter(joinV), (joinV - 0.5f) * worldSize);
                float width = Math.Max(2.5f, mainRiverWidth *
                    (0.44f + 0.12f * (float)random.NextDouble()));
                branches[i] = new BranchData(points, joinV, width, side);
            }
            return branches;
        }
    }
}
