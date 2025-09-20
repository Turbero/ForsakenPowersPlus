using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using ForsakenPowersPlusRemastered.tools;
using Logger = ForsakenPowersPlusRemastered.tools.Logger;

namespace ForsakenPowersPlusRemastered
{

    public enum BossPowerEnum
    {
        GP_Eikthyr = 1,
        GP_TheElder = 2,
        GP_Bonemass = 3,
        GP_Moder = 4,
        GP_Yagluth = 5,
        GP_Queen = 6,
        GP_Fader = 7
    }
    
    [HarmonyPatch(typeof(Player), "Update")]
    public class ForsakenPower_Patch
    {
        private static void Prefix(Player __instance)
        {
            if (__instance == null)
            {
                return;
            }

            Player player = __instance;
            bool hasAnyPower = player.GetGuardianPowerName() != null && player.GetGuardianPowerName().Length > 0;
            if (Input.GetKeyDown(ConfigurationFile.ForsakenPowerHotkey.Value) && hasAnyPower)
            {
                BossPowerEnum nextAvailablePower = findNextAvailablePower(player);
                player.SetGuardianPower(nextAvailablePower.ToString());
                player.Message(MessageHud.MessageType.Center, $"{nextAvailablePower.ToString().Replace("GP_", "")} {ConfigurationFile.messagePowerSelected.Value}");
            }

            if (Input.GetKeyDown(ConfigurationFile.ResetPowerHotkey.Value) && hasAnyPower)
            {
                // resets the active power and gets the player ready for a new power
                player.m_guardianPowerCooldown = 0.1f;

                if (ConfigurationFile.enabledReset.Value == ConfigurationFile.Toggle.On)
                {
                    SEMan sem = player.GetSEMan();
                    List<int> removePowers = new List<int>();
                    foreach (StatusEffect se in sem.GetStatusEffects())
                        if (se.name.StartsWith("GP_"))
                            removePowers.Add(se.NameHash());

                    foreach (int power in removePowers)
                    {
                        sem.RemoveStatusEffect(power, true);
                        Logger.Log($"Removed {power} forsaken power.");
                    }
                    player.Message(MessageHud.MessageType.Center, ConfigurationFile.messagePowerReset.Value);
                } else
                    player.Message(MessageHud.MessageType.Center, ConfigurationFile.messagePowerReady.Value);
            }
        }

        private static BossPowerEnum findNextAvailablePower(Player player)
        {
            BossPowerEnum currentPower = (BossPowerEnum)Enum.Parse(typeof(BossPowerEnum), player.GetGuardianPowerName());
            
            List<BossPowerEnum> playerPowersKeys = new List<BossPowerEnum>();
            foreach (string playerKey in player.GetUniqueKeys())
            {
                if (playerKey.StartsWith("GP_"))
                    playerPowersKeys.Add((BossPowerEnum)Enum.Parse(typeof(BossPowerEnum), playerKey));
            }
            Logger.Log("Available powers of the player size: "+playerPowersKeys.Count);
            
            List<BossPowerEnum> sortedList = playerPowersKeys
                .FindAll(p => p > currentPower)
                .OrderBy(x => (int) x)
                .ToList();
            Logger.Log("Next powers sorted list size: "+sortedList.Count);

            if (sortedList.Count == 0)
            {
                //return first known power of the sequence
                List<BossPowerEnum> allSortedList = playerPowersKeys
                    .OrderBy(x => (int) x)
                    .ToList();
                return allSortedList[0];
            }

            //return next available power of the sequence
            BossPowerEnum firstAvailablePower = sortedList[0];
            Logger.Log("Next available: " + firstAvailablePower);

            return firstAvailablePower;
        }
    }
}