using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Utilities.USerialize
{
    // Uses reflection to recursively produce a text based dump of the types, names and values of all fields in an object and it's sub-objects.
    // Useful for visual verification or comparison of the data
    internal class DumpToText
    {
        // Custom code to dump a type to text that cannot be properly handled by the default reflection based code
        internal interface ICustomDumper
        {
            // Return the type that this custom dumper deals with
            Type GetType();

            // Dump an object to text
            void CustomDumper(DumpToText dumpToText, object value);
        }


        Dictionary<Type, ICustomDumper> m_CustomDumpers = new Dictionary<Type, ICustomDumper>();

        StringBuilder m_StringBuilder = new StringBuilder();
        string m_Indent = "";

        internal DumpToText()
        {
        }

        internal DumpToText(params ICustomDumper[] customDumpers)
        {
            if (customDumpers != null)
                Array.ForEach(customDumpers, (customDumper) => AddCustomDumper(customDumper));
        }

        internal void Clear()
        {
            m_StringBuilder = new StringBuilder();
            m_Indent = "";
        }

        internal void AddCustomDumper(ICustomDumper customDumper)
        {
            m_CustomDumpers.Add(customDumper.GetType(), customDumper);
        }

        internal string SanitiseFieldName(string fieldName)
        {
            if ((fieldName[0] == '<') && fieldName.EndsWith(">k__BackingField"))
                fieldName = fieldName.Substring(1, fieldName.Length - 17);
            return fieldName;
        }

        internal static string SanitiseGenericName(string genericName)
        {
            int argSepPos = genericName.IndexOf('`');
            return (argSepPos != -1) ? genericName.Substring(0, argSepPos) : genericName;
        }

        internal void Add(string label)
        {
            label = SanitiseFieldName(label);

            m_StringBuilder.AppendLine($"{m_Indent}{label}");
        }

        internal void Add(string label, string value)
        {
            label = SanitiseFieldName(label);

            m_StringBuilder.AppendLine($"{m_Indent}{label}: {value}");
        }

        internal void Add(string typename, string label, string value)
        {
            label = SanitiseFieldName(label);

            m_StringBuilder.AppendLine($"{m_Indent}{typename} {label} = {value}");
        }

        internal void Indent()
        {
            m_Indent = m_Indent + " |  ";
        }

        internal void Undent()
        {
            m_Indent = m_Indent.Substring(0, m_Indent.Length - 4);
        }

        // Generate a one line string describing a type.  For normal types it's just the type name, for generic type it has the generic arguments included
        internal static string DescribeType(Type typeToDescribe)
        {
            if (!typeToDescribe.IsGenericType)
                return typeToDescribe.Name;

            Type[] genericArgs = typeToDescribe.GetGenericArguments();

            string argString = $"{SanitiseGenericName(typeToDescribe.Name)}<";
            if (genericArgs.Length > 0)
            {
                if (genericArgs[0].IsGenericType)
                    argString += DescribeType(genericArgs[0]);
                else
                    argString += genericArgs[0].Name;
                for (int argNum = 1; argNum < genericArgs.Length; argNum++)
                {
                    if (genericArgs[argNum].IsGenericType)
                        argString += ", " + DescribeType(genericArgs[argNum]);
                    else
                        argString += ", " + genericArgs[argNum].Name;
                }
            }
            return argString + ">";
        }

        internal StringBuilder Dump(string label, object thingToDump)
        {
            if (thingToDump == null)
            {
                Add(label, "<null>");
                return m_StringBuilder;
            }

            Type thingType = thingToDump.GetType();
            if ((thingToDump != null) && (m_CustomDumpers.TryGetValue(thingType, out ICustomDumper customDumper)))
            {
                Add(label);
                customDumper.CustomDumper(this, thingToDump);
                return m_StringBuilder;
            }

            Add(label);

            if (m_Indent.Length > 256)
            {
                m_StringBuilder.AppendLine("*** Indent depth exceeded, dump aborted ***");
                return m_StringBuilder;
            }

            if (m_StringBuilder.Length > 32 * 1024 * 1024)
            {
                m_StringBuilder.AppendLine("*** StringBuilder length exceeded, dump aborted ***");
                return m_StringBuilder;
            }

            Indent();

            foreach (FieldInfo field in thingType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(Array).IsAssignableFrom(field.FieldType))
                {
                    // An array
                    Array array = field.GetValue(thingToDump) as Array;
                    if (array != null)
                    {
                        if (array.Rank != 1)
                            throw new InvalidDataException($"Arrays of ranks other than 1 are not currently supported - array '{field.Name}' is rank {array.Rank})");
                        Type elementType = field.FieldType.GetElementType();
                        Add(field.FieldType.Name, field.Name, $"{DescribeType(elementType)}[{array.Length}]");
                        string name = SanitiseFieldName(field.Name);
                        if (elementType == typeof(string))
                        {
                            Indent();
                            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                            {
                                Add($"{name}[{elementIndex}]", "\"" + ((string)array.GetValue(elementIndex)) + "\"");
                            }
                            Undent();
                        }
                        else if (elementType == typeof(byte))
                            DumpPrimitiveArray<byte>(name, array);
                        else if (elementType == typeof(Type))
                            DumpSimpleObjectArray<Type>(name, array);
                        else
                        {
                            // General array
                            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
                            {
                                Indent();
                                object element = array.GetValue(elementIndex);
                                Dump($"{name}[{elementIndex}] ({DescribeType(element.GetType())})", element);
                                Undent();
                            }
                        }
                    }
                    else
                        Add(field.FieldType.Name, field.Name, "null (array)");
                }
                else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    // A List<>
                    System.Collections.IList list = field.GetValue(thingToDump) as System.Collections.IList;
                    if (list != null)
                    {
                        string typeString = DescribeType(field.FieldType);
                        Add(typeString, field.Name, $"({list.Count})");
                        string name = SanitiseFieldName(field.Name);
                        for (int elementIndex = 0; elementIndex < list.Count; elementIndex++)
                        {
                            Indent();
                            object element = list[elementIndex];
                            Dump($"{name}[{elementIndex}] ({DescribeType(element.GetType())})", element);
                            Undent();
                        }
                    }
                    else
                        Add(field.FieldType.Name, field.Name, "null (List<>)");
                }
                else if (field.FieldType.IsEnum)
                {
                    Add("enum " + field.FieldType.Name, SanitiseFieldName(field.Name), Enum.GetName(field.FieldType, field.GetValue(thingToDump)));
                }
                else if (field.FieldType == typeof(String))
                {
                    object fieldValue = field.GetValue(thingToDump);
                    if (fieldValue != null)
                        Add(field.FieldType.Name, SanitiseFieldName(field.Name), "\"" + ((string)fieldValue) + "\"");
                    else
                        Add(field.FieldType.Name, SanitiseFieldName(field.Name), "<null>");
                }
                else if (field.FieldType == typeof(RuntimeTypeHandle))
                {
                    Add("RuntimeTypeHandle", field.Name, Type.GetTypeFromHandle((RuntimeTypeHandle)field.GetValue(thingToDump)).AssemblyQualifiedName);
                }
                else if (field.FieldType.IsClass)
                {
                    if (String.Equals(field.FieldType.Name, "MonoCMethod")) // Don't recurse into 'MonoCMethod' as it can end up in a loop
                        Add("class " + field.FieldType.Name, SanitiseFieldName(field.Name));
                    else
                        Dump("class " + field.FieldType.Name + " " + SanitiseFieldName(field.Name), field.GetValue(thingToDump));
                }
                else if (field.FieldType.IsValueType && (!field.FieldType.IsPrimitive))
                {
                    Dump("struct " + field.FieldType.Name + " " + SanitiseFieldName(field.Name), field.GetValue(thingToDump));
                }
                else
                    Add(field.FieldType.Name, SanitiseFieldName(field.Name), field.GetValue(thingToDump).ToString());
            }

            Undent();

            return m_StringBuilder;
        }

        void DumpPrimitiveArray<PrimitiveType>(string name, Array array)
        {
            Indent();
            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
            {
                Add($"{name}[{elementIndex}]", ((PrimitiveType)array.GetValue(elementIndex)).ToString());
            }
            Undent();
        }

        void DumpSimpleObjectArray<PrimitiveType>(string name, Array array)
        {
            Indent();
            for (int elementIndex = 0; elementIndex < array.Length; elementIndex++)
            {
                PrimitiveType element = (PrimitiveType)array.GetValue(elementIndex);
                if (element != null)
                    Add($"{name}[{elementIndex}]", element.ToString());
                else
                    Add($"{name}[{elementIndex}]", "null");
            }
            Undent();
        }
    }

    // Custom DumpToText support for BuildUsageTagSet that cannot be properly dumped by reflection alone
    class CustomDumper_BuildUsageTagSet : DumpToText.ICustomDumper
    {
        Type DumpToText.ICustomDumper.GetType()
        {
            return typeof(BuildUsageTagSet);
        }

        void DumpToText.ICustomDumper.CustomDumper(DumpToText dumpToText, object value)
        {
            BuildUsageTagSet buildUsageTagSet = (BuildUsageTagSet)value;
            ObjectIdentifier[] objectIdentifiers = buildUsageTagSet.GetObjectIdentifiers();
            dumpToText.Indent();
            if (objectIdentifiers != null)
            {
                dumpToText.Add("ObjectIdentifier[]", "objectIdentifiers", $"ObjectIdentifier[{objectIdentifiers.Length}]");
                dumpToText.Indent();
                for (int objectIdentifierIndex = 0; objectIdentifierIndex < objectIdentifiers.Length; objectIdentifierIndex++)
                {
                    dumpToText.Add("ObjectIdentifier", $"[{objectIdentifierIndex}]");
                    dumpToText.Indent();
                    dumpToText.Dump("ObjectIdentifier", objectIdentifiers[objectIdentifierIndex]);
                    dumpToText.Undent();
                }
                dumpToText.Undent();
            }
            else
                dumpToText.Add("ObjectIdentifier[]", "objectIdentifiers", "null");
            dumpToText.Undent();
        }
    }
}
