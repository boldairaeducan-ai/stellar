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

    /// <summary>Per-generator research tier: unlocked at N owned, costs baseCost x costFactor.</summary>
    [Serializable]
    public class ResearchTierDef
    {
        public int unlockOwned;
        public double costFactor;
    }

    /// <summary>Research boosting every generator.</summary>
    [Serializable]
    public class GlobalResearchDef
    {
        public double cost;
        public double multiplier = 2;
    }

    /// <summary>One node of the Dark Matter skill tree. Only one node per tier can be chosen.</summary>
    [Serializable]
    public class SkillNodeDef
    {
        public string id;
        public string name;
        public string branch;   // see SkillBranch
        public string effect;   // see SkillEffect
        public double value;    // per level
        public int maxLevel = 5;
    }

    /// <summary>A tier of the tree: unlocked by Dark Matter earned (lifetime), nodes get stronger with each tier.</summary>
    [Serializable]
    public class SkillTierDef
    {
        public string name;
        public double unlockDarkMatter;
        public double baseCost = 1;
        public double costGrowth = 1.6;
        public bool requiresGenesis; // hidden until the "Genesis" cosmic upgrade is bought
        public SkillNodeDef[] nodes = Array.Empty<SkillNodeDef>();
    }

    public static class SkillBranch
    {
        public const string Industry = "industry";
        public const string Command = "command";
        public const string Logistics = "logistics";
    }

    public static class SkillEffect
    {
        public const string Production = "production";               // +value x level production (additive)
        public const string ProductionMult = "productionMult";       // x(1 + value) per level (multiplicative)
        public const string Tap = "tap";                             // +value x level share of production per tap
        public const string TapMult = "tapMult";                     // tap value +value x level
        public const string Cost = "cost";                           // buildings cost x(1 - value) per level
        public const string StartCredits = "startCredits";           // value x 10^(level-1) credits after each warp
        public const string OfflineHours = "offlineHours";           // +value x level hours of offline cap
        public const string OfflineEfficiency = "offlineEfficiency"; // +value x level offline efficiency (max 100%)
        public const string BoostHours = "boostHours";               // +value x level hours per ad boost
        public const string BoostMultiplier = "boostMultiplier";     // ad boost multiplier +value x level
        public const string BonusDrop = "bonusDrop";                 // cargo pod reward +value x level
        public const string BonusDropRate = "bonusDropRate";         // pods value x level more often, +1/hour cap per level
        public const string Research = "research";                   // research multiplier +value x level
        public const string Milestone = "milestone";                 // milestone multiplier +value x level
        public const string DarkMatterGain = "darkMatterGain";       // Dark Matter from warps +value x level
        public const string PrestigeBonus = "prestigeBonus";         // passive bonus per Dark Matter +value x level
        public const string AutoTap = "autoTap";                     // +value x level automatic taps per second

        // Cosmic upgrades only (Big Crunch)
        public const string TreeMaxLevel = "treeMaxLevel";           // +value x level max level of every tree node
        public const string GenesisTier = "genesisTier";             // unlocks the tree tiers marked requiresGenesis
        public const string AutoBuy = "autoBuy";                     // buys buildings automatically
        public const string StartDarkMatter = "startDarkMatter";     // value x 10^(level-1) Dark Matter after each crunch
    }

    /// <summary>Ship rarity: drop weight, shards to build the ship and to reach each extra star.</summary>
    [Serializable]
    public class ShipRarityDef
    {
        public string id;
        public string name;
        public double weight;          // relative chance per shard roll
        public int unlockShards;       // to build the ship (1 star)
        public int[] starCosts = Array.Empty<int>(); // shards for 2, 3, 4, 5 stars
    }

    /// <summary>A collectible ship. Its bonus applies while it is in the active fleet.</summary>
    [Serializable]
    public class ShipDef
    {
        public string id;
        public string name;
        public string rarity;    // ShipRarityDef.id
        public string role;      // see ShipRole
        public string generator; // miner only: GeneratorDef.id it boosts
        public double value;     // bonus at 1 star
    }

    public static class ShipRole
    {
        public const string Miner = "miner";         // +value production of one building
        public const string Cruiser = "cruiser";     // +value tap power
        public const string Freighter = "freighter"; // +value cargo pod credits
        public const string Scout = "scout";         // +value offline earnings
        public const string Flagship = "flagship";   // +value production of everything
        public const string Harbinger = "harbinger"; // +value Dark Matter from warps
    }

    /// <summary>Timed mission a ship (not in the active fleet) can be sent on.</summary>
    [Serializable]
    public class ExpeditionDef
    {
        public string id;
        public string name;
        public double minutes;
        public int rolls;               // shard rolls (+ bonus by ship rarity)
        public long crystals;
        public double productionMinutes; // credits = this many minutes of production x ship stars
    }

    /// <summary>
    /// Destination chosen at each warp. Modifiers make the run harder and pay more Dark Matter;
    /// reaching the relic thresholds in a galaxy grants (or levels up) its permanent relic.
    /// </summary>
    [Serializable]
    public class GalaxyDef
    {
        public string id;
        public string name;
        public int unlockWarps;               // lifetime warps needed to travel there
        public double darkMatterMultiplier = 1;
        public double costMultiplier = 1;
        public double productionMultiplier = 1;
        public int maxGenerators;             // 0 = all buildings
        public bool noTap;
        public bool noResearch;
        public bool noOffline;
        public string relicName;
        public string relicEffect;            // a SkillEffect, applied value x relic level
        public double relicValue;
        public double[] relicThresholds = Array.Empty<double>(); // credits earned in one run there, per relic level
    }

    /// <summary>Permanent upgrade bought with Singularities (Big Crunch currency).</summary>
    [Serializable]
    public class CosmicUpgradeDef
    {
        public string id;
        public string name;
        public string effect; // a SkillEffect
        public double value;
        public double baseCost = 1;
        public double costGrowth = 2;
        public int maxLevel = 1;
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

        // Prestige ("Dark Matter"): scale x (run earnings / divisor)^exponent
        public double prestigeDivisor = 1e6;
        public double prestigeExponent = 0.5;
        public double prestigeScale = 1;
        public double prestigeBonusPerPoint = 0.02;

        // Rewarded-ad boost
        public double boostMultiplier = 2;
        public double boostDurationSeconds = 4 * 3600;
        public double boostMaxSeconds = 12 * 3600;

        // Bonus drops: cargo pods that appear on screen, opened with a rewarded ad.
        // Reward = N seconds of production (without the x2 boost), with a floor for the very start.
        public double bonusDropProductionSeconds = 300;
        public double bonusDropMinReward = 50;
        public double bonusDropFirstDelaySeconds = 120;
        public double bonusDropMinIntervalSeconds = 240;
        public double bonusDropMaxIntervalSeconds = 480;
        public double bonusDropLifetimeSeconds = 25;
        public int bonusDropMaxPerHour = 6;

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

        // Research (credits, reset on prestige)
        public double researchMultiplier = 3;
        public ResearchTierDef[] researchTiers = Array.Empty<ResearchTierDef>();
        public GlobalResearchDef[] globalResearch = Array.Empty<GlobalResearchDef>();

        // Dark Matter skill tree (permanent)
        public SkillTierDef[] skillTiers = Array.Empty<SkillTierDef>();
        public long respecCrystalCost = 50;

        // Achievements (crystal rewards)
        public int[] achievementOwnedThresholds = Array.Empty<int>();
        public long[] achievementOwnedRewards = Array.Empty<long>();
        public double[] achievementEarningsThresholds = Array.Empty<double>();
        public long[] achievementEarningsRewards = Array.Empty<long>();
        public int[] achievementWarpThresholds = Array.Empty<int>();
        public long[] achievementWarpRewards = Array.Empty<long>();
        public int[] achievementShipsBuiltThresholds = Array.Empty<int>();
        public long[] achievementShipsBuiltRewards = Array.Empty<long>();
        public long[] achievementRarityCompleteRewards = Array.Empty<long>(); // one per rarity, in shipRarities order
        public int[] achievementShipsMaxedThresholds = Array.Empty<int>();
        public long[] achievementShipsMaxedRewards = Array.Empty<long>();

        // Galaxies and relics
        public GalaxyDef[] galaxies = Array.Empty<GalaxyDef>();

        // Big Crunch (second prestige): Singularities = scale x (Dark Matter earned this cycle / requirement)^exponent
        public double crunchRequirement = 10000;
        public double crunchExponent = 0.5;
        public double crunchScale = 1;
        public double crunchRequirementGrowth = 1; // the requirement is multiplied by this after each crunch
        public double singularityBonus = 0.25; // +25% production per Singularity ever earned
        public CosmicUpgradeDef[] cosmicUpgrades = Array.Empty<CosmicUpgradeDef>();

        // Ships, fleet and expeditions
        public ShipRarityDef[] shipRarities = Array.Empty<ShipRarityDef>();
        public ShipDef[] ships = Array.Empty<ShipDef>();
        public ExpeditionDef[] expeditions = Array.Empty<ExpeditionDef>();
        public int[] fleetSlotWarps = { 0, 0, 0, 1, 5 }; // one slot per entry, unlocked at that many warps
        public int expeditionSlots = 3;
        public int shardsPerRoll = 5;
        public double shipStarBonus = 0.5;   // each star above 1 adds 50% of the base bonus
        public int warpChestRolls = 3;
        public double bonusDropShardChance = 0.3;
        public double expeditionAdSpeedup = 0.5; // a rewarded ad cuts the remaining time by half
        public string starterShip = "";

        // Daily distress signal (rewarded ad): shard rolls once per local day, more with a streak.
        public int distressSignalRolls = 2;
        public int[] distressStreakDays = { 3, 7 }; // +1 roll from each of these consecutive-day streaks

        public GeneratorDef[] generators = Array.Empty<GeneratorDef>();

        public void Validate()
        {
            if (generators == null || generators.Length == 0)
                throw new InvalidOperationException("GameBalance: no generators defined.");
            if (milestones == null) milestones = Array.Empty<int>();
            Array.Sort(milestones);
            researchTiers ??= Array.Empty<ResearchTierDef>();
            globalResearch ??= Array.Empty<GlobalResearchDef>();
            skillTiers ??= Array.Empty<SkillTierDef>();
            galaxies ??= Array.Empty<GalaxyDef>();
            foreach (var galaxy in galaxies) galaxy.relicThresholds ??= Array.Empty<double>();
            cosmicUpgrades ??= Array.Empty<CosmicUpgradeDef>();
            shipRarities ??= Array.Empty<ShipRarityDef>();
            ships ??= Array.Empty<ShipDef>();
            expeditions ??= Array.Empty<ExpeditionDef>();
            fleetSlotWarps ??= Array.Empty<int>();
            foreach (var ship in ships)
                if (Array.FindIndex(shipRarities, r => r.id == ship.rarity) < 0)
                    throw new InvalidOperationException($"GameBalance: ship '{ship.id}' has unknown rarity '{ship.rarity}'.");
            foreach (var tier in skillTiers) tier.nodes ??= Array.Empty<SkillNodeDef>();
            achievementOwnedThresholds ??= Array.Empty<int>();
            achievementOwnedRewards ??= Array.Empty<long>();
            achievementEarningsThresholds ??= Array.Empty<double>();
            achievementEarningsRewards ??= Array.Empty<long>();
            achievementWarpThresholds ??= Array.Empty<int>();
            achievementWarpRewards ??= Array.Empty<long>();
            achievementShipsBuiltThresholds ??= Array.Empty<int>();
            achievementShipsBuiltRewards ??= Array.Empty<long>();
            achievementRarityCompleteRewards ??= Array.Empty<long>();
            achievementShipsMaxedThresholds ??= Array.Empty<int>();
            achievementShipsMaxedRewards ??= Array.Empty<long>();
            distressStreakDays ??= Array.Empty<int>();
            if (achievementOwnedRewards.Length < achievementOwnedThresholds.Length
                || achievementEarningsRewards.Length < achievementEarningsThresholds.Length
                || achievementWarpRewards.Length < achievementWarpThresholds.Length
                || achievementShipsBuiltRewards.Length < achievementShipsBuiltThresholds.Length
                || achievementShipsMaxedRewards.Length < achievementShipsMaxedThresholds.Length)
                throw new InvalidOperationException("GameBalance: each achievement threshold needs a reward.");
            foreach (var g in generators)
            {
                if (string.IsNullOrEmpty(g.id)) throw new InvalidOperationException("GameBalance: generator without id.");
                if (g.baseCost <= 0 || g.costGrowth < 1 || g.baseProduction < 0)
                    throw new InvalidOperationException($"GameBalance: invalid values for generator '{g.id}'.");
            }
        }
    }
}
