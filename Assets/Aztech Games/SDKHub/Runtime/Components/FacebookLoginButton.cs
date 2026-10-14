using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SDKHub.Components
{
    [SdkHubHelp("Logs in and out with Facebook and changes its own text. The permissions are set in the hub (Facebook page).\n\n" +
                "From any script:\n" +
                "SdkManager.Facebook.Login(success => { });")]
    [AddComponentMenu("SDK Hub/Facebook Login Button")]
    public class FacebookLoginButton : SdkHubButton
    {
        TMP_Text label;
        IFacebookService facebook;

        [Header("Results")]
        [SerializeField] UnityEvent onLoggedIn = new UnityEvent();
        [SerializeField] UnityEvent onLoginFailed = new UnityEvent();
        [SerializeField] UnityEvent onLoggedOut = new UnityEvent();

        static string[] Permissions
        {
            get
            {
                var config = SdkHubConfig.Instance;
                var text = config != null ? config.Get("facebook", "permissions") : "";
                if (string.IsNullOrEmpty(text)) return new[] { "public_profile", "email" };
                var parts = text.Split(',');
                for (var i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
                return parts;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            label = GetComponentInChildren<TMP_Text>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            facebook = SdkManager.Facebook;
            facebook.Initialized += Refresh;
            facebook.Initialize();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (facebook != null) facebook.Initialized -= Refresh;
        }

        protected override void OnClicked()
        {
            if (facebook.IsLoggedIn)
            {
                facebook.Logout();
                Refresh();
                onLoggedOut.Invoke();
                return;
            }

            facebook.Login(success =>
            {
                Refresh();
                if (success) onLoggedIn.Invoke();
                else onLoginFailed.Invoke();
            }, Permissions);
        }

        void Refresh()
        {
            Button.interactable = facebook.IsInitialized;
            if (label != null) label.text = facebook.IsLoggedIn ? "Log out" : "Log in with Facebook";
        }
    }
}
