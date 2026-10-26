using System.Globalization;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>
    /// Prestige: warp card (gain preview, progress) and the Dark Matter skill tree,
    /// laid out as three branch columns (Industry / Command / Logistics) over six tiers.
    /// </summary>
    public sealed class WarpPanel : TabPanel
    {
        const float CardHeight = 270;

        readonly Text titleText, bonusText, statusText;
        readonly RectTransform progressFill;
        readonly GameObject progressTrack;
        readonly Button warpButton;
        readonly PunchScale warpPunch;
        readonly Text[] tierHeaders;
        readonly SkillNodeCard[][] cards;
        readonly Image[][] connectors;
        readonly ListRow respecRow;

        public WarpPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Warp")
        {
            var card = UIFactory.Image("WarpCard", Content, Theme.PanelLight, ProceduralSprites.Rounded);
            card.WithLayout(preferredHeight: 430);

            titleText = UIFactory.Text("Title", card.transform, "", 52, Theme.Prestige, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -90), new Vector2(-20, -20));

            bonusText = UIFactory.Text("Bonus", card.transform, "", 30, Theme.TextDim);
            bonusText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -150), new Vector2(-30, -90));

            statusText = UIFactory.Text("Status", card.transform, "", 36, Theme.Text);
            statusText.supportRichText = true;
            statusText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -270), new Vector2(-30, -150));

            var track = UIFactory.Image("Progress", card.transform, Theme.Background, ProceduralSprites.Rounded);
            track.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -300), new Vector2(-60, -276));
            progressTrack = track.gameObject;
            progressFill = UIFactory.Image("Fill", track.transform, Theme.Prestige, ProceduralSprites.Rounded)
                .rectTransform.Anchor(Vector2.zero, new Vector2(0, 1));

            warpButton = UIFactory.Button("WarpButton", card.transform, Strings.WarpButton, Theme.Prestige, ConfirmWarp, out _, 46);
            warpButton.GetComponent<RectTransform>().Anchor(new Vector2(0.2f, 0), new Vector2(0.8f, 0), new Vector2(0, 24), new Vector2(0, 124));
            warpPunch = warpButton.gameObject.AddComponent<PunchScale>();

            BuildBranchLegend();

            var tiers = Eco.SkillTiers;
            tierHeaders = new Text[tiers.Length];
            cards = new SkillNodeCard[tiers.Length][];
            connectors = new Image[tiers.Length][];
            for (int t = 0; t < tiers.Length; t++)
            {
                if (t > 0) connectors[t] = BuildConnectors(tiers[t].nodes.Length);
                tierHeaders[t] = AddHeader("", 30, 70);
                tierHeaders[t].alignment = TextAnchor.MiddleLeft;

                var row = UIFactory.Rect($"Tier{t + 1}", Content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = CardHeight;
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 14;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = true;

                var nodes = tiers[t].nodes;
                cards[t] = new SkillNodeCard[nodes.Length];
                for (int n = 0; n < nodes.Length; n++)
                {
                    int tier = t, index = n;
                    cards[t][n] = new SkillNodeCard(row, Theme.Branch(nodes[n].branch), () => BuySkill(tier, index));
                }
            }

            respecRow = new ListRow(Content, "Respec", ConfirmRespec, height: 140);
        }

        void BuildBranchLegend()
        {
            var legend = UIFactory.Rect("Legend", Content);
            legend.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            var layout = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            UIFactory.Text("Industry", legend, Strings.BranchIndustry, 30, Theme.Industry, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Text("Command", legend, Strings.BranchCommand, 30, Theme.Command, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Text("Logistics", legend, Strings.BranchLogistics, 30, Theme.Logistics, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Thin vertical links between two tiers, one per branch column.</summary>
        Image[] BuildConnectors(int columns)
        {
            var row = UIFactory.Rect("Links", Content);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;
            var lines = new Image[columns];
            for (int c = 0; c < columns; c++)
            {
                lines[c] = UIFactory.Image("Link", row, Theme.Disabled);
                float x = (c + 0.5f) / columns;
                lines[c].rectTransform.Anchor(new Vector2(x, 0), new Vector2(x, 1), new Vector2(-3, -10), new Vector2(3, 10));
            }
            return lines;
        }

        public override int Badge => (Eco.CanPrestige ? 1 : 0) + Eco.AffordableSkillCount;

        public override void Refresh(long now)
        {
            var eco = Eco;
            RefreshWarpCard(eco);

            var tiers = eco.SkillTiers;
            for (int t = 0; t < tiers.Length; t++)
            {
                var tier = tiers[t];
                bool unlocked = eco.IsTierUnlocked(t);
                var chosen = eco.ChosenNode(t);
                string roman = Roman(t + 1);
                string state = !unlocked
                    ? string.Format(Strings.TierLocked, NumberFormat.Format(tier.unlockDarkMatter),
                        NumberFormat.Format(eco.State.darkMatterEarned) + "/" + NumberFormat.Format(tier.unlockDarkMatter))
                    : chosen == null ? Strings.TierChoose : Strings.TierChosen;
                tierHeaders[t].text = $"<b>{string.Format(Strings.TierTitle, roman, tier.name)}</b>   <size=24>{state}</size>";
                tierHeaders[t].color = unlocked ? Theme.Text : Theme.TextDim;

                for (int n = 0; n < tier.nodes.Length; n++)
                {
                    var node = tier.nodes[n];
                    int level = eco.SkillLevel(node);
                    SkillNodeCard.Look look;
                    string cost;
                    if (!unlocked) { look = SkillNodeCard.Look.TierLocked; cost = ""; }
                    else if (eco.IsSkillBlocked(t, node)) { look = SkillNodeCard.Look.Blocked; cost = Strings.SkillBlocked; }
                    else if (eco.IsSkillMaxed(node)) { look = SkillNodeCard.Look.Maxed; cost = Strings.Maxed; }
                    else
                    {
                        cost = string.Format(Strings.DmCostLabel, NumberFormat.Format(eco.SkillCost(t, node)));
                        look = eco.CanBuySkill(t, node) ? SkillNodeCard.Look.Available : SkillNodeCard.Look.TooExpensive;
                    }
                    cards[t][n].Set(node.name, string.Format(Strings.SkillLevel, level, node.maxLevel),
                        Describe(node, level) + Strings.PerLevel, cost, look);

                    if (t > 0 && n < connectors[t].Length)
                    {
                        // Light up the link when both this node and the one above in the same column are taken.
                        var above = n < tiers[t - 1].nodes.Length ? tiers[t - 1].nodes[n] : null;
                        bool lit = level > 0 && above != null && eco.SkillLevel(above) > 0;
                        connectors[t][n].color = lit ? Theme.Branch(node.branch) : Theme.Disabled;
                    }
                }
            }

            respecRow.Visible = eco.State.skillNodes.Count > 0;
            respecRow.SetIcon(null, Theme.Disabled, "R");
            respecRow.SetTexts(Strings.RespecTitle, string.Format(Strings.RespecDetails, NumberFormat.Format(eco.SkillTreeInvested)));
            respecRow.SetButton(eco.RespecCost == 0 ? Strings.RespecFree : string.Format(Strings.CrystalCost, eco.RespecCost),
                eco.CanRespec, Theme.Premium);
        }

        void RefreshWarpCard(IdleEconomy eco)
        {
            titleText.text = string.Format(Strings.WarpHeader, NumberFormat.Format(eco.State.darkMatter));
            bonusText.text = string.Format(Strings.WarpBonus, Multiplier(eco.PrestigeMultiplier),
                (eco.PrestigeBonusPerPoint * 100).ToString("0.#", CultureInfo.InvariantCulture));

            bool ready = eco.CanPrestige;
            statusText.text = ready
                ? string.Format(Strings.WarpReady, NumberFormat.Format(eco.PendingDarkMatter),
                    Multiplier(eco.PrestigeMultiplier), Multiplier(eco.PrestigeMultiplierAfterWarp))
                : string.Format(Strings.WarpNotReady, NumberFormat.Format(eco.PrestigeRequirement));
            progressTrack.SetActive(!ready);
            progressFill.anchorMax = new Vector2((float)System.Math.Min(1, eco.State.runEarnings / eco.PrestigeRequirement), 1);
            warpButton.interactable = ready;
            if (ready && Time.unscaledTime % 2f < 0.1f) warpPunch.Play(0.04f); // gentle pulse when available
        }

        static string Multiplier(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        static string Percent(double value) => (value * 100).ToString("0.#", CultureInfo.InvariantCulture);
        static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        static string Roman(int n)
        {
            string[] numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return n >= 1 && n <= numerals.Length ? numerals[n - 1] : n.ToString();
        }

        static string Describe(SkillNodeDef node, int level)
        {
            double v = node.value;
            switch (node.effect)
            {
                case SkillEffect.Production: return string.Format(Strings.FxProduction, Percent(v));
                case SkillEffect.ProductionMult: return string.Format(Strings.FxProductionMult, Number(1 + v));
                case SkillEffect.Tap: return string.Format(Strings.FxTap, Percent(v));
                case SkillEffect.TapMult: return string.Format(Strings.FxTapMult, Percent(v));
                case SkillEffect.Cost: return string.Format(Strings.FxCost, Percent(v));
                case SkillEffect.StartCredits: return string.Format(Strings.FxStartCredits, NumberFormat.Format(v * System.Math.Pow(10, level)));
                case SkillEffect.OfflineHours: return string.Format(Strings.FxOfflineHours, Number(v));
                case SkillEffect.OfflineEfficiency: return string.Format(Strings.FxOfflineEfficiency, Percent(v));
                case SkillEffect.BoostHours: return string.Format(Strings.FxBoostHours, Number(v));
                case SkillEffect.BoostMultiplier: return string.Format(Strings.FxBoostMultiplier, Number(v));
                case SkillEffect.BonusDrop: return string.Format(Strings.FxBonusDrop, Percent(v));
                case SkillEffect.BonusDropRate: return string.Format(Strings.FxBonusDropRate, Percent(v));
                case SkillEffect.Research: return string.Format(Strings.FxResearch, Number(v));
                case SkillEffect.Milestone: return string.Format(Strings.FxMilestone, Number(v));
                case SkillEffect.DarkMatterGain: return string.Format(Strings.FxDarkMatterGain, Percent(v));
                case SkillEffect.PrestigeBonus: return string.Format(Strings.FxPrestigeBonus, Percent(v));
                default: return node.effect;
            }
        }

        void BuySkill(int tier, int index)
        {
            var node = Eco.SkillTiers[tier].nodes[index];
            if (!Eco.TryBuySkill(tier, node)) return;
            cards[tier][index].Punch();
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }

        void ConfirmRespec()
        {
            if (!Eco.CanRespec) return;
            Popup.Show(Ctx.PopupRoot, Strings.RespecTitle,
                string.Format(Strings.RespecConfirm, NumberFormat.Format(Eco.SkillTreeInvested)),
                new Popup.Choice(Strings.RespecTitle, Theme.Premium, () =>
                {
                    if (!Eco.TryRespec()) return;
                    Ctx.Game.Save();
                    Ctx.Toast(Strings.RespecDone);
                    Ctx.RequestRefresh();
                }),
                new Popup.Choice(Strings.Cancel, Theme.Disabled));
        }

        void ConfirmWarp()
        {
            if (!Eco.CanPrestige) return;
            Popup.Show(Ctx.PopupRoot, Strings.PrestigeTitle,
                string.Format(Strings.WarpConfirmBody, NumberFormat.Format(Eco.PendingDarkMatter)),
                new Popup.Choice(Strings.PrestigeConfirm, Theme.Prestige, () =>
                {
                    double gain = Ctx.Game.DoPrestige();
                    Ctx.Flash();
                    Ctx.Toast(string.Format(Strings.PrestigeDone, NumberFormat.Format(gain)));
                    if (Eco.LastWarpChest.Count > 0)
                        Popup.Show(Ctx.PopupRoot, Strings.WarpChestTitle,
                            string.Format(Strings.ShardsFound, ShipText.Shards(Eco, Eco.LastWarpChest)),
                            new Popup.Choice(Strings.Close, Theme.Buy));
                    Ctx.RequestRefresh();
                }),
                new Popup.Choice(Strings.Cancel, Theme.Disabled));
        }
    }
}
