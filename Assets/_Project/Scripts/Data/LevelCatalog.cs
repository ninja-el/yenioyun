using System;
using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Bölümlerin oynanma sırası. `PlayerData.CurrentLevel` bu listedeki 1'den başlayan sıra
    /// numarasıdır; yeni bölüm eklemek listeye bir satır eklemekten ibarettir.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "MatchPack/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [Tooltip("Bölümler oynanma sırasına göre. Boş satır bırakılmaz.")]
        [SerializeField] private LevelData[] _levels = Array.Empty<LevelData>();

        /// <summary>Katalogdaki bölüm sayısı.</summary>
        public int Count => _levels != null ? _levels.Length : 0;

        /// <summary>1'den başlayan bölüm numarasına karşılık gelen level; numara listenin dışındaysa null.</summary>
        public LevelData GetByNumber(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > Count) { return null; }

            return _levels[levelNumber - 1];
        }

        /// <summary>Verilen levelin 1'den başlayan sıra numarası; katalogda yoksa 0.</summary>
        public int GetNumber(LevelData level)
        {
            if (level == null) { return 0; }

            for (int i = 0; i < Count; i++)
            {
                if (_levels[i] == level) { return i + 1; }
            }

            return 0;
        }

        /// <summary>Verilen levelden sonraki level varsa true döner. Son bölümde false döner.</summary>
        public bool TryGetNext(LevelData current, out LevelData next)
        {
            next = GetByNumber(GetNumber(current) + 1);
            return next != null;
        }
    }
}
