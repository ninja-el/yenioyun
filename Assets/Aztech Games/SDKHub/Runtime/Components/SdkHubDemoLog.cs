using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SDKHub.Components
{
    [SdkHubHelp("Shows SDK messages on screen. Used by the demo scene.")]
    [AddComponentMenu("SDK Hub/Demo Log")]
    public class SdkHubDemoLog : SdkHubBehaviour
    {
        [SerializeField] TMP_Text output;
        [SerializeField] int maxLines = 12;

        readonly Queue<string> lines = new Queue<string>();

        void OnEnable()
        {
            Application.logMessageReceived += OnUnityLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnUnityLog;
        }

        public void LogMessage(string message)
        {
            lines.Enqueue("- " + message);
            while (lines.Count > maxLines) lines.Dequeue();
            if (output != null) output.text = string.Join("\n", lines);
        }

        public void LogPurchased(string productId)
        {
            LogMessage("Purchased: " + productId);
        }

        public void LogPurchaseFailed(string reason)
        {
            LogMessage("Purchase failed: " + reason);
        }

        void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            const string prefix = "[SDK Hub]";
            if (condition.StartsWith(prefix)) LogMessage(condition.Substring(prefix.Length).Trim());
        }
    }
}
