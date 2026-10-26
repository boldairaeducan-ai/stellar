using System.Globalization;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>Prestige: warp card (gain preview, progress) and the Dark Matter upgrade tree.</summary>
    public sealed class WarpPanel : TabPanel
    {
        readonly Text titleText, bonusText, statusText;
        readonly RectTransform progressFill;
        readonly GameObject progressTrack;
        readonly Button warpButton;
        readonly PunchScale warpPunch;
        readonly ListRow[] rows;

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

            var upgrades = Eco.DarkMatterUpgrades;
            rows = new ListRow[upgrades.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var def = upgrades[i];
                rows[i] = new ListRow(Content, $"DarkMatter_{def.id}", () => BuyUpgrade(def));
            }
        }

        public override int Badge => (Eco.CanPrestige ? 1 : 0) + Eco.AffordableDarkMatterUpgradeCount;

        public override void Refresh(long now)
        {
            var eco = Eco;
            titleText.text = string.Format(Strings.WarpHeader, NumberFormat.Format(eco.State.darkMatter));
            bonusText.text = string.Format(Strings.WarpBonus, Multiplier(eco.PrestigeMultiplier),
                (eco.Balance.prestigeBonusPerPoint * 100).ToString("0.#", CultureInfo.InvariantCulture));

            bool ready = eco.CanPrestige;
            statusText.text = ready
                ? string.Format(Strings.WarpReady, NumberFormat.Format(eco.PendingDarkMatter),
                    Multiplier(eco.PrestigeMultiplier), Multiplier(eco.PrestigeMultiplierAfterWarp))
                : string.Format(Strings.WarpNotReady, NumberFormat.Format(eco.PrestigeRequirement));
            progressTrack.SetActive(!ready);
            progressFill.anchorMax = new Vector2((float)System.Math.Min(1, eco.State.runEarnings / eco.PrestigeRequirement), 1);
            warpButton.interactable = ready;
            if (ready && Time.unscaledTime % 2f < 0.1f) warpPunch.Play(0.04f); // gentle pulse when available

            var upgrades = eco.DarkMatterUpgrades;
            for (int i = 0; i < rows.Length; i++)
            {
                var def = upgrades[i];
                int level = eco.DarkMatterLevel(def);
                string levelText = def.maxLevel > 0
                    ? string.Format(Strings.DarkMatterLevelMax, level, def.maxLevel)
                    : string.Format(Strings.DarkMatterLevel, level);
                rows[i].SetIcon(Art.DarkMatterIcon, Theme.Prestige, Initial(def.name));
                rows[i].SetTexts($"{def.name}  <color=#468CF0>{levelText}</color>", Describe(def, level));
                rows[i].SetHighlight(eco.CanBuyDarkMatterUpgrade(def));
                if (eco.IsDarkMatterMaxed(def))
                    rows[i].SetButton(Strings.Maxed, false, Theme.Prestige);
                else
                    rows[i].SetButton(string.Format(Strings.DmCostLabel, NumberFormat.Format(eco.DarkMatterUpgradeCost(def))),
                        eco.CanBuyDarkMatterUpgrade(def), Theme.Prestige);
            }
        }

        static string Multiplier(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        static string Percent(double value) => (value * 100).ToString("0.#", CultureInfo.InvariantCulture);

        static string Describe(DarkMatterUpgradeDef def, int level)
        {
            switch (def.effect)
            {
                case DarkMatterEffect.Production: return string.Format(Strings.DmProduction, Percent(def.value));
                case DarkMatterEffect.Tap: return string.Format(Strings.DmTap, Percent(def.value));
                case DarkMatterEffect.Cost: return string.Format(Strings.DmCost, Percent(def.value));
                case DarkMatterEffect.StartCredits:
                    return string.Format(Strings.DmStartCredits, NumberFormat.Format(def.value * System.Math.Pow(10, level)));
                case DarkMatterEffect.OfflineHours: return string.Format(Strings.DmOffline, def.value);
                case DarkMatterEffect.BoostHours: return string.Format(Strings.DmBoost, def.value);
                case DarkMatterEffect.BonusDrop: return string.Format(Strings.DmBonusDrop, Percent(def.value));
                default: return def.effect;
            }
        }

        void BuyUpgrade(DarkMatterUpgradeDef def)
        {
            if (!Eco.TryBuyDarkMatterUpgrade(def)) return;
            Ctx.Game.Save();
            Ctx.RequestRefresh();
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
                    Ctx.RequestRefresh();
                }),
                new Popup.Choice(Strings.Cancel, Theme.Disabled));
        }
    }
}
