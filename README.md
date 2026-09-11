# Windows Runtime E2E Fixture

A deliberately broken, Windows-only .NET 8 repository for validating Persea's
Azure DevOps to Windows Spot debugger path. Azure DevOps hosts the repository,
Work Item, branch, and pull request; it does not run a pipeline.

The fixture exercises two Windows APIs:

- per-user Windows Registry persistence through `Microsoft.Win32.Registry`;
- native Win32 interop through `GetSystemDirectoryW` in `kernel32.dll`.

The baseline is expected to compile successfully and fail one runtime check with
the symptom `Expected the initialized installation root`. The repository does
not document the underlying defect because discovering and repairing it is the
debugger's task.

## Persea service contract

Configure the Azure DevOps service with:

| Field | Value |
|---|---|
| Runtime platform | `windows/amd64` |
| Dockerfile | `.persea/Dockerfile` |
| Build context | `.` |
| Setup command | `dotnet restore WindowsRuntimeE2E.sln --configfile NuGet.Config` |
| Test command | `dotnet run --project tests/WindowsRuntimeE2E.Tests --configuration Release --no-restore` |

The runtime image is Windows Server Core LTSC 2022 and runs project commands as
`ContainerUser`. The test project uses no external NuGet packages, so the
container can execute with network access disabled.

## Trigger without a Windows workstation

The repository can be authored and pushed from macOS. Once the service is
registered in Persea as an external backend, submit a fresh structured error
directly to Logcore:

```bash
export PERSEA_LOGCORE_URL='https://the-logcore-endpoint'
read -s PERSEA_LOGCORE_API_KEY
export PERSEA_LOGCORE_API_KEY
python3 scripts/emit-e2e-error.py
unset PERSEA_LOGCORE_API_KEY
```

The script never prints the API key. Each invocation generates a new fixture ID
and fingerprint so classifier idempotency does not reuse an earlier Work Item.

## Local validation boundary

A Linux or macOS .NET SDK can validate the managed-code compilation. Executing
the checks requires a Windows host or Windows container and exits early on every
other operating system because the implementation calls the Registry and Win32.
