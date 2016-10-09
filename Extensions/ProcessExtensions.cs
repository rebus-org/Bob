using System;
using System.Diagnostics;

namespace Bob.Extensions
{
    public static class ProcessExtensions
    {
        public static void WaitForSuccess(this Process process)
        {
            process.WaitForExit();

            if (process.ExitCode == 0) return;

            throw new ApplicationException($@"The process for

    {process.StartInfo.FileName} {process.StartInfo.Arguments}

existed with code {process.ExitCode}");
        }
    }
}