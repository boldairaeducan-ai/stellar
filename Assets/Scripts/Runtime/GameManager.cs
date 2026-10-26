using System;
using StellarIdle.Core;
using StellarIdle.Services;
using UnityEngine;

namespace StellarIdle
{
    public static class Clock
    {
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Owns the economy, the save and the services; drives the simulation and the app lifecycle.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        const float AutosaveInterval = 15f;

        public static GameManager Instance { get; private set; }

        public IdleEconomy Economy { get; private set; }
        public IAdsService Ads { get; private set; }
        public IStoreService Store { get; private set; }
        public OfflineReport PendingOffline { get; private set; }

        public event Action<OfflineReport> OfflineEarningsAvailable;
        public event Action<string> ProductDelivered;

        SaveService save;
        float autosaveTimer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;

            save = new SaveService();
            var balance = LoadBalance();
            var state = save.Load() ?? GameState.CreateNew(Clock.Now);
            Economy = new IdleEconomy(balance, state);

            Ads = ServiceFactory.CreateAds(this);
            Store = ServiceFactory.CreateStore(this);
            Store.ProductPurchased += OnProductPurchased;
            Ads.Initialize();
            Store.Initialize();

            CheckOffline();
        }

        void Update()
        {
            // deltaTime is clamped by Time.maximumDeltaTime, so a long pause is never counted twice
            // (time away is handled by the offline earnings).
            Economy.Tick(Time.deltaTime, Clock.Now);

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= AutosaveInterval) Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
            else CheckOffline();
        }

        void OnApplicationQuit() => Save();

        public void Save()
        {
            autosaveTimer = 0;
            Economy.MarkSeen(Clock.Now);
            save.Save(Economy.State);
        }

        // ---------- Offline earnings ----------

        void CheckOffline()
        {
            var report = Economy.CollectOffline(Clock.Now);
            if (!report.HasEarnings) return;
            PendingOffline = PendingOffline.Merge(report);
            OfflineEarningsAvailable?.Invoke(PendingOffline);
        }

        public void ClaimOffline(bool doubled)
        {
            if (!PendingOffline.HasEarnings) return;
            Economy.Earn(PendingOffline.Amount * (doubled ? 2 : 1));
            PendingOffline = OfflineReport.None;
            Save();
        }

        // ---------- Monetization ----------

        /// <summary>VIP players get every ad reward instantly, without watching.</summary>
        public bool SkipsAds => Economy.State.vip;

        public void RequestReward(string placement, Action onRewarded, Action onFailed = null)
        {
            if (SkipsAds)
            {
                onRewarded();
                return;
            }
            if (!Ads.IsRewardedReady)
            {
                onFailed?.Invoke();
                return;
            }
            Ads.ShowRewarded(placement, granted =>
            {
                if (granted) onRewarded();
                else onFailed?.Invoke();
            });
        }

        void OnProductPurchased(string productId)
        {
            if (!Economy.GrantProduct(productId, Clock.Now))
            {
                Debug.LogWarning($"[Store] Unknown product '{productId}'");
                return;
            }
            Save();
            ProductDelivered?.Invoke(productId);
        }

        // ---------- Prestige ----------

        public double DoPrestige()
        {
            double gain = Economy.Prestige();
            Save();
            return gain;
        }

        [ContextMenu("Delete save and reset")]
        public void ResetProgress()
        {
            save.Delete();
            Economy = new IdleEconomy(LoadBalance(), GameState.CreateNew(Clock.Now));
        }

        static GameBalance LoadBalance()
        {
            var asset = Resources.Load<TextAsset>("GameBalance");
            if (asset == null) throw new InvalidOperationException("Missing Resources/GameBalance.json");
            return JsonUtility.FromJson<GameBalance>(asset.text);
        }
    }
}
