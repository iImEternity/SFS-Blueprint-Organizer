# Blueprint Organizer - README
Mod for Spaceflight Simulator that adds search, tags and virtual folders to the load blueprints menu. Written in C# using Harmony to patch the game.

-----------------------------------------------

- MainMod.cs
  - Main class of the mod. Here the ID, name, author and version of the mod are defined. In Early_Load() the Harmony instance is created and PatchAll() is called to apply all the patches in the project, and the GameObject that holds the UI is created. If you need to change the mod's metadata (name, version, dependencies) or the initialization order, this is the place.

- BlueprintMeta.cs
  - Static class that works as the "data model". Holds a Dictionary<string, Entry> where each Entry has a list of Tags and a Folder. It handles serializing/deserializing this to disk using MiniJson, and computes the file path based on ModFolder or persistentDataPath if there is no mod folder. If you're going to add more fields per blueprint (for example, favorites or notes), this is where the Entry class and the save/load logic are defined.

- BlueprintOrganizerUI.cs
  - MonoBehaviour that draws the whole interface with OnGUI (Unity's IMGUI). Keeps the UI state: search text (_search), selected tags (_selectedTags), selected folder (_selectedFolder), text input buffers, etc. It hooks into the game's LoadMenu through SetMenu(). It's the biggest file and the one that will be touched the most if you want to change the button layout, add new filters, or change the UI arrangement.

- LoadMenuPatches.cs
  - This is where the Harmony patches ([HarmonyPatch]) on the game's LoadMenu class live. There are patches on OnOpen, OnClose and ReloadElements to detect when the blueprints menu opens/closes and when the list elements are reloaded, in order to show/hide/update the mod's UI at the right time. If the game updates its version and these methods change name or signature, the patches here need to be updated.

- OrganizerSkin.cs
  - Defines the GUIStyle and Color used by the interface (panels, buttons, tag chips, text). Everything centralized here so it's easy to change the color palette or the look of the controls without having to search through BlueprintOrganizerUI.cs.

- SafeAccess.cs
  - Reflection utility. GetRaw() looks up a field or property by name in an object (including private ones, going up the class hierarchy with BaseType), and Get<T>() does the same but with safe casting and a default value if it fails. It's used to read internal game data that isn't public. If the game changes the names of internal variables, the strings passed to these methods need to be adjusted wherever they're used.

- MiniJson.cs
  - Hand-written JSON parser and serializer, with no external library dependencies (to avoid conflicts with other mods or with the game's runtime). Supports objects, arrays, strings, numbers, booleans and null. The only thing that uses this file is BlueprintMeta.cs, to save/read the tags and folders file. It doesn't need to be touched unless support for another data type in the JSON is needed.

-----------------------------------------------