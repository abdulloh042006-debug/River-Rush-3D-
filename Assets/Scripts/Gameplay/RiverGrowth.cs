using UnityEngine;

namespace RiverRush.Gameplay
{
    /// <summary>
    /// Prototype river volume and tributary collection.
    /// Attach to the same object as RiverRunner; add trigger colliders to tributaries.
    /// </summary>
    [RequireComponent(typeof(RiverRunner))]
    public sealed class RiverGrowth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float startingVolume = 10f;
        [SerializeField, Min(1f)] private float maxVolume = 100f;
        [SerializeField, Min(0f)] private float visualScaleFactor = 0.035f;
        [SerializeField] private Transform waterVisual;

        private float volume;
        private Vector3 initialScale;

        public float Volume => volume;
        public float NormalizedVolume => Mathf.InverseLerp(startingVolume, maxVolume, volume);

        private void Awake()
        {
            if (waterVisual == null) waterVisual = transform;
            initialScale = waterVisual.localScale;
            volume = startingVolume;
            UpdateVisual();
        }

        public void AddWater(float amount)
        {
            if (amount <= 0f) return;
            float previous = volume;
            volume = Mathf.Min(maxVolume, volume + amount);
            UpdateVisual();
            if (volume > previous)
            {
                var waves = FindFirstObjectByType<RiverRush.World.WaterWaveSimulator>();
                var runner = GetComponent<RiverRunner>();
                if (waves != null && runner != null)
                    waves.AddImpulse(runner.Progress, Mathf.Min(6f, (volume - previous) * 0.45f), 7f);
            }
        }

        private void UpdateVisual()
        {
            if (waterVisual == null) return;
            float growth = 1f + (volume - startingVolume) * visualScaleFactor;
            waterVisual.localScale = new Vector3(
                initialScale.x * growth, initialScale.y, initialScale.z * growth);
        }

        private void OnTriggerEnter(Collider other)
        {
            var tributary = other.GetComponent<TributaryPickup>();
            if (tributary != null) tributary.TryCollect(this);
        }

        private void OnValidate() => maxVolume = Mathf.Max(startingVolume, maxVolume);
    }
}
