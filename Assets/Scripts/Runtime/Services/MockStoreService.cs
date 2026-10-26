using System;
using System.Collections;
using System.Collections.Generic;
using StellarIdle.Core;
using UnityEngine;

namespace StellarIdle.Services
{
    /// <summary>Fake store: every purchase succeeds. For the editor and dev builds only.</summary>
    public sealed class MockStoreService : IStoreService
    {
        static readonly Dictionary<string, string> Prices = new Dictionary<string, string>
        {
            { ProductIds.VipPass, "$4.99" },
            { ProductIds.StarterPack, "$1.99" },
            { ProductIds.CrystalsSmall, "$0.99" },
            { ProductIds.CrystalsLarge, "$4.99" },
        };

        readonly MonoBehaviour host;
        readonly HashSet<string> ownedNonConsumables = new HashSet<string>();

        public MockStoreService(MonoBehaviour host) => this.host = host;

        public event Action<string> ProductPurchased;
        public bool IsReady => true;

        public void Initialize()
        {
            if (!Debug.isDebugBuild)
                Debug.LogError("[Store] MockStoreService is used in a release build. Plug in Unity IAP before publishing.");
        }

        public string GetPrice(string productId) => Prices.TryGetValue(productId, out var p) ? p : "-";

        public void Purchase(string productId, Action<bool> onComplete)
        {
            Debug.Log($"[Store] (mock) purchase '{productId}'");
            host.StartCoroutine(Complete(productId, onComplete));
        }

        public void RestorePurchases()
        {
            foreach (var id in ownedNonConsumables) ProductPurchased?.Invoke(id);
        }

        IEnumerator Complete(string productId, Action<bool> onComplete)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (!ProductIds.IsConsumable(productId)) ownedNonConsumables.Add(productId);
            ProductPurchased?.Invoke(productId);
            onComplete?.Invoke(true);
        }
    }
}
