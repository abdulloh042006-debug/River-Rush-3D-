using System;
using UnityEngine;
using RiverRush.Gameplay;
using RiverRush.World;

namespace RiverRush.Audio
{
    /// <summary>
    /// Three low-cost generated ambient loops: flowing water, gusting wind and
    /// forest leaves/birds. No licensed audio files or audio thread synthesis.
    /// Sounds are stylized approximations that may later be replaced by recordings.
    /// </summary>
    public sealed class RiverNatureAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.65f;
        [SerializeField] private bool showAudioControls = true;
        [SerializeField, Min(8000)] private int sampleRate = 16000;
        [SerializeField, Range(2, 10)] private int loopSeconds = 5;
        [Header("Base mix")]
        [SerializeField, Range(0f, 1f)] private float riverVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] private float windVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] private float forestVolume = 0.24f;

        [Header("Optional recorded ambience (overrides procedural synthesis)")]
        [SerializeField] private AudioClip recordedWaterLoop;
        [SerializeField] private AudioClip recordedWindLoop;
        [SerializeField] private AudioClip recordedForestLoop;
        [SerializeField] private AudioClip recordedSplash;
        private AudioClip splashClip;
        private AudioClip generatedSplash;
        private AudioSource[] layers;
        private AudioClip[] clips;
        private RiverRunner humanRunner;
        private ProceduralTerrain terrain;
        private bool audioEnabled = true;

        public bool Enabled => audioEnabled;
        public float MasterVolume => masterVolume;

        private void Start()
        {
            audioEnabled = PlayerPrefs.GetInt("RR_AudioEnabled", 1) == 1;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("RR_MasterVolume", masterVolume));
            terrain = FindFirstObjectByType<ProceduralTerrain>();
            foreach (var runner in FindObjectsByType<RiverRunner>(FindObjectsSortMode.None))
            {
                if (!runner.IsHumanControlled) continue;
                humanRunner = runner;
                break;
            }

            int seed = terrain != null ? terrain.Seed : 6187;
            layers = new AudioSource[3];
            clips = new AudioClip[3];
            for (int layer = 0; layer < 3; layer++)
            {
                var layerObject = new GameObject(
                    layer == 0 ? "Flowing Water" : layer == 1 ? "Mountain Wind" : "Forest Nature & Birds");
                layerObject.transform.SetParent(transform, false);
                var source = layerObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f; // non-positional ambience; one AudioListener on camera
                source.volume = 0f;
                source.priority = 100 + layer * 5;

                AudioClip recorded = layer == 0 ? recordedWaterLoop :
                    layer == 1 ? recordedWindLoop : recordedForestLoop;
                if (recorded != null)
                {
                    source.clip = recorded;
                }
                else
                {
                    float[] data = CreateLoop(seed + layer * 3571, layer);
                    var clip = AudioClip.Create("River Rush " + layer, data.Length, 1, sampleRate, false);
                    if (!clip.SetData(data, 0))
                        Debug.LogError("River Rush: failed to fill generated nature audio clip.", this);
                    source.clip = clip;
                    clips[layer] = clip; // only destroy clips we created ourselves
                }
                source.Play();
                layers[layer] = source;
            }
            if (recordedSplash != null) splashClip = recordedSplash;
            else
            {
                generatedSplash = MakeSplashClip(seed ^ 0x3521F);
                splashClip = generatedSplash;
            }
        }

        /// <summary>Water pickup and splash one-shot. Uses current water layer volume.</summary>
        public void PlaySplash(float intensity = 1f)
        {
            if (!audioEnabled || layers == null || layers.Length == 0 ||
                layers[0] == null || splashClip == null) return;
            layers[0].PlayOneShot(splashClip, Mathf.Clamp(intensity, 0.2f, 1.3f));
        }

        private AudioClip MakeSplashClip(int seed)
        {
            int length = Mathf.Max(1000, Mathf.RoundToInt(sampleRate * 0.42f));
            float[] data = new float[length];
            var random = new System.Random(seed);
            float low = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                low = low * 0.88f + noise * 0.12f;
                float envelope = (1f - Mathf.Exp(-85f * t)) * Mathf.Exp(-8.5f * t);
                float bubble = Mathf.Sin(6.2831853f * (270f * t - 120f * t * t));
                data[i] = Mathf.Clamp((low * 0.65f + noise * 0.14f + bubble * 0.16f)
                    * envelope * 0.9f, -0.95f, 0.95f);
            }
            AudioClip clip = AudioClip.Create("RiverRush Water Splash", length, 1, sampleRate, false);
            if (!clip.SetData(data, 0))
                Debug.LogError("Failed to create splash sound clip.", this);
            return clip;
        }

        private float[] CreateLoop(int seed, int layer)
        {
            int count = sampleRate * loopSeconds;
            float[] data = new float[count];
            var random = new System.Random(seed);
            float smooth = 0f;
            float gentle = 0f;
            float last = 0f;
            float[] chirpStart = new float[5];
            for (int j = 0; j < chirpStart.Length; j++)
                chirpStart[j] = 0.3f + (loopSeconds - 1f) * (float)random.NextDouble();

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float white = (float)random.NextDouble() * 2f - 1f;
                smooth = smooth * 0.986f + white * 0.014f;
                gentle = gentle * 0.9993f + white * 0.0007f;
                float highPass = white - smooth;
                last = last * 0.76f + white * 0.24f;
                float sample;

                if (layer == 0)
                {
                    // Bubbling water: broadband gentle splash + slower water surges.
                    float swell = 0.65f + 0.22f * Mathf.Sin(t * 2.2f) +
                                  0.13f * Mathf.Sin(t * 5.3f + seed * 0.002f);
                    sample = (last * 0.46f + highPass * 0.12f +
                              smooth * 0.55f) * swell;
                }
                else if (layer == 1)
                {
                    // Wind: slow gusting low-passed noise, slight soft whistling.
                    float gust = 0.45f + 0.20f * Mathf.Sin(1.7f * t) +
                                 0.16f * Mathf.Sin(0.78f * t + 0.7f);
                    sample = (smooth * 1.5f + gentle * 3.0f + last * 0.08f) *
                             Mathf.Max(0.1f, gust);
                }
                else
                {
                    // Rustling forest + individually enveloped bird chirps.
                    sample = smooth * 0.42f + highPass * 0.035f;
                    for (int j = 0; j < chirpStart.Length; j++)
                    {
                        float local = t - chirpStart[j];
                        if (local <= 0f || local >= 0.24f) continue;
                        float phase = 6.2831853f * (1000f * local + 700f * local * local);
                        float envelope = Mathf.Sin(Mathf.PI * local / 0.24f);
                        sample += Mathf.Sin(phase) * envelope * envelope * 0.11f;
                    }
                }

                // Short fade near the loop seam prevents speaker clicks.
                const float seamSeconds = 0.018f;
                float fadeSamples = Mathf.Max(1f, sampleRate * seamSeconds);
                float fade = Mathf.Min(1f,
                    Mathf.Min(i / fadeSamples, (count - 1 - i) / fadeSamples));
                data[i] = Mathf.Clamp(sample * fade * 0.78f, -0.90f, 0.90f);
            }
            return data;
        }

        private void Update()
        {
            if (layers == null) return;
            float progress = humanRunner != null ? humanRunner.NormalizedProgress : 0f;
            float speed = humanRunner != null ? humanRunner.CurrentSpeed : 0f;
            float current = Mathf.Clamp01(speed / 23f);
            float water = riverVolume * (0.62f + 0.38f * current);
            float wind = windVolume * (0.65f + 0.35f * Mathf.Sin(Time.time * 0.17f + 0.4f));
            float forest = forestVolume * (1f - Mathf.SmoothStep(0.55f, 1f, progress) * 0.8f);
            float master = audioEnabled ? masterVolume : 0f;
            float fadeSpeed = 0.7f * Time.unscaledDeltaTime;
            layers[0].volume = Mathf.MoveTowards(layers[0].volume, master * water, fadeSpeed);
            layers[1].volume = Mathf.MoveTowards(layers[1].volume, master * wind, fadeSpeed);
            layers[2].volume = Mathf.MoveTowards(layers[2].volume, master * forest, fadeSpeed);
        }

        public void SetEnabled(bool enabled)
        {
            audioEnabled = enabled;
            PlayerPrefs.SetInt("RR_AudioEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("RR_MasterVolume", masterVolume);
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            if (!showAudioControls) return;
            float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 900f, Screen.height / 500f));
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            GUI.Box(new Rect(w - 188f, h - 92f, 180f, 84f), "NATURE AUDIO");
            if (GUI.Button(new Rect(w - 181f, h - 66f, 92f, 29f),
                audioEnabled ? "Sound ON" : "Sound OFF"))
                SetEnabled(!audioEnabled);
            if (GUI.Button(new Rect(w - 85f, h - 66f, 34f, 29f), "-"))
                SetMasterVolume(masterVolume - 0.1f);
            if (GUI.Button(new Rect(w - 47f, h - 66f, 34f, 29f), "+"))
                SetMasterVolume(masterVolume + 0.1f);
            GUI.Label(new Rect(w - 178f, h - 36f, 165f, 22f),
                "Volume " + Mathf.RoundToInt(masterVolume * 100f) + "%");
            GUI.matrix = previous;
        }

        private void OnDestroy()
        {
            if (generatedSplash != null)
            {
                if (Application.isPlaying) Destroy(generatedSplash);
                else DestroyImmediate(generatedSplash);
            }
            if (layers != null)
            {
                foreach (var source in layers)
                    if (source != null) source.Stop();
            }
            if (clips != null)
            {
                foreach (var clip in clips)
                    if (clip != null)
                    {
                        if (Application.isPlaying) Destroy(clip);
                        else DestroyImmediate(clip);
                    }
            }
        }
    }
}
