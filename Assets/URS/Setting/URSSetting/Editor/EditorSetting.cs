using Hextant;
using Hextant.Editor;
using UnityEditor;
using UnityEngine;

/*
[Settings(SettingsUsage.EditorProject, "URSEditorProjectSettings")]
public sealed class URSEditorProjectSettings : Settings<URSEditorProjectSettings>
{
    public int integerValue => _integerValue;
    [SerializeField, Range(0, 10)] int _integerValue = 5;

    public float floatValue => _floatValue;
    [SerializeField, Range(0, 100)] float _floatValue = 25.0f;

    public string stringValue => _stringValue;
    [SerializeField, Tooltip("A string value.")] string _stringValue = "Hello";

    [SettingsProvider]
    static SettingsProvider GetSettingsProvider() =>
        instance.GetSettingsProvider();
}
*/
[Settings(SettingsUsage.EditorUser, "URSEditorUserSettings")]
public sealed class URSEditorUserSettings : Settings<URSEditorUserSettings>
{
    // 本次构建的资源版本号（资源的版本号，不是包的版本号）
    [SerializeField, Tooltip("CurrentBuildVersionCode")]
    public string BuildVersionCode = "1.0.0";

    // 本次进包的资源版本号（这个资源版本号必须有效，例如本次资源版本号或者之前打过的资源版本号）
    [SerializeField, Tooltip("CopyToStreamTargetVersion")]
    public string CopyToStreamTargetVersion = "1.0.0";


    [SerializeField, Tooltip("CurrentBuildChanel")]
    public string BuildChannel = "default_channel";


    [SerializeField, Tooltip("CurrentBuildAppId")]
    public string AppId = "AppId";


   [SerializeField, Tooltip("AppToChannelRouter")]
    public AppToChannelRouter AppToChannelRouter  = new AppToChannelRouter();
    [SettingsProvider]
    static SettingsProvider GetSettingsProvider() =>
         instance.GetSettingsProvider();
}