using System;

namespace StellarIdle.Core
{
    public readonly struct OfflineReport
    {
        public readonly double Seconds;
        public readonly double Amount;
        public readonly bool Capped;

        public OfflineReport(double seconds, double amount, bool capped)
        {
            Seconds = seconds;
            Amount = amount;
            Capped = capped;
        }

        public bool HasEarnings => Amount > 0;
        public static OfflineReport None => default;

        public OfflineReport Merge(OfflineReport other) =>
            new OfflineReport(Seconds + other.Seconds, Amount + other.Amount, Capped || other.Capped);
    }

    /// <summary>
    /// All game rules. Pure C# (no UnityEngine), time is always passed in as unix seconds,
    /// which keeps it deterministic and unit-testable. Split by system across IdleEconomy.*.cs files.
    /// </summary>
    public sealed partial class IdleEconomy
    {
        const int MaxBulkBuy = 100000;

        public GameBalance Balance { get; }
        public GameState State { get; }

        public IdleEconomy(GameBalance balance, GameState state)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Balance.Validate();
            State.Sanitize();
            State.EnsureGenerators(Balance.generators);
            BuildResearch();
            BuildAchievements();
            InitFleet();
        }

        // ---------- Generators ----------

        public int GeneratorCount => Balance.generators.Length;
        public GeneratorDef Def(int i) => Balance.generators[i];
        public int Owned(int i) => State.generators[i].count;
        public bool IsUnlocked(int i) => i < MaxGenerators && (i == 0 || Owned(i) > 0 || Owned(i - 1) > 0);

        /// <summary>Price of the next single unit.</summary>
        double NextUnitCost(int i) =>
            Def(i).baseCost * BuildingCostMultiplier * GalaxyCostMultiplier * Math.Pow(Def(i).costGrowth, Owned(i));

        /// <summary>Total cost of buying <paramref name="amount"/> units (geometric series).</summary>
        public double CostOf(int i, int amount)
        {
            if (amount <= 0) return 0;
            var d = Def(i);
            double first = NextUnitCost(i);
            if (d.costGrowth - 1 < 1e-9) return first * amount;
            return first * (Math.Pow(d.costGrowth, amount) - 1) / (d.costGrowth - 1);
        }

        public int MaxAffordable(int i)
        {
            var d = Def(i);
            double first = NextUnitCost(i);
            if (State.credits < first) return 0;

            double n = d.costGrowth - 1 < 1e-9
                ? Math.Floor(State.credits / first)
                : Math.Floor(Math.Log(State.credits * (d.costGrowth - 1) / first + 1) / Math.Log(d.costGrowth));
            int count = (int)Math.Min(n, MaxBulkBuy);
            while (count > 0 && CostOf(i, count) > State.credits) count--; // floating point guard
            return count;
        }

        /// <summary>Buy amount for a buy mode: a positive number, or 0 meaning "max affordable" (at least 1 to show a price).</summary>
        public int ResolveBuyAmount(int i, int mode) => mode > 0 ? mode : Math.Max(1, MaxAffordable(i));

        public bool CanAfford(int i, int amount) => amount > 0 && CostOf(i, amount) <= State.credits;

        public bool TryBuy(int i, int amount)
        {
            if (!CanAfford(i, amount)) return false;
            State.credits -= CostOf(i, amount);
            var g = State.generators[i];
            g.count += amount;
            g.best = Math.Max(g.best, g.count);
            return true;
        }

        public double MilestoneMultiplier(int owned)
        {
            double m = 1;
            foreach (int t in Balance.milestones)
            {
                if (owned < t) break;
                m *= MilestoneMultiplierValue;
            }
            return m;
        }

        /// <summary>Previous milestone threshold (0 if none), for progress bars.</summary>
        public int PreviousMilestone(int owned)
        {
            int previous = 0;
            foreach (int t in Balance.milestones)
            {
                if (owned < t) break;
                previous = t;
            }
            return previous;
        }

        /// <summary>Next milestone threshold, or -1 when all are reached.</summary>
        public int NextMilestone(int owned)
        {
            foreach (int t in Balance.milestones)
                if (owned < t) return t;
            return -1;
        }

        // ---------- Production ----------

        /// <summary>Passive bonus from all Dark Matter ever earned (spending it in the tree costs nothing).</summary>
        public double PrestigeMultiplier => 1 + State.darkMatterEarned * PrestigeBonusPerPoint;

        public double PermanentMultiplier =>
            PrestigeMultiplier * SingularityMultiplier * TreeProductionMultiplier * FleetGlobalMultiplier
            * GalaxyProductionMultiplier * (State.vip ? Balance.vipMultiplier : 1);

