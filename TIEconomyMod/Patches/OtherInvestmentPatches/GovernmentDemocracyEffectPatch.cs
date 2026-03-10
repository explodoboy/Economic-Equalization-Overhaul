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
    [HarmonyPatch(typeof(TINationState), "governmentPriorityDemocracyChange", MethodType.Getter)] /* Was 'knowledgePriorityDemocracyChange', but it looks like the method was renamed to 'governmentPriorityDemocracyChange'. Seems to work. */
    public static class KnowledgeDemocracyEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetGovernmentPriorityDemocracyChangeOverwrite(ref float __result, TINationState __instance)
        {
            //Patch changes the democracy effect of a knowledge investment to scale inversely with population size

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseDemocracy = Main.settings.governmentInvestment.baseDemocracy;
            float democracyMultPerEducationLevel = Main.settings.governmentInvestment.democracyMultPerEducationLevel;

            // Refer to EffectStrength() comments for explanation.
            float baseEffect = Tools.EffectStrength(baseDemocracy, __instance);

            // Each full point of Education gives +10% Democracy score.
            float educationMult = 1f + (__instance.education * democracyMultPerEducationLevel);

            __result = baseEffect * educationMult;



            return false; //Skip original getter
        }
    }
}
