using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace ForsakenPowersPlus
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ForsakenPowersPlusMod : BaseUnityPlugin
    {
        internal const string ModName = "ForsakenPowersPlus";
        internal const string ModVersion = "1.3.0";
        internal const string Author = "TastyChickenLegs";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        internal static string ConnectionError = "";

        private readonly Harmony _harmony = new(ModGUID);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public static int bossesDefeatedCount;
        public static ConfigEntry<bool> debugMode;
        public static ConfigEntry<bool> enabledMod;
        public static ConfigEntry<bool> enabledReset;
        public static ConfigEntry<bool> enabledAllBosses;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<KeyboardShortcut> ForsakenPowerHotkey;
        public static ConfigEntry<KeyboardShortcut> ResetPowerHotkey;
        public static ConfigEntry<float> guardianBuffDuration;
        public static ConfigEntry<float> guardianBuffCooldown;
        public static ManualLogSource logger;
        public static string PluginName = "ForsakenPowers";
        public static ConfigEntry<bool> enableBuffChange;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> enablepassivemode;
        public static ConfigEntry<bool> disableGui;
        public static ConfigEntry<bool> enableStacking;
        public static ConfigEntry<KeyboardShortcut> StackingPowerHotkey;
        public static ConfigEntry<bool> enabledAllBossesAtOnce;
        public static ConfigEntry<bool> configVerifyClient;
        public static List<string> allBossesKilled;
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            _serverConfigLocked = config("", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            enabledMod = config("", "Mod Enabled", true, "Enable this mod");

            //debugMode = Config.Bind<bool>("", "Debug Mode", false,
            //    new ConfigDescription("Turn on to enable console logging for debuggin with Mod Author",
            //    null, new ConfigurationManagerAttributes { DispName = "Debug Mode"}));

            enabledReset = Config.Bind<bool>("General", "Enable Powers Reset", true,
                new ConfigDescription("Enable the ability to reset the current power. If this is disabled the player may stack powers.  See the ReadMe for more information."
            , null, new  { Order = 4, DispName = "Enable Power Reset " }));
            
            
            ForsakenPowerHotkey = config("General", "ForsakenPowerHotkey", new KeyboardShortcut(KeyCode.F8),
                new ConfigDescription("Key Used to toggle through powers"
            , null, new  { Order = 2, DispName = "Forsaken Power Hotkey" }));

            ResetPowerHotkey = config("General", "ResetPowerHotkey", new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription("Press to reset the cooldown of your Forsaken Power and use another Power.",
                null, new  { Order = 3, DispName = "Reset Power HotKey" }));

            enabledAllBosses = config("General", "EnableAllBosses", false,
                new ConfigDescription("Whether or not to enable usage of all the bosses or just the ones you have defeated",
                null, new  { DispName = "Enable All Bosses" }));

            enabledAllBossesAtOnce = config("General", "EnableAllBossesAtOnce", false,
                new ConfigDescription("Enable usage of all the bosses at one time.",
                null, new  { DispName = "Enable Activating All Bosses At Once" }));
            //enableBuffChange = Config.Bind("Buff Changes", "EnableBuffChanges - Overrides Passive", true, "Enable changing BuffCooldown and Duration.  Overrides the enablepassivemode setting");
            guardianBuffCooldown = config("Buff Changes", "guardianBuffCoolDown (Restart Game)",
                1200f, new ConfigDescription("Time before the next guardian power can be used.",
                null, new  { DispName = "Guardian Buff Cooldown (Seconds)" }));

            guardianBuffDuration = config("Buff Changes", "guardianBuffDuration (Restart Game)"
                , 300f, new ConfigDescription("Time in seconds the guardian power lasts.",
                null, new  { DispName = "Guardian Buff Durration (Seconds)" }));

            enablepassivemode = config("Buff Changes", "Passive Mode - Overrides BuffChange", false, "Set the Power to never expire - Game Restart Required.  Overrides the enableBuffChange setting");

            configVerifyClient = config("", "Verify Clients", false, "Disable this to turn off the client verification and version checks.");
            //disableGui = Config.Bind<bool>("Buff Changes", "Remove Powers from GUI", false, "Remove the Powers from the screen to declutter the interface.");
            //nexusID = Config.Bind<int>("General", "NexusID", 2067, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes {ReadOnly=true }));

            logger = Logger;

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
            
        }
        private void OnDestroy()
        {
            _harmony.UnpatchSelf(); 
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                logger.LogDebug("ReadConfigValues called");
                Config.Reload();
                ChangePassiveMode();
            }
            catch
            {
                logger.LogError($"There was an issue loading your {ConfigFileName}");
                logger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }
        
        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion

        // used to define the bosses to activate all power in passive mode //

        public static Dictionary<string, bool> powers = new Dictionary<string, bool>()
        {
                    { "GP_Eikthyr", false },
                    { "GP_TheElder", false },
                    { "GP_Bonemass", false },
                    { "GP_Moder", false },
                    { "GP_Yagluth", false },
                    { "GP_Queen", false },
        };

        //used to define the bosses for the check keys count //

        public static List<string> keyPowers = new List<string>()
        {
                    { "defeated_eikthyr"},
                    { "defeated_gdking"},
                    { "defeated_bonemass"},
                    { "defeated_dragon"},
                    { "defeated_goblinking"},
                    { "defeated_queen"},
        };

        //Once the config file is changed, reload the settings and populate the datatables.
        private static void ChangePassiveMode()
        {
            if (!enablepassivemode.Value)
            {
                //if not passive mode then allow the user to cancel the power
                foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                {
                    if (se.name.StartsWith("GP_"))
                    {
                        se.m_ttl = guardianBuffDuration.Value;
                        //logger.LogInfo($"[{PluginName}] Made {se.name} Buff Duration set to. {guardianBuffDuration.Value}");
                        se.m_cooldown = guardianBuffCooldown.Value;
                        //logger.LogInfo($"[{PluginName}] Made {se.name} Buff Cooldown set to. {guardianBuffCooldown.Value}");
                    }
                }
            }
            else
            {
                //if Passive set everything to never expire.
                foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                {
                    if (se.name.StartsWith("GP_"))
                    {
                        se.m_ttl = 0f;
                        se.m_cooldown = 0f;
                        //  logger.LogInfo($"[{PluginName}] Made {se.name} status effect passive.");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "StartGuardianPower")]
        internal class Patch_Player_StartGuardianPower
        {
            private static void Prefix(ref Player __instance)
            {
                foreach (var powerKV in powers)
                {
                    if (powerKV.Value || enabledAllBossesAtOnce.Value)
                    {
                        Traverse.Create(__instance.GetSEMan().AddStatusEffect(powerKV.Key, true));
                        Debug.Log($"[{PluginName}] Applying {powerKV.Key} forsaken power.");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        internal class Patch_ObjectDB_CopyOtherDB
        {
            private static void Postfix(ref ObjectDB __instance)
            {
                if (!enablepassivemode.Value)
                {
                    foreach (StatusEffect se in __instance.m_StatusEffects)
                    {
                        if (se.name.StartsWith("GP_"))
                        {
                            se.m_ttl = guardianBuffDuration.Value;
                            // logger.LogInfo($"[{PluginName}] Made {se.name} Buff Duration set to. {guardianBuffDuration.Value}");
                            se.m_cooldown = guardianBuffCooldown.Value;
                            // logger.LogInfo($"[{PluginName}] Made {se.name} Buff Cooldown set to. {guardianBuffCooldown.Value}");
                        }
                    }
                }
                else
                {
                    foreach (StatusEffect se in __instance.m_StatusEffects)
                    {
                        if (se.name.StartsWith("GP_"))
                        {
                            se.m_ttl = 0f;
                            se.m_cooldown = 0f;
                            // logger.LogInfo($"[{PluginName}] Made {se.name} status effect passive.");
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        private class ForsakenPower_Patch
        {
            private static void Prefix(Player __instance)
            {
                if (!((object)Player.m_localPlayer != null))
                {
                    return;
                }
                if (enabledAllBosses.Value)
                {
                    if (Input.GetKeyDown(ForsakenPowerHotkey.Value.MainKey))
                    {
                        GetPlayerGlobalKeys();

                        logger.LogInfo(Player.m_localPlayer.GetGuardianPowerName());
                        switch (Player.m_localPlayer.GetGuardianPowerName())
                            
                        {
                            
                            case "GP_Eikthyr":
                                Player.m_localPlayer.SetGuardianPower("GP_TheElder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Elder Power Selected");
                                break;

                            case "GP_TheElder":
                                Player.m_localPlayer.SetGuardianPower("GP_Bonemass");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Bonemass Power Selected");
                                break;

                            case "GP_Bonemass":
                                Player.m_localPlayer.SetGuardianPower("GP_Moder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Moder Power Selected");
                                break;

                            case "GP_Moder":
                                Player.m_localPlayer.SetGuardianPower("GP_Yagluth");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Yagluth Power Selected");
                                break;

                            case "GP_Yagluth":
                                Player.m_localPlayer.SetGuardianPower("GP_Queen");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "The Queen Power Selected");
                                break;

                            case "GP_Queen":
                                Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                                break;
                        }
                    }
                }
                else if (Input.GetKeyDown(ForsakenPowerHotkey.Value.MainKey))
                {
                    //get the number of bosses the player has beaten //
                    GetPlayerGlobalKeys();

                    logger.LogInfo("ForsakenPowerPlus - Number of bosses defeated = " + bossesDefeatedCount.ToString());

                    if (bossesDefeatedCount == 1)
                    {
                        Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                    }
                    else if (bossesDefeatedCount == 2)
                    {
                        string guardianPowerName = Player.m_localPlayer.GetGuardianPowerName();
                        if (!(guardianPowerName == "GP_Eikthyr"))
                        {
                            if (guardianPowerName == "GP_TheElder")
                            {
                                Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                            }
                        }
                        else
                        {
                            Player.m_localPlayer.SetGuardianPower("GP_TheElder");
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Elder Power Selected");
                        }
                    }
                    else if (bossesDefeatedCount == 3)
                    {
                        switch (Player.m_localPlayer.GetGuardianPowerName())
                        {
                            case "GP_Eikthyr":
                                Player.m_localPlayer.SetGuardianPower("GP_TheElder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Elder Power Selected");
                                break;

                            case "GP_TheElder":
                                Player.m_localPlayer.SetGuardianPower("GP_Bonemass");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Bonemass Power Selected");
                                break;

                            case "GP_Bonemass":
                                Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                                break;
                        }
                    }
                    else if (bossesDefeatedCount == 4)
                    {
                        switch (Player.m_localPlayer.GetGuardianPowerName())
                        {
                            case "GP_Eikthyr":
                                Player.m_localPlayer.SetGuardianPower("GP_TheElder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Elder Power Selected");
                                break;

                            case "GP_TheElder":
                                Player.m_localPlayer.SetGuardianPower("GP_Bonemass");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Bonemass Power Selected");
                                break;

                            case "GP_Bonemass":
                                Player.m_localPlayer.SetGuardianPower("GP_Moder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Moder Power Selected");
                                break;

                            case "GP_Moder":
                                Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                                break;
                        }
                    }
                    else if (bossesDefeatedCount == 5)
                    {
                        switch (Player.m_localPlayer.GetGuardianPowerName())
                        {
                            case "GP_Eikthyr":
                                Player.m_localPlayer.SetGuardianPower("GP_TheElder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Elder Power Selected");
                                break;

                            case "GP_TheElder":
                                Player.m_localPlayer.SetGuardianPower("GP_Bonemass");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Bonemass Power Selected");
                                break;

                            case "GP_Bonemass":
                                Player.m_localPlayer.SetGuardianPower("GP_Moder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Moder Power Selected");
                                break;

                            case "GP_Moder":
                                Player.m_localPlayer.SetGuardianPower("GP_Yagluth");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Yagluth Power Selected");
                                break;

                            case "GP_Yagluth":
                                Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                                break;
                        }
                    }
                    else
                    {
                        if (bossesDefeatedCount != 6)
                        {
                            return;
                        }

                        switch (Player.m_localPlayer.GetGuardianPowerName())
                        {
                            case "GP_Eikthyr":
                                Player.m_localPlayer.SetGuardianPower("GP_TheElder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Elder Power Selected");
                                break;

                            case "GP_TheElder":
                                Player.m_localPlayer.SetGuardianPower("GP_Bonemass");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Bonemass Power Selected");
                                break;

                            case "GP_Bonemass":
                                Player.m_localPlayer.SetGuardianPower("GP_Moder");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Moder Power Selected");
                                break;

                            case "GP_Moder":
                                Player.m_localPlayer.SetGuardianPower("GP_Yagluth");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Yagluth Power Selected");
                                break;

                            case "GP_Yagluth":
                                Player.m_localPlayer.SetGuardianPower("GP_Queen");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "The Queen Power Selected");
                                break;

                            case "GP_Queen":
                                Player.m_localPlayer.SetGuardianPower("GP_Eikthyr");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Eikthyr Power Selected");
                                break;
                        }
                    }
                }

                if (Input.GetKeyDown(ResetPowerHotkey.Value.MainKey))

                {
                    // resets the active power and gets the player ready for a new power
                    Player.m_localPlayer.m_guardianPowerCooldown = 0.1f;
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Forsaken Power Has Been Reset");

                    if (enabledReset.Value)
                    {
                        SEMan sem = Player.m_localPlayer.GetSEMan();
                        List<string> removePowers = new List<string>();
                        foreach (StatusEffect se in sem.GetStatusEffects())
                            if (se.name.StartsWith("GP_"))
                                removePowers.Add(se.name);
                        foreach (string power in removePowers)
                        {
                            sem.RemoveStatusEffect(power, true);
                            Debug.Log($"[{PluginName}] Removed {power} forsaken power.");
                        }
                    }
                }
            }

            //gets the count of the boses defeated
            private static void GetPlayerGlobalKeys()
            {
                if (!((object)Player.m_localPlayer != null))
                {
                    return;
                }
                bossesDefeatedCount = 0;

                // this is comparison of set bosses and bosses that have been beaten
                // I had to do this because some mods use the defeated tag in the DB
                // and not only Valhiem vanilla bosses can be counted using defeated
                //Monstornomicon use defeated_oceankraken which threw off my key count

                //first part gets a list of beaten bosses from the database//

                List<string> getKeysFromDB = new List<string>();
                foreach (string globalKey in ZoneSystem.instance.GetGlobalKeys())
                {
                    if (globalKey.StartsWith("defeated"))
                        getKeysFromDB.Add(globalKey);
                }
                //possible values from the database are checked against //
                // a list defined above called keyPowers //

                var output = keyPowers.Intersect(getKeysFromDB).Count();
                // the total number of matching keys are added and sent back to the above routine //
                bossesDefeatedCount = (int)output;

            }
        }
    }
}