using System.Collections.Generic;
using System.Globalization;
using StellarIdle.Core;

namespace StellarIdle.UI
{
    /// <summary>Shared wording for galaxy modifiers, relics and effects.</summary>
    public static class GalaxyText
    {
        static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        static string Percent(double value) => (value * 100).ToString("0.#", CultureInfo.InvariantCulture);

        public static string Modifiers(GalaxyDef galaxy)
        {
            var parts = new List<string>();
            if (galaxy.noTap) parts.Add(Strings.ModNoTap);
            if (galaxy.costMultiplier != 1) parts.Add(string.Format(Strings.ModCost, Number(galaxy.costMultiplier)));
            if (galaxy.productionMultiplier != 1) parts.Add(string.Format(Strings.ModProduction, Number(galaxy.productionMultiplier)));
            if (galaxy.maxGenerators > 0) parts.Add(string.Format(Strings.ModMaxGenerators, galaxy.maxGenerators));
            if (galaxy.noResearch) parts.Add(Strings.ModNoResearch);
            if (galaxy.noOffline) parts.Add(Strings.ModNoOffline);
            return parts.Count == 0 ? Strings.GalaxyNoModifier : string.Join(", ", parts);
        }

        public static string Relic(IdleEconomy eco, GalaxyDef galaxy)
        {
            if (string.IsNullOrEmpty(galaxy.relicEffect)) return "";
            int level = eco.RelicLevel(galaxy);
            string line = string.Format(Strings.RelicLine, galaxy.relicName, level, eco.RelicMaxLevel(galaxy),
                Effect(galaxy.relicEffect, galaxy.relicValue, 0) + Strings.PerLevel);
            double next = eco.NextRelicThreshold(galaxy);
            return line + "\n" + (next > 0 ? string.Format(Strings.RelicNext, NumberFormat.Format(next)) : Strings.RelicMaxed);
        }

        /// <summary>Effect wording for one level; <paramref name="level"/> is only used by "next value" effects.</summary>
        public static string Effect(string effect, double v, int level)
        {
            switch (effect)
            {
                case SkillEffect.Production: return string.Format(Strings.FxProduction, Percent(v));
                case SkillEffect.ProductionMult: return string.Format(Strings.FxProductionMult, Number(1 + v));
                case SkillEffect.Tap: return string.Format(Strings.FxTap, Percent(v));
                case SkillEffect.TapMult: return string.Format(Strings.FxTapMult, Percent(v));
                case SkillEffect.AutoTap: return string.Format(Strings.FxAutoTap, Number(v));
                case SkillEffect.Cost: return string.Format(Strings.FxCost, Percent(v));
                case SkillEffect.StartCredits: return string.Format(Strings.FxStartCredits, NumberFormat.Format(v * System.Math.Pow(10, level)));
                case SkillEffect.OfflineHours: return string.Format(Strings.FxOfflineHours, Number(v));
                case SkillEffect.OfflineEfficiency: return string.Format(Strings.FxOfflineEfficiency, Percent(v));
                case SkillEffect.BoostHours: return string.Format(Strings.FxBoostHours, Number(v));
                case SkillEffect.BoostMultiplier: return string.Format(Strings.FxBoostMultiplier, Number(v));
                case SkillEffect.BonusDrop: return string.Format(Strings.FxBonusDrop, Percent(v));
                case SkillEffect.BonusDropRate: return string.Format(Strings.FxBonusDropRate, Percent(v));
                case SkillEffect.Research: return string.Format(Strings.FxResearch, Number(v));
                case SkillEffect.Milestone: return string.Format(Strings.FxMilestone, Number(v));
                case SkillEffect.DarkMatterGain: return string.Format(Strings.FxDarkMatterGain, Percent(v));
                case SkillEffect.PrestigeBonus: return string.Format(Strings.FxPrestigeBonus, Percent(v));
                case SkillEffect.TreeMaxLevel: return string.Format(Strings.FxTreeMaxLevel, Number(v));
                case SkillEffect.GenesisTier: return Strings.FxGenesis;
                case SkillEffect.AutoBuy: return Strings.FxAutoBuy;
                case SkillEffect.StartDarkMatter: return string.Format(Strings.FxStartDarkMatter, NumberFormat.Format(v * System.Math.Pow(10, level)));
                default: return effect;
            }
        }
    }
}
