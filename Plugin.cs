using BepInEx;
using HarmonyLib;

namespace ForsakenPowersPlus
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ForsakenPowersPlusMod : BaseUnityPlugin
    {
        public const string Author = "Turbero";
        public const string ModGUID = Author+".ForsakenPowersPlusRemastered";
        public const string ModName = "Forsaken Powers Plus Remastered";
        public const string ModVersion = "2.0.0";
        
        private readonly Harmony harmony = new Harmony(ModGUID);

        
        void Awake()
        {
            ConfigurationFile.LoadConfig(this);

            harmony.PatchAll();
        }

        void onDestroy()
        {
            harmony.UnpatchSelf();
        }
        
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        internal class Patch_ObjectDB_CopyOtherDB
        {
            private static void Postfix(ref ObjectDB __instance)
            {
                BossesGameConfiguration.ChangePassiveMode();
            }
        }

        
    }
}