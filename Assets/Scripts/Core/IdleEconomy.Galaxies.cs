using System;

namespace StellarIdle.Core
{
    /// <summary>
    /// Galaxies: the destination picked at each warp. Their modifiers apply to the whole run and
    /// multiply the Dark Matter earned; reaching a galaxy's thresholds grants its permanent relic.
    /// </summary>
    public sealed partial class IdleEconomy
    {
        public GalaxyDef[] Galaxies => Balance.galaxies;

        /// <summary>The galaxy of the current run (the first one by default).</summary>
        public GalaxyDef CurrentGalaxy
        {
            get
            {
                var galaxy = FindGalaxy(State.currentGalaxy);
                return galaxy ?? (Galaxies.Length > 0 ? Galaxies[0] : null);
            }
        }

        public GalaxyDef FindGalaxy(string id) => Array.Find(Galaxies, g => g.id == id);

        public bool IsGalaxyUnlocked(GalaxyDef galaxy) => State.prestigeCount >= galaxy.unlockWarps;

        public bool TapsDisabled => CurrentGalaxy?.noTap ?? false;
        public bool ResearchDisabled => CurrentGalaxy?.noResearch ?? false;
        public bool OfflineDisabled => CurrentGalaxy?.noOffline ?? false;
        double GalaxyCostMultiplier => CurrentGalaxy?.costMultiplier ?? 1;
        double GalaxyProductionMultiplier => CurrentGalaxy?.productionMultiplier ?? 1;
        double GalaxyDarkMatterMultiplier => CurrentGalaxy?.darkMatterMultiplier ?? 1;

        /// <summary>Number of building types available in this galaxy.</summary>
        public int MaxGenerators
        {
            get
            {
                int max = CurrentGalaxy?.maxGenerators ?? 0;
                return max > 0 ? Math.Min(max, GeneratorCount) : GeneratorCount;
            }
        }

        // ---------- Relics ----------

        public int RelicLevel(GalaxyDef galaxy) => LevelOf(State.relics, galaxy.id);

        public int RelicMaxLevel(GalaxyDef galaxy) => galaxy.relicThresholds.Length;

        /// <summary>Relic level this run would reach if the player warped now.</summary>
        public int RelicLevelReachedThisRun(GalaxyDef galaxy)
        {
            int level = 0;
            foreach (double threshold in galaxy.relicThresholds)
                if (State.runEarnings >= threshold) level++;
            return level;
        }

        /// <summary>Earnings needed this run for the next relic level here, or -1 when maxed.</summary>
        public double NextRelicThreshold(GalaxyDef galaxy)
        {
            int level = RelicLevel(galaxy);
            return level < galaxy.relicThresholds.Length ? galaxy.relicThresholds[level] : -1;
        }

        /// <summary>Applied when leaving a galaxy: keeps the best relic level reached there.</summary>
        void AwardRelic(GalaxyDef galaxy)
        {
            if (galaxy == null || string.IsNullOrEmpty(galaxy.relicEffect)) return;
            int reached = RelicLevelReachedThisRun(galaxy);
            int current = RelicLevel(galaxy);
            if (reached <= current) return;
            for (int i = current; i < reached; i++) IncrementLevel(State.relics, galaxy.id);
            LastRelicGalaxy = galaxy;
            InvalidateEffects();
        }

        /// <summary>Galaxy whose relic was gained or upgraded by the last warp, or null.</summary>
        public GalaxyDef LastRelicGalaxy { get; private set; }
    }
}
