using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    public static class DeltaFileBuilder
    {
        public static void Build(string fromFile, string toFile, string patchFile, string signatureFile)
        {
            SignatureBuilder signatureBuilder = new SignatureBuilder();
            DeltaBuilder deltaBuilder = new DeltaBuilder();

            FileStream oldFile = new FileStream(fromFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            FileStream newFile = new FileStream(toFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            FileStream patch = new FileStream(patchFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            FileStream sign = new FileStream(signatureFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            signatureBuilder.Build(oldFile, new SignatureWriter(sign));
            sign.Close();
            sign.Dispose();
            deltaBuilder.BuildDelta(newFile, new SignatureReader(sign.Name, deltaBuilder.ProgressReporter), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(patch)));

            oldFile.Close();
            oldFile.Dispose();
            newFile.Close();
            newFile.Dispose();
            patch.Close();
            patch.Dispose();
        }
    }
}
