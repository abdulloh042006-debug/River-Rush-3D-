using UnityEngine;
using RiverRush.World;

namespace RiverRush.Gameplay
{
    /// <summary>
    /// Buoyancy and current drag for physical objects floating in the generated river.
    /// Attach to a non-kinematic Rigidbody with a Collider; leave the racing runner kinematic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class WaterFloatBody : MonoBehaviour
    {
        [SerializeField] private RiverFlowField river;
        [SerializeField, Min(0f)] private float liftStrength = 1.5f;
        [SerializeField, Min(0f)] private float dragAcceleration = 4f;
        [SerializeField, Min(0f)] private float maxHorizontalAcceleration = 28f;
        [SerializeField, Min(0f)] private float extraDepth = 0.25f;
        private Rigidbody body;
        private Collider bodyCollider;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            if (river == null) river = FindFirstObjectByType<RiverFlowField>();
        }

        private void FixedUpdate()
        {
            if (river == null || body.isKinematic) return;
            if (!river.ClosestPoint(body.worldCenterOfMass, out float meters,
                out float lateral, out Vector3 surface)) return;
            if (!river.TrySample(meters, lateral, out Vector3 current, out _, out _)) return;

            Bounds bounds = bodyCollider.bounds;
            float depth = surface.y + extraDepth - bounds.min.y;
            float submergedFraction = Mathf.Clamp01(depth / Mathf.Max(0.05f, bounds.size.y));
            if (submergedFraction <= 0f) return;

            // Approximate displaced-water buoyancy (forces scale by Rigidbody mass).
            Vector3 buoyancy = Vector3.up * (Physics.gravity.magnitude * body.mass *
                                             liftStrength * submergedFraction);
            body.AddForce(buoyancy, ForceMode.Force);

            Vector3 horizontalCurrent = Vector3.ProjectOnPlane(current, Vector3.up);
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 acceleration = Vector3.ClampMagnitude(
                (horizontalCurrent - horizontalVelocity) * dragAcceleration * submergedFraction,
                maxHorizontalAcceleration);
            body.AddForce(acceleration, ForceMode.Acceleration);
        }
    }
}
