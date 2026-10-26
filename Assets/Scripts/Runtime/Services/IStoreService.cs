using System;

namespace StellarIdle.Services
{
    /// <summary>
    /// In-app purchases. Implementations: MockStoreService (editor / development), Unity IAP later.
    /// </summary>
    public interface IStoreService
    {
        void Initialize();
        bool IsReady { get; }

        /// <summary>Localized price from the store (e.g. "4,99 €"), or a placeholder before init.</summary>
        string GetPrice(string productId);

        /// <summary>Starts a purchase. The callback only reports the UI outcome; rewards come from <see cref="ProductPurchased"/>.</summary>
        void Purchase(string productId, Action<bool> onComplete);

        void RestorePurchases();

        /// <summary>
        /// Raised once per purchase to deliver (new purchase, pending purchase recovered at startup, or restore).
        /// Rewards must be granted here and only here.
        /// </summary>
        event Action<string> ProductPurchased;
    }
}
