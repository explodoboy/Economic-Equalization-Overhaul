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
    [HarmonyPatch(typeof(TINationState), "welfarePriorityInequalityChange", MethodType.Getter)]
    public static class WelfareInequalityEffectPatch
    {
        [HarmonyPrefix]
        public static bool GetWelfarePriorityInequalityChangeOverwrite(ref float __result, TINationState __instance)
        {
            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            float baseInequality = Main.settings.welfareInvestment.baseInequality;

            // Refer to EffectStrength() comments for explanation.
            __result = Tools.EffectStrength(baseInequality, __instance);



            return false; //Skip original getter
        }
    }
}
