using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Gameplay;
using MatchPack.Meta;
using TMPro;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Oyun içi HUD'un kalan süre ve gold göstergesi. Süreyi <see cref="LevelTimer"/>, gold'u
    /// <see cref="EconomyManager"/> işletir; bu bileşen yalnızca event'leri dinleyip yazar.
    /// Panel kapandığında aboneliklerini bırakır.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        [Tooltip("Kalan sürenin okunduğu sayaç.")]
        [SerializeField] private LevelTimer _timer;

        [Tooltip("Kalan sürenin yazıldığı alan.")]
        [SerializeField] private TMP_Text _timeText;

        [Tooltip("Oyuncunun gold miktarının yazıldığı alan.")]
        [SerializeField] private TMP_Text _goldText;

        private void OnEnable()
        {
            if (_timer != null) { _timer.OnTimerTicked += SetRemainingTime; }

            SetRemainingTime(GetRemainingSeconds());

            if (EconomyManager.Instance == null) { return; }

            EconomyManager.Instance.OnGoldChanged += SetGold;
            SetGold(EconomyManager.Instance.Gold);
        }

        private void OnDisable()
        {
            if (_timer != null) { _timer.OnTimerTicked -= SetRemainingTime; }

            if (EconomyManager.Instance != null) { EconomyManager.Instance.OnGoldChanged -= SetGold; }
        }

        private void SetGold(int gold)
        {
            if (_goldText != null) { _goldText.text = gold.ToString(); }
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
