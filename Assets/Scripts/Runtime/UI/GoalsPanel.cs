using System.Collections.Generic;
using StellarIdle.Core;
using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>Achievements: claimable first, then closest to completion, finished ones at the bottom.</summary>
    public sealed class GoalsPanel : TabPanel
    {
        readonly ListRow[] rows;
        readonly List<int> order = new List<int>();
        readonly int firstRowSibling;

        public GoalsPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Goals")
        {
            var achievements = Eco.Achievements;
            rows = new ListRow[achievements.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                var a = achievements[i];
                int index = i;
                rows[i] = new ListRow(Content, $"Goal_{a.Id}", () => Claim(index), withProgress: true, height: 150);
            }
            firstRowSibling = rows.Length > 0 ? rows[0].Root.transform.GetSiblingIndex() : 0;
        }

        public override int Badge => Eco.ClaimableAchievementCount;

        public override void Refresh(long now)
        {
            var eco = Eco;
            var achievements = eco.Achievements;
            for (int i = 0; i < rows.Length; i++)
            {
                var a = achievements[i];
                var row = rows[i];
                // Building goals stay hidden until that building is discovered (no spoilers).
                row.Visible = a.Kind != AchievementKind.Owned || eco.IsUnlocked(a.Generator) || eco.State.generators[a.Generator].best > 0;
                if (!row.Visible) continue;

                bool claimed = eco.IsAchievementClaimed(a);
                bool claimable = eco.CanClaimAchievement(a);
                string progress = $"{NumberFormat.Format(System.Math.Min(eco.AchievementProgress(a), a.Target))} / {NumberFormat.Format(a.Target)}";
                row.SetTexts(Title(eco, a), claimed ? Strings.GoalDone : $"{progress}   {string.Format(Strings.GoalReward, a.Reward)}");
                row.SetProgress((float)eco.AchievementRatio(a));
                SetGoalIcon(eco, row, a, claimed);
                row.SetHighlight(claimable);
                row.SetButton(claimed ? Strings.GoalDone : string.Format(Strings.GoalClaim, a.Reward), claimable, Theme.Premium);
            }
            Reorder(eco);
        }

        void Reorder(IdleEconomy eco)
        {
            var achievements = eco.Achievements;
            order.Clear();
            for (int i = 0; i < rows.Length; i++) order.Add(i);
            order.Sort((x, y) =>
            {
                int rank = Rank(eco, achievements[x]).CompareTo(Rank(eco, achievements[y]));
                if (rank != 0) return rank;
                int ratio = eco.AchievementRatio(achievements[y]).CompareTo(eco.AchievementRatio(achievements[x]));
                return ratio != 0 ? ratio : x.CompareTo(y);
            });
            for (int position = 0; position < order.Count; position++)
            {
                var t = rows[order[position]].Root.transform;
                int sibling = firstRowSibling + position;
                if (t.GetSiblingIndex() != sibling) t.SetSiblingIndex(sibling);
            }
        }

        static void SetGoalIcon(IdleEconomy eco, ListRow row, AchievementDef a, bool claimed)
        {
            switch (a.Kind)
            {
                case AchievementKind.Owned:
                    var def = eco.Def(a.Generator);
                    row.SetIcon(Art.Generator(def.id), claimed ? Theme.Disabled : Theme.GeneratorColor(a.Generator), Initial(def.name));
                    break;
                case AchievementKind.ShipsBuilt:
                case AchievementKind.ShipsMaxed:
                    row.SetIcon(null, claimed ? Theme.Disabled : Theme.Rarity(a.Kind == AchievementKind.ShipsMaxed ? 4 : 2), "S");
                    break;
                case AchievementKind.RarityComplete:
                    row.SetIcon(null, claimed ? Theme.Disabled : Theme.Rarity(a.Rarity), "S");
                    break;
                default:
                    row.SetIcon(Art.CrystalIcon, claimed ? Theme.Disabled : Theme.Premium, a.Kind == AchievementKind.Warps ? "W" : "$");
                    break;
            }
        }

        static int Rank(IdleEconomy eco, AchievementDef a) =>
            eco.CanClaimAchievement(a) ? 0 : eco.IsAchievementClaimed(a) ? 2 : 1;

        static string Title(IdleEconomy eco, AchievementDef a)
        {
            switch (a.Kind)
            {
                case AchievementKind.Owned: return string.Format(Strings.GoalOwned, eco.Def(a.Generator).name, a.Target);
                case AchievementKind.Earnings: return string.Format(Strings.GoalEarnings, NumberFormat.Format(a.Target));
                case AchievementKind.Warps: return string.Format(Strings.GoalWarps, a.Target);
                case AchievementKind.ShipsBuilt: return string.Format(Strings.GoalShipsBuilt, a.Target);
                case AchievementKind.RarityComplete:
                    return string.Format(Strings.GoalRarityComplete, eco.Balance.shipRarities[a.Rarity].name);
                default: return string.Format(Strings.GoalShipsMaxed, a.Target);
            }
        }

        void Claim(int index)
        {
            var a = Eco.Achievements[index];
            if (!Eco.TryClaimAchievement(a)) return;
            rows[index].Punch(0.1f);
            Ctx.Toast(string.Format(Strings.GoalClaimed, a.Reward));
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }
    }
}
