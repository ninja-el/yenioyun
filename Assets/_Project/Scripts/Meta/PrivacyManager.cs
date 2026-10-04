using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class PrivacyManager : MonoBehaviour
{
    public GameObject gdprPanel;

    [Tooltip("GDPR onayını kabul eden buton (Agree).")]
    [SerializeField] private Button _acceptButton;

    [Tooltip("GDPR onayını reddeden buton (Disagree).")]
    [SerializeField] private Button _declineButton;

    public bool IsConsentProcessCompleted { get; private set; } = false;

    private void Awake()
    {
        if (_acceptButton != null) { _acceptButton.onClick.AddListener(OnAcceptGDPRClicked); }
        if (_declineButton != null) { _declineButton.onClick.AddListener(OnDeclineGDPRClicked); }
    }

    private void OnDestroy()
    {
        if (_acceptButton != null) { _acceptButton.onClick.RemoveListener(OnAcceptGDPRClicked); }
        if (_declineButton != null) { _declineButton.onClick.RemoveListener(OnDeclineGDPRClicked); }
    }

    private bool _hasAnsweredPanel;

    /// <summary>
    /// GDPR panelini ve iOS'ta ATT izin penceresini sırayla gösterip cevabı bekler. ATT cevaplanmadıkça panel
    /// her açılışta tekrar gösterilir. Token iptal edilirse bekleme bırakılır ve panel kapanır.
    /// </summary>
    public async Task RequestConsentAsync(CancellationToken cancellationToken)
    {
        IsConsentProcessCompleted = false;
        _hasAnsweredPanel = false;

        if (PlayerPrefs.GetInt("GDPR_Consent", 0) == 0 || IsTrackingUndetermined())
        {
            if (gdprPanel != null) gdprPanel.SetActive(true);

            while (!_hasAnsweredPanel && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
            }

            if (gdprPanel != null) gdprPanel.SetActive(false);
        }

#if UNITY_IOS && !UNITY_EDITOR
        if (IsTrackingUndetermined() && !cancellationToken.IsCancellationRequested)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            while (IsTrackingUndetermined() && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }
#endif
        IsConsentProcessCompleted = !cancellationToken.IsCancellationRequested;
    }

    // The ATT binding reports NOT_DETERMINED in the Editor too, which would block every Editor run.
    private static bool IsTrackingUndetermined()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
#else
        return false;
#endif
    }

    public void OnAcceptGDPRClicked()
    {
        PlayerPrefs.SetInt("GDPR_Consent", 1);
        PlayerPrefs.Save();
        _hasAnsweredPanel = true;
        Debug.Log("Kullanıcı GDPR onayını KABUL ETTİ.");
    }

    public void OnDeclineGDPRClicked()
    {
        PlayerPrefs.SetInt("GDPR_Consent", -1);
        PlayerPrefs.Save();
        _hasAnsweredPanel = true;
        Debug.Log("Kullanıcı GDPR onayını REDDETTİ.");
    }
}
