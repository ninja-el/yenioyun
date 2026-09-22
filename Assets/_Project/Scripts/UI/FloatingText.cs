using DG.Tweening;
using MatchPack.Core;
using TMPro;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Bir noktadan hedefe uçan tek satırlık UI yazısı ("+5 sn" gibi). Havuzdan alınır, hedefe
    /// varınca kendini havuza iade eder.
    /// </summary>
    public class FloatingText : MonoBehaviour, IPoolable
    {
        [Tooltip("Metnin yazıldığı alan.")]
        [SerializeField] private TMP_Text _text;

        private Tween _flyTween;

        /// <summary>Yazıyı başlangıç noktasından hedefe verilen sürede uçurur.</summary>
        public void Fly(string text, Vector3 from, Vector3 to, float duration, Ease ease)
        {
            _text.text = text;
            transform.position = from;

            _flyTween = transform.DOMove(to, duration).SetEase(ease).OnComplete(ReleaseToPool);
        }

        public void OnSpawned()
        {
            transform.localScale = Vector3.one;
        }

        public void OnDespawned()
        {
            _flyTween?.Kill();
            _flyTween = null;
        }

        private void ReleaseToPool()
        {
            _flyTween = null;
            PoolManager.Instance.Release(gameObject);
        }
    }
}
