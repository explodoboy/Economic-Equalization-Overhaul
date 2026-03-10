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
    [HarmonyPatch(typeof(NationInfoController), "BuildInvestmentTooltip")]
    public static class IPTooltipPatch
    {
        [HarmonyPrefix]
        public static bool BuildInvestmentTooltipOverwrite(ref string __result, TINationState nation)
        {
            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            /* Improved breakdown of IP bonuses and penalties */
            StringBuilder stringBuilder = new StringBuilder(Loc.T("UI.Nation.InvestmentPoints")).AppendLine().AppendLine();
            bool penaltyMilitary = false;
            bool penaltyUnrest = false;

            float economyScore = nation.economyScore;
            float finalEconomyScore = nation.BaseInvestmentPoints_month();
            stringBuilder.Append(Loc.T("UI.Nation.BaseRIPs", economyScore.ToString("N2")));
            if (finalEconomyScore != economyScore)
            {
                stringBuilder.Append(Loc.T("UI.Nation.CurrentRIPs", finalEconomyScore.ToString("N2")));
            }
            stringBuilder.AppendLine().AppendLine();

            float adviserAdministrationBonus = nation.adviserAdministrationBonus;
            if (adviserAdministrationBonus > 0f)
            {
                stringBuilder.Append(Loc.T("UI.Nation.AdviserBonus", adviserAdministrationBonus.ToPercent("P0"), TIUtilities.FormatSmallNumber(economyScore * adviserAdministrationBonus)));
                stringBuilder.AppendLine().AppendLine();
            }

            float investmentPoints_unrestPenalty_frac = nation.investmentPoints_unrestPenalty_frac;
            float unrestPenalty = economyScore * investmentPoints_unrestPenalty_frac;
            if (investmentPoints_unrestPenalty_frac > 0f)
            {
                penaltyUnrest = true;
                stringBuilder.Append(Loc.T("UI.Nation.IPUnrestPenalty", investmentPoints_unrestPenalty_frac.ToPercent("P0"), unrestPenalty.ToString("N1")));
                stringBuilder.AppendLine().AppendLine();
            }

            float true_sIP = Tools.GetSimulatedMonthlyIP(nation);
            float raw_sIP = Tools.GetSimulatedMonthlyIP_Raw(nation);
            float valueOf_rIP = 1 / Tools.IPFactor(nation);
            if (raw_sIP == true_sIP)
            {
                stringBuilder.Append(Loc.T("UI.Nation.BaseSIPs", true_sIP.ToString("N2")));
            }
            else
            {
                stringBuilder.Append(Loc.T("UI.Nation.CrutchSIPs", true_sIP.ToString("N2")));
            }
            stringBuilder.Append(Loc.T("UI.Nation.ConvertSIPs", valueOf_rIP.ToString("N2")));
            stringBuilder.AppendLine().AppendLine();

            int armiesAtHome = nation.armies.Count((TIArmyState x) => x.investmentArmyFactor > 0f && x.useHomeInvestmentFactor);
            int deployedArmies = nation.armies.Count((TIArmyState x) => x.investmentArmyFactor > 0f && !x.useHomeInvestmentFactor);
            int numNavies = nation.armies.Count((TIArmyState x) => x.deploymentType == DeploymentType.Naval && x.investmentNavyFactor > 0f);
            float upkeepHomeMult = TemplateManager.global.nationalInvestmentArmyFactorHome;
            float upkeepAwayMult = TemplateManager.global.nationalInvestmentArmyFactorAway;
            float upkeepNavyMult = TemplateManager.global.nationalInvestmentNavyFactor;
            float totalArmyHomeUpkeep = (float)armiesAtHome * upkeepHomeMult;
            float totalArmyAwayUpkeep = (float)deployedArmies * upkeepAwayMult;
            float totalNavyUpkeep = (float)numNavies * upkeepNavyMult;

            if (armiesAtHome > 0)
            {
                penaltyMilitary = true;
                stringBuilder.Append(Loc.T("UI.Nation.HomeArmiesPenalty", TIUtilities.FormatSmallNumber(upkeepHomeMult), TIUtilities.FormatSmallNumber(totalArmyHomeUpkeep)));
                stringBuilder.AppendLine().AppendLine();
            }
            if (deployedArmies > 0)
            {
                penaltyMilitary = true;
                stringBuilder.Append(Loc.T("UI.Nation.AwayArmiesPenalty", TIUtilities.FormatSmallNumber(upkeepAwayMult), TIUtilities.FormatSmallNumber(totalArmyAwayUpkeep)));
                stringBuilder.AppendLine().AppendLine();
            }
            if (numNavies > 0)
            {
                penaltyMilitary = true;
                stringBuilder.Append(Loc.T("UI.Nation.NaviesPenalty", TIUtilities.FormatSmallNumber(upkeepNavyMult), TIUtilities.FormatSmallNumber(totalNavyUpkeep)));
                stringBuilder.AppendLine().AppendLine();
            }

            /* Added additional tooltip that combines Home, Away, and Navy costs. */
            // example text: The above penalties have a combined Investment Point penalty of {0}
            float combinedUpkeep = totalArmyHomeUpkeep + totalArmyAwayUpkeep + totalNavyUpkeep;
            if (armiesAtHome > 0 || deployedArmies > 0)
            {
                stringBuilder.Append(Loc.T("UI.Nation.MilitaryTotalPenalty", TIUtilities.FormatSmallNumber(combinedUpkeep), (combinedUpkeep * Tools.IPFactor(nation)).ToString("N2")));
                stringBuilder.AppendLine().AppendLine();
            }

            float combinedPenalty = (combinedUpkeep * Tools.IPFactor(nation)) + unrestPenalty;
            if (penaltyMilitary && penaltyUnrest)
            {
                stringBuilder.Append(Loc.T("UI.Nation.OverallPenaltyToIP", combinedPenalty.ToString("N2")));
            }

            __result = stringBuilder.ToString().Trim();

            return false; //Skip original method
        }
    }
}
