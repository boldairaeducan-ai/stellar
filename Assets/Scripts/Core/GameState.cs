using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    [Serializable]
    public class OwnedGenerator
    {
        public string id;
        public int count;
    }

    /// <summary>
    /// Everything that is persisted for the player. Plain serializable data, no logic beyond repair.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public double credits;
        public double runEarnings;      // earned since last prestige
        public double totalEarnings;    // earned ever
        public double darkMatter;       // prestige currency
        public long crystals;           // premium currency
        public int prestigeCount;
        public long lastSeenUnix;
        public long boostEndsAtUnix;
        public bool vip;
        public bool starterPackOwned;
        public List<OwnedGenerator> generators = new List<OwnedGenerator>();

        public static GameState CreateNew(long nowUnix) => new GameState { lastSeenUnix = nowUnix };

        /// <summary>
        /// Aligns the saved generator list with the balance file (same order, same ids),
        /// keeping counts of known ids. Lets you add/reorder generators without breaking saves.
        /// </summary>
        public void EnsureGenerators(GeneratorDef[] defs)
        {
            var counts = new Dictionary<string, int>();
            if (generators != null)
                foreach (var g in generators)
                    if (g != null && !string.IsNullOrEmpty(g.id))
                        counts[g.id] = Math.Max(0, g.count);

            generators = new List<OwnedGenerator>(defs.Length);
            foreach (var d in defs)
                generators.Add(new OwnedGenerator { id = d.id, count = counts.TryGetValue(d.id, out var c) ? c : 0 });
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
            if (generators == null) generators = new List<OwnedGenerator>();
        }

        static double Clean(double v) => double.IsNaN(v) || double.IsInfinity(v) || v < 0 ? 0 : v;
    }
}
