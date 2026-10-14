using NUnit.Framework;

namespace SDKHub.Tests
{
    public class AdRemovalTests
    {
        [SetUp]
        public void SetUp()
        {
            AdRemoval.Restore(AdTypes.All);
        }

        [TearDown]
        public void TearDown()
        {
            AdRemoval.Restore(AdTypes.All);
        }

        [Test]
        public void RemovingOneKindLeavesTheOthersOn()
        {
            AdRemoval.Remove(AdTypes.Banner);

            Assert.IsTrue(AdRemoval.IsRemoved(AdFormat.Banner));
            Assert.IsFalse(AdRemoval.IsRemoved(AdFormat.Interstitial));
            Assert.IsFalse(AdRemoval.IsRemoved(AdFormat.Rewarded));
        }

        [Test]
        public void RemovingSeveralKindsWorksTogether()
        {
            AdRemoval.Remove(AdTypes.Banner | AdTypes.Interstitial);

            Assert.IsTrue(AdRemoval.AreRemoved(AdTypes.Banner | AdTypes.Interstitial));
            Assert.IsFalse(AdRemoval.AreRemoved(AdTypes.All));
            Assert.IsFalse(AdRemoval.IsRemoved(AdFormat.Rewarded));
        }

        [Test]
        public void RemoveAllTurnsEveryKindOff()
        {
            AdRemoval.Remove(AdTypes.All);

            Assert.IsTrue(AdRemoval.IsRemoved(AdFormat.Banner));
            Assert.IsTrue(AdRemoval.IsRemoved(AdFormat.Interstitial));
            Assert.IsTrue(AdRemoval.IsRemoved(AdFormat.Rewarded));
            Assert.IsTrue(AdRemoval.AreRemoved(AdTypes.All));
        }

        [Test]
        public void RestoreTurnsOnlyThatKindBackOn()
        {
            AdRemoval.Remove(AdTypes.All);
            AdRemoval.Restore(AdTypes.Banner);

            Assert.IsFalse(AdRemoval.IsRemoved(AdFormat.Banner));
            Assert.IsTrue(AdRemoval.IsRemoved(AdFormat.Interstitial));
            Assert.IsTrue(AdRemoval.IsRemoved(AdFormat.Rewarded));
        }

        [Test]
        public void NothingIsRemovedForAnEmptySelection()
        {
            Assert.IsFalse(AdRemoval.AreRemoved(AdTypes.None));
        }

        [Test]
        public void TheChangedEventFiresOnlyWhenSomethingChanges()
        {
            var calls = 0;
            System.Action handler = () => calls++;
            AdRemoval.Changed += handler;

            AdRemoval.Remove(AdTypes.Banner);
            AdRemoval.Remove(AdTypes.Banner);
            AdRemoval.Restore(AdTypes.Banner);

            AdRemoval.Changed -= handler;
            Assert.AreEqual(2, calls);
        }
    }
}
