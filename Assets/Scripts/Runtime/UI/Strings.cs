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

        public const string PrestigeTitle = "Warp to a new galaxy";
        public const string PrestigeConfirm = "Warp!";
        public const string PrestigeDone = "+{0} Dark Matter";

        public const string CrystalsSmallName = "{0} Crystals";
        public const string CrystalsLargeName = "{0} Crystals (best value)";
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

        public const string ConsentTitle = "Your privacy";
        public const string ConsentBody = "Stellar Idle is free thanks to optional video ads.\nCan our ad partners use data from your device to show you more relevant ads?\nYou can change this anytime in Shop > Privacy.";
        public const string ConsentAccept = "Accept";
        public const string ConsentDecline = "Decline (non-personalized ads)";
        public const string PrivacyTitle = "Privacy settings";
        public const string PrivacyPersonalized = "Personalized ads: {0}";
        public const string PrivacyDoNotSell = "Do not sell my personal info: {0}";
        public const string PrivacyPolicy = "Privacy policy";
        public const string On = "ON";
        public const string Off = "OFF";

        public const string DropTitle = "Cargo pod!";
        public const string DropBody = "A supply pod drifted by your planet.\nWatch a short video to collect {0} credits.";
        public const string DropBodyVip = "A supply pod drifted by your planet.\nCollect {0} credits.";
        public const string DropWatch = "Watch ad";
        public const string DropCollectVip = "Collect (VIP)";
        public const string DropSkip = "No thanks";
        public const string DropCollected = "+{0} credits!";

        // Tabs
        public const string TabBuild = "Build";
        public const string TabResearch = "Research";
        public const string TabWarp = "Warp";
        public const string TabGoals = "Goals";
        public const string TabShop = "Shop";

        // Buildings
        public const string MilestoneReached = "{0} production x2!";
        public const string AllMilestones = "All bonuses reached";

        // Research
        public const string ResearchHeader = "Research multiplies production. It resets when you warp.";
        public const string ResearchName = "{0} upgrade {1}";
        public const string ResearchGlobalName = "Fleet coordination {0}";
        public const string ResearchEffect = "{0} production x{1}";
        public const string ResearchGlobalEffect = "All buildings x{0}";
        public const string ResearchRequires = "Requires {0} {1}";
        public const string ResearchDone = "All research complete. Time to warp!";

        // Warp / Dark Matter
        public const string WarpHeader = "Dark Matter: {0}";
        public const string WarpBonus = "Production bonus x{0}  (+{1}% per Dark Matter ever earned)";
        public const string WarpReady = "Warp now for <color=#FFC440>+{0} Dark Matter</color>\nBonus x{1} -> x{2}";
        public const string WarpNotReady = "Earn {0} credits in this galaxy to warp.";
        public const string WarpButton = "WARP";
        public const string WarpConfirmBody = "Your credits, buildings and research reset.\nYou keep Dark Matter, its upgrades, crystals and purchases.\n\nGain {0} Dark Matter?";
        public const string DarkMatterLevel = "Lv {0}";
        public const string DarkMatterLevelMax = "Lv {0}/{1}";
        public const string Maxed = "MAX";
        public const string DmProduction = "+{0}% production per level";
        public const string DmTap = "+{0}% of production per tap, per level";
        public const string DmCost = "Buildings {0}% cheaper per level";
        public const string DmStartCredits = "Start each galaxy with {0} credits";
        public const string DmOffline = "+{0}h offline earnings time per level";
        public const string DmBoost = "Ad boost lasts +{0}h per level";
        public const string DmBonusDrop = "Cargo pods give +{0}% per level";
        public const string DmCostLabel = "{0} DM";

        // Goals
        public const string GoalOwned = "Own {1} {0}";
        public const string GoalEarnings = "Earn {0} credits";
        public const string GoalWarps = "Warp {0} time(s)";
        public const string GoalClaim = "Claim\n+{0} crystals";
        public const string GoalReward = "+{0} crystals";
        public const string GoalDone = "Done";
        public const string GoalClaimed = "+{0} crystals!";

        // Shop
        public const string ShopOwned = "Owned";
        public const string TimeWarpName = "Time Warp";
        public const string TimeWarpDetails = "Instantly gain {0}h of production (+{1})";
        public const string CrystalCost = "{0} crystals";
        public const string StarterTitle = "Starter Pack";
        public const string VipTitle = "VIP Pass";
        public const string VipDetails = "x2 production forever, rewards without ads, 12h offline";
        public const string StarterDetails = "{0} crystals + {1}h x2 boost. One-time offer";
        public const string CrystalsDetails = "Spend them on Time Warps";
        public const string PrivacyDetails = "Personalized ads, data sharing";
        public const string Open = "Open";
        public const string RestoreDetails = "Recover VIP and Starter Pack";

        public const string AdUnavailable = "No ad available right now, try again later.";
        public const string Close = "Close";
        public const string Cancel = "Cancel";
    }
}
