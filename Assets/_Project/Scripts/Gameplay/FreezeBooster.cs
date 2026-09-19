using System.Collections;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Time Freeze. Sayacı BoosterData'daki süre boyunca durdurur; bandın da durup durmayacağı
    /// aynı asset'ten gelir. Sayaca ve banda doğrudan dokunmaz: kısıtları
    /// <see cref="BoosterManager.ApplyHolds"/> uygular, bu sınıf yalnızca donmanın sürdüğünü bilir.
    /// </summary>
    public class FreezeBooster : BoosterBehaviour
    {
        private Coroutine _freezeRoutine;
        private bool _isFrozen;
        private bool _isBeltFrozen;

        public override BoosterType Type => BoosterType.Freeze;

        /// <summary>Donma şu an sürüyor mu? Sürerken booster tekrar kullanılamaz.</summary>
        public bool IsFrozen => _isFrozen;

        /// <summary>Süren donma bandı da durduruyor mu?</summary>
        public bool IsBeltFrozen => _isFrozen && _isBeltFrozen;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || _isFrozen || !BoosterManager.Instance.IsTimerRunning) { return false; }

            // StartCoroutine gövdeyi ilk yield'e kadar hemen işletiyor; kısıtlar orada uygulandığı
            // için donma bayrağı coroutine başlamadan önce kalkmalı.
            _isFrozen = true;
            _isBeltFrozen = data.IsBeltFrozen;
            _freezeRoutine = StartCoroutine(FreezeRoutine(data));
            return true;
        }

        public override void Cancel()
        {
            if (!_isFrozen) { return; }

            if (_freezeRoutine != null) { StopCoroutine(_freezeRoutine); }

            _freezeRoutine = null;
            Release();
        }

        private IEnumerator FreezeRoutine(BoosterData data)
        {
            BoosterManager.Instance.ApplyHolds();
            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.FreezeDuration);

            yield return new WaitForSeconds(data.FreezeDuration);

            _freezeRoutine = null;
            Release();
        }

        private void Release()
        {
            _isFrozen = false;
            _isBeltFrozen = false;

            if (BoosterManager.Instance == null) { return; }

            BoosterManager.Instance.ApplyHolds();
        }
    }
}
