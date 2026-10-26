using System;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>Bottom navigation with a badge per tab ("something to do here").</summary>
    public sealed class TabBar
    {
        static readonly Color BadgeColor = new Color32(235, 70, 70, 255);

        readonly Button[] buttons;
        readonly Text[] labels;
        readonly GameObject[] badges;
        readonly Text[] badgeTexts;
        readonly PunchScale[] punches;
        readonly int[] lastCounts;

        public event Action<int> Selected;

        public TabBar(RectTransform bar, string[] names)
        {
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            int n = names.Length;
            buttons = new Button[n];
            labels = new Text[n];
            badges = new GameObject[n];
            badgeTexts = new Text[n];
            punches = new PunchScale[n];
            lastCounts = new int[n];

            for (int i = 0; i < n; i++)
            {
                int index = i;
                buttons[i] = UIFactory.Button($"Tab_{names[i]}", bar, names[i], Theme.Panel, () => Selected?.Invoke(index), out labels[i], 32);
                punches[i] = buttons[i].gameObject.AddComponent<PunchScale>();

                var badge = UIFactory.Image("Badge", buttons[i].transform, BadgeColor, ProceduralSprites.Circle);
                badge.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1));
                badge.rectTransform.anchoredPosition = new Vector2(-18, -18);
                badge.rectTransform.sizeDelta = new Vector2(46, 46);
                badgeTexts[i] = UIFactory.Text("Count", badge.transform, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                badgeTexts[i].rectTransform.Stretch();
                badges[i] = badge.gameObject;
                badges[i].SetActive(false);
            }
        }

        public void SetSelected(int selected)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i == selected;
                buttons[i].image.color = on ? Theme.PanelLight : Theme.Panel;
                labels[i].color = on ? Theme.Accent : Theme.TextDim;
            }
            punches[selected].Play(0.06f);
        }

        public void SetBadge(int tab, int count)
        {
            bool show = count > 0;
            if (badges[tab].activeSelf != show) badges[tab].SetActive(show);
            if (show) badgeTexts[tab].text = count > 9 ? "9+" : count.ToString();
            if (count > lastCounts[tab]) punches[tab].Play(0.05f); // new thing to do: nudge the tab
            lastCounts[tab] = count;
        }
    }
}
