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
    [HarmonyPatch(typeof(TINationState), "knowledgePriorityCohesionChange", MethodType.Getter)]
    public static class KnowledgeCohesionEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetKnowledgePriorityCohesionChangeOverwrite(ref float __result, TINationState __instance)
        {
            // Due to the moving of the Democracy effect from this to its own priority, a buff to this one may be warranted.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            float baseCohesion = Main.settings.knowledgeInvestment.baseCohesion;

            // Cohesion change is a centering effect, drawing it towards 5; additional logis is needed for that.
            // Refer to EffectStrength() comments for explanation.
            float cohesionChangeAmount = Math.Min(Mathf.Abs(__instance.cohesion - 5f), (Tools.EffectStrength(baseCohesion, __instance))); //Calculate the amount of change and prevent overshooting 5
            if (__instance.cohesion > 5f)
            {
                cohesionChangeAmount *= -1f; //Make it reduce cohesion instead if it's currently above 5
            }

            __result = cohesionChangeAmount;

            return false; //Skip original getter
        }
    }
}
