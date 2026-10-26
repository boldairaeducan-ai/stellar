using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    public enum AchievementKind
    {
        Owned,     // own N of a generator (best count ever, survives prestige)
        Earnings,  // earn N credits in total
        Warps,     // warp N times
    }

    public sealed class AchievementDef
    {
        public string Id;
        public AchievementKind Kind;
        public int Generator; // Owned only
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
                default: return 0;
            }
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
