#if SDKHUB_LEADERBOARDS
using System;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class UnityLeaderboardService : ILeaderboardService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterLeaderboards(new UnityLeaderboardService());
        }

        public async void Submit(string leaderboardId, double score, Action<bool> onFinished = null)
        {
            if (!await UgsBootstrap.SignInAsync())
            {
                onFinished?.Invoke(false);
                return;
            }

            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
                onFinished?.Invoke(true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SDK Hub] Score submit failed: " + exception.Message);
                onFinished?.Invoke(false);
            }
        }

        public async void GetTop(string leaderboardId, int count, Action<bool, IList<LeaderboardRow>> onFinished)
        {
            if (!await UgsBootstrap.SignInAsync())
            {
                onFinished?.Invoke(false, new List<LeaderboardRow>());
                return;
            }

            try
            {
                var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions { Limit = count });
                var rows = new List<LeaderboardRow>();
                foreach (var entry in page.Results)
                {
                    rows.Add(new LeaderboardRow { Rank = entry.Rank + 1, PlayerName = entry.PlayerName, Score = entry.Score });
                }
                onFinished?.Invoke(true, rows);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SDK Hub] Leaderboard load failed: " + exception.Message);
                onFinished?.Invoke(false, new List<LeaderboardRow>());
            }
        }
    }
}
#endif
