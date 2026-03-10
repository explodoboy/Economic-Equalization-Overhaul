using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PavonisInteractive.TerraInvicta.TINationState;

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "OnUnityPriorityComplete")]
    public static class UnityEffectsPatch
    {
        [HarmonyPrefix]
        public static bool OnUnityPriorityCompleteOverwrite(TINationState __instance)
        {
            //This patch changes the effects of the unity investment
            //This overwrite is necessary to fix the propoganda effect, which would otherwise be far too powerful
            //Otherwise, this method is almost as vanilla, barring referenced values that are changed in other patches

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values cached for readability.
            float propagandaOffset = Main.settings.unityInvestment.propagandaOffset;


            //-------Propoganda Effect-------
            //As with the Spoils propoganda effect, it's unfortunately beyond me to understand this well enough to get things where I want them
            //For now, the best way for me to handle this issue is to simply disable the system entirely, pending additional research and math
            //TODO change this once I understand PropogandaOnPop

            /* I honestly don't understand this either AT ALL, but I want to have the propaganda effect active.
             * So, I'm going to do something quick and dirty, and just reduce the power of propaganda overall.
             * Yes, this will probably cause unbalanced application of the system, but I barely understand PropagandaOnPop() and want this to be in play. */

            // Below as vanilla.
            TIFactionState controlPointOfTypeFaction = __instance.GetControlPointOfTypeFaction(ControlPointType.Religion);
            foreach (TIFactionState item in __instance.FactionsWithControlPoint)
            {
                // Strength is multiplied by 0.2, to account for much higher IP.
                // For more details, refer to SpoilsEffectPatch.cs
                __instance.PropagandaOnPop_PerOwnedCP(item.ideology, TemplateManager.global.unityPublicOpinionBaseStrength * __instance.priorityEffectPopScaling * propagandaOffset / Tools.IPFactor(__instance), (controlPointOfTypeFaction == item) ? TemplateManager.global.religionUnityPublicOpinionBonusStrength : 0);
            }

            // Below as vanilla.
            __instance.AddToCohesion(__instance.unityPriorityCohesionChange, CohesionChangeReason.CohesionReason_UnityPriority);
            __instance.AddToEducation(__instance.unityPriorityEducationChange, EducationChangeReason.EducationReason_UnityPriority);



            return false; //Cancels the call to the original method but runs any other prefixes
        }
    }
}
