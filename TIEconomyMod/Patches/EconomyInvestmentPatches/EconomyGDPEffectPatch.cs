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
    [HarmonyPatch(typeof(TINationState), "economyPriorityPerCapitaIncomeChange", MethodType.Getter)]
    public static class EconomyGDPEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetEconomyPriorityPerCapitaIncomeChangeOverwrite(ref float __result, TINationState __instance)
        {
            // This patch makes the economy investment increase PCGDP by a percentage. The intention is for countries to grow economically at a more consistent (and realistic-feeling) rate compared to vanilla TI or previous versions of EEO.
            // The intention of this entire formula is for the 2022 USA to grow about 2.5% each year, and for China to grow about 5.5%. This assumes both countries invest about 25% of their IP into economy each year and have zero unrest.
            // While this implementation means very rich countries can remain permanently ahead of poorer countries, I think it's fine. This is a singleplayer game, after all, and if anyone is going to outgrow everyone else it's always going to be the player.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float baseAnnualGrowthAsMult = Main.settings.econonyInvestmentGDP.baseAnnualGrowthPercentage / 100f;
            float baseMonthlyGrowthAsMult = baseAnnualGrowthAsMult / 12;
            float baseGrowthRatePerIP = baseMonthlyGrowthAsMult / Main.settings.investmentPoints.IPPerMonth;

            float growthMultPerSpecialRegion = Main.settings.econonyInvestmentGDP.growthMultPerSpecialRegion;
            float growthMultPerDemocracyLevel = Main.settings.econonyInvestmentGDP.growthMultPerDemocracyLevel;
            float growthMultPerEducationLevel = Main.settings.econonyInvestmentGDP.growthMultPerEducationLevel;
            float doubleGrowthBonusIncrement = Main.settings.econonyInvestmentGDP.doubleGrowthBonusIncrement;
            float baseEffectStrengthPCGDP = Main.settings.investmentPoints.baseEffectStrengthPCGDP;

            float currentPCGDP = __instance.perCapitaGDP;

            // These multipliers are combined together to create a single bonus, instead of making them all separate multipliers. Things got way too complicated way too fast with all those separate multipliers, and I want something more stable here.
            float regionMult = (__instance.currentResourceRegions + __instance.numCoreEconomicRegions_dailyCache) * growthMultPerSpecialRegion;
            float democracyMult = __instance.democracy * growthMultPerDemocracyLevel;
            float educationMult = __instance.education * growthMultPerEducationLevel;
            float combinedMult = 1f + regionMult + democracyMult + educationMult;

            // Doubles GDP growth per 7.5k PCGDP under 30k. The value is clamped to a minimum of 1 so rich countries don't experience diminishing returns. This allows very poor countries to catch up quickly.
            // This also means that countries with high population but poor PCGDP like China and especially India can be used to rapidly scale up boost/mission control/armies. They are really bad at, say, increasing their miltech level, which evens things out IMO. This, obviously, changes when they finally catch up, but that's just how things work in EEO.
            float scalingMult = Mathf.Max(1f, Mathf.Pow(2f, (baseEffectStrengthPCGDP - currentPCGDP) / doubleGrowthBonusIncrement));

            // Multiplies PCGDP by the intended growth percentage per investment, then by the sum of region/democracy/education mults, then by the poor country catch-up mult.
            __result = currentPCGDP * baseGrowthRatePerIP * combinedMult * scalingMult;



            return false; //Skip original getter
        }
    }
}