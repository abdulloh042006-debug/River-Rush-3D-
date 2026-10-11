using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RiverRush.World;

namespace RiverRush.Graphics
{
    public enum GraphicsPreset { Auto, BatterySaver, Balanced, High, Ultra }
    public enum FrameCap { Auto, FPS30, FPS60, FPS90, FPS120, Native }

    /// <summary>
    /// Platform-aware graphics and frame pacing. Supports optional auto down/up-scaling
    /// with cooldown to avoid quality oscillation. Uses only Unity + URP APIs.
    /// </summary>
    public sealed class AdaptiveQualityController : MonoBehaviour
    {
        [SerializeField] private GraphicsPreset preset = GraphicsPreset.Auto;
        [SerializeField] private FrameCap frameCap = FrameCap.Auto;
        [SerializeField] private bool showFps = true;
        [SerializeField, Range(2f, 15f)] private float evaluateEvery = 6f;
        [SerializeField, Range(0f, 20f)] private float adjustmentCooldown = 12f;

        private readonly float[] renderScales = { 0.70f, 0.82f, 1.0f, 1.12f };
        private readonly float[] shadowDistances = { 12f, 30f, 55f, 85f };
        private readonly float[] waterDetails = { 0.12f, 0.40f, 0.72f, 1.0f };
        private readonly float[] lodBiases = { 0.6f, 0.85f, 1.2f, 1.65f };
        private UniversalRenderPipelineAsset urp;
        private float originalRenderScale, originalShadowDistance, originalLodBias;
        private int originalVsync, originalFrameRate;
        private int level;
        private float smoothedFps = 60f;
        private float evaluationTime, lastAdjustmentTime, fpsSum;
        private int fpsSamples, healthyIntervals, slowIntervals;
        private bool showMenu;

        public float CurrentFPS => smoothedFps;
        public GraphicsPreset SelectedPreset => preset;
        public FrameCap SelectedFrameCap => frameCap;
        public int EffectiveQualityLevel => level;

        private void Awake()
        {
            originalVsync = QualitySettings.vSyncCount;
            originalFrameRate = Application.targetFrameRate;
            originalLodBias = QualitySettings.lodBias;
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                originalRenderScale = urp.renderScale;
                originalShadowDistance = urp.shadowDistance;
            }
            preset = (GraphicsPreset)Mathf.Clamp(PlayerPrefs.GetInt("RR_GraphicsPreset", (int)preset), 0, 4);
            frameCap = (FrameCap)Mathf.Clamp(PlayerPrefs.GetInt("RR_FrameCap", (int)frameCap), 0, 5);
            level = InitialLevel();
            ApplySettings();
        }

        private int InitialLevel()
        {
            if (preset != GraphicsPreset.Auto) return (int)preset - 1;
            if (Application.isMobilePlatform)
            {
                int graphicsMB = SystemInfo.graphicsMemorySize;
                int systemMB = SystemInfo.systemMemorySize;
                // Some Android drivers return unknown (0) for dedicated GPU memory.
                if (systemMB >= 7500 && graphicsMB >= 3000) return 3;
                if (systemMB >= 5500 && graphicsMB >= 1500) return 2;
                if (systemMB >= 4500 || graphicsMB >= 1200) return 1;
                return 0;
            }
            return SystemInfo.graphicsMemorySize >= 6000 ? 3 : 2;
        }

        private int DisplayRefreshRate()
        {
            int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            return refresh >= 24 ? refresh : 60;
        }

        private int TargetFPS()
        {
            int request = frameCap == FrameCap.Native ? DisplayRefreshRate() : frameCap == FrameCap.Auto
                ? (level == 0 ? 30 : level == 1 ? 60 : level == 2 ? 90 : 120)
                : (int)frameCap == 1 ? 30 : (int)frameCap == 2 ? 60
                : (int)frameCap == 3 ? 90 : 120;
            int refresh = DisplayRefreshRate();
            if (request >= refresh) return refresh;
            // Mobile OS may round requested FPS to a divisor of the physical refresh.
            // Choose closest divisor at or below the requested cap.
            if (Application.isMobilePlatform)
            {
                int best = 1;
                for (int divisor = 1; divisor <= refresh; divisor++)
                {
                    if (refresh % divisor == 0 && divisor <= request) best = divisor;
                }
                return Mathf.Max(1, best);
            }
            return request;
        }

