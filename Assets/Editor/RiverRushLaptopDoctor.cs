#if UNITY_EDITOR
using System.Text;
using RiverRush.Graphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RiverRush.Editor
{
    /// <summary>Read-only machine and URP project readiness report, no changes to project settings.</summary>
    public static class RiverRushLaptopDoctor
    {
        [MenuItem("River Rush/Diagnostics/Check Laptop and URP")]
        public static void CheckLaptopAndProject()
        {
            int ram = SystemInfo.systemMemorySize;
            int graphics = SystemInfo.graphicsMemorySize;
            string gpuName = SystemInfo.graphicsDeviceName;
            int recommended = HardwareQualityPolicy.ChooseDesktopLevel(ram, graphics, gpuName);

            var report = new StringBuilder(1200);
            report.AppendLine("RIVER RUSH 3D — READ-ONLY UNITY READINESS REPORT");
            report.AppendLine("Unity: " + Application.unityVersion);
            report.AppendLine("OS: " + SystemInfo.operatingSystem);
            report.AppendLine("CPU: " + SystemInfo.processorType);
            report.AppendLine("System RAM: " + (ram / 1024f).ToString("F1") + " GiB");
            report.AppendLine("Detected GPU: " + gpuName);
            report.AppendLine("Reported graphics memory: " + graphics +
                              " MB (may be shared or inaccurate on integrated GPUs)");
            report.AppendLine("Starting graphics: " +
                              (recommended == 3 ? "Ultra" : recommended == 2 ? "High" :
                               recommended == 1 ? "Balanced" : "BatterySaver"));
            report.AppendLine("Reason: " +
                HardwareQualityPolicy.Reason(ram, graphics, gpuName));

            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            bool urpReady = urp != null;
            report.AppendLine("URP active: " + (urpReady ? "YES" : "NO"));
            int missingShaders = 0;
            string[] shaderNames = {
                "RiverRush/StylizedRiverURP", "RiverRush/StylizedOceanURP",
                "RiverRush/WaterDropletURP", "RiverRush/TerrainBiomeURP",
                "RiverRush/NatureVertexColorURP"
            };
            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                bool ok = shader != null && shader.isSupported;
                if (!ok) missingShaders++;
                report.AppendLine(shaderName + ": " + (ok ? "OK" : "NOT READY"));
            }

            report.AppendLine();
            report.AppendLine("IMPORTANT: This does NOT benchmark FPS or validate gameplay.");
            report.AppendLine("Next: River Rush > Create Prototype Scene, then " +
                              "Validate Prototype Geometry and run the 60s benchmark.");
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog(
                "River Rush — Laptop / URP",
                "Recommended starting quality: " +
                (recommended == 3 ? "Ultra" : recommended == 2 ? "High" : "Balanced") +
                "\nURP: " + (urpReady ? "OK" : "Missing") +
                "\nCustom shaders missing/unsupported: " + missingShaders +
                "\nFull report is in the Unity Console.", "OK");
        }
    }
}
#endif
