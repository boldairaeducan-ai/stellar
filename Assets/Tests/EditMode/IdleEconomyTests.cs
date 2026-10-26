using NUnit.Framework;
using StellarIdle.Core;

namespace StellarIdle.Tests
{
    public class IdleEconomyTests
    {
        const long Now = 1_700_000_000;

        static GameBalance Balance() => new GameBalance
        {
            researchTiers = new[] { new ResearchTierDef { unlockOwned = 10, costFactor = 50 } },
            globalResearch = new[] { new GlobalResearchDef { cost = 1000, multiplier = 2 } },
            skillTiers = new[]
            {
                new SkillTierDef
                {
                    unlockDarkMatter = 0, baseCost = 1, costGrowth = 2,
                    nodes = new[]
                    {
                        new SkillNodeDef { id = "engines", effect = SkillEffect.Production, value = 0.25, maxLevel = 5 },
                        new SkillNodeDef { id = "logistics", effect = SkillEffect.Cost, value = 0.1, maxLevel = 2 },
                    }
                },
                new SkillTierDef
                {
                    unlockDarkMatter = 10, baseCost = 1, costGrowth = 1,
                    nodes = new[]
                    {
                        new SkillNodeDef { id = "headstart", effect = SkillEffect.StartCredits, value = 1000, maxLevel = 5 },
                        new SkillNodeDef { id = "harvest", effect = SkillEffect.DarkMatterGain, value = 0.5, maxLevel = 5 },
                    }
                },
            },
            achievementOwnedThresholds = new[] { 25 },
            achievementOwnedRewards = new long[] { 5 },
            achievementWarpThresholds = new[] { 1 },
            achievementWarpRewards = new long[] { 10 },
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

        [Test]
        public void Research_MultipliesProduction_AndResetsOnPrestige()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 10); // 10 x 1/s x milestone 2 = 20/s
            var drone = FindResearch(eco, 0);
            Assert.IsTrue(eco.IsResearchUnlocked(drone));
            Assert.IsTrue(eco.TryResearch(drone));
            Assert.IsFalse(eco.TryResearch(drone));
            Assert.AreEqual(60, eco.ProductionPerSecond(Now), 1e-9);

            var global = FindResearch(eco, -1);
            Assert.IsTrue(eco.TryResearch(global));
            Assert.AreEqual(120, eco.ProductionPerSecond(Now), 1e-9);

            eco.Earn(1e8);
            eco.Prestige();
            Assert.IsFalse(eco.IsResearched(drone));
            Assert.AreEqual(1, eco.ResearchMultiplier(0), 1e-9);
        }

