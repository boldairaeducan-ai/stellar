using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>Modal dialog: title, body and a column of buttons. Any button closes it.</summary>
    public sealed class Popup : MonoBehaviour
    {
        public readonly struct Choice
        {
            public readonly string Label;
            public readonly Color Color;
            public readonly Action Action;
            public readonly bool Interactable;

            public Choice(string label, Color color, Action action = null, bool interactable = true)
            {
                Label = label;
                Color = color;
                Action = action;
                Interactable = interactable;
            }
        }

        static readonly List<Popup> Open = new List<Popup>();
        public static bool AnyOpen => Open.Count > 0;

        public static Popup Show(Transform root, string title, string body, params Choice[] choices)
        {
            var overlay = UIFactory.Image("Popup", root, Theme.Overlay, raycast: true); // blocks clicks behind
            overlay.rectTransform.Stretch();
            var popup = overlay.gameObject.AddComponent<Popup>();

            var panel = UIFactory.Image("Panel", overlay.transform, Theme.Panel, ProceduralSprites.Rounded, raycast: true);
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 0));
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 48);
            layout.spacing = 24;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIFactory.Text("Title", panel.transform, title, 56, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (!string.IsNullOrEmpty(body))
                UIFactory.Text("Body", panel.transform, body, 40, Theme.Text);

            foreach (var choice in choices)
            {
                var c = choice;
                var button = UIFactory.Button("Choice", panel.transform, c.Label, c.Color, () =>
                {
                    popup.Close();
                    c.Action?.Invoke();
                }, out _, 40);
                button.interactable = c.Interactable;
                button.WithLayout(preferredHeight: 120);
            }

            Open.Add(popup);
            return popup;
        }

        /// <summary>Closes the top-most popup (Android back button). Returns false if none is open.</summary>
        public static bool CloseTop()
        {
            if (Open.Count == 0) return false;
            Open[Open.Count - 1].Close();
            return true;
        }

        public void Close()
        {
            Open.Remove(this);
            Destroy(gameObject);
        }

        void OnDestroy() => Open.Remove(this);
    }
}
