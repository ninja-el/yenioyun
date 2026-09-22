using DG.Tweening;
using MatchPack.Core;
using MatchPack.Gameplay;
using MatchPack.Localization;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Ek Süre booster'ının görseli. Booster kullanılınca butonun üstünden "+X sn" yazısını süre
    /// göstergesine uçurur, süre eklenince göstergeyi büyütüp küçültür. Süreyi eklemek ve
    /// zamanlamak <see cref="TimeBonusBooster"/>'ın işidir; bu bileşen yalnızca event dinler.
    /// </summary>
    public class TimeBonusView : MonoBehaviour
    {
        [Tooltip("Event'leri dinlenen Ek Süre booster'ı.")]
        [SerializeField] private TimeBonusBooster _booster;

        [Tooltip("Yazının çıktığı nokta. Ek Süre butonu bağlanır.")]
        [SerializeField] private RectTransform _source;

        [Tooltip("Yazının uçtuğu ve süre eklenince büyüyüp küçülen süre yazısı.")]
        [SerializeField] private RectTransform _target;

        [Tooltip("Uçan yazının prefab'ı. Havuzdan alınır.")]
        [SerializeField] private FloatingText _textPrefab;

        [Tooltip("Uçan yazıların parent edildiği katman. Butonların ve süre yazısının üstünde çizilmeli.")]
        [SerializeField] private RectTransform _layer;

        [Tooltip("Yazının localization key'i. {0} eklenen saniyedir.")]
        [SerializeField] private string _textKey = "ui.booster.timebonus.fly";

        [Tooltip("Uçuşun hız eğrisi. Uçuş süresi Booster_TimeBonus asset'inden gelir.")]
        [SerializeField] private Ease _flyEase = Ease.InQuad;

        [Tooltip("Süre yazısının büyürken ulaşacağı ölçek çarpanı.")]
        [SerializeField, Min(1f)] private float _punchScale = 1.3f;

        [Tooltip("Büyüyüp küçülmenin toplam süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.3f;

        private Vector3 _targetBaseScale;
        private Tween _punchTween;

        private void Awake()
        {
            if (_target != null) { _targetBaseScale = _target.localScale; }
        }

        private void OnEnable()
        {
            if (_booster == null) { return; }

            _booster.OnBonusLaunched += HandleBonusLaunched;
            _booster.OnBonusApplied += HandleBonusApplied;
        }

        private void OnDisable()
        {
            if (_booster != null)
            {
                _booster.OnBonusLaunched -= HandleBonusLaunched;
                _booster.OnBonusApplied -= HandleBonusApplied;
            }

            ResetPunch();
        }

        private void HandleBonusLaunched(float seconds, float flyDuration)
        {
            if (_textPrefab == null || _source == null || _target == null || _layer == null) { return; }

            GameObject instance = PoolManager.Instance.Get(_textPrefab.gameObject);
            if (instance == null) { return; }

            instance.transform.SetParent(_layer, false);

            FloatingText floatingText = instance.GetComponent<FloatingText>();
            string text = Loc.Format(_textKey, Mathf.RoundToInt(seconds));
            floatingText.Fly(text, _source.position, _target.position, flyDuration, _flyEase);
        }

        private void HandleBonusApplied(float seconds)
        {
            if (_target == null) { return; }

            ResetPunch();
            _punchTween = _target
                .DOScale(_targetBaseScale * _punchScale, _punchDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo);
        }

        private void ResetPunch()
        {
            _punchTween?.Kill();
            _punchTween = null;

            if (_target != null) { _target.localScale = _targetBaseScale; }
        }
    }
}
