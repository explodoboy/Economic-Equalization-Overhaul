using FullSerializer;
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
    [HarmonyPatch(typeof(TINationState), "economyScore", MethodType.Getter)]
    public static class BaseInvestmentPointPatch
    {
        [HarmonyPrefix]
        public static bool GetEconomyScoreOverwrite(ref float __result, TINationState __instance)
        {
            //Patches the amount of Investment Points available to a nation

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // Settings values are cached for readability.
            float crutchLimit = Main.settings.investmentPoints.crutchLimit;
            float crutchAggressiveness = Main.settings.investmentPoints.crutchAggressiveness;

            //ex: 500 billion GDP / 100 billion = 5 IP/month
            float baseIP = (float)(__instance.GDP / Tools.GDPPerIP);


            // Bonus to IP for nations with very low IP. Bonus becomes more aggressive as baseIP approaches 0.
            // The bonus is also used for research, which is otherwise unaffected by IP.

            // IPCrutchScaling: How aggressive boost should be; higher values are more steep.
            // At a value of 1, the result is always minIP.
            // Above 1, post-processing IP approaches infinity as baseIP approaches 0, and IP approaches 0 as baseIP approaches minToTrigger. This is not recommended.
            // At 0, scaling is linear.
            // When negative, it becomes a penalty instead. This isn't recommended, either, but only because it'd further nerf small countries.

            // IPCrutchMax: Below this amount, the scaling engages. This also has an effect on scaling, particularly for very low baseIP.

            __result = baseIP * GetCrutchMult(__instance);

            return false; //Skip original getter
        }

        public static float GetCrutchMult(TINationState country)
        {

            // Settings values are cached for readability.
            float crutchLimit = Main.settings.investmentPoints.crutchLimit;
            float crutchAggressiveness = Main.settings.investmentPoints.crutchAggressiveness;

            //ex: 500 billion GDP / 100 billion = 5 IP/month
            float baseIP = (float)(country.GDP / Tools.GDPPerIP);

            // 
            float crutchMult;
            if (baseIP < crutchLimit)
            {
                // (crutchLimit / baseIP) ^ crutchAggressiveness
                crutchMult = Mathf.Pow(crutchLimit / baseIP, crutchAggressiveness);
            }
            else
            {
                // if IP over max, use linear scaling
                crutchMult = 1;
            }

            return crutchMult;
        }
    }
}
