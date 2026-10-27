using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    /// <summary>
    /// One place that sums every permanent effect source (skill tree, relics, cosmic upgrades),
    /// cached because production formulas read these values many times per frame.
    /// </summary>
    public sealed partial class IdleEconomy
    {
        readonly Dictionary<string, double> totalCache = new Dictionary<string, double>();
        readonly Dictionary<string, double> productCache = new Dictionary<string, double>();

        /// <summary>Call after anything that changes a tree level, a relic or a cosmic upgrade.</summary>
        void InvalidateEffects()
        {
            totalCache.Clear();
            productCache.Clear();
        }

        static void IncrementLevel(List<UpgradeLevel> levels, string id)
        {
            foreach (var l in levels)
            {
                if (l.id != id) continue;
                l.level++;
                return;
            }
            levels.Add(new UpgradeLevel { id = id, level = 1 });
        }

        static int LevelOf(List<UpgradeLevel> levels, string id)
        {
            foreach (var l in levels)
                if (l.id == id) return l.level;
            return 0;
        }

        /// <summary>Visits (value, level) of every source with this effect.</summary>
        void ForEachEffect(string effect, Action<double, int> visit)
        {
            foreach (var tier in SkillTiers)
                foreach (var node in tier.nodes)
                    if (node.effect == effect) visit(node.value, SkillLevel(node));
            foreach (var galaxy in Balance.galaxies)
                if (galaxy.relicEffect == effect) visit(galaxy.relicValue, RelicLevel(galaxy));
            foreach (var upgrade in Balance.cosmicUpgrades)
                if (upgrade.effect == effect) visit(upgrade.value, CosmicLevel(upgrade));
        }

        /// <summary>Sum of value x level over all sources.</summary>
        double EffectTotal(string effect)
        {
            if (totalCache.TryGetValue(effect, out double cached)) return cached;
            double total = 0;
            ForEachEffect(effect, (value, level) => total += value * level);
            totalCache[effect] = total;
            return total;
        }

        /// <summary>Product of (1 + value)^level, or (1 - value)^level when <paramref name="reduce"/>.</summary>
        double EffectProduct(string effect, bool reduce = false)
        {
            string key = reduce ? effect + "-" : effect;
            if (productCache.TryGetValue(key, out double cached)) return cached;
            double product = 1;
            ForEachEffect(effect, (value, level) => product *= Math.Pow(reduce ? 1 - value : 1 + value, level));
            productCache[key] = product;
            return product;
        }

        int EffectLevels(string effect, out double value)
        {
            double v = 0;
            int levels = 0;
            ForEachEffect(effect, (val, level) =>
            {
                if (level <= 0) return;
                levels += level;
                v = val;
            });
            value = v;
            return levels;
        }

        public double TreeProductionMultiplier =>
            (1 + EffectTotal(SkillEffect.Production)) * EffectProduct(SkillEffect.ProductionMult);

        public double TreeTapShare => EffectTotal(SkillEffect.Tap);
        public double TreeTapMultiplier => 1 + EffectTotal(SkillEffect.TapMult);
        public double AutoTapsPerSecond => TapsDisabled ? 0 : EffectTotal(SkillEffect.AutoTap);
        public double TreeOfflineHours => EffectTotal(SkillEffect.OfflineHours);
        public double TreeBoostHours => EffectTotal(SkillEffect.BoostHours);

        public double BuildingCostMultiplier => Math.Max(0.05, EffectProduct(SkillEffect.Cost, reduce: true));
        public double OfflineEfficiency => Math.Min(1, Balance.offlineEfficiency + EffectTotal(SkillEffect.OfflineEfficiency));
        public double BoostMultiplier => Balance.boostMultiplier + EffectTotal(SkillEffect.BoostMultiplier);
        public double BonusDropMultiplier => 1 + EffectTotal(SkillEffect.BonusDrop);
        public double BonusDropIntervalMultiplier => Math.Max(0.3, 1 - EffectTotal(SkillEffect.BonusDropRate));
        public int BonusDropMaxPerHour => Balance.bonusDropMaxPerHour + EffectLevels(SkillEffect.BonusDropRate, out _);
        public double ResearchMultiplierValue => Balance.researchMultiplier + EffectTotal(SkillEffect.Research);
        public double MilestoneMultiplierValue => Balance.milestoneMultiplier + EffectTotal(SkillEffect.Milestone);
        public double DarkMatterGainMultiplier => 1 + EffectTotal(SkillEffect.DarkMatterGain);
        public double PrestigeBonusPerPoint => Balance.prestigeBonusPerPoint + EffectTotal(SkillEffect.PrestigeBonus);

        public int TreeMaxLevelBonus => (int)EffectTotal(SkillEffect.TreeMaxLevel);
        public bool GenesisUnlocked => EffectTotal(SkillEffect.GenesisTier) > 0;
        public bool AutoBuyUnlocked => EffectTotal(SkillEffect.AutoBuy) > 0;

        /// <summary>Credits granted at the start of each galaxy (after a warp).</summary>
        public double StartingCredits
        {
            get
            {
                int levels = EffectLevels(SkillEffect.StartCredits, out double value);
                return levels <= 0 ? 0 : value * Math.Pow(10, levels - 1);
            }
        }

        /// <summary>Dark Matter granted at the start of each cycle (after a Big Crunch).</summary>
        public double StartingDarkMatter
        {
            get
            {
                int levels = EffectLevels(SkillEffect.StartDarkMatter, out double value);
                return levels <= 0 ? 0 : value * Math.Pow(10, levels - 1);
            }
        }
    }
}
