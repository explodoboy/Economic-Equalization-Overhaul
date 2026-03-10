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
    [HarmonyPatch(typeof(TINationState), "economyPriorityInequalityChange", MethodType.Getter)]
    public static class EconomyInequalityEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetEconomyPriorityInequalityChangeOverwrite(ref float __result, TINationState __instance)
        {
            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseInequality = Main.settings.economyInvestmentOther.baseInequality;
            float inequalityMultPerResourceRegion = Main.settings.economyInvestmentOther.inequalityMultPerResourceRegion;

            // Effect is ~13.3 times weaker than welfare.
            // Refer to EffectStrength() comments for explanation.
            float baseInequalityGain = Tools.EffectStrength(baseInequality, __instance);
            float resourceRegionsMult = 1f + ((float)__instance.currentResourceRegions * inequalityMultPerResourceRegion);

            __result = baseInequalityGain * resourceRegionsMult;



            return false; //Skip original getter
        }
    }
}
