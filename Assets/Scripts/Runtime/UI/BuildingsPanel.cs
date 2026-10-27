using StellarIdle.Core;
using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>Generators: buy them, see their output and the progress to their next x2 milestone.</summary>
    public sealed class BuildingsPanel : TabPanel
    {
        readonly ListRow[] rows;
        readonly ListRow autoBuyRow;
        readonly UnityEngine.UI.Text galaxyNote;

        public BuildingsPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Buildings")
        {
            galaxyNote = AddHeader("", 28, 60);
            autoBuyRow = new ListRow(Content, "AutoBuy", ToggleAutoBuy, height: 130);
            rows = new ListRow[Eco.GeneratorCount];
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i] = new ListRow(Content, $"Generator{i}", () => Buy(index), withProgress: true);
            }
        }

        public override void Refresh(long now)
        {
            var eco = Eco;
            var galaxy = eco.CurrentGalaxy;
            bool limited = galaxy != null && eco.MaxGenerators < eco.GeneratorCount;
            galaxyNote.gameObject.SetActive(limited);
            if (limited) galaxyNote.text = string.Format(Strings.GalaxyBuildingsLimit, galaxy.name, eco.MaxGenerators);

            autoBuyRow.Visible = eco.AutoBuyUnlocked;
            if (eco.AutoBuyUnlocked)
            {
                bool on = eco.State.autoBuyEnabled;
                autoBuyRow.SetIcon(null, on ? Theme.Buy : Theme.Disabled, "A");
                autoBuyRow.SetTexts(Strings.AutoBuyTitle, on ? Strings.AutoBuyOn : Strings.AutoBuyOff);
                autoBuyRow.SetButton(on ? Strings.On : Strings.Off, true, on ? Theme.Buy : Theme.PanelLight);
            }

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                bool unlocked = eco.IsUnlocked(i);
                bool teaser = !unlocked && i > 0 && i < eco.MaxGenerators && eco.IsUnlocked(i - 1);
                row.Visible = unlocked || teaser;
                if (!row.Visible) continue;

                var def = eco.Def(i);
                int owned = eco.Owned(i);
                int amount = eco.ResolveBuyAmount(i, Ctx.BuyMode);
                double cost = eco.CostOf(i, amount);

                if (unlocked)
                {
                    row.SetIcon(Art.Generator(def.id), Theme.GeneratorColor(i), Initial(def.name));
                    int next = eco.NextMilestone(owned);
                    int previous = eco.PreviousMilestone(owned);
                    string production = $"+{NumberFormat.Format(eco.GeneratorProduction(i, now))}{Strings.PerSecond}";
                    string milestone = next > 0 ? $"   {Strings.NextBonus} {next}" : $"   {Strings.AllMilestones}";
                    row.SetTexts($"{def.name}  <color=#FFC440>{owned}</color>", production + milestone);
                    row.SetProgress(next > 0 ? (float)(owned - previous) / (next - previous) : 1);
                }
                else
                {
                    row.SetIcon(null, Theme.Disabled, "?");
                    row.SetTexts(Strings.Locked, "");
                    row.SetProgress(0);
                }

                row.SetButton($"{Strings.Buy} x{amount}\n{NumberFormat.Format(cost)}", eco.CanAfford(i, amount), Theme.Buy);
            }
        }

        void ToggleAutoBuy()
        {
            Eco.State.autoBuyEnabled = !Eco.State.autoBuyEnabled;
            Ctx.RequestRefresh();
        }

        void Buy(int index)
        {
            var eco = Eco;
            int amount = eco.ResolveBuyAmount(index, Ctx.BuyMode);
            double multiplierBefore = eco.MilestoneMultiplier(eco.Owned(index));
            if (!eco.TryBuy(index, amount)) return;

            rows[index].Punch();
            if (eco.MilestoneMultiplier(eco.Owned(index)) > multiplierBefore)
            {
                rows[index].Punch(0.12f);
                Ctx.Toast(string.Format(Strings.MilestoneReached, eco.Def(index).name));
            }
            Ctx.RequestRefresh();
        }
    }
}
