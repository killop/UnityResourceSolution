using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    public static class DeltaFileApplier
    {
        public static void Apply(string fileBackupPath, string patchPath, string filePath)
        {
            var delta = new DeltaApplier
            {
                SkipHashCheck = true
            };

            using (var basisStream = new FileStream(fileBackupPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var deltaStream = new FileStream(patchPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var newFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                delta.Apply(basisStream, new BinaryDeltaReader(deltaStream, null), newFileStream);
            }
        }
    }
}
