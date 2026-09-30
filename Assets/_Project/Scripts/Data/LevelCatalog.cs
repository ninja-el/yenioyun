using System;
using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Bölümlerin oynanma sırası. `PlayerData.CurrentLevel` oyuncuya gösterilen, 1'den başlayıp
    /// sınırsız artan bölüm numarasıdır. Numara katalogdaki bölüm sayısını aşınca bölümler
    /// <see cref="LoopStartLevel"/>'dan başlayarak döngüyle tekrar oynanır; gösterilen numara artmaya devam eder.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "MatchPack/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [Tooltip("Bölümler oynanma sırasına göre. Boş satır bırakılmaz.")]
        [SerializeField] private LevelData[] _levels = Array.Empty<LevelData>();

        [Tooltip("Son bölüm bitince döngünün başlayacağı bölüm (1'den başlayan sıra). Ör. 50 bölümde 21: 50'den sonra " +
            "gösterge 51 der ama 21. bölüm oynanır, 80'den sonra yine 21'e döner. Bölüm sayısından büyükse son bölüm tekrar eder.")]
        [SerializeField, Min(1)] private int _loopStartLevel = 21;

        /// <summary>Katalogdaki bölüm sayısı.</summary>
        public int Count => _levels != null ? _levels.Length : 0;

        /// <summary>Döngünün başladığı bölüm; katalog sınırlarına kırpılmış.</summary>
        public int LoopStartLevel => Mathf.Clamp(_loopStartLevel, 1, Mathf.Max(1, Count));

        /// <summary>
        /// Gösterilen bölüm numarasına karşılık gelen level. Katalogdan büyük numaralar döngüye eşlenir;
        /// numara 1'den küçükse veya katalog boşsa null.
        /// </summary>
        public LevelData GetByNumber(int levelNumber)
        {
            if (levelNumber < 1 || Count == 0) { return null; }

            return _levels[ToCatalogNumber(levelNumber) - 1];
        }

        /// <summary>Gösterilen bölüm numarasının katalogdaki 1'den başlayan sırası.</summary>
        public int ToCatalogNumber(int levelNumber)
        {
            if (levelNumber <= Count) { return levelNumber; }

            int loopStart = LoopStartLevel;
            int loopLength = Count - loopStart + 1;
            return loopStart + (levelNumber - Count - 1) % loopLength;
        }
    }
}
