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
    [HarmonyPatch(typeof(TINationState), "OnBoostPriorityComplete")]
    internal class BoostEffectPatch
    {
        [HarmonyPrefix]
        public static bool OnBoostPriorityCompleteOverwrite(TINationState __instance)
        {
            // This patch overwrites the entire OnBoostPriorityComplete() function because there isn't a clear way to just modify the boost income while making it nation-specific.

            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            // As vanilla
            TIRegionState tIRegionState;
            if (!__instance.regions.Any((TIRegionState x) => x.boostPerYear_dekatons > 0f))
            {
                List<TIRegionState> source = new List<TIRegionState>(__instance.regions);
                if (source.Any((TIRegionState x) => x.NoOccupationUnderwayOrComplete()))
                {
                    source = source.Where((TIRegionState x) => x.NoOccupationUnderwayOrComplete()).ToList();
                }

                float min = __instance.regions.Min((TIRegionState x) => Mathf.Abs(x.boostLatitude));
                source = __instance.regions.Where((TIRegionState x) => Mathf.Abs(x.boostLatitude) == min).ToList();
                if (source.Any((TIRegionState x) => x.isCoastal))
                {
                    source = source.Where((TIRegionState x) => x.isCoastal).ToList();
                }

                if (source.Any((TIRegionState x) => !x.colonyRegion))
                {
                    source = source.Where((TIRegionState x) => !x.colonyRegion).ToList();
                }

                tIRegionState = source.MaxBy((TIRegionState x) => x.longitude);
            }
            else
            {
                Dictionary<TIRegionState, float> dictionary = new Dictionary<TIRegionState, float>();
                List<TIRegionState> source2 = new List<TIRegionState>(__instance.regions);
                if (source2.Any((TIRegionState x) => x.NoOccupationUnderwayOrComplete()))
                {
                    source2 = source2.Where((TIRegionState x) => x.NoOccupationUnderwayOrComplete()).ToList();
                }

                foreach (TIRegionState region in __instance.regions)
                {
                    float num = region.boostPerMonth_dekatons * 500000f;
                    num += Mathf.Sqrt(90.1f - Mathf.Abs(region.boostLatitude));
                    dictionary[region] = num;
                }

                tIRegionState = dictionary.SelectRandomWeightedItem((KeyValuePair<TIRegionState, float> j) => j.Value).Key;
            }

            // Boost generated is scaled by the inverse of the simulated IP factor.
            // For examples, refer to FundingEffectPatch.cs
            tIRegionState.ChangeSpaceFacilityValue(SpaceFacilityType.launchFacility, __instance.BoostIncrease(tIRegionState.boostLatitude) / Tools.IPFactor(__instance));



            return false; // Skip original method.
        }
    }

    // The following two patches adjust the investment tooltips so they reflect the actual effects per-investment.
    [HarmonyPatch(typeof(TINationState), "BoostGainLow")]
    internal class BoostGainLowPatch
    {
        [HarmonyPrefix]
        public static bool BoostGainLowOverwrite(ref float __result, TINationState __instance)
        {
            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            __result = __instance.BoostIncrease(__instance.regions.Max((TIRegionState x) => Mathf.Abs(x.boostLatitude))) / Tools.IPFactor(__instance);

            return false; // Skip original method.
        }
    }

    [HarmonyPatch(typeof(TINationState), "BoostGainHigh")]
    internal class BoostGainHighPatch
    {
        [HarmonyPrefix]
        public static bool BoostGainHighOverwrite(ref float __result, TINationState __instance)
        {
            // If mod has been disabled, abort patch and use original method.
            if (!Main.enabled) { return true; }

            __result = __instance.BoostIncrease(__instance.regions.Min((TIRegionState x) => Mathf.Abs(x.boostLatitude))) / Tools.IPFactor(__instance);

            return false; // Skip original method.
        }
    }
}
