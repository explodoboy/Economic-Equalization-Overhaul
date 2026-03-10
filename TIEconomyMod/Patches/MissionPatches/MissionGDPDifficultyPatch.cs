using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TIMissionModifier_TargetNationGDP), "GetModifier")]
    public static class MissionGDPDifficultyPatch
    {
        // Grab the reflection info for ObjectToNation(TIFactionState, TIGameState).
        // The function is protected, so it can't be run directly.
        private static readonly MethodInfo objectToNationMethod = AccessTools.Method(typeof(TIMissionModifier), "ObjectToNation", new Type[] { typeof(TIFactionState), typeof(TIGameState) });

        [HarmonyPrefix]
        public static bool GetModifierOverwrite(ref float __result, TICouncilorState attackingCouncilor, TIGameState target = null, float resourcesSpent = 0f, FactionResource resource = FactionResource.None)
        {
            // This patch has been disabled because the vanilla game has changed how nation difficulty modifiers has been calculated. In short, it's independent from the economyScore variable.
            // I'm not going to delete it though, in case the devs change it back in the future.

            //Patches the default amount of mission difficulty from national economy size for certain missions like control nation
            //Amount of difficulty for a given nation with a given GDP is to be identical in vanilla and this mod

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            if (target == null)
            {
                __result = 0f;
                return false; // End the overwrite here, skip original method.
            }

            __result = 0f;
            TINationState nation = YoinkObjectToNation(attackingCouncilor.faction, target);
            if (nation != null)
            {
                float vanillaEcoScore = (float)Mathd.Pow(nation.GDP / 1000000000.0, TIGlobalConfig.globalConfig.controlPointIPScaling); //This is the vanilla economyScore the country would have
                __result = vanillaEcoScore * TemplateManager.global.TIMissionModifier_TargetNationGDP_Multiplier;
            }


            return false; //Skip orignal method
        }

        // Not gonna lie, I have ChatGPT to thank for much of this.
        public static TINationState YoinkObjectToNation(TIFactionState faction, TIGameState target)
        {
            // In case the developers decide to change ObjectToNation()'s name or parameters, a check is done to confirm the method was actually acquired.
            if (objectToNationMethod == null)
            {
                throw new MissingMethodException("Could not find method ObjectToNation");
            }

            // Call the method; note that since it is static, pass null as the instance
            object result = objectToNationMethod.Invoke(null, new object[] { faction, target });
            return result as TINationState;
        }
    }
}
