using System.Collections;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Time Freeze. Sayacı BoosterData'daki süre boyunca kalan süreyi koruyarak durdurur; istenirse
    /// aynı süre boyunca bandı da durdurur. Süre dolunca ikisi de kaldığı yerden devam eder.
    /// </summary>
    public class FreezeBooster : BoosterBehaviour
    {
        [Tooltip("Dondurulacak level sayacı.")]
        [SerializeField] private LevelTimer _timer;

        [Tooltip("Donma süresince durdurulacak bant.")]
        [SerializeField] private Conveyor _conveyor;

        private Coroutine _freezeRoutine;

        public override BoosterType Type => BoosterType.Freeze;

        /// <summary>Donma şu an sürüyor mu? Sürerken booster tekrar kullanılamaz.</summary>
        public bool IsFrozen => _freezeRoutine != null;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || IsFrozen || !_timer.IsRunning) { return false; }

            _freezeRoutine = StartCoroutine(FreezeRoutine(data));
            return true;
        }

        public override void Cancel()
        {
            if (_freezeRoutine == null) { return; }

            StopCoroutine(_freezeRoutine);
            _freezeRoutine = null;
            Thaw();
        }

        private IEnumerator FreezeRoutine(BoosterData data)
        {
            _timer.SetPaused(true);
            if (data.IsBeltFrozen) { _conveyor.SetSpeedScale(0f); }

            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.FreezeDuration);

            yield return new WaitForSeconds(data.FreezeDuration);

            _freezeRoutine = null;
            Thaw();
        }

        private void Thaw()
        {
            _timer.SetPaused(false);
            _conveyor.SetSpeedScale(1f);
        }
    }
}
