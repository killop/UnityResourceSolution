using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public enum ChooseItemAction { Open, Ping }

    [FilePath("SmartLibrary/LibraryPreferences.settings", FilePathAttribute.Location.PreferencesFolder)]
    public class LibraryPreferences : ScriptableSingleton<LibraryPreferences>
    {
        private static readonly string Version = "2.0.1";
            
        [SerializeField] private string _lastSavedVersion = Version;
        [SerializeField] private TextTruncationPosition _gridTruncationPosition = TextTruncationPosition.Middle;
        [SerializeField] private ChooseItemAction _chooseItemAction = ChooseItemAction.Ping;
        [SerializeField] private bool _showPathInListView = true;
        [SerializeField] private PreviewResolution _previewResolution = PreviewResolution.x128;
        [SerializeField] private bool _showTypeIcons = true;
        [SerializeField] private float _minItemSizeDisplayTypeIcon = 70.0f;
        [SerializeField] private bool _showTextureTypeIcon = true;
        [SerializeField] private bool _showAudioTypeIcon = true;
        [SerializeField] private bool _showNamesInGridView = true;

#if HDRP_1_OR_NEWER
        [SerializeField] private bool _didShowHDRPPrompt = false;
#endif

        internal static string LastSavedVersion
        {
            get { return instance._lastSavedVersion; }
        }
        
        public static TextTruncationPosition GridTruncationPosition
        {
            get { return instance._gridTruncationPosition; }
            set 
            {
                if (value == instance._gridTruncationPosition)
                    return;

                instance._gridTruncationPosition = value;
                Modified();
            }
        }

        public static ChooseItemAction ChooseAction
        {
            get { return instance._chooseItemAction; }
            set 
            {
                if (value == instance._chooseItemAction)
                    return;
                instance._chooseItemAction = value;
                Modified();
            }
        }

        public static bool ShowPathInListView
        {
            get { return instance._showPathInListView; }
            set
            {
                if (value == instance._showPathInListView)
                    return;
                instance._showPathInListView = value;
                Modified();
            }
        }

        public static PreviewResolution PreviewResolution
        {
            get { return instance._previewResolution; }
            set
            {
                if (value == instance._previewResolution)
                    return;

                instance._previewResolution = value;
                Previewer.Resolution = value;
                Modified();
            }
        }

        public static bool ShowItemTypeIcon
        {
            get { return instance._showTypeIcons; }
            set
            {
                if (value == instance._showTypeIcons)
                    return;

                instance._showTypeIcons = value;
                Modified();
            }
        }

        public static float MinItemSizeDisplayTypeIcon
        {
            get { return instance._minItemSizeDisplayTypeIcon; }
            set
            {
                if (value == instance._minItemSizeDisplayTypeIcon)
                    return;

                instance._minItemSizeDisplayTypeIcon = value;
                Modified();
            }
        }

        public static bool ShowTextureTypeIcon
        {
            get { return instance._showTextureTypeIcon; }
            set
            {
                if (value == instance._showTextureTypeIcon)
                    return;

                instance._showTextureTypeIcon = value;
                Modified();
            }
        }
        
        public static bool ShowAudioTypeIcon
        {
            get { return instance._showAudioTypeIcon; }
            set
            {
                if (value == instance._showAudioTypeIcon)
                    return;

                instance._showAudioTypeIcon = value;
                Modified();
            }
        }

        public static bool ShowNamesInGridView
        {
            get { return instance._showNamesInGridView; }
            set
            {
                if (value == instance._showNamesInGridView)
                    return;

                instance._showNamesInGridView = value;
                Modified();
            }
        }

#if HDRP_1_OR_NEWER
        internal static bool DidShowHDRPPrompt
        {
            get { return instance._didShowHDRPPrompt; }
            set
            {
                if (value == instance._didShowHDRPPrompt)
                    return;
                
                instance._didShowHDRPPrompt = value;
                Modified();
            }
        }
#endif

        private static void Modified()
        {
            instance.Save(true);
        }

        [SettingsProvider]
        internal static SettingsProvider CreateLibraryPreferences()
        {
            var provider = new SettingsProvider("Preferences/Smart Library", SettingsScope.User)
            {
                label = "Smart Library",
                guiHandler = text =>
                {
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 251;
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);

                    var gridTruncationContent = new GUIContent("Grid Truncation Position", "The position to put the ellips when truncating text in the GridView.");
                    GridTruncationPosition = (TextTruncationPosition)EditorGUILayout.EnumPopup(gridTruncationContent, GridTruncationPosition);

                    var chooseActionContent = new GUIContent("Item Chosen Action", "The action to take when an item in the library window is chosen (Double-clicked, pressed enter/return)");
                    ChooseAction = (ChooseItemAction)EditorGUILayout.EnumPopup(chooseActionContent, ChooseAction);

                    ShowPathInListView =
                        EditorGUILayout.Toggle(new GUIContent("Show Path In List View"), ShowPathInListView);

                    ShowNamesInGridView =
                        EditorGUILayout.Toggle(new GUIContent("Show Names In Grid View"), ShowNamesInGridView);

                    GUILayout.Space(10);
                    
                    var previewResolutionContent = new GUIContent("Preview Resolution",
                                       "The resolution of the previews generated for assets in the Library. Higher resolutions increase memory usage of Unity, each size uses 4x the memory of the previous size, so X256 uses 4x the memory of X128.");
                    PreviewResolution = (PreviewResolution)EditorGUILayout.EnumPopup(previewResolutionContent, PreviewResolution);

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12);
                        if (GUILayout.Button("Force Regenerate All Previews", GUILayout.ExpandWidth(false)))
                        {
                            AssetPreviewManager.DeleteAllPreviewTextures();
                        }
                    }

                    var showItemTypeIconContent = new GUIContent("Show Item Type Icon", "Whether to show a mini icon in the corner of item preview that indicates its asset type.");
                    ShowItemTypeIcon = EditorGUILayout.Toggle(showItemTypeIconContent, ShowItemTypeIcon);

                    if (ShowItemTypeIcon)
                    {
                        EditorGUI.indentLevel++;
                        var minItemSizeDisplayTypeIconContent = new GUIContent("Min Item Size Display Type Icon", "The minimum size of an item where the type icon will display at.");
                        MinItemSizeDisplayTypeIcon = EditorGUILayout.Slider(minItemSizeDisplayTypeIconContent, MinItemSizeDisplayTypeIcon, 40, 256);

                        var showTextureTypeIconContent = new GUIContent("Show Texture Type Icon", "Whether to show the type icon for texture type items.");
                        ShowTextureTypeIcon = EditorGUILayout.Toggle(showTextureTypeIconContent, ShowTextureTypeIcon);
                        var showAudioTypeIconContent = new GUIContent("Show Audio Type Icon", "Whether to show the type icon for audio type items.");
                        ShowAudioTypeIcon = EditorGUILayout.Toggle(showAudioTypeIconContent, ShowAudioTypeIcon);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            };

            return provider;
        }
    } 
}
