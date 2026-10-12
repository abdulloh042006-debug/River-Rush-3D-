using System;

namespace RiverRush.Graphics
{
    /// <summary>
    /// Conservative hardware initialization, independent of Unity.
    /// Measured runtime FPS (not RAM alone) determines whether a laptop can run Ultra.
    /// Quality levels: 0=BatterySaver, 1=Balanced, 2=High, 3=Ultra.
    /// </summary>
    public static class HardwareQualityPolicy
    {
        public static bool IsLikelyIntegrated(string deviceName)
        {
            string name = (deviceName ?? "").ToLowerInvariant();
            if (name.Length == 0) return false;

            if (name.Contains("iris") || name.Contains("uhd graphics") ||
                name.Contains("hd graphics") || name.Contains("vega") ||
                name.Contains("radeon 780m") || name.Contains("radeon 760m") ||
                name.Contains("radeon 680m") || name.Contains("radeon 660m") ||
                name.Contains("radeon 890m") || name.Contains("radeon 880m") ||
                name.Contains("radeon(tm) graphics") || name.Contains("amd radeon graphics") ||
                name.Contains("intel(r) graphics") || name.Contains("intel graphics"))
                return true;
            // Other integrated GPUs may be named only "Radeon Graphics".
            if (name.Contains("radeon graphics") && !name.Contains("radeon rx"))
                return true;
            // Intel Arc Graphics without A-series / B-series suffix can be integrated.
            if (name.Contains("intel") && name.Contains("arc graphics") &&
                !name.Contains("arc a") && !name.Contains("arc b"))
                return true;
            return false;
        }

        public static bool IsLikelyDiscrete(string deviceName)
        {
            string name = (deviceName ?? "").ToLowerInvariant();
            return name.Contains("nvidia") || name.Contains("geforce") ||
                   name.Contains("quadro") || name.Contains("radeon rx") ||
                   name.Contains("radeon pro") ||
                   (name.Contains("intel arc") &&
                    (name.Contains("arc a") || name.Contains("arc b")));
        }

        public static int ChooseDesktopLevel(int systemMemoryMb, int graphicsMemoryMb,
                                              string deviceName)
        {
            if (systemMemoryMb > 0 && systemMemoryMb <= 9000) return 1;
            if (IsLikelyIntegrated(deviceName)) return 1;

            if (IsLikelyDiscrete(deviceName))
            {
                if (systemMemoryMb >= 24000 && graphicsMemoryMb >= 7000) return 3;
                if (graphicsMemoryMb >= 3500) return 2;
                return 1;
            }

            // Missing/ambiguous GPU data is not evidence of a powerful video card.
            if (graphicsMemoryMb <= 0 || graphicsMemoryMb <= 2000) return 1;
            return 1;
        }

        public static string Reason(int systemMemoryMb, int graphicsMemoryMb,
                                     string deviceName)
        {
            if (systemMemoryMb > 0 && systemMemoryMb <= 9000)
                return "Low system RAM: Balanced to reduce memory pressure";
            if (IsLikelyIntegrated(deviceName))
                return "Integrated/shared graphics: start Balanced even with 32 GB RAM";
            if (IsLikelyDiscrete(deviceName) &&
                systemMemoryMb >= 24000 && graphicsMemoryMb >= 7000)
                return "Powerful dedicated GPU and ample RAM: Ultra candidate";
            if (IsLikelyDiscrete(deviceName) && graphicsMemoryMb >= 3500)
                return "Dedicated GPU: start High, then measure FPS";
            return "GPU class unclear: conservative Balanced start";
        }
    }
}
