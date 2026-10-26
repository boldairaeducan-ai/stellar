using System;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>What every tab panel needs from the main screen.</summary>
    public sealed class UIContext
    {
        public GameManager Game;
        public Transform PopupRoot;
        public Action<string> Toast;
        public Action RequestRefresh;
        public Action OpenPrivacy;
        public Action Flash;
        public int BuyMode = 1; // 0 = max affordable

        public IdleEconomy Eco => Game.Economy;
    }

    /// <summary>One tab of the bottom navigation: a scrollable list rebuilt from code.</summary>
    public abstract class TabPanel
    {
        protected readonly UIContext Ctx;
        public readonly RectTransform Root;
        protected readonly RectTransform Content;
        readonly ScrollRect scroll;

        protected IdleEconomy Eco => Ctx.Eco;

        protected TabPanel(UIContext ctx, Transform parent, string name)
        {
            Ctx = ctx;
            Root = UIFactory.Rect(name, parent).Stretch();
            Content = BuildScrollList(Root, out scroll);
        }

        public abstract void Refresh(long now);

        /// <summary>Number shown on the tab button (0 hides the badge).</summary>
        public virtual int Badge => 0;

        public bool Visible
        {
            get => Root.gameObject.activeSelf;
            set
            {
                if (Root.gameObject.activeSelf == value) return;
                Root.gameObject.SetActive(value);
                if (value) scroll.verticalNormalizedPosition = 1;
            }
        }

        protected Text AddHeader(string text, int size = 32, float height = 90)
        {
            var header = UIFactory.Text("Header", Content, text, size, Theme.TextDim);
            header.WithLayout(preferredHeight: height);
            header.supportRichText = true;
            return header;
        }

        static RectTransform BuildScrollList(Transform parent, out ScrollRect scroll)
        {
            scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30;

            var viewport = UIFactory.Image("Viewport", parent, Color.clear, raycast: true).rectTransform.Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = UIFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.padding = new RectOffset(0, 0, 8, 24);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        protected static string Initial(string name) => string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1);
    }
}
