using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Oblitercycle
{
    /// <summary>
    /// Replaces the Obliterator's conversion coroutine. The lever animation, effects, delay and the ownership and
    /// in-use checks are reproduced from the game; only the "what does the load turn into" step differs.
    /// In Vanilla mode the game's own coroutine runs untouched.
    /// </summary>
    [HarmonyPatch]
    internal static class IncineratorPatch
    {
        private static readonly AccessTools.FieldRef<Incinerator, ZNetView> NviewRef =
            AccessTools.FieldRefAccess<Incinerator, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<Incinerator, bool> InUseRef =
            AccessTools.FieldRefAccess<Incinerator, bool>("isInUse");

        private const int ResponseSuccess = 1;      // "$piece_incinerator_success"
        private const int ResponseConversion = 2;   // "$piece_incinerator_conversion"

        private static Settings S => Plugin.Settings;

        [HarmonyPatch(typeof(Incinerator), "Incinerate")]
        [HarmonyPrefix]
        private static bool Incinerate_Prefix(Incinerator __instance, long uid, ref IEnumerator __result)
        {
            if (S.Mode.Value == RecycleMode.Vanilla)
            {
                return true;
            }

            __result = Recycle(__instance, uid);
            return false;
        }

        private static IEnumerator Recycle(Incinerator incinerator, long uid)
        {
            var nview = NviewRef(incinerator);

            InUseRef(incinerator) = true;
            nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLever");
            incinerator.m_leverEffects.Create(incinerator.transform.position, incinerator.transform.rotation);
            yield return new WaitForSeconds(Random.Range(incinerator.m_effectDelayMin, incinerator.m_effectDelayMax));
            nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLeverReturn");

            if (!nview.IsValid() || !nview.IsOwner() || incinerator.m_container.IsInUse())
            {
                InUseRef(incinerator) = false;
                yield break;
            }

            incinerator.Invoke("StopAOE", 4f);
            Object.Instantiate(incinerator.m_lightingAOEs, incinerator.transform.position, incinerator.transform.rotation);

            var inventory = incinerator.m_container.GetInventory();
            int response = ResponseSuccess;

            try
            {
                // The game's own conversions (and any a mod added) take precedence, as in vanilla.
                var converted = new List<ItemDrop>();
                if (S.VanillaConversions.Value && incinerator.m_conversions != null)
                {
                    foreach (var conversion in incinerator.m_conversions)
                    {
                        conversion.AttemptCraft(inventory, converted);
                    }
                }

                var plan = Recycler.Build(inventory, incinerator, S);
                Recycler.Apply(plan, inventory, incinerator);

                foreach (var item in converted)
                {
                    inventory.AddItem(item.gameObject, 1);
                }

                if (plan.Recovered || converted.Count > 0)
                {
                    response = ResponseConversion;
                }

                Plugin.Log.LogDebug($"Obliterator: consumed {plan.Consumed.Count} stacks, returned {plan.Outputs.Count} kinds of item, {plan.Coal} coal, mode {S.Mode.Value}");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Recycling failed; nothing was changed beyond this point: {e}");
            }

            nview.InvokeRPC(uid, "RPC_IncinerateRespons", response);
        }
    }
}
