using MatchPack.Core;
using MatchPack.Data;
using TMPro;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Bant makinesinin panelindeki kutu sayacı: "hedef kutu / banta giren kutu". Hedef levelin
    /// kutu sayısıdır ve değişmez; sağdaki sayı <see cref="LevelManager"/>'ın yayınladığı sayıdır.
    /// </summary>
    public class BoxCounterView : MonoBehaviour
    {
        [Tooltip("Sayacın yazıldığı alan.")]
        [SerializeField] private TMP_Text _text;

        private void OnEnable()
        {
            if (LevelManager.Instance == null) { return; }

            LevelManager.Instance.OnSpawnedBoxCountChanged += SetSpawnedCount;
            SetSpawnedCount(LevelManager.Instance.SpawnedBoxCount);
        }

        private void OnDisable()
        {
            if (LevelManager.Instance != null) { LevelManager.Instance.OnSpawnedBoxCountChanged -= SetSpawnedCount; }
        }

        private void SetSpawnedCount(int spawnedCount)
        {
            if (_text == null) { return; }

            LevelData level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
            int targetCount = level != null ? level.TargetBoxCount : 0;
            _text.text = $"{targetCount}/{spawnedCount}";
        }
    }
}
