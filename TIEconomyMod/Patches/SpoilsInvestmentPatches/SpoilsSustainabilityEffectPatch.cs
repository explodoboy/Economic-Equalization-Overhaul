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
    [HarmonyPatch(typeof(TINationState), "spoilsSustainabilityChange", MethodType.Getter)]
    public static class SpoilsSustainabilityEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetSpoilsSustainabilityChangeOverwrite(ref float __result, TINationState __instance)
        {
            // Changes spoils priority to scale inversly by population.
            // Overall, the scaling should have lesser extremes. Might need tweaking.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            // baseSustainability is inverted, because for whatever reason sustainability increases with negative change, and decreases with positive change.
            float baseSustainability = -Main.settings.spoilsInvestment.baseSustainability;
            float sustainabilityMultPerResourceRegion = Main.settings.spoilsInvestment.sustainabilityMultPerResourceRegion;

            // Effect is 1/2 that of Environment.
            // Refer to EffectStrength() comments for explanation.
            float baseEffect = Tools.EffectStrength(baseSustainability, __instance);

            // Scaling is more aggressive than in Environment. We'll see if this ends up being a good idea.
            float regionMult = 1f + (__instance.currentResourceRegions * sustainabilityMultPerResourceRegion);

            __result = baseEffect * regionMult;



            return false; //Skip the original method
        }
    }
}
