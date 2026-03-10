using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection.Emit;
using System.Collections;
using System.Reflection;

namespace TIEconomyMod
{
    // THIS PRICK TOOK FOREVER TO FIND OH MY GOD I HAD TWO INSTALL TWO SEPARATE PROGRAMS TO FIND THIS ACCURSED FUNCTION
    // I REGRET NOTHING
    [HarmonyPatch(typeof(PriorityListItemController), "priorityTipStr")]
    public static class PriorityPatches
    {
        // Before the function runs, cache the current nation so an accurate tooltip may be displayed.
        private static TINationState currentNation = null;
        [HarmonyPrefix]
        public static bool CacheNation(TINationState nation)
        {
            SetCurrentNation(nation);

            return true; // Continue to the original getter.
        }

        public static void SetCurrentNation(TINationState nation)
        {
            currentNation = nation;
        }

        public static TINationState GetCurrentNation()
        {
            return currentNation;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Refer to EconomyRegionEffectPatch.cs for specifics on what's happening here.
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == EconomyRegionEffectPatch.baseOilThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, EconomyRegionEffectPatch.getOilThreshold);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == EconomyRegionEffectPatch.baseMiningThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, EconomyRegionEffectPatch.getMiningThreshold);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == EconomyRegionEffectPatch.baseEconomicThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, EconomyRegionEffectPatch.getEconomicThreshold);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == WelfareRegionEffectPatch.baseDecolonizeThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, WelfareRegionEffectPatch.getDecolonizeThreshold);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == EnvironmentRegionEffectPatch.baseCleanupThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, EnvironmentRegionEffectPatch.getCleanupThreshold);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}