using System.Text;
using TMPro;
using UnityEngine;

namespace SDKHub.Components
{
    [SdkHubHelp("Shows the top 10 players of a leaderboard in a text. The leaderboard is set from the hub (Leaderboards page, Add to scene). It refreshes whenever it is shown.\n\n" +
                "Without it, from any script:\n" +
                "SdkManager.Leaderboards.GetTop(\"my_board\", 10, (success, rows) => { });")]
    [AddComponentMenu("SDK Hub/Leaderboard View")]
    public class LeaderboardView : SdkHubBehaviour
    {
        [HideInInspector]
        [SerializeField] string leaderboardId;

        [HideInInspector]
        [SerializeField] TMP_Text output;

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (output == null) output = GetComponent<TMP_Text>();
            if (output == null) return;

            output.text = "Loading...";
            SdkManager.Leaderboards.GetTop(leaderboardId, 10, (success, list) =>
            {
                if (output == null) return;
                if (!success)
                {
                    output.text = "Could not load the leaderboard.";
                    return;
                }

                var builder = new StringBuilder();
                foreach (var row in list)
                {
                    builder.AppendLine(row.Rank + ". " + row.PlayerName + "   " + row.Score.ToString("0.##"));
                }
                output.text = builder.Length > 0 ? builder.ToString() : "No scores yet.";
            });
        }
    }
}
