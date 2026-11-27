using System.Runtime.Versioning;
using Microsoft.Win32;

namespace WindowsRuntimeE2E;

[SupportedOSPlatform("windows")]
public sealed class WindowsMachineSettings : IDisposable
{
    private const string InstallRootValue = "InstallRoot";
    private readonly string _subkeyPath;
    private bool _disposed;

    public WindowsMachineSettings(string subkeyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subkeyPath);
        _subkeyPath = subkeyPath;
    }

    public void SaveInstallRoot(string installRoot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);

        using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using var settings = root.CreateSubKey(_subkeyPath, writable: true)
            ?? throw new InvalidOperationException("The Windows settings key could not be created.");
        settings.SetValue(InstallRootValue, installRoot, RegistryValueKind.String);
    }

    public string? LoadInstallRoot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using var settings = root.OpenSubKey(_subkeyPath, writable: false);
        return settings?.GetValue(InstallRootValue) as string;
    }

    public string RequireInstallRoot()
    {
        return LoadInstallRoot()
            ?? throw new InvalidOperationException(
                "Windows configuration could not be loaded after initialization"
            );
    }

    public void DeleteTestData()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DeleteFromView(RegistryView.Registry32);
        DeleteFromView(RegistryView.Registry64);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void DeleteFromView(RegistryView view)
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        root.DeleteSubKeyTree(_subkeyPath, throwOnMissingSubKey: false);
    }
}
