using UnityEngine;

namespace RiverRush.Gameplay
{
    /// <summary>Simple prototype: moves a river runner downhill along a generated route.</summary>
    public sealed class RiverRunner : MonoBehaviour
    {
        [SerializeField] private RiverRush.World.RiverRoute route;
        [SerializeField, Min(0.1f)] private float speed = 12f;
        [SerializeField, Min(0f)] private float lateralRange = 6f;
        [SerializeField] private float steerSpeed = 7f;
        private float progress;
        private float lateral;
        public float Progress => progress;

        private void Start()
        {
            if (route == null) route = FindFirstObjectByType<RiverRush.World.RiverRoute>();
            if (route != null && (route.Points == null || route.Points.Count < 2)) route.Rebuild();
        }

        private void Update()
        {
            if (route == null || route.Points == null || route.Points.Count < 2) return;
            float input = Input.GetAxisRaw("Horizontal");
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    input = Mathf.Clamp((touch.position.x / Screen.width - 0.5f) * 2f, -1f, 1f);
            }
            lateral = Mathf.Clamp(lateral + input * steerSpeed * Time.deltaTime, -lateralRange, lateralRange);
            progress = Mathf.Min(progress + speed * Time.deltaTime, route.Points.Count - 1);
            int i = Mathf.Min(Mathf.FloorToInt(progress), route.Points.Count - 2);
            float t = progress - i;
            Vector3 a = route.Points[i];
            Vector3 b = route.Points[i + 1];
            Vector3 direction = (b - a).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            transform.position = route.transform.TransformPoint(Vector3.Lerp(a, b, t) + right * lateral);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
        }
    }
}
