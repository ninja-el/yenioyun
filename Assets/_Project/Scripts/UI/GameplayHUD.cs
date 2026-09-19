using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Gameplay;
using TMPro;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Oyun içi HUD'un kalan süre göstergesi. Süreyi <see cref="LevelTimer"/> işletir, bu bileşen
    /// yalnızca event'i dinleyip yazar. Panel kapandığında aboneliğini bırakır.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        [Tooltip("Kalan sürenin okunduğu sayaç.")]
        [SerializeField] private LevelTimer _timer;

        [Tooltip("Kalan sürenin yazıldığı alan.")]
        [SerializeField] private TMP_Text _timeText;

        private void OnEnable()
        {
            if (_timer != null) { _timer.OnTimerTicked += SetRemainingTime; }

            SetRemainingTime(GetRemainingSeconds());
        }

        private void OnDisable()
        {
            if (_timer != null) { _timer.OnTimerTicked -= SetRemainingTime; }
        }

        // Sayaç yığın oturana kadar başlamıyor; o aralıkta 00:00 yerine level'in tam süresi durur.
        private float GetRemainingSeconds()
        {
            if (_timer != null && (_timer.IsRunning || _timer.Remaining > 0f)) { return _timer.Remaining; }

            LevelData level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
            return level != null ? level.Duration : 0f;
        }

        private void SetRemainingTime(float secondsRemaining)
        {
            if (_timeText == null) { return; }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, secondsRemaining));
            _timeText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
