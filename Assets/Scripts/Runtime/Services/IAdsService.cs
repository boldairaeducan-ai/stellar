using System;

namespace StellarIdle.Services
{
    /// <summary>
    /// Rewarded ads only: the player always chooses to watch, in exchange for a bonus.
    /// Implementations: MockAdsService (editor / development), LevelPlay later.
    /// </summary>
    public interface IAdsService
    {
        void Initialize();
        bool IsRewardedReady { get; }

        /// <summary>Shows a rewarded ad. The callback receives true only if the reward must be granted.</summary>
        void ShowRewarded(string placement, Action<bool> onComplete);
    }
}
