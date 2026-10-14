using System;
using UnityEngine;

namespace SDKHub.Components
{
    // Shown at the top of a component's Inspector as an info box.
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SdkHubHelpAttribute : Attribute
    {
        public readonly string Text;

        public SdkHubHelpAttribute(string text)
        {
            Text = text;
        }
    }

    public abstract class SdkHubBehaviour : MonoBehaviour
    {
    }
}
