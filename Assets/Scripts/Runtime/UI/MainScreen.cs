using System.Collections.Generic;
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
    /// The whole game screen, built from code: currencies, tappable planet, generator list,
    /// boost / prestige / shop buttons, popups and toasts.
    /// </summary>
    public sealed class MainScreen : MonoBehaviour
    {
        static readonly int[] BuyModes = { 1, 10, 0 }; // 0 = max affordable
        const float RefreshInterval = 0.1f;
        const float ToastDuration = 2.2f;

        GameManager game;
        IdleEconomy Eco => game.Economy;

        Transform popupRoot;
        Text creditsText, rateText, crystalsText, darkMatterText, statusText, toastText;
        Text buyModeLabel, boostLabel, prestigeLabel;
        RectTransform planet, floatLayer;
        FloatingTextPool floats;
        readonly List<GeneratorRow> rows = new List<GeneratorRow>();
        Popup offlinePopup;

        int buyModeIndex;
        float refreshTimer, toastTimer, planetScale = 1;

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
            Refresh();
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.OfflineEarningsAvailable -= ShowOffline;
            game.ProductDelivered -= OnProductDelivered;
        }

        void Update()
        {
            planetScale = Mathf.Lerp(planetScale, 1f, Time.unscaledDeltaTime * 12f);
            planet.localScale = Vector3.one * planetScale;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0) Refresh();

            if (toastTimer > 0)
            {
                toastTimer -= Time.unscaledDeltaTime;
                var c = toastText.color;
                c.a = Mathf.Clamp01(toastTimer / 0.4f);
                toastText.color = c;
            }

            if (BackPressed()) Popup.CloseTop();
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

            var background = UIFactory.Image("Background", root, Theme.Background);
            background.rectTransform.Stretch();
            BuildStars(background.transform);

            var safe = UIFactory.Rect("SafeArea", root).Stretch();
            safe.gameObject.AddComponent<SafeArea>();

            BuildTopBar(safe);
            BuildPlanet(safe);
            BuildGeneratorList(safe);
            BuildBottomBar(safe);

            floatLayer = UIFactory.Rect("FloatingTexts", safe).Stretch();
            floats = floatLayer.gameObject.AddComponent<FloatingTextPool>();

            toastText = UIFactory.Text("Toast", root, "", 44, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            toastText.rectTransform.Anchor(new Vector2(0, 0.2f), new Vector2(1, 0.2f), new Vector2(40, -60), new Vector2(-40, 60));
            toastText.gameObject.AddComponent<Outline>().effectColor = Color.black;
            toastText.color = Color.clear;

            popupRoot = UIFactory.Rect("Popups", root).Stretch();
        }

        static void BuildStars(Transform parent)
        {
            var rng = new System.Random(42);
            for (int i = 0; i < 80; i++)
            {
                float size = 3 + (float)rng.NextDouble() * 6;
                var star = UIFactory.Image("Star", parent, new Color(1, 1, 1, 0.2f + (float)rng.NextDouble() * 0.6f), ProceduralSprites.Circle);
                var pos = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                star.rectTransform.Place(pos, Vector2.zero, new Vector2(size, size));
            }
        }

        void BuildTopBar(Transform parent)
        {
            var bar = UIFactory.Rect("TopBar", parent).Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -260), Vector2.zero);

            crystalsText = UIFactory.Text("Crystals", bar, "", 34, Theme.Premium, TextAnchor.UpperLeft, FontStyle.Bold);
            crystalsText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(30, -70), new Vector2(0, -20));

            darkMatterText = UIFactory.Text("DarkMatter", bar, "", 34, Theme.Prestige, TextAnchor.UpperRight, FontStyle.Bold);
            darkMatterText.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -70), new Vector2(-30, -20));

            creditsText = UIFactory.Text("Credits", bar, "0", 92, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            creditsText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -185), new Vector2(0, -70));

            rateText = UIFactory.Text("Rate", bar, "", 40, Theme.TextDim);
            rateText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -250), new Vector2(0, -185));
        }

        void BuildPlanet(Transform parent)
        {
            var img = UIFactory.Image("Planet", parent, Color.white, ProceduralSprites.Planet, raycast: true);
            planet = img.rectTransform.Anchor(new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.84f));
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1;
            img.gameObject.AddComponent<TapZone>().Tapped += OnPlanetTapped;

            statusText = UIFactory.Text("Status", parent, "", 36, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            statusText.rectTransform.Anchor(new Vector2(0, 0.495f), new Vector2(0.7f, 0.55f), new Vector2(30, 0), Vector2.zero);

            var buyMode = UIFactory.Button("BuyMode", parent, "", Theme.PanelLight, CycleBuyMode, out buyModeLabel, 36);
            buyMode.GetComponent<RectTransform>().Anchor(new Vector2(0.72f, 0.5f), new Vector2(1, 0.545f), new Vector2(0, 0), new Vector2(-20, 0));
        }

        void BuildGeneratorList(Transform parent)
        {
            var scrollRt = UIFactory.Rect("Generators", parent).Anchor(Vector2.zero, new Vector2(1, 0.49f), new Vector2(20, 190), new Vector2(-20, 0));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var viewport = UIFactory.Image("Viewport", scrollRt, Color.clear, raycast: true).rectTransform.Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = UIFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            for (int i = 0; i < Eco.GeneratorCount; i++)
                rows.Add(new GeneratorRow(content, i, Buy));
        }

        void BuildBottomBar(Transform parent)
        {
            var bar = UIFactory.Rect("BottomBar", parent).Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(20, 20), new Vector2(-20, 170));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            UIFactory.Button("Boost", bar, "", Theme.Ad, OnBoostClicked, out boostLabel, 36);
            UIFactory.Button("Prestige", bar, "", Theme.Prestige, OnPrestigeClicked, out prestigeLabel, 36);
            UIFactory.Button("Shop", bar, Strings.ShopButton, Theme.Premium, OpenShop, out _, 40);
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

            creditsText.text = NumberFormat.Format(state.credits);
            rateText.text = $"+{NumberFormat.Format(eco.ProductionPerSecond(now))}{Strings.PerSecond}";
            crystalsText.text = $"{Strings.Crystals}: {state.crystals}";
            darkMatterText.text = state.darkMatter > 0 ? $"{Strings.DarkMatter}: {NumberFormat.Format(state.darkMatter)}" : "";

            if (eco.IsBoostActive(now))
                statusText.text = $"{Strings.BoostActive}  {NumberFormat.Duration(eco.BoostRemaining(now))}";
            else
                statusText.text = state.totalEarnings < 100 ? Strings.TapHint : "";

            int mode = BuyModes[buyModeIndex];
            buyModeLabel.text = mode > 0 ? $"x{mode}" : Strings.Max;
            foreach (var row in rows) row.Refresh(eco, mode, now);

            boostLabel.text = eco.IsBoostActive(now)
                ? $"{Strings.BoostButton}\n{NumberFormat.Duration(eco.BoostRemaining(now))}"
                : $"{Strings.BoostButton}\n{(game.SkipsAds ? Strings.BoostFree : Strings.BoostWatch)}";
            prestigeLabel.text = eco.CanPrestige
                ? $"{Strings.PrestigeButton}\n+{NumberFormat.Format(eco.PendingDarkMatter)}"
                : Strings.PrestigeButton;
        }

        void Toast(string message)
        {
            toastText.text = message;
            toastText.color = Theme.Text;
            toastTimer = ToastDuration;
        }

        // ---------- Actions ----------

        void OnPlanetTapped(PointerEventData eventData)
        {
            double value = Eco.Tap(Clock.Now);
            planetScale = 0.92f;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, eventData.position, eventData.pressEventCamera, out var local))
                floats.Spawn("+" + NumberFormat.Format(value), local);
        }

        void CycleBuyMode()
        {
            buyModeIndex = (buyModeIndex + 1) % BuyModes.Length;
            Refresh();
        }

        void Buy(int index)
        {
            int amount = Eco.ResolveBuyAmount(index, BuyModes[buyModeIndex]);
            if (Eco.TryBuy(index, amount)) Refresh();
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
                Eco.AddBoost(Clock.Now, Eco.Balance.boostDurationSeconds);
                game.Save();
                Toast(Strings.BoostAdded);
                Refresh();
            }, () => Toast(Strings.AdUnavailable));
        }

        void OnPrestigeClicked()
        {
            var eco = Eco;
            if (!eco.CanPrestige)
            {
                string progress = $"{NumberFormat.Format(eco.State.runEarnings)} / {NumberFormat.Format(eco.PrestigeRequirement)}";
                Popup.Show(popupRoot, Strings.PrestigeTitle,
                    string.Format(Strings.PrestigeLockedBody, NumberFormat.Format(eco.PrestigeRequirement), progress),
                    new Popup.Choice(Strings.Close, Theme.Disabled));
                return;
            }

            string body = string.Format(Strings.PrestigeBody,
                NumberFormat.Format(eco.PendingDarkMatter),
                eco.Balance.prestigeBonusPerPoint * 100,
                NumberFormat.Format(eco.State.darkMatter),
                $"x{eco.PrestigeMultiplier:0.##}");
            Popup.Show(popupRoot, Strings.PrestigeTitle, body,
                new Popup.Choice(Strings.PrestigeConfirm, Theme.Prestige, () =>
                {
                    double gain = game.DoPrestige();
                    Toast(string.Format(Strings.PrestigeDone, NumberFormat.Format(gain)));
                    Refresh();
                }),
                new Popup.Choice(Strings.Cancel, Theme.Disabled));
        }

        void OpenShop()
        {
            var eco = Eco;
            var balance = eco.Balance;
            var choices = new List<Popup.Choice>();

            if (!eco.State.starterPackOwned)
                choices.Add(ProductChoice(ProductIds.StarterPack,
                    string.Format(Strings.StarterName, balance.starterPackCrystals, balance.starterPackBoostHours), Theme.Ad));
            if (!eco.State.vip)
                choices.Add(ProductChoice(ProductIds.VipPass, Strings.VipName, Theme.Premium));
            choices.Add(ProductChoice(ProductIds.CrystalsSmall, string.Format(Strings.CrystalsSmallName, balance.crystalsSmallAmount), Theme.PanelLight));
            choices.Add(ProductChoice(ProductIds.CrystalsLarge, string.Format(Strings.CrystalsLargeName, balance.crystalsLargeAmount), Theme.PanelLight));
            choices.Add(new Popup.Choice(
                string.Format(Strings.TimeWarp, NumberFormat.Format(eco.TimeWarpValue), balance.timeWarpCrystalCost),
                Theme.Prestige, DoTimeWarp, eco.TimeWarpValue > 0));
            choices.Add(new Popup.Choice(Strings.Restore, Theme.Disabled, game.Store.RestorePurchases));
            choices.Add(new Popup.Choice(Strings.Close, Theme.Disabled));

            Popup.Show(popupRoot, Strings.ShopTitle, $"{Strings.Crystals}: {eco.State.crystals}", choices.ToArray());
        }

        Popup.Choice ProductChoice(string productId, string label, Color color) =>
            new Popup.Choice($"{label}\n{game.Store.GetPrice(productId)}", color, () =>
                game.Store.Purchase(productId, ok =>
                {
                    if (!ok) Toast(Strings.PurchaseFailed);
                }));

        void DoTimeWarp()
        {
            if (Eco.TryTimeWarp(out double gained))
            {
                game.Save();
                Toast("+" + NumberFormat.Format(gained));
                Refresh();
            }
            else Toast(Strings.NotEnoughCrystals);
        }

        void OnProductDelivered(string productId)
        {
            Toast(Strings.PurchaseThanks);
            Refresh();
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
