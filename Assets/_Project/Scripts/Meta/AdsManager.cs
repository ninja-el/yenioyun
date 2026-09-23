using System;
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

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
            //if("Check Remove Ads is on/off") interstitialAd.ShowAd(); 
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