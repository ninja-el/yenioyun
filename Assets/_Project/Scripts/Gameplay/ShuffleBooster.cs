using DG.Tweening;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Shuffle. Yığında duran objeleri alan içinde yeniden dağıtır; objeler birbirine çarpmadan
    /// yeni noktalarına kayar. Kutuya uçmakta olan objeler yığından çıkmış olduğu için etkilenmez.
    /// </summary>
    public class ShuffleBooster : BoosterBehaviour
    {
        [Tooltip("Karıştırılacak yığın.")]
        [SerializeField] private ItemStack _itemStack;

        [Tooltip("Objelerin yeni yerlerine kayarken izlediği hız eğrisi. Süre Booster_Shuffle asset'inden gelir.")]
        [SerializeField] private Ease _moveEase = Ease.InOutQuad;

        public override BoosterType Type => BoosterType.Shuffle;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || !_itemStack.IsSettled || !_itemStack.Reshuffle(data.ShuffleDuration, _moveEase)) { return false; }

            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.EffectDuration);
            return true;
        }
    }
}
