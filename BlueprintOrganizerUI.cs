using System;
using System.Collections.Generic;
using System.Linq;
using SFS.UI;
using UnityEngine;

namespace SFSBlueprintOrganizer
{

    public class BlueprintOrganizerUI : MonoBehaviour
    {
        public static BlueprintOrganizerUI Instance;

        public LoadMenu CurrentMenu { get; private set; }

        private string _search = "";
        private readonly HashSet<string> _selectedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string _selectedFolder;

        private string _tagInputBuffer = "";
        private string _folderInputBuffer = "";
        private string _tagEditTarget;

        private Font _nativeFont;
        private bool _triedNativeFont;

        private const float MinPanelHeight = 90f;
        private float _measuredContentHeight = 200f;

        private void Awake()
        {
            Instance = this;
        }

        public void SetMenu(LoadMenu menu)
        {
            CurrentMenu = menu;
            _tagEditTarget = null;

            if (menu != null)
            {
                if (!_triedNativeFont)
                {
                    _triedNativeFont = true;
                    _nativeFont = OrganizerSkin.TryGetNativeFont(menu);
                }
                ApplyFilter();
            }
        }

        public void ApplyFilter()
        {
            if (CurrentMenu == null) return;

            var elements = SafeAccess.Get<List<LoadMenuElement>>(CurrentMenu, "elements");
            if (elements == null) return;

            foreach (var el in elements)
            {
                if (el == null) continue;
                string name = el.text != null ? el.text.Text : null;
                bool show = Matches(name);
                if (el.gameObject.activeSelf != show) el.gameObject.SetActive(show);
            }
        }