        public double GlobalMultiplier(long now) => PermanentMultiplier * (IsBoostActive(now) ? BoostMultiplier : 1);

        /// <summary>Generator output before global multipliers: units x milestones x research.</summary>
        public double BaseGeneratorProduction(int i) =>
            Def(i).baseProduction * Owned(i) * MilestoneMultiplier(Owned(i)) * ResearchMultiplier(i) * GlobalResearchMultiplier * FleetGeneratorMultiplier(i);
        public double GeneratorProduction(int i, long now) => BaseGeneratorProduction(i) * GlobalMultiplier(now);

        double RawProduction()
        {
            double sum = 0;
            for (int i = 0; i < GeneratorCount; i++) sum += BaseGeneratorProduction(i);
            return sum;
        }

        public double ProductionPerSecond(long now) => RawProduction() * GlobalMultiplier(now);

        public double TapValue(long now) => TapsDisabled ? 0 :
            (Balance.tapBaseValue * GlobalMultiplier(now) + ProductionPerSecond(now) * (Balance.tapProductionShare + TreeTapShare))
            * TreeTapMultiplier * FleetTapMultiplier;

        public double Tap(long now)
        {
            double v = TapValue(now);
            Earn(v);
            return v;
        }

        /// <summary>Credits per second including automatic taps (skill tree "autoTap").</summary>
        public double IncomePerSecond(long now) => ProductionPerSecond(now) + TapValue(now) * AutoTapsPerSecond;

        public void Tick(double deltaSeconds, long now)
        {
            if (deltaSeconds > 0) Earn(IncomePerSecond(now) * deltaSeconds);
        }

