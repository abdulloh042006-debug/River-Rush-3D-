using System;
using UnityEngine;
using RiverRush.Gameplay;

namespace RiverRush.World
{
    /// <summary>
    /// Rebuilds route references and aligns pickups after a newly randomized race seed.
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

            var pickups = FindObjectsByType<TributaryPickup>(FindObjectsSortMode.None);
            Array.Sort(pickups, (a, b) =>
                string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

            for (int i = 0; i < pickups.Length; i++)
            {
                float t = (i + 1f) / (pickups.Length + 1f);
                if (route.SampleBank(t * route.TotalLength, 0f, out Vector3 center))
                    pickups[i].transform.position = center + Vector3.up * 0.4f;
            }

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
