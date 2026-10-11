using UnityEngine;

namespace RiverRush.Gameplay
{
    /// <summary>Third-person chase camera suitable for prototype mouse and touch play.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RiverFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 22f, -28f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.22f;
        [SerializeField] private float lookAhead = 6f;
        private Vector3 velocity;

        private void Start()
        {
            if (target == null)
            {
                foreach (var runner in FindObjectsByType<RiverRunner>(FindObjectsSortMode.None))
                {
                    if (runner.IsHumanControlled)
                    {
                        target = runner.transform;
                        break;
                    }
                }
            }
            if (target != null) transform.position = target.position + offset;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(
                transform.position, target.position + offset, ref velocity, smoothTime);
            Vector3 focus = target.position + Vector3.forward * lookAhead;
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
