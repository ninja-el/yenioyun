using SDKHub.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    // Shows the usage info box (buttons and code) at the top of every SDK Hub component.
    [CustomEditor(typeof(SdkHubBehaviour), true)]
    public class SdkHubBehaviourEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var attributes = target.GetType().GetCustomAttributes(typeof(SdkHubHelpAttribute), false);
            if (attributes.Length > 0)
            {
                var help = new HelpBox(((SdkHubHelpAttribute)attributes[0]).Text, HelpBoxMessageType.Info);
                root.Add(help);
            }

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
