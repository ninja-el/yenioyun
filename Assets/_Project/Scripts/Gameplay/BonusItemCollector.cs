using System;
using DG.Tweening;
using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Meta;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Dokunulan bonus objeyi yığından çıkarıp toplar: <see cref="BonusItemType"/>'ta açılan ödülleri
    /// (ek süre, booster, etkinlik sayacı) verir, objeyi yükselip küçülerek kaybolacak şekilde oynatır
    /// ve havuza iade eder. Bonus kutuya gitmez ve hatalı hamle sayılmaz.
    /// </summary>
    public class BonusItemCollector : MonoBehaviour
    {
        /// <summary>Bonus obje toplandığında, ödüller verildikten sonra yayınlanır. Ses, UI ve ileride para birimi bunu dinler.</summary>
        public event Action<StackItem, BonusItemType> OnBonusCollected;

        [Tooltip("Toplanan bonusun yükselip küçülerek kaybolma süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float _collectDuration = 0.35f;

        [Tooltip("Toplanan bonusun kaybolurken yükseleceği mesafe (birim).")]
        [SerializeField, Min(0f)] private float _collectRiseHeight = 1f;

        /// <summary>Obje bir bonus ise toplar ve true döner; normal objede hiçbir şey yapmaz.</summary>
        public bool TryCollect(StackItem item, ItemStack itemStack)
        {
            if (item == null || !(item.Type is BonusItemType bonus)) { return false; }

            itemStack.Remove(item);
            item.SetSimulated(false);

            GrantRewards(bonus);
            PlayCollectAnimation(item);

            OnBonusCollected?.Invoke(item, bonus);
            return true;
        }

        private void PlayCollectAnimation(StackItem item)
        {
            Transform itemTransform = item.transform;
            itemTransform.DOKill();

            DOTween.Sequence()
                .Join(itemTransform.DOMove(itemTransform.position + Vector3.up * _collectRiseHeight, _collectDuration)
                    .SetEase(Ease.OutQuad))
                .Join(itemTransform.DOScale(Vector3.zero, _collectDuration).SetEase(Ease.InBack))
                .OnComplete(() => PoolManager.Instance.Release(item.gameObject));
        }

        private static void GrantRewards(BonusItemType bonus)
        {
            if (bonus.BonusSeconds > 0f && BoosterManager.Instance != null)
            {
                BoosterManager.Instance.TryAddTimerSeconds(bonus.BonusSeconds);
            }

            if (bonus.GrantsBooster) { GrantBooster(bonus); }

            if (bonus.HasEvent && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddEventItems(bonus.EventId, bonus.EventAmount);
            }
        }

        private static void GrantBooster(BonusItemType bonus)
        {
            int inventoryAmount = bonus.BoosterAmount;

            if (bonus.UseBoosterInstantly
                && BoosterManager.Instance != null
                && BoosterManager.Instance.TryActivateFree(bonus.BoosterType))
            {
                inventoryAmount--;
            }

            if (inventoryAmount > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddBooster((int)bonus.BoosterType, inventoryAmount);
            }
        }
    }
}
