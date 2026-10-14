using UnityEngine;
using UnityEngine.UI;

namespace SDKHub.Components
{
    [RequireComponent(typeof(Button))]
    public abstract class SdkHubButton : SdkHubBehaviour
    {
        CanvasGroup group;
        LayoutElement layout;
        bool layoutIgnoredByDefault;

        protected Button Button { get; private set; }

        protected virtual void Awake()
        {
            Button = GetComponent<Button>();
        }

        protected virtual void OnEnable()
        {
            Button.onClick.AddListener(OnClicked);
        }

        protected virtual void OnDisable()
        {
            Button.onClick.RemoveListener(OnClicked);
        }

        protected abstract void OnClicked();

        // Hides the button without turning the object off, so it can come back later.
        // A hidden button also stops taking space in layout groups.
        protected void SetVisible(bool visible)
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;

            if (layout == null)
            {
                if (visible) return;
                layout = GetComponent<LayoutElement>();
                if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
                layoutIgnoredByDefault = layout.ignoreLayout;
            }
            layout.ignoreLayout = visible ? layoutIgnoredByDefault : true;
        }
    }
}
