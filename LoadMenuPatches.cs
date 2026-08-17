using System;
using HarmonyLib;
using SFS.Builds;
using SFS.UI;

namespace SFSBlueprintOrganizer
{

    [HarmonyPatch(typeof(LoadMenu), "OnOpen")]
    internal static class LoadMenu_OnOpen_Patch
    {
        private static void Postfix(LoadMenu __instance)
        {
            bool isBlueprintMenu = __instance.implementation is Blueprint_Saving;
            BlueprintOrganizerUI.Instance?.SetMenu(isBlueprintMenu ? __instance : null);
        }
    }

    [HarmonyPatch(typeof(LoadMenu), "OnClose")]
    internal static class LoadMenu_OnClose_Patch
    {
        private static void Postfix(LoadMenu __instance)
        {
            if (BlueprintOrganizerUI.Instance != null && BlueprintOrganizerUI.Instance.CurrentMenu == __instance)
                BlueprintOrganizerUI.Instance.SetMenu(null);
        }
    }

    [HarmonyPatch(typeof(LoadMenu), "ReloadElements")]
    internal static class LoadMenu_ReloadElements_Patch
    {
        private static void Postfix(LoadMenu __instance)
        {
            if (BlueprintOrganizerUI.Instance != null && BlueprintOrganizerUI.Instance.CurrentMenu == __instance)
                BlueprintOrganizerUI.Instance.ApplyFilter();
        }
    }

    [HarmonyPatch(typeof(Blueprint_Saving), "SFS.UI.I_SavingBase.Rename", new[] { typeof(string), typeof(string) })]
    internal static class Blueprint_Rename_Patch
    {
        private static void Postfix(string oldName, string newName)
        {
            BlueprintMeta.RenameKey(oldName, newName);
        }
    }

    [HarmonyPatch(typeof(Blueprint_Saving), "SFS.UI.I_SavingBase.Delete", new[] { typeof(string) })]
    internal static class Blueprint_Delete_Patch
    {
        private static void Postfix(string name)
        {
            BlueprintMeta.RemoveKey(name);
        }
    }
}
