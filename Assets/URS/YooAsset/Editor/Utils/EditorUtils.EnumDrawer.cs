
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public partial class EditorUtils
{
    private class MaskBit
    {
        public long enumValue = 0;
        public long maskValue = 0;
        public long indexMaskValue = 0;
        public string name = "";
        public bool toggle = false;
    }
    private class Mask
    {
        public long fullMaskValue = 0;
        public long indexFullMaskValue = 0;
        public string[] nameArray = { };
        public List<MaskBit> bitList = new List<MaskBit>();

        public long MaskValue
        {
            get
            {
                long v = 0;
                for (int i = 0; i < bitList.Count; ++i)
                {
                    if (bitList[i].toggle)
                        v |= bitList[i].maskValue;
                }
                if (v == fullMaskValue)
                    return -1;
                return v;
            }
            set
            {
                for (int i = 0; i < bitList.Count; ++i)
                {
                    var bit = bitList[i];
                    if ((value & bit.maskValue) != 0)
                        bit.toggle = true;
                    else
                        bit.toggle = false;
                }
            }
        }
        public long IndexMaskValue
        {
            get
            {
                long v = 0;
                for (int i = 0; i < bitList.Count; ++i)
                {
                    if (bitList[i].toggle)
                        v |= (long)1 << i;
                }
                if (v == indexFullMaskValue)
                    return -1;
                return v;
            }
            set
            {
                for (int i = 0; i < bitList.Count; ++i)
                {
                    var bit = bitList[i];
                    if ((value & bit.indexMaskValue) != 0)
                        bit.toggle = true;
                    else
                        bit.toggle = false;
                }
            }
        }
        public override string ToString()
        {
            long value = MaskValue;

            if (value == 0)
                return "Nothing";
            if (value == -1)
                return "Everything";

            StringBuilder sb = new StringBuilder();
            sb.Remove(0, sb.Length);
            for (int i = 0; i < bitList.Count; ++i)
            {
                if (bitList[i].toggle)
                {
                    if (sb.Length != 0)
                        sb.Append("/");
                    sb.Append(bitList[i].name);
                }
            }

            return sb.ToString();
        }
    }
    private class _EnumValue
    {
        public string name = "";
        public string upper = "";
        //public string abbr = "";
        public int value = 0;
    }
    private class _Enum
    {
        private Type type = null;
        private int value = 0;
        private string filterText = null;
        private string[] displayedOptions = new string[] { };
        private List<int> displayedValues = new List<int>();
        private List<_EnumValue> allValues = new List<_EnumValue>();

        private void SetAllValues()
        {
            displayedOptions = new string[allValues.Count];
            displayedValues.Clear();
            for (int i = 0; i < allValues.Count; ++i)
            {
                displayedOptions[i] = allValues[i].name;
                displayedValues.Add(allValues[i].value);
            }
        }
        public string FilterText
        {
            get { return filterText; }
            set
            {
                if (value.Equals(filterText))
                    return;
                filterText = value;
                if (string.IsNullOrEmpty(filterText))
                {
                    SetAllValues();
                    return;
                }
                string upper = filterText.ToUpper();
                List<string> displayedOptionsTemp = new List<string>();
                displayedValues.Clear();
                for (int i = 0; i < allValues.Count; ++i)
                {
                    var v = allValues[i];
                    if (v.upper.Contains(upper))// || v.abbr.Contains(upper))
                    {
                        displayedOptionsTemp.Add(v.name);
                        displayedValues.Add(v.value);
                    }
                }
                if (displayedValues.Count == 0)
                {
                    SetAllValues();
                }
                else
                {
                    displayedOptions = displayedOptionsTemp.ToArray();
                }
            }
        }
        public string[] DisplayedOptions
        {
            get { return displayedOptions; }
        }
        public Enum EnumValue
        {
            get { return (Enum)Enum.ToObject(type, value); }
            set { this.value = Convert.ToInt32(value); }
        }
        public int DisplayedValue
        {
            get
            {
                int index = -1;
                for (int i = 0; i < displayedValues.Count; ++i)
                {
                    if (displayedValues[i] == value)
                    {
                        index = i;
                        break;
                    }
                }
                return index;
            }
            set
            {
                if (value >= 0 && value < displayedValues.Count)
                    this.value = displayedValues[value];
            }
        }
        public _Enum(Type type)
        {
            this.type = type;

            allValues.Clear();
            var vs = Enum.GetValues(type);
            foreach (var it in vs)
            {
                _EnumValue v = new _EnumValue();
                v.value = Convert.ToInt32(it);
                v.name = v.value.ToString() + "_" + it.ToString();
                v.upper = v.name.ToUpper();
                allValues.Add(v);
            }

            FilterText = "";
        }
    }
    private static Dictionary<Type, Mask> maskMap = new Dictionary<Type, Mask>();
    private static Dictionary<Type, _Enum> enumMap = new Dictionary<Type, _Enum>();
    private static byte[] tempByteArray = new byte[1024];

    private static Mask GetMask(Type type)
    {
        Mask mask = null;
        if (!maskMap.TryGetValue(type, out mask))
        {
            mask = new Mask();
            var values = Enum.GetValues(type);

            foreach (var v in values)
            {
                long enumValue = Convert.ToInt64(v);
                if (enumValue > 0)
                {
                    MaskBit bit = new MaskBit();
                    bit.enumValue = enumValue;
                    bit.maskValue = (long)1 << ((int)enumValue - 1);
                    bit.name = v.ToString();
                    bit.toggle = false;
                    mask.bitList.Add(bit);

                    mask.fullMaskValue |= bit.maskValue;
                }
            }
            mask.bitList.Sort((x, y) =>
            {
                if (x.enumValue > y.enumValue)
                {
                    return 1;
                }
                else if (x.enumValue < y.enumValue)
                {
                    return -1;
                }
                return 0;
            });
            mask.nameArray = new string[mask.bitList.Count];
            for (int i = 0; i < mask.bitList.Count; ++i)
            {
                var bit = mask.bitList[i];
                bit.indexMaskValue = (long)1 << i;
                mask.nameArray[i] = bit.name;

                mask.indexFullMaskValue |= bit.indexMaskValue;
            }

            maskMap.Add(type, mask);
        }
        return mask;
    }

    private static _Enum GetEnum(Type type)
    {
        _Enum _enum = null;
        if (!enumMap.TryGetValue(type, out _enum))
        {
            _enum = new _Enum(type);

            enumMap.Add(type, _enum);
        }
        return _enum;
    }

    public static string TranslateMask(Type type, long value)
    {
        Mask mask = GetMask(type);
        mask.MaskValue = value;
        return mask.ToString();
    }

    public static int MaxEnumValue(Type enumType)
    {
        int maxValue = 0;
        var values = Enum.GetValues(enumType);
        foreach (var v in values)
        {
            int vv = Convert.ToInt32(v);
            if (vv > maxValue)
                maxValue = vv;
        }
        return maxValue;
    }

    public static void GetCharCount(string info, ref int cnCount, ref int enCount, ref int A2ZCount)
    {
        for (int i = 0; i <= info.Length - 1; i++)
        {
            int byteCount = System.Text.Encoding.Default.GetBytes(info, i, 1, tempByteArray, 0);
            if (byteCount > 1)
            {
                cnCount++;
            }
            else if (byteCount > 0)
            {
                enCount++;
                if (info[0] >= 'A' && info[0] <= 'Z')
                {
                    A2ZCount++;
                }
            }
        }
    }
}