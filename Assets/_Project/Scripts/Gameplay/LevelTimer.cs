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

        /// <summary>Sayaç dondurulmuş mu? Time Freeze booster'ı bunu açar.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Sayacı verilen süreyle başlatır.</summary>
        public void StartTimer(float duration)
        {
            Remaining = duration;
            _lastTickedSecond = Mathf.CeilToInt(duration);
            IsRunning = true;
            IsPaused = false;
            OnTimerTicked?.Invoke(Remaining);
        }

        /// <summary>Sayacı durdurur. Level kazanıldığında ve sahne boşaltılırken çağrılır.</summary>
        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>
        /// Sayacı kalan süreyi koruyarak dondurur veya çözer. Stop'tan farkı, çözüldüğünde
        /// sayacın kaldığı yerden devam etmesidir; Time Freeze booster'ı bunu kullanır.
        /// </summary>
        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        /// <summary>
        /// Kalan süreyi değiştirir. Pozitif değer süre ekler (devam etme), negatif değer ceza
        /// olarak düşer. Süre sıfırın altına inmez; sayaç durmuşsa hiçbir şey yapmaz.
        /// </summary>
        public void AddSeconds(float seconds)
        {
            if (!IsRunning) { return; }

            Remaining = Mathf.Max(0f, Remaining + seconds);
            _lastTickedSecond = Mathf.CeilToInt(Remaining);
            OnTimerTicked?.Invoke(Remaining);
        }

        /// <summary>Durmuş sayacı verilen süreyle yeniden başlatır. Kaybedilen levele devam etmek için.</summary>
        public void Resume(float extraSeconds)
        {
            Remaining = Mathf.Max(Remaining, extraSeconds);
            _lastTickedSecond = Mathf.CeilToInt(Remaining);
            IsRunning = true;
            OnTimerTicked?.Invoke(Remaining);
        }

        private void Update()
        {
            if (!IsRunning || IsPaused) { return; }

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
