using UnityEngine;
using RiverRush.Gameplay;

namespace RiverRush.World
{
    /// <summary>
    /// Rebuilds the main route after terrain seed randomization; the branch
    /// network handles tributaries and pickups separately.
    /// Leave terrain.RandomizeEachRace=false to replay a saved numeric seed.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(ProceduralTerrain), typeof(RiverRoute))]
    public sealed class RiverRaceBootstrap : MonoBehaviour
    {
        [SerializeField] private ProceduralTerrain terrain;
        [SerializeField] private RiverRoute route;

        private void Awake()
        {
            if (terrain == null) terrain = GetComponent<ProceduralTerrain>();
            if (route == null) route = GetComponent<RiverRoute>();
            if (terrain == null || route == null) return;
            // Terrain already generated at execution order -300, with a new seed.
            route.Rebuild();

            // RiverTributaryNetwork regenerates its own seeded pickups in Start().
            // Do not relocate old scene pickups to artificial spline intervals.

            var floater = FindFirstObjectByType<WaterFloatBody>();
            if (floater != null &&
                route.SampleBank(route.TotalLength * 0.55f, 1.4f, out Vector3 point))
            {
                var body = floater.GetComponent<Rigidbody>();
                if (body != null) body.position = point + Vector3.up * 0.5f;
                else floater.transform.position = point + Vector3.up * 0.5f;
            }

            Debug.Log("River Rush race seed: " + terrain.Seed +
                      ". Disable Randomize Each Race on ProceduralTerrain to replay it.");
        }
    }
}
