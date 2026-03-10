using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TIEconomyMod.Patches.OtherInvestmentPatches
{
    [HarmonyPatch(typeof(TINationState), "GetRequiredInvestmentPointsForPriority")]
    internal class RequiredInvestmentPointForPriorityPatch
    {
        [HarmonyPrefix]
        public static bool GetRequiredInvestmentPointsForPriority(ref float __result, TINationState __instance, PriorityType priority)
        {
            // This patch intercepts GetRequiredInvestmentPointsForPriority(), giving it alternate IP requirements for investment types which need to take into account simulated IP.
            switch (priority)
            {
                case PriorityType.MissionControl:
                case PriorityType.Civilian_InitiateSpaceflightProgram:
                case PriorityType.Military_FoundMilitary:
                case PriorityType.Military_BuildArmy:
                case PriorityType.Military_BuildNavy:
                case PriorityType.Military_InitiateNuclearProgram:
                case PriorityType.Military_BuildNuclearWeapons:
                case PriorityType.Military_BuildSTOSquadron:
                    __result = TemplateManager.global.GetRequiredInvestmentPoints(priority) * Tools.IPFactor(__instance);
                    break;
                case PriorityType.Military_BuildSpaceDefenses:
                    __result = (TemplateManager.global.GetRequiredInvestmentPoints(priority) * Tools.IPFactor(__instance)) * Mathf.Clamp((__instance.GetNextSpaceDefensesRegion()?.area_km2 ?? TIGlobalValuesState.GlobalValues.medianRegionArea_km2) / TIGlobalValuesState.GlobalValues.medianRegionArea_km2, 0.5f, 2f);
                    break;
                default:
                    return true; // Proceed to original method.
            }
            __result = Mathf.Max(0.1f, __result); // Clamp to a minimum value to avoid possible infinities.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            return false; // Skip original method.
        }
    }
}
