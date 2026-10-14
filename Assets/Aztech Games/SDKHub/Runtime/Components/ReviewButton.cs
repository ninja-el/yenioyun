using UnityEngine;

namespace SDKHub.Components
{
    [SdkHubHelp("Asks the player to rate the game without leaving it.\n\nCode: SdkManager.Review.RequestReview();")]
    [AddComponentMenu("SDK Hub/Review Button")]
    public class ReviewButton : SdkHubButton
    {
        protected override void OnClicked()
        {
            SdkManager.Review.RequestReview();
        }
    }
}
