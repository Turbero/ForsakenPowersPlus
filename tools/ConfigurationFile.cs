using System.IO;
using BepInEx.Configuration;
using BepInEx;
using ServerSync;
using UnityEngine;

namespace ForsakenPowersPlus
{
    public enum Toggle
    {
        On = 1,
        Off = 0
    }
    
    internal static class ConfigurationFile
    {
        private static ConfigEntry<bool> _serverConfigLocked;
        
        public static ConfigEntry<bool> debug;
        public static ConfigEntry<Toggle> enabledReset;
        public static ConfigEntry<KeyCode> ForsakenPowerHotkey;
        public static ConfigEntry<KeyCode> ResetPowerHotkey;
        public static ConfigEntry<float> guardianBuffDuration;
        public static ConfigEntry<float> guardianBuffCooldown;
        public static ConfigEntry<Toggle> enablePassiveMode;
        public static ConfigEntry<string> messagePowerSelected;
        public static ConfigEntry<string> messagePowerReset;
        public static ConfigEntry<string> messagePowerReady;

        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        
        private static ConfigFile configFile;
        private const string ConfigFileName = ForsakenPowersPlusMod.ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private static readonly ConfigSync ConfigSync = new ConfigSync(ForsakenPowersPlusMod.ModGUID)
        {
            DisplayName = ForsakenPowersPlusMod.ModName, 
            CurrentVersion = ForsakenPowersPlusMod.ModVersion, 
            MinimumRequiredVersion = ForsakenPowersPlusMod.ModVersion
        };

        internal static void LoadConfig(BaseUnityPlugin plugin)
        {
            {
                configFile = plugin.Config;

                _serverConfigLocked = config("1 - Mod", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
                _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

                debug = config("1 - Mod", "DebugMode", false, "Enabling/Disabling the debugging in the console (default = false)", false);
                
                ForsakenPowerHotkey = config("2 - General", "ForsakenPowerHotkey", KeyCode.F8,
                    new ConfigDescription("Key Used to toggle through powers"
                , null, new  { Order = 2, DispName = "Forsaken Power Hotkey" }));
                ResetPowerHotkey = config("2 - General", "ResetPowerHotkey", KeyCode.F9,
                    new ConfigDescription("Press to reset the cooldown of your Forsaken Power and use another Power.",
                        null, new  { Order = 3, DispName = "Reset Power HotKey" }));
                enabledReset = config("2 - General", "Enable Powers Reset", Toggle.On,
                    new ConfigDescription("Enable the ability to reset the current power. If this is disabled the player may stack powers.  See the ReadMe for more information."
                        , null, new  { Order = 4, DispName = "Enable Power Reset " }));
                
                guardianBuffCooldown = config("3 - Buff Changes", "guardianBuffCoolDown",
                    1200f, new ConfigDescription("Time before the next guardian power can be used.",
                    null, new  { DispName = "Guardian Buff Cooldown (Seconds)" }));
                guardianBuffDuration = config("3 - Buff Changes", "guardianBuffDuration"
                    , 300f, new ConfigDescription("Time in seconds the guardian power lasts.",
                    null, new  { DispName = "Guardian Buff Duration (Seconds)" }));
                enablePassiveMode = config("3 - Buff Changes", "Passive Mode - Overrides BuffChange", Toggle.Off, "Set the Power to never expire - Overrides the enableBuffChange setting");
                messagePowerSelected = config("4 - Translations", "Translation - Power Selected", "Power Selected", "'Power Selected' translated message");
                messagePowerReset = config("4 - Translations", "Translation - Power Reset", "Forsaken Power Has Been Reset", "'Forsaken Power Has Been Reset' translated message");
                messagePowerReady = config("4 - Translations", "Translation - Power Ready", "Ready To Stack Another Power", "'Ready To Stack Another Power' translated message");
                
                SetupWatcher();
            }
        }
        
        private static void SetupWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private static void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Logger.Log("ReadConfigValues called");
                configFile.Reload();
                BossesGameConfiguration.ChangePassiveMode();
            }
            catch
            {
                Logger.LogError($"There was an issue loading your {ConfigFileName}");
                Logger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = configFile.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
    }
}