        public void Earn(double amount)
        {
            if (amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
            State.credits += amount;
            State.runEarnings += amount;
            State.totalEarnings += amount;
        }

        // ---------- Offline ----------

        public double OfflineCapSeconds =>
            ((State.vip ? Balance.vipOfflineMaxHours : Balance.offlineMaxHours) + TreeOfflineHours) * 3600;

        public OfflineReport ComputeOffline(long now)
        {
            long since = State.lastSeenUnix;
            if (since <= 0 || now <= since) return OfflineReport.None; // first launch or clock moved back
            double elapsed = now - since;
            if (elapsed < Balance.minOfflineSeconds || OfflineDisabled) return OfflineReport.None;

            double counted = Math.Min(elapsed, OfflineCapSeconds);
            double perSecond = RawProduction() * PermanentMultiplier * OfflineEfficiency;
            double boostedSeconds = Math.Max(0, Math.Min(State.boostEndsAtUnix - since, counted));
            double amount = perSecond * (counted + boostedSeconds * (BoostMultiplier - 1)) * FleetOfflineMultiplier;
            return new OfflineReport(elapsed, amount, elapsed > OfflineCapSeconds);
        }

        /// <summary>Computes offline earnings (not yet credited) and marks the player as seen now.</summary>
        public OfflineReport CollectOffline(long now)
        {
            var report = ComputeOffline(now);
            MarkSeen(now);
            return report;
        }

        public void MarkSeen(long now) => State.lastSeenUnix = now;

        // ---------- Boost (rewarded ad) ----------

        public bool IsBoostActive(long now) => State.boostEndsAtUnix > now;
        public double BoostRemaining(long now) => Math.Max(0, State.boostEndsAtUnix - now);
        /// <summary>Duration given by one ad boost (Dark Matter "boostHours" extends it).</summary>
        public double BoostDurationSeconds => Balance.boostDurationSeconds + TreeBoostHours * 3600;
        public double BoostMaxSeconds => Balance.boostMaxSeconds + TreeBoostHours * 3600 * 3;
        public bool CanAddBoost(long now) => BoostRemaining(now) < BoostMaxSeconds - 60;

        public void AddBoost(long now, double seconds)
        {
            long start = Math.Max(State.boostEndsAtUnix, now);
            long end = start + (long)seconds;
            State.boostEndsAtUnix = Math.Min(end, now + (long)BoostMaxSeconds);
        }

        // ---------- Bonus drops (rewarded ad) ----------

        const long BonusDropWindowSeconds = 3600;

        /// <summary>Credits a bonus drop gives right now: a few minutes of production, never less than the floor.</summary>
        public double BonusDropReward =>
            Math.Max(Balance.bonusDropMinReward, RawProduction() * PermanentMultiplier * Balance.bonusDropProductionSeconds)
            * BonusDropMultiplier * FleetBonusDropMultiplier;

        /// <summary>False once the hourly cap is reached (counted per rolling one-hour window, persisted).</summary>
        public bool CanSpawnBonusDrop(long now) =>
            now - State.bonusDropWindowStartUnix >= BonusDropWindowSeconds || State.bonusDropsInWindow < BonusDropMaxPerHour;

        public double ClaimBonusDrop(long now)
        {
            if (!CanSpawnBonusDrop(now)) return 0;
            if (now - State.bonusDropWindowStartUnix >= BonusDropWindowSeconds || now < State.bonusDropWindowStartUnix)
            {
                State.bonusDropWindowStartUnix = now;
                State.bonusDropsInWindow = 0;
            }
            State.bonusDropsInWindow++;
            double reward = BonusDropReward;
            Earn(reward);
            LastBonusDropShards = Rng.NextDouble() < Balance.bonusDropShardChance ? RollShards(1) : NoShards;
            return reward;
        }

        // ---------- Prestige ----------

        /// <summary>Dark Matter multipliers (tree, fleet, galaxy) applied on top of the base curve.</summary>
        double DarkMatterMultipliers => DarkMatterGainMultiplier * FleetDarkMatterMultiplier * GalaxyDarkMatterMultiplier;

        /// <summary>scale x (run earnings / divisor)^exponent: grows slowly with each order of magnitude.</summary>
        public double PendingDarkMatter =>
            Math.Floor(Balance.prestigeScale * Math.Pow(State.runEarnings / Balance.prestigeDivisor, Balance.prestigeExponent)
                       * DarkMatterMultipliers);

        public bool CanPrestige => PendingDarkMatter >= 1;

        /// <summary>Earnings needed this run to be able to warp for the first point.</summary>
        public double PrestigeRequirement =>
            Balance.prestigeDivisor * Math.Pow(1 / (Balance.prestigeScale * DarkMatterMultipliers), 1 / Balance.prestigeExponent);

        /// <summary>Production multiplier the player would have right after warping now.</summary>
        public double PrestigeMultiplierAfterWarp =>
            1 + (State.darkMatterEarned + PendingDarkMatter) * PrestigeBonusPerPoint;

        /// <summary>Warps to <paramref name="nextGalaxyId"/> (or stays in the same galaxy when null or locked).</summary>
        public double Prestige(string nextGalaxyId = null)
        {
            double gain = PendingDarkMatter;
            if (gain < 1) return 0;
            LastRelicGalaxy = null;
            AwardRelic(CurrentGalaxy);
            State.darkMatter += gain;
            State.darkMatterEarned += gain;

            // Destinations are those unlocked before this warp (what the player was offered).
            var next = FindGalaxy(nextGalaxyId);
            if (next != null && IsGalaxyUnlocked(next)) State.currentGalaxy = next.id;
            State.prestigeCount++;

            ResetRun();
            LastWarpChest = RollShards(Balance.warpChestRolls);
            return gain;
        }

        /// <summary>Clears what one run builds (credits, buildings, research) and grants the starting credits.</summary>
        void ResetRun()
        {
            State.runEarnings = 0;
            foreach (var g in State.generators) g.count = 0;
            State.research.Clear();
            ReloadResearched();
            // Without taps the first building could never be bought: always afford it.
            State.credits = TapsDisabled ? Math.Max(StartingCredits, NextUnitCost(0)) : StartingCredits;
        }

        // ---------- Premium currency ----------

        public double TimeWarpValue => RawProduction() * PermanentMultiplier * Balance.timeWarpHours * 3600;

        public bool TryTimeWarp(out double gained)
        {
            gained = 0;
            if (State.crystals < Balance.timeWarpCrystalCost || TimeWarpValue <= 0) return false;
            State.crystals -= Balance.timeWarpCrystalCost;
            gained = TimeWarpValue;
            Earn(gained);
            return true;
        }

        /// <summary>
        /// Delivers an IAP. Must be idempotent for non-consumables (restores call it again).
        /// Returns false for unknown products.
        /// </summary>
        public bool GrantProduct(string productId, long now)
        {
            switch (productId)
            {
                case ProductIds.VipPass:
                    State.vip = true;
                    return true;
                case ProductIds.StarterPack:
                    if (State.starterPackOwned) return true;
                    State.starterPackOwned = true;
                    State.crystals += Balance.starterPackCrystals;
                    AddBoost(now, Balance.starterPackBoostHours * 3600);
                    return true;
                case ProductIds.CrystalsSmall:
                    State.crystals += Balance.crystalsSmallAmount;
                    return true;
                case ProductIds.CrystalsLarge:
                    State.crystals += Balance.crystalsLargeAmount;
                    return true;
                default:
                    return false;
            }
        }
    }
}
