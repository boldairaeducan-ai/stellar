using System;
using UnityEngine;

namespace StellarIdle
{
    /// <summary>
    /// Player privacy choices (GDPR consent, CCPA "do not sell"), persisted on the device.
    /// The ads SDK listens to <see cref="Changed"/> and must not start before the GDPR answer exists.
    /// </summary>
    public static class PrivacyConsent
    {
        // TODO: replace with your real privacy policy URL (required by Google Play and ad networks).
        public const string PrivacyPolicyUrl = "https://example.com/stellar-idle/privacy";

        const string GdprKey = "privacy.gdpr_consent";
        const string CcpaKey = "privacy.ccpa_opt_out";

        public static event Action Changed;

        public static bool HasGdprAnswer => PlayerPrefs.HasKey(GdprKey);
        public static bool GdprConsent => PlayerPrefs.GetInt(GdprKey, 0) == 1;
        public static bool CcpaOptOut => PlayerPrefs.GetInt(CcpaKey, 0) == 1;

        public static void SetGdprConsent(bool consent) => Store(GdprKey, consent);
        public static void SetCcpaOptOut(bool optOut) => Store(CcpaKey, optOut);

        static void Store(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
