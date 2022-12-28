using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class ConsoleProgressReporter : IProgressReporter
    {
        private string currentOperation;
        private int progressPercentage;

        public void ReportProgress(string operation, long currentPosition, long total)
        {
            var percent = (int)((double)currentPosition/total * 100d + 0.5);
            if (currentOperation != operation)
            {
                progressPercentage = -1;
                currentOperation = operation;
            }

            if (progressPercentage != percent && percent % 10 == 0)
            {
                progressPercentage = percent;
                Console.WriteLine("{0}: {1}%", currentOperation, percent);
            }
        }
    }
}