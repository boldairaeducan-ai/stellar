using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>Pooled "+123" texts that float up and fade out.</summary>
    public sealed class FloatingTextPool : MonoBehaviour
    {
        const float Lifetime = 0.8f;
        const float Rise = 160f;

        struct Item
        {
            public Text Text;
            public Vector2 Start;
            public float Age;
        }

        readonly List<Item> active = new List<Item>();
        readonly Stack<Text> free = new Stack<Text>();

        public void Spawn(string value, Vector2 localPosition)
        {
            var text = free.Count > 0
                ? free.Pop()
                : UIFactory.Text("Float", transform, "", 52, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.sizeDelta = new Vector2(400, 80);
            text.text = value;
            text.gameObject.SetActive(true);
            text.rectTransform.anchoredPosition = localPosition;
            active.Add(new Item { Text = text, Start = localPosition, Age = 0 });
        }

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var item = active[i];
                item.Age += Time.unscaledDeltaTime;
                float t = item.Age / Lifetime;
                if (t >= 1)
                {
                    item.Text.gameObject.SetActive(false);
                    free.Push(item.Text);
                    active.RemoveAt(i);
                    continue;
                }
                item.Text.rectTransform.anchoredPosition = item.Start + new Vector2(0, Rise * t);
                var c = item.Text.color;
                c.a = 1 - t * t;
                item.Text.color = c;
                active[i] = item;
            }
        }
    }
}
