namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class BinaryComparer
    {
        public static bool CompareArray(byte[] strA, byte[] strB)
        {
            int length = strA.Length;
            if (length != strB.Length)
            {
                return false;
            }
            for (int i = 0; i < length; i++)
            {
                if (strA[i] != strB[i]) return false;
            }
            return true;
        }
    }
}
