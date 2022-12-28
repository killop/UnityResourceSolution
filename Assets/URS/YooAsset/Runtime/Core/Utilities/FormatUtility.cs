using System;

namespace MHLab.Patch.Core.Utilities
{
    public static class FormatUtility
    {
        private static readonly string[] SizesBinary = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };
        private static readonly string[] SizesDecimal = { "B", "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string FormatSizeBinary(long size, int decimals)
        {
            double formattedSize = size;
            int sizeIndex = 0;
            while (formattedSize >= 1024 && sizeIndex < SizesBinary.Length)
            {
                formattedSize /= 1024;
                sizeIndex += 1;
            }
            return Math.Round(formattedSize, decimals) + SizesBinary[sizeIndex];
        }

        public static string FormatSizeBinary(ulong size, int decimals)
        {
            double formattedSize = size;
            int sizeIndex = 0;
            while (formattedSize >= 1024 && sizeIndex < SizesBinary.Length)
            {
                formattedSize /= 1024;
                sizeIndex += 1;
            }
            return Math.Round(formattedSize, decimals) + SizesBinary[sizeIndex];
        }

        public static string FormatSizeDecimal(long size, int decimals)
        {
            double formattedSize = size;
            int sizeIndex = 0;
            while (formattedSize >= 1000 && sizeIndex < SizesDecimal.Length)
            {
                formattedSize /= 1000;
                sizeIndex += 1;
            }
            return Math.Round(formattedSize, decimals) + SizesDecimal[sizeIndex];
        }

        public static string FormatSizeDecimal(ulong size, int decimals)
        {
            double formattedSize = size;
            int sizeIndex = 0;
            while (formattedSize >= 1000 && sizeIndex < SizesDecimal.Length)
            {
                formattedSize /= 1000;
                sizeIndex += 1;
            }
            return Math.Round(formattedSize, decimals) + SizesDecimal[sizeIndex];
        }
    }
}
