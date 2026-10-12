using RiverRush.Graphics;

internal static class HardwareQualityTests
{
    public static void Run()
    {
        // 8 GB system RAM remains conservative even when GPU name is strong.
        Assert(8192, 8192, "NVIDIA GeForce RTX 4070", 1);
        // 32 GB RAM with shared-memory integrated graphics is NOT an Ultra GPU.
        Assert(32768, 16384, "AMD Radeon 780M Graphics", 1);
        Assert(32768, 16384, "Intel(R) Iris(R) Xe Graphics", 1);
        Assert(32768, 8192, "Intel UHD Graphics 770", 1);
        // Discrete hardware can offer higher graphics quality.
        Assert(32768, 8192, "NVIDIA GeForce RTX 4060 Laptop GPU", 3);
        Assert(32768, 12288, "AMD Radeon RX 7700S", 3);
        Assert(32768, 4096, "NVIDIA GeForce RTX 3050 Laptop GPU", 2);
        Assert(32768, 2048, "NVIDIA GeForce MX450", 1);
        Assert(16384, 4096, "Intel Arc A370M", 2);
        // Unrecognized hardware has no reason to begin at Ultra.
        Assert(32768, 0, "Generic GPU Adapter", 1);
        Assert(32768, 0, "", 1);

        if (!HardwareQualityPolicy.IsLikelyIntegrated("AMD Radeon(TM) Graphics"))
            throw new Exception("Shared-memory Radeon detection failed");
        if (!HardwareQualityPolicy.IsLikelyDiscrete("NVIDIA GeForce RTX 4060"))
            throw new Exception("Discrete NVIDIA detection failed");
        Console.WriteLine("PASS: 13 GPU/RAM quality policy assertions.");
    }

    private static void Assert(int ramMb, int vramMb, string gpuName, int expected)
    {
        int actual = HardwareQualityPolicy.ChooseDesktopLevel(ramMb, vramMb, gpuName);
        if (actual != expected)
            throw new Exception($"For GPU '{gpuName}' RAM={ramMb} VRAM={vramMb}: expected quality {expected}, got {actual}");
    }
}
