using UnityEngine;

namespace RiverRush.Gameplay
{
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
