using System;
using UnityEngine;
using RiverRush.Gameplay;
using RiverRush.World;

namespace RiverRush.Graphics
{
    /// <summary>
    /// Real humans (not bots) control when we expand from naturally translucent blue
    /// to every hue. Racers share a small palette of materials for URP batching.
    /// Call SetConnectedHumanCount from the multiplayer authority when it exists.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class RiverColorSystem : MonoBehaviour
    {
        [SerializeField, Min(2)] private int crowdThreshold = 8;
        [SerializeField, Min(1)] private int connectedHumanCount = 1;
        [SerializeField] private bool inspectorCrowdPreview;
        [SerializeField, Min(1)] private int previewHumanCount = 12;
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField, Range(0.3f, 1f)] private float dropletOpacity = 0.77f;

        private const int NaturalVariants = 12;
        private const int CrowdVariants = 48;
        private Material[] palette;
        private MaterialPropertyBlock environmentProperties;
        private int activeSeed = int.MinValue;
        private bool activeCrowdMode;

        public int ConnectedHumanCount => connectedHumanCount;
        public int CrowdThreshold => crowdThreshold;
        public bool FullRandomColors =>
            (inspectorCrowdPreview ? previewHumanCount : connectedHumanCount) >= crowdThreshold;

        private void Start()
        {
            if (terrain == null) terrain = FindFirstObjectByType<ProceduralTerrain>();
            RefreshPalette();
        }

        public void SetConnectedHumanCount(int count)
        {
            int corrected = Mathf.Max(1, count);
            if (connectedHumanCount == corrected && !inspectorCrowdPreview) return;
            connectedHumanCount = corrected;
            RefreshPalette();
        }

        [ContextMenu("Refresh Water Palette")]
        public void RefreshPalette()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("River Rush: palette previews are applied in Play Mode, not while editing the scene.");
                return;
            }
            if (terrain == null) terrain = FindFirstObjectByType<ProceduralTerrain>();
            int seed = terrain != null ? terrain.Seed : 9182;
            if (environmentProperties == null)
                environmentProperties = new MaterialPropertyBlock();

            // Mountains, river and ocean always retain a natural water color.
            var waterRng = new System.Random(unchecked(seed ^ 0x37A4D1));
            float hue = 0.505f + 0.065f * (float)waterRng.NextDouble();
            float seaHue = 0.54f + 0.065f * (float)waterRng.NextDouble();
            Color shallow = Color.HSVToRGB(hue, 0.72f, 0.82f);
            Color deep = Color.HSVToRGB(Mathf.Clamp01(hue + 0.065f), 0.82f, 0.43f);
            Color ocean = Color.HSVToRGB(seaHue, 0.78f, 0.41f);
            Color shine = Color.HSVToRGB(Mathf.Max(0f, seaHue - 0.04f), 0.56f, 0.77f);

            foreach (var water in FindObjectsByType<RiverWaterSurface>(FindObjectsSortMode.None))
            {
                Renderer renderer = water.GetComponent<Renderer>();
                if (renderer == null) continue;
                renderer.GetPropertyBlock(environmentProperties);
                environmentProperties.SetColor("_DeepColor", deep);
                environmentProperties.SetColor("_ShallowColor", shallow);
                renderer.SetPropertyBlock(environmentProperties);
                environmentProperties.Clear();
            }
            var sea = FindFirstObjectByType<OceanWaterPhysics>();
            if (sea != null)
            {
                Renderer renderer = sea.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(environmentProperties);
                    environmentProperties.SetColor("_OceanColor", ocean);
                    environmentProperties.SetColor("_Highlight", shine);
                    renderer.SetPropertyBlock(environmentProperties);
                    environmentProperties.Clear();
                }
            }

            EnsurePalette(seed);
            var racers = FindObjectsByType<RiverRunner>(FindObjectsSortMode.None);
            Array.Sort(racers, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (palette != null)
            {
                // Full-spectrum colors cover the hue wheel. Seed changes their
                // offset on every race; the pool is shared rather than one material
                // allocation per human, preserving batching at large player counts.
                for (int i = 0; i < racers.Length; i++)
                {
                    var renderer = racers[i].GetComponent<Renderer>();
                    if (renderer == null) continue;
                    int index = racers[i].IsHumanControlled && !FullRandomColors
                        ? 0 : (i + (FullRandomColors ? 0 : 1)) % palette.Length;
                    renderer.SetPropertyBlock(null); // clear old overrides on racer only
                    renderer.sharedMaterial = palette[index];
                }
            }
            Debug.Log("River Rush colors: seed " + seed + ", real people " +
                      connectedHumanCount + ", mode " +
                      (FullRandomColors ? "Full random" : "Natural blue") +
                      ", shared variants " + (palette == null ? 0 : palette.Length));
        }

        private void EnsurePalette(int seed)
        {
            bool crowd = FullRandomColors;
            if (palette != null && activeSeed == seed && activeCrowdMode == crowd) return;

            DisposePalette();
            Shader shader = Shader.Find("RiverRush/WaterDropletURP");
            if (shader == null)
            {
                Debug.LogError("Missing shader RiverRush/WaterDropletURP; colors cannot update.", this);
                return;
            }

            activeSeed = seed;
            activeCrowdMode = crowd;
            int count = crowd ? CrowdVariants : NaturalVariants;
            palette = new Material[count];
            var rng = new System.Random(unchecked(seed ^ 0x5C7013));
            float crowdOffset = (float)rng.NextDouble();
            for (int i = 0; i < count; i++)
            {
                float selectedHue = crowd
                    ? Mathf.Repeat(crowdOffset + i * 0.61803399f, 1f)
                    : (i == 0 ? 0.515f : 0.49f + 0.11f * (float)rng.NextDouble());
                float saturation = crowd
                    ? 0.64f + 0.30f * (float)rng.NextDouble()
                    : 0.48f + 0.25f * (float)rng.NextDouble();
                float brightness = crowd
                    ? 0.84f + 0.16f * (float)rng.NextDouble()
                    : 0.81f + 0.19f * (float)rng.NextDouble();
                Material material = new Material(shader)
                {
                    name = "RiverRush_" + (crowd ? "Crowd_" : "Natural_") + i
                };
                material.SetColor("_BaseTint", Color.HSVToRGB(selectedHue, saturation, brightness));
                material.SetFloat("_Opacity", i == 0 && !crowd ? dropletOpacity :
                                   Mathf.Clamp(dropletOpacity - 0.05f, 0.3f, 1f));
                palette[i] = material;
            }
        }

        private void DisposePalette()
        {
            if (palette == null) return;
            foreach (Material material in palette)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            palette = null;
        }

        private void OnDestroy() => DisposePalette();

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
