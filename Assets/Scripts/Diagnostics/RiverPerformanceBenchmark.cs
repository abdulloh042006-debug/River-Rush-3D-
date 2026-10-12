using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using RiverRush.World;
using RiverRush.Graphics;

namespace RiverRush.Diagnostics
{
    /// <summary>
    /// On-device performance sample for real Android/Windows benchmarks.
    /// No network, telemetry uploads, analytics SDK or personal identifiers.
    /// Saves CSV only to the game's local persistent data directory.
    /// </summary>
    public sealed class RiverPerformanceBenchmark : MonoBehaviour
    {
        [SerializeField, Range(10f, 180f)] private float benchmarkSeconds = 60f;
        [SerializeField] private bool showBenchmarkControls = true;
        [SerializeField] private int maximumSamples = 30000;

        private float[] frameTimes;
        private int sampleCount;
        private float totalTime;
        private float startTime;
        private bool recording;
        private bool invalidated;
        private string lastSummary = "No benchmark yet";
        private string lastReport = "";
        private AdaptiveQualityController quality;
        private ProceduralTerrain terrain;

        public bool IsRecording => recording;
        public string LastSummary => lastSummary;

        private void Awake()
        {
            quality = FindFirstObjectByType<AdaptiveQualityController>();
            terrain = FindFirstObjectByType<ProceduralTerrain>();
        }

        public void StartBenchmark()
        {
            frameTimes = new float[Mathf.Max(600, maximumSamples)];
            sampleCount = 0;
            totalTime = 0f;
            startTime = Time.unscaledTime;
            invalidated = false;
            recording = true;
            lastSummary = "Benchmark running...";
            lastReport = "";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5) && !recording)
                StartBenchmark();

            if (!recording) return;
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.25f)
                invalidated = true; // Pause/loading spike makes results incomparable.
            if (dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt))
            {
                if (sampleCount < frameTimes.Length)
                {
                    frameTimes[sampleCount++] = dt;
                    totalTime += dt;
                }
                else
                {
                    Complete();
                    return;
                }
            }
            if (Time.unscaledTime - startTime >= benchmarkSeconds)
                Complete();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && recording) invalidated = true;
        }

        private void Complete()
        {
            recording = false;
            if (sampleCount == 0 || totalTime <= 0f)
            {
                lastSummary = "Benchmark failed: no frames.";
                return;
            }

            Array.Sort(frameTimes, 0, sampleCount); // ascending dt
            int worstCount = Mathf.Max(1, Mathf.CeilToInt(sampleCount * 0.01f));
            double worstSum = 0;
            for (int i = sampleCount - worstCount; i < sampleCount; i++)
                worstSum += frameTimes[i];
            double avg = sampleCount / (double)totalTime;
            double low = worstCount / worstSum; // 1% low FPS based on worst frames
            double worstMs = frameTimes[sampleCount - 1] * 1000d;
            double ramMB = Profiler.GetTotalAllocatedMemoryLong() / (1024d * 1024d);
            string status = invalidated ? "INVALID_PAUSED_OR_STALLED" : "VALID_SAMPLE";
            string preset = quality == null ? "unknown" : quality.SelectedPreset.ToString();
            string cap = quality == null ? "unknown" : quality.SelectedFrameCap.ToString();
            int seed = terrain != null ? terrain.Seed : 0;

            lastSummary = status + "\nAvg " + avg.ToString("F1", CultureInfo.InvariantCulture) +
                          " | 1% low " + low.ToString("F1", CultureInfo.InvariantCulture) +
                          "\nWorst " + worstMs.ToString("F1", CultureInfo.InvariantCulture) +
                          " ms | RAM " + ramMB.ToString("F0", CultureInfo.InvariantCulture) + " MB";

            try
            {
                string path = Path.Combine(Application.persistentDataPath,
                    "RiverRush_Benchmark_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss",
                        CultureInfo.InvariantCulture) + ".csv");
                string header = "utc,status,platform,device,seed,width,height," +
                    "graphics,frame_cap,samples,seconds,avg_fps,one_percent_low_fps," +
                    "worst_frame_ms,unity_allocated_mb";
                string line = string.Join(",", new string[] {
                    Csv(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
                    Csv(status), Csv(Application.platform.ToString()),
                    Csv(SystemInfo.deviceModel), Csv(seed.ToString(CultureInfo.InvariantCulture)),
                    Csv(Screen.width.ToString(CultureInfo.InvariantCulture)),
                    Csv(Screen.height.ToString(CultureInfo.InvariantCulture)),
                    Csv(preset), Csv(cap),
                    Csv(sampleCount.ToString(CultureInfo.InvariantCulture)),
                    Csv(totalTime.ToString("F3", CultureInfo.InvariantCulture)),
                    Csv(avg.ToString("F3", CultureInfo.InvariantCulture)),
                    Csv(low.ToString("F3", CultureInfo.InvariantCulture)),
                    Csv(worstMs.ToString("F3", CultureInfo.InvariantCulture)),
                    Csv(ramMB.ToString("F2", CultureInfo.InvariantCulture))
                });
                File.WriteAllText(path, header + "\n" + line + "\n");
                lastReport = path;
                Debug.Log("River Rush FPS benchmark saved locally: " + lastReport +
                          "\n" + lastSummary);
            }
            catch (Exception exception)
            {
                lastReport = "";
                Debug.LogWarning("River Rush benchmark couldn't save the local CSV: " +
                                 exception.Message);
            }
        }

        private static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void OnGUI()
        {
            if (!showBenchmarkControls) return;
            float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 900f,
                                                   Screen.height / 500f));
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            if (GUI.Button(new Rect(w - 120, 51, 110, 33),
                recording ? "FPS: Running" : "Benchmark") && !recording)
                StartBenchmark();

            if (recording)
            {
                GUI.Box(new Rect(w - 245, 89, 235, 52),
                    "Measuring FPS...\n" +
                    Mathf.Clamp(benchmarkSeconds - (Time.unscaledTime - startTime),
                        0f, benchmarkSeconds).ToString("F0") + "s remaining");
            }
            else if (lastReport.Length > 0)
            {
                GUI.Box(new Rect(8, h - 140, 285, 130), lastSummary);
            }
            GUI.matrix = previous;
        }
    }
}
