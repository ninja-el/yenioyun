using System;
using MatchPack.Core;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Level süresini işletir. Kalan saniye değiştikçe tek bir event yayınlar, süre bitince
    /// leveli kaybettirir.
    /// </summary>
    public class LevelTimer : MonoBehaviour
    {
        /// <summary>Kalan saniye değiştiğinde yayınlanır. Değer kalan süredir.</summary>
        public event Action<float> OnTimerTicked;

        private int _lastTickedSecond;

        public float Remaining { get; private set; }
        public bool IsRunning { get; private set; }

        /// <summary>Sayacı verilen süreyle başlatır.</summary>
        public void StartTimer(float duration)
        {
            Remaining = duration;
            _lastTickedSecond = Mathf.CeilToInt(duration);
            IsRunning = true;
            OnTimerTicked?.Invoke(Remaining);
        }

        /// <summary>Sayacı durdurur. Level kazanıldığında ve sahne boşaltılırken çağrılır.</summary>
        public void Stop()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning) { return; }

            Remaining -= Time.deltaTime;

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                IsRunning = false;
                OnTimerTicked?.Invoke(0f);
                GameManager.Instance.FailLevel();
                return;
            }

            int currentSecond = Mathf.CeilToInt(Remaining);
            if (currentSecond == _lastTickedSecond) { return; }

            _lastTickedSecond = currentSecond;
            OnTimerTicked?.Invoke(Remaining);
        }
    }
}
