using System.ComponentModel;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsRuntimeE2E;

[SupportedOSPlatform("windows")]
public static class WindowsHostInformation
{
    private const int InitialBufferLength = 260;

    public static string GetNativeSystemDirectory()
    {
        EnsureWindows();

        var buffer = new StringBuilder(InitialBufferLength);
        var length = GetSystemDirectory(buffer, (uint)buffer.Capacity);
        if (length == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (length >= buffer.Capacity)
        {
            buffer = new StringBuilder(checked((int)length + 1));
            length = GetSystemDirectory(buffer, (uint)buffer.Capacity);
            if (length == 0 || length >= buffer.Capacity)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        return buffer.ToString();
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Native host information requires Windows.");
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "GetSystemDirectoryW", CharSet = CharSet.Unicode,
        ExactSpelling = true, SetLastError = true)]
    private static extern uint GetSystemDirectory(StringBuilder buffer, uint size);
}
