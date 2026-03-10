using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "spaceFundingPriorityIncomeChange", MethodType.Getter)]
    public static class FundingEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetSpaceFundingPriorityIncomeChangeOverwrite(ref float __result, TINationState __instance)
        {
            //Patches the monthly funding gain from completing a funding investment

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float fundingAmount = Main.settings.fundingInvestment.fundingAmount;

            //Spoils gives a instant funding of around 60 money, at the cost of a fair bit of greenhouse gas and half an investment of welfare in inequality
            //It seems balanced to make the monetary benefit of a funding investment be about equal to that of a spoils investment after a period of 20 game years
            //The funding gained from a funding investment is annual, so an annual income of 15 money is equal to a spoils investment (about 300-400 money) after 20 years

            // Base funding is scaled by the inverse of the simulated IP factor. Basically, converts real IP to simulated IP and multiplies fundingAmount by that.
            // For example, if a country has 60 real IP and 120 simulated IP, and fundingAmount is 15, then a single investment will give 30 money.
            // Conversely, if simulated IP is 30, then the investment will give 7.5 money.
            __result = fundingAmount / Tools.IPFactor(__instance);

            return false; //Skip original getter
        }
    }
}
