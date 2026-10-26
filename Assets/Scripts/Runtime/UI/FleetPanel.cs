using System.Collections.Generic;
using StellarIdle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StellarIdle.UI
{
    /// <summary>
    /// Ships: active fleet slots (bonuses), expeditions (timed missions for idle ships)
    /// and the hangar (collection, build and level up with shards).
    /// </summary>
    public sealed class FleetPanel : TabPanel
    {
        const int MaxShipChoices = 6;

        readonly Text fleetHeader, expeditionsHeader, hangarHeader;
        readonly Button[] slotButtons;
        readonly Text[] slotLabels;
        readonly ListRow[] activeRows;
        readonly ListRow[] destinationRows;
        readonly ListRow[] shipRows;
        readonly ShipDef[] sortedShips;

        public FleetPanel(UIContext ctx, Transform parent) : base(ctx, parent, "Fleet")
        {
            var eco = Eco;

            fleetHeader = AddHeader("", 32, 70);
            var slots = UIFactory.Rect("Slots", Content);
            slots.gameObject.AddComponent<LayoutElement>().preferredHeight = 170;
            var layout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            slotButtons = new Button[eco.MaxFleetSlots];
            slotLabels = new Text[eco.MaxFleetSlots];
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                slotButtons[i] = UIFactory.Button($"Slot{i}", slots, "", Theme.Panel, () => OnSlotClicked(index), out slotLabels[i], 26);
                slotLabels[i].supportRichText = true;
            }

            expeditionsHeader = AddHeader("", 32, 70);
            activeRows = new ListRow[eco.ExpeditionSlots];
            for (int i = 0; i < activeRows.Length; i++)
            {
                int index = i;
                activeRows[i] = new ListRow(Content, $"Expedition{i}", () => OnExpeditionClicked(index), withProgress: true, height: 150);
            }
            destinationRows = new ListRow[eco.Expeditions.Length];
            for (int i = 0; i < destinationRows.Length; i++)
            {
                var expedition = eco.Expeditions[i];
                destinationRows[i] = new ListRow(Content, $"Destination_{expedition.id}", () => ChooseShip(expedition), height: 140);
            }

            hangarHeader = AddHeader("", 32, 70);
            // Rarest first, so the collection reads like a trophy shelf.
            sortedShips = (ShipDef[])eco.Ships.Clone();
            System.Array.Sort(sortedShips, (a, b) => eco.RarityIndex(b).CompareTo(eco.RarityIndex(a)));
            shipRows = new ListRow[sortedShips.Length];
            for (int i = 0; i < shipRows.Length; i++)
            {
                var ship = sortedShips[i];
                shipRows[i] = new ListRow(Content, $"Ship_{ship.id}", () => OnShipClicked(ship), withProgress: true);
            }
        }

        public override int Badge
        {
            get
            {
                var eco = Eco;
                int count = eco.ReadyExpeditionCount(Clock.Now) + eco.UpgradableShipCount;
                if (eco.State.fleet.Count < eco.FleetSlots && HasIdleShip(eco)) count++;
                return count;
            }
        }

        static bool HasIdleShip(IdleEconomy eco)
        {
            foreach (var ship in eco.Ships)
                if (eco.IsShipIdle(ship)) return true;
            return false;
        }

        public override void Refresh(long now)
        {
            var eco = Eco;
            RefreshFleet(eco);
            RefreshExpeditions(eco, now);
            RefreshHangar(eco);
        }

        void RefreshFleet(IdleEconomy eco)
        {
            int next = eco.NextFleetSlotWarps;
            fleetHeader.text = $"<b>{string.Format(Strings.FleetHeader, eco.State.fleet.Count, eco.FleetSlots)}</b>   <size=24>"
                               + (next > 0 ? string.Format(Strings.FleetNextSlot, next) : Strings.FleetAllSlots) + "</size>";

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (i < eco.State.fleet.Count)
                {
                    var ship = eco.FindShip(eco.State.fleet[i]);
                    slotLabels[i].text = $"{ship.name}\n<size=20>{ShipText.Role(eco, ship, eco.ShipBonus(ship))}</size>";
                    slotButtons[i].image.color = ShipText.Color(eco, ship) * new Color(0.55f, 0.55f, 0.55f, 1);
                    slotButtons[i].interactable = true;
                }
                else if (i < eco.FleetSlots)
                {
                    slotLabels[i].text = Strings.SlotEmpty;
                    slotButtons[i].image.color = Theme.Panel;
                    slotButtons[i].interactable = false;
                }
                else
                {
                    slotLabels[i].text = string.Format(Strings.SlotLocked, eco.Balance.fleetSlotWarps[i]);
                    slotButtons[i].image.color = Theme.Panel;
                    slotButtons[i].interactable = false;
                }
            }
        }

        void RefreshExpeditions(IdleEconomy eco, long now)
        {
            var active = eco.ActiveExpeditions;
            expeditionsHeader.text = $"<b>{string.Format(Strings.ExpeditionsHeader, active.Count, eco.ExpeditionSlots)}</b>";

            for (int i = 0; i < activeRows.Length; i++)
            {
                var row = activeRows[i];
                row.Visible = i < active.Count;
                if (!row.Visible) continue;

                var e = active[i];
                var ship = eco.FindShip(e.shipId);
                var expedition = eco.FindExpedition(e.expeditionId);
                bool done = eco.IsExpeditionDone(e, now);
                row.SetIcon(Art.Ship(ship.id), ShipText.Color(eco, ship), Initial(ship.name));
                row.SetTexts(string.Format(Strings.ExpeditionTitle, ship.name, expedition.name),
                    done ? Strings.ExpeditionReady : string.Format(Strings.ExpeditionLeft, NumberFormat.Duration(eco.ExpeditionRemaining(e, now))));
                row.SetProgress((float)eco.ExpeditionProgress(e, now));
                row.SetHighlight(done);
                if (done) row.SetButton(Strings.ExpeditionCollect, true, Theme.Buy);
                else if (eco.CanSpeedUpExpedition(e, now))
                    row.SetButton(Ctx.Game.SkipsAds ? Strings.ExpeditionSpeedUpVip : Strings.ExpeditionSpeedUp, true, Theme.Ad);
                else row.SetButton(NumberFormat.Duration(eco.ExpeditionRemaining(e, now)), false, Theme.Ad);
            }

            bool slotFree = active.Count < eco.ExpeditionSlots;
            for (int i = 0; i < destinationRows.Length; i++)
            {
                var expedition = eco.Expeditions[i];
                var row = destinationRows[i];
                row.Visible = slotFree;
                if (!slotFree) continue;
                string crystals = expedition.crystals > 0 ? string.Format(Strings.DestinationCrystals, expedition.crystals) : "";
                row.SetIcon(null, Theme.Prestige, Initial(expedition.name));
                row.SetTexts($"{expedition.name}  <color=#FFC440>{NumberFormat.Duration(expedition.minutes * 60)}</color>",
                    string.Format(Strings.DestinationDetails, expedition.rolls, crystals));
                row.SetButton(Strings.Send, HasIdleShip(eco), Theme.Prestige);
            }
        }

        void RefreshHangar(IdleEconomy eco)
        {
            int built = 0;
            foreach (var ship in eco.Ships)
                if (eco.IsShipBuilt(ship)) built++;
            hangarHeader.text = $"<b>{string.Format(Strings.HangarHeader, built, eco.Ships.Length)}</b>";

            for (int i = 0; i < shipRows.Length; i++)
            {
                var ship = sortedShips[i];
                var row = shipRows[i];
                var color = ShipText.Color(eco, ship);
                string rarity = ShipText.RarityName(eco, ship);
                int stars = eco.Stars(ship);
                int needed = eco.ShardsForNextStar(ship);
                int shards = eco.Shards(ship);

                if (!eco.IsShipDiscovered(ship))
                {
                    row.SetIcon(null, Theme.Disabled, "?");
                    row.SetTexts(string.Format(Strings.ShipUnknown, rarity), $"<color=#{ShipText.Hex(color)}>{rarity}</color>");
                    row.SetProgress(0);
                    row.SetHighlight(false);
                    row.SetButton("", false, Theme.Disabled, show: false);
                    continue;
                }

                string level = stars > 0 ? string.Format(Strings.ShipLevel, stars, IdleEconomy.MaxStars) : rarity;
                string progress = needed > 0 ? string.Format(Strings.ShipShards, shards, needed) : Strings.ShipShardsMax;
                row.SetIcon(Art.Ship(ship.id), stars > 0 ? color : color * new Color(0.5f, 0.5f, 0.5f, 1), Initial(ship.name));
                row.SetTexts(string.Format(Strings.ShipTitle, ship.name, ShipText.Hex(color), level),
                    $"{ShipText.Role(eco, ship, eco.ShipBonus(ship))}   {progress}");
                row.SetProgress(needed > 0 ? Mathf.Clamp01((float)shards / needed) : 1);

                bool upgradable = eco.CanUpgradeShip(ship);
                row.SetHighlight(upgradable);
                if (upgradable) row.SetButton(stars == 0 ? Strings.ShipBuild : Strings.ShipStarUp, true, Theme.Accent);
                else if (stars == 0) row.SetButton(progress, false, Theme.Disabled);
                else if (eco.IsOnExpedition(ship)) row.SetButton(Strings.ShipOnMission, false, Theme.Disabled);
                else if (eco.IsEquipped(ship)) row.SetButton(Strings.ShipUnequip, true, Theme.PanelLight);
                else row.SetButton(Strings.ShipEquip, true, Theme.Buy);
            }
        }

        // ---------- Actions ----------

        void OnSlotClicked(int index)
        {
            var fleet = Eco.State.fleet;
            if (index >= fleet.Count) return;
            Eco.TryUnequip(Eco.FindShip(fleet[index]));
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }

        void OnShipClicked(ShipDef ship)
        {
            var eco = Eco;
            int index = System.Array.IndexOf(sortedShips, ship);
            if (eco.CanUpgradeShip(ship))
            {
                bool building = eco.Stars(ship) == 0;
                eco.TryUpgradeShip(ship);
                shipRows[index].Punch(0.12f);
                Ctx.Toast(building ? string.Format(Strings.ShipBuilt, ship.name) : string.Format(Strings.ShipLeveled, ship.name, eco.Stars(ship)));
            }
            else if (eco.IsEquipped(ship)) eco.TryUnequip(ship);
            else if (eco.IsShipIdle(ship))
            {
                if (!eco.TryEquip(ship))
                {
                    Ctx.Toast(Strings.FleetFull);
                    return;
                }
                shipRows[index].Punch();
            }
            else return;
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }

        void ChooseShip(ExpeditionDef expedition)
        {
            var eco = Eco;
            var idle = new List<ShipDef>();
            foreach (var ship in eco.Ships)
                if (eco.IsShipIdle(ship)) idle.Add(ship);
            if (idle.Count == 0)
            {
                Ctx.Toast(Strings.NoIdleShip);
                return;
            }
            // Best ships first: they bring back more.
            idle.Sort((a, b) =>
            {
                int rarity = eco.RarityIndex(b).CompareTo(eco.RarityIndex(a));
                return rarity != 0 ? rarity : eco.Stars(b).CompareTo(eco.Stars(a));
            });

            var choices = new List<Popup.Choice>();
            for (int i = 0; i < idle.Count && i < MaxShipChoices; i++)
            {
                var ship = idle[i];
                string label = $"{ship.name}  {string.Format(Strings.ShipLevel, eco.Stars(ship), IdleEconomy.MaxStars)}\n"
                               + $"<size=26>{eco.ExpeditionRolls(expedition, ship)} rolls, +{NumberFormat.Format(eco.ExpeditionCredits(expedition, ship))}</size>";
                choices.Add(new Popup.Choice(label, ShipText.Color(eco, ship) * new Color(0.6f, 0.6f, 0.6f, 1), () => Launch(expedition, ship)));
            }
            choices.Add(new Popup.Choice(Strings.Cancel, Theme.Disabled));
            Popup.Show(Ctx.PopupRoot, Strings.ChooseShip,
                string.Format(Strings.ChooseShipBody, expedition.name, NumberFormat.Duration(expedition.minutes * 60)), choices.ToArray());
        }

        void Launch(ExpeditionDef expedition, ShipDef ship)
        {
            if (!Eco.TryLaunchExpedition(expedition, ship, Clock.Now)) return;
            Ctx.Game.Save();
            Ctx.RequestRefresh();
        }

        void OnExpeditionClicked(int index)
        {
            var eco = Eco;
            if (index >= eco.ActiveExpeditions.Count) return;
            var e = eco.ActiveExpeditions[index];
            long now = Clock.Now;

            if (eco.IsExpeditionDone(e, now))
            {
                var result = eco.CollectExpedition(e, now);
                if (result == null) return;
                Ctx.Game.Save();
                string crystals = result.Crystals > 0 ? string.Format(Strings.DestinationCrystals, result.Crystals) : "";
                string shards = result.Shards.Count > 0 ? string.Format(Strings.ShardsFound, ShipText.Shards(eco, result.Shards)) : "";
                Popup.Show(Ctx.PopupRoot, Strings.ExpeditionDone,
                    string.Format(Strings.ExpeditionLoot, NumberFormat.Format(result.Credits), crystals, shards),
                    new Popup.Choice(Strings.Close, Theme.Buy));
                Ctx.RequestRefresh();
                return;
            }

            if (!eco.CanSpeedUpExpedition(e, now)) return;
            Ctx.Game.RequestReward(AdPlacements.ExpeditionSpeedUp, () =>
            {
                eco.SpeedUpExpedition(e, Clock.Now);
                activeRows[index].Punch();
                Ctx.Game.Save();
                Ctx.RequestRefresh();
            }, () => Ctx.Toast(Strings.AdUnavailable));
        }
    }
}
