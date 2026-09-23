using System;
using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Bölüm zorluğunun tahmin modeli ve bölüm üretim eğrileri. Ortalama bir oyuncunun kutu başına
    /// harcadığı süre obje çeşidine göre tahmin edilir; kutu hedefi, bu sürenin level süresinin
    /// belirli bir oranını (zaman kullanımı) doldurmasına göre seçilir.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelBalanceConfig", menuName = "MatchPack/Level Balance Config")]
    public class LevelBalanceConfig : ScriptableObject
    {
        [Header("Oyuncu tahmini")]
        [Tooltip("Yığında tek obje tipi varken ortalama oyuncunun bir objeyi bulup dokunma süresi (sn).")]
        [SerializeField, Min(0.1f)] private float _baseSecondsPerItem = 0.9f;

        [Tooltip("Yığındaki her ek obje tipinin obje başına arama süresine eklediği süre (sn).")]
        [SerializeField, Min(0f)] private float _secondsPerExtraType = 0.08f;

        [Tooltip("Kutu başına sabit ek süre: yeni kutunun banda girişini beklemek, hatalı hamleler (sn).")]
        [SerializeField, Min(0f)] private float _boxOverheadSeconds = 0.4f;

        [Tooltip("Oyuncular arası hız farkı (standart sapma / ortalama). Kaybetme oranı tahmini bununla yapılır.")]
        [SerializeField, Range(0.01f, 1f)] private float _playerSpeedVariation = 0.2f;

        [Header("Bölüm sayısı ve süre")]
        [Tooltip("Üretilecek bölüm sayısı.")]
        [SerializeField, Min(1)] private int _levelCount = 50;

        [Tooltip("İlk bölümün süresi (sn).")]
        [SerializeField, Min(1f)] private float _minDuration = 30f;

        [Tooltip("Bölüm süresinin ulaşacağı en yüksek değer (sn).")]
        [SerializeField, Min(1f)] private float _maxDuration = 180f;

        [Tooltip("Sürenin en yüksek değere ulaştığı bölüm.")]
        [SerializeField, Min(2)] private int _durationRampEndLevel = 25;

        [Tooltip("Süreler bu adımın katına yuvarlanır (sn).")]
        [SerializeField, Min(1f)] private float _durationRoundStep = 5f;

        [Tooltip("Süre tavana ulaştıktan sonra zor bölümden uzaklaştıkça normal bölümden düşülen süre adımı (sn). Zor bölüme doğru süre tırmanır.")]
        [SerializeField, Min(0f)] private float _postRampDurationStep = 10f;

        [Header("Zor bölüm")]
        [Tooltip("Kaç bölümde bir zor bölüm gelir. 5 = 5, 10, 15...")]
        [SerializeField, Min(2)] private int _hardLevelInterval = 5;

        [Tooltip("Zor bölümde normal bölüme göre eklenen obje tipi sayısı.")]
        [SerializeField, Min(0)] private int _hardExtraTypes = 2;

        [Header("Obje çeşidi")]
        [Tooltip("İlk bölümdeki obje tipi sayısı.")]
        [SerializeField, Min(1)] private int _minTypeCount = 3;

        [Tooltip("Son bölümdeki obje tipi sayısı (zor bölüm eki hariç).")]
        [SerializeField, Min(1)] private int _maxTypeCount = 18;

        [Tooltip("Tip sayısı eğrisinin üssü. 1 = doğrusal, 1'den küçük = ilk bölümlerde daha hızlı artar.")]
        [SerializeField, Range(0.1f, 3f)] private float _typeCountCurveExponent = 0.7f;

        [Tooltip("İlk bölümde açık olan obje tipi sayısı (havuzun başından).")]
        [SerializeField, Min(1)] private int _startUnlockedTypes = 4;

        [Tooltip("Her bölümde havuzdan açılan yeni obje tipi sayısı.")]
        [SerializeField, Min(0)] private int _typesUnlockedPerLevel = 1;

        [Tooltip("Obje tipleri açılma sırasına göre. Bölümler yalnızca açılmış tiplerden seçer.")]
        [SerializeField] private ItemType[] _itemPool = Array.Empty<ItemType>();

        [Header("Zaman kullanımı (ortalama oyuncunun gereken süresi / bölüm süresi)")]
        [Tooltip("İlk normal bölümün zaman kullanımı. Düşük = kolay, 1 = ortalama oyuncu tam süreyi kullanır.")]
        [SerializeField, Range(0.1f, 1.5f)] private float _normalTimeUsageStart = 0.6f;

        [Tooltip("Son normal bölümün zaman kullanımı.")]
        [SerializeField, Range(0.1f, 1.5f)] private float _normalTimeUsageEnd = 0.85f;

        [Tooltip("İlk zor bölümün zaman kullanımı.")]
        [SerializeField, Range(0.1f, 1.5f)] private float _hardTimeUsageStart = 0.88f;

        [Tooltip("Son zor bölümün zaman kullanımı.")]
        [SerializeField, Range(0.1f, 1.5f)] private float _hardTimeUsageEnd = 0.98f;

        [Header("Süreye göre obje sayısı (üs eğrisi, iki noktadan geçer)")]
        [Tooltip("Eğrinin ilk noktasının süresi (sn).")]
        [SerializeField, Min(1f)] private float _itemCurveBaseDuration = 30f;

        [Tooltip("İlk noktadaki obje sayısı.")]
        [SerializeField, Min(1)] private int _itemCurveBaseCount = 30;

        [Tooltip("Eğrinin ikinci noktasının süresi (sn). Eğim bu iki noktadan hesaplanır.")]
        [SerializeField, Min(1f)] private float _itemCurveReferenceDuration = 60f;

        [Tooltip("İkinci noktadaki obje sayısı. Süre büyüdükçe obje başına düşen süre artar.")]
        [SerializeField, Min(1)] private int _itemCurveReferenceCount = 45;

        [Tooltip("Zor bölümler dışında bir bölümün en fazla obje sayısı.")]
        [SerializeField, Min(1)] private int _maxNormalItemCount = 150;

        [Header("Üretim")]
        [Tooltip("Bantta aynı anda duran kutu sayısı; bütün bölümlere yazılır.")]
        [SerializeField, Min(1)] private int _conveyorCapacity = 3;

        [Tooltip("Tip seçimi ve kutu sırası için rastgelelik tohumu. Aynı tohum aynı bölümleri üretir.")]
        [SerializeField] private int _seed = 1234;

        public int LevelCount => _levelCount;
        public int ConveyorCapacity => _conveyorCapacity;
        public int Seed => _seed;
        public ItemType[] ItemPool => _itemPool;

        /// <summary>Bölüm zor bölüm mü?</summary>
        public bool IsHardLevel(int levelNumber) => levelNumber % _hardLevelInterval == 0;

        /// <summary>Ortalama oyuncunun verilen tip sayısında tek bir kutuyu doldurma süresi (sn).</summary>
        public float EstimateSecondsPerBox(int typeCount, int boxCapacity)
        {
            float secondsPerItem = _baseSecondsPerItem + _secondsPerExtraType * Mathf.Max(0, typeCount - 1);
            return boxCapacity * secondsPerItem + _boxOverheadSeconds;
        }

        /// <summary>Ortalama oyuncunun verilen sürede doldurabileceği kutu sayısı (kesirli).</summary>
        public float EstimateBoxesInTime(float duration, int typeCount, int boxCapacity)
        {
            return duration / EstimateSecondsPerBox(typeCount, boxCapacity);
        }

        /// <summary>
        /// Süreye karşılık gelen obje sayısı: iki noktadan geçen <c>n = n0 x (süre / süre0)^k</c> eğrisi.
        /// Varsayılanlarla 30 sn = 30, 60 sn = 45 obje; 100 objede bir sonraki obje ~4 sn ekler.
        /// </summary>
        public float GetItemCountForDuration(float duration)
        {
            float exponent = Mathf.Log((float)_itemCurveReferenceCount / _itemCurveBaseCount)
                / Mathf.Log(_itemCurveReferenceDuration / _itemCurveBaseDuration);
            return _itemCurveBaseCount * Mathf.Pow(duration / _itemCurveBaseDuration, exponent);
        }

        /// <summary>Süreye karşılık gelen kutu sayısı; obje sayısı kutu kapasitesinin katına yukarı yuvarlanır.</summary>
        public int GetBoxCountForDuration(float duration, int boxCapacity)
        {
            return Mathf.CeilToInt(GetItemCountForDuration(duration) / boxCapacity);
        }

        /// <summary>Bölümün kutu tavanı. Zor bölümde tavan yoktur.</summary>
        public int GetMaxBoxCount(int levelNumber, int boxCapacity)
        {
            return IsHardLevel(levelNumber) ? int.MaxValue : _maxNormalItemCount / boxCapacity;
        }

        /// <summary>
        /// Oyuncuların süreyi yetiştirememe oranı. Oyuncu süresi, ortalaması zaman kullanımı olan
        /// normal dağılım kabul edilir; 1 kullanımda oran %50'dir.
        /// </summary>
        public float EstimateFailRate(float timeUsage)
        {
            float z = (1f / timeUsage - 1f) / _playerSpeedVariation;
            return 1f - NormalCdf(z);
        }

        /// <summary>Bölüm süresi (sn). Tavana kadar doğrusal tırmanır, sonra zor bölüme doğru testere dişi çizer.</summary>
        public float GetDuration(int levelNumber)
        {
            float t = Mathf.Clamp01((levelNumber - 1f) / (_durationRampEndLevel - 1f));
            float duration = Mathf.Lerp(_minDuration, _maxDuration, t);

            if (levelNumber > _durationRampEndLevel && !IsHardLevel(levelNumber))
            {
                int stepsToHard = _hardLevelInterval - levelNumber % _hardLevelInterval;
                duration -= stepsToHard * _postRampDurationStep;
            }

            return Mathf.Round(duration / _durationRoundStep) * _durationRoundStep;
        }

        /// <summary>Bölümde kullanılacak obje tipi sayısı; açılmış tip sayısını aşmaz.</summary>
        public int GetTypeCount(int levelNumber)
        {
            float t = Mathf.Clamp01((levelNumber - 1f) / Mathf.Max(1, _levelCount - 1));
            int count = Mathf.RoundToInt(Mathf.Lerp(_minTypeCount, _maxTypeCount, Mathf.Pow(t, _typeCountCurveExponent)));
            if (IsHardLevel(levelNumber)) { count += _hardExtraTypes; }

            return Mathf.Min(count, GetUnlockedTypeCount(levelNumber));
        }

        /// <summary>Bölümde havuzdan seçilebilecek tip sayısı.</summary>
        public int GetUnlockedTypeCount(int levelNumber)
        {
            int unlocked = _startUnlockedTypes + (levelNumber - 1) * _typesUnlockedPerLevel;
            return Mathf.Min(unlocked, _itemPool.Length);
        }

        /// <summary>Bölümün hedeflenen zaman kullanımı.</summary>
        public float GetTargetTimeUsage(int levelNumber)
        {
            float t = Mathf.Clamp01((levelNumber - 1f) / Mathf.Max(1, _levelCount - 1));
            return IsHardLevel(levelNumber)
                ? Mathf.Lerp(_hardTimeUsageStart, _hardTimeUsageEnd, t)
                : Mathf.Lerp(_normalTimeUsageStart, _normalTimeUsageEnd, t);
        }

        // Abramowitz-Stegun 7.1.26; kaybetme oranı tahmini için hassasiyeti (~1e-7) fazlasıyla yeterli.
        private static float NormalCdf(float z)
        {
            float x = Mathf.Abs(z) / Mathf.Sqrt(2f);
            float t = 1f / (1f + 0.3275911f * x);
            float poly = t * (0.254829592f + t * (-0.284496736f + t * (1.421413741f + t * (-1.453152027f + t * 1.061405429f))));
            float erf = 1f - poly * Mathf.Exp(-x * x);
            return 0.5f * (1f + Mathf.Sign(z) * erf);
        }
    }
}
