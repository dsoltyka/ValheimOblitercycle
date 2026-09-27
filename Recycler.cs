using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oblitercycle
{
    /// <summary>
    /// Decides what an Obliterator load turns into. Pure planning: nothing is removed or added until
    /// <see cref="Apply"/> runs on the owning peer.
    /// </summary>
    internal static class Recycler
    {
        internal sealed class Plan
        {
            /// <summary>Items to remove from the Obliterator.</summary>
            public readonly List<ItemDrop.ItemData> Consumed = new List<ItemDrop.ItemData>();

            /// <summary>Prefab name to amount for everything returned to the player.</summary>
            public readonly Dictionary<string, int> Outputs = new Dictionary<string, int>(StringComparer.Ordinal);

            /// <summary>Coal produced from whatever burned.</summary>
            public int Coal;

            /// <summary>True when at least one trophy or material was recovered.</summary>
            public bool Recovered;
        }

        public static Plan Build(Inventory inventory, Incinerator incinerator, Settings settings)
        {
            var plan = new Plan();
            var db = ObjectDB.instance;
            var mode = settings.Mode.Value;
            int defaultCost = Math.Max(1, incinerator.m_defaultCost);
            int yieldPercent = Mathf.Clamp(settings.MaterialYieldPercent.Value, 0, 100);

            // Snapshot: the inventory list is mutated later.
            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            foreach (var item in items)
            {
                var shared = item?.m_shared;
                if (shared == null || item.m_stack <= 0)
                {
                    continue;
                }

                int stack = item.m_stack;
                bool isTrophy = shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
                if (isTrophy && settings.KeepLooseTrophies.Value)
                {
                    continue; // stays in the Obliterator
                }

                var recipe = FindRecipe(db, item);
                if (recipe == null)
                {
                    // A base item: raw material, cooked food, creature drop...
                    if (settings.BaseItems.Value == BaseItemFate.Keep)
                    {
                        continue;
                    }
                    plan.Consumed.Add(item);
                    plan.Coal += stack / defaultCost;
                    continue;
                }

                bool recoveredAny = false;
                int perCraft = Math.Max(1, recipe.m_amount);
                foreach (var requirement in recipe.m_resources)
                {
                    var resource = requirement?.m_resItem;
                    var resourceShared = resource != null ? resource.m_itemData?.m_shared : null;
                    if (resourceShared == null)
                    {
                        continue;
                    }

                    bool resourceIsTrophy = resourceShared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
                    if (mode == RecycleMode.Trophies && !resourceIsTrophy)
                    {
                        continue;
                    }

                    // Crafting cost is per recipe output; upgrade costs are per item and per level.
                    long total = (long)requirement.m_amount * stack / perCraft;
                    if (settings.RecoverUpgrades.Value && item.m_quality > 1)
                    {
                        long perItem = 0;
                        for (int level = 2; level <= item.m_quality; level++)
                        {
                            perItem += requirement.GetAmount(level);
                        }
                        total += perItem * stack;
                    }

                    if (!resourceIsTrophy && mode == RecycleMode.Materials)
                    {
                        total = total * yieldPercent / 100;
                    }

                    if (total <= 0)
                    {
                        continue;
                    }

                    string prefab = resource.gameObject.name;
                    plan.Outputs.TryGetValue(prefab, out int existing);
                    plan.Outputs[prefab] = (int)Math.Min(int.MaxValue, existing + total);
                    recoveredAny = true;
                }

                plan.Consumed.Add(item);
                plan.Recovered |= recoveredAny;

                // In Trophies mode the item itself still burns; in Materials mode it is fully broken down,
                // unless the yield rounded everything away, in which case vanilla coal is the consolation.
                if (mode == RecycleMode.Trophies || !recoveredAny)
                {
                    plan.Coal += stack / defaultCost;
                }
            }

            return plan;
        }

        /// <summary>Applies a plan to the Obliterator's inventory; anything that does not fit is dropped beside it.</summary>
        public static void Apply(Plan plan, Inventory inventory, Incinerator incinerator)
        {
            foreach (var item in plan.Consumed)
            {
                inventory.RemoveItem(item);
            }

            var db = ObjectDB.instance;
            foreach (var pair in plan.Outputs)
            {
                var prefab = db.GetItemPrefab(pair.Key);
                if (prefab == null)
                {
                    Plugin.Log.LogWarning($"Recipe resource '{pair.Key}' is not a known item; skipped.");
                    continue;
                }
                Give(inventory, prefab, pair.Value, incinerator);
            }

            if (plan.Coal > 0 && incinerator.m_defaultResult != null)
            {
                Give(inventory, incinerator.m_defaultResult.gameObject, plan.Coal, incinerator);
            }
        }

        private static void Give(Inventory inventory, GameObject prefab, int amount, Incinerator incinerator)
        {
            var drop = prefab.GetComponent<ItemDrop>();
            int maxStack = Math.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            while (amount > 0)
            {
                int chunk = Math.Min(amount, maxStack);
                if (!inventory.AddItem(prefab, chunk))
                {
                    var data = drop.m_itemData.Clone();
                    data.m_dropPrefab = prefab;
                    data.m_stack = chunk;
                    var position = incinerator.transform.position + Vector3.up * 2f + UnityEngine.Random.insideUnitSphere * 0.4f;
                    ItemDrop.DropItem(data, chunk, position, incinerator.transform.rotation);
                }
                amount -= chunk;
            }
        }

        /// <summary>
        /// The first enabled recipe for the item whose ingredients can be attributed. "Only one ingredient" recipes
        /// (pick any of several inputs) cannot say which input was used, so such items count as base items.
        /// </summary>
        private static Recipe FindRecipe(ObjectDB db, ItemDrop.ItemData item)
        {
            if (db == null)
            {
                return null;
            }

            string name = item.m_shared.m_name;
            foreach (var recipe in db.m_recipes)
            {
                if (recipe == null || !recipe.m_enabled || recipe.m_item == null || recipe.m_requireOnlyOneIngredient)
                {
                    continue;
                }
                if (recipe.m_item.m_itemData.m_shared.m_name != name)
                {
                    continue;
                }
                if (recipe.m_resources == null || recipe.m_resources.Length == 0)
                {
                    continue;
                }
                return recipe;
            }
            return null;
        }
    }
}
