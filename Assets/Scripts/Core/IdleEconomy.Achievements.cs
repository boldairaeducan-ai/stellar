using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    public enum AchievementKind
    {
        Owned,     // own N of a generator (best count ever, survives prestige)
        Earnings,  // earn N credits in total
        Warps,     // warp N times
        ShipsBuilt,      // build N different ships
        RarityComplete,  // build every ship of one rarity
        ShipsMaxed,      // bring N ships to max level
    }

    public sealed class AchievementDef
    {
        public string Id;
        public AchievementKind Kind;
        public int Generator; // Owned only
        public int Rarity;    // RarityComplete only (index in shipRarities)
        public double Target;
        public long Reward;   // crystals
    }

    public sealed partial class IdleEconomy
    {
        readonly List<AchievementDef> achievements = new List<AchievementDef>();
        readonly HashSet<string> claimedAchievements = new HashSet<string>();

        public IReadOnlyList<AchievementDef> Achievements => achievements;

        void BuildAchievements()
        {
            achievements.Clear();
            for (int g = 0; g < GeneratorCount; g++)
                for (int k = 0; k < Balance.achievementOwnedThresholds.Length; k++)
                    achievements.Add(new AchievementDef
                    {
                        Id = $"own_{Def(g).id}_{Balance.achievementOwnedThresholds[k]}",
                        Kind = AchievementKind.Owned,
                        Generator = g,
                        Target = Balance.achievementOwnedThresholds[k],
                        Reward = Balance.achievementOwnedRewards[k],
                    });
            for (int k = 0; k < Balance.achievementEarningsThresholds.Length; k++)
                achievements.Add(new AchievementDef
                {
                    Id = $"earn_{k + 1}",
                    Kind = AchievementKind.Earnings,
                    Target = Balance.achievementEarningsThresholds[k],
                    Reward = Balance.achievementEarningsRewards[k],
                });
            for (int k = 0; k < Balance.achievementWarpThresholds.Length; k++)
                achievements.Add(new AchievementDef
                {
                    Id = $"warp_{Balance.achievementWarpThresholds[k]}",
                    Kind = AchievementKind.Warps,
                    Target = Balance.achievementWarpThresholds[k],
                    Reward = Balance.achievementWarpRewards[k],
                });

            for (int k = 0; k < Balance.achievementShipsBuiltThresholds.Length; k++)
                achievements.Add(new AchievementDef
                {
                    Id = $"ships_{Balance.achievementShipsBuiltThresholds[k]}",
                    Kind = AchievementKind.ShipsBuilt,
                    Target = Math.Min(Balance.achievementShipsBuiltThresholds[k], Balance.ships.Length),
                    Reward = Balance.achievementShipsBuiltRewards[k],
                });
            for (int r = 0; r < Balance.shipRarities.Length && r < Balance.achievementRarityCompleteRewards.Length; r++)
            {
                string rarity = Balance.shipRarities[r].id;
                int count = Array.FindAll(Balance.ships, s => s.rarity == rarity).Length;
                if (count == 0) continue;
                achievements.Add(new AchievementDef
                {
                    Id = $"rarity_{rarity}",
                    Kind = AchievementKind.RarityComplete,
                    Rarity = r,
                    Target = count,
                    Reward = Balance.achievementRarityCompleteRewards[r],
                });
            }
            for (int k = 0; k < Balance.achievementShipsMaxedThresholds.Length; k++)
                achievements.Add(new AchievementDef
                {
                    Id = $"maxed_{Balance.achievementShipsMaxedThresholds[k]}",
                    Kind = AchievementKind.ShipsMaxed,
                    Target = Balance.achievementShipsMaxedThresholds[k],
                    Reward = Balance.achievementShipsMaxedRewards[k],
                });

            claimedAchievements.Clear();
            foreach (var id in State.achievementsClaimed) claimedAchievements.Add(id);
        }

        public double AchievementProgress(AchievementDef a)
        {
            switch (a.Kind)
            {
                case AchievementKind.Owned: return State.generators[a.Generator].best;
                case AchievementKind.Earnings: return State.totalEarnings;
                case AchievementKind.Warps: return State.prestigeCount;
                case AchievementKind.ShipsBuilt: return CountShips(s => IsShipBuilt(s));
                case AchievementKind.RarityComplete:
                    string rarity = Balance.shipRarities[a.Rarity].id;
                    return CountShips(s => s.rarity == rarity && IsShipBuilt(s));
                case AchievementKind.ShipsMaxed: return CountShips(s => Stars(s) >= MaxStars);
                default: return 0;
            }
        }

        int CountShips(Predicate<ShipDef> match)
        {
            int n = 0;
            foreach (var ship in Balance.ships)
                if (match(ship)) n++;
            return n;
        }

        /// <summary>0..1 completion ratio.</summary>
        public double AchievementRatio(AchievementDef a) => Math.Min(1, AchievementProgress(a) / a.Target);

        public bool IsAchievementClaimed(AchievementDef a) => claimedAchievements.Contains(a.Id);
        public bool IsAchievementComplete(AchievementDef a) => AchievementProgress(a) >= a.Target;
        public bool CanClaimAchievement(AchievementDef a) => IsAchievementComplete(a) && !IsAchievementClaimed(a);

        public bool TryClaimAchievement(AchievementDef a)
        {
            if (!CanClaimAchievement(a)) return false;
            claimedAchievements.Add(a.Id);
            State.achievementsClaimed.Add(a.Id);
            State.crystals += a.Reward;
            return true;
        }

        public int ClaimableAchievementCount
        {
            get
            {
                int n = 0;
                foreach (var a in achievements)
                    if (CanClaimAchievement(a)) n++;
                return n;
            }
        }
    }
}