        [Test]
        public void Research_LockedUntilEnoughOwned()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 9);
            Assert.IsFalse(eco.CanResearch(FindResearch(eco, 0)));
        }

        [Test]
        public void SkillTree_SpendingKeepsPassiveBonus_AndNodesApply()
        {
            var eco = NewEconomy();
            eco.Earn(16e6);
            Assert.AreEqual(4, eco.Prestige());
            Assert.AreEqual(1.08, eco.PrestigeMultiplier, 1e-9);

            var engines = eco.SkillTiers[0].nodes[0];
            Assert.IsTrue(eco.TryBuySkill(0, engines)); // costs 1
            Assert.IsTrue(eco.TryBuySkill(0, engines)); // costs 2
            Assert.AreEqual(1, eco.State.darkMatter, 1e-9);
            Assert.AreEqual(1.08, eco.PrestigeMultiplier, 1e-9);
            Assert.AreEqual(1.5, eco.TreeProductionMultiplier, 1e-9);
        }

        [Test]
        public void SkillTree_OneChoicePerTier_TiersUnlockWithEarnedDarkMatter()
        {
            var eco = NewEconomy();
            eco.State.darkMatter = 100;
            eco.State.darkMatterEarned = 9;
            var tier0 = eco.SkillTiers[0].nodes;
            Assert.IsTrue(eco.TryBuySkill(0, tier0[1]));
            Assert.IsTrue(eco.IsSkillBlocked(0, tier0[0]));
            Assert.IsFalse(eco.TryBuySkill(0, tier0[0]));
            Assert.IsTrue(eco.TryBuySkill(0, tier0[1]));
            Assert.IsFalse(eco.TryBuySkill(0, tier0[1])); // max level 2
            Assert.AreEqual(0.81, eco.BuildingCostMultiplier, 1e-9);

            Assert.IsFalse(eco.IsTierUnlocked(1));
            eco.State.darkMatterEarned = 10;
            Assert.IsTrue(eco.TryBuySkill(1, eco.SkillTiers[1].nodes[0]));
            Assert.IsTrue(eco.TryBuySkill(1, eco.SkillTiers[1].nodes[0]));
            Assert.AreEqual(10000, eco.StartingCredits, 1e-9);
        }

        [Test]
        public void SkillTree_RespecRefundsEverything_FirstOneFree()
        {
            var eco = NewEconomy();
            eco.State.darkMatter = 10;
            eco.State.crystals = 60;
            var engines = eco.SkillTiers[0].nodes[0];
            eco.TryBuySkill(0, engines); // 1
            eco.TryBuySkill(0, engines); // 2
            eco.TryBuySkill(0, engines); // 4
            Assert.AreEqual(3, eco.State.darkMatter, 1e-9);
            Assert.AreEqual(7, eco.SkillTreeInvested, 1e-9);

            Assert.AreEqual(0, eco.RespecCost);
            Assert.IsTrue(eco.TryRespec());
            Assert.AreEqual(10, eco.State.darkMatter, 1e-9);
            Assert.AreEqual(60, eco.State.crystals);
            Assert.AreEqual(1, eco.TreeProductionMultiplier, 1e-9);

            eco.TryBuySkill(0, eco.SkillTiers[0].nodes[1]);
            Assert.AreEqual(eco.Balance.respecCrystalCost, eco.RespecCost);
            Assert.IsTrue(eco.TryRespec());
            Assert.AreEqual(60 - eco.Balance.respecCrystalCost, eco.State.crystals);
        }

        [Test]
        public void SkillTree_DarkMatterGain_AppliesToWarp()
        {
            var eco = NewEconomy();
            eco.State.darkMatter = 10;
            eco.State.darkMatterEarned = 10;
            eco.TryBuySkill(1, eco.SkillTiers[1].nodes[1]); // +50% Dark Matter
            eco.Earn(4e6); // sqrt(4) = 2 -> x1.5 = 3
            Assert.AreEqual(3, eco.PendingDarkMatter, 1e-9);
        }

        [Test]
        public void Migration_V2UpgradesAreRefunded()
        {
            var state = GameState.CreateNew(Now);
            state.version = 2;
            state.darkMatterEarned = 12;
            state.darkMatter = 2;
            state.darkMatterUpgrades.Add(new UpgradeLevel { id = "engines", level = 3 });
            var eco = new IdleEconomy(Balance(), state);
            Assert.AreEqual(12, eco.State.darkMatter, 1e-9);
            Assert.AreEqual(0, eco.State.darkMatterUpgrades.Count);
        }

        [Test]
        public void Achievements_UseBestCount_AndPayOnce()
        {
            var eco = NewEconomy(1e12);
            eco.TryBuy(0, 25);
            eco.Earn(1e8);
            eco.Prestige(); // count reset, best kept, warp_1 complete
            var own = FindAchievement(eco, AchievementKind.Owned);
            var warp = FindAchievement(eco, AchievementKind.Warps);
            Assert.AreEqual(2, eco.ClaimableAchievementCount);
            long crystals = eco.State.crystals;
            Assert.IsTrue(eco.TryClaimAchievement(own));
            Assert.IsFalse(eco.TryClaimAchievement(own));
            Assert.IsTrue(eco.TryClaimAchievement(warp));
            Assert.AreEqual(crystals + 15, eco.State.crystals);
        }

        [Test]
        public void Migration_V1DarkMatterBecomesEarned()
        {
            var state = GameState.CreateNew(Now);
            state.version = 1;
            state.darkMatter = 7;
            var eco = new IdleEconomy(Balance(), state);
            Assert.AreEqual(7, eco.State.darkMatterEarned, 1e-9);
            Assert.AreEqual(GameState.CurrentVersion, eco.State.version);
        }

        static ResearchItem FindResearch(IdleEconomy eco, int generator)
        {
            foreach (var r in eco.ResearchItems)
                if (r.Generator == generator) return r;
            return null;
        }

        static AchievementDef FindAchievement(IdleEconomy eco, AchievementKind kind)
        {
            foreach (var a in eco.Achievements)
                if (a.Kind == kind && (kind != AchievementKind.Owned || a.Generator == 0)) return a;
            return null;
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
