using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Oyundaki tüm ses referanslarının tek toplandığı yer. AudioManager klipleri buradan okur,
    /// gameplay sınıfları ses dosyası tutmaz.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "MatchPack/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Gameplay SFX")]
        [Tooltip("Obje uygun kutuya gönderildiğinde.")]
        [SerializeField] private AudioClip _itemMatched;

        [Tooltip("Hatalı hamlede.")]
        [SerializeField] private AudioClip _itemMissed;

        [Tooltip("Kutu üçüncü objesini alıp dolduğunda.")]
        [SerializeField] private AudioClip _boxFilled;

        [Header("Level SFX")]
        [Tooltip("Level kazanıldığında.")]
        [SerializeField] private AudioClip _levelCompleted;

        [Tooltip("Süre bittiğinde.")]
        [SerializeField] private AudioClip _levelFailed;

        [Header("UI SFX")]
        [Tooltip("Her butona basıldığında.")]
        [SerializeField] private AudioClip _buttonClick;

        [Tooltip("Satın alma başarıyla tamamlandığında.")]
        [SerializeField] private AudioClip _purchaseSucceeded;

        [Tooltip("Gold veya can eklendiğinde.")]
        [SerializeField] private AudioClip _rewardGranted;

        [Header("Müzik")]
        [Tooltip("Menüde çalan müzik.")]
        [SerializeField] private AudioClip _menuMusic;

        [Tooltip("Level oynanırken çalan müzik.")]
        [SerializeField] private AudioClip _gameMusic;

        public AudioClip ItemMatched => _itemMatched;
        public AudioClip ItemMissed => _itemMissed;
        public AudioClip BoxFilled => _boxFilled;
        public AudioClip LevelCompleted => _levelCompleted;
        public AudioClip LevelFailed => _levelFailed;
        public AudioClip ButtonClick => _buttonClick;
        public AudioClip PurchaseSucceeded => _purchaseSucceeded;
        public AudioClip RewardGranted => _rewardGranted;
        public AudioClip MenuMusic => _menuMusic;
        public AudioClip GameMusic => _gameMusic;
    }
}
