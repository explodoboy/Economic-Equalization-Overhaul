using PavonisInteractive.TerraInvicta;
using UnityEngine;

namespace TIEconomyMod
{
    // This is a helper class used to streamline things, and centralize important variables.
    public static class Tools
    {
        // These values are dynamically calculated inside a function.
        // They're first calculated after the mod loads, and then whenever settings are changed.
        public static double GDPPerIP;

        public static float EffectStrength(float idealGainPerMonth, TINationState nation)
        {
            // Effect strength is a factor of per-capita GDP, divided equally among all IPs a country makes each month.
            // A nation with 30k PCGDP will, if putting all of its focus on the relevant priority and with no other modifiers, increase a particular nation stat by [idealGainPerMonth].
            // A nation with 60k PCGDP will increase that stat by 2x, relative to 30k.
            // A nation with 30k PCGDP will increase that stat by 50%, relative to 30k.

            // While I like to cache the settings values for readability, I decided to try and optimize this function a little since I can expect it to be called a lot.
            // Not as much as previous versions of EEO, but still I think it's good opportunity for practice.

            // With the current default settings, this is:
            // idealGainPerMonth * (PCGDP / 30000) / 60
            return (idealGainPerMonth * (nation.perCapitaGDP / Main.settings.investmentPoints.baseEffectStrengthPCGDP)) / Main.settings.investmentPoints.IPPerMonth;
        }

        // If a country has a low simulated IP, then it's given a boost to avoid useless small countries.
        public static float GetSimulatedMonthlyIP(TINationState country)
        {
            // Multiplies base simulated IP by a multiplier, which is also based on the base simulated IP.
            float baseIP = GetSimulatedMonthlyIP_Raw(country);
            return baseIP * GetCrutchMult(baseIP);
        }

        // Returns the simulated IP a country should have. Or, the modded IP a country would have if I chose to use a simpler system.
        public static float GetSimulatedMonthlyIP_Raw(TINationState country)
        {
            //ex: 500 billion GDP / 100 billion = 5 IP/month
            return (float)(country.GDP / Tools.GDPPerIP);
        }

        // The crutch is a bonus given to nations with a low GDP. It boosts the effective GDP of a country.
        // It becomes increasingly aggressive as simulated IP approaches 0.
        public static float GetCrutchMult(float baseIP)
        {
            // Settings values are cached for readability.
            float crutchLimit = Main.settings.investmentPoints.crutchLimit;
            float crutchAggressiveness = Main.settings.investmentPoints.crutchAggressiveness;

            // crutchAggressiveness: How aggressive boost should be; higher values are more steep.
            // At a value of 1, the result is always minIP.
            // Above 1, post-processing IP approaches infinity as baseIP approaches 0, and IP approaches 0 as baseIP approaches minToTrigger. This is not recommended.
            // At 0, scaling is linear.
            // When negative, it becomes a penalty instead. This isn't recommended, either, but only because it'd further nerf small countries.

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

        // This function is a wrapper that grabs the raw crutch mult from a country, intended for use outside of calculating simulated IP.
        public static float GetCrutchMult(TINationState country)
        {
            return GetCrutchMult(GetSimulatedMonthlyIP_Raw(country));
        }

        // Returns how much a simulated IP is worth, in the real IPs every country generates.
        public static float IPFactor(TINationState nation)
        {
            return Main.settings.investmentPoints.IPPerMonth / GetSimulatedMonthlyIP(nation);
        }

        public static void Recalculate()
        {
            // 1 billion * setting value
            GDPPerIP = 1000000000 * Main.settings.investmentPoints.IPPerGDPBillions;
        }
    }
}
