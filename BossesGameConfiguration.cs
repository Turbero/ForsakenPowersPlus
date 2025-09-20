namespace ForsakenPowersPlus
{

    public class BossesGameConfiguration
    {
        // used to define the bosses to activate all power in passive mode //
        public static void ChangePassiveMode()
        {
            if (ConfigurationFile.enablePassiveMode.Value == ConfigurationFile.Toggle.Off)
            {
                //if not passive mode then allow the user to cancel the power
                foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                {
                    if (se.name.StartsWith("GP_"))
                    {
                        se.m_ttl = ConfigurationFile.guardianBuffDuration.Value;
                        Logger.LogInfo(
                            $"Made {se.name} Buff Duration set to {ConfigurationFile.guardianBuffDuration.Value}");
                        se.m_cooldown = ConfigurationFile.guardianBuffCooldown.Value;
                        Logger.LogInfo(
                            $"Made {se.name} Buff Cooldown set to {ConfigurationFile.guardianBuffCooldown.Value}");
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
                        Logger.LogInfo($"Made {se.name} status effect passive.");
                    }
                }
            }
        }
    }
}