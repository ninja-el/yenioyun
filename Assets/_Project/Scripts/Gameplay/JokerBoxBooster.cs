using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Joker Box. Banta tipsiz bir kutu gönderir: kutu ilk objesini her türden kabul eder, o
    /// objenin tipine kilitlenir ve kalan yuvalarını yalnızca aynı tipten objelerle doldurur.
    /// Kutunun alacağı obje adedi prefab'taki yuva sayısıdır.
    /// </summary>
    public class JokerBoxBooster : BoosterBehaviour
    {
        [Tooltip("Joker kutunun gönderileceği bant.")]
        [SerializeField] private Conveyor _conveyor;

        public override BoosterType Type => BoosterType.JokerBox;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || data.JokerBoxPrefab == null) { return false; }

            bool isAnyQueued = false;

            for (int i = 0; i < data.JokerBoxCount; i++)
            {
                if (!_conveyor.TryQueueJokerBox(data.JokerBoxPrefab)) { break; }

                isAnyQueued = true;
            }

            if (!isAnyQueued) { return false; }

            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.EffectDuration);
            return true;
        }
    }
}
