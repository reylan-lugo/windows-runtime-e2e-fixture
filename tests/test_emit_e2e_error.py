from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location(
    "emit_e2e_error", ROOT / "scripts" / "emit-e2e-error.py"
)
assert SPEC is not None and SPEC.loader is not None
EMITTER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(EMITTER)


class EmitE2EErrorTests(unittest.TestCase):
    def test_payload_matches_the_reproducible_windows_failure(self) -> None:
        self.assertTrue(
            hasattr(EMITTER, "build_payload"),
            "the emitter must expose the payload it sends to Logcore",
        )
        payload = EMITTER.build_payload(
            timestamp="2026-09-11T12:34:56Z",
            service="windows-runtime-e2e",
            run_id="run-123",
        )

        self.assertEqual(payload["schemaVersion"], 1)
        self.assertEqual(len(payload["entries"]), 1)
        entry = payload["entries"][0]
        self.assertEqual(entry["insertId"], "ade07e32f9c60ebf0c209f85fdbb1f56")
        self.assertEqual(
            entry["error"],
            {
                "type": "InvalidOperationException",
                "message": "Windows configuration could not be loaded after initialization",
                "stack": [
                    {
                        "function": "WindowsRuntimeE2E.WindowsMachineSettings.RequireInstallRoot",
                        "file": "src/WindowsRuntimeE2E/WindowsMachineSettings.cs",
                        "line": 42,
                        "inApp": True,
                    },
                    {
                        "function": (
                            "WindowsRuntimeE2E.Tests.Program."
                            "InstallationRootSurvivesRegistryRoundTrip"
                        ),
                        "file": "tests/WindowsRuntimeE2E.Tests/Program.cs",
                        "line": 73,
                        "inApp": True,
                    },
                ],
            },
        )
        self.assertEqual(entry["fingerprint"], "windows-runtime-e2e-run-123")


if __name__ == "__main__":
    unittest.main()
