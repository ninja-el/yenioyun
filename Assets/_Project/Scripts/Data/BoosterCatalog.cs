using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Oyundaki tüm booster tanımlarının listesi. BoosterManager ve butonlar tanımı buradan bulur.
    /// </summary>
    [CreateAssetMenu(fileName = "BoosterCatalog", menuName = "MatchPack/Booster Catalog")]
    public class BoosterCatalog : ScriptableObject
    {
        [Tooltip("Booster tanımları. Her tip listede yalnızca bir kez bulunmalı.")]
        [SerializeField] private BoosterData[] _boosters = new BoosterData[0];

        public BoosterData[] Boosters => _boosters;

        /// <summary>Verilen tipin tanımını döner; bulunamazsa null.</summary>
        public BoosterData Find(BoosterType type)
        {
            for (int i = 0; i < _boosters.Length; i++)
            {
                if (_boosters[i] != null && _boosters[i].Type == type) { return _boosters[i]; }
            }

            return null;
        }
    }
}
