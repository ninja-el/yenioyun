using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Tek bir bölümün tanımı. Bölümler ayrı sahne dosyası değildir; GameScene bu asset ile beslenir.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_000", menuName = "MatchPack/Level Data")]
    public class LevelData : ScriptableObject
    {
        /// <summary>Yığına eklenecek tek bir obje tipi ve adedi.</summary>
        [Serializable]
        public class ItemEntry
        {
            [Tooltip("Yığına eklenecek obje tipi.")]
            [SerializeField] private ItemType _type;

            [Tooltip("Bu tipten kaç adet eklenecek.")]
            [SerializeField, Min(0)] private int _count;

            public ItemType Type => _type;
            public int Count => _count;
        }

        [Tooltip("Bölüm numarası. Level_012 asset'i için 12.")]
        [SerializeField, Min(1)] private int _levelIndex = 1;

        [Tooltip("Bölüm süresi (saniye).")]
        [SerializeField, Min(1f)] private float _duration = 60f;

        [Tooltip("Bölümü kazanmak için doldurulması gereken kutu sayısı.")]
        [SerializeField, Min(1)] private int _targetBoxCount = 6;

        [Tooltip("Bantta aynı anda duran kutu sayısı.")]
        [SerializeField, Min(1)] private int _conveyorCapacity = 3;

        [Tooltip("Yığındaki objeler. Toplam adet, kutu hedefi x kutu kapasitesi olmak zorundadır.")]
        [SerializeField] private ItemEntry[] _items;

        public int LevelIndex => _levelIndex;
        public float Duration => _duration;
        public int TargetBoxCount => _targetBoxCount;
        public int ConveyorCapacity => _conveyorCapacity;
        public IReadOnlyList<ItemEntry> Items => _items;

        /// <summary>Yığında oluşturulacak toplam obje adedi.</summary>
        public int TotalItemCount
        {
            get
            {
                if (_items == null) { return 0; }

                int total = 0;
                for (int i = 0; i < _items.Length; i++)
                {
                    total += _items[i].Count;
                }

                return total;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // AssetDatabase import sırasında çağrılamaz; doğrulama bir sonraki editör frame'ine bırakılır.
            UnityEditor.EditorApplication.delayCall -= ValidateContent;
            UnityEditor.EditorApplication.delayCall += ValidateContent;
        }

        private void ValidateContent()
        {
            if (this == null || _items == null) { return; }

            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].Type == null)
                {
                    Debug.LogWarning($"{name}: Items listesinde {i}. satırın obje tipi boş.", this);
                }
            }

            GameConfig config = FindGameConfig();
            if (config == null) { return; }

            int expectedItemCount = _targetBoxCount * config.BoxCapacity;
            if (TotalItemCount != expectedItemCount)
            {
                Debug.LogWarning(
                    $"{name}: toplam obje sayısı {TotalItemCount}, olması gereken {expectedItemCount} " +
                    $"({_targetBoxCount} kutu x {config.BoxCapacity} kapasite). Artık obje kalamaz.",
                    this);
            }
        }

        private static GameConfig FindGameConfig()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GameConfig");
            if (guids.Length == 0) { return null; }

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(path);
        }
#endif
    }
}
