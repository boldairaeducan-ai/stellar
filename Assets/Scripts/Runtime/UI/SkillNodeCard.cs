using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>One node of the skill tree: branch color stripe, name, level, effect and cost. Tap to buy.</summary>
    public sealed class SkillNodeCard
    {
        public enum Look { TierLocked, Blocked, Available, TooExpensive, Maxed }

        readonly Image background, stripe;
        readonly Button button;
        readonly Text nameText, levelText, effectText, costText;
        readonly PunchScale punch;
        readonly Color branchColor;

        public RectTransform Rect => background.rectTransform;

        public SkillNodeCard(Transform parent, Color branchColor, UnityAction onClick)
        {
            this.branchColor = branchColor;
            background = UIFactory.Image("Node", parent, Theme.Panel, ProceduralSprites.Rounded, raycast: true);
            button = background.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            punch = background.gameObject.AddComponent<PunchScale>();
            punch.enabled = false;

            stripe = UIFactory.Image("Stripe", background.transform, branchColor, ProceduralSprites.Rounded);
            stripe.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -20), new Vector2(-10, -8));

            nameText = UIFactory.Text("Name", background.transform, "", 28, Theme.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            nameText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -96), new Vector2(-8, -26));

            levelText = UIFactory.Text("Level", background.transform, "", 26, branchColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            levelText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -132), new Vector2(-8, -96));

            effectText = UIFactory.Text("Effect", background.transform, "", 23, Theme.TextDim, TextAnchor.UpperCenter);
            effectText.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 52), new Vector2(-10, -136));

            costText = UIFactory.Text("Cost", background.transform, "", 28, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            costText.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(8, 8), new Vector2(-8, 50));
        }

        public void Set(string name, string level, string effect, string cost, Look look)
        {
            if (nameText.text != name) nameText.text = name;
            if (levelText.text != level) levelText.text = level;
            if (effectText.text != effect) effectText.text = effect;
            if (costText.text != cost) costText.text = cost;

            bool dim = look == Look.TierLocked || look == Look.Blocked;
            background.color = look == Look.Available ? Theme.PanelLight : Theme.Panel;
            stripe.color = dim ? Theme.Disabled : branchColor;
            nameText.color = dim ? Theme.TextDim : Theme.Text;
            levelText.color = dim ? Theme.TextDim : branchColor;
            costText.color = look == Look.Available ? Theme.Accent : look == Look.Maxed ? branchColor : Theme.TextDim;
            button.interactable = look == Look.Available;
        }

        public void Punch() => punch.Play(0.1f);
    }
}
