using NUnit.Framework;

namespace SDKHub.Tests
{
    public class InterstitialPolicyTests
    {
        [Test]
        public void FirstRequestIsAllowedWhenThereAreNoRules()
        {
            var policy = new InterstitialPolicy(0f, 0f);
            Assert.IsNull(policy.Check("level_end", 1, 0f));
        }

        [Test]
        public void StartDelayBlocksEarlyRequests()
        {
            var policy = new InterstitialPolicy(30f, 0f);
            StringAssert.Contains("start delay", policy.Check("a", 1, 10f));
            Assert.IsNull(policy.Check("a", 1, 31f));
        }

        [Test]
        public void CooldownBlocksTheNextAdUntilItHasPassed()
        {
            var policy = new InterstitialPolicy(0f, 60f);
            Assert.IsNull(policy.Check("a", 1, 0f));
            policy.MarkShown("a", 0f);

            StringAssert.Contains("cooldown", policy.Check("a", 1, 30f));
            Assert.IsNull(policy.Check("a", 1, 61f));
        }

        [Test]
        public void CooldownIsSharedBetweenPlacements()
        {
            var policy = new InterstitialPolicy(0f, 60f);
            policy.MarkShown("level_end", 0f);
            StringAssert.Contains("cooldown", policy.Check("after_action", 1, 10f));
        }

        [Test]
        public void EveryNthShowsOnlyOnTheNthRequest()
        {
            var policy = new InterstitialPolicy(0f, 0f);
            Assert.IsNotNull(policy.Check("a", 3, 1f));
            Assert.IsNotNull(policy.Check("a", 3, 2f));
            Assert.IsNull(policy.Check("a", 3, 3f));

            policy.MarkShown("a", 3f);
            Assert.IsNotNull(policy.Check("a", 3, 4f));
        }

        [Test]
        public void EachPlacementCountsOnItsOwn()
        {
            var policy = new InterstitialPolicy(0f, 0f);
            Assert.IsNotNull(policy.Check("a", 2, 1f));
            Assert.IsNotNull(policy.Check("b", 2, 1f));
            Assert.IsNull(policy.Check("a", 2, 2f));
        }

        [Test]
        public void ADueAdWaitsForTheCooldownAndThenShows()
        {
            var policy = new InterstitialPolicy(0f, 60f);
            Assert.IsNotNull(policy.Check("a", 2, 0f));
            Assert.IsNull(policy.Check("a", 2, 1f));
            policy.MarkShown("a", 1f);

            Assert.IsNotNull(policy.Check("a", 2, 2f));
            StringAssert.Contains("cooldown", policy.Check("a", 2, 3f));
            Assert.IsNull(policy.Check("a", 2, 70f));
        }

        [Test]
        public void AnAdThatWasNotShownStaysDue()
        {
            var policy = new InterstitialPolicy(0f, 0f);
            Assert.IsNotNull(policy.Check("a", 2, 0f));
            Assert.IsNull(policy.Check("a", 2, 1f));
            Assert.IsNull(policy.Check("a", 2, 2f));
        }

        [Test]
        public void ZeroOrNegativeEveryNthMeansEveryRequest()
        {
            var policy = new InterstitialPolicy(0f, 0f);
            Assert.IsNull(policy.Check("a", 0, 0f));
            Assert.IsNull(policy.Check("a", -5, 0f));
        }
    }
}
