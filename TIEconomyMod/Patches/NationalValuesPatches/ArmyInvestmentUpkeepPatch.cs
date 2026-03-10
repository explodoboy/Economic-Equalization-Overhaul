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
    [HarmonyPatch(typeof(TIArmyState), "investmentArmyFactor", MethodType.Getter)]
    public static class ArmyInvestmentUpkeepPatch
    {
        [HarmonyPrefix]
        public static bool GetInvestmentArmyFactorOverwrite(ref float __result, TIArmyState __instance)
        {
            // Adjusts costs of armies to subtract from the simulated IP of a country.

            // So, this is a little complicated. I think the best way of explaining this is by giving an example.
            /* 
             * simulatedIP = BaseInvestmentPointPatch.GetSimulatedMonthlyIP(__instance.homeNation);
             * realIP = Main.settings.investmentPoints.IPPerMonth;
             * IPScale = realIP / simulatedIP;
             * 
             * Say, simulatedIP is 120 and realIP is 60.
             *
             * 60 / 120 = 0.5
             * Each 0.5 of realIP is worth 1 simulatedIP
             * 
             * Now, say the cost of all armies is 10 simulatedIP.
             * armyCost * mappedIP = 5
             * 
             * Therefore, the IP of a country will be deducted by 5, which is equal to 10 simulated IP.
             */

            // If army is at home, use nationalInvestmentArmyFactorHome. If away from home, use nationalInvestmentArmyFactorAway
            float baseCost = (__instance.useHomeInvestmentFactor) ? TemplateManager.global.nationalInvestmentArmyFactorHome : TemplateManager.global.nationalInvestmentArmyFactorAway;

            // Converts simulated IP to real IP.
            __result = baseCost * Tools.IPFactor(__instance.homeNation);



            return false; //Skip default getter
        }
    }
}
