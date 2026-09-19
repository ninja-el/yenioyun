using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Shuffle. Yığında duran objeleri alan içinde yeniden dağıtır; objeler level başındaki gibi
    /// yeni noktalarına düşer. Kutuya uçmakta olan objeler yığından çıkmış olduğu için etkilenmez.
    /// </summary>
    public class ShuffleBooster : BoosterBehaviour
    {
        [Tooltip("Karıştırılacak yığın.")]
        [SerializeField] private ItemStack _itemStack;

        public override BoosterType Type => BoosterType.Shuffle;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || !_itemStack.IsSettled || !_itemStack.Reshuffle()) { return false; }

            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.EffectDuration);
            return true;
        }
    }
}
