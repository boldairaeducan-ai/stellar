using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>
    /// Reusable list line used by every tab: icon, title, details, optional progress bar and an action button.
    /// </summary>
    public sealed class ListRow
    {
        const float IconSize = 110;
        const float ButtonWidth = 290;

        public readonly GameObject Root;
        readonly Image background;
        readonly Image icon;
        readonly Text iconLetter;
        readonly Text title;
        readonly Text details;
        readonly RectTransform progressFill;
        readonly Button button;
        readonly Text buttonLabel;
        readonly PunchScale punch;

        public ListRow(Transform parent, string name, UnityAction onClick, bool withProgress = false, float height = 170)
        {
            background = UIFactory.Image(name, parent, Theme.Panel, ProceduralSprites.Rounded);
            background.WithLayout(preferredHeight: height);
            Root = background.gameObject;
            punch = Root.AddComponent<PunchScale>();
            punch.enabled = false;

            icon = UIFactory.Image("Icon", background.transform, Color.white, ProceduralSprites.Circle);
            icon.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            icon.rectTransform.anchoredPosition = new Vector2(20 + IconSize / 2, 0);
            icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
            icon.preserveAspect = true;
            iconLetter = UIFactory.Text("Letter", icon.transform, "", 48, Theme.Background, TextAnchor.MiddleCenter, FontStyle.Bold);
            iconLetter.rectTransform.Stretch();

            float left = IconSize + 40;
            float right = ButtonWidth + 30;
            title = UIFactory.Text("Title", background.transform, "", 40, Theme.Text, TextAnchor.LowerLeft, FontStyle.Bold);
            title.supportRichText = true;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.rectTransform.Anchor(new Vector2(0, 0.52f), new Vector2(1, 1), new Vector2(left, 0), new Vector2(-right, -12));

            details = UIFactory.Text("Details", background.transform, "", 30, Theme.TextDim, TextAnchor.UpperLeft);
            details.supportRichText = true;
            details.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.52f), new Vector2(left, withProgress ? 36 : 8), new Vector2(-right, -4));

            if (withProgress)
            {
                var track = UIFactory.Image("Progress", background.transform, Theme.Background, ProceduralSprites.Rounded);
                track.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(left, 16), new Vector2(-right, 32));
                var fill = UIFactory.Image("Fill", track.transform, Theme.Accent, ProceduralSprites.Rounded);
                progressFill = fill.rectTransform.Anchor(Vector2.zero, new Vector2(0, 1));
            }

            button = UIFactory.Button("Action", background.transform, "", Theme.Buy, onClick, out buttonLabel, 32);
            button.GetComponent<RectTransform>().Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-ButtonWidth - 16, 16), new Vector2(-16, -16));
            buttonLabel.supportRichText = true;
        }

        public bool Visible
        {
            get => Root.activeSelf;
            set { if (Root.activeSelf != value) Root.SetActive(value); }
        }

        /// <summary>Custom sprite if provided, otherwise a colored disc with a letter.</summary>
        public void SetIcon(Sprite custom, Color color, string letter)
        {
            if (custom != null)
            {
                icon.sprite = custom;
                icon.color = Color.white;
                iconLetter.text = "";
            }
            else
            {
                icon.sprite = ProceduralSprites.Circle;
                icon.color = color;
                iconLetter.text = letter;
            }
        }

        public void SetTexts(string titleText, string detailsText)
        {
            if (title.text != titleText) title.text = titleText;
            if (details.text != detailsText) details.text = detailsText;
        }

        public void SetProgress(float ratio)
        {
            if (progressFill == null) return;
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
        }

        public void SetButton(string label, bool interactable, Color color, bool show = true)
        {
            if (button.gameObject.activeSelf != show) button.gameObject.SetActive(show);
            if (buttonLabel.text != label) buttonLabel.text = label;
            button.interactable = interactable;
            button.image.color = interactable ? color : Theme.Disabled;
        }

        public void SetHighlight(bool on) => background.color = on ? Theme.PanelLight : Theme.Panel;

        public void Punch(float strength = 0.05f) => punch.Play(strength);
    }
}