        private void ApplySettings()
        {
            level = Mathf.Clamp(level, 0, 3);
            if (urp != null)
            {
                urp.renderScale = renderScales[level];
                urp.shadowDistance = shadowDistances[level];
            }
            QualitySettings.lodBias = lodBiases[level];
            // Mobile ignores vSyncCount; on desktop prefer hardware pacing at native rate.
            int target = TargetFPS();
            QualitySettings.vSyncCount = !Application.isMobilePlatform &&
                target >= DisplayRefreshRate() ? 1 : 0;
            Application.targetFrameRate = target;
            foreach (var water in FindObjectsByType<RiverWaterSurface>(FindObjectsSortMode.None))
                water.SetDetail(waterDetails[level]);
            Debug.Log("River Rush quality: " + (level + 1) +
                      "/4, target FPS " + target + ", measured ~" + smoothedFps.ToString("F0"));
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.0001f && dt < 0.25f)
            {
                smoothedFps = Mathf.Lerp(smoothedFps, 1f / dt, 0.06f);
                fpsSum += dt;
                fpsSamples++;
            }

            if (Input.GetKeyDown(KeyCode.F3)) showFps = !showFps;
            if (Input.GetKeyDown(KeyCode.F4)) showMenu = !showMenu;
            if (preset != GraphicsPreset.Auto || !Application.isFocused) return;

            evaluationTime += dt;
            if (evaluationTime < evaluateEvery) return;
            evaluationTime = 0f;
            if (fpsSamples == 0) return;
            float observed = fpsSamples / Mathf.Max(0.01f, fpsSum);
            fpsSum = 0f;
            fpsSamples = 0;
            int target = TargetFPS();
            bool slow = observed < target * 0.78f;
            bool healthy = observed >= target * 0.95f;

            slowIntervals = slow ? slowIntervals + 1 : 0;
            healthyIntervals = healthy ? healthyIntervals + 1 : 0;
            if (Time.unscaledTime - lastAdjustmentTime < adjustmentCooldown) return;

            if (slowIntervals >= 2 && level > 0)
            {
                level--;
                slowIntervals = healthyIntervals = 0;
                lastAdjustmentTime = Time.unscaledTime;
                ApplySettings();
            }
            else if (healthyIntervals >= 5 && level < InitialLevel())
            {
                level++;
                slowIntervals = healthyIntervals = 0;
                lastAdjustmentTime = Time.unscaledTime;
                ApplySettings();
            }
        }

        public void SelectGraphics(GraphicsPreset selection)
        {
            preset = selection;
            PlayerPrefs.SetInt("RR_GraphicsPreset", (int)preset);
            PlayerPrefs.Save();
            level = InitialLevel();
            ResetEvaluation();
            ApplySettings();
        }

        public void SelectFrameCap(FrameCap selection)
        {
            frameCap = selection;
            PlayerPrefs.SetInt("RR_FrameCap", (int)frameCap);
            PlayerPrefs.Save();
            ResetEvaluation();
            ApplySettings();
        }

        private void ResetEvaluation()
        {
            fpsSamples = slowIntervals = healthyIntervals = 0;
            fpsSum = evaluationTime = 0f;
            lastAdjustmentTime = Time.unscaledTime;
        }

        private void OnGUI()
        {
            if (!showFps && !showMenu) return;
            float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 900f, Screen.height / 500f));
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            if (showFps)
                GUI.Box(new Rect(8, 8, 180, 32),
                    "FPS " + Mathf.RoundToInt(smoothedFps) + " / " + TargetFPS() +
                    "  Q" + (level + 1));
            if (GUI.Button(new Rect(w - 110, 8, 100, 38), "Graphics"))
                showMenu = !showMenu;

            if (showMenu)
            {
                GUI.Box(new Rect(w - 275, 55, 265, 345), "Graphics & FPS");
                int y = 89;
                foreach (GraphicsPreset choice in System.Enum.GetValues(typeof(GraphicsPreset)))
                {
                    if (GUI.Button(new Rect(w - 265, y, 250, 31),
                        (choice == preset ? "● " : "   ") + choice))
                        SelectGraphics(choice);
                    y += 35;
                }
                y += 5;
                foreach (FrameCap cap in System.Enum.GetValues(typeof(FrameCap)))
                {
                    if (GUI.Button(new Rect(w - 265 + ((int)cap % 3) * 82,
                        y + ((int)cap / 3) * 38, 78, 34),
                        (cap == frameCap ? "●" : "") + (cap == FrameCap.Auto
                        ? "Auto" : ((int)cap == 1 ? "30" : (int)cap == 2 ? "60" :
                        (int)cap == 3 ? "90" : (int)cap == 4 ? "120" : "Native"))))
                        SelectFrameCap(cap);
                }
            }
            GUI.matrix = oldMatrix;
        }

        private void OnDestroy()
        {
            if (urp != null)
            {
                urp.renderScale = originalRenderScale;
                urp.shadowDistance = originalShadowDistance;
            }
            QualitySettings.lodBias = originalLodBias;
            QualitySettings.vSyncCount = originalVsync;
            Application.targetFrameRate = originalFrameRate;
        }
    }
}