        private bool Matches(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;

            if (!string.IsNullOrEmpty(_search) && name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (_selectedTags.Count > 0 || _selectedNoTag)
            {
                var tags = BlueprintMeta.GetTags(name);
                bool matchesAnyTag = tags.Any(t => _selectedTags.Contains(t));
                bool matchesNoTag = _selectedNoTag && tags.Count == 0;
                if (!matchesAnyTag && !matchesNoTag)
                    return false;
            }

            if (_selectedFolder != null)
            {
                string folder = BlueprintMeta.GetFolder(name);
                if (_selectedFolder.Length == 0)
                {
                    if (!string.IsNullOrEmpty(folder)) return false;
                }
                else if (!string.Equals(folder, _selectedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private string GetNativeSelectedName()
        {
            if (CurrentMenu == null) return null;
            var elements = SafeAccess.Get<List<LoadMenuElement>>(CurrentMenu, "elements");
            if (elements == null) return null;

            foreach (var el in elements)
            {
                if (el == null || el.radioButton == null) continue;

                if (SafeAccess.Get<bool>(el.radioButton, "IsSelected"))
                    return el.text != null ? el.text.Text : null;
            }
            return null;
        }

        private Vector2 _manualOffset = Vector2.zero;
        private bool _dragging;
        private Vector2 _dragStartMouseScreen;
        private Vector2 _dragStartOffset;

        private Rect GetPanelRect()
        {

            float width = Mathf.Min(590f, Screen.width - 40f);
            float centerX = Screen.width / 2f;
            float y = Screen.height - 40f;

            RectTransform holder = CurrentMenu != null ? SafeAccess.Get<RectTransform>(CurrentMenu, "holder") : null;
            if (holder != null)
            {
                Vector3[] corners = new Vector3[4];
                holder.GetWorldCorners(corners);

                Canvas canvas = holder.GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

                float menuBottom = Screen.height - bottomLeft.y;

                centerX = (bottomLeft.x + topRight.x) / 2f;
                y = menuBottom + 4f;
            }

            float x = Mathf.Clamp(centerX - width / 2f, 10f, Mathf.Max(10f, Screen.width - width - 10f));

            float roomBelow = Screen.height - y - 20f;
            float height;

            if (roomBelow < MinPanelHeight + 10f)
            {

                height = Mathf.Min(Mathf.Max(_measuredContentHeight, MinPanelHeight), 220f);
                y = Screen.height - height - 20f;
            }
            else
            {
                height = Mathf.Clamp(_measuredContentHeight, MinPanelHeight, roomBelow);
            }

            x = Mathf.Clamp(x + _manualOffset.x, -width + 60f, Screen.width - 60f);
            y = Mathf.Clamp(y + _manualOffset.y, -height + 40f, Screen.height - 40f);

            return new Rect(x, y, width, height);
        }

        private void HandleDrag(Rect titleRect)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (titleRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        _dragging = true;
                        _dragStartMouseScreen = Input.mousePosition;
                        _dragStartOffset = _manualOffset;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && _dragging)
                    {
                        Vector2 deltaScreen = (Vector2)Input.mousePosition - _dragStartMouseScreen;

                        _manualOffset = _dragStartOffset + new Vector2(deltaScreen.x, -deltaScreen.y);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        _dragging = false;
                        e.Use();
                    }
                    break;
            }
        }

        private float _panelWidth;

        private void OnGUI()
        {
            if (CurrentMenu == null) return;
            OrganizerSkin.EnsureInit(_nativeFont);

            Rect panelRect = GetPanelRect();
            _panelWidth = panelRect.width;

            GUILayout.BeginArea(panelRect, OrganizerSkin.Panel);
            GUILayout.BeginVertical();

            GUILayout.Label("Blueprint Organizer", OrganizerSkin.Title);
            HandleDrag(GUILayoutUtility.GetLastRect());
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(_panelWidth * 0.48f));
            DrawTagChips();
            GUILayout.Space(4);
            DrawFolderChips();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical();
            DrawSelectedBlueprintEditor();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            DrawSearchBar();

            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {

                _measuredContentHeight = GUILayoutUtility.GetLastRect().yMax + OrganizerSkin.Panel.padding.vertical + 6f;
            }

            GUILayout.EndArea();
        }

        private void DrawSearchBar()
        {
            GUILayout.Label("Search", OrganizerSkin.Header);
            GUILayout.BeginHorizontal();
            string newSearch = GUILayout.TextField(_search, OrganizerSkin.TextField, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            if (newSearch != _search)
            {
                _search = newSearch;
                ApplyFilter();
            }
            if (GUILayout.Button("X", OrganizerSkin.Chip, GUILayout.Width(22), GUILayout.Height(20)))
            {
                _search = "";
                ApplyFilter();
            }
            GUILayout.EndHorizontal();
        }

        private bool _selectedNoTag;

        private void DrawTagChips()
        {
            var tags = BlueprintMeta.AllTags();
            GUILayout.Label("Tags", OrganizerSkin.Header);

            var options = new List<string> { "No Tag" };
            options.AddRange(tags);

            DrawWrappedChips(options, label =>
            {
                if (label == "No Tag") return _selectedNoTag;
                return _selectedTags.Contains(label);
            }, label =>
            {
                if (label == "No Tag") _selectedNoTag = !_selectedNoTag;
                else if (!_selectedTags.Add(label)) _selectedTags.Remove(label);
                ApplyFilter();
            });
        }

        private void DrawFolderChips()
        {
            var folders = BlueprintMeta.AllFolders();
            if (folders.Count == 0) return;

            GUILayout.Label("Folders", OrganizerSkin.Header);

            var options = new List<string> { "All", "No folder" };
            options.AddRange(folders);

            DrawWrappedChips(options, label =>
            {
                if (label == "All") return _selectedFolder == null;
                if (label == "No folder") return _selectedFolder == "";
                return _selectedFolder == label;
            }, label =>
            {
                if (label == "All") _selectedFolder = null;
                else if (label == "No folder") _selectedFolder = "";
                else _selectedFolder = label;
                ApplyFilter();
            });
        }

        private void DrawWrappedChips(List<string> items, Func<string, bool> isSelected, Action<string> onClick)
        {
            float maxWidth = Mathf.Max(160f, _panelWidth * 0.48f);
            float lineWidth = 0f;

            GUILayout.BeginHorizontal();
            foreach (var item in items)
            {
                float w = OrganizerSkin.Chip.CalcSize(new GUIContent(item)).x + 4f;
                if (lineWidth + w > maxWidth)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    lineWidth = 0f;
                }
                lineWidth += w;

                bool selected = isSelected(item);
                if (GUILayout.Button(item, selected ? OrganizerSkin.ChipSelected : OrganizerSkin.Chip, GUILayout.Width(w)))
                {
                    onClick(item);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSelectedBlueprintEditor()
        {
            string selectedName = GetNativeSelectedName();

            if (string.IsNullOrEmpty(selectedName))
            {
                GUILayout.Label("Select a blueprint above to edit its tags and folder.", OrganizerSkin.Hint);
                _tagEditTarget = null;
                return;
            }

            if (_tagEditTarget != selectedName)
            {
                _tagEditTarget = selectedName;
                _tagInputBuffer = string.Join(", ", BlueprintMeta.GetTags(selectedName));
                _folderInputBuffer = BlueprintMeta.GetFolder(selectedName);
            }

            GUILayout.Label("Editing: " + selectedName, OrganizerSkin.Header);

            GUILayout.Label("Tags (comma separated)", OrganizerSkin.Label);
            _tagInputBuffer = GUILayout.TextField(_tagInputBuffer, OrganizerSkin.TextField);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Folder (optional)", OrganizerSkin.Label);
            _folderInputBuffer = GUILayout.TextField(_folderInputBuffer, OrganizerSkin.TextField);
            GUILayout.EndVertical();

            GUILayout.Space(6);

            GUILayout.BeginVertical(GUILayout.Width(54));
            GUILayout.Space(27);
            if (GUILayout.Button("Save", OrganizerSkin.Button, GUILayout.Height(20)))
            {
                var tags = _tagInputBuffer
                    .Split(',')
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                BlueprintMeta.SetTags(selectedName, tags);
                BlueprintMeta.SetFolder(selectedName, _folderInputBuffer.Trim());
                ApplyFilter();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
    }
}
