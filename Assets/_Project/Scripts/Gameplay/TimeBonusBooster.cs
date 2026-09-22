using System;
using System.Collections;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Ek Süre. Sayaca BoosterData'daki saniyeyi ekler. Süre hemen değil, "+X sn" yazısının süre
    /// göstergesine uçma süresi dolunca eklenir; yazıyı UI bu sınıfın event'lerinden çizer.
    /// </summary>
    public class TimeBonusBooster : BoosterBehaviour
    {
        /// <summary>
        /// Booster kullanıldığında yayınlanır. Değerler: eklenecek saniye ve süre eklenene kadar
        /// geçecek uçuş süresi.
        /// </summary>
        public event Action<float, float> OnBonusLaunched;

        /// <summary>Süre sayaca eklendiğinde yayınlanır. Değer eklenen saniyedir.</summary>
        public event Action<float> OnBonusApplied;

        public override BoosterType Type => BoosterType.TimeBonus;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || data.BonusSeconds <= 0f || !BoosterManager.Instance.IsTimerRunning) { return false; }

            StartCoroutine(BonusRoutine(data));
            return true;
        }

        public override void Cancel()
        {
            StopAllCoroutines();
        }

        private IEnumerator BonusRoutine(BoosterData data)
        {
            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.EffectDuration);
            OnBonusLaunched?.Invoke(data.BonusSeconds, data.BonusFlyDuration);

            yield return new WaitForSeconds(data.BonusFlyDuration);

            // Uçuş sırasında süre bitip level kaybedilmiş olabilir; o durumda eklenecek sayaç yok.
            if (!BoosterManager.Instance.TryAddTimerSeconds(data.BonusSeconds)) { yield break; }

            OnBonusApplied?.Invoke(data.BonusSeconds);
        }
    }
}
