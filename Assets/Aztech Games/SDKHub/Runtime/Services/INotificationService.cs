using System;

namespace SDKHub
{
    public interface INotificationService
    {
        void Initialize();
        void RequestPermission();
        int Schedule(string title, string text, TimeSpan delay);
        int ScheduleAt(string title, string text, DateTime localTime);
        void Cancel(int id);
        void CancelAll();
    }
}
