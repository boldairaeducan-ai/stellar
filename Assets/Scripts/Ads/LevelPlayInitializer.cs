using System;
using StellarIdle;
using Unity.Services.LevelPlay;
using UnityEngine;

/// <summary>
/// Initializes LevelPlay once the player has answered the GDPR consent question,
/// and keeps the GDPR / CCPA settings in sync with the player's choices.
/// Lives on a persistent "Ads" GameObject in the first scene (App Key set in the Inspector).
/// </summary>
/// <remarks>
/// Kept outside the StellarIdle assemblies (no asmdef in this folder) so it can see the LevelPlay package;
/// the game talks to ads only through StellarIdle.Services.IAdsService.
/// </remarks>
public class LevelPlayInitializer : MonoBehaviour
{
    const float RetryDelaySeconds = 15f;
    const int MaxRetries = 3;

    [SerializeField] private string appKey = "290385fad";

    public static LevelPlayInitializer Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    /// <summary>Raised once the SDK is ready; ad objects must only be created after this.</summary>
    public event Action Initialized;

    bool initStarted;
    int retries;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Persist across scene loads so ads stay initialized
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PrivacyConsent.Changed += OnPrivacyChanged;

        // Privacy settings must be applied BEFORE LevelPlay.Init(): wait for the consent popup on first launch.
        if (PrivacyConsent.HasGdprAnswer) InitializeLevelPlay();
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        PrivacyConsent.Changed -= OnPrivacyChanged;
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        Instance = null;
    }

    void OnPrivacyChanged()
    {
        if (!PrivacyConsent.HasGdprAnswer) return;
        if (initStarted) ApplyPrivacySettings(); // applies to future ad requests
        else InitializeLevelPlay();
    }

    static void ApplyPrivacySettings()
    {
        LevelPlayPrivacySettings.SetGDPRConsent(PrivacyConsent.GdprConsent);
        LevelPlayPrivacySettings.SetCCPA(PrivacyConsent.CcpaOptOut);
    }

    void InitializeLevelPlay()
    {
        if (string.IsNullOrEmpty(appKey))
        {
            Debug.LogError("LevelPlay: App Key is empty. Set it on the LevelPlayInitializer component.");
            return;
        }
        initStarted = true;
        ApplyPrivacySettings();

        // Register initialization callbacks before Init()
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
        IsInitialized = true;
        Initialized?.Invoke();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay initialization failed: {error.ErrorCode} {error.ErrorMessage}");
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        // Usually no network at startup: retry a few times. Rewarded ads simply stay unavailable meanwhile.
        if (retries++ < MaxRetries) Invoke(nameof(InitializeLevelPlay), RetryDelaySeconds);
    }
}
