
using System.Linq;
using System.Reflection;
using UnityEngine;
using NinjaBeats;

public partial class UnityMacro
{
    private static UnityMacro s_Instance = null;

    public static UnityMacro Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new();
                var type = s_Instance.GetType();
                var fields = type.GetFields((BindingFlags)(-1));
                var injectFields = fields.Where(x => x.Name.StartsWith("inject_"));
                foreach (var injectField in injectFields)
                {
                    var fieldName = injectField.Name.Substring("inject_".Length);
                    var field = fields.Find(x => x.Name == fieldName);
                    if (field == null)
                        continue;

                    if (field.FieldType != injectField.FieldType)
                    {
                        Debug.LogError(
                            $"UnityMacro Inject [{field.Name}] error, injectFieldType:{injectField.FieldType} fieldType:{field.FieldType}");
                        continue;
                    }

                    var value = injectField.GetValue(s_Instance);
                    field.SetValue(s_Instance, value);

#if UNITY_EDITOR
                    Debug.LogWarning($"UnityMacro Inject [{field.Name}], value:{value}");
#endif
                }
            }

            return s_Instance;
        }
    }

    public string BUILD_CHANNEL = "";
    public string SERVER_URL = "http://ninja.happyelements.net:8020";
    public string DEFAULT_LANGUAGE = "";
    public bool ENABLE_CONSOLE = true;
    public int  MAINTENANCE_VERSION_COUNT= 4;
    public bool ENABLE_HOTUPDATE = true;
    public bool ENABLE_DEVELOPMENT_BUILD = false;
    public bool ENABLE_HESDK = true;

}