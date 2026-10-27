using System;
using System.Collections.Generic;

namespace StellarIdle.Core
{
    public readonly struct ShardGain
    {
        public readonly ShipDef Ship;
        public readonly int Amount;

        public ShardGain(ShipDef ship, int amount)
        {
            Ship = ship;
            Amount = amount;
        }
    }

    public sealed class ExpeditionResult
    {
        public double Credits;
        public long Crystals;
        public IReadOnlyList<ShardGain> Shards = Array.Empty<ShardGain>();
    }

    /// <summary>
    /// Ship collection: shards (free random sources only), build and star upgrades,
    /// active fleet bonuses, and timed expeditions.
    /// </summary>
    public sealed partial class IdleEconomy
    {
        public const int MaxStars = 5;
        static readonly IReadOnlyList<ShardGain> NoShards = Array.Empty<ShardGain>();

        /// <summary>Random source for shard rolls (replace with a seeded one in tests).</summary>
        public Random Rng { get; set; } = new Random();

        /// <summary>Shards found in the last cargo pod (empty most of the time).</summary>
        public IReadOnlyList<ShardGain> LastBonusDropShards { get; private set; } = NoShards;

        /// <summary>Shards from the chest opened by the last warp.</summary>
        public IReadOnlyList<ShardGain> LastWarpChest { get; private set; } = NoShards;

        double[] fleetGeneratorBonus = Array.Empty<double>();
        double fleetGlobalBonus, fleetTapBonus, fleetDropBonus, fleetOfflineBonus, fleetDarkMatterBonus;

        public ShipDef[] Ships => Balance.ships;
        public ExpeditionDef[] Expeditions => Balance.expeditions;

        void InitFleet()
        {
            // Forget saved ids that no longer exist in the balance file.
            State.fleet.RemoveAll(id => FindShip(id) == null);
            State.expeditions.RemoveAll(e => FindShip(e.shipId) == null || FindExpedition(e.expeditionId) == null);

            if (!State.starterShipGiven)
            {
                State.starterShipGiven = true;
                var starter = FindShip(Balance.starterShip);
                if (starter != null)
                {
                    var save = GetShipSave(starter, create: true);
                    if (save.stars == 0) save.stars = 1;
                    if (!State.fleet.Contains(starter.id)) State.fleet.Add(starter.id);
                }
            }
            RecomputeFleet();
        }

        public ShipDef FindShip(string id) => Array.Find(Ships, s => s.id == id);
        public ExpeditionDef FindExpedition(string id) => Array.Find(Expeditions, e => e.id == id);

        public ShipRarityDef Rarity(ShipDef ship) => Array.Find(Balance.shipRarities, r => r.id == ship.rarity);
        public int RarityIndex(ShipDef ship) => Array.FindIndex(Balance.shipRarities, r => r.id == ship.rarity);

        ShipSave GetShipSave(ShipDef ship, bool create = false)
        {
            foreach (var s in State.ships)
                if (s.id == ship.id) return s;
            if (!create) return null;
            var save = new ShipSave { id = ship.id };
            State.ships.Add(save);
            return save;
        }

        public int Stars(ShipDef ship) => GetShipSave(ship)?.stars ?? 0;
        public int Shards(ShipDef ship) => GetShipSave(ship)?.shards ?? 0;
        public bool IsShipBuilt(ShipDef ship) => Stars(ship) > 0;

        /// <summary>True once the player has seen at least one shard of this ship.</summary>
        public bool IsShipDiscovered(ShipDef ship) => GetShipSave(ship) != null;

        /// <summary>Shards needed for the next step (build or next star), or -1 at max stars.</summary>
        public int ShardsForNextStar(ShipDef ship)
        {
            int stars = Stars(ship);
            var rarity = Rarity(ship);
            if (stars == 0) return rarity.unlockShards;
            if (stars >= MaxStars || stars - 1 >= rarity.starCosts.Length) return -1;
            return rarity.starCosts[stars - 1];
        }

        public bool CanUpgradeShip(ShipDef ship)
        {
            int needed = ShardsForNextStar(ship);
            return needed > 0 && Shards(ship) >= needed;
        }

        /// <summary>Builds the ship (0 to 1 star) or adds a star.</summary>
        public bool TryUpgradeShip(ShipDef ship)
        {
            if (!CanUpgradeShip(ship)) return false;
            var save = GetShipSave(ship);
            save.shards -= ShardsForNextStar(ship);
            save.stars++;
            RecomputeFleet();
            return true;
        }

        public int UpgradableShipCount
        {
            get
            {
                int n = 0;
                foreach (var ship in Ships)
                    if (CanUpgradeShip(ship)) n++;
                return n;
            }
        }

