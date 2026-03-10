using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TIEconomyMod.AIPatches
{
    [HarmonyPatch(typeof(AIEvaluators), "EvaluateControlPoint")]
    public static class AIEvaluateControlPointPatch
    {
        [HarmonyPrefix]
        public static bool EvaluateControlPointOverwrite(TIFactionState faction, TIControlPoint controlPoint, ref float __result)
        {
            //Patches AI evaluation of a control point's value/importance to account for the higher IP amount in large modded nations

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            //As vanilla
            float num = 0f;
            TINationState nation = controlPoint.nation;

            // This line adjusted from economyScore^3, which was vanilla GDP in billions, to this, which is (post-crutch) GDP in billions. Vanilla and modded control points should be evaluated similarly given a certain GDP.
            num += Tools.GetSimulatedMonthlyIP(nation) * 100f;

            //As vanilla
            num += (nation.spaceFlightProgram ? (100f * faction.aiValues.wantSpaceFacilities * faction.aiValues.wantSpaceWarCapability) : 0f);
            num += AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.Research, nation.GetMonthlyResearchFromControlPoint(faction));
            num += AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.Money, nation.GetMonthlyMoneyIncomeFromControlPoint(faction));
            num += AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.Boost, nation.GetMonthlyBoostIncomeFromControlPoint() / TemplateManager.global.spaceResourceToTons);
            num += AIEvaluators.EvaluateMonthlyResourceIncome(faction, FactionResource.MissionControl, nation.GetMissionControlFromControlPoint(controlPoint.positionInNation));
            num += (nation.nuclearProgram ? (50f * faction.aiValues.wantEarthWarCapability) : 0f);
            num += (float)nation.GetNumArmiesAtControlPoint(controlPoint.positionInNation) * 25f * faction.aiValues.wantEarthWarCapability;
            num += nation.militaryTechLevel * 1.5f * faction.aiValues.wantEarthWarCapability;
            num += nation.spaceDefenseCoverage * 1000f;
            num += (nation.unrest + nation.unrestRestState) / 2f * -8f * faction.aiValues.riskAversion;
            num += (controlPoint.nation.CouncilControlPointFraction(faction, includeDisabled: true, includePermanentAllies: false) + 1f / (float)controlPoint.nation.numControlPoints) * 5f;
            num *= (controlPoint.executive ? 2f : 1f);
            num *= 1f + Mathf.Max(0f, (nation.GetPublicOpinionOfFaction(faction.ideology) - 0.2f) * 1.25f);
            if (faction.lostControlPoints.ContainsKey(controlPoint))
            {
                float num2 = (float)TITimeState.Now().DifferenceInDays(faction.lostControlPoints[controlPoint]) / 30.4368744f;
                num *= Mathf.Clamp(6f - num2, 1f, 4f);
            }
            __result = num;


            return false; //Skip original method
        }
    }
}
