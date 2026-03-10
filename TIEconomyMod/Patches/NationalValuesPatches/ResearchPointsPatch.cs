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
    [HarmonyPatch(typeof(TINationState), "research_month", MethodType.Getter)]
    public static class ResearchPointsPatch
    {
        [HarmonyPrefix]
        public static bool GetResearchMonthOverwrite(ref float __result, TINationState __instance)
        {
            //Patches the amount of research points available to a nation
            //This removes both the flat research amount every country gets based on its education score, and the ^1.1 scaling on population size

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values cached for readability.
            float baseResearchAtPCGDP = Main.settings.researchProduction.baseResearchAtPCGDP;
            float worstMalus = Main.settings.researchProduction.worstMalus;
            float researchOffset = Main.settings.researchProduction.researchOffset;
            float crutchOffset = Main.settings.researchProduction.crutchOffset;


            //Now, it would be nice to go into a really precise and comprehensive calculation here to figure out how much research per million pop I should give
            //But the reality is that this is way too complicated a relationship between population, GDP, GDP per capita, and the various multipliers here
            //So I'm gonna go with old faithful: Guess and check some values until it's where I want it to be.

            // Was previously 0.0037f. Vanilla is currently 0.0075f. I reduced the base by about 15%.
            // I'm guessing the devs reworked research_month since EEO was first made since the dev says the vanilla multiplier was 0.00225.
            float baseResearch = 0.0064f;
            
            float popMultiplier = __instance.population_Millions; //Removed vanilla better-than-linear scaling.

            float educationEffect = Mathf.Pow(__instance.education, 2); // Removed vanilla hard cap of 12. I'd considered adding a soft cap, but the Knowledge patch has diminishing returns already, so it's probably unnecessary. That may change with playtesting.
            //float educationEffect = (__instance.education * __instance.education) / (1f + __instance.education / 12f); // Changed vanilla hard education cap at 12 to a soft cap.
            float gdpPerCapEffect = Mathf.Max(__instance.perCapitaGDP / baseResearchAtPCGDP, worstMalus); //Floored at 60% research at 12000 gdp per capita, 200% at 40k, and indefinitely upward
            float democracyEffect = Mathf.Pow(Mathf.Max(__instance.democracy, 1f), 1f / 6f); // Vanilla; no bonus at 1 democracy, ~147% at 10.
            float cohesionEffect = (1.25f - Mathf.Abs(__instance.cohesion - 5f) / 10f); //Vanilla, get 25% research boost at 5 cohesion, 25% penalty at 0 or 10 cohesion
            float unrestEffect = (1f - __instance.unrest * __instance.unrest * 0.01f); // Vanilla; quadratic penalty curve. Really small malus at low unrest, and complete loss at 10.
            float crutchMult = (1f * crutchOffset) * Tools.GetCrutchMult(__instance); // Small countries get a bonus equal to their "small country IP bonus".

            float advisorBonus = (1f + __instance.adviserScienceBonus); //Vanilla, up to 25% boost at 25 councilor science score

            __result = baseResearch * popMultiplier * educationEffect * gdpPerCapEffect * democracyEffect * cohesionEffect * unrestEffect * advisorBonus * crutchMult * researchOffset;

            return false; //Skip original getter
        }
    }
}