        /// <summary>Bonus of a ship at its current stars (or at 1 star if not built, for previews).</summary>
        public double ShipBonus(ShipDef ship) => ShipBonusAt(ship, Math.Max(1, Stars(ship)));

        public double ShipBonusAt(ShipDef ship, int stars) => ship.value * (1 + Balance.shipStarBonus * (stars - 1));

        // ---------- Shards ----------

        /// <summary>Rolls shards: rarity by weight, then a random ship of that rarity.</summary>
        public IReadOnlyList<ShardGain> RollShards(int rolls)
        {
            if (rolls <= 0 || Ships.Length == 0) return NoShards;
            var gains = new List<ShardGain>(rolls);
            double totalWeight = 0;
            foreach (var r in Balance.shipRarities) totalWeight += r.weight;

            for (int i = 0; i < rolls; i++)
            {
                double pick = Rng.NextDouble() * totalWeight;
                ShipRarityDef rarity = Balance.shipRarities[Balance.shipRarities.Length - 1];
                foreach (var r in Balance.shipRarities)
                {
                    if (pick < r.weight) { rarity = r; break; }
                    pick -= r.weight;
                }

                var candidates = Array.FindAll(Ships, s => s.rarity == rarity.id);
                if (candidates.Length == 0) candidates = Ships;
                var ship = candidates[Rng.Next(candidates.Length)];
                GetShipSave(ship, create: true).shards += Balance.shardsPerRoll;
                gains.Add(new ShardGain(ship, Balance.shardsPerRoll));
            }
            return gains;
        }

        // ---------- Daily distress signal (rewarded ad) ----------

        /// <summary>Available once per local day. <paramref name="day"/> is a day number from the device's local date;
        /// a clock set back never re-opens a signal already answered.</summary>
        public bool CanAnswerDistressSignal(int day) => day > State.distressSignalDay;

        /// <summary>Streak the player would reach by answering today (resets after a missed day).</summary>
        public int DistressStreakIfAnswered(int day) =>
            State.distressSignalDay == day - 1 ? State.distressStreak + 1 : 1;

        public int DistressSignalRolls(int day)
        {
            int streak = CanAnswerDistressSignal(day) ? DistressStreakIfAnswered(day) : State.distressStreak;
            int rolls = Balance.distressSignalRolls;
            foreach (int days in Balance.distressStreakDays)
                if (streak >= days) rolls++;
            return rolls;
        }

        public IReadOnlyList<ShardGain> AnswerDistressSignal(int day)
        {
            if (!CanAnswerDistressSignal(day)) return NoShards;
            int rolls = DistressSignalRolls(day);
            State.distressStreak = DistressStreakIfAnswered(day);
            State.distressSignalDay = day;
            return RollShards(rolls);
        }

        // ---------- Active fleet ----------

        public int FleetSlots
        {
            get
            {
                int slots = 0;
                foreach (int warps in Balance.fleetSlotWarps)
                    if (State.prestigeCount >= warps) slots++;
                return slots;
            }
        }

        public int MaxFleetSlots => Balance.fleetSlotWarps.Length;

        /// <summary>Warps needed for the next fleet slot, or -1 when all are unlocked.</summary>
        public int NextFleetSlotWarps
        {
            get
            {
                foreach (int warps in Balance.fleetSlotWarps)
                    if (State.prestigeCount < warps) return warps;
                return -1;
            }
        }

        public bool IsEquipped(ShipDef ship) => State.fleet.Contains(ship.id);

        public bool IsOnExpedition(ShipDef ship) => State.expeditions.Exists(e => e.shipId == ship.id);

        public bool IsShipIdle(ShipDef ship) => IsShipBuilt(ship) && !IsEquipped(ship) && !IsOnExpedition(ship);

        public bool CanEquip(ShipDef ship) => IsShipIdle(ship) && State.fleet.Count < FleetSlots;

        public bool TryEquip(ShipDef ship)
        {
            if (!CanEquip(ship)) return false;
            State.fleet.Add(ship.id);
            RecomputeFleet();
            return true;
        }

        public bool TryUnequip(ShipDef ship)
        {
            if (!State.fleet.Remove(ship.id)) return false;
            RecomputeFleet();
            return true;
        }

