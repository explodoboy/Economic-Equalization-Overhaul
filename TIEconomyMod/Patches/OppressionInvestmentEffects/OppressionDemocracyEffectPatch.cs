using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Pending deletion; Knowledge no longer has a Democracy modifier. */

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "OppressionPriorityDemocracyChange", MethodType.Getter)]
    public static class OppressionDemocracyEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetOppressionPriorityDemocracyChangeOverwrite(ref float __result, TINationState __instance)
        {
            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseDemocracy = Main.settings.oppressionInvestment.baseDemocracy;

            // About 35% of (base) Government effect.
            // Refer to EffectStrength() comments for explanation.
            __result = Tools.EffectStrength(baseDemocracy, __instance);



            return false; //Skip original getter
        }
    }
}
