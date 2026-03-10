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
    [HarmonyPatch(typeof(TINationState), "environmentPrioritySustainabilityChange", MethodType.Getter)]
    public static class EnvironmentSustainabilityEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetEnvironmentPrioritySustainabilityChangeOverwrite(ref float __result, TINationState __instance)
        {
            // Changes environment priority to scale inversly by population, while still being affected by other modifiers.
            // Overall, the scaling should have lesser extremes. Might need tweaking.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            // For whatever reason, sustainability is increased with negative values. But, it's more intuitive to the user if they input a positive number to raise sustainability.
            float baseSustainability = -Main.settings.environmentInvestment.baseSustainability;
            float sustainabilityMultPerSustainabilityLevel = Main.settings.environmentInvestment.sustainabilityMultPerSustainabilityLevel;
            float maxPenaltyFromNukedRegions = Main.settings.environmentInvestment.maxPenaltyFromNukedRegions;
            float penaltyPerNukedRegion = Main.settings.environmentInvestment.penaltyPerNukedRegion;

            // Refer to EffectStrength() comments for explanation.
            float baseEffect = Tools.EffectStrength(baseSustainability, __instance);

            // Each full point of sustainability give a +10% bonus, up to +100%.
            float sustainabilityMult = 1f + (__instance.sustainability * sustainabilityMultPerSustainabilityLevel);

            // Each nuked region causes a Sustainability generation malus of -5%, up to -50%.
            float nukedCounter = __instance.regions.Sum((TIRegionState x) => x.nuclearDetonations);
            float nukedMult = Mathf.Max(maxPenaltyFromNukedRegions, 1f - (nukedCounter * penaltyPerNukedRegion));

            // Base effect is first hit by the 'nuked' malus, then given the 'sustainability' bonus.
            __result = (baseEffect * nukedMult) * sustainabilityMult;



            return false; //Skip the original method
        }
    }
}
