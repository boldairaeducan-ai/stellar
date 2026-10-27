using StellarIdle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace StellarIdle.UI
{
    /// <summary>
    /// The game screen, built from code: currencies, tappable planet, boost, tab panels
    /// (Build / Research / Warp / Goals / Shop), popups, toasts and visual feedback.
    /// </summary>
    public sealed class MainScreen : MonoBehaviour
    {
        static readonly int[] BuyModes = { 1, 10, 0 }; // 0 = max affordable
        const float RefreshInterval = 0.1f;
        const float ToastDuration = 2.2f;
        const int BuildTab = 0;

        GameManager game;
        IdleEconomy Eco => game.Economy;
        readonly UIContext ctx = new UIContext();

        Transform popupRoot;
        Text creditsText, rateText, crystalsText, darkMatterText, toastText, boostLabel, buyModeLabel;
        RectTransform planet, floatLayer;
        GameObject buyModeButton;
        Image flash;
        PunchScale creditsPunch, boostPunch;
        FloatingTextPool floats;
        TapParticles particles;
        TabPanel[] panels;
        TabBar tabBar;
        Popup offlinePopup;

        int buyModeIndex, currentTab;
        float refreshTimer, toastTimer, planetScale = 1;
        double displayedCredits;

        public static MainScreen Create(GameManager game)
        {
            var go = new GameObject("MainScreen");
            DontDestroyOnLoad(go);
            var screen = go.AddComponent<MainScreen>();
            screen.Build(game);
            return screen;
        }

        void Start()
        {
            game.OfflineEarningsAvailable += ShowOffline;
            game.ProductDelivered += OnProductDelivered;
            if (game.PendingOffline.HasEarnings) ShowOffline(game.PendingOffline);
            if (!PrivacyConsent.HasGdprAnswer) ShowConsent(); // shown last so it sits on top
            displayedCredits = Eco.State.credits;
            SelectTab(BuildTab);
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.OfflineEarningsAvailable -= ShowOffline;
            game.ProductDelivered -= OnProductDelivered;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            planetScale = Mathf.Lerp(planetScale, 1f, dt * 12f);
            planet.localScale = Vector3.one * planetScale;

            UpdateRollingCredits(dt);

            refreshTimer -= dt;
            if (refreshTimer <= 0) Refresh();

            if (toastTimer > 0)
            {
                toastTimer -= dt;
                var c = toastText.color;
                c.a = Mathf.Clamp01(toastTimer / 0.4f);
                toastText.color = c;
            }

            if (flash.color.a > 0)
            {
                var c = flash.color;
                c.a = Mathf.Max(0, c.a - dt * 1.5f);
                flash.color = c;
                if (c.a <= 0) flash.gameObject.SetActive(false);
            }

            if (BackPressed()) Popup.CloseTop();
        }

        /// <summary>The credit counter rolls up smoothly instead of jumping; spending snaps down.</summary>
        void UpdateRollingCredits(float dt)
        {
            double target = Eco.State.credits;
            if (target <= displayedCredits || target - displayedCredits < target * 0.002) displayedCredits = target;
            else displayedCredits += (target - displayedCredits) * Mathf.Clamp01(dt * 10f);

            string text = NumberFormat.Format(displayedCredits);
            if (creditsText.text != text) creditsText.text = text;
        }

        // ---------- Build ----------

        void Build(GameManager gameManager)
        {
            game = gameManager;
            EnsureEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            BuildBackground(root);

            var safe = UIFactory.Rect("SafeArea", root).Stretch();
            safe.gameObject.AddComponent<SafeArea>();

            ctx.Game = game;
            ctx.Toast = Toast;
            ctx.RequestRefresh = Refresh;
            ctx.OpenPrivacy = OpenPrivacy;
            ctx.Flash = Flash;

            BuildTopBar(safe);
            BuildPlanet(safe);
            BuildTabs(safe);

            var dropLayer = UIFactory.Rect("BonusDrops", safe).Stretch();
            BonusDropSpawner.Create(dropLayer, game, OnBonusDropTapped);

            particles = UIFactory.Rect("Particles", safe).Stretch().gameObject.AddComponent<TapParticles>();
            floatLayer = UIFactory.Rect("FloatingTexts", safe).Stretch();
            floats = floatLayer.gameObject.AddComponent<FloatingTextPool>();

            toastText = UIFactory.Text("Toast", root, "", 44, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            toastText.supportRichText = true;
            toastText.rectTransform.Anchor(new Vector2(0, 0.72f), new Vector2(1, 0.72f), new Vector2(40, -60), new Vector2(-40, 60));
            toastText.gameObject.AddComponent<Outline>().effectColor = Color.black;
            toastText.color = Color.clear;

            flash = UIFactory.Image("Flash", root, new Color(1, 1, 1, 0));
            flash.rectTransform.Stretch();
            flash.gameObject.SetActive(false);

            popupRoot = UIFactory.Rect("Popups", root).Stretch();
            ctx.PopupRoot = popupRoot;
        }

        static void BuildBackground(Transform root)
        {
            var custom = Art.Background;
            var background = UIFactory.Image("Background", root, custom != null ? Color.white : Theme.Background, custom);
            background.rectTransform.Stretch();
            if (custom != null) return;

            var rng = new System.Random(42);
            for (int i = 0; i < 80; i++)
            {
                float size = 3 + (float)rng.NextDouble() * 6;
                var star = UIFactory.Image("Star", background.transform, new Color(1, 1, 1, 0.2f + (float)rng.NextDouble() * 0.6f), ProceduralSprites.Circle);
                var pos = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                star.rectTransform.Place(pos, Vector2.zero, new Vector2(size, size));
            }
        }

        void BuildTopBar(Transform parent)
        {
            var bar = UIFactory.Rect("TopBar", parent).Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -240), Vector2.zero);

            crystalsText = UIFactory.Text("Crystals", bar, "", 34, Theme.Premium, TextAnchor.UpperLeft, FontStyle.Bold);
            crystalsText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(30, -70), new Vector2(0, -20));

            darkMatterText = UIFactory.Text("DarkMatter", bar, "", 34, Theme.Prestige, TextAnchor.UpperRight, FontStyle.Bold);
            darkMatterText.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -70), new Vector2(-30, -20));

            creditsText = UIFactory.Text("Credits", bar, "0", 92, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            creditsText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -180), new Vector2(0, -65));
            creditsPunch = creditsText.gameObject.AddComponent<PunchScale>();

            rateText = UIFactory.Text("Rate", bar, "", 38, Theme.TextDim);
            rateText.supportRichText = true;
            rateText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -235), new Vector2(0, -180));
        }

        void BuildPlanet(Transform parent)
        {
            var img = UIFactory.Image("Planet", parent, Color.white, Art.Planet, raycast: true);
            planet = img.rectTransform.Anchor(new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.835f));
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1;
            img.preserveAspect = true;
            img.gameObject.AddComponent<TapZone>().Tapped += OnPlanetTapped;

            var boost = UIFactory.Button("Boost", parent, "", Theme.Ad, OnBoostClicked, out boostLabel, 32);
            boost.GetComponent<RectTransform>().Anchor(new Vector2(0.03f, 0.545f), new Vector2(0.48f, 0.593f));
            boostPunch = boost.gameObject.AddComponent<PunchScale>();

            var buyMode = UIFactory.Button("BuyMode", parent, "", Theme.PanelLight, CycleBuyMode, out buyModeLabel, 34);
            buyMode.GetComponent<RectTransform>().Anchor(new Vector2(0.75f, 0.545f), new Vector2(0.97f, 0.593f));
            buyModeButton = buyMode.gameObject;
        }

        void BuildTabs(Transform parent)
        {
            var area = UIFactory.Rect("Panels", parent).Anchor(Vector2.zero, new Vector2(1, 0.535f), new Vector2(20, 170), new Vector2(-20, 0));
            panels = new TabPanel[]
            {
                new BuildingsPanel(ctx, area),
                new ResearchPanel(ctx, area),
                new WarpPanel(ctx, area),
                new FleetPanel(ctx, area),
                new GoalsPanel(ctx, area),
                new ShopPanel(ctx, area),
            };

            var bar = UIFactory.Rect("TabBar", parent).Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(12, 12), new Vector2(-12, 158));
            tabBar = new TabBar(bar, new[] { Strings.TabBuild, Strings.TabResearch, Strings.TabWarp, Strings.TabFleet, Strings.TabGoals, Strings.TabShop });
            tabBar.Selected += SelectTab;
        }

        void SelectTab(int tab)
        {
            currentTab = tab;
            for (int i = 0; i < panels.Length; i++) panels[i].Visible = i == tab;
            buyModeButton.SetActive(tab == BuildTab);
            tabBar.SetSelected(tab);
            Refresh();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(go);
        }

        static bool BackPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#elif HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        // ---------- Refresh ----------

        void Refresh()
        {
            refreshTimer = RefreshInterval;
            long now = Clock.Now;
            var eco = Eco;
            var state = eco.State;

            var galaxy = eco.CurrentGalaxy;
            string galaxyName = galaxy != null && galaxy != eco.Galaxies[0] ? $"   <color=#468CF0>{galaxy.name}</color>" : "";
            rateText.text = $"+{NumberFormat.Format(eco.IncomePerSecond(now))}{Strings.PerSecond}{galaxyName}";
            crystalsText.text = $"{Strings.Crystals}: {state.crystals}";
            darkMatterText.text = state.darkMatterEarned > 0 ? $"{Strings.DarkMatter}: {NumberFormat.Format(state.darkMatter)}" : "";

            boostLabel.text = eco.IsBoostActive(now)
                ? $"{Strings.BoostActive}  {NumberFormat.Duration(eco.BoostRemaining(now))}"
                : state.totalEarnings < 100
                    ? Strings.TapHint
                    : $"{Strings.BoostButton}: {(game.SkipsAds ? Strings.BoostFree : Strings.BoostWatch)}";

            int mode = BuyModes[buyModeIndex];
            ctx.BuyMode = mode;
            buyModeLabel.text = mode > 0 ? $"x{mode}" : Strings.Max;

            panels[currentTab].Refresh(now);
            for (int i = 0; i < panels.Length; i++) tabBar.SetBadge(i, panels[i].Badge);
        }

        void Toast(string message)
        {
            toastText.text = message;
            toastText.color = Theme.Text;
            toastTimer = ToastDuration;
        }

        void Flash()
        {
            flash.gameObject.SetActive(true);
            flash.color = new Color(1, 1, 1, 0.9f);
            creditsPunch.Play(0.2f);
        }

        // ---------- Actions ----------

        void OnPlanetTapped(PointerEventData eventData)
        {
            if (Eco.TapsDisabled)
            {
                if (toastTimer <= 0) Toast(Strings.TapsDisabled);
                return;
            }
            double value = Eco.Tap(Clock.Now);
            planetScale = 0.92f;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, eventData.position, eventData.pressEventCamera, out var local))
            {
                floats.Spawn("+" + NumberFormat.Format(value), local);
                particles.Burst(local);
            }
        }

        void CycleBuyMode()
        {
            buyModeIndex = (buyModeIndex + 1) % BuyModes.Length;
            Refresh();
        }

        void OnBoostClicked()
        {
            if (!Eco.CanAddBoost(Clock.Now))
            {
                Toast(Strings.BoostFull);
                return;
            }
            game.RequestReward(AdPlacements.Boost, () =>
            {
                Eco.AddBoost(Clock.Now, Eco.BoostDurationSeconds);
                game.Save();
                boostPunch.Play(0.12f);
                Toast(Strings.BoostAdded);
                Refresh();
            }, () => Toast(Strings.AdUnavailable));
        }

        void OnProductDelivered(string productId)
        {
            creditsPunch.Play(0.15f);
            Toast(Strings.PurchaseThanks);
            Refresh();
        }

        void OnBonusDropTapped()
        {
            string amount = NumberFormat.Format(Eco.BonusDropReward);
            bool vip = game.SkipsAds;
            Popup.Show(popupRoot, Strings.DropTitle, string.Format(vip ? Strings.DropBodyVip : Strings.DropBody, amount),
                new Popup.Choice(vip ? Strings.DropCollectVip : Strings.DropWatch, Theme.Ad, () =>
                    game.RequestReward(AdPlacements.BonusDrop, () =>
                    {
                        double gained = Eco.ClaimBonusDrop(Clock.Now);
                        game.Save();
                        creditsPunch.Play(0.15f);
                        string toast = string.Format(Strings.DropCollected, NumberFormat.Format(gained));
                        if (Eco.LastBonusDropShards.Count > 0)
                            toast += "\n" + string.Format(Strings.ShardsFound, ShipText.Shards(Eco, Eco.LastBonusDropShards));
                        Toast(toast);
                        Refresh();
                    }, () => Toast(Strings.AdUnavailable))),
                new Popup.Choice(Strings.DropSkip, Theme.Disabled));
        }

        void ShowConsent()
        {
            Popup.Show(popupRoot, Strings.ConsentTitle, Strings.ConsentBody,
                new Popup.Choice(Strings.ConsentAccept, Theme.Buy, () => PrivacyConsent.SetGdprConsent(true)),
                new Popup.Choice(Strings.ConsentDecline, Theme.Disabled, () => PrivacyConsent.SetGdprConsent(false)),
                new Popup.Choice(Strings.PrivacyPolicy, Theme.PanelLight, () =>
                {
                    Application.OpenURL(PrivacyConsent.PrivacyPolicyUrl);
                    ShowConsent(); // the question still needs an answer
                }));
        }

        void OpenPrivacy()
        {
            string personalized = PrivacyConsent.GdprConsent ? Strings.On : Strings.Off;
            string doNotSell = PrivacyConsent.CcpaOptOut ? Strings.On : Strings.Off;
            Popup.Show(popupRoot, Strings.PrivacyTitle, null,
                new Popup.Choice(string.Format(Strings.PrivacyPersonalized, personalized), Theme.PanelLight, () =>
                {
                    PrivacyConsent.SetGdprConsent(!PrivacyConsent.GdprConsent);
                    OpenPrivacy();
                }),
                new Popup.Choice(string.Format(Strings.PrivacyDoNotSell, doNotSell), Theme.PanelLight, () =>
                {
                    PrivacyConsent.SetCcpaOptOut(!PrivacyConsent.CcpaOptOut);
                    OpenPrivacy();
                }),
                new Popup.Choice(Strings.PrivacyPolicy, Theme.PanelLight, () => Application.OpenURL(PrivacyConsent.PrivacyPolicyUrl)),
                new Popup.Choice(Strings.Close, Theme.Disabled));
        }

        void ShowOffline(OfflineReport report)
        {
            if (offlinePopup != null) offlinePopup.Close();

            string body = string.Format(Strings.OfflineBody, NumberFormat.Duration(report.Seconds), NumberFormat.Format(report.Amount));
            if (report.Capped) body += string.Format(Strings.OfflineCapped, Eco.OfflineCapSeconds / 3600);

            offlinePopup = Popup.Show(popupRoot, Strings.OfflineTitle, body,
                new Popup.Choice(game.SkipsAds ? Strings.OfflineDoubleVip : Strings.OfflineDouble, Theme.Ad, () =>
                    game.RequestReward(AdPlacements.OfflineDouble,
                        () => { game.ClaimOffline(true); Refresh(); },
                        () => { Toast(Strings.AdUnavailable); game.ClaimOffline(false); Refresh(); })),
                new Popup.Choice(Strings.OfflineCollect, Theme.Buy, () => { game.ClaimOffline(false); Refresh(); }));
        }
    }
}
