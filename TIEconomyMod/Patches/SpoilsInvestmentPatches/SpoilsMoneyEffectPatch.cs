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
    [HarmonyPatch(typeof(TINationState), "spoilsPriorityMoney", MethodType.Getter)]
    public static class SpoilsMoneyEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetSpoilsPriorityMoneyOverwrite(ref float __result, TINationState __instance)
        {
            //Patch changes the instant money effect of a spoils investment to be a flat value, not scaled by nation size

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseMoney = Main.settings.spoilsInvestment.baseMoney;
            float moneyMultPerResourceRegion = Main.settings.spoilsInvestment.moneyMultPerResourceRegion;
            float maxMultFromLowDemocracy = Main.settings.spoilsInvestment.maxMultFromLowDemocracy;

            //Add money per resource region.
            float resourceRegionBonusMoney = 1f + (__instance.currentResourceRegions * moneyMultPerResourceRegion);

            //(by default) Up to 30% extra money at 0 democracy, 0% extra at 10 democracy
            float democracyMult = (1f + maxMultFromLowDemocracy) - (__instance.democracy * (maxMultFromLowDemocracy / 10f));

            __result = baseMoney * resourceRegionBonusMoney * democracyMult;


            return false; //Skip original getter
        }
    }
}
