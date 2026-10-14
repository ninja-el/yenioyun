using System;
using UnityEngine;

namespace SDKHub
{
    public sealed class NullNotificationService : INotificationService
    {
        int nextId;

        public void Initialize()
        {
        }

        public void RequestPermission()
        {
            Debug.Log("[SDK Hub] Notifications mock: permission request ignored.");
        }

        public int Schedule(string title, string text, TimeSpan delay)
        {
            Debug.Log($"[SDK Hub] Notifications mock: '{title}' in {delay.TotalSeconds:0}s.");
            return ++nextId;
        }

        public int ScheduleAt(string title, string text, DateTime localTime)
        {
            Debug.Log($"[SDK Hub] Notifications mock: '{title}' at {localTime}.");
            return ++nextId;
        }

        public void Cancel(int id)
        {
        }

        public void CancelAll()
        {
        }
    }
}
