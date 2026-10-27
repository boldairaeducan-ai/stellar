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

        public bool IsTierUnlocked(int tier) =>
            State.darkMatterEarned >= SkillTiers[tier].unlockDarkMatter && (!SkillTiers[tier].requiresGenesis || GenesisUnlocked);

        /// <summary>Tiers reserved to the Genesis cosmic upgrade stay hidden until it is bought.</summary>
        public bool IsTierVisible(int tier) => !SkillTiers[tier].requiresGenesis || GenesisUnlocked;

        /// <summary>Max level of a node, raised by the "Expanded Mind" cosmic upgrade.</summary>
        public int SkillMaxLevel(SkillNodeDef node) => node.maxLevel + TreeMaxLevelBonus;

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

        public bool IsSkillMaxed(SkillNodeDef node) => SkillLevel(node) >= SkillMaxLevel(node);

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
            IncrementLevel(State.skillNodes, node.id);
            InvalidateEffects();
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
            InvalidateEffects();
            return true;
        }
    }
}
