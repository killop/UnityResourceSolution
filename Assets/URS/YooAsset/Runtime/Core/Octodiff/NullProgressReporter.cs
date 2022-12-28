namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class NullProgressReporter : IProgressReporter
    {
        public void ReportProgress(string operation, long currentPosition, long total)
        {
        }
    }
}