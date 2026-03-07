using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/* Pending deletion; Knowledge no longer has a Democracy modifier. */

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "OppressionPriorityUnrestChange", MethodType.Getter)]
    public static class OppressionUnrestEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetOppressionPriorityUnrestChangeOverwrite(ref float __result, TINationState __instance)
        {
            // Changes Oppression so its effect scales inversely with population.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseUnrest = Main.settings.oppressionInvestment.baseUnrest;
            float unrestPenaltyMultPerDemocracyLevel = Main.settings.oppressionInvestment.unrestPenaltyMultPerDemocracyLevel;

            // VERY powerful for totalitarian nations...especially if they're rich. In practice, no country will get this close unless controlled by the player.
            // Refer to EffectStrength() comments for explanation.
            float baseEffect = Tools.EffectStrength(baseUnrest, __instance.population);

            // Effect is reduced by Democracy, -10% per full point. At full Democracy, Oppression does nothing to Unrest.
            float adjustedEffect = baseEffect * (1f - (__instance.democracy * unrestPenaltyMultPerDemocracyLevel));

            // For whatever reason, vanilla code explicitly disallows a value that'd go under 0. I'm playing it safe by doing that too.
            __result = Mathf.Min(__instance.unrest, adjustedEffect);



            return false; //Skip original getter
        }
    }
}
