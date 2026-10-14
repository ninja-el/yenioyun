using System;
using UnityEngine;

namespace SDKHub
{
    public enum ConsentStatus { Unknown, Agreed, Declined }

    public interface IConsentService
    {
        event Action<ConsentStatus> Changed;

        ConsentStatus Status { get; }
        bool HasAnswered { get; }

        void Agree();
        void Disagree();
        void Reset();
        void RequestTrackingPermission(Action<bool> onFinished);
    }

    public sealed class ConsentService : IConsentService
    {
        const string Key = "SDKHub.Consent";

        // Set by the optional iOS tracking support integration.
        public static Action<Action<bool>> TrackingRequester;

        public event Action<ConsentStatus> Changed;

        public ConsentStatus Status
        {
            get { return (ConsentStatus)PlayerPrefs.GetInt(Key, (int)ConsentStatus.Unknown); }
        }

        public bool HasAnswered => Status != ConsentStatus.Unknown;

        public void Agree()
        {
            Set(ConsentStatus.Agreed);
        }

        public void Disagree()
        {
            Set(ConsentStatus.Declined);
        }

        public void Reset()
        {
            Set(ConsentStatus.Unknown);
        }

        public void RequestTrackingPermission(Action<bool> onFinished)
        {
            if (TrackingRequester == null)
            {
                onFinished?.Invoke(true);
                return;
            }
            TrackingRequester(onFinished);
        }

        void Set(ConsentStatus status)
        {
            PlayerPrefs.SetInt(Key, (int)status);
            PlayerPrefs.Save();
            Changed?.Invoke(status);
        }
    }
}
