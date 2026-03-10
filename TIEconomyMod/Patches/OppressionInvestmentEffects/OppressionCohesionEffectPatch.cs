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
    [HarmonyPatch(typeof(TINationState), "OppressionPriorityCohesionChange", MethodType.Getter)]
    public static class OppressionCohesionEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetOppressionPriorityCohesionChangeOverwrite(ref float __result, TINationState __instance)
        {

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseCohesion = Main.settings.oppressionInvestment.baseCohesion;
            float minDemocracyForCohesionChange = Main.settings.oppressionInvestment.minDemocracyForCohesionChange;

            // What this does, basically, is that it determines the multiplier applied by each level of democracy, above the minimum, to reach 100% at Democracy 10.
            // minDemocracyForCohesionChange = 0, result = 0.1
            // minDemocracyForCohesionChange = 5, result = 0.2
            // minDemocracyForCohesionChange = 6, result = 0.25
            // If minDemocracyForCohesionChange is 10, then there is no cohesion change whatsoever.

            // Also, to account for floating precision errors (and prevent a potential divide by zero), the 'if' check checks for a settings value very close to 10, rather than exactly.
            float cohesionRampupPerDemocracyLevel = (minDemocracyForCohesionChange <= 9.99f) ? 1 / Mathf.Abs(minDemocracyForCohesionChange - 10f) : 0;

            /* For those newer to modding, the above line basically does the following, but very compactly:
            float cohesionRampupPerDemocracyLevel;
            if (minDemocracyForCohesionChange <= 9.99)
            {
                cohesionRampupPerDemocracyLevel = 1 / Mathf.Abs(minDemocracyForCohesionChange - 10);
            }
            else
            {
                cohesionRampupPerDemocracyLevel = 0;
            } */

            // Refer to EffectStrength() comments for explanation.
            float baseEffect = Tools.EffectStrength(baseCohesion, __instance);

            // Effect ramps up the higher Democracy is. With default settings, it's 0% at/under 5, 100% at 10.
            float democracyMult = Mathf.Max(0f, cohesionRampupPerDemocracyLevel * (__instance.democracy - minDemocracyForCohesionChange));

            __result = baseEffect * democracyMult;



            return false; //Skip original getter
        }
    }
}
