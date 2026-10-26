using UnityEngine;

namespace StellarIdle.UI
{
    public static class Theme
    {
        public static readonly Color Background = new Color32(10, 12, 32, 255);
        public static readonly Color Panel = new Color32(26, 30, 66, 255);
        public static readonly Color PanelLight = new Color32(40, 46, 96, 255);
        public static readonly Color Text = new Color32(235, 240, 255, 255);
        public static readonly Color TextDim = new Color32(150, 160, 210, 255);
        public static readonly Color Accent = new Color32(255, 196, 64, 255);
        public static readonly Color Buy = new Color32(54, 180, 110, 255);
        public static readonly Color Disabled = new Color32(70, 74, 100, 255);
        public static readonly Color Ad = new Color32(120, 90, 230, 255);
        public static readonly Color Premium = new Color32(230, 80, 150, 255);
        public static readonly Color Prestige = new Color32(70, 140, 240, 255);
        public static readonly Color Overlay = new Color(0, 0, 0, 0.7f);
        public static readonly Color PlanetDark = new Color32(40, 70, 160, 255);
        public static readonly Color PlanetLight = new Color32(120, 210, 255, 255);

        public static readonly Color Industry = new Color32(255, 150, 60, 255);
        public static readonly Color Command = new Color32(170, 110, 255, 255);
        public static readonly Color Logistics = new Color32(60, 200, 190, 255);

        public static Color Branch(string branch) =>
            branch == Core.SkillBranch.Command ? Command : branch == Core.SkillBranch.Logistics ? Logistics : Industry;

        static readonly Color[] RarityColors =
        {
            new Color32(170, 175, 190, 255), // common
            new Color32(80, 200, 110, 255),  // uncommon
            new Color32(70, 150, 255, 255),  // rare
            new Color32(185, 95, 255, 255),  // epic
            new Color32(255, 190, 50, 255),  // legendary
        };

        public static Color Rarity(int index) => RarityColors[Mathf.Clamp(index, 0, RarityColors.Length - 1)];

        /// <summary>A distinct, pleasant color per generator index.</summary>
        public static Color GeneratorColor(int index) => Color.HSVToRGB(index * 0.13f % 1f, 0.55f, 0.95f);
    }
}
