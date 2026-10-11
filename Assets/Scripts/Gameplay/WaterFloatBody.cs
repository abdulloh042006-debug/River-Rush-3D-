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
        [SerializeField] private OceanWaterPhysics ocean;
        [SerializeField] private RiverTributaryNetwork tributaries;
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
            if (ocean == null) ocean = FindFirstObjectByType<OceanWaterPhysics>();
            if (tributaries == null) tributaries = FindFirstObjectByType<RiverTributaryNetwork>();
        }

        private void FixedUpdate()
        {
            if (body.isKinematic) return;
            Vector3 surface = Vector3.zero;
            Vector3 current = Vector3.zero;
            bool submergedInRiver = river != null &&
                river.ClosestPoint(body.worldCenterOfMass, out float meters,
                    out float lateral, out surface) &&
                river.TrySample(meters, lateral, out current, out _, out _);
            bool submergedInTributary = !submergedInRiver && tributaries != null &&
                tributaries.TrySampleSurface(body.worldCenterOfMass, out surface, out current);
            bool submergedInOcean = !submergedInRiver && !submergedInTributary &&
                ocean != null &&
                ocean.TrySample(body.worldCenterOfMass, out surface, out current);
            if (!submergedInRiver && !submergedInTributary && !submergedInOcean) return;

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
