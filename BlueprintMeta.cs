using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SFSBlueprintOrganizer
{

    internal static class BlueprintMeta
    {
        public class Entry
        {
            public List<string> Tags = new List<string>();
            public string Folder = "";
        }

        private static readonly Dictionary<string, Entry> Data = new Dictionary<string, Entry>();
        private static bool _loaded;
        private static string _filePath;

        private static string FilePath
        {
            get
            {
                if (_filePath == null)
                {
                    string folder = Main.Instance != null ? Main.Instance.ModFolder : null;
                    if (string.IsNullOrEmpty(folder)) folder = Application.persistentDataPath;
                    _filePath = Path.Combine(folder, "blueprint_meta.json");
                }
                return _filePath;
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            Load();
        }

        private static void Load()
        {
            Data.Clear();
            try
            {
                if (!File.Exists(FilePath)) return;
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                if (MiniJson.Parse(json) is Dictionary<string, object> root)
                {
                    foreach (var kv in root)
                    {
                        if (!(kv.Value is Dictionary<string, object> entryObj)) continue;
                        var entry = new Entry();

                        if (entryObj.TryGetValue("tags", out object tagsObj) && tagsObj is List<object> tagsList)
                        {
                            foreach (var t in tagsList)
                            {
                                string tag = (t as string)?.Trim();
                                if (!string.IsNullOrEmpty(tag)) entry.Tags.Add(tag);
                            }
                        }

                        if (entryObj.TryGetValue("folder", out object folderObj) && folderObj is string folderStr)
                        {
                            entry.Folder = folderStr;
                        }

                        Data[kv.Key] = entry;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SFSBlueprintOrganizer] Could not read blueprint_meta.json: " + e.Message);
            }
        }

        public static void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append('{');
                bool firstEntry = true;
                foreach (var kv in Data)
                {

                    if (kv.Value.Tags.Count == 0 && string.IsNullOrEmpty(kv.Value.Folder)) continue;

                    if (!firstEntry) sb.Append(',');
                    firstEntry = false;

                    sb.Append(MiniJson.WriteString(kv.Key));
                    sb.Append(':');
                    sb.Append('{');
                    sb.Append("\"tags\":").Append(MiniJson.WriteStringArray(kv.Value.Tags));
                    sb.Append(",\"folder\":").Append(MiniJson.WriteString(kv.Value.Folder ?? ""));
                    sb.Append('}');
                }
                sb.Append('}');

                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SFSBlueprintOrganizer] Could not write blueprint_meta.json: " + e.Message);
            }
        }

        private static Entry GetOrCreate(string blueprintName)
        {
            EnsureLoaded();
            if (!Data.TryGetValue(blueprintName, out Entry entry))
            {
                entry = new Entry();
                Data[blueprintName] = entry;
            }
            return entry;
        }

        public static List<string> GetTags(string blueprintName)
        {
            EnsureLoaded();
            return Data.TryGetValue(blueprintName, out Entry entry) ? entry.Tags : new List<string>();
        }

        public static string GetFolder(string blueprintName)
        {
            EnsureLoaded();
            return Data.TryGetValue(blueprintName, out Entry entry) ? entry.Folder : "";
        }

        public static void SetTags(string blueprintName, IEnumerable<string> tags)
        {
            var entry = GetOrCreate(blueprintName);
            entry.Tags = tags
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Save();
        }

        public static void SetFolder(string blueprintName, string folder)
        {
            var entry = GetOrCreate(blueprintName);
            entry.Folder = folder?.Trim() ?? "";
            Save();
        }

        public static void RenameKey(string oldName, string newName)
        {
            EnsureLoaded();
            if (oldName == newName) return;
            if (Data.TryGetValue(oldName, out Entry entry))
            {
                Data.Remove(oldName);
                Data[newName] = entry;
                Save();
            }
        }

        public static void RemoveKey(string blueprintName)
        {
            EnsureLoaded();
            if (Data.Remove(blueprintName)) Save();
        }

        public static List<string> AllTags()
        {
            EnsureLoaded();
            return Data.Values
                .SelectMany(e => e.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> AllFolders()
        {
            EnsureLoaded();
            return Data.Values
                .Select(e => e.Folder)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
