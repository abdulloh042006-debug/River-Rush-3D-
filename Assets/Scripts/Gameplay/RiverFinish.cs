using UnityEngine;
using RiverRush.World;

namespace RiverRush.Gameplay
{
    /// <summary>Ends a solo prototype race when the river reaches the sea.</summary>
    [RequireComponent(typeof(RiverRunner))]
    public sealed class RiverFinish : MonoBehaviour
    {
        private RiverRunner runner;
        private bool announced;
        private float finishTime;
        private void Awake() => runner = GetComponent<RiverRunner>();
        private void Update()
        {
            if (announced || runner == null || !runner.Finished) return;
            announced = true;
            finishTime = Time.timeSinceLevelLoad;
            Debug.Log("River Rush: reached the ocean in " + finishTime.ToString("F1") + " seconds");
        }
        private void OnGUI()
        {
            if (!announced) return;
            float width = Mathf.Min(Screen.width * 0.8f, 460f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.16f, width, 85f),
                "OCEAN REACHED!\nRace time: " + finishTime.ToString("F1") + "s");
        }
    }
}
