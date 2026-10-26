using System;

namespace StellarIdle.Core
{
    /// <summary>Dark Matter tree: permanent upgrades bought with prestige currency.</summary>
    public sealed partial class IdleEconomy
    {
        public DarkMatterUpgradeDef[] DarkMatterUpgrades => Balance.darkMatterUpgrades;

        public int DarkMatterLevel(DarkMatterUpgradeDef def)
        {
            foreach (var u in State.darkMatterUpgrades)
                if (u.id == def.id) return u.level;
            return 0;
        }

        public bool IsDarkMatterMaxed(DarkMatterUpgradeDef def) => def.maxLevel > 0 && DarkMatterLevel(def) >= def.maxLevel;

        public double DarkMatterUpgradeCost(DarkMatterUpgradeDef def) =>
            Math.Ceiling(def.baseCost * Math.Pow(def.costGrowth, DarkMatterLevel(def)));

        public bool CanBuyDarkMatterUpgrade(DarkMatterUpgradeDef def) =>
            !IsDarkMatterMaxed(def) && State.darkMatter >= DarkMatterUpgradeCost(def);

        public bool TryBuyDarkMatterUpgrade(DarkMatterUpgradeDef def)
        {
            if (!CanBuyDarkMatterUpgrade(def)) return false;
            State.darkMatter -= DarkMatterUpgradeCost(def);
            foreach (var u in State.darkMatterUpgrades)
            {
                if (u.id != def.id) continue;
                u.level++;
                return true;
            }
            State.darkMatterUpgrades.Add(new UpgradeLevel { id = def.id, level = 1 });
            return true;
        }

        public int AffordableDarkMatterUpgradeCount
        {
            get
            {
                int n = 0;
                foreach (var def in DarkMatterUpgrades)
                    if (CanBuyDarkMatterUpgrade(def)) n++;
                return n;
            }
        }

        /// <summary>Sum of value x level over every upgrade with this effect.</summary>
        double DarkMatterEffectTotal(string effect)
        {
            double total = 0;
            foreach (var def in DarkMatterUpgrades)
                if (def.effect == effect) total += def.value * DarkMatterLevel(def);
            return total;
        }

        int DarkMatterEffectLevel(string effect, out double value)
        {
            value = 0;
            int level = 0;
            foreach (var def in DarkMatterUpgrades)
            {
                if (def.effect != effect) continue;
                level += DarkMatterLevel(def);
                value = def.value;
            }
            return level;
        }

        public double DarkMatterProductionMultiplier => 1 + DarkMatterEffectTotal(DarkMatterEffect.Production);
        public double DarkMatterTapShare => DarkMatterEffectTotal(DarkMatterEffect.Tap);
        public double DarkMatterOfflineHours => DarkMatterEffectTotal(DarkMatterEffect.OfflineHours);
        public double DarkMatterBoostHours => DarkMatterEffectTotal(DarkMatterEffect.BoostHours);
        public double DarkMatterBonusDropMultiplier => 1 + DarkMatterEffectTotal(DarkMatterEffect.BonusDrop);

        public double BuildingCostMultiplier
        {
            get
            {
                int level = DarkMatterEffectLevel(DarkMatterEffect.Cost, out double value);
                return Math.Max(0.1, Math.Pow(1 - value, level));
            }
        }

        /// <summary>Credits granted at the start of each galaxy (after a warp).</summary>
        public double StartingCredits
        {
            get
            {
                int level = DarkMatterEffectLevel(DarkMatterEffect.StartCredits, out double value);
                return level <= 0 ? 0 : value * Math.Pow(10, level - 1);
            }
        }
    }
}
