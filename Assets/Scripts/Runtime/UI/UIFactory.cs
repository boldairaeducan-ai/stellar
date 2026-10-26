using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>Small helpers to build uGUI hierarchies from code.</summary>
    public static class UIFactory
    {
        static Font font;
        public static Font Font => font ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Text Text(string name, Transform parent, string value, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Color color, UnityAction onClick,
            out Text labelText, int fontSize = 40)
        {
            var img = Image(name, parent, color, ProceduralSprites.Rounded, raycast: true);
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            labelText = Text("Label", img.transform, label, fontSize, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            labelText.rectTransform.Stretch(12, 6, 12, 6);
            return button;
        }

        public static T WithLayout<T>(this T component, float preferredHeight = -1, float preferredWidth = -1, float flexibleWidth = -1)
            where T : Component
        {
            // No "??" here: Unity objects override == null, the null-coalescing operator does not.
            var le = component.GetComponent<LayoutElement>();
            if (le == null) le = component.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            return component;
        }
    }
}
