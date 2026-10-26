using System;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>
    /// Spawns cargo pods that drift into the scenery from time to time. Tapping one offers credits
    /// for a rewarded ad. Pods only appear when an ad can actually be shown (or for VIP players),
    /// never while a popup is open, and within the hourly cap set in GameBalance.json.
    /// </summary>
    public sealed class BonusDropSpawner : MonoBehaviour
    {
        const float EnterDuration = 0.8f;
        const float ExitDuration = 0.5f;
        const float RetryDelay = 10f;
        const float Size = 150f;

        enum Phase { Waiting, Enter, Idle, Exit }

        GameManager game;
        Action onTapped;
        RectTransform pod, halo, body;
        Text label;
        Graphic[] graphics;
        float[] baseAlphas;
        Phase phase = Phase.Waiting;
        float timer, phaseTime;
        Vector2 enterFrom;
        bool exitByTap;

        public static BonusDropSpawner Create(RectTransform layer, GameManager game, Action onTapped)
        {
            var spawner = layer.gameObject.AddComponent<BonusDropSpawner>();
            spawner.game = game;
            spawner.onTapped = onTapped;
            spawner.timer = (float)game.Economy.Balance.bonusDropFirstDelaySeconds;
            spawner.BuildPod(layer);
            return spawner;
        }

        void BuildPod(RectTransform layer)
        {
            pod = UIFactory.Rect("CargoPod", layer);
            pod.sizeDelta = new Vector2(Size, Size);

            halo = UIFactory.Image("Halo", pod, new Color(1f, 0.77f, 0.25f, 0.35f), ProceduralSprites.Circle).rectTransform;
            halo.sizeDelta = new Vector2(Size * 1.6f, Size * 1.6f);

            var bodyImage = UIFactory.Image("Body", pod, Theme.Accent, ProceduralSprites.Rounded, raycast: true);
            body = bodyImage.rectTransform;
            body.sizeDelta = new Vector2(Size * 0.7f, Size * 0.7f);
            var inner = UIFactory.Image("Inner", body, new Color32(200, 120, 30, 255), ProceduralSprites.Rounded);
            inner.rectTransform.Stretch(14, 14, 14, 14);
            bodyImage.gameObject.AddComponent<TapZone>().Tapped += _ => OnPodTapped();

            label = UIFactory.Text("Label", pod, "?", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.sizeDelta = new Vector2(Size, Size);
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);

            var custom = Art.CargoPod;
            if (custom != null)
            {
                bodyImage.sprite = custom;
                bodyImage.color = Color.white;
                bodyImage.preserveAspect = true;
                bodyImage.type = Image.Type.Simple;
                body.sizeDelta = new Vector2(Size, Size);
                inner.gameObject.SetActive(false);
                label.gameObject.SetActive(false);
            }

            graphics = pod.GetComponentsInChildren<Graphic>(true);
            baseAlphas = Array.ConvertAll(graphics, g => g.color.a);
            pod.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            phaseTime += dt;
            switch (phase)
            {
                case Phase.Waiting:
                    timer -= dt;
                    if (timer <= 0) TrySpawn();
                    break;
                case Phase.Enter:
                    AnimateEnter();
                    break;
                case Phase.Idle:
                    AnimateIdle();
                    if (phaseTime >= game.Economy.Balance.bonusDropLifetimeSeconds) StartExit(byTap: false);
                    break;
                case Phase.Exit:
                    AnimateExit();
                    break;
            }
        }

        bool CanSpawn()
        {
            if (Popup.AnyOpen || !PrivacyConsent.HasGdprAnswer) return false;
            if (!game.Economy.CanSpawnBonusDrop(Clock.Now)) return false;
            return game.SkipsAds || game.Ads.IsRewardedReady;
        }

        void TrySpawn()
        {
            if (!CanSpawn())
            {
                timer = RetryDelay;
                return;
            }

            // Drift in from the closest screen edge to a free spot on either side of the planet.
            bool left = UnityEngine.Random.value < 0.5f;
            float x = left ? UnityEngine.Random.Range(0.12f, 0.26f) : UnityEngine.Random.Range(0.74f, 0.88f);
            float y = UnityEngine.Random.Range(0.6f, 0.8f);
            pod.anchorMin = pod.anchorMax = new Vector2(x, y);
            enterFrom = new Vector2(left ? -700 : 700, UnityEngine.Random.Range(-150f, 150f));

            label.text = "?";
            pod.localScale = Vector3.one;
            SetAlpha(1);
            pod.gameObject.SetActive(true);
            SetPhase(Phase.Enter);
        }

        void AnimateEnter()
        {
            float t = Mathf.Clamp01(phaseTime / EnterDuration);
            float eased = EaseOutBack(t);
            pod.anchoredPosition = Vector2.LerpUnclamped(enterFrom, Vector2.zero, eased);
            body.localRotation = Quaternion.Euler(0, 0, 45 + (1 - t) * 540);
            pod.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, t);
            if (t >= 1) SetPhase(Phase.Idle);
        }

        void AnimateIdle()
        {
            float t = phaseTime;
            pod.anchoredPosition = new Vector2(Mathf.Sin(t * 0.9f) * 10f, Mathf.Sin(t * 2f) * 14f);
            body.localRotation = Quaternion.Euler(0, 0, 45 + Mathf.Sin(t * 1.6f) * 10f);
            halo.localScale = Vector3.one * (1f + Mathf.Sin(t * 4f) * 0.15f);

            // Blink during the last seconds to signal it is about to leave.
            float remaining = (float)game.Economy.Balance.bonusDropLifetimeSeconds - t;
            SetAlpha(remaining < 4f && Mathf.Repeat(t * 4f, 1f) < 0.35f ? 0.4f : 1f);
        }

        void AnimateExit()
        {
            float t = Mathf.Clamp01(phaseTime / ExitDuration);
            if (exitByTap)
            {
                pod.localScale = Vector3.one * (1f + t * 0.6f); // pop open
            }
            else
            {
                pod.anchoredPosition += new Vector2(0, 900f * Time.unscaledDeltaTime); // drift away
                pod.localScale = Vector3.one * (1f - t * 0.5f);
            }
            SetAlpha(1 - t);
            if (t >= 1) Despawn();
        }

        void OnPodTapped()
        {
            if (phase != Phase.Idle && phase != Phase.Enter) return;
            StartExit(byTap: true);
            onTapped?.Invoke();
        }

        void StartExit(bool byTap)
        {
            exitByTap = byTap;
            SetPhase(Phase.Exit);
        }

        void Despawn()
        {
            pod.gameObject.SetActive(false);
            var b = game.Economy.Balance;
            timer = UnityEngine.Random.Range((float)b.bonusDropMinIntervalSeconds, (float)b.bonusDropMaxIntervalSeconds);
            SetPhase(Phase.Waiting);
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseTime = 0;
        }

        void SetAlpha(float a)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                var c = graphics[i].color;
                c.a = baseAlphas[i] * a;
                graphics[i].color = c;
            }
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
