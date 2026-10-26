using System.Collections.Generic;
using System.Globalization;
using System.Text;
using StellarIdle.Core;
using UnityEngine;

namespace StellarIdle.UI
{
    /// <summary>Shared ship wording and colors (fleet tab, popups, warp chest).</summary>
    public static class ShipText
    {
        public static Color Color(IdleEconomy eco, ShipDef ship) => Theme.Rarity(eco.RarityIndex(ship));

        public static string RarityName(IdleEconomy eco, ShipDef ship) => eco.Rarity(ship)?.name ?? ship.rarity;

        public static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        public static string Role(IdleEconomy eco, ShipDef ship, double bonus)
        {
            string pct = (bonus * 100).ToString("0.#", CultureInfo.InvariantCulture);
            switch (ship.role)
            {
                case ShipRole.Miner:
                    var def = System.Array.Find(eco.Balance.generators, g => g.id == ship.generator);
                    return string.Format(Strings.RoleMiner, pct, def != null ? def.name : ship.generator);
                case ShipRole.Cruiser: return string.Format(Strings.RoleCruiser, pct);
                case ShipRole.Freighter: return string.Format(Strings.RoleFreighter, pct);
                case ShipRole.Scout: return string.Format(Strings.RoleScout, pct);
                case ShipRole.Flagship: return string.Format(Strings.RoleFlagship, pct);
                case ShipRole.Harbinger: return string.Format(Strings.RoleHarbinger, pct);
                default: return ship.role;
            }
        }

        /// <summary>"Courier +10, Interceptor +5" (rich text, colored by rarity).</summary>
        public static string Shards(IdleEconomy eco, IReadOnlyList<ShardGain> gains)
        {
            var totals = new Dictionary<ShipDef, int>();
            var order = new List<ShipDef>();
            foreach (var gain in gains)
            {
                if (!totals.ContainsKey(gain.Ship))
                {
                    totals[gain.Ship] = 0;
                    order.Add(gain.Ship);
                }
                totals[gain.Ship] += gain.Amount;
            }
            var sb = new StringBuilder();
            foreach (var ship in order)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append("<color=#").Append(Hex(Color(eco, ship))).Append('>').Append(ship.name).Append("</color> +").Append(totals[ship]);
            }
            return sb.ToString();
        }
    }
}
