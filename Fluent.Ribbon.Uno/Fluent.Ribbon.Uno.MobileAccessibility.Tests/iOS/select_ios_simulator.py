#!/usr/bin/env python3
"""Select one available iOS 26 simulator deterministically."""

from __future__ import annotations

import json
import re
import sys


PREFERRED_DEVICES = (
    "iPhone 17 Pro",
    "iPhone 17",
    "iPhone 16 Pro",
    "iPhone 16",
    "iPhone 15 Pro",
    "iPhone 15",
)


def runtime_version(identifier: str) -> tuple[int, ...]:
    match = re.search(r"\.iOS-(26(?:-\d+)*)$", identifier)
    return tuple(int(part) for part in match.group(1).split("-")) if match else ()


def main() -> int:
    data = json.load(sys.stdin)
    candidates: list[tuple[tuple[int, ...], int, str, str, str, str]] = []
    for runtime, devices in data.get("devices", {}).items():
        version = runtime_version(runtime)
        if not version:
            continue
        for device in devices:
            if not device.get("isAvailable", False):
                continue
            name = device.get("name", "")
            try:
                preference = PREFERRED_DEVICES.index(name)
            except ValueError:
                preference = len(PREFERRED_DEVICES)
            candidates.append(
                (
                    tuple(-part for part in version),
                    preference,
                    name,
                    device.get("udid", ""),
                    device.get("state", ""),
                    runtime,
                )
            )

    if not candidates:
        print("No available iOS 26 simulator was found.", file=sys.stderr)
        return 1

    _, _, name, udid, state, runtime = sorted(candidates)[0]
    print("\t".join((udid, state, name, runtime)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
