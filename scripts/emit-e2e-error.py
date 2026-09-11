#!/usr/bin/env python3
"""Emit one fresh Windows fixture error without running the Windows application."""

from __future__ import annotations

import hashlib
import json
import os
import sys
import urllib.error
import urllib.request
import uuid
from datetime import UTC, datetime


def required_environment(name: str) -> str:
    value = os.environ.get(name, "").strip()
    if not value:
        raise SystemExit(f"{name} is required")
    return value


def build_payload(*, timestamp: str, service: str, run_id: str) -> dict[str, object]:
    message = "Windows configuration could not be loaded after initialization"
    context = {"operation": "initialize", "fixtureRunId": run_id}
    canonical_context = json.dumps(context, separators=(",", ":"), sort_keys=True)
    insert_id = hashlib.sha256(
        f"{timestamp}|{service}|ERROR|{message}|{canonical_context}".encode()
    ).hexdigest()[:32]
    return {
        "schemaVersion": 1,
        "entries": [
            {
                "timestamp": timestamp,
                "severity": "ERROR",
                "message": message,
                "service": service,
                "env": "test",
                "insertId": insert_id,
                "error": {
                    "type": "InvalidOperationException",
                    "message": message,
                    "stack": [
                        {
                            "function": (
                                "WindowsRuntimeE2E.WindowsMachineSettings.RequireInstallRoot"
                            ),
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
                "context": context,
                "labels": {"e2e": "windows-runtime", "fixture_run_id": run_id},
                "fingerprint": f"windows-runtime-e2e-{run_id}",
            }
        ],
    }


def main() -> int:
    endpoint = required_environment("PERSEA_LOGCORE_URL").rstrip("/")
    api_key = required_environment("PERSEA_LOGCORE_API_KEY")
    service = os.environ.get("PERSEA_SERVICE", "windows-runtime-e2e").strip()
    run_id = os.environ.get("PERSEA_E2E_RUN_ID", uuid.uuid4().hex).strip()
    timestamp = datetime.now(UTC).isoformat().replace("+00:00", "Z")
    payload = build_payload(timestamp=timestamp, service=service, run_id=run_id)
    request = urllib.request.Request(
        f"{endpoint}/v1/logs",
        data=json.dumps(payload, separators=(",", ":")).encode(),
        headers={"Content-Type": "application/json", "X-API-Key": api_key},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            body = json.loads(response.read())
    except urllib.error.HTTPError as error:
        print(f"Logcore rejected the fixture error with HTTP {error.code}.", file=sys.stderr)
        return 1
    except (OSError, ValueError) as error:
        print(f"Could not submit the fixture error: {error}", file=sys.stderr)
        return 1

    if response.status != 202 or body.get("accepted") != 1:
        print("Logcore did not accept exactly one fixture error.", file=sys.stderr)
        return 1
    print(f"Submitted Windows E2E fixture run {run_id}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
