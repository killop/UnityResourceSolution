namespace MHLab.Patch.Core.Octodiff
{
    internal interface IProgressReporter
    {
        void ReportProgress(string operation, long currentPosition, long total);
    }
}