using RiverRush.World;

static void Check(bool condition, string reason)
{
    if (!condition) throw new Exception(reason);
}

static float Center(float v, float size, float offset)
{
    return (float)Math.Sin(v * 7f + offset * 0.001f) * size * 0.12f +
           (float)Math.Sin(v * 17f + offset * 0.002f) * size * 0.035f;
}

static float Bed(float v) =>
    3f + 95f * 0.46f * (float)Math.Pow(1f - Math.Clamp(v, 0f, 1f), 1.4f);

int worlds = 0;
int tributaries = 0;
int points = 0;
bool observedVariation = false;
float previousSampleX = 0f;

foreach (float size in new[] { 260f, 500f, 950f })
foreach (float width in new[] { 8f, 12f, 18f })
for (int seed = -100; seed <= 200; seed++)
{
    float offsetX = new Random(seed).Next(-100000, 100000);
    Func<float, float> center = v => Center(v, size, offsetX);
    var branches = RiverBranchMath.Generate(seed, size, width, center);
    var repeated = RiverBranchMath.Generate(seed, size, width, center);
    Check(branches.Length == 5 && repeated.Length == 5, "Wrong tributary count");
    worlds++;

    float previousJoin = 0f;
    for (int i = 0; i < branches.Length; i++)
    {
        var branch = branches[i];
        var repeat = repeated[i];
        Check(branch.Points.Length == 18, "Unexpected spline resolution");
        Check(branch.Side == (i % 2 == 0 ? 1 : -1), "Missing alternating banks");
        Check(branch.JunctionV > previousJoin + 0.055f, "Overlapping confluences");
        Check(branch.JunctionV >= 0.16f && branch.JunctionV <= 0.87f,
            "Confluence outside playable river");
        Check(branch.Width >= 2.5f && branch.Width <= Math.Max(2.5f, width * 0.56f) + 0.001f,
            "Tributary too wide");
        previousJoin = branch.JunctionV;

        float lastZ = float.NegativeInfinity;
        float lastBed = float.PositiveInfinity;
        for (int j = 0; j < branch.Points.Length; j++)
        {
            var p = branch.Points[j];
            var q = repeat.Points[j];
            Check(float.IsFinite(p.X) && float.IsFinite(p.Z), "Non-finite coordinate");
            Check(p.X.Equals(q.X) && p.Z.Equals(q.Z), "Same seed different map");
            Check(Math.Abs(p.X) <= size * 0.46f + 0.001f, "Headwater outside map");
            Check(p.Z > lastZ, "Tributary has uphill / reversed Z segment");
            float normalized = p.Z / size + 0.5f;
            float bed = Bed(normalized);
            Check(bed <= lastBed + 0.0001f, "Tributary bed climbed uphill");
            lastZ = p.Z;
            lastBed = bed;
            points++;
        }

        var end = branch.Points[^1];
        Check(Math.Abs(end.X - center(branch.JunctionV)) < 0.0001f,
            "Tributary disconnected from main river");
        Check(Math.Abs(end.Z - (branch.JunctionV - 0.5f) * size) < 0.0001f,
            "Confluence Z mismatch");
        tributaries++;
    }
    if (worlds > 1 && Math.Abs(branches[0].Points[0].X - previousSampleX) > 0.05f)
        observedVariation = true;
    previousSampleX = branches[0].Points[0].X;
}

Check(observedVariation, "Different seeds produce the same world");
Console.WriteLine($"PASS: {worlds} seeded worlds, {tributaries} tributaries, " +
                  $"{points} sample points tested with real RiverBranchMath.cs");
Console.WriteLine("NOTE: Unity terrain meshes, shaders and Android FPS still require Unity/device tests.");
