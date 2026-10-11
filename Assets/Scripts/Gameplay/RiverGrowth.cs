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
            volume = startingVolume;
            if (waterVisual != null) initialScale = waterVisual.localScale;
        }

        public void AddWater(float amount)
        {
            if (amount <= 0f) return;
            volume = Mathf.Min(maxVolume, volume + amount);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (waterVisual == null) return;
            float growth = 1f + (volume - startingVolume) * visualScaleFactor;
            waterVisual.localScale = new Vector3(initialScale.x * growth, initialScale.y, initialScale.z * growth);
        }

        private void OnTriggerEnter(Collider other)
        {
            var tributary = other.GetComponent<TributaryPickup>();
            if (tributary != null) tributary.TryCollect(this);
        }
    }

    [RequireComponent(typeof(Collider))]
    public sealed class TributaryPickup : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float waterAmount = 5f;
        private bool collected;

        private void Reset()
        {
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        public void TryCollect(RiverGrowth target)
        {
            if (collected || target == null) return;
            collected = true;
            target.AddWater(waterAmount);
            gameObject.SetActive(false);
        }
    }
}
