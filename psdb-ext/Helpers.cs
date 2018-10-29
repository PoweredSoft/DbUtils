using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psdb_ext
{
    public static class Helpers
    {
        public static void WriteToOutput(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var generalPaneGuid = VSConstants.GUID_OutWindowDebugPane; // P.S. There's also the GUID_OutWindowDebugPane available.
            outWindow.GetPane(ref generalPaneGuid, out var generalPane);

            if (generalPane != null)
            { 
                generalPane.OutputString(message);
                generalPane.Activate(); // Brings this pane into vie
            }
        }

        public static void InvokeCommandLineAndOutput(string exeName, string arguments, int timeout = 5000)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = exeName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();


            process.WaitForExit(timeout);
            if (process.HasExited)
            {
                string errorStr = process.StandardError.ReadToEnd();
                string standardStr = process.StandardOutput.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(standardStr))
                    Helpers.WriteToOutput(standardStr);
                if (!string.IsNullOrWhiteSpace(errorStr))
                    Helpers.WriteToOutput(errorStr);

            }
        }
    }
}
