using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    [Serializable]
    public class OwnedGenerator
    {
        public string id;
        public int count;
        public int best; // highest count ever reached (kept through prestige, used by achievements)
    }

    [Serializable]
    public class ShipSave
    {
        public string id;
        public int shards;
        public int stars; // 0 = not built yet
    }

    [Serializable]
    public class ExpeditionSave
    {
        public string expeditionId;
        public string shipId;
        public long startUnix;
        public long endUnix;
        public bool adUsed;
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
    }

    /// <summary>
    /// Everything that is persisted for the player. Plain serializable data, no logic beyond repair.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int CurrentVersion = 5;

        public int version = CurrentVersion;
        public double credits;
        public double runEarnings;      // earned since last prestige
        public double totalEarnings;    // earned ever
        public double darkMatter;       // prestige currency, spendable
        public double darkMatterEarned; // all Dark Matter ever earned (drives the passive bonus)
        public long crystals;           // premium currency
        public int prestigeCount;
        public long lastSeenUnix;
        public long boostEndsAtUnix;
        public long bonusDropWindowStartUnix;
        public int bonusDropsInWindow;
        public bool vip;
        public bool starterPackOwned;
        public List<OwnedGenerator> generators = new List<OwnedGenerator>();
        public List<string> research = new List<string>();                 // reset on prestige
        public List<UpgradeLevel> skillNodes = new List<UpgradeLevel>();
        public bool freeRespecUsed;
        public List<UpgradeLevel> darkMatterUpgrades = new List<UpgradeLevel>(); // v2 only, refunded on load
        public List<string> achievementsClaimed = new List<string>();
        public List<ShipSave> ships = new List<ShipSave>();
        public List<string> fleet = new List<string>();                 // equipped ship ids
        public List<ExpeditionSave> expeditions = new List<ExpeditionSave>();
        public bool starterShipGiven;
        public string currentGalaxy = "";
        public List<UpgradeLevel> relics = new List<UpgradeLevel>();       // id = galaxy id
        public double singularities;        // Big Crunch currency, spendable
        public double singularitiesEarned;  // all ever earned (drives the passive bonus)
        public int crunchCount;
        public double crunchStartDarkMatter; // Dark Matter granted at the start of the cycle (not earned by warps)
        public List<UpgradeLevel> cosmicUpgrades = new List<UpgradeLevel>();
        public bool autoBuyEnabled = true;
        public int distressSignalDay;    // local day number of the last claim
        public int distressStreak;       // consecutive days claimed

        public static GameState CreateNew(long nowUnix) => new GameState { lastSeenUnix = nowUnix };

        /// <summary>
        /// Aligns the saved generator list with the balance file (same order, same ids),
        /// keeping counts of known ids. Lets you add/reorder generators without breaking saves.
        /// </summary>
        public void EnsureGenerators(GeneratorDef[] defs)
        {
            var known = new Dictionary<string, OwnedGenerator>();
            if (generators != null)
                foreach (var g in generators)
                    if (g != null && !string.IsNullOrEmpty(g.id))
                        known[g.id] = g;

            generators = new List<OwnedGenerator>(defs.Length);
            foreach (var d in defs)
            {
                int count = known.TryGetValue(d.id, out var old) ? Math.Max(0, old.count) : 0;
                int best = old != null ? Math.Max(old.best, count) : count;
                generators.Add(new OwnedGenerator { id = d.id, count = count, best = best });
            }
        }

        /// <summary>Repairs corrupted or tampered values after loading.</summary>
        public void Sanitize()
        {
            credits = Clean(credits);
            runEarnings = Clean(runEarnings);
            totalEarnings = Clean(totalEarnings);
            darkMatter = Math.Floor(Clean(darkMatter));
            if (crystals < 0) crystals = 0;
            if (prestigeCount < 0) prestigeCount = 0;
            if (bonusDropsInWindow < 0) bonusDropsInWindow = 0;
            darkMatterEarned = Clean(darkMatterEarned);
            if (generators == null) generators = new List<OwnedGenerator>();
            research ??= new List<string>();
            darkMatterUpgrades ??= new List<UpgradeLevel>();
            skillNodes ??= new List<UpgradeLevel>();
            ships ??= new List<ShipSave>();
            fleet ??= new List<string>();
            expeditions ??= new List<ExpeditionSave>();
            relics ??= new List<UpgradeLevel>();
            cosmicUpgrades ??= new List<UpgradeLevel>();
            currentGalaxy ??= "";
            singularities = Clean(singularities);
            singularitiesEarned = Clean(singularitiesEarned);
            achievementsClaimed ??= new List<string>();

            if (version < 2) darkMatterEarned = Math.Max(darkMatterEarned, darkMatter); // v1 had no spending
            if (version < 3 && darkMatterUpgrades.Count > 0)
            {
                // v2 linear upgrades were replaced by the skill tree: refund everything that was spent.
                darkMatter = darkMatterEarned;
                darkMatterUpgrades.Clear();
            }
            if (version < 5)
            {
                // v5 rebalanced Dark Matter (much slower curve, pricier tree): pre-release saves are
                // scaled down and the tree is refunded so the new costs apply.
                darkMatterEarned = Math.Min(darkMatterEarned, 20);
                darkMatter = darkMatterEarned;
                skillNodes.Clear();
            }
            version = CurrentVersion;
        }

        static double Clean(double v) => double.IsNaN(v) || double.IsInfinity(v) || v < 0 ? 0 : v;
    }
}
