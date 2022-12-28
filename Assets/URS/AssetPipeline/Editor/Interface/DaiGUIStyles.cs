using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class DaiGUIStyles
    {
        const string IconsFolderPath = "Packages/com.daihenka.assetpipeline/Editor/Resources/Icons/";
        static GUIStyle s_BoldLabel;
        static GUIStyle s_BoldLabelCentered;
        static GUIStyle s_Box;
        static GUIStyle s_Button;
        static GUIStyle s_ButtonSelected;
        static GUIStyle s_ButtonLeft;
        static GUIStyle s_ButtonLeftSelected;
        static GUIStyle s_ButtonMid;
        static GUIStyle s_ButtonMidSelected;
        static GUIStyle s_ButtonRight;
        static GUIStyle s_ButtonRightSelected;
        static GUIStyle s_IconButton;
        static GUIStyle s_Label;
        static GUIStyle s_MiniButton;
        static GUIStyle s_MiniButtonSelected;
        static GUIStyle s_MiniButtonLeft;
        static GUIStyle s_MiniButtonLeftSelected;
        static GUIStyle s_MiniButtonMid;
        static GUIStyle s_MiniButtonMidSelected;
        static GUIStyle s_MiniButtonRight;
        static GUIStyle s_MiniButtonRightSelected;
        static GUIStyle s_PaddedSettingsBlock;
        static GUIStyle s_RichTextLabel;
        static GUIStyle s_RoundedRect;
        static GUIStyle s_SectionHeader;
        static GUIStyle s_SectionHeaderCentered;
        static GUIStyle s_RenameImportProfileTextField;
        static GUIStyle s_TextArea;
        static GUIStyle s_Toolbar;
        static GUIStyle s_IgnoreCaseOn;
        static GUIStyle s_IgnoreCaseOff;
        static GUIStyle s_HorizontalSeparator;
        static GUIStyle s_VerticalSeparator;
        static GUIStyle s_ImportProfileHeader;
        static GUIStyle s_RenameImportProfileButton;
        static GUIStyle s_SectionSubHeader;
        static GUIStyle s_ImportProfilePane;
        static GUIStyle s_AddProcessorButton;
        static GUIStyle s_ButtonLarge;
        static GUIStyle s_TreeViewLabel;
        static GUIStyle s_ToolbarSearchTextField;
        static GUIStyle s_ToolbarSearchCancelButton;
        static GUIStyle s_ToolbarSearchCancelButtonEmpty;
        static GUIStyle s_ToolbarButton;

        public static GUIStyle horizontalSeparator
        {
            get
            {
                if (s_HorizontalSeparator == null)
                {
                    s_HorizontalSeparator = ColorPalette.BackgroundDarker.CreateBackground();
                    s_HorizontalSeparator.fixedHeight = 1.5f;
                    s_HorizontalSeparator.stretchWidth = true;
                }

                return s_HorizontalSeparator;
            }
        }

        public static GUIStyle verticalSeparator
        {
            get
            {
                if (s_VerticalSeparator == null)
                {
                    s_VerticalSeparator = ColorPalette.BackgroundDarker.CreateBackground();
                    s_VerticalSeparator.fixedWidth = 1.5f;
                    s_VerticalSeparator.stretchHeight = true;
                }

                return s_VerticalSeparator;
            }
        }

        public static GUIStyle boldLabel => s_BoldLabel ?? (s_BoldLabel = new GUIStyle(EditorStyles.boldLabel) {contentOffset = new Vector2(0, 0), margin = new RectOffset(0, 0, 0, 0)});
        public static GUIStyle boldLabelCentered => s_BoldLabelCentered ?? (s_BoldLabelCentered = new GUIStyle(boldLabel) {alignment = TextAnchor.MiddleCenter});
        public static GUIStyle box => s_Box ?? (s_Box = new GUIStyle(GUI.skin.box) {padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0)});
        public static GUIStyle button => s_Button ?? (s_Button = new GUIStyle("Button"));
        public static GUIStyle buttonSelected => s_ButtonSelected ?? (s_ButtonSelected = new GUIStyle(button) {normal = new GUIStyle(button).onNormal});
        public static GUIStyle buttonLeft => s_ButtonLeft ?? (s_ButtonLeft = new GUIStyle("ButtonLeft"));
        public static GUIStyle buttonLeftSelected => s_ButtonLeftSelected ?? (s_ButtonLeftSelected = new GUIStyle(buttonLeft) {normal = new GUIStyle(buttonLeft).onNormal});
        public static GUIStyle buttonMid => s_ButtonMid ?? (s_ButtonMid = new GUIStyle("ButtonMid"));
        public static GUIStyle buttonMidSelected => s_ButtonMidSelected ?? (s_ButtonMidSelected = new GUIStyle(buttonMid) {normal = new GUIStyle(buttonMid).onNormal});
        public static GUIStyle buttonRight => s_ButtonRight ?? (s_ButtonRight = new GUIStyle("ButtonRight"));
        public static GUIStyle buttonRightSelected => s_ButtonRightSelected ?? (s_ButtonRightSelected = new GUIStyle(buttonRight) {normal = new GUIStyle(buttonRight).onNormal});
        public static GUIStyle miniButton => s_MiniButton ?? (s_MiniButton = new GUIStyle(EditorStyles.miniButton));

        public static GUIStyle miniButtonSelected
        {
            get
            {
                if (s_MiniButtonSelected == null)
                {
                    s_MiniButtonSelected = new GUIStyle(miniButton) {normal = new GUIStyle(miniButton).onNormal};
                    s_MiniButtonSelected.normal.textColor = ColorPalette.HighlightedTextColor;
                }

                return s_MiniButtonSelected;
            }
        }

        public static GUIStyle miniButtonLeft => s_MiniButtonLeft ?? (s_MiniButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft));

        public static GUIStyle miniButtonLeftSelected
        {
            get
            {
                if (s_MiniButtonLeftSelected == null)
                {
                    s_MiniButtonLeftSelected = new GUIStyle(miniButtonLeft) {normal = new GUIStyle(miniButtonLeft).onNormal};
                    s_MiniButtonLeftSelected.normal.textColor = ColorPalette.HighlightedTextColor;
                }

                return s_MiniButtonLeftSelected;
            }
        }

        public static GUIStyle miniButtonMid => s_MiniButtonMid ?? (s_MiniButtonMid = new GUIStyle(EditorStyles.miniButtonMid));

        public static GUIStyle miniButtonMidSelected
        {
            get
            {
                if (s_MiniButtonMidSelected == null)
                {
                    s_MiniButtonMidSelected = new GUIStyle(miniButtonMid) {normal = new GUIStyle(miniButtonMid).onNormal};
                    s_MiniButtonMidSelected.normal.textColor = ColorPalette.HighlightedTextColor;
                }

                return s_MiniButtonMidSelected;
            }
        }

        public static GUIStyle miniButtonRight => s_MiniButtonRight ?? (s_MiniButtonRight = new GUIStyle(EditorStyles.miniButtonRight));

        public static GUIStyle miniButtonRightSelected
        {
            get
            {
                if (s_MiniButtonRightSelected == null)
                {
                    s_MiniButtonRightSelected = new GUIStyle(miniButtonRight) {normal = new GUIStyle(miniButtonRight).onNormal};
                    s_MiniButtonRightSelected.normal.textColor = ColorPalette.HighlightedTextColor;
                }

                return s_MiniButtonRightSelected;
            }
        }

        public static GUIStyle iconButton => s_IconButton ?? (s_IconButton = new GUIStyle(GUIStyle.none) {padding = new RectOffset(1, 1, 1, 1)});
        public static GUIStyle label => s_Label ?? (s_Label = new GUIStyle(EditorStyles.label) {padding = new RectOffset(0, 0, 0, 0)});
        public static GUIStyle treeViewLabel => s_TreeViewLabel ?? (s_TreeViewLabel = new GUIStyle(EditorStyles.label) {padding = new RectOffset(0, 0, 0, 0), richText = true, fontSize = 14});
        public static GUIStyle richTextLabel => s_RichTextLabel ?? (s_RichTextLabel = new GUIStyle(EditorStyles.label) {richText = true});

        public static GUIStyle importProfileHeader => s_ImportProfileHeader ?? (s_ImportProfileHeader = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 22,
            margin = new RectOffset(0, 0, 3, 0),
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            font = EditorStyles.centeredGreyMiniLabel.font,
            overflow = new RectOffset(0, 0, 0, 0)
        });

        public static GUIStyle buttonLarge => s_ButtonLarge ?? (s_ButtonLarge = new GUIStyle(button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            font = EditorStyles.centeredGreyMiniLabel.font,
            padding = new RectOffset(16, 16, 16, 16)
        });

        public static GUIStyle sectionHeader => s_SectionHeader ?? (s_SectionHeader = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 22,
            margin = new RectOffset(0, 0, 5, 0),
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            font = EditorStyles.centeredGreyMiniLabel.font,
            overflow = new RectOffset(0, 0, 0, 0)
        });

        public static GUIStyle sectionHeaderCentered => s_SectionHeaderCentered ?? (s_SectionHeaderCentered = new GUIStyle(sectionHeader) {alignment = TextAnchor.MiddleCenter});

        public static GUIStyle sectionSubHeader => s_SectionSubHeader ?? (s_SectionSubHeader = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 16,
            margin = new RectOffset(0, 0, 5, 0),
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            font = EditorStyles.centeredGreyMiniLabel.font,
            overflow = new RectOffset(0, 0, 0, 0)
        });

        public static GUIStyle importProfilePane => s_ImportProfilePane ?? (s_ImportProfilePane = new GUIStyle {padding = new RectOffset(10, 10, 10, 10), normal = new GUIStyleState {background = ColorPalette.Background.GetPixel()}});
        public static GUIStyle settingsPadding => s_PaddedSettingsBlock ?? (s_PaddedSettingsBlock = new GUIStyle {padding = new RectOffset(18, 18, 14, 4)});
        public static GUIStyle renameImportProfileTextField => s_RenameImportProfileTextField ?? (s_RenameImportProfileTextField = new GUIStyle(EditorStyles.textField) {fontSize = 22, font = EditorStyles.centeredGreyMiniLabel.font, margin = new RectOffset(6, 10, 4, 0), stretchWidth = true});
        public static GUIStyle renameImportProfileButton => s_RenameImportProfileButton ?? (s_RenameImportProfileButton = new GUIStyle(button) {margin = new RectOffset(0, 0, 7, 0), fixedWidth = 80});
        public static GUIStyle textArea => s_TextArea ?? (s_TextArea = new GUIStyle(EditorStyles.textArea));
        public static GUIStyle toolbar => s_Toolbar ?? (s_Toolbar = new GUIStyle {padding = new RectOffset(4, 3, 0, 0)});
        public static GUIStyle ignoreCaseOn = s_IgnoreCaseOn ?? (s_IgnoreCaseOn = new GUIStyle {normal = new GUIStyleState {background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconsFolderPath}IgnoreCaseOn@2x.png")}});
        public static GUIStyle ignoreCaseOff = s_IgnoreCaseOff ?? (s_IgnoreCaseOff = new GUIStyle {normal = new GUIStyleState {background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconsFolderPath}IgnoreCaseOff@2x.png")}});
        public static GUIStyle roundedRect = s_RoundedRect ?? (s_RoundedRect = new GUIStyle("sv_iconselector_labelselection") {padding = new RectOffset(15, 15, 15, 15), margin = new RectOffset(0, 0, 0, 0), stretchHeight = false});
        public static GUIStyle addProcessorButton = s_AddProcessorButton ?? (s_AddProcessorButton = new GUIStyle("RL FooterButton") {fixedHeight = 32});
        public static GUIStyle searchField = s_ToolbarSearchTextField ?? (s_ToolbarSearchTextField = new GUIStyle("ToolbarSeachTextFieldPopup"));
        public static GUIStyle searchFieldCancelButton = s_ToolbarSearchCancelButton ?? (s_ToolbarSearchCancelButton = new GUIStyle("ToolbarSeachCancelButton"));
        public static GUIStyle searchFieldCancelButtonEmpty = s_ToolbarSearchCancelButtonEmpty ?? (s_ToolbarSearchCancelButtonEmpty = new GUIStyle("ToolbarSeachCancelButtonEmpty"));
        public static GUIStyle toolbarButton = s_ToolbarButton ?? (s_ToolbarButton = new GUIStyle("ToolbarButton"));
    }
}