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
    /// which keeps it deterministic and unit-testable.
    /// </summary>
    public sealed class IdleEconomy
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
        }

        // ---------- Generators ----------

        public int GeneratorCount => Balance.generators.Length;
        public GeneratorDef Def(int i) => Balance.generators[i];
        public int Owned(int i) => State.generators[i].count;
        public bool IsUnlocked(int i) => i == 0 || Owned(i) > 0 || Owned(i - 1) > 0;

        /// <summary>Total cost of buying <paramref name="amount"/> units (geometric series).</summary>
        public double CostOf(int i, int amount)
        {
            if (amount <= 0) return 0;
            var d = Def(i);
            double first = d.baseCost * Math.Pow(d.costGrowth, Owned(i));
            if (d.costGrowth - 1 < 1e-9) return first * amount;
            return first * (Math.Pow(d.costGrowth, amount) - 1) / (d.costGrowth - 1);
        }

        public int MaxAffordable(int i)
        {
            var d = Def(i);
            double first = d.baseCost * Math.Pow(d.costGrowth, Owned(i));
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
            State.generators[i].count += amount;
            return true;
        }

        public double MilestoneMultiplier(int owned)
        {
            double m = 1;
            foreach (int t in Balance.milestones)
            {
                if (owned < t) break;
                m *= Balance.milestoneMultiplier;
            }
            return m;
        }

        /// <summary>Next milestone threshold, or -1 when all are reached.</summary>
        public int NextMilestone(int owned)
        {
            foreach (int t in Balance.milestones)
                if (owned < t) return t;
            return -1;
        }

        // ---------- Production ----------

        public double PrestigeMultiplier => 1 + State.darkMatter * Balance.prestigeBonusPerPoint;
        public double PermanentMultiplier => PrestigeMultiplier * (State.vip ? Balance.vipMultiplier : 1);
        public double GlobalMultiplier(long now) => PermanentMultiplier * (IsBoostActive(now) ? Balance.boostMultiplier : 1);

        public double BaseGeneratorProduction(int i) => Def(i).baseProduction * Owned(i) * MilestoneMultiplier(Owned(i));
        public double GeneratorProduction(int i, long now) => BaseGeneratorProduction(i) * GlobalMultiplier(now);

        double RawProduction()
        {
            double sum = 0;
            for (int i = 0; i < GeneratorCount; i++) sum += BaseGeneratorProduction(i);
            return sum;
        }

        public double ProductionPerSecond(long now) => RawProduction() * GlobalMultiplier(now);

        public double TapValue(long now) =>
            Balance.tapBaseValue * GlobalMultiplier(now) + ProductionPerSecond(now) * Balance.tapProductionShare;

        public double Tap(long now)
        {
            double v = TapValue(now);
            Earn(v);
            return v;
        }

        public void Tick(double deltaSeconds, long now)
        {
            if (deltaSeconds > 0) Earn(ProductionPerSecond(now) * deltaSeconds);
        }

        public void Earn(double amount)
        {
            if (amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
            State.credits += amount;
            State.runEarnings += amount;
            State.totalEarnings += amount;
        }

        // ---------- Offline ----------

        public double OfflineCapSeconds => (State.vip ? Balance.vipOfflineMaxHours : Balance.offlineMaxHours) * 3600;

        public OfflineReport ComputeOffline(long now)
        {
            long since = State.lastSeenUnix;
            if (since <= 0 || now <= since) return OfflineReport.None; // first launch or clock moved back
            double elapsed = now - since;
            if (elapsed < Balance.minOfflineSeconds) return OfflineReport.None;

            double counted = Math.Min(elapsed, OfflineCapSeconds);
            double perSecond = RawProduction() * PermanentMultiplier * Balance.offlineEfficiency;
            double boostedSeconds = Math.Max(0, Math.Min(State.boostEndsAtUnix - since, counted));
            double amount = perSecond * (counted + boostedSeconds * (Balance.boostMultiplier - 1));
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
        public bool CanAddBoost(long now) => BoostRemaining(now) < Balance.boostMaxSeconds - 60;

        public void AddBoost(long now, double seconds)
        {
            long start = Math.Max(State.boostEndsAtUnix, now);
            long end = start + (long)seconds;
            State.boostEndsAtUnix = Math.Min(end, now + (long)Balance.boostMaxSeconds);
        }

        // ---------- Prestige ----------

        public double PendingDarkMatter => Math.Floor(Math.Sqrt(State.runEarnings / Balance.prestigeDivisor));
        public bool CanPrestige => PendingDarkMatter >= 1;

        /// <summary>Earnings needed this run to be able to prestige for the first point.</summary>
        public double PrestigeRequirement => Balance.prestigeDivisor;

        public double Prestige()
        {
            double gain = PendingDarkMatter;
            if (gain < 1) return 0;
            State.darkMatter += gain;
            State.credits = 0;
            State.runEarnings = 0;
            State.prestigeCount++;
            foreach (var g in State.generators) g.count = 0;
            return gain;
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
