using System;

namespace StellarIdle.Core
{
    /// <summary>
    /// Dark Matter skill tree: tiers unlocked by lifetime Dark Matter, one node chosen per tier,
    /// each node levelled with spendable Dark Matter. Respec refunds everything.
    /// </summary>
    public sealed partial class IdleEconomy
    {
        public SkillTierDef[] SkillTiers => Balance.skillTiers;

        public int SkillLevel(SkillNodeDef node)
        {
            foreach (var n in State.skillNodes)
                if (n.id == node.id) return n.level;
            return 0;
        }

        public bool IsTierUnlocked(int tier) => State.darkMatterEarned >= SkillTiers[tier].unlockDarkMatter;

        /// <summary>The node picked in this tier, or null if none yet.</summary>
        public SkillNodeDef ChosenNode(int tier)
        {
            foreach (var node in SkillTiers[tier].nodes)
                if (SkillLevel(node) > 0) return node;
            return null;
        }

        /// <summary>True when another node of the same tier was already chosen.</summary>
        public bool IsSkillBlocked(int tier, SkillNodeDef node)
        {
            var chosen = ChosenNode(tier);
            return chosen != null && chosen != node;
        }

        public bool IsSkillMaxed(SkillNodeDef node) => SkillLevel(node) >= node.maxLevel;

        public double SkillCost(int tier, SkillNodeDef node) => SkillCostAtLevel(tier, SkillLevel(node));

        double SkillCostAtLevel(int tier, int level) =>
            Math.Ceiling(SkillTiers[tier].baseCost * Math.Pow(SkillTiers[tier].costGrowth, level));

        public bool CanBuySkill(int tier, SkillNodeDef node) =>
            IsTierUnlocked(tier) && !IsSkillBlocked(tier, node) && !IsSkillMaxed(node)
            && State.darkMatter >= SkillCost(tier, node);

        public bool TryBuySkill(int tier, SkillNodeDef node)
        {
            if (!CanBuySkill(tier, node)) return false;
            State.darkMatter -= SkillCost(tier, node);
            foreach (var n in State.skillNodes)
            {
                if (n.id != node.id) continue;
                n.level++;
                return true;
            }
            State.skillNodes.Add(new UpgradeLevel { id = node.id, level = 1 });
            return true;
        }

        public int AffordableSkillCount
        {
            get
            {
                int count = 0;
                for (int t = 0; t < SkillTiers.Length; t++)
                    foreach (var node in SkillTiers[t].nodes)
                        if (CanBuySkill(t, node)) count++;
                return count;
            }
        }

        /// <summary>Dark Matter currently invested in the tree (what a respec gives back).</summary>
        public double SkillTreeInvested
        {
            get
            {
                double total = 0;
                for (int t = 0; t < SkillTiers.Length; t++)
                    foreach (var node in SkillTiers[t].nodes)
                        for (int level = 0; level < SkillLevel(node); level++)
                            total += SkillCostAtLevel(t, level);
                return total;
            }
        }

        /// <summary>First respec is free, then it costs crystals.</summary>
        public long RespecCost => State.freeRespecUsed ? Balance.respecCrystalCost : 0;

        public bool CanRespec => State.skillNodes.Count > 0 && State.crystals >= RespecCost;

        public bool TryRespec()
        {
            if (!CanRespec) return false;
            State.crystals -= RespecCost;
            State.darkMatter += SkillTreeInvested;
            State.skillNodes.Clear();
            State.freeRespecUsed = true;
            return true;
        }

        // ---------- Effects ----------

        /// <summary>Sum of value x level over chosen nodes with this effect.</summary>
        double SkillTotal(string effect)
        {
            double total = 0;
            foreach (var tier in SkillTiers)
                foreach (var node in tier.nodes)
                    if (node.effect == effect) total += node.value * SkillLevel(node);
            return total;
        }

        /// <summary>Product of (1 + value)^level, or (1 - value)^level when <paramref name="reduce"/>.</summary>
        double SkillProduct(string effect, bool reduce = false)
        {
            double product = 1;
            foreach (var tier in SkillTiers)
                foreach (var node in tier.nodes)
                    if (node.effect == effect)
                        product *= Math.Pow(reduce ? 1 - node.value : 1 + node.value, SkillLevel(node));
            return product;
        }

        int SkillLevels(string effect, out double value)
        {
            value = 0;
            int levels = 0;
            foreach (var tier in SkillTiers)
                foreach (var node in tier.nodes)
                {
                    if (node.effect != effect || SkillLevel(node) == 0) continue;
                    levels += SkillLevel(node);
                    value = node.value;
                }
            return levels;
        }

        public double TreeProductionMultiplier =>
            (1 + SkillTotal(SkillEffect.Production)) * SkillProduct(SkillEffect.ProductionMult);

        public double TreeTapShare => SkillTotal(SkillEffect.Tap);
        public double TreeTapMultiplier => 1 + SkillTotal(SkillEffect.TapMult);
        public double TreeOfflineHours => SkillTotal(SkillEffect.OfflineHours);
        public double TreeBoostHours => SkillTotal(SkillEffect.BoostHours);

        public double BuildingCostMultiplier => Math.Max(0.1, SkillProduct(SkillEffect.Cost, reduce: true));
        public double OfflineEfficiency => Math.Min(1, Balance.offlineEfficiency + SkillTotal(SkillEffect.OfflineEfficiency));
        public double BoostMultiplier => Balance.boostMultiplier + SkillTotal(SkillEffect.BoostMultiplier);
        public double BonusDropMultiplier => 1 + SkillTotal(SkillEffect.BonusDrop);
        public double BonusDropIntervalMultiplier => Math.Max(0.3, 1 - SkillTotal(SkillEffect.BonusDropRate));
        public int BonusDropMaxPerHour => Balance.bonusDropMaxPerHour + SkillLevels(SkillEffect.BonusDropRate, out _);
        public double ResearchMultiplierValue => Balance.researchMultiplier + SkillTotal(SkillEffect.Research);
        public double MilestoneMultiplierValue => Balance.milestoneMultiplier + SkillTotal(SkillEffect.Milestone);
        public double DarkMatterGainMultiplier => 1 + SkillTotal(SkillEffect.DarkMatterGain);
        public double PrestigeBonusPerPoint => Balance.prestigeBonusPerPoint + SkillTotal(SkillEffect.PrestigeBonus);

        /// <summary>Credits granted at the start of each galaxy (after a warp).</summary>
        public double StartingCredits
        {
            get
            {
                int levels = SkillLevels(SkillEffect.StartCredits, out double value);
                return levels <= 0 ? 0 : value * Math.Pow(10, levels - 1);
            }
        }
    }
}
