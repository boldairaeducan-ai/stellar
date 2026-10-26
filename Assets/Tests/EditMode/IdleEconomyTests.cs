using NUnit.Framework;
using StellarIdle.Core;

namespace StellarIdle.Tests
{
    public class IdleEconomyTests
    {
        const long Now = 1_700_000_000;

        static GameBalance Balance() => new GameBalance
        {
            generators = new[]
            {
                new GeneratorDef { id = "a", name = "A", baseCost = 10, costGrowth = 1.15, baseProduction = 1 },
                new GeneratorDef { id = "b", name = "B", baseCost = 100, costGrowth = 1.15, baseProduction = 10 },
            }
        };

        static IdleEconomy NewEconomy(double credits = 0)
        {
            var state = GameState.CreateNew(Now);
            state.credits = credits;
            return new IdleEconomy(Balance(), state);
        }

        [Test]
        public void BulkCost_MatchesSumOfSingleCosts()
        {
            var eco = NewEconomy(1e9);
            double expected = 0;
            for (int k = 0; k < 10; k++) expected += 10 * System.Math.Pow(1.15, k);
            Assert.AreEqual(expected, eco.CostOf(0, 10), 1e-6);
        }

        [Test]
        public void MaxAffordable_NeverExceedsCredits()
        {
            var eco = NewEconomy(12345);
            int n = eco.MaxAffordable(0);
            Assert.Greater(n, 0);
            Assert.LessOrEqual(eco.CostOf(0, n), 12345);
            Assert.Greater(eco.CostOf(0, n + 1), 12345);
        }

        [Test]
        public void TryBuy_SpendsCreditsAndAddsUnits()
        {
            var eco = NewEconomy(100);
            Assert.IsTrue(eco.TryBuy(0, 1));
            Assert.AreEqual(1, eco.Owned(0));
            Assert.AreEqual(90, eco.State.credits, 1e-9);
            Assert.IsFalse(eco.TryBuy(1, 1));
        }

        [Test]
        public void Milestones_DoubleProduction()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 10);
            Assert.AreEqual(10 * 1 * 2, eco.ProductionPerSecond(Now), 1e-9);
        }

        [Test]
        public void Boost_DoublesProduction_AndIsCapped()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 1);
            double before = eco.ProductionPerSecond(Now);
            eco.AddBoost(Now, 4 * 3600);
            Assert.AreEqual(before * 2, eco.ProductionPerSecond(Now), 1e-9);
            for (int k = 0; k < 10; k++) eco.AddBoost(Now, 4 * 3600);
            Assert.AreEqual(eco.Balance.boostMaxSeconds, eco.BoostRemaining(Now), 1e-9);
            Assert.IsFalse(eco.CanAddBoost(Now));
        }

        [Test]
        public void Offline_IsCappedAndUsesEfficiency()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 1); // 1/s
            var report = eco.CollectOffline(Now + 100 * 3600);
            Assert.IsTrue(report.Capped);
            Assert.AreEqual(4 * 3600 * 0.5, report.Amount, 1e-6);
            Assert.AreEqual(Now + 100 * 3600, eco.State.lastSeenUnix);
        }

        [Test]
        public void Offline_IgnoresShortAbsenceAndClockRollback()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 1);
            Assert.IsFalse(eco.ComputeOffline(Now + 10).HasEarnings);
            Assert.IsFalse(eco.ComputeOffline(Now - 5000).HasEarnings);
        }

        [Test]
        public void Prestige_ResetsRunAndAwardsDarkMatter()
        {
            var eco = NewEconomy();
            eco.Earn(4e6);
            eco.TryBuy(0, 5);
            Assert.AreEqual(2, eco.PendingDarkMatter);
            Assert.AreEqual(2, eco.Prestige());
            Assert.AreEqual(0, eco.State.credits);
            Assert.AreEqual(0, eco.Owned(0));
            Assert.AreEqual(1.04, eco.PrestigeMultiplier, 1e-9);
        }

        [Test]
        public void GrantProduct_NonConsumablesAreIdempotent()
        {
            var eco = NewEconomy();
            Assert.IsTrue(eco.GrantProduct(ProductIds.StarterPack, Now));
            Assert.IsTrue(eco.GrantProduct(ProductIds.StarterPack, Now));
            Assert.AreEqual(eco.Balance.starterPackCrystals, eco.State.crystals);
            Assert.IsTrue(eco.GrantProduct(ProductIds.CrystalsSmall, Now));
            Assert.AreEqual(eco.Balance.starterPackCrystals + eco.Balance.crystalsSmallAmount, eco.State.crystals);
            Assert.IsFalse(eco.GrantProduct("unknown", Now));
        }

        [Test]
        public void EnsureGenerators_KeepsCountsWhenBalanceChanges()
        {
            var state = GameState.CreateNew(Now);
            state.generators.Add(new OwnedGenerator { id = "b", count = 3 });
            state.generators.Add(new OwnedGenerator { id = "removed", count = 9 });
            var eco = new IdleEconomy(Balance(), state);
            Assert.AreEqual(0, eco.Owned(0));
            Assert.AreEqual(3, eco.Owned(1));
            Assert.AreEqual(2, state.generators.Count);
        }

        [Test]
        public void BonusDrop_GivesProductionMinutes_WithFloor_AndIgnoresBoost()
        {
            var eco = NewEconomy();
            Assert.AreEqual(eco.Balance.bonusDropMinReward, eco.BonusDropReward, 1e-9);

            eco.Earn(1e9);
            eco.TryBuy(1, 1); // 10/s
            eco.AddBoost(Now, 3600);
            Assert.AreEqual(10 * eco.Balance.bonusDropProductionSeconds, eco.BonusDropReward, 1e-6);

            double before = eco.State.credits;
            double reward = eco.ClaimBonusDrop(Now);
            Assert.AreEqual(reward, eco.State.credits - before, 1e-6);
        }

        [Test]
        public void BonusDrop_IsCappedPerHour()
        {
            var eco = NewEconomy();
            for (int k = 0; k < eco.Balance.bonusDropMaxPerHour; k++)
                Assert.Greater(eco.ClaimBonusDrop(Now + k), 0);
            Assert.IsFalse(eco.CanSpawnBonusDrop(Now + 100));
            Assert.AreEqual(0, eco.ClaimBonusDrop(Now + 100));
            Assert.IsTrue(eco.CanSpawnBonusDrop(Now + 3600));
            Assert.Greater(eco.ClaimBonusDrop(Now + 3600), 0);
        }

        [TestCase(0, "0")]
        [TestCase(5.55, "5.5")]
        [TestCase(999, "999")]
        [TestCase(1234, "1.23K")]
        [TestCase(45_600_000, "45.6M")]
        [TestCase(999_999, "999K")]
        [TestCase(1e15, "1.00aa")]
        [TestCase(2.5e18, "2.50ab")]
        public void NumberFormat_UsesSuffixes(double value, string expected)
        {
            Assert.AreEqual(expected, NumberFormat.Format(value));
        }

        [Test]
        public void NumberFormat_Duration()
        {
            Assert.AreEqual("2h 05m", NumberFormat.Duration(2 * 3600 + 5 * 60));
            Assert.AreEqual("45s", NumberFormat.Duration(45));
        }
    }
}
