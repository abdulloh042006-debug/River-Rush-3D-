using System;
using UnityEngine;
using RiverRush.World;

namespace RiverRush.Gameplay
{
    /// <summary>Deterministic-ish lane planner for a lightweight offline AI racer.</summary>
    [RequireComponent(typeof(RiverRunner))]
    public sealed class RiverBotAI : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 4f)] private float steeringSkill = 1.5f;
        [SerializeField, Range(0.5f, 8f)] private float decisionInterval = 2.4f;
        [SerializeField, Min(1f)] private float avoidanceDistance = 9f;
        [SerializeField, Min(0f)] private float preferredClearance = 2.8f;

        private RiverRunner runner;
        private RiverRoute route;
        private System.Random random;
        private RiverRunner[] rivals;
        private float targetLane;
        private float nextDecision;
        private int botNumber;

        public void Configure(int number, int raceSeed)
        {
            botNumber = number;
            random = new System.Random(unchecked(raceSeed ^ (number * 15485863)));
        }

        private void Start()
        {
            runner = GetComponent<RiverRunner>();
            route = FindFirstObjectByType<RiverRoute>();
            if (random == null)
                Configure(botNumber + 1, route != null && route.Terrain != null
                    ? route.Terrain.Seed : 2391);
            rivals = FindObjectsByType<RiverRunner>(FindObjectsSortMode.None);
            DecideLane();
        }

        private void Update()
        {
            if (runner == null || runner.Finished || route == null) return;
            if (Time.time >= nextDecision) DecideLane();

            float avoidance = 0f;
            if (rivals != null)
            {
                foreach (var rival in rivals)
                {
                    if (rival == null || rival == runner || rival.Finished) continue;
                    float ahead = rival.Progress - runner.Progress;
                    float separation = Mathf.Abs(rival.LateralPosition - runner.LateralPosition);
                    if (ahead <= 0f || ahead >= avoidanceDistance ||
                        separation >= preferredClearance) continue;

                    float side = Mathf.Sign(runner.LateralPosition - rival.LateralPosition);
                    if (Mathf.Abs(side) < 0.1f) side = botNumber % 2 == 0 ? 1f : -1f;
                    avoidance += side * (1f - ahead / avoidanceDistance)
                                 * (1f - separation / Mathf.Max(0.01f, preferredClearance));
                }
            }

            float desired = Mathf.Clamp(targetLane + avoidance * preferredClearance,
                -route.Width * 0.33f, route.Width * 0.33f);
            float error = desired - runner.LateralPosition;
            float steering = error * steeringSkill * 0.35f
                           - runner.SidewaysSpeed * 0.22f;
            runner.SetBotSteer(Mathf.Clamp(steering, -1f, 1f));
        }

        private void DecideLane()
        {
            if (route == null || random == null) return;
            float usable = route.Width * 0.32f;
            targetLane = ((float)random.NextDouble() * 2f - 1f) * usable;
            nextDecision = Time.time + decisionInterval *
                           (0.6f + (float)random.NextDouble() * 0.8f);
        }
    }
}
