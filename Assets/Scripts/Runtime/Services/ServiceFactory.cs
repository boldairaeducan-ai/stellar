using UnityEngine;
using Object = UnityEngine.Object;

namespace StellarIdle.Services
{
    /// <summary>
    /// Single place where the concrete ads / store implementations are chosen.
    /// When the real SDKs are integrated, return them here; nothing else in the game changes.
    /// </summary>
    public static class ServiceFactory
    {
        static IAdsService registeredAds;

        /// <summary>
        /// Called by the real ads implementation (LevelPlayRewardedAdManager, in the scene) before the game starts.
        /// Without it (no "Ads" object in the scene) the game falls back to mock ads.
        /// </summary>
        public static void RegisterAds(IAdsService ads) => registeredAds = ads;

        public static IAdsService CreateAds(MonoBehaviour host) =>
            registeredAds as Object != null ? registeredAds : new MockAdsService(host);

        public static IStoreService CreateStore(MonoBehaviour host) => new MockStoreService(host);
    }
}
