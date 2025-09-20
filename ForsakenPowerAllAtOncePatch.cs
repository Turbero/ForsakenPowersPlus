using System.Collections.Generic;
using ForsakenPowersPlusRemastered.tools;
using HarmonyLib;
using UnityEngine;
using Logger = ForsakenPowersPlusRemastered.tools.Logger;

namespace ForsakenPowersPlusRemastered
{
    [HarmonyPatch(typeof(Player), "StartGuardianPower")]
    public class ActivateAllGuardianPowersPrefixPatch
    {
        private static bool altAtStart;
        
        public static bool Prefix(ref Player __instance)
        {
            Logger.Log("Detect alt");
            altAtStart = Input.GetKey(KeyCode.LeftAlt);
            Logger.Log("Alt " + altAtStart);
            return true;
        }
        
        public static void Postfix(ref Player __instance)
        {
            Logger.Log("Check activate all powers available at once");
            if (ConfigurationFile.enableAllAtOnceMode.Value == ConfigurationFile.Toggle.On && altAtStart)
            {
                Logger.Log("Activating everything available at once");
                string currentPower = __instance.GetGuardianPowerName();
                
                //1) remove existing except currently selected
                SEMan sem = __instance.GetSEMan();
                List<string> existingActivePowers = new List<string>();
                foreach (StatusEffect se in sem.GetStatusEffects())
                    if (se.name.StartsWith("GP_") && !se.name.Equals(currentPower))
                        existingActivePowers.Add(se.name);

                //2) Calculate available
                List<string> otherPlayerPowersKeys = new List<string>();
                foreach (string playerKey in __instance.GetUniqueKeys())
                {
                    if (playerKey.StartsWith("GP_") && !playerKey.Equals(currentPower))
                        otherPlayerPowersKeys.Add(playerKey);
                }
                
                //3) Apply available powers
                foreach (string name in otherPlayerPowersKeys)
                {
                    if (!existingActivePowers.Contains(name))
                    {
                        int m_guardianPowerHash = string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode();
                        StatusEffect m_guardianSE = ObjectDB.instance.GetStatusEffect(m_guardianPowerHash);
                        sem.AddStatusEffect(m_guardianSE.NameHash(), true);
                    }
                }
                
                altAtStart = false;
            }
        }
    }
}