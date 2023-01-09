using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//Original Aedenthorn Utilities has been renamed to TastyUtils as I add my own custom items in the future
//Idea, code and credit go back to the original author
namespace AutomaticFuel
{
    public class TastyUtils
    {
        public static bool IgnoreKeyPresses(bool extra = false)
        {
            if (!extra)
                return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() ||
                    ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true;
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() ||
                ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() ||
                TextViewer.instance?.IsVisible() == true;
        }

        public static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckKeyHeld(string value, bool req = true)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return !req;
            }
        }

        public static string GetPrefabName(string name)
        {
            char[] anyOf = new char[] { '(', ' ' };
            int num = name.IndexOfAny(anyOf);
            string result;
            if (num >= 0)
                result = name.Substring(0, num);
            else
                result = name;
            return result;
        }


        public static void AddContainer(Container container, ZNetView nview)
        {
            try
            {
                AutomaticFuelMain.ServerSyncModTemplateLogger.LogDebug(
                    $"Checking {container.name} {nview != null} {nview?.GetZDO() != null} {nview?.GetZDO()?.GetLong("creator".GetStableHashCode())}");
                if (container.GetInventory() == null || nview?.GetZDO() == null ||
                    (!container.name.StartsWith("piece_", StringComparison.Ordinal) &&
                     !container.name.StartsWith("Container", StringComparison.Ordinal) &&
                     nview.GetZDO().GetLong("creator".GetStableHashCode()) == 0)) return;
                AutomaticFuelMain.ServerSyncModTemplateLogger.LogDebug($"Adding {container.name}");
                AutomaticFuelMain.ContainerList.Add(container);
            }
            catch
            {
                // ignored
            }
        }


        public static List<Container> GetNearbyContainers(Vector3 center, float range)
        {
            List<Container> containers = new();
            foreach (Container container in AutomaticFuelMain.ContainerList.Where(container => container != null &&
                         container.GetComponentInParent<Piece>() != null && Player.m_localPlayer != null &&
                         container?.transform != null && container.GetInventory() != null && (range <= 0 ||
                             Vector3.Distance(center, container.transform.position) <
                             AutomaticFuelMain.mRange.Value) && container.CheckAccess(Player.m_localPlayer.GetPlayerID()) &&
                         !container.IsInUse()))
            {
                var containerPos = container.transform.position;
                if (!PrivateArea.CheckAccess(containerPos, 0f, false))
                    continue;
                if (!PrivateArea.InsideFactionArea(container.transform.position, Character.Faction.Players))
                {
                    container.Load();
                    containers.Add(container);
                    continue;
                }
                else
                {
                    container.Load();
                containers.Add(container);
                 }
            }
            return containers;
        }
    }
}