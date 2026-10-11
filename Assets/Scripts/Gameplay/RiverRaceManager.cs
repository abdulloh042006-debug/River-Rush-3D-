using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiverRush.Gameplay
{
    /// <summary>Offline race countdown, standings, finish order, and replay UI.</summary>
    public sealed class RiverRaceManager : MonoBehaviour
    {
        [SerializeField, Range(1f, 8f)] private float countdownSeconds = 3f;
        [SerializeField] private bool showLeaderboard = true;
        private readonly List<RiverRunner> competitors = new List<RiverRunner>(8);
        private readonly Dictionary<RiverRunner, float> finishTimes = new Dictionary<RiverRunner, float>();
        private RiverRunner player;
        private float startAt;
        private float nextSort;
        private bool playerFinished;
        private int finalRank;
        private float finalTime;

        public bool IsRacing => Time.unscaledTime >= startAt && !playerFinished;
        public float RaceTime => Mathf.Max(0f, Time.unscaledTime - startAt);
        public int PlayerRank
        {
            get
            {
                int index = competitors.IndexOf(player);
                return index >= 0 ? index + 1 : 0;
            }
        }

        private void Start()
        {
            var found = FindObjectsByType<RiverRunner>(FindObjectsSortMode.None);
            foreach (var runner in found)
            {
                if (runner == null) continue;
                competitors.Add(runner);
                if (runner.IsHumanControlled) player = runner;
            }
            if (player == null && competitors.Count > 0) player = competitors[0];
            startAt = Time.unscaledTime + countdownSeconds;
            nextSort = 0f;
            SortStandings();
        }

        private void Update()
        {
            if (Time.unscaledTime < startAt || competitors.Count == 0) return;
            foreach (var runner in competitors)
            {
                if (runner != null && runner.Finished && !finishTimes.ContainsKey(runner))
                    finishTimes.Add(runner, RaceTime);
            }
            if (Time.unscaledTime >= nextSort)
            {
                SortStandings();
                nextSort = Time.unscaledTime + 0.18f;
            }
            if (!playerFinished && player != null && player.Finished)
            {
                playerFinished = true;
                SortStandings();
                finalRank = PlayerRank;
                finalTime = RaceTime;
                Debug.Log("River Rush finished: position " + finalRank + "/" +
                          competitors.Count + " in " + RaceTime.ToString("F2") + " seconds");
            }
        }

        private void SortStandings()
        {
            competitors.Sort((a, b) =>
            {
                bool aDone = finishTimes.TryGetValue(a, out float aTime);
                bool bDone = finishTimes.TryGetValue(b, out float bTime);
                if (aDone && bDone)
                {
                    int byTime = aTime.CompareTo(bTime);
                    if (byTime != 0) return byTime;
                }
                else if (aDone) return -1;
                else if (bDone) return 1;

                int byProgress = b.Progress.CompareTo(a.Progress);
                return byProgress != 0 ? byProgress :
                    string.CompareOrdinal(a.gameObject.name, b.gameObject.name);
            });
        }

        private void OnGUI()
        {
            if (player == null) return;
            float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            GUI.Box(new Rect(8, 48, 235, 73),
                "TIME: " + RaceTime.ToString("F1") + "s  |  POSITION: " +
                (playerFinished ? finalRank : PlayerRank) + "/" + competitors.Count +
                "\nDISTANCE: " + Mathf.RoundToInt(player.NormalizedProgress * 100f) + "%");

            if (showLeaderboard)
            {
                int count = Mathf.Min(competitors.Count, 8);
                GUI.Box(new Rect(8, 127, 235, 25 + count * 26), "LEADERBOARD");
                for (int i = 0; i < count; i++)
                {
                    RiverRunner racer = competitors[i];
                    if (racer == null) continue;
                    string label = (i + 1) + ". " + (racer == player ? "YOU" : racer.name) +
                                   "  " + Mathf.RoundToInt(racer.NormalizedProgress * 100f) + "%";
                    GUI.Label(new Rect(20, 152 + i * 26, 212, 24), label);
                }
            }

            if (!playerFinished && Time.unscaledTime < startAt)
            {
                string message = Mathf.CeilToInt(startAt - Time.unscaledTime).ToString();
                GUI.Box(new Rect(w * 0.5f - 90, h * 0.22f, 180, 80), "START IN\n" + message);
            }
            else if (playerFinished)
            {
                GUI.Box(new Rect(w * 0.5f - 160, h * 0.19f, 320, 130),
                    "OCEAN REACHED!\nPLACE: " + finalRank + "/" + competitors.Count +
                    "\nTIME: " + RaceTime.ToString("F1") + "s");
                if (GUI.Button(new Rect(w * 0.5f - 100, h * 0.19f + 138, 200, 54),
                    "NEW RANDOM RACE"))
                {
                    Scene active = SceneManager.GetActiveScene();
                    if (active.buildIndex >= 0) SceneManager.LoadScene(active.buildIndex);
                    else Debug.LogError("Add the prototype scene to Build Settings to enable replay.");
                }
            }

            GUI.matrix = previous;
        }
    }
}
