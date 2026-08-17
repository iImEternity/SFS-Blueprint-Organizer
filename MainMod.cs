using System.Collections.Generic;
using HarmonyLib;
using ModLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSBlueprintOrganizer
{
    public class Main : Mod
    {
        public static Main Instance;

        public override string ModNameID => "SFSBlueprintOrganizer";
        public override string DisplayName => "Blueprint Search & Tags";
        public override string Author => "Eternity";
        public override string MinimumGameVersionNecessary => "1.6.0";
        public override string ModVersion => "1.0.0";
        public override string Description => "Search box, tags and virtual folders for the Load Blueprint menu.";

        public override Dictionary<string, string> Dependencies => _dependencies;
        private readonly Dictionary<string, string> _dependencies = new Dictionary<string, string>();

        private GameObject _uiObject;

        public override void Early_Load()
        {
            Instance = this;

            Harmony harmony = new Harmony(ModNameID);
            harmony.PatchAll();
        }

        public override void Load()
        {
            Debug.Log("[SFSBlueprintOrganizer] Loaded. Open the Load Blueprint menu to see search & tags.");

            BlueprintMeta.EnsureLoaded();

            SpawnUi();
            SceneManager.sceneLoaded += (scene, mode) => SpawnUi();
        }

        private void SpawnUi()
        {
            if (_uiObject != null) return;

            _uiObject = new GameObject("SFSBlueprintOrganizerUI");
            _uiObject.AddComponent<BlueprintOrganizerUI>();
            Object.DontDestroyOnLoad(_uiObject);
        }
    }
}
