using UnityEngine;

namespace RiverRush.World
{
    /// <summary>
    /// Lightweight ocean surface height/current sampler used by floating Rigidbody objects.
    /// Its gentle sine waves are a low-cost gameplay approximation, not ocean CFD.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class OceanWaterPhysics : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float waveHeight = 0.12f;
        [SerializeField, Min(0.001f)] private float waveFrequency = 0.10f;
        [SerializeField, Min(0f)] private float waveSpeed = 0.75f;
        [SerializeField, Min(0f)] private float currentSpeed = 0.75f;

        public bool TrySample(Vector3 position, out Vector3 surface, out Vector3 velocity)
        {
            surface = Vector3.zero;
            velocity = Vector3.zero;
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return false;
            Bounds bounds = renderer.bounds;
            if (position.x < bounds.min.x || position.x > bounds.max.x ||
                position.z < bounds.min.z || position.z > bounds.max.z) return false;

            float time = Time.time * waveSpeed;
            float ripple = Mathf.Sin(position.x * waveFrequency + time) *
                           Mathf.Cos(position.z * waveFrequency * 0.8f - time * 0.9f);
            surface = new Vector3(position.x, transform.position.y + ripple * waveHeight, position.z);
            velocity = Vector3.forward * currentSpeed;
            return true;
        }
    }
}
