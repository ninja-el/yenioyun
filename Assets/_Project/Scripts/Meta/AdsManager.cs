using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.LevelPlay;
using MatchPack.Core;
using MatchPack.Data;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Tooltip("Bölüm sonu interstitial olasılığının okunduğu config.")]
    [SerializeField] private GameConfig _config;

    [Header("App Key")]
    [SerializeField] private string androidAppKey;
    [SerializeField] private string IOSAppKey;

    [Header("Interstitial Ad Unit ID")]
    [SerializeField] private string androidInterstitialAdUnitID;
    [SerializeField] private string IOSInterstitialAdUnitID;

    [Header("Rewarded Ad Unit ID")]
    [SerializeField] private string androidRewardedAdUnitID;
    [SerializeField] private string IOSRewardedAdUnitID;

    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    private Action onRewardSuccessCallBack;

    private GameManager _boundGameManager;

    private string appKey =>
#if UNITY_ANDROID
        androidAppKey;
#elif UNITY_IOS
        IOSAppKey;
#else
        string.Empty;
#endif

    private string InterstitialAdUnitID =>
#if UNITY_ANDROID
        androidInterstitialAdUnitID;
#elif UNITY_IOS
        IOSInterstitialAdUnitID;
#else
        string.Empty;
#endif
    
    private string RewardedAdUnitID =>
#if UNITY_ANDROID
        androidRewardedAdUnitID;
#elif UNITY_IOS
        IOSRewardedAdUnitID;
#else
        string.Empty;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindGameManager();
    }

    // AdsManager BootScene'de, GameManager MainScene'de doğar; bağlantı sahne yüklenince kurulur.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this || GameManager.Instance == null || GameManager.Instance == _boundGameManager) return;

        UnbindGameManager();
        _boundGameManager = GameManager.Instance;
        _boundGameManager.OnLevelCompleted += TryShowLevelEndInterstitial;
        _boundGameManager.OnLevelFailed += TryShowLevelEndInterstitial;
    }

    private void UnbindGameManager()
    {
        if (_boundGameManager == null) return;

        _boundGameManager.OnLevelCompleted -= TryShowLevelEndInterstitial;
        _boundGameManager.OnLevelFailed -= TryShowLevelEndInterstitial;
        _boundGameManager = null;
    }

    private void TryShowLevelEndInterstitial()
    {
        if (_config == null)
        {
            Debug.LogWarning("AdsManager: GameConfig is not assigned, level end interstitial skipped.");
            return;
        }

        if (UnityEngine.Random.value < _config.InterstitialChance) ShowInterstitialAd();
    }

    public void Start()
    {
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        LevelPlay.Init(appKey);
        LevelPlay.SetPauseGame(true);
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError obj)
    {
        Debug.LogError("Ads SDK Initialization Failed: " + obj);
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration obj)
    {
        Debug.Log("Ads SDK initialization completed.");
        CreateInterstitialAd();
        CreateRewardedAd();

    }

    #region Interstitial Ads

    private void CreateInterstitialAd()
    {
        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitID);
        interstitialAd.OnAdLoadFailed += (error) => Invoke(nameof(LoadInterstitialAd), 5f);;
        interstitialAd.OnAdClosed += (info) => LoadInterstitialAd();;

        LoadInterstitialAd();
    }

    public void LoadInterstitialAd()
    {
        if (interstitialAd != null) interstitialAd.LoadAd();
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            if (!SaveManager.Instance.Data.HasRemovedAds) interstitialAd.ShowAd();
            return;
        }
        else
        {
            Debug.LogWarning("Interstitial Ad is not ready!");
            LoadInterstitialAd();
        }
    }
    
    #endregion

    #region Rewarded Ads 

    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitID);
        rewardedAd.OnAdLoadFailed += (error) => Invoke(nameof(LoadRewardedAd), 5f);
        rewardedAd.OnAdRewarded += OnAdRewardedEvent;
        rewardedAd.OnAdClosed += (info) => LoadRewardedAd();

        LoadRewardedAd();
    }

    public void LoadRewardedAd() => rewardedAd?.LoadAd();
    
    public void ShowRewardedAd(Action onSuccess)
    {
#if UNITY_EDITOR
        // Editor'de reklam yüklenmediği için ödül doğrudan verilir.
        onSuccess?.Invoke();
#else
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            onRewardSuccessCallBack = onSuccess;
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Rewarded Coin Ad is not ready!");
            LoadRewardedAd();
        }
#endif
    }

    private void OnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
    {
        onRewardSuccessCallBack?.Invoke();
        onRewardSuccessCallBack = null;
    }
    #endregion

    
    private void OnDestroy()
    {
        interstitialAd?.DestroyAd();
        rewardedAd?.DestroyAd();
    }
}