using System;
using System.Collections;
using UnityEngine;

namespace StellarIdle.Services
{
    /// <summary>Fake rewarded ads: waits a moment then grants the reward. For the editor and dev builds only.</summary>
    public sealed class MockAdsService : IAdsService
    {
        readonly MonoBehaviour host;

        public MockAdsService(MonoBehaviour host) => this.host = host;

        public bool IsRewardedReady => true;

        public void Initialize()
        {
            if (!Debug.isDebugBuild)
                Debug.LogError("[Ads] MockAdsService is used in a release build. Plug in the real ads SDK before publishing.");
        }

        public void ShowRewarded(string placement, Action<bool> onComplete)
        {
            Debug.Log($"[Ads] (mock) rewarded ad '{placement}'");
            host.StartCoroutine(Complete(onComplete));
        }

        static IEnumerator Complete(Action<bool> onComplete)
        {
            yield return new WaitForSecondsRealtime(0.75f);
            onComplete?.Invoke(true);
        }
    }
}
