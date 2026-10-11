using System;
using UnityEngine;
using RiverRush.Gameplay;

namespace RiverRush.Graphics
{
    /// <summary>
    /// Natural blue / translucent cyan racers at low population.
    /// When REAL human participants >= crowdThreshold, all racers get full-spectrum
    /// deterministic colors. Offline AI bots do not count as real people.
    /// Environments stay naturally blue regardless of number of players.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class RiverColorSystem : MonoBehaviour
    {
        [SerializeField, Min(2)] private int crowdThreshold = 8;
        [SerializeField, Min(1)] private int connectedHumanCount = 1;
        [SerializeField] private bool inspectorCrowdPreview = false;
        [SerializeField, Min(1)] private int previewHumanCount = 12;
        [SerializeField] private RiverRush.World.ProceduralTerrain terrain;
        [Header("Water tint")]
        [SerializeField, Range(0.3f, 1f)] private float dropletOpacity = 0.77f;

        private MaterialPropertyBlock properties;
        private int lastPaletteSeed = int.MinValue;
        public int ConnectedHumanCount => connectedHumanCount;
        public int CrowdThreshold => crowdThreshold;
        public bool FullRandomColors => (inspectorCrowdPreview ? previewHumanCount : connectedHumanCount)
                                        >= crowdThreshold;

        private void Start()
        {
            if (terrain == null) terrain = FindFirstObjectByType<RiverRush.World.ProceduralTerrain>();
            RefreshPalette();
        }

        /// <summary>Call from an authoritative multiplayer session as humans join/leave.</summary>
        public void SetConnectedHumanCount(int count)
        {
            int newCount = Mathf.Max(1, count);
            if (connectedHumanCount == newCount && !inspectorCrowdPreview) return;
            connectedHumanCount = newCount;
            RefreshPalette();
        }

        [ContextMenu("Refresh Water Palette")]
        public void RefreshPalette()
        {
            if (terrain == null) terrain = FindFirstObjectByType<RiverRush.World.ProceduralTerrain>();
            int seed = terrain != null ? terrain.Seed : 9182;
            lastPaletteSeed = seed;
            if (properties == null) properties = new MaterialPropertyBlock();

            // These shades never leave the natural blue/turquoise family.
            var rng = new System.Random(unchecked(seed ^ 0x37A4D1));
            float waterHue = 0.505f + (float)rng.NextDouble() * 0.065f;
            float seaHue = 0.54f + (float)rng.NextDouble() * 0.065f;
            Color shallow = Color.HSVToRGB(waterHue, 0.72f, 0.82f);
            Color deep = Color.HSVToRGB(Mathf.Clamp01(waterHue + 0.065f), 0.82f, 0.43f);
            Color ocean = Color.HSVToRGB(seaHue, 0.78f, 0.41f);
            Color oceanHighlight = Color.HSVToRGB(Mathf.Max(0f, seaHue - 0.04f), 0.56f, 0.77f);

            var rivers = FindObjectsByType<RiverRush.World.RiverWaterSurface>(FindObjectsSortMode.None);
            foreach (var water in rivers)
            {
                Renderer renderer = water.GetComponent<Renderer>();
                if (renderer == null) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_DeepColor", deep);
                properties.SetColor("_ShallowColor", shallow);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
            var oceanPhysics = FindFirstObjectByType<RiverRush.World.OceanWaterPhysics>();
            if (oceanPhysics != null)
            {
                var renderer = oceanPhysics.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor("_OceanColor", ocean);
                    properties.SetColor("_Highlight", oceanHighlight);
                    renderer.SetPropertyBlock(properties);
                    properties.Clear();
                }
            }

            var racers = FindObjectsByType<RiverRunner>(FindObjectsSortMode.None);
            // Sort for identical seed => identical visible colors regardless of Unity object iteration.
            Array.Sort(racers, (a, b) => string.CompareOrdinal(a.name, b.name));
            var colors = new System.Random(unchecked(seed ^ 0x5C7013));
            for (int i = 0; i < racers.Length; i++)
            {
                var renderer = racers[i].GetComponent<Renderer>();
                if (renderer == null) continue;
                Color tint;
                if (FullRandomColors)
                {
                    // More players: saturated red, purple, green, yellow etc. allowed.
                    tint = Color.HSVToRGB((float)colors.NextDouble(),
                        0.64f + 0.30f * (float)colors.NextDouble(),
                        0.85f + 0.15f * (float)colors.NextDouble());
                }
                else
                {
                    // Player starts brighter cyan; other streams get diverse NATURAL blues.
                    float hue = racers[i].IsHumanControlled
                        ? 0.515f + 0.025f * (float)colors.NextDouble()
                        : 0.49f + 0.11f * (float)colors.NextDouble();
                    tint = Color.HSVToRGB(hue, 0.46f + 0.30f * (float)colors.NextDouble(),
                                          0.78f + 0.21f * (float)colors.NextDouble());
                }
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseTint", tint);
                properties.SetFloat("_Opacity", racers[i].IsHumanControlled
                    ? dropletOpacity : Mathf.Clamp01(dropletOpacity - 0.05f));
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
            Debug.Log("River Rush palette: seed=" + lastPaletteSeed +
                      ", human players=" + connectedHumanCount +
                      ", mode=" + (FullRandomColors ? "Full random" : "Natural blue"));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            crowdThreshold = Mathf.Max(2, crowdThreshold);
            connectedHumanCount = Mathf.Max(1, connectedHumanCount);
            previewHumanCount = Mathf.Max(1, previewHumanCount);
        }
#endif
    }
}
