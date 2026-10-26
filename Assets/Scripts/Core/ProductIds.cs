namespace StellarIdle.Core
{
    /// <summary>
    /// Store product ids. They must match exactly the products created in Google Play Console
    /// (and in the Unity IAP catalog).
    /// </summary>
    public static class ProductIds
    {
        public const string VipPass = "vip_pass";             // non-consumable
        public const string StarterPack = "starter_pack";     // non-consumable, one-time offer
        public const string CrystalsSmall = "crystals_small"; // consumable
        public const string CrystalsLarge = "crystals_large"; // consumable

        public static readonly string[] All = { VipPass, StarterPack, CrystalsSmall, CrystalsLarge };

        public static bool IsConsumable(string id) => id == CrystalsSmall || id == CrystalsLarge;
    }

    /// <summary>Rewarded ad placement names (used for analytics and per-placement capping).</summary>
    public static class AdPlacements
    {
        public const string OfflineDouble = "offline_double";
        public const string Boost = "boost";
    }
}
