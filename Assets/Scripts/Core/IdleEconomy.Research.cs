using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    /// <summary>One research: a one-time purchase with credits that multiplies a generator (or all of them).</summary>
    public sealed class ResearchItem
    {
        public string Id;
        public int Generator;   // -1 = all generators
        public int Tier;        // 1-based
        public double Cost;
        public double Multiplier;
        public int UnlockOwned; // generator research only
    }

    public sealed partial class IdleEconomy
    {
        readonly List<ResearchItem> researchItems = new List<ResearchItem>();
        readonly HashSet<string> researched = new HashSet<string>();
        double[] researchMultipliers = Array.Empty<double>();
        double globalResearchMultiplier = 1;

        public IReadOnlyList<ResearchItem> ResearchItems => researchItems;

        void BuildResearch()
        {
            researchItems.Clear();
            for (int g = 0; g < GeneratorCount; g++)
            {
                for (int t = 0; t < Balance.researchTiers.Length; t++)
                {
                    var tier = Balance.researchTiers[t];
                    researchItems.Add(new ResearchItem
                    {
                        Id = $"{Def(g).id}_{t + 1}",
                        Generator = g,
                        Tier = t + 1,
                        Cost = Def(g).baseCost * tier.costFactor,
                        Multiplier = Balance.researchMultiplier,
                        UnlockOwned = tier.unlockOwned,
                    });
                }
            }
            for (int k = 0; k < Balance.globalResearch.Length; k++)
            {
                researchItems.Add(new ResearchItem
                {
                    Id = $"global_{k + 1}",
                    Generator = -1,
                    Tier = k + 1,
                    Cost = Balance.globalResearch[k].cost,
                    Multiplier = Balance.globalResearch[k].multiplier,
                });
            }
            researchItems.Sort((a, b) => a.Cost.CompareTo(b.Cost));

            ReloadResearched();
        }

        /// <summary>Rebuilds the lookup and cached multipliers from the saved list (after load or prestige).</summary>
        void ReloadResearched()
        {
            researched.Clear();
            foreach (var id in State.research) researched.Add(id);

            researchMultipliers = new double[GeneratorCount];
            for (int g = 0; g < researchMultipliers.Length; g++) researchMultipliers[g] = 1;
            globalResearchMultiplier = 1;
            foreach (var item in researchItems)
            {
                if (!researched.Contains(item.Id)) continue;
                if (item.Generator < 0) globalResearchMultiplier *= item.Multiplier;
                else researchMultipliers[item.Generator] *= item.Multiplier;
            }
        }

        public bool IsResearched(ResearchItem item) => researched.Contains(item.Id);

        public bool IsResearchUnlocked(ResearchItem item) => item.Generator < 0 || Owned(item.Generator) >= item.UnlockOwned;

        public bool CanResearch(ResearchItem item) =>
            !IsResearched(item) && IsResearchUnlocked(item) && State.credits >= item.Cost;

        public bool TryResearch(ResearchItem item)
        {
            if (!CanResearch(item)) return false;
            State.credits -= item.Cost;
            State.research.Add(item.Id);
            ReloadResearched();
            return true;
        }

        public int AffordableResearchCount
        {
            get
            {
                int n = 0;
                foreach (var item in researchItems)
                    if (CanResearch(item)) n++;
                return n;
            }
        }

        public double ResearchMultiplier(int generator) => researchMultipliers[generator];
        public double GlobalResearchMultiplier => globalResearchMultiplier;
    }
}
