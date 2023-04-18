using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BestHTTP.PlatformSupport.Text;

namespace BestHTTP.Forms
{
    /// <summary>
    /// A HTTP Form implementation to send textual values.
    /// </summary>
    public sealed class HTTPUrlEncodedForm : HTTPFormBase
    {
        private const int EscapeTreshold = 256;

        private byte[] CachedData;

        public override void PrepareRequest(HTTPRequest request)
        {
            request.SetHeader("Content-Type", "application/x-www-form-urlencoded");
        }

        public override byte[] GetData()
        {
            if (CachedData != null && !IsChanged)
                return CachedData;

            StringBuilder sb = PlatformSupport.Text.StringBuilderPool.Get(Fields.Count * 4);

            // Create a "field1=value1&field2=value2" formatted string
            for (int i = 0; i < Fields.Count; ++i)
            {
                var field = Fields[i];

                if (i > 0)
                    sb.Append("&");

                sb.Append(EscapeString(field.Name));
                sb.Append("=");

                if (!string.IsNullOrEmpty(field.Text) || field.Binary == null)
                    sb.Append(EscapeString(field.Text));
                else
                    // If forced to this form type with binary data, we will create a base64 encoded string from it.
                    sb.Append(Convert.ToBase64String(field.Binary, 0, field.Binary.Length));
            }

            IsChanged = false;
            return CachedData = Encoding.UTF8.GetBytes(PlatformSupport.Text.StringBuilderPool.ReleaseAndGrab(sb));
        }

        public static string EscapeString(string originalString)
        {
            if (originalString.Length < EscapeTreshold)
                return Uri.EscapeDataString(originalString);
            else
            {
                int loops = originalString.Length / EscapeTreshold;
                StringBuilder sb = StringBuilderPool.Get(loops); //new StringBuilder(loops);

                for (int i = 0; i <= loops; i++)
                   sb.Append(i < loops ?
                                Uri.EscapeDataString(originalString.Substring(EscapeTreshold * i, EscapeTreshold)) :
                                Uri.EscapeDataString(originalString.Substring(EscapeTreshold * i)));
                return StringBuilderPool.ReleaseAndGrab(sb);
            }
        }

    }
}
