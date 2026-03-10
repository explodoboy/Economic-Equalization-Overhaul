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
    [HarmonyPatch(typeof(TIArmyState), "investmentNavyFactor", MethodType.Getter)]
    public static class NavyInvestmentUpkeepPatch
    {
        [HarmonyPrefix]
        public static bool GetInvestmentNavyFactorOverwrite(ref float __result, TIArmyState __instance)
        {
            // Adjusts costs of navies to subtract from the simulated IP of a country.
            // For an explanation, refer to ArmyInvestmentUpkeepPatch.cs

            // Converts simulated IP to real IP.
            __result = TemplateManager.global.nationalInvestmentNavyFactor * Tools.IPFactor(__instance.homeNation);



            return false; //Skip default getter
        }
    }
}
