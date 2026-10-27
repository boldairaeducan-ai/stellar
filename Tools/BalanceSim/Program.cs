using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using StellarIdle.Core;

// Simulates an engaged player with the real economy code.
// Assumptions: plays actively (taps 2/s) for SessionHours per day, idle the rest (offline earnings),
// buys the best building/research greedily, warps when Dark Matter would grow enough,
// spends Dark Matter on the cheapest node of a chosen branch, crunches when possible.
class Sim
{
    static void Main(string[] args)
    {
        string branch = args.Length > 0 ? args[0] : "industry";
        double days = args.Length > 1 ? double.Parse(args[1]) : 60;
        double sessionHours = 2;

        var opts = new JsonSerializerOptions { IncludeFields = true };
        var balance = JsonSerializer.Deserialize<GameBalance>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../../Assets/Resources/GameBalance.json")), opts);
        long now = 1_700_000_000;
        var eco = new IdleEconomy(balance, GameState.CreateNew(now)) { Rng = new Random(1) };

        double runStart = 0, t = 0; // seconds of game time
        string Tiers() { int n = 0; for (int i = 0; i < eco.SkillTiers.Length; i++) if (eco.IsTierUnlocked(i)) n++; return n.ToString(); }
        Console.WriteLine($"branch={branch}");
        Console.WriteLine("day   hours | warp# galaxy     | runEarn   DM+    DMearned DMbank | tiers | Sg  crunch");
        double dayLen = 86400;
        while (t < days * dayLen)
        {
            double dayTime = t % dayLen;
            bool active = dayTime < sessionHours * 3600;
            double dt = active ? 2 : 0;
            if (active)
            {
                eco.Tick(dt, now);
                for (int k = 0; k < 4; k++) eco.Tap(now); // 2 taps/s
                t += dt; now += (long)dt;
                Shop(eco);
            }
            else
            {
                // jump to next session: offline earnings
                double offlineSeconds = dayLen - dayTime;
                eco.MarkSeen(now);
                now += (long)offlineSeconds; t += offlineSeconds;
                var rep = eco.ComputeOffline(now); eco.Earn(rep.Amount); eco.MarkSeen(now);
                Shop(eco);
                continue;
            }

            // Warp decision: pending big enough relative to what we have, and run lasted a bit.
            double pending = eco.PendingDarkMatter;
            double runTime = t - runStart;
            bool worth = pending >= Math.Max(1, eco.State.darkMatterEarned * 0.4) || (pending >= 1 && runTime > 6 * 3600);
            if (worth && runTime > 600)
            {
                string next = BestGalaxy(eco);
                double run = eco.State.runEarnings;
                if (eco.CurrentGalaxy == eco.Galaxies[0]) lastRun = run;
                double gain = eco.Prestige(next);
                runStart = t;
                SpendDm(eco, branch);
                var g = eco.CurrentGalaxy?.name ?? "-";
                Console.WriteLine($"{t / dayLen,5:0.0} {ActiveHours(t, sessionHours),6:0.0} | {eco.State.prestigeCount,4} {g,-10} | {run,8:0.0e0} {gain,6:0} {eco.State.darkMatterEarned,8:0} {eco.State.darkMatter,6:0} | {Tiers(),5} | {eco.State.singularitiesEarned,3:0}");
                if (eco.CanCrunch)
                {
                    double sg = eco.Crunch();
                    while (eco.CosmicUpgrades.Any(u => eco.TryBuyCosmic(u))) { }
                    SpendDm(eco, branch);
                    Console.WriteLine($"      *** BIG CRUNCH +{sg} Sg (total {eco.State.singularitiesEarned}) ***");
                }
            }
        }
        Console.WriteLine($"Relics: {string.Join(", ", eco.Galaxies.Select(g => g.id + ":" + eco.RelicLevel(g)))}");
        Console.WriteLine($"Tree: {string.Join(", ", eco.State.skillNodes.Select(n => n.id + ":" + n.level))}");
    }

    static double ActiveHours(double t, double session) => Math.Floor(t / 86400) * session + Math.Min(t % 86400, session * 3600) / 3600;

    static double lastRun;
    static readonly System.Collections.Generic.Dictionary<string, double> tried = new System.Collections.Generic.Dictionary<string, double>();
    static string BestGalaxy(IdleEconomy eco)
    {
        // Try a relic galaxy when the last Milky Way run was well beyond its next threshold;
        // don't retry the same threshold until the player is 100x stronger.
        foreach (var g in eco.Galaxies.Where(eco.IsGalaxyUnlocked).OrderByDescending(g => g.darkMatterMultiplier))
        {
            double next = eco.NextRelicThreshold(g);
            if (string.IsNullOrEmpty(g.relicEffect) || next <= 0 || lastRun < next * 100) continue;
            if (tried.TryGetValue(g.id + next, out double at) && lastRun < at * 100) continue;
            tried[g.id + next] = lastRun;
            return g.id;
        }
        return eco.Galaxies[0].id;
    }

    static void SpendDm(IdleEconomy eco, string branch)
    {
        bool bought = true;
        while (bought)
        {
            bought = false;
            for (int t = 0; t < eco.SkillTiers.Length && !bought; t++)
                foreach (var n in eco.SkillTiers[t].nodes)
                    if (n.branch == branch && eco.TryBuySkill(t, n)) { bought = true; break; }
        }
    }

    static void Shop(IdleEconomy eco)
    {
        for (int guard = 0; guard < 200; guard++)
        {
            // research first when affordable (big multipliers)
            var r = eco.ResearchItems.FirstOrDefault(eco.CanResearch);
            if (r != null) { eco.TryResearch(r); continue; }
            // best building by production gained per credit, buy 1
            int best = -1; double bestRatio = 0;
            for (int i = 0; i < eco.GeneratorCount; i++)
            {
                if (!eco.IsUnlocked(i) || !eco.CanAfford(i, 1)) continue;
                double ratio = (eco.Def(i).baseProduction * eco.ResearchMultiplier(i) + 1e-12) / eco.CostOf(i, 1);
                if (eco.NextMilestone(eco.Owned(i)) == eco.Owned(i) + 1) ratio *= 1 + eco.Owned(i);
                if (ratio > bestRatio) { bestRatio = ratio; best = i; }
            }
            if (best < 0) break;
            int amount = Math.Max(1, eco.MaxAffordable(best) / 4);
            eco.TryBuy(best, amount);
        }
    }
}
