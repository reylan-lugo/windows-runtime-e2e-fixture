using System.Runtime.Versioning;
using WindowsRuntimeE2E;

namespace WindowsRuntimeE2E.Tests;

internal static class Program
{
    public static int Main()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("This verification executable requires Windows.");
            return 2;
        }

        var failures = new List<string>();
        Run(failures, "native system directory is available", NativeSystemInformationReturnsAWindowsDirectory);
        Run(failures, "installation root survives a registry round trip", InstallationRootSurvivesRegistryRoundTrip);
        Run(failures, "installation root survives redirected registry round trip", InstallationRootSurvivesRedirectedRegistryRoundTrip);

        if (failures.Count == 0)
        {
            Console.WriteLine("All Windows runtime checks passed.");
            return 0;
        }

        Console.Error.WriteLine($"{failures.Count} Windows runtime check(s) failed:");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"- {failure}");
        }

        return 1;
    }

    private static void Run(List<string> failures, string name, Action verification)
    {
        try
        {
            verification();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception exception)
        {
            failures.Add($"{name}: {exception.Message}");
            Console.Error.WriteLine($"FAIL: {name}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void NativeSystemInformationReturnsAWindowsDirectory()
    {
        var systemDirectory = WindowsHostInformation.GetNativeSystemDirectory();
        if (!Path.IsPathFullyQualified(systemDirectory) || !Directory.Exists(systemDirectory))
        {
            throw new InvalidOperationException(
                $"The native Windows system directory is invalid: '{systemDirectory}'."
            );
        }
    }

    [SupportedOSPlatform("windows")]
    private static void InstallationRootSurvivesRegistryRoundTrip()
    {
        var testId = Guid.NewGuid().ToString("N");
        var expected = $@"C:\ProgramData\Persea\fixtures\{testId}";
        using var settings = new WindowsMachineSettings(
            $@"Software\Persea\WindowsRuntimeE2E\{testId}"
        );

        try
        {
            settings.SaveInstallRoot(expected);
            var actual = settings.RequireInstallRoot();
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Expected the initialized installation root, but received '{actual}'."
                );
            }
        }
        finally
        {
            settings.DeleteTestData();
        }
    }

    [SupportedOSPlatform("windows")]
    private static void InstallationRootSurvivesRedirectedRegistryRoundTrip()
    {
        var testId = Guid.NewGuid().ToString("N");
        var expected = $@"C:\ProgramData\Persea\fixtures\{testId}";
        using var settings = new WindowsMachineSettings(
            $@"Software\Classes\Persea\WindowsRuntimeE2E\{testId}"
        );

        try
        {
            settings.SaveInstallRoot(expected);
            var actual = settings.RequireInstallRoot();
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Expected the initialized installation root, but received '{actual}'."
                );
            }
        }
        finally
        {
            settings.DeleteTestData();
        }
    }
}
