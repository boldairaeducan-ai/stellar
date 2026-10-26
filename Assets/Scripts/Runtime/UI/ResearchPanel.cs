using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>One-time credit upgrades (x3 per building, x2 global). The mid-game decision layer.</summary>
    public sealed class ResearchPanel : TabPanel
    {
        const int MaxShown = 25;
        static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        readonly ListRow[] rows;
        readonly Text emptyText;

        public ResearchPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Research")
        {
            AddHeader(Strings.ResearchHeader);
            var items = Eco.ResearchItems; // sorted by cost
            rows = new ListRow[items.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                var item = items[i];
                rows[i] = new ListRow(Content, $"Research_{item.Id}", () => DoResearch(item));
            }
            emptyText = AddHeader(Strings.ResearchDone, 36, 200);
        }

        public override int Badge => Eco.AffordableResearchCount;

        public override void Refresh(long now)
        {
            var eco = Eco;
            var items = eco.ResearchItems;
            int shown = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var item = items[i];
                var row = rows[i];
                bool visible = shown < MaxShown && !eco.IsResearched(item)
                               && (item.Generator < 0 || eco.IsUnlocked(item.Generator));
                row.Visible = visible;
                if (!visible) continue;
                shown++;

                bool unlocked = eco.IsResearchUnlocked(item);
                string tier = Roman[Mathf.Clamp(item.Tier - 1, 0, Roman.Length - 1)];
                if (item.Generator < 0)
                {
                    row.SetIcon(null, Theme.Accent, "x2");
                    row.SetTexts(string.Format(Strings.ResearchGlobalName, tier),
                        string.Format(Strings.ResearchGlobalEffect, item.Multiplier));
                }
                else
                {
                    var def = eco.Def(item.Generator);
                    row.SetIcon(Art.Generator(def.id), Theme.GeneratorColor(item.Generator), Initial(def.name));
                    row.SetTexts(string.Format(Strings.ResearchName, def.name, tier),
                        unlocked
                            ? string.Format(Strings.ResearchEffect, def.name, eco.ResearchItemMultiplier(item).ToString("0.##"))
                            : string.Format(Strings.ResearchRequires, item.UnlockOwned, def.name));
                }
                row.SetHighlight(eco.CanResearch(item));
                row.SetButton(NumberFormat.Format(item.Cost), eco.CanResearch(item), Theme.Buy);
            }
            emptyText.gameObject.SetActive(shown == 0);
        }

        void DoResearch(ResearchItem item)
        {
            if (!Eco.TryResearch(item)) return;
            string effect = item.Generator < 0
                ? string.Format(Strings.ResearchGlobalEffect, item.Multiplier)
                : string.Format(Strings.ResearchEffect, Eco.Def(item.Generator).name, Eco.ResearchItemMultiplier(item).ToString("0.##"));
            Ctx.Toast(effect + "!");
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }
    }
}
