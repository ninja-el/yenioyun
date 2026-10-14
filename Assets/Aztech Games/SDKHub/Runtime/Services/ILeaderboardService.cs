using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public sealed class LeaderboardRow
    {
        public int Rank;
        public string PlayerName;
        public double Score;
    }

    [Serializable]
    public class LeaderboardCatalog
    {
        public List<string> ids = new List<string>();

        public static LeaderboardCatalog Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return new LeaderboardCatalog();
            try
            {
                return JsonUtility.FromJson<LeaderboardCatalog>(json) ?? new LeaderboardCatalog();
            }
            catch (ArgumentException)
            {
                return new LeaderboardCatalog();
            }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public interface ILeaderboardService
    {
        void Submit(string leaderboardId, double score, Action<bool> onFinished = null);
        void GetTop(string leaderboardId, int count, Action<bool, IList<LeaderboardRow>> onFinished);
    }

    // Mock service: a local leaderboard on this device.
    public sealed class NullLeaderboardService : ILeaderboardService
    {
        readonly Dictionary<string, List<double>> scores = new Dictionary<string, List<double>>();

        public void Submit(string leaderboardId, double score, Action<bool> onFinished = null)
        {
            List<double> list;
            if (!scores.TryGetValue(leaderboardId, out list))
            {
                list = new List<double>();
                scores[leaderboardId] = list;
            }
            list.Add(score);
            list.Sort((a, b) => b.CompareTo(a));
            Debug.Log($"[SDK Hub] Leaderboard '{leaderboardId}' (mock): score {score} saved.");
            onFinished?.Invoke(true);
        }

        public void GetTop(string leaderboardId, int count, Action<bool, IList<LeaderboardRow>> onFinished)
        {
            var rows = new List<LeaderboardRow>();
            List<double> list;
            if (scores.TryGetValue(leaderboardId, out list))
            {
                for (var i = 0; i < list.Count && i < count; i++)
                {
                    rows.Add(new LeaderboardRow { Rank = i + 1, PlayerName = "You", Score = list[i] });
                }
            }
            onFinished?.Invoke(true, rows);
        }
    }
}
