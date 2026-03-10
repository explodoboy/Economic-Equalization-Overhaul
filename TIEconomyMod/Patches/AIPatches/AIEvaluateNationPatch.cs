using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIEconomyMod.AIPatches
{
    [HarmonyPatch(typeof(AIEvaluators), "EvaluateNation")]
    public static class AIEvaluateNationPatch
    {
        [HarmonyPrefix]
        public static bool EvaluateNationOverwrite(TIFactionState faction, TINationState nation, ref float __result)
        {
            //Patches AI evaluation of a nation's value/importance to account for the higher IP amount in large modded nations

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Changed from economyScore^3, which was vanilla GDP in billions, to this, which is modded GDP in billions, post-crutch. Vanilla and modded control points should be evaluated similarly given a certain GDP.
            float num = Tools.GetSimulatedMonthlyIP(nation) * 100f;

            //Below as vanilla
            float num2 = 100f * faction.aiValues.wantSpaceFacilities * faction.aiValues.wantSpaceWarCapability;
            float num3 = num + (nation.spaceFlightProgram ? num2 : 0f);
            float num4 = (90f - nation.BestBoostLatitude) / 3f;
            __result = num3 + num4 + AIEvaluators. EvaluateMonthlyResourceIncome(faction, FactionResource.Money, nation.spaceFundingIncome_month) + AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.Boost, nation.boostIncome_month_dekatons * TemplateManager.global.spaceResourceToTons) + AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.Research, nation.research_month) + AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.MissionControl, nation.missionControl) + (nation.nuclearProgram ? (50f * faction.aiValues.wantEarthWarCapability) : 0f) + (float)nation.armies.Count * 25f * faction.aiValues.wantEarthWarCapability + nation.militaryTechLevel * 1.5f * faction.aiValues.wantEarthWarCapability + nation.spaceDefenseCoverage * 1000f;

            return false; //Skip original method
        }
    }
}
