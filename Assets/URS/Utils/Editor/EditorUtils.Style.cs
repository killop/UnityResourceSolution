using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace NinjaBeats
{
    public partial class EditorUtils
    {
        
        // Unity Editor Built-in Icons
        // https://github.com/halak/unity-editor-icons

        public const float kPrefixPaddingRight = 2;
        public const float kLabelW = 80;
        public const float kSpacing = 5;
        public const float kSpacingSubLabel = 4;
        public const float kSliderMinW = 50;
        public const float kSliderMaxW = 100;
        public const float kSingleLineHeight = 18f;
        public const float kSingleSmallLineHeight = 16f;
        public const float kStructHeaderLineHeight = 18;
        public const float kObjectFieldThumbnailHeight = 64;
        public const float kObjectFieldMiniThumbnailHeight = 18f;
        public const float kObjectFieldMiniThumbnailWidth = 32f;
        public static string kFloatFieldFormatString = "g7";
        public static string kDoubleFieldFormatString = "g15";
        public static string kIntFieldFormatString = "#######0";
        public const float kIndentPerLevel = 15;
        public const int kControlVerticalSpacingLegacy = 2;
        public const int kDefaultSpacing = 6;
        public const int kInspTitlebarIconWidth = 16;
        public const int kInspTitlebarFoldoutIconWidth = 13;
        public const int kTabButtonHeight = 22;
        public const string kEnabledPropertyName = "m_Enabled";
        public const string k_MultiEditValueString = "<>";
        public const float kDropDownArrowMargin = 2;
        public const float kDropDownArrowWidth = 12;
        public const float kDropDownArrowHeight = 12;


        private static GUIStyle s_GS_CenteredLabel;
        public static GUIStyle GS_CenteredLabel => s_GS_CenteredLabel ??= new GUIStyle("CenteredLabel");

        private static GUIStyle s_GS_RightLabel;
        public static GUIStyle GS_RightLabel => s_GS_RightLabel ??= new GUIStyle("RightLabel");

        private static GUIStyle _GS_CenteredLabelError;

        public static GUIStyle GS_CenteredLabelError
        {
            get
            {
                if (_GS_CenteredLabelError == null)
                {
                    _GS_CenteredLabelError = new GUIStyle("CenteredLabel");
                    _GS_CenteredLabelError.normal.textColor = Color.red;
                }

                return _GS_CenteredLabelError;
            }
        }

        private static GUIStyle _GS_ControlLabel;

        public static GUIStyle GS_ControlLabel
        {
            get
            {
                if (_GS_ControlLabel == null)
                {
                    _GS_ControlLabel = new GUIStyle("ControlLabel");
                }

                return _GS_ControlLabel;
            }
        }

        static Dictionary<float, GUILayoutOption> _GUILayoutOption_MaxWidthDict =
            new Dictionary<float, GUILayoutOption>();

        public static GUILayoutOption GUILayoutOption_MaxWidth(float value)
        {
            if (!_GUILayoutOption_MaxWidthDict.TryGetValue(value, out var r))
            {
                r = GUILayout.MaxWidth(value);
                _GUILayoutOption_MaxWidthDict.Add(value, r);
            }

            return r;
        }

        static Dictionary<float, GUILayoutOption> _GUILayoutOption_MaxHeightDict =
            new Dictionary<float, GUILayoutOption>();

        public static GUILayoutOption GUILayoutOption_MaxHeight(float value)
        {
            if (!_GUILayoutOption_MaxHeightDict.TryGetValue(value, out var r))
            {
                r = GUILayout.MaxHeight(value);
                _GUILayoutOption_MaxHeightDict.Add(value, r);
            }

            return r;
        }

        static Dictionary<float, GUILayoutOption> _GUILayoutOption_WidthDict = new Dictionary<float, GUILayoutOption>();

        public static GUILayoutOption GUILayoutOption_Width(float value)
        {
            if (!_GUILayoutOption_WidthDict.TryGetValue(value, out var r))
            {
                r = GUILayout.Width(value);
                _GUILayoutOption_WidthDict.Add(value, r);
            }

            return r;
        }

        static Dictionary<float, GUILayoutOption>
            _GUILayoutOption_HeightDict = new Dictionary<float, GUILayoutOption>();

        public static GUILayoutOption GUILayoutOption_Height(float value)
        {
            if (!_GUILayoutOption_HeightDict.TryGetValue(value, out var r))
            {
                r = GUILayout.Height(value);
                _GUILayoutOption_HeightDict.Add(value, r);
            }

            return r;
        }


        static GUIStyle _AnimationEventTooltip;

        static GUIStyle AnimationEventTooltip
        {
            get
            {
                if (_AnimationEventTooltip == null)
                {
                    _AnimationEventTooltip = nameof(AnimationEventTooltip);
                    _AnimationEventTooltip.contentOffset = new Vector2(0, 0);
                    _AnimationEventTooltip.overflow = new RectOffset(0, 0, 0, 0);
                    _AnimationEventTooltip.clipping = TextClipping.Clip;
                    _AnimationEventTooltip.alignment = TextAnchor.MiddleLeft;
                }

                return _AnimationEventTooltip;
            }
        }

        static GUIStyle _AnimationEventTooltipArrow;

        static GUIStyle AnimationEventTooltipArrow =>
            _AnimationEventTooltipArrow ??= (GUIStyle)(nameof(AnimationEventTooltipArrow));


        static Texture2D _LuaIcon = null;
        static bool _LuaIconInit = false;

        public static Texture2D LuaIcon
        {
            get
            {
                if (!_LuaIconInit)
                {
                    _LuaIconInit = true;
                    _LuaIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/LuaScriptEditor/lua@2x.png");
                }

                return _LuaIcon;
            }
        }

        static Texture2D _LuaModuleIcon = null;
        static bool _LuaModuleIconInit = false;

        public static Texture2D LuaModuleIcon
        {
            get
            {
                if (!_LuaModuleIconInit)
                {
                    _LuaModuleIconInit = true;
                    _LuaModuleIcon =
                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/LuaScriptEditor/module@2x.png");
                }

                return _LuaModuleIcon;
            }
        }

        private static GUIStyle _s_GS_TextFieldImageLeft;

        public static GUIStyle s_GS_TextFieldImageLeft => _s_GS_TextFieldImageLeft ??= new GUIStyle("TextField")
        {
            imagePosition = ImagePosition.ImageLeft
        };

        private static GUIStyle _s_GS_TextFieldTextOnly;

        public static GUIStyle s_GS_TextFieldTextOnly => _s_GS_TextFieldTextOnly ??= new GUIStyle("TextField")
        {
            imagePosition = ImagePosition.TextOnly
        };
        
        
    }
}