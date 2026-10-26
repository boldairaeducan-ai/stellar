using UnityEngine;

namespace StellarIdle.Services
{
    /// <summary>
    /// Single place where the concrete ads / store implementations are chosen.
    /// When the real SDKs are integrated, return them here; nothing else in the game changes.
    /// </summary>
    public static class ServiceFactory
    {
        public static IAdsService CreateAds(MonoBehaviour host) => new MockAdsService(host);

        public static IStoreService CreateStore(MonoBehaviour host) => new MockStoreService(host);
    }
}
