namespace StellarIdle.UI
{
    /// <summary>
    /// All player-facing text in one place (English first: largest market).
    /// Swap for Unity Localization string tables when adding languages.
    /// </summary>
    public static class Strings
    {
        public const string PerSecond = "/s";
        public const string Crystals = "Crystals";
        public const string DarkMatter = "Dark Matter";
        public const string TapHint = "Tap the planet!";

        public const string Owned = "Owned";
        public const string NextBonus = "x2 at";
        public const string Locked = "???";
        public const string Buy = "Buy";
        public const string Max = "MAX";

        public const string BoostButton = "x2 Boost";
        public const string BoostWatch = "Watch ad";
        public const string BoostFree = "VIP: free";
        public const string BoostActive = "x2 active";
        public const string BoostFull = "Boost is already at maximum.";
        public const string BoostAdded = "x2 boost activated!";

        public const string PrestigeButton = "Prestige";
        public const string PrestigeTitle = "Warp to a new galaxy";
        public const string PrestigeBody = "Reset your credits and buildings to gain {0} Dark Matter.\nEach point permanently boosts production by {1}%.\nYou currently have {2} ({3}).";
        public const string PrestigeLockedBody = "Earn {0} credits in this galaxy to unlock Dark Matter.\nProgress: {1}";
        public const string PrestigeConfirm = "Warp!";
        public const string PrestigeDone = "+{0} Dark Matter";

        public const string ShopButton = "Shop";
        public const string ShopTitle = "Shop";
        public const string VipName = "VIP Pass: x2 forever + skip all ads";
        public const string StarterName = "Starter Pack: {0} crystals + {1}h x2 boost";
        public const string CrystalsSmallName = "{0} Crystals";
        public const string CrystalsLargeName = "{0} Crystals (best value)";
        public const string TimeWarp = "Time Warp: +{0} credits ({1} crystals)";
        public const string NotEnoughCrystals = "Not enough crystals.";
        public const string Restore = "Restore purchases";
        public const string PurchaseThanks = "Thank you for your support!";
        public const string PurchaseFailed = "Purchase was not completed.";

        public const string OfflineTitle = "Welcome back, Commander!";
        public const string OfflineBody = "Your fleet kept working for {0}.\nYou earned {1} credits.";
        public const string OfflineCapped = "\n(Offline time is limited to {0}h.)";
        public const string OfflineCollect = "Collect";
        public const string OfflineDouble = "x2 (ad)";
        public const string OfflineDoubleVip = "x2 (VIP)";

        public const string AdUnavailable = "No ad available right now, try again later.";
        public const string Close = "Close";
        public const string Cancel = "Cancel";
    }
}
