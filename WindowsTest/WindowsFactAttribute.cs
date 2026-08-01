using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Codecrete.SwissQRBill.WindowsTest
{
    public sealed class WindowsFactAttribute : FactAttribute
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public WindowsFactAttribute(
            [CallerFilePath] string sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            if (!IsWindows)
            {
                Skip = "Only supported on Windows";
            }
        }
    }
}