        void RecomputeFleet()
        {
            fleetGeneratorBonus = new double[GeneratorCount];
            fleetGlobalBonus = fleetTapBonus = fleetDropBonus = fleetOfflineBonus = fleetDarkMatterBonus = 0;

            // Only as many ships as there are slots count (a lost slot never happens, but stay safe).
            int counted = 0;
            foreach (var id in State.fleet)
            {
                if (counted++ >= FleetSlots) break;
                var ship = FindShip(id);
                if (ship == null || !IsShipBuilt(ship)) continue;
                double bonus = ShipBonus(ship);
                switch (ship.role)
                {
                    case ShipRole.Miner:
                        int g = Array.FindIndex(Balance.generators, d => d.id == ship.generator);
                        if (g >= 0) fleetGeneratorBonus[g] += bonus;
                        break;
                    case ShipRole.Cruiser: fleetTapBonus += bonus; break;
                    case ShipRole.Freighter: fleetDropBonus += bonus; break;
                    case ShipRole.Scout: fleetOfflineBonus += bonus; break;
                    case ShipRole.Flagship: fleetGlobalBonus += bonus; break;
                    case ShipRole.Harbinger: fleetDarkMatterBonus += bonus; break;
                }
            }
        }

        public double FleetGeneratorMultiplier(int generator) =>
            generator < fleetGeneratorBonus.Length ? 1 + fleetGeneratorBonus[generator] : 1;

        public double FleetGlobalMultiplier => 1 + fleetGlobalBonus;
        public double FleetTapMultiplier => 1 + fleetTapBonus;
        public double FleetBonusDropMultiplier => 1 + fleetDropBonus;
        public double FleetOfflineMultiplier => 1 + fleetOfflineBonus;
        public double FleetDarkMatterMultiplier => 1 + fleetDarkMatterBonus;

        // ---------- Expeditions ----------

        public int ExpeditionSlots => Balance.expeditionSlots;
        public IReadOnlyList<ExpeditionSave> ActiveExpeditions => State.expeditions;

        public bool CanLaunchExpedition(ExpeditionDef expedition, ShipDef ship) =>
            expedition != null && ship != null && IsShipIdle(ship) && State.expeditions.Count < ExpeditionSlots;

        public bool TryLaunchExpedition(ExpeditionDef expedition, ShipDef ship, long now)
        {
            if (!CanLaunchExpedition(expedition, ship)) return false;
            State.expeditions.Add(new ExpeditionSave
            {
                expeditionId = expedition.id,
                shipId = ship.id,
                startUnix = now,
                endUnix = now + (long)(expedition.minutes * 60),
            });
            return true;
        }

        public double ExpeditionRemaining(ExpeditionSave e, long now) => Math.Max(0, e.endUnix - now);
        public bool IsExpeditionDone(ExpeditionSave e, long now) => now >= e.endUnix;

        public double ExpeditionProgress(ExpeditionSave e, long now)
        {
            double total = Math.Max(1, e.endUnix - e.startUnix);
            return Math.Min(1, Math.Max(0, (now - e.startUnix) / total));
        }

        public bool CanSpeedUpExpedition(ExpeditionSave e, long now) => !e.adUsed && !IsExpeditionDone(e, now);

        /// <summary>Rewarded-ad speed-up, once per expedition.</summary>
        public bool SpeedUpExpedition(ExpeditionSave e, long now)
        {
            if (!CanSpeedUpExpedition(e, now)) return false;
            e.endUnix = now + (long)(ExpeditionRemaining(e, now) * (1 - Balance.expeditionAdSpeedup));
            e.adUsed = true;
            return true;
        }

        public int ReadyExpeditionCount(long now)
        {
            int n = 0;
            foreach (var e in State.expeditions)
                if (IsExpeditionDone(e, now)) n++;
            return n;
        }

        /// <summary>Rolls of an expedition for this ship: rarer ships bring back more.</summary>
        public int ExpeditionRolls(ExpeditionDef expedition, ShipDef ship) => expedition.rolls + Math.Max(0, RarityIndex(ship)) / 2;

        public double ExpeditionCredits(ExpeditionDef expedition, ShipDef ship) =>
            RawProduction() * PermanentMultiplier * expedition.productionMinutes * 60 * Math.Max(1, Stars(ship));

        public ExpeditionResult CollectExpedition(ExpeditionSave e, long now)
        {
            if (!IsExpeditionDone(e, now) || !State.expeditions.Remove(e)) return null;
            var expedition = FindExpedition(e.expeditionId);
            var ship = FindShip(e.shipId);
            var result = new ExpeditionResult
            {
                Credits = ExpeditionCredits(expedition, ship),
                Crystals = expedition.crystals,
                Shards = RollShards(ExpeditionRolls(expedition, ship)),
            };
            Earn(result.Credits);
            State.crystals += result.Crystals;
            return result;
        }
    }
}
