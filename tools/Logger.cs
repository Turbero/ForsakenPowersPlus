using BepInEx.Logging;
using UnityEngine;

namespace ForsakenPowersPlus
{
    public static class Logger
    {
        private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(ForsakenPowersPlusMod.ModName);
        internal static void Log(object s)
        {
            if (!ConfigurationFile.debug.Value)
            {
                return;
            }

            logger.LogInfo(s?.ToString());
        }

        internal static void LogInfo(object s)
        {
            logger.LogInfo(s?.ToString());
        }

        internal static void LogWarning(object s)
        {
            var toPrint = $"{ForsakenPowersPlusMod.ModName} {ForsakenPowersPlusMod.ModVersion}: {(s != null ? s.ToString() : "null")}";

            Debug.LogWarning(toPrint);
        }

        internal static void LogError(object s)
        {
            var toPrint = $"{ForsakenPowersPlusMod.ModName} {ForsakenPowersPlusMod.ModVersion}: {(s != null ? s.ToString() : "null")}";

            Debug.LogError(toPrint);
        }
    }
}
