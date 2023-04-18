using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NinjaBeats
{

    public static partial class Utils
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendRepeat(this StringBuilder self, string value, int count)
        {
            for (int i = 0; i < count; ++i)
                self.Append(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendRepeat(this StringBuilder self, char value, int count)
        {
            for (int i = 0; i < count; ++i)
                self.Append(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendSlash(this StringBuilder self)
        {
            int len = self.Length;
            if (len > 0)
            {
                var last = self[len - 1];
                if (last == '\\' || last == '/')
                    return;
            }

            self.Append('/');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReplaceCharNoGC(this string str, char oldChar, char newChar)
        {
            if (string.IsNullOrEmpty(str))
                return;

            fixed (char* ptr = str)
            {
                int len = str.Length;
                for (int i = 0; i < len; ++i)
                {
                    if (ptr[i] == oldChar)
                        ptr[i] = newChar;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Replace(this string body, string oldStr, string newStr, int startIndex)
        {
            return body.Remove(startIndex, oldStr.Length).Insert(startIndex, newStr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Between(this string self, char left, char right)
        {
            int leftIdx = self.IndexOf(left);
            if (leftIdx == -1)
                return default;

            int rightIdx = self.LastIndexOf(right);
            return self.Substring(leftIdx + 1, rightIdx - leftIdx - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string Inverse(this string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var len = text.Length;
                var halfLen = len / 2;
                fixed (char* p = text)
                {
                    for (int i = 0; i < halfLen; ++i)
                    {
                        var j = len - i - 1;
                        var t = p[i];
                        p[i] = p[j];
                        p[j] = t;
                    }
                }
            }

            return text;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToInt32(this string self, out int result)
        {
            result = 0;
            try
            {
                if (int.TryParse(self, out result))
                    return true;
                return false;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(this string self) => self.ToInt32(out var result) ? result : 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToFloat(this string self, out float result)
        {
            result = 0;
            try
            {
                if (float.TryParse(self, out result))
                    return true;
                return false;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(this string self) => self.ToFloat(out var result) ? result : 0;

        private const int cLower2Upper = (int)'A' - (int)'a';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSpace(this char self) => self == '\r' || self == '\n' || self == '\t' || self == ' ';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLowerLetter(this char self) => self >= 'a' && self <= 'z';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUpperLetter(this char self) => self >= 'A' && self <= 'Z';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLetter(this char self) => self.IsLowerLetter() || self.IsUpperLetter();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumber(this char self) => self >= '0' && self <= '9';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsToken(this char self) => self.IsLetter() || self.IsNumber() || self == '_';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToUpper(this char self) => self.IsLowerLetter() ? (char)((int)self + cLower2Upper) : self;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToLower(this char self) => self.IsUpperLetter() ? (char)((int)self - cLower2Upper) : self;

    }
}