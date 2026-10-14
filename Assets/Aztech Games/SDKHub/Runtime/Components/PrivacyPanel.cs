using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SDKHub.Components
{
    [SdkHubHelp("Asks the player for permission before data is collected. The texts are set in the hub (Privacy page). The panel shows itself until the player answers, and the answer is remembered.\n\n" +
                "From any script:\n" +
                "SdkManager.Consent.Agree();   // or Disagree()\n" +
                "PrivacyPanel.ShowPrivacyPanel();   // let the player change their answer")]
    [AddComponentMenu("SDK Hub/Privacy Panel")]
    public class PrivacyPanel : SdkHubBehaviour
    {
        static PrivacyPanel instance;

        [HideInInspector]
        [SerializeField] GameObject panel;
        [HideInInspector]
        [SerializeField] Button agreeButton;
        [HideInInspector]
        [SerializeField] Button disagreeButton;

        [Header("Results")]
        [SerializeField] UnityEvent onAgreed = new UnityEvent();
        [SerializeField] UnityEvent onDisagreed = new UnityEvent();

        // Shows the privacy panel from any script, for example from a settings screen.
        public static void ShowPrivacyPanel()
        {
            if (instance != null) instance.Show();
        }

        void Awake()
        {
            if (panel == null) panel = gameObject;
            instance = this;
        }

        void OnEnable()
        {
            if (agreeButton != null) agreeButton.onClick.AddListener(Agree);
            if (disagreeButton != null) disagreeButton.onClick.AddListener(Disagree);
        }

        void OnDisable()
        {
            if (agreeButton != null) agreeButton.onClick.RemoveListener(Agree);
            if (disagreeButton != null) disagreeButton.onClick.RemoveListener(Disagree);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Start()
        {
            panel.SetActive(!SdkManager.Consent.HasAnswered);
        }

        public void Show()
        {
            panel.SetActive(true);
        }

        public void Agree()
        {
            SdkManager.Consent.Agree();
            panel.SetActive(false);
            SdkManager.Consent.RequestTrackingPermission(_ => { });
            onAgreed.Invoke();
        }

        public void Disagree()
        {
            SdkManager.Consent.Disagree();
            panel.SetActive(false);
            onDisagreed.Invoke();
        }
    }
}
