using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class PrivacyManager : MonoBehaviour
{
    public GameObject gdprPanel; 
    public bool IsConsentProcessCompleted { get; private set; } = false;

    public async Task RequestConsentAsync()
    {
        IsConsentProcessCompleted = false;

        int gdprConsent = PlayerPrefs.GetInt("GDPR_Consent", 0);

        if (gdprConsent == 0)
        {
            if (gdprPanel != null) gdprPanel.SetActive(true);

            while (PlayerPrefs.GetInt("GDPR_Consent", 0) == 0)
            {
                await Task.Yield();
            }

            if (gdprPanel != null) gdprPanel.SetActive(false);
        }

#if UNITY_IOS
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                await Task.Yield();
            }
        }
#endif
        IsConsentProcessCompleted = true;
    }

    public void OnAcceptGDPRClicked()
    {
        PlayerPrefs.SetInt("GDPR_Consent", 1);
        PlayerPrefs.Save();
        Debug.Log("Kullanıcı GDPR onayını KABUL ETTİ.");
    }

    public void OnDeclineGDPRClicked()
    {
        PlayerPrefs.SetInt("GDPR_Consent", -1);
        PlayerPrefs.Save();
        Debug.Log("Kullanıcı GDPR onayını REDDETTİ.");
    }
}