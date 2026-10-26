using System;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>One line of the generator list: icon, name, stats and buy button.</summary>
    public sealed class GeneratorRow
    {
        readonly int index;
        readonly GameObject root;
        readonly Image icon;
        readonly Text title;
        readonly Text details;
        readonly Button buyButton;
        readonly Text buyLabel;
        readonly Color iconColor;

        public GeneratorRow(Transform parent, int index, Action<int> onBuy)
        {
            this.index = index;
            iconColor = Theme.GeneratorColor(index);

            var bg = UIFactory.Image($"Generator{index}", parent, Theme.Panel, ProceduralSprites.Rounded);
            bg.WithLayout(preferredHeight: 170);
            root = bg.gameObject;

            icon = UIFactory.Image("Icon", bg.transform, iconColor, ProceduralSprites.Circle);
            icon.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            icon.rectTransform.anchoredPosition = new Vector2(85, 0);
            icon.rectTransform.sizeDelta = new Vector2(110, 110);

            title = UIFactory.Text("Name", bg.transform, "", 42, Theme.Text, TextAnchor.LowerLeft, FontStyle.Bold);
            title.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(165, 0), new Vector2(-330, -14));

            details = UIFactory.Text("Details", bg.transform, "", 32, Theme.TextDim, TextAnchor.UpperLeft);
            details.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(165, 10), new Vector2(-330, -4));

            buyButton = UIFactory.Button("Buy", bg.transform, "", Theme.Buy, () => onBuy(index), out buyLabel, 34);
            buyButton.GetComponent<RectTransform>().Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-310, 18), new Vector2(-18, -18));
        }

        public void Refresh(IdleEconomy eco, int buyMode, long now)
        {
            bool unlocked = eco.IsUnlocked(index);
            bool teaser = !unlocked && index > 0 && eco.IsUnlocked(index - 1);
            root.SetActive(unlocked || teaser);
            if (!root.activeSelf) return;

            var def = eco.Def(index);
            int owned = eco.Owned(index);
            int amount = eco.ResolveBuyAmount(index, buyMode);
            double cost = eco.CostOf(index, amount);
            bool affordable = eco.CanAfford(index, amount);

            if (unlocked)
            {
                title.text = $"{def.name}  <color=#FFC440>{owned}</color>";
                title.supportRichText = true;
                int next = eco.NextMilestone(owned);
                string milestone = next > 0 ? $"   {Strings.NextBonus} {next}" : "";
                details.text = $"+{NumberFormat.Format(eco.GeneratorProduction(index, now))}{Strings.PerSecond}{milestone}";
                icon.color = iconColor;
            }
            else
            {
                title.text = Strings.Locked;
                details.text = "";
                icon.color = Theme.Disabled;
            }

            buyButton.interactable = affordable;
            buyButton.image.color = affordable ? Theme.Buy : Theme.Disabled;
            buyLabel.text = $"{Strings.Buy} x{amount}\n{NumberFormat.Format(cost)}";
        }
    }
}
