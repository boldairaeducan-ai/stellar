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
        public const string BranchIndustry = "Industry";
        public const string BranchCommand = "Command";
        public const string BranchLogistics = "Logistics";
        public const string TierTitle = "{0} · {1}";
        public const string TierLocked = "Unlocks at {0} Dark Matter earned ({1})";
        public const string TierChoose = "Choose one path";
        public const string TierChosen = "Path chosen";
        public const string SkillLevel = "Lv {0}/{1}";
        public const string SkillBlocked = "Other path chosen";
        public const string Maxed = "MAX";
        public const string DmCostLabel = "{0} DM";
        public const string PerLevel = " / lvl";
        public const string RespecTitle = "Reset skill tree";
        public const string RespecDetails = "Get back all {0} Dark Matter invested";
        public const string RespecFree = "Free";
        public const string RespecConfirm = "Reset all your choices and get {0} Dark Matter back?";
        public const string RespecDone = "Skill tree reset";

        public const string FxProduction = "+{0}% production";
        public const string FxProductionMult = "x{0} production";
        public const string FxTap = "+{0}% of /s per tap";
        public const string FxTapMult = "+{0}% tap power";
        public const string FxCost = "-{0}% building cost";
        public const string FxStartCredits = "Start galaxies with {0} credits";
        public const string FxOfflineHours = "+{0}h offline time";
        public const string FxOfflineEfficiency = "+{0}% offline earnings";
        public const string FxBoostHours = "+{0}h ad boost";
        public const string FxBoostMultiplier = "+x{0} ad boost power";
        public const string FxBonusDrop = "+{0}% cargo pod credits";
        public const string FxBonusDropRate = "Pods {0}% more often, +1/hour";
        public const string FxResearch = "+x{0} per research";
        public const string FxMilestone = "+x{0} per building bonus";
        public const string FxDarkMatterGain = "+{0}% Dark Matter from warps";
        public const string FxPrestigeBonus = "+{0}% bonus per Dark Matter";
        public const string FxAutoTap = "+{0} automatic taps/s";
        public const string FxTreeMaxLevel = "+{0} max level on every tree node";
        public const string FxGenesis = "Unlocks the Genesis tier of the tree";
        public const string FxAutoBuy = "Buys buildings automatically";
        public const string FxStartDarkMatter = "Start each cycle with {0} Dark Matter";

        // Warp sub-sections
        public const string SectionTree = "Skill tree";
        public const string SectionGalaxies = "Galaxies";
        public const string SectionCrunch = "Big Crunch";
        public const string CurrentGalaxy = "Current galaxy: <b>{0}</b>";
        public const string ChooseGalaxy = "Choose your destination";
        public const string ChooseGalaxyBody = "Credits, buildings and research reset. You keep Dark Matter, the tree, ships, relics, crystals and purchases.\n+{0} Dark Matter (x galaxy bonus applies to the next warp).";
        public const string GalaxyChoice = "{0}  <color=#FFC440>x{1} DM</color>\n<size=24>{2}</size>";
        public const string GalaxyLocked = "Unlocks after {0} warps";
        public const string GalaxyHere = "You are here";
        public const string GalaxyNoModifier = "No modifier";
        public const string ModNoTap = "No taps";
        public const string ModCost = "Buildings x{0} cost";
        public const string ModProduction = "Production x{0}";
        public const string ModMaxGenerators = "Only {0} building types";
        public const string ModNoResearch = "No research";
        public const string ModNoOffline = "No offline earnings";
        public const string RelicLine = "Relic <color=#FFC440>{0}</color> Lv {1}/{2}: {3}";
        public const string RelicNext = "Next level: earn {0} in one run here";
        public const string RelicMaxed = "Relic fully upgraded";
        public const string RelicGained = "Relic {0} reached level {1}!";
        public const string WarpReportTitle = "Warp complete";
        public const string TapsDisabled = "Taps don't work in this galaxy.";
        public const string ResearchDisabled = "Research is disabled in {0}.";
        public const string GalaxyBuildingsLimit = "{0}: only {1} building types here.";

        // Big Crunch
        public const string CrunchHeader = "Singularities: {0}";
        public const string CrunchBonus = "Production x{0}  (+{1}% per Singularity ever earned)";
        public const string CrunchReady = "Big Crunch now for <color=#FFC440>+{0} Singularities</color>";
        public const string CrunchNotReady = "Earn {0} Dark Matter this cycle to collapse the universe.\nProgress: {1}";
        public const string CrunchButton = "BIG CRUNCH";
        public const string CrunchConfirmBody = "Resets Dark Matter, the skill tree, credits, buildings and research.\nYou keep Singularities, cosmic upgrades, ships, relics, galaxies, crystals and purchases.\n\nGain {0} Singularities?";
        public const string CrunchDone = "+{0} Singularities";
        public const string CosmicCostLabel = "{0} Sg";
        public const string CosmicLevel = "Lv {0}/{1}";

        // Automation
        public const string AutoBuyTitle = "Automation Core";
        public const string AutoBuyOn = "Buying buildings automatically";
        public const string AutoBuyOff = "Paused";

        // Fleet
        public const string TabFleet = "Fleet";
        public const string FleetHeader = "Active fleet  {0}/{1}";
        public const string FleetNextSlot = "Next slot after {0} warps";
        public const string FleetAllSlots = "All slots unlocked";
        public const string SlotEmpty = "Empty slot\n<size=22>Equip a ship below</size>";
        public const string SlotLocked = "Locked\n<size=22>{0} warps</size>";
        public const string ExpeditionsHeader = "Expeditions  {0}/{1}";
        public const string ExpeditionTitle = "{0} -> {1}";
        public const string ExpeditionLeft = "{0} left";
        public const string ExpeditionReady = "Back home!";
        public const string ExpeditionCollect = "Collect";
        public const string ExpeditionSpeedUp = "Speed up\n(ad)";
        public const string ExpeditionSpeedUpVip = "Speed up\n(VIP)";
        public const string DestinationDetails = "{0} shard rolls{1}, credits";
        public const string DestinationCrystals = ", {0} crystals";
        public const string Send = "Send";
        public const string ChooseShip = "Choose a ship";
        public const string ChooseShipBody = "{0} ({1}). Ships in the active fleet stay home.";
        public const string NoIdleShip = "No ship available: unequip one or wait for a return.";
        public const string ExpeditionDone = "Expedition complete!";
        public const string ExpeditionLoot = "+{0} credits{1}\n{2}";
        public const string HangarHeader = "Hangar  {0}/{1} ships";
        public const string ShipTitle = "{0}  <color=#{1}>{2}</color>";
        public const string ShipUnknown = "Unknown {0} ship";
        public const string ShipLevel = "Lv {0}/{1}";
        public const string ShipShards = "{0}/{1} shards";
        public const string ShipShardsMax = "Max level";
        public const string ShipBuild = "Build";
        public const string ShipStarUp = "Level up";
        public const string ShipEquip = "Equip";
        public const string ShipUnequip = "Unequip";
        public const string ShipOnMission = "On mission";
        public const string ShipBuilt = "{0} joins your fleet!";
        public const string ShipLeveled = "{0} reached level {1}!";
        public const string FleetFull = "Fleet is full: unequip a ship first.";
        public const string DistressTitle = "Distress signal";
        public const string DistressStreak = "  <color=#FFC440>{0}-day streak</color>";
        public const string DistressDetails = "Answer for {0} shard rolls";
        public const string DistressNextBonus = " (+1 roll at a {0}-day streak)";
        public const string DistressDone = "Next signal tomorrow. Come back to keep your streak!";
        public const string DistressAnswer = "Answer\n(ad)";
        public const string DistressAnswerVip = "Answer\n(VIP)";
        public const string DistressTomorrow = "Tomorrow";
        public const string DistressResultTitle = "Survivors rescued!";
        public const string ShardsFound = "Shards: {0}";
        public const string RoleMiner = "+{0}% {1} production";
        public const string RoleCruiser = "+{0}% tap power";
        public const string RoleFreighter = "+{0}% cargo pod credits";
        public const string RoleScout = "+{0}% offline earnings";
        public const string RoleFlagship = "+{0}% all production";
        public const string RoleHarbinger = "+{0}% Dark Matter from warps";

        // Goals
        public const string GoalOwned = "Own {1} {0}";
        public const string GoalEarnings = "Earn {0} credits";
        public const string GoalWarps = "Warp {0} time(s)";
        public const string GoalShipsBuilt = "Build {0} ships";
        public const string GoalRarityComplete = "Complete all {0} ships";
        public const string GoalShipsMaxed = "Bring {0} ship(s) to max level";
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
