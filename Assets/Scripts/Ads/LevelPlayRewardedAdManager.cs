using System;
using System.Collections;
using StellarIdle.Services;
using Unity.Services.LevelPlay;
using UnityEngine;

/// <summary>
/// LevelPlay rewarded ads, exposed to the game as <see cref="IAdsService"/>.
/// Lives on the persistent "Ads" GameObject next to <see cref="LevelPlayInitializer"/>
/// (added automatically by the initializer if missing).
/// </summary>
public class LevelPlayRewardedAdManager : MonoBehaviour, IAdsService
{
    const float LoadRetryDelaySeconds = 30f;
    // OnAdRewarded can arrive after OnAdClosed: wait a little before concluding "no reward".
    const float RewardGraceSeconds = 1.5f;

    [SerializeField] private string adUnitId = "p15gaz3j25581h18";
    [Tooltip("Pass placement names (boost, offline_double, bonus_drop) to ShowAd. Enable only once these placements exist in the LevelPlay dashboard.")]
    [SerializeField] private bool usePlacements;

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInitializer initializer;
    private bool isLoadingAd;
    private Action<bool> pendingCallback;

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.IsAdReady();

    void Awake()
    {
        // Must happen before the GameManager is created (Bootstrap runs after scene Awake calls).
        ServiceFactory.RegisterAds(this);
    }

    void Start()
    {
        initializer = LevelPlayInitializer.Instance;
        if (initializer == null)
        {
            Debug.LogError("LevelPlayRewardedAdManager: no LevelPlayInitializer found in the scene.");
            return;
        }
        // Ad objects may only be created after LevelPlay.Init succeeded.
        if (initializer.IsInitialized) CreateRewardedAd();
        else initializer.Initialized += CreateRewardedAd;
    }

    void OnDestroy()
    {
        if (initializer != null) initializer.Initialized -= CreateRewardedAd;
        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            rewardedAd.OnAdRewarded -= OnAdRewarded;
            rewardedAd.OnAdClosed -= OnAdClosed;
            rewardedAd.OnAdClicked -= OnAdClicked;
            rewardedAd.OnAdImpressionDataReady -= OnImpressionDataReady;
        }
    }

    // IAdsService: LevelPlay itself is initialized by LevelPlayInitializer (after privacy consent).
    public void Initialize() { }

    private void CreateRewardedAd()
    {
        if (rewardedAd != null) return;
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogError("LevelPlayRewardedAdManager: Ad Unit ID is empty.");
            return;
        }

        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        // ILRD (SDK 9.5.0+): impression revenue, per ad instance.
        rewardedAd.OnAdImpressionDataReady += OnImpressionDataReady;

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;
        rewardedAd.OnAdClicked += OnAdClicked;

        // Eager preload: the game offers ads (boost button, cargo pods) only when one is ready.
        LoadAd();
    }

    public void LoadAd()
    {
        if (rewardedAd == null || isLoadingAd || rewardedAd.IsAdReady()) return; // avoid error 1037
        isLoadingAd = true;
        Debug.Log("Loading rewarded ad...");
        rewardedAd.LoadAd();
    }

    public void ShowRewarded(string placement, Action<bool> onComplete)
    {
        if (pendingCallback != null)
        {
            Debug.LogWarning("Rewarded ad already showing");
            onComplete?.Invoke(false);
            return;
        }
        if (!IsRewardedReady)
        {
            Debug.LogWarning("Rewarded ad is not ready yet");
            LoadAd();
            onComplete?.Invoke(false);
            return;
        }
        if (usePlacements && !string.IsNullOrEmpty(placement) && LevelPlayRewardedAd.IsPlacementCapped(placement))
        {
            Debug.LogWarning($"Placement '{placement}' is capped");
            onComplete?.Invoke(false);
            return;
        }

        pendingCallback = onComplete;
        Debug.Log($"Showing rewarded ad ({placement})");
        if (usePlacements && !string.IsNullOrEmpty(placement)) rewardedAd.ShowAd(placementName: placement);
        else rewardedAd.ShowAd();
    }

    private void Complete(bool rewarded)
    {
        var callback = pendingCallback;
        pendingCallback = null;
        callback?.Invoke(rewarded);
    }

    // ---------- Event callbacks (main thread) ----------

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        isLoadingAd = false;
        Debug.Log("Rewarded ad loaded successfully");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        isLoadingAd = false;
        Debug.LogWarning($"Rewarded ad failed to load: {error.ErrorCode} {error.ErrorMessage}");
        // The game keeps working: ad buttons and cargo pods just wait for the next fill.
        CancelInvoke(nameof(LoadAd));
        Invoke(nameof(LoadAd), LoadRetryDelaySeconds);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad displayed");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded ad failed to display: {error.ErrorCode} {error.ErrorMessage}");
        Complete(false);
        LoadAd();
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"User earned reward ({adInfo?.PlacementName})");
        Complete(true); // rewards are granted by the game through the callback
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad closed");
        if (pendingCallback != null) StartCoroutine(CompleteWithoutRewardAfterGrace());
        LoadAd(); // reload so the next opportunity has an ad ready
    }

    private IEnumerator CompleteWithoutRewardAfterGrace()
    {
        yield return new WaitForSecondsRealtime(RewardGraceSeconds);
        if (pendingCallback != null) Complete(false); // closed before the reward was earned
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad clicked");
    }

    private void OnImpressionDataReady(LevelPlayImpressionData impressionData)
    {
        // Runs on a BACKGROUND thread: no Unity API calls here except logging.
        // Forward to your analytics platform (Firebase, AppsFlyer, Adjust...) later.
        if (impressionData == null) return;
        Debug.Log($"ILRD - {impressionData.AdNetwork} / {impressionData.AdFormat} / ${impressionData.Revenue}");
    }
}
