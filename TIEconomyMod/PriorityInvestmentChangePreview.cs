using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using System.Reflection;
using PavonisInteractive.TerraInvicta;
using System.Text;
using System.Linq;
using System;

namespace PriorityInvestmentChangePreview
{
    public class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static Settings settings;

        //Boilerplate code, the entry point to the mod
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            //
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            return true;
        }

        //Boilerplate code, called when the user toggles the mod on/off in UMM in-game
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        //Boilerplate code, draws the configurable settings in the UMM
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        //Boilerplate code, saves settings changes to the xml file
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }


    //Settings class to interface with Unity Mod Manager
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        //TODO: switch to localization once it is available

        //Boilerplate code to save your settings to a Settings.xml file when changed
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        //Hook to allow to do things when a value is changed, if you want
        public void OnChange()
        {

        }

        [Draw("Hide monthly and yearly changes if no IPs are allocated?", Collapsible = true)]
        public bool hideMonthlyYearlyIfNoIPs = true;

        [Draw("Show Investment Points gain in economy priority tooltip?", Collapsible = true)]
        public bool showIPGain = true;

        //[Draw("Show GHGs removed in welfare priority tooltip?", Collapsible = true)]
        //public bool showGHGsRemoved = true;

        [Draw("Always show precise predictions for education, sustainability, and military? (may degrade performance)")]
        public bool precisePredictions = false;

        [Draw("Highlight wasted investment?")]
        public bool highlightWastedInvestment = true;

        [Draw("Cap IP investment effects?")]
        public bool capIPeffects = true;

        [Draw("Show monthly instead of yearly funding income?")]
        public bool monthlyFundingIncome = true;
        [Draw("Show monthly instead of yearly boost income?")]
        public bool monthlyBoostIncome = true;
    }

    [HarmonyPatch(typeof(PriorityListItemController), nameof(PriorityListItemController.priorityTipStr))]
    static class PrePatchCaller
    {
        static public void Prefix()
        {
            PriorityMonthlyYearlyChange.is_correct_method_flag = true;
        }
    }
    
    [HarmonyPatch(typeof(NationInfoController), nameof(NationInfoController.PrioritySummaryString))]
    static class PriorityMonthlyYearlyChange
    {
        public static bool is_correct_method_flag = false;
        public static float GetEconomyScore(TINationState nation, float expectedIncrease)
        {
            float economyScore = (float)Mathd.Pow((nation.GDP + expectedIncrease * nation.population) / 1000000000.0, (double)TIGlobalConfig.globalConfig.controlPointIPScaling);
            return economyScore;
        }
        public static float SustainabilitySelfModifier(float sustainability)
        {
            if (sustainability > 2f)
            {
                return sustainability / 2f;
            }
            if (sustainability < 0.5f)
            {
                return Mathf.Max(0.25f, sustainability / 0.5f);
            }
            return 1f;
        }
        public static float EducationSelfModifier(float education)
        {
            if (education < 8.5f)
            {
                return 8.5f /Mathf.Max(1f, education); // supposed to be bigger than 1
            }
            if (education > 12f)
            {
                return 12f / Mathf.Max(1f, education); // supposed to be smaller than 1
            }
            return 1f;
        }
        public static string BuildPriorityString(TINationState nation, PriorityType priority, double current_IPs, float accumulated_IPs, float req_IPs, bool monthly, bool flagPreciseMode)
        {
            TIGlobalConfig global = TemplateManager.global;

            if (!monthly) current_IPs *= 12;
            StringBuilder stringBuilder = new StringBuilder(global.investmentInlineSpritePath).Append(TIUtilities.FormatSmallNumber(current_IPs, 7, 0, true)).Append(": ");
            int priorityCompletions = (int)((current_IPs + accumulated_IPs) / req_IPs);

            bool overinvestment = false;

            switch (priority)
            {
                case PriorityType.Economy:
                    float proposedInequalityChangeEconomy = nation.economyPriorityInequalityChange * priorityCompletions;
                    string proposedInequalityChangeEconomyString = TIUtilities.FormatSmallNumber(proposedInequalityChangeEconomy, 7, 1, true);

                    //bool inequalityOverflowEconomy = false;
                    if (nation.inequality + proposedInequalityChangeEconomy > 9f)
                    {
                        proposedInequalityChangeEconomyString = TIUtilities.YellowLine(proposedInequalityChangeEconomyString);
                    }

                    stringBuilder.Append(global.perCapitaGDPInlineSpritePath).Append((nation.economyPriorityPerCapitaIncomeChange * priorityCompletions).ToString("N2")).Append(" ").Append(global.inequalityInlineSpritePath).Append(proposedInequalityChangeEconomyString);
                    if (Main.settings.showIPGain) stringBuilder.Append(" ").Append(global.investmentInlineSpritePath).Append(TIUtilities.FormatSmallNumber(GetEconomyScore(nation, nation.economyPriorityPerCapitaIncomeChange * priorityCompletions) - nation.economyScore));
                    break;
                case PriorityType.Welfare:
                    float proposedInequalityChangeWelfare = nation.welfarePriorityInequalityChange * priorityCompletions;
                    string proposedInequalityChangeWelfareString = TIUtilities.FormatSmallNumber(proposedInequalityChangeWelfare, 7, 1, true);

                    //bool inequalityOverflowWelfare = false;
                    if (nation.inequality + proposedInequalityChangeWelfare < 1)
                    {
                        proposedInequalityChangeWelfareString = TIUtilities.YellowLine(proposedInequalityChangeWelfareString);
                    }
                    stringBuilder.Append(global.inequalityInlineSpritePath).Append(proposedInequalityChangeWelfareString);
                    break;
                case PriorityType.Environment:
                    if (nation.sustainability > 0)
                    {
                        int estimateComparison;
                        if (SustainabilitySelfModifier(nation.sustainability) > SustainabilitySelfModifier(nation.sustainability + nation.environmentPrioritySustainabilityChange * priorityCompletions))
                        {
                            estimateComparison = 1; // overestimate
                        }
                        else if (SustainabilitySelfModifier(nation.sustainability) < SustainabilitySelfModifier(nation.sustainability + nation.environmentPrioritySustainabilityChange * priorityCompletions))
                        {
                            estimateComparison = -1; // underestimate
                        }
                        else estimateComparison = 0; // correct estimate

                        if (flagPreciseMode || Main.settings.precisePredictions)
                        {
                            float preciseSustainabilityChange = 0f;
                            if (estimateComparison != 0)
                            {
                                float currentSus = nation.sustainability;
                                float environmentPrioritySustainabilityChangeSimulatedBase = nation.priorityEffectPopScaling * (TemplateManager.global.environmentPrioritySustainabilityChange + TIEffectsState.SumEffectsModifiers(Context.Environment_SustainabilityChange, nation, TemplateManager.global.environmentPrioritySustainabilityChange));
                                float environmentPrioritySustainabilityChangeSimulated;

                                for (int i = 0; i < priorityCompletions; i++)
                                {
                                    environmentPrioritySustainabilityChangeSimulated = environmentPrioritySustainabilityChangeSimulatedBase * SustainabilitySelfModifier(currentSus);
                                    preciseSustainabilityChange += environmentPrioritySustainabilityChangeSimulated;
                                    currentSus += environmentPrioritySustainabilityChangeSimulated;
                                }
                            }
                            else
                            {
                                preciseSustainabilityChange = nation.environmentPrioritySustainabilityChange * priorityCompletions;
                            }

                            string preciseSustainabilityChangeString = nation.SustainabilityChangeForDisplay(preciseSustainabilityChange);
                            if (nation.sustainability + preciseSustainabilityChange < 0) preciseSustainabilityChangeString = TIUtilities.YellowLine(preciseSustainabilityChangeString);

                            stringBuilder.Append(nation.SustainabilityIconInlinePath()).Append(preciseSustainabilityChangeString);
                        }
                        else
                        {
                            stringBuilder.Append(nation.SustainabilityIconInlinePath()).Append(nation.SustainabilityChangeForDisplay(nation.environmentPrioritySustainabilityChange * priorityCompletions)).Append(estimateComparison == 0 ? "" : global.warningInlineSpritePath);
                        }
                    }
                    else
                    {
                        stringBuilder.Append(nation.SustainabilityIconInlinePath());
                        stringBuilder.Append(Loc.T("UI.Nation.WelfareGHGReductionShort", new object[]
                        {
                            TIUtilities.FormatSmallNumber(priorityCompletions * nation.EnvPriorityCO2Removed()),
                            TIUtilities.FormatSmallNumber(priorityCompletions * nation.EnvPriorityCH4Removed()),
                            TIUtilities.FormatSmallNumber(priorityCompletions * nation.EnvPriorityN2ORemoved())
                        }));
                    }
                    break;
                case PriorityType.Knowledge:
                    float currentEducation = nation.education;
                    
                    float proposedCohesionChangeKnowledge = nation.knowledgePriorityCohesionChange * priorityCompletions;
                    float proposedEducationChangeKnowledge = nation.knowledgePriorityEducationChange * priorityCompletions;

                    bool overestimateEducation = false;
                    if (EducationSelfModifier(currentEducation) > EducationSelfModifier(proposedEducationChangeKnowledge)) overestimateEducation = true;

                    if (overestimateEducation && (flagPreciseMode || Main.settings.precisePredictions))
                    {
                        float KnowledgePriorityEducationChangeSimulated;

                        proposedEducationChangeKnowledge = 0;
                        for (int i = 0; i < priorityCompletions; i++)
                        {
                            KnowledgePriorityEducationChangeSimulated = TemplateManager.global.knowledgePriorityEducationIncrease * nation.priorityEffectPopScaling * EducationSelfModifier(currentEducation);
                            currentEducation += KnowledgePriorityEducationChangeSimulated;
                            proposedEducationChangeKnowledge += KnowledgePriorityEducationChangeSimulated;
                        }
                    }

                    string proposedEducationChangeKnowledgeString = TIUtilities.FormatSmallNumber(proposedEducationChangeKnowledge, 7, 1, true);
                    string proposedCohesionChangeKnowledgeString = TIUtilities.FormatSmallNumber(proposedCohesionChangeKnowledge, 7, 1, true);

                    //bool cohesionOverflowKnowledge = false;
                    if (Main.settings.highlightWastedInvestment)
                    {
                        if (nation.cohesion < 5f && nation.cohesion + proposedCohesionChangeKnowledge > 5f)
                        {
                            proposedCohesionChangeKnowledgeString = TIUtilities.YellowLine(proposedCohesionChangeKnowledgeString);
                        }
                        else if (nation.cohesion > 5f && nation.cohesion + proposedCohesionChangeKnowledge < 5f)
                        {
                            proposedCohesionChangeKnowledgeString = TIUtilities.YellowLine(proposedCohesionChangeKnowledgeString);
                        }
                    }

                    stringBuilder.Append(global.educationInlineSpritePath).Append(proposedEducationChangeKnowledgeString).Append(" ").Append(global.cohesionInlineSpritePath).Append(proposedCohesionChangeKnowledgeString);
                    break;
                case PriorityType.Government:
                    float proposedDemocracyChangeGovernment = nation.governmentPriorityDemocracyChange * priorityCompletions;

                    string proposedDemocracyChangeGovernmentString = TIUtilities.FormatSmallNumber(proposedDemocracyChangeGovernment, 7, 1, true);

                    if (Main.settings.highlightWastedInvestment)
                    {
                        if (nation.democracy + proposedDemocracyChangeGovernment > 10f)
                            proposedDemocracyChangeGovernmentString = TIUtilities.YellowLine(proposedDemocracyChangeGovernmentString);
                    }

                    stringBuilder.Append(global.democracyInlineSpritePath).Append(proposedDemocracyChangeGovernmentString);

                    break;
                case PriorityType.Unity:
                    float proposedCohesionChangeUnity = nation.unityPriorityCohesionChange * priorityCompletions;
                    float proposedEducationChangeUnity = nation.unityPriorityEducationChange * priorityCompletions;

                    string proposedCohesionChangeUnityString = TIUtilities.FormatSmallNumber(proposedCohesionChangeUnity, 7, 1, true);
                    string proposedEducationChangeUnityString = TIUtilities.FormatSmallNumber(proposedEducationChangeUnity, 7, 1, true);

                    //bool cohesionOverflowUnity = false;
                    //bool democracyOverflowUnity = false;
                    if (nation.cohesion + proposedCohesionChangeUnity > 10f)
                    {
                        proposedCohesionChangeUnityString = TIUtilities.YellowLine(proposedCohesionChangeUnityString);
                    }
                    if (nation.education + proposedEducationChangeUnity < 0f)
                    {
                        proposedEducationChangeUnityString = TIUtilities.YellowLine(proposedEducationChangeUnityString);
                    }

                    stringBuilder.Append(global.cohesionInlineSpritePath).Append(proposedCohesionChangeUnityString).Append(" ").Append(global.educationInlineSpritePath).Append(proposedEducationChangeUnityString);
                    break;
                case PriorityType.Oppression:
                    float proposedDemocracyChangeOppression = nation.OppressionPriorityDemocracyChange * priorityCompletions;
                    float proposedCohesionChangeOppression;
                    if (nation.democracy > 5)
                        proposedCohesionChangeOppression = nation.OppressionPriorityCohesionChange * priorityCompletions;
                    else
                        proposedCohesionChangeOppression = 0f;

                    string proposedDemocracyChangeOppressionString = TIUtilities.FormatSmallNumber(proposedDemocracyChangeOppression, 7, 1, true);
                    string proposedCohesionChangeOppressionString = TIUtilities.FormatSmallNumber(proposedCohesionChangeOppression, 7, 1, true);

                    if (flagPreciseMode || Main.settings.precisePredictions)
                    {
                        float preciseUnrestChange = 0;
                        float currentUnrest = nation.unrest;
                        float UnrestChangeSimulated;

                        for (int i = 0; i < priorityCompletions; i++)
                        {
                            UnrestChangeSimulated = TemplateManager.global.oppressionPriorityUnrestMultiplier * nation.priorityEffectPopScaling * -Mathf.Min(currentUnrest, 0.1f - nation.democracy / 100f);
                            preciseUnrestChange += UnrestChangeSimulated;
                            currentUnrest += UnrestChangeSimulated;
                        }
                        if (currentUnrest < 0)
                        {
                            currentUnrest = 0;
                            preciseUnrestChange = -nation.unrest;
                        }

                        string preciseUnrestChangeString = TIUtilities.FormatSmallNumber(preciseUnrestChange, 7, 1, true);

                        //bool miltechOverflowMilitary = false;
                        //bool unrestOverflowMilitary = false;
                        if (Main.settings.highlightWastedInvestment)
                        {
                            if (nation.OppressionPriorityUnrestChange < -1e-4 && currentUnrest < 1e-3) preciseUnrestChangeString = TIUtilities.YellowLine(preciseUnrestChangeString);
                        }
                        //FileLog.Log($"nation.militaryPriorityUnrestChange: {nation.militaryPriorityUnrestChange}, currentUnrest: {currentUnrest}");

                        stringBuilder.Append(global.unrestInlineSpritePath).Append(preciseUnrestChangeString);
                    }
                    else
                    {
                        bool overestimateMiltech = nation.militaryTechLevel < TIGlobalValuesState.GlobalValues.bestGlobalHumanMiltech;
                        bool overestimateUnrest = nation.unrest + nation.OppressionPriorityUnrestChange * priorityCompletions > nation.unrestRestState;

                        stringBuilder.Append(global.unrestInlineSpritePath).Append(TIUtilities.FormatSmallNumber((double)(nation.OppressionPriorityUnrestChange * priorityCompletions), 7, 0, true)).Append(overestimateUnrest ? global.warningInlineSpritePath : "");
                    }

                    stringBuilder.Append(" ").Append(global.democracyInlineSpritePath).Append(proposedDemocracyChangeOppressionString);
                    if (proposedCohesionChangeOppression != 0f)
                        stringBuilder.Append(" ").Append(global.cohesionInlineSpritePath).Append(proposedCohesionChangeOppressionString);
                    break;
                case PriorityType.Funding:
                    stringBuilder.Append(global.moneyInlineSpritePath);
                    stringBuilder.Append(TIUtilities.FormatSmallNumber((double)(nation.spaceFundingPriorityIncomeChange * priorityCompletions / (Main.settings.monthlyFundingIncome ? 12f : 1f)), 7, 1, true));
                    stringBuilder.Append(Loc.T(Main.settings.monthlyFundingIncome ? "PriorityTooltipExpanded.Monthly" : "UI.Nation.Yearly"));
                    break;
                case PriorityType.Spoils:
                    float proposedInequalityChangeSpoils = nation.spoilsPriorityInequalityChange * priorityCompletions;
                    float proposedDemocracyChangeSpoils = nation.spoilsPriorityDemocracyChange * priorityCompletions;
                    
                    string proposedInequalityChangeSpoilsString = TIUtilities.FormatSmallNumber(proposedInequalityChangeSpoils, 7, 1, true);
                    string proposedDemocracyChangeSpoilsString = TIUtilities.FormatSmallNumber(proposedDemocracyChangeSpoils, 7, 1, true);

                    //bool inequalityOverflowSpoils = false;
                    if (nation.inequality + proposedInequalityChangeSpoils > 9)
                    {
                        proposedInequalityChangeSpoilsString = TIUtilities.YellowLine(proposedInequalityChangeSpoilsString);
                    }
                    //bool democracyOverflowSpoils = false;
                    if (nation.democracy + proposedDemocracyChangeSpoils < 0)
                    {
                        proposedDemocracyChangeSpoilsString = TIUtilities.YellowLine(proposedDemocracyChangeSpoilsString);
                    }

                    stringBuilder.Append(global.moneyInlineSpritePath).Append((nation.spoilsPriorityMoney * priorityCompletions).ToString("N1")).Append(" ").Append(global.inequalityInlineSpritePath).Append(proposedInequalityChangeSpoilsString).Append(" ").Append(global.democracyInlineSpritePath).Append(proposedDemocracyChangeSpoilsString).Append(" ").Append(global.sustainabilityInlineSpritePath_Red).Append(TIUtilities.FormatSmallNumber((double)(nation.spoilsSustainabilityChange * priorityCompletions), 7, 0, true));
                    break;
                case PriorityType.Civilian_InitiateSpaceflightProgram:
                    if (current_IPs + accumulated_IPs > req_IPs + 1) overinvestment = true;

                    stringBuilder.Append(global.boostInlineSpritePath).Append(nation.spaceflightInitialBoost * Mathf.Min(priorityCompletions, 1));
                    break;
                case PriorityType.LaunchFacilities:
                    {
                        float num = nation.BoostGainLow();
                        float num2 = nation.BoostGainHigh();
                        if (num != num2)
                        {
                            stringBuilder.Append(global.boostInlineSpritePath).Append(Loc.T("UI.Nation.BoostRange", new object[]
                            {
                                TIUtilities.FormatSmallNumber((double)num * priorityCompletions / (Main.settings.monthlyBoostIncome ? 12f : 1f), 7, 0, true),
                                TIUtilities.FormatSmallNumber((double)num2 * priorityCompletions / (Main.settings.monthlyBoostIncome ? 12f : 1f), 7, 0, true)
                            })).Append(Loc.T(Main.settings.monthlyBoostIncome ? "PriorityTooltipExpanded.Monthly" : "UI.Nation.Yearly"));
                        }
                        else
                        {
                            stringBuilder.Append(global.boostInlineSpritePath);
                            stringBuilder.Append(TIUtilities.FormatSmallNumber((double)num * priorityCompletions / (Main.settings.monthlyBoostIncome ? 12f : 1f), 7, 0, true));
                            stringBuilder.Append(Loc.T(Main.settings.monthlyBoostIncome ? "PriorityTooltipExpanded.Monthly" : "UI.Nation.Yearly"));
                        }
                        break;
                    }
                case PriorityType.MissionControl:
                    if (Main.settings.highlightWastedInvestment)
                    {
                        int maxMissionControl = 0;
                        foreach (TIRegionState region in nation.regions)
                        {
                            maxMissionControl += region.maxMissionControl;
                        }
                        int proposedTotalMissionControl = nation.currentMissionControl + priorityCompletions;

                        if (proposedTotalMissionControl >= maxMissionControl)
                        {
                            if (Main.settings.capIPeffects) priorityCompletions = maxMissionControl - nation.missionControl;
                            if (current_IPs + accumulated_IPs > req_IPs * priorityCompletions + 1) overinvestment = true;
                        }
                    }
                    stringBuilder.Append(global.missionControlInlineSpritePath).Append(priorityCompletions);
                    break;
                case PriorityType.Military_FoundMilitary:
                    {
                        if (current_IPs + accumulated_IPs > req_IPs + 1) overinvestment = true;

                        stringBuilder.Append(global.miltechInlineSpritePath).Append(Mathf.Min(priorityCompletions, 1));
                        break;
                    }
                case PriorityType.Military:
                    if (flagPreciseMode || Main.settings.precisePredictions)
                    {
                        float preciseMiltechChange = 0;
                        float currentMiltech = nation.militaryTechLevel;
                        float MiltechChangeSimulated;
                        float bestHumanMiltech = TIGlobalValuesState.GlobalValues.bestGlobalHumanMiltech;

                        for (int i = 0; i < priorityCompletions; i++)
                        {
                            if (currentMiltech > bestHumanMiltech) bestHumanMiltech = currentMiltech;
                            MiltechChangeSimulated = TemplateManager.global.militaryPriorityMiltechIncrease * (bestHumanMiltech / Mathf.Max(currentMiltech, 1f));
                            preciseMiltechChange += MiltechChangeSimulated;
                            currentMiltech += MiltechChangeSimulated;
                        }

                        string preciseMiltechChangeString = TIUtilities.FormatSmallNumber(preciseMiltechChange, 7, 1, true);

                        //bool miltechOverflowMilitary = false;
                        //bool unrestOverflowMilitary = false;
                        if (Main.settings.highlightWastedInvestment)
                            if (nation.militaryTechLevel + preciseMiltechChange > nation.maxMilitaryTechLevel) preciseMiltechChangeString = TIUtilities.YellowLine(preciseMiltechChangeString);
                        
                        //FileLog.Log($"nation.militaryPriorityUnrestChange: {nation.militaryPriorityUnrestChange}, currentUnrest: {currentUnrest}");

                        stringBuilder.Append(global.miltechInlineSpritePath).Append(" ").Append(preciseMiltechChangeString);
                    }
                    else
                    {
                        bool overestimateMiltech = nation.militaryTechLevel < TIGlobalValuesState.GlobalValues.bestGlobalHumanMiltech;
                        stringBuilder.Append(global.miltechInlineSpritePath).Append(" ").Append(TIUtilities.FormatSmallNumber((double)(nation.militaryPriorityTechLevelChange * priorityCompletions), 7, 1, true));
                    }
                    break;
                case PriorityType.Military_BuildArmy:
                    if (Main.settings.highlightWastedInvestment)
                    {
                        int proposedTotalArmies = nation.numStandardArmies + priorityCompletions;
                        if (proposedTotalArmies >= nation.allowedArmies)
                        {
                            if (Main.settings.capIPeffects) priorityCompletions = nation.allowedArmies - nation.numStandardArmies;
                            if (current_IPs + accumulated_IPs > req_IPs * priorityCompletions + 1) overinvestment = true;
                        }
                    }
                    // TODO: loc
                    stringBuilder.Append((priorityCompletions > 0) ? priorityCompletions.ToString() : "No").Append((priorityCompletions != 1) ? " new armies" : " new army");
                    break;
                case PriorityType.Military_BuildNavy:
                    if (Main.settings.highlightWastedInvestment)
                    {
                        int proposedTotalNavies = nation.numNavies + priorityCompletions;
                        if (proposedTotalNavies >= nation.numStandardArmies)
                        {
                            if (Main.settings.capIPeffects) priorityCompletions = nation.numStandardArmies - nation.numNavies;
                            if (current_IPs + accumulated_IPs > req_IPs * priorityCompletions + 1) overinvestment = true;
                        }
                    }
                    // TODO: loc
                    stringBuilder.Append((priorityCompletions > 0) ? priorityCompletions.ToString() : "No").Append((priorityCompletions != 1) ? " new navies" : " new navy");
                    break;
                case PriorityType.Military_InitiateNuclearProgram:
                    if (current_IPs + accumulated_IPs > req_IPs + 1) overinvestment = true;

                    stringBuilder.Append(global.nukesInlineSpritePath).Append(Mathf.Min(priorityCompletions, 1));
                    break;
                case PriorityType.Military_BuildNuclearWeapons:
                    stringBuilder.Append(global.nukesInlineSpritePath).Append(priorityCompletions);
                    break;
                case PriorityType.Military_BuildSpaceDefenses:
                    if (Main.settings.highlightWastedInvestment)
                    {
                        int numSpaceDefenses = 0;
                        foreach (TIRegionState region in nation.regions)
                        {
                            numSpaceDefenses += region.antiSpaceDefenses ? 1 : 0;
                        }
                        int proposedTotalSpaceDefenses = priorityCompletions + numSpaceDefenses;

                        if (proposedTotalSpaceDefenses >= nation.regions.Count)
                        {
                            if (Main.settings.capIPeffects) priorityCompletions = nation.regions.Count - numSpaceDefenses;
                            if (current_IPs + accumulated_IPs > req_IPs * priorityCompletions + 1) overinvestment = true;
                        }
                    }
                    // TODO: loc
                    stringBuilder.Append((priorityCompletions > 0) ? priorityCompletions.ToString() : "No").Append((priorityCompletions != 1) ? " regions gain anti-space defenses" : " region gains anti-space defenses");
                    break;
                case PriorityType.Military_BuildSTOSquadron:
                    if (Main.settings.highlightWastedInvestment)
                    {
                        int numSpaceFighters = nation.numSTOFighters;
                        int proposedTotalSpaceFighters = priorityCompletions + numSpaceFighters;
                        int maxSpaceFighters = nation.regions.Sum(x => x.maxSTOFighters);

                        if (proposedTotalSpaceFighters >= maxSpaceFighters)
                        {
                            if (Main.settings.capIPeffects) priorityCompletions = maxSpaceFighters - numSpaceFighters;
                            if (current_IPs + accumulated_IPs > req_IPs * priorityCompletions + 1) overinvestment = true;
                        }
                    }
                    // TODO: loc
                    stringBuilder.Append((priorityCompletions > 0) ? priorityCompletions.ToString() : "No").Append((priorityCompletions != 1) ? " new exofighters" : " new exofighter");
                    break;
            }

            // TODO: loc
            stringBuilder.Append(monthly ? " (monthly)" : " (yearly)");

            if (overinvestment) return TIUtilities.YellowLine(stringBuilder.ToString());
            else return stringBuilder.ToString();
        }
        static public void Postfix(ref string __result, PriorityType priority, TINationState nation)
        {
            if (!Main.enabled || !is_correct_method_flag) return;
            
            is_correct_method_flag = false;

            // TODO: loc
            __result += " (one-off)";

            bool flagAllIn = false;
            bool flagPreciseMode = false;

            if (TIInputManager.IsControlKeyDown) flagAllIn = true;
            if (TIInputManager.IsShiftKeyDown) flagPreciseMode = true;

            TIGlobalConfig global = TemplateManager.global;

            double current_IPs;
            // float diversity_bonus;
            // double prio_bonus = 0;
            // double prio_weight;
            double prioWeightTotal;
            // double IPs_per_CP = nation.GetInvestmentFromControlPoint();

            // foreach (TIControlPoint CP in nation.controlPoints)
            // {
                // if (CP.faction != null)
                // {
                //     prio_bonus = CP.faction.SumPriorityBonuses(priority, false);
                // }
                // if (flagAllIn)
                // {
                //     // diversity_bonus = 0;
                //     // prio_weight = 1;
                // }
                // else
                // {
                //     // TODO: check what this is really
                //     // diversity_bonus = CP.diversityBonus[priority];
                //     // prio_weight = (float)CP.GetControlPointPriority(priority, true);
                //     // prio_weight /= (float)CP.totalWeightsForControlPoint;
                // }
                // // prio_weight_total += prio_weight / (float)nation.numControlPoints;
                // TODO: redefine current_IPs to account for new modifiers, like fed bonus for eco
                // current_IPs += IPs_per_CP * (1 + prio_bonus + diversity_bonus + nation.NationalPriorityBonuses(priority)) * prio_weight;
            // }
            if (!flagAllIn)
            {
                prioWeightTotal = nation.percentWeighttoPriority(priority);
                current_IPs = nation.ControlPointWeightsTotalToPriorityIP(priority) * 30.436874f;
            }
            else
            {
                prioWeightTotal = 1f;
                current_IPs = 0f;
                float investmentFromControlPoint = nation.GetInvestmentFromControlPoint();
                for (int i = 0; i < nation.controlPoints.Count; i++)
                {
                    TIControlPoint ticontrolPoint = nation.controlPoints[i];
                    float IPsFromControlPoint = investmentFromControlPoint;
                    if (IPsFromControlPoint > 0f)
                    {
                        float num;
                        if (!ticontrolPoint.benefitsDisabled)
                        {
                            TIFactionState faction = ticontrolPoint.faction;
                            num = ((faction != null) ? faction.cachedPriorityBonuses[priority] : 0f);
                        }
                        else
                        {
                            num = 0f;
                        }
                        num += nation.NationalPriorityBonuses(priority);
                        IPsFromControlPoint *= 1f + num;
                    }
                    current_IPs += IPsFromControlPoint;
                }
            }

            // FileLog.Log($"priority: {priority}, prioWeightTotal: {prioWeightTotal}, current IPs: {current_IPs}");

            if (!flagAllIn && Main.settings.hideMonthlyYearlyIfNoIPs && prioWeightTotal == 0) return;

            float req_IPs = global.GetRequiredInvestmentPoints(priority);
            float accumulated_IPs = nation.GetAccumulatedInvestmentPoints(priority);

            string monthlyString = BuildPriorityString(nation, priority, current_IPs, accumulated_IPs, req_IPs, monthly: true, flagPreciseMode);
            string yearlyString = BuildPriorityString(nation, priority, current_IPs, accumulated_IPs, req_IPs, monthly: false, flagPreciseMode);
            
            __result += "\n" + monthlyString + "\n" + yearlyString;
            // TODO: loc
            if (flagAllIn) __result += "\n<color=orange>This is the result of spending 100% of the investment points produced by this nation in this priority.</color>";
        }
    }
}