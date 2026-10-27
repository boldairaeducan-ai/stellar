using System.Collections.Generic;
using System.Globalization;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>
    /// Prestige layers. A warp card on top (current galaxy, Dark Matter preview), then three sections:
    /// the Dark Matter skill tree, the galaxies and their relics, and the Big Crunch with cosmic upgrades.
    /// </summary>
    public sealed class WarpPanel : TabPanel
    {
        const float CardHeight = 270;
        enum Section { Tree, Galaxies, Crunch }

        // Warp card
        readonly Text titleText, galaxyText, bonusText, statusText;
        readonly RectTransform progressFill;
        readonly GameObject progressTrack;
        readonly Button warpButton;
        readonly PunchScale warpPunch;

        // Section switcher
        readonly Button[] sectionButtons;
        readonly Text[] sectionLabels;
        readonly RectTransform[] sections;
        Section current = Section.Tree;

        // Tree
        readonly Text[] tierHeaders;
        readonly GameObject[] tierRows;
        readonly GameObject[] linkRows;
        readonly SkillNodeCard[][] cards;
        readonly Image[][] connectors;
        readonly ListRow respecRow;

        // Galaxies
        readonly ListRow[] galaxyRows;

        // Crunch
        readonly Text crunchTitle, crunchBonus, crunchStatus;
        readonly RectTransform crunchFill;
        readonly Button crunchButton;
        readonly ListRow[] cosmicRows;

        public WarpPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Warp")
        {
            var eco = Eco;

            // ----- Warp card -----
            var card = UIFactory.Image("WarpCard", Content, Theme.PanelLight, ProceduralSprites.Rounded);
            card.WithLayout(preferredHeight: 470);
            titleText = CardText(card.transform, 52, Theme.Prestige, FontStyle.Bold, -20, -90);
            galaxyText = CardText(card.transform, 28, Theme.Accent, FontStyle.Normal, -90, -130);
            bonusText = CardText(card.transform, 28, Theme.TextDim, FontStyle.Normal, -130, -180);
            statusText = CardText(card.transform, 34, Theme.Text, FontStyle.Normal, -180, -300);
            progressTrack = ProgressBar(card.transform, -330, -306, Theme.Prestige, out progressFill);
            warpButton = UIFactory.Button("WarpButton", card.transform, Strings.WarpButton, Theme.Prestige, ChooseDestination, out _, 46);
            warpButton.GetComponent<RectTransform>().Anchor(new Vector2(0.2f, 0), new Vector2(0.8f, 0), new Vector2(0, 24), new Vector2(0, 124));
            warpPunch = warpButton.gameObject.AddComponent<PunchScale>();

            // ----- Section switcher -----
            var switcher = UIFactory.Rect("Sections", Content);
            switcher.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var switcherLayout = switcher.gameObject.AddComponent<HorizontalLayoutGroup>();
            switcherLayout.spacing = 10;
            switcherLayout.childControlWidth = switcherLayout.childControlHeight = true;
            switcherLayout.childForceExpandWidth = switcherLayout.childForceExpandHeight = true;
            string[] names = { Strings.SectionTree, Strings.SectionGalaxies, Strings.SectionCrunch };
            sectionButtons = new Button[names.Length];
            sectionLabels = new Text[names.Length];
            sections = new RectTransform[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var section = (Section)i;
                sectionButtons[i] = UIFactory.Button($"Section{i}", switcher, names[i], Theme.Panel, () => Show(section), out sectionLabels[i], 30);
                sections[i] = SectionRoot(names[i]);
            }

            // ----- Tree -----
            var tree = sections[(int)Section.Tree];
            BuildBranchLegend(tree);
            var tiers = eco.SkillTiers;
            tierHeaders = new Text[tiers.Length];
            tierRows = new GameObject[tiers.Length];
            linkRows = new GameObject[tiers.Length];
            cards = new SkillNodeCard[tiers.Length][];
            connectors = new Image[tiers.Length][];
            for (int t = 0; t < tiers.Length; t++)
            {
                if (t > 0) connectors[t] = BuildConnectors(tree, tiers[t].nodes.Length, out linkRows[t]);
                tierHeaders[t] = Header(tree, "", 30, 70);
                tierHeaders[t].alignment = TextAnchor.MiddleLeft;

                var row = UIFactory.Rect($"Tier{t + 1}", tree);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = CardHeight;
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 14;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = true;
                tierRows[t] = row.gameObject;

                var nodes = tiers[t].nodes;
                cards[t] = new SkillNodeCard[nodes.Length];
                for (int n = 0; n < nodes.Length; n++)
                {
                    int tier = t, index = n;
                    cards[t][n] = new SkillNodeCard(row, Theme.Branch(nodes[n].branch), () => BuySkill(tier, index));
                }
            }
            respecRow = new ListRow(tree, "Respec", ConfirmRespec, height: 140);

            // ----- Galaxies -----
            var galaxies = sections[(int)Section.Galaxies];
            galaxyRows = new ListRow[eco.Galaxies.Length];
            for (int i = 0; i < galaxyRows.Length; i++)
                galaxyRows[i] = new ListRow(galaxies, $"Galaxy_{eco.Galaxies[i].id}", null, height: 210);

            // ----- Crunch -----
            var crunch = sections[(int)Section.Crunch];
            var crunchCard = UIFactory.Image("CrunchCard", crunch, Theme.PanelLight, ProceduralSprites.Rounded);
            crunchCard.WithLayout(preferredHeight: 430);
            crunchTitle = CardText(crunchCard.transform, 50, Theme.Accent, FontStyle.Bold, -20, -90);
            crunchBonus = CardText(crunchCard.transform, 28, Theme.TextDim, FontStyle.Normal, -90, -140);
            crunchStatus = CardText(crunchCard.transform, 32, Theme.Text, FontStyle.Normal, -140, -260);
            ProgressBar(crunchCard.transform, -290, -266, Theme.Accent, out crunchFill);
            crunchButton = UIFactory.Button("CrunchButton", crunchCard.transform, Strings.CrunchButton, Theme.Premium, ConfirmCrunch, out _, 44);
            crunchButton.GetComponent<RectTransform>().Anchor(new Vector2(0.15f, 0), new Vector2(0.85f, 0), new Vector2(0, 24), new Vector2(0, 124));

            cosmicRows = new ListRow[eco.CosmicUpgrades.Length];
            for (int i = 0; i < cosmicRows.Length; i++)
            {
                var upgrade = eco.CosmicUpgrades[i];
                cosmicRows[i] = new ListRow(crunch, $"Cosmic_{upgrade.id}", () => BuyCosmic(upgrade));
            }

            Show(Section.Tree, refresh: false); // the main screen is still being built
        }

        // ---------- Layout helpers ----------

        static Text CardText(Transform card, int size, Color color, FontStyle style, float top, float bottom)
        {
            var text = UIFactory.Text("Text", card, "", size, color, TextAnchor.MiddleCenter, style);
            text.supportRichText = true;
            text.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, bottom), new Vector2(-30, top));
            return text;
        }

        static GameObject ProgressBar(Transform card, float bottom, float top, Color color, out RectTransform fill)
        {
            var track = UIFactory.Image("Progress", card, Theme.Background, ProceduralSprites.Rounded);
            track.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, bottom), new Vector2(-60, top));
            fill = UIFactory.Image("Fill", track.transform, color, ProceduralSprites.Rounded).rectTransform.Anchor(Vector2.zero, new Vector2(0, 1));
            return track.gameObject;
        }

        RectTransform SectionRoot(string name)
        {
            var root = UIFactory.Rect(name, Content);
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return root;
        }

        static Text Header(Transform parent, string text, int size, float height)
        {
            var header = UIFactory.Text("Header", parent, text, size, Theme.TextDim);
            header.WithLayout(preferredHeight: height);
            header.supportRichText = true;
            return header;
        }

        static void BuildBranchLegend(Transform parent)
        {
            var legend = UIFactory.Rect("Legend", parent);
            legend.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            var layout = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            UIFactory.Text("Industry", legend, Strings.BranchIndustry, 30, Theme.Industry, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Text("Command", legend, Strings.BranchCommand, 30, Theme.Command, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Text("Logistics", legend, Strings.BranchLogistics, 30, Theme.Logistics, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Thin vertical links between two tiers, one per branch column.</summary>
        static Image[] BuildConnectors(Transform parent, int columns, out GameObject row)
        {
            var rt = UIFactory.Rect("Links", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;
            row = rt.gameObject;
            var lines = new Image[columns];
            for (int c = 0; c < columns; c++)
            {
                lines[c] = UIFactory.Image("Link", rt, Theme.Disabled);
                float x = (c + 0.5f) / columns;
                lines[c].rectTransform.Anchor(new Vector2(x, 0), new Vector2(x, 1), new Vector2(-3, -10), new Vector2(3, 10));
            }
            return lines;
        }

        void Show(Section section) => Show(section, refresh: true);

        void Show(Section section, bool refresh)
        {
            current = section;
            for (int i = 0; i < sections.Length; i++)
            {
                bool on = i == (int)section;
                sections[i].gameObject.SetActive(on);
                sectionButtons[i].image.color = on ? Theme.PanelLight : Theme.Panel;
                sectionLabels[i].color = on ? Theme.Accent : Theme.TextDim;
            }
            if (refresh) Ctx.RequestRefresh();
        }

        // ---------- Refresh ----------

        public override int Badge =>
            (Eco.CanPrestige ? 1 : 0) + Eco.AffordableSkillCount + (Eco.CanCrunch ? 1 : 0) + Eco.AffordableCosmicCount;

        public override void Refresh(long now)
        {
            var eco = Eco;
            RefreshWarpCard(eco);
            switch (current)
            {
                case Section.Tree: RefreshTree(eco); break;
                case Section.Galaxies: RefreshGalaxies(eco); break;
                case Section.Crunch: RefreshCrunch(eco); break;
            }
        }

        void RefreshWarpCard(IdleEconomy eco)
        {
            titleText.text = string.Format(Strings.WarpHeader, NumberFormat.Format(eco.State.darkMatter));
            var galaxy = eco.CurrentGalaxy;
            galaxyText.text = galaxy == null ? "" : string.Format(Strings.CurrentGalaxy, galaxy.name) + "  ·  " + GalaxyText.Modifiers(galaxy);
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

            sectionLabels[(int)Section.Crunch].text = eco.CanCrunch || eco.AffordableCosmicCount > 0
                ? $"<color=#FFC440>{Strings.SectionCrunch}</color>" : Strings.SectionCrunch;
            sectionLabels[(int)Section.Crunch].supportRichText = true;
        }

        void RefreshTree(IdleEconomy eco)
        {
            var tiers = eco.SkillTiers;
            for (int t = 0; t < tiers.Length; t++)
            {
                var tier = tiers[t];
                bool visible = eco.IsTierVisible(t);
                tierHeaders[t].gameObject.SetActive(visible);
                tierRows[t].SetActive(visible);
                if (linkRows[t] != null) linkRows[t].SetActive(visible);
                if (!visible) continue;

                bool unlocked = eco.IsTierUnlocked(t);
                var chosen = eco.ChosenNode(t);
                string state = !unlocked
                    ? string.Format(Strings.TierLocked, NumberFormat.Format(tier.unlockDarkMatter),
                        NumberFormat.Format(eco.State.darkMatterEarned) + "/" + NumberFormat.Format(tier.unlockDarkMatter))
                    : chosen == null ? Strings.TierChoose : Strings.TierChosen;
                tierHeaders[t].text = $"<b>{string.Format(Strings.TierTitle, Roman(t + 1), tier.name)}</b>   <size=24>{state}</size>";
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
                    cards[t][n].Set(node.name, string.Format(Strings.SkillLevel, level, eco.SkillMaxLevel(node)),
                        GalaxyText.Effect(node.effect, node.value, level) + Strings.PerLevel, cost, look);

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

        void RefreshGalaxies(IdleEconomy eco)
        {
            for (int i = 0; i < galaxyRows.Length; i++)
            {
                var galaxy = eco.Galaxies[i];
                var row = galaxyRows[i];
                bool unlocked = eco.IsGalaxyUnlocked(galaxy);
                bool here = eco.CurrentGalaxy == galaxy;
                row.SetIcon(null, unlocked ? Theme.Prestige : Theme.Disabled, Initial(galaxy.name));
                row.SetTexts($"{galaxy.name}  <color=#FFC440>x{Multiplier(galaxy.darkMatterMultiplier)} DM</color>",
                    GalaxyText.Modifiers(galaxy) + (string.IsNullOrEmpty(galaxy.relicEffect) ? "" : "\n" + GalaxyText.Relic(eco, galaxy)));
                row.SetHighlight(here);
                row.SetButton(here ? Strings.GalaxyHere : unlocked ? "" : string.Format(Strings.GalaxyLocked, galaxy.unlockWarps),
                    false, Theme.Prestige, show: here || !unlocked);
            }
        }

        void RefreshCrunch(IdleEconomy eco)
        {
            crunchTitle.text = string.Format(Strings.CrunchHeader, NumberFormat.Format(eco.State.singularities));
            crunchBonus.text = string.Format(Strings.CrunchBonus, Multiplier(eco.SingularityMultiplier),
                (eco.Balance.singularityBonus * 100).ToString("0.#", CultureInfo.InvariantCulture));
            bool ready = eco.CanCrunch;
            crunchStatus.text = ready
                ? string.Format(Strings.CrunchReady, NumberFormat.Format(eco.PendingSingularities))
                : string.Format(Strings.CrunchNotReady, NumberFormat.Format(eco.CrunchRequirement),
                    NumberFormat.Format(eco.CycleDarkMatter) + " / " + NumberFormat.Format(eco.CrunchRequirement));
            crunchFill.anchorMax = new Vector2((float)System.Math.Min(1, eco.CycleDarkMatter / eco.CrunchRequirement), 1);
            crunchButton.interactable = ready;

            for (int i = 0; i < cosmicRows.Length; i++)
            {
                var upgrade = eco.CosmicUpgrades[i];
                int level = eco.CosmicLevel(upgrade);
                var row = cosmicRows[i];
                row.SetIcon(null, Theme.Accent, Initial(upgrade.name));
                row.SetTexts($"{upgrade.name}  <color=#FFC440>{string.Format(Strings.CosmicLevel, level, upgrade.maxLevel)}</color>",
                    GalaxyText.Effect(upgrade.effect, upgrade.value, level) + (upgrade.maxLevel > 1 ? Strings.PerLevel : ""));
                row.SetHighlight(eco.CanBuyCosmic(upgrade));
                if (eco.IsCosmicMaxed(upgrade)) row.SetButton(Strings.Maxed, false, Theme.Accent);
                else row.SetButton(string.Format(Strings.CosmicCostLabel, NumberFormat.Format(eco.CosmicCost(upgrade))),
                    eco.CanBuyCosmic(upgrade), Theme.Accent);
            }
        }

        static string Multiplier(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        static string Roman(int n)
        {
            string[] numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return n >= 1 && n <= numerals.Length ? numerals[n - 1] : n.ToString();
        }

        // ---------- Actions ----------

        void BuySkill(int tier, int index)
        {
            var node = Eco.SkillTiers[tier].nodes[index];
            if (!Eco.TryBuySkill(tier, node)) return;
            cards[tier][index].Punch();
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }

        void BuyCosmic(CosmicUpgradeDef upgrade)
        {
            if (!Eco.TryBuyCosmic(upgrade)) return;
            cosmicRows[System.Array.IndexOf(Eco.CosmicUpgrades, upgrade)].Punch(0.1f);
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

        /// <summary>Warp: pick the destination galaxy (each choice warps immediately).</summary>
        void ChooseDestination()
        {
            var eco = Eco;
            if (!eco.CanPrestige) return;
            var choices = new List<Popup.Choice>();
            foreach (var galaxy in eco.Galaxies)
            {
                if (!eco.IsGalaxyUnlocked(galaxy)) continue;
                string id = galaxy.id;
                choices.Add(new Popup.Choice(
                    string.Format(Strings.GalaxyChoice, galaxy.name, Multiplier(galaxy.darkMatterMultiplier), GalaxyText.Modifiers(galaxy)),
                    eco.CurrentGalaxy == galaxy ? Theme.PanelLight : Theme.Prestige, () => Warp(id)));
            }
            if (choices.Count == 0) choices.Add(new Popup.Choice(Strings.PrestigeConfirm, Theme.Prestige, () => Warp(null)));
            choices.Add(new Popup.Choice(Strings.Cancel, Theme.Disabled));
            Popup.Show(Ctx.PopupRoot, Strings.ChooseGalaxy,
                string.Format(Strings.ChooseGalaxyBody, NumberFormat.Format(eco.PendingDarkMatter)), choices.ToArray());
        }

        void Warp(string galaxyId)
        {
            var eco = Eco;
            double gain = Ctx.Game.DoPrestige(galaxyId);
            if (gain <= 0) return;
            Ctx.Flash();

            var lines = new List<string> { string.Format(Strings.PrestigeDone, NumberFormat.Format(gain)) };
            if (eco.LastRelicGalaxy != null)
                lines.Add(string.Format(Strings.RelicGained, eco.LastRelicGalaxy.relicName, eco.RelicLevel(eco.LastRelicGalaxy)));
            if (eco.LastWarpChest.Count > 0)
                lines.Add(string.Format(Strings.ShardsFound, ShipText.Shards(eco, eco.LastWarpChest)));
            Popup.Show(Ctx.PopupRoot, Strings.WarpReportTitle, string.Join("\n", lines), new Popup.Choice(Strings.Close, Theme.Buy));
            Ctx.RequestRefresh();
        }

        void ConfirmCrunch()
        {
            if (!Eco.CanCrunch) return;
            Popup.Show(Ctx.PopupRoot, Strings.SectionCrunch,
                string.Format(Strings.CrunchConfirmBody, NumberFormat.Format(Eco.PendingSingularities)),
                new Popup.Choice(Strings.CrunchButton, Theme.Premium, () =>
                {
                    double gain = Ctx.Game.DoCrunch();
                    if (gain <= 0) return;
                    Ctx.Flash();
                    Ctx.Toast(string.Format(Strings.CrunchDone, NumberFormat.Format(gain)));
                    Ctx.RequestRefresh();
                }),
                new Popup.Choice(Strings.Cancel, Theme.Disabled));
        }
    }
}
