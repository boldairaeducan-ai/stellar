using StellarIdle.Core;
using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>In-app purchases, crystal sinks, restore and privacy settings.</summary>
    public sealed class ShopPanel : TabPanel
    {
        readonly ListRow starter, vip, crystalsSmall, crystalsLarge, timeWarp, restore, privacy;

        public ShopPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Shop")
        {
            starter = new ListRow(Content, "StarterPack", () => Purchase(ProductIds.StarterPack));
            vip = new ListRow(Content, "VipPass", () => Purchase(ProductIds.VipPass));
            crystalsSmall = new ListRow(Content, "CrystalsSmall", () => Purchase(ProductIds.CrystalsSmall));
            crystalsLarge = new ListRow(Content, "CrystalsLarge", () => Purchase(ProductIds.CrystalsLarge));
            timeWarp = new ListRow(Content, "TimeWarp", DoTimeWarp);
            restore = new ListRow(Content, "Restore", () => Ctx.Game.Store.RestorePurchases(), height: 130);
            privacy = new ListRow(Content, "Privacy", () => Ctx.OpenPrivacy(), height: 130);
        }

        public override void Refresh(long now)
        {
            var eco = Eco;
            var b = eco.Balance;
            var store = Ctx.Game.Store;

            starter.Visible = !eco.State.starterPackOwned;
            starter.SetIcon(null, Theme.Ad, "S");
            starter.SetTexts(Strings.StarterTitle,
                string.Format(Strings.StarterDetails, b.starterPackCrystals, b.starterPackBoostHours));
            starter.SetButton(store.GetPrice(ProductIds.StarterPack), store.IsReady, Theme.Ad);

            vip.SetIcon(null, Theme.Accent, "V");
            vip.SetTexts(Strings.VipTitle, Strings.VipDetails);
            if (eco.State.vip) vip.SetButton(Strings.ShopOwned, false, Theme.Accent);
            else vip.SetButton(store.GetPrice(ProductIds.VipPass), store.IsReady, Theme.Accent);

            crystalsSmall.SetIcon(Art.CrystalIcon, Theme.Premium, "C");
            crystalsSmall.SetTexts(string.Format(Strings.CrystalsSmallName, b.crystalsSmallAmount), Strings.CrystalsDetails);
            crystalsSmall.SetButton(store.GetPrice(ProductIds.CrystalsSmall), store.IsReady, Theme.Premium);

            crystalsLarge.SetIcon(Art.CrystalIcon, Theme.Premium, "C");
            crystalsLarge.SetTexts(string.Format(Strings.CrystalsLargeName, b.crystalsLargeAmount), Strings.CrystalsDetails);
            crystalsLarge.SetButton(store.GetPrice(ProductIds.CrystalsLarge), store.IsReady, Theme.Premium);

            timeWarp.SetIcon(null, Theme.Prestige, "T");
            timeWarp.SetTexts(Strings.TimeWarpName,
                string.Format(Strings.TimeWarpDetails, b.timeWarpHours, NumberFormat.Format(eco.TimeWarpValue)));
            timeWarp.SetButton(string.Format(Strings.CrystalCost, b.timeWarpCrystalCost),
                eco.State.crystals >= b.timeWarpCrystalCost && eco.TimeWarpValue > 0, Theme.Prestige);

            restore.SetIcon(null, Theme.Disabled, "R");
            restore.SetTexts(Strings.Restore, Strings.RestoreDetails);
            restore.SetButton(Strings.Open, true, Theme.PanelLight);

            privacy.SetIcon(null, Theme.Disabled, "P");
            privacy.SetTexts(Strings.PrivacyTitle, Strings.PrivacyDetails);
            privacy.SetButton(Strings.Open, true, Theme.PanelLight);
        }

        void Purchase(string productId)
        {
            Ctx.Game.Store.Purchase(productId, ok =>
            {
                if (!ok) Ctx.Toast(Strings.PurchaseFailed);
            });
        }

        void DoTimeWarp()
        {
            if (Eco.TryTimeWarp(out double gained))
            {
                timeWarp.Punch(0.1f);
                Ctx.Game.Save();
                Ctx.Toast("+" + NumberFormat.Format(gained));
                Ctx.RequestRefresh();
            }
            else Ctx.Toast(Strings.NotEnoughCrystals);
        }
    }
}
