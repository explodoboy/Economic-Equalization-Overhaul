using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "spoilsPriorityDemocracyChange", MethodType.Getter)]
    public static class SpoilsDemocracyEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetSpoilsPriorityDemocracyChangeOverwrite(ref float __result, TINationState __instance)
        {
            //Patch changes the democracy effect of a spoils investment to scale inversely with population size

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseDemocracy = Main.settings.spoilsInvestment.baseDemocracy;

            // Refer to EffectStrength() comments for explanation.
            __result = Tools.EffectStrength(baseDemocracy, __instance);

            return false; //Skip original getter
        }
    }
}
