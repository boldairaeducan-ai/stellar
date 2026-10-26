using System;

namespace StellarIdle.Core
{
    /// <summary>
    /// Static definition of one generator (a building that produces credits).
    /// Loaded from Assets/Resources/GameBalance.json.
    /// </summary>
    [Serializable]
    public class GeneratorDef
    {
        public string id;
        public string name;
        public double baseCost;
        public double costGrowth = 1.15;
        public double baseProduction;
    }

    /// <summary>
    /// Every tunable number of the game lives here so balancing never requires a code change
    /// (and can later be driven by Remote Config).
    /// </summary>
    [Serializable]
    public class GameBalance
    {
        // Tapping
        public double tapBaseValue = 1;
        public double tapProductionShare = 0.05;

        // Each milestone reached by a generator multiplies its output.
        public int[] milestones = { 10, 25, 50, 100, 200, 300, 400, 500 };
        public double milestoneMultiplier = 2;

        // Offline earnings
        public double offlineEfficiency = 0.5;
        public double offlineMaxHours = 4;
        public double vipOfflineMaxHours = 12;
        public double minOfflineSeconds = 60;

        // Prestige ("Dark Matter")
        public double prestigeDivisor = 1e6;
        public double prestigeBonusPerPoint = 0.02;

        // Rewarded-ad boost
        public double boostMultiplier = 2;
        public double boostDurationSeconds = 4 * 3600;
        public double boostMaxSeconds = 12 * 3600;

        // VIP pass (non-consumable IAP)
        public double vipMultiplier = 2;

        // Premium currency sinks
        public double timeWarpHours = 4;
        public long timeWarpCrystalCost = 50;

        // IAP rewards
        public long crystalsSmallAmount = 100;
        public long crystalsLargeAmount = 650;
        public long starterPackCrystals = 300;
        public double starterPackBoostHours = 8;

        public GeneratorDef[] generators = Array.Empty<GeneratorDef>();

        public void Validate()
        {
            if (generators == null || generators.Length == 0)
                throw new InvalidOperationException("GameBalance: no generators defined.");
            if (milestones == null) milestones = Array.Empty<int>();
            Array.Sort(milestones);
            foreach (var g in generators)
            {
                if (string.IsNullOrEmpty(g.id)) throw new InvalidOperationException("GameBalance: generator without id.");
                if (g.baseCost <= 0 || g.costGrowth < 1 || g.baseProduction < 0)
                    throw new InvalidOperationException($"GameBalance: invalid values for generator '{g.id}'.");
            }
        }
    }
}
