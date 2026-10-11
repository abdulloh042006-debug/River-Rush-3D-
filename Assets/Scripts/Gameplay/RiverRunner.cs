using UnityEngine;
using RiverRush.World;

namespace RiverRush.Gameplay
{
    /// <summary>River racing movement driven by flow speed, inertia and steering forces.</summary>
    public sealed class RiverRunner : MonoBehaviour
    {
        [SerializeField] private RiverRoute route;
        [SerializeField] private RiverFlowField flow;
        [SerializeField, Min(0f)] private float paddleAcceleration = 2.5f;
        [SerializeField, Min(0f)] private float sideAcceleration = 25f;
        [SerializeField, Min(0f)] private float lateralDamping = 5f;
        [SerializeField, Range(0f, 1f)] private float bankRestitution = 0.15f;
        [SerializeField, Min(0f)] private float bankPadding = 1f;
        [SerializeField, Min(0.01f)] private float turnRate = 8f;

        private float meters;
        private float lateral;
        private float sidewaysSpeed;
        private float downstreamSpeed;
        private bool completed;

        public float Progress => meters;
        public float NormalizedProgress => route != null && route.TotalLength > 0f
            ? Mathf.Clamp01(meters / route.TotalLength) : 0f;
        public float CurrentSpeed => downstreamSpeed;
        public bool Finished => completed;

        private void Start()
        {
            if (route == null) route = FindFirstObjectByType<RiverRoute>();
            if (flow == null) flow = FindFirstObjectByType<RiverFlowField>();
            if (route != null && route.Points == null) route.Rebuild();
            if (route != null && route.SampleBank(0f, 0f, out Vector3 start))
                transform.position = start;
        }

        private void Update()
        {
            if (completed || route == null || flow == null || Time.deltaTime <= 0f) return;
            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            float steer = GetSteer();
            if (!flow.TrySample(meters, lateral, out Vector3 velocity, out _, out float targetSpeed))
                return;

            // Momentum catches up with the current, rather than snapping to fixed spline speed.
            downstreamSpeed = Mathf.MoveTowards(downstreamSpeed,
                targetSpeed + paddleAcceleration, 12f * deltaTime);
            sidewaysSpeed += steer * sideAcceleration * deltaTime;
            sidewaysSpeed = Mathf.MoveTowards(sidewaysSpeed, 0f, lateralDamping * deltaTime);
            lateral += sidewaysSpeed * deltaTime;

            float limit = Mathf.Max(0.1f, route.Width * 0.5f - bankPadding);
            if (Mathf.Abs(lateral) > limit)
            {
                lateral = Mathf.Sign(lateral) * limit;
                sidewaysSpeed = -sidewaysSpeed * bankRestitution;
                downstreamSpeed *= 0.97f;
            }

            meters = Mathf.Min(meters + downstreamSpeed * deltaTime, route.TotalLength);
            if (!route.SampleBank(meters, lateral, out Vector3 point)) return;
            transform.position = point;

            if (route.Sample(meters, out _, out Vector3 forward, out _))
            {
                Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up);
                if (flat.sqrMagnitude > 0.00001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(flat.normalized), turnRate * deltaTime);
            }

            if (meters >= route.TotalLength - 0.01f) completed = true;
        }

        private static float GetSteer()
        {
            float input = Input.GetAxisRaw("Horizontal");
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Canceled && touch.phase != TouchPhase.Ended)
                    input = Mathf.Clamp((touch.position.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
            }
            return Mathf.Clamp(input, -1f, 1f);
        }
    }
}
