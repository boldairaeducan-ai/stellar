using System;

namespace StellarIdle.Core
{
    /// <summary>
    /// Big Crunch: the prestige above the warp. Resets Dark Matter, the skill tree and the run
    /// for Singularities, which give a permanent production bonus and buy cosmic upgrades.
    /// Ships, relics, crystals, goals and purchases are kept.
    /// </summary>
    public sealed partial class IdleEconomy
    {
        public CosmicUpgradeDef[] CosmicUpgrades => Balance.cosmicUpgrades;

        /// <summary>Dark Matter earned by warps during this cycle (the starting grant does not count).</summary>
        public double CycleDarkMatter => Math.Max(0, State.darkMatterEarned - State.crunchStartDarkMatter);

        /// <summary>Dark Matter to earn in this cycle before crunching; grows after each crunch.</summary>
        public double CrunchRequirement =>
            Balance.crunchRequirement * Math.Pow(Balance.crunchRequirementGrowth, State.crunchCount);

        public double PendingSingularities =>
            CycleDarkMatter < CrunchRequirement
                ? 0
                : Math.Floor(Balance.crunchScale * Math.Pow(CycleDarkMatter / CrunchRequirement, Balance.crunchExponent));

        public bool CanCrunch => PendingSingularities >= 1;

        /// <summary>Passive bonus from all Singularities ever earned.</summary>
        public double SingularityMultiplier => 1 + State.singularitiesEarned * Balance.singularityBonus;

        public double Crunch()
        {
            double gain = PendingSingularities;
            if (gain < 1) return 0;

            State.singularities += gain;
            State.singularitiesEarned += gain;
            State.crunchCount++;

            State.skillNodes.Clear();
            State.darkMatterEarned = 0;
            State.darkMatter = 0;
            InvalidateEffects(); // tree cleared: starting values below must not include it
            State.darkMatter = State.darkMatterEarned = State.crunchStartDarkMatter = StartingDarkMatter;

            State.currentGalaxy = Galaxies.Length > 0 ? Galaxies[0].id : "";
            ResetRun();
            return gain;
        }

        // ---------- Cosmic upgrades ----------

        public int CosmicLevel(CosmicUpgradeDef upgrade) => LevelOf(State.cosmicUpgrades, upgrade.id);

        public bool IsCosmicMaxed(CosmicUpgradeDef upgrade) => upgrade.maxLevel > 0 && CosmicLevel(upgrade) >= upgrade.maxLevel;

        public double CosmicCost(CosmicUpgradeDef upgrade) =>
            Math.Ceiling(upgrade.baseCost * Math.Pow(upgrade.costGrowth, CosmicLevel(upgrade)));

        public bool CanBuyCosmic(CosmicUpgradeDef upgrade) => !IsCosmicMaxed(upgrade) && State.singularities >= CosmicCost(upgrade);

        public bool TryBuyCosmic(CosmicUpgradeDef upgrade)
        {
            if (!CanBuyCosmic(upgrade)) return false;
            State.singularities -= CosmicCost(upgrade);
            IncrementLevel(State.cosmicUpgrades, upgrade.id);
            InvalidateEffects();
            return true;
        }

        public int AffordableCosmicCount
        {
            get
            {
                int n = 0;
                foreach (var upgrade in CosmicUpgrades)
                    if (CanBuyCosmic(upgrade)) n++;
                return n;
            }
        }

        // ---------- Automation ----------

        public bool AutoBuyActive => AutoBuyUnlocked && State.autoBuyEnabled;

        /// <summary>Buys as many buildings as possible, most advanced first. Returns true if anything was bought.</summary>
        public bool AutoBuy()
        {
            if (!AutoBuyActive) return false;
            bool bought = false;
            for (int i = MaxGenerators - 1; i >= 0; i--)
            {
                if (!IsUnlocked(i)) continue;
                int amount = MaxAffordable(i);
                if (amount > 0 && TryBuy(i, amount)) bought = true;
            }
            return bought;
        }
    }
}
