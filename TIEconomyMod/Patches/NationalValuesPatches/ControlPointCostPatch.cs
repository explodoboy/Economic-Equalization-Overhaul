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
    /// <summary>
    /// Patches the getter for control cost of a single control point for a nation
    /// </summary>
    [HarmonyPatch(typeof(TINationState), "ControlPointMaintenanceCost", MethodType.Getter)]
    public static class ControlPointCostPatch
    {
        [HarmonyPostfix]
        public static void GetControlPointMaintenanceCostPostfix(ref float __result, TINationState __instance)
        {
            // If mod has been disabled, abort patch.
            if (!Main.enabled) { return; }

            if (__result != 0) //Will be 0 and should stay 0 if the nation's controller is the aliens
            {
                float baseControlCost = __instance.economyScore; //Total cost to control the entire nation. 1 cost per 1 IP

                int numTechs = 0; //Number of control-cost-reducing techs that have been researched
                if (GameStateManager.GlobalResearch().finishedTechsNames.Contains("ArrivalInternationalRelations")) numTechs++;
                if (GameStateManager.GlobalResearch().finishedTechsNames.Contains("UnityMovements")) numTechs++;
                if (GameStateManager.GlobalResearch().finishedTechsNames.Contains("GreatNations")) numTechs++;
                if (GameStateManager.GlobalResearch().finishedTechsNames.Contains("ArrivalGovernance")) numTechs++;
                if (GameStateManager.GlobalResearch().finishedTechsNames.Contains("Accelerando")) numTechs++;

                // I changed the reduction in control point cost to a flat multiplier. To compensate, I also increased the reduced control point cost. I may modify it further later.
                float mult = 1 - (Main.settings.ControlPointCostReduction * numTechs);

                //__result = (float)Mathd.Pow(baseControlCost, power) / __instance.numControlPoints; //Total cost is split across the control points
                __result = (baseControlCost * mult) / __instance.numControlPoints; //Total cost is split across the control points
            }
        }
    }
}
