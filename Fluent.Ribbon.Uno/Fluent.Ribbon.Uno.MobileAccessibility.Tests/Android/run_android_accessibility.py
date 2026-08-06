#!/usr/bin/env python3
"""Exercise the Showcase through Android's out-of-process accessibility tree."""

from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import sys
import threading
import time
import xml.etree.ElementTree as ET
from collections.abc import Callable
from pathlib import Path


DEFAULT_PACKAGE = "com.companyname.fluent_ribbon_uno_showcase"
REMOTE_DUMP = "/sdcard/fluent-ribbon-accessibility.xml"


class TestFailure(RuntimeError):
    pass


class AndroidAccessibilityTest:
    def __init__(
        self,
        adb: str,
        apk: Path,
        package: str,
        artifact_directory: Path,
        timeout: float,
        serial: str | None,
    ) -> None:
        self.adb = adb
        self.apk = apk
        self.package = package
        self.artifact_directory = artifact_directory
        self.timeout = timeout
        self.serial = serial
        self.assertion_count = 0
        self.action_count = 0
        self.dump_count = 0
        self.latest_xml: bytes | None = None
        self.latest_root: ET.Element | None = None

    def run(self) -> None:
        self.artifact_directory.mkdir(parents=True, exist_ok=True)
        try:
            self._verify_device()
            self._install_and_launch()
            initial = self._wait_for_tree(
                "initial",
                lambda root: self._is_selected(root, "Toolbars")
                and self._count(root, "Clipboard") == 1
                and self._count(root, "Spinners") == 1,
            )
            self._assert_initial_tree(initial)

            bold = self._unique(initial, "Bold")
            self._tap(bold, "Bold")
            checked = self._wait_for_tree(
                "bold-checked",
                lambda root: self._count(root, "Bold") == 1
                and self._unique(root, "Bold").get("checked") == "true",
            )
            self._check(
                self._unique(checked, "Bold").get("checked") == "true",
                "Bold did not expose checked=true after its accessibility-visible tap.",
            )

            galleries = self._unique(checked, "Galleries")
            self._tap(galleries, "Galleries tab")
            gallery_tree = self._wait_for_tree(
                "galleries",
                lambda root: self._is_selected(root, "Galleries")
                and not self._is_selected(root, "Toolbars")
                and self._count(root, "Without Grouping") == 1,
            )
            self._assert_gallery_tree(gallery_tree)

            self._check(self.dump_count > 0, "No Android accessibility hierarchy was dumped.")
            self._check(self.action_count >= 2, "The Android accessibility actions did not execute.")
            print(
                "PASS: Android native accessibility "
                f"({self.dump_count} hierarchy dumps, "
                f"{self.action_count} actions, {self.assertion_count} assertions)."
            )
        except Exception:
            self._write_failure_diagnostics()
            raise
        finally:
            self._clean_app_state()

    def _verify_device(self) -> None:
        output = self._adb("devices", "-l").stdout
        devices = [
            line.split()[0]
            for line in output.splitlines()[1:]
            if "\tdevice" in line or re.search(r"\sdevice(?:\s|$)", line)
        ]
        if self.serial:
            self._check(
                self.serial in devices,
                f"Requested Android device {self.serial!r} is not online; found {devices}.",
            )
        else:
            self._check(
                len(devices) == 1,
                f"Expected exactly one online Android device; found {devices}. Use --serial to select one.",
            )
            self.serial = devices[0]

        api_level = self._adb("shell", "getprop", "ro.build.version.sdk").stdout.strip()
        architecture = self._adb("shell", "getprop", "ro.product.cpu.abi").stdout.strip()
        self._check(api_level.isdigit(), f"Android device returned an invalid API level: {api_level!r}.")
        print(f"Android device: {self.serial}, API {api_level}, ABI {architecture}")

    def _install_and_launch(self) -> None:
        self._check(self.apk.is_file(), f"Showcase APK does not exist: {self.apk}")
        print(f"Installing {self.apk}")
        self._adb("install", "-r", "-t", str(self.apk), timeout=max(self.timeout, 180))
        self._adb("shell", "am", "force-stop", self.package, check=False)
        self._adb("shell", "pm", "clear", self.package)

        resolved = self._adb(
            "shell",
            "cmd",
            "package",
            "resolve-activity",
            "--brief",
            "-c",
            "android.intent.category.LAUNCHER",
            self.package,
        ).stdout
        components = [line.strip() for line in resolved.splitlines() if "/" in line]
        self._check(
            len(components) == 1,
            f"Could not resolve exactly one launcher activity for {self.package}: {resolved!r}",
        )
        component = components[0]
        self._adb("shell", "am", "start", "-n", component)
        self._wait_until(
            "Showcase process",
            lambda: bool(self._adb("shell", "pidof", self.package, check=False).stdout.strip()),
        )
        self._wait_until("Showcase foreground window", self._is_foreground)
        print(f"Launched {component}")

    def _assert_initial_tree(self, root: ET.Element) -> None:
        ribbon = self._unique(root, "Fluent Ribbon Showcase")
        toolbars = self._unique(root, "Toolbars")
        clipboard = self._unique(root, "Clipboard")
        spinners = self._unique(root, "Spinners")
        painter = self._unique(root, "Format Painter")
        bold = self._unique(root, "Bold")
        right_margin = self._unique(root, "Right margin")

        self._check(toolbars.get("selected") == "true", "Toolbars was not selected initially.")
        self._check(toolbars.get("clickable") == "true", "Toolbars was not actionable.")
        self._check(clipboard.get("focusable") == "true", "Clipboard group was not focusable.")
        self._check(spinners.get("focusable") == "true", "Spinners group was not focusable.")
        self._check(painter.get("clickable") == "true", "Format Painter was not clickable.")
        self._check(painter.get("enabled") == "true", "Format Painter was not enabled.")
        self._check(bold.get("checkable") == "true", "Bold was not checkable.")
        self._check(bold.get("checked") == "false", "Bold did not expose its unchecked state.")
        self._check(right_margin.get("focusable") == "true", "Right margin was not focusable.")

        disabled_cut = [
            node
            for node in self._nodes_named(root, "Cut")
            if node.get("enabled") == "false"
        ]
        self._check(
            len(disabled_cut) == 1,
            f"Expected one disabled Cut command, found {len(disabled_cut)}.",
        )

        self._check(self._is_descendant(root, ribbon, toolbars), "Toolbars was outside the Ribbon tree.")
        self._check(self._is_descendant(root, ribbon, clipboard), "Clipboard was outside the Ribbon tree.")
        self._check(self._is_descendant(root, ribbon, spinners), "Spinners was outside the Ribbon tree.")
        self._check(
            self._is_descendant(root, clipboard, painter),
            "Format Painter was outside the Clipboard group.",
        )
        self._check(
            self._is_descendant(root, spinners, right_margin),
            "Right margin was outside the Spinners group.",
        )

        self._assert_absent(
            root,
            "Text 1",
            "Text 2",
            "Grouped ToggleButton",
            "Toggle 2",
            context="hidden Tests descendants",
        )
        self._assert_absent(
            root,
            "Confidential",
            "Do Not Copy",
            "Draft",
            "Remove Watermark",
            context="closed popup descendants",
        )

    def _assert_gallery_tree(self, root: ET.Element) -> None:
        self._check(self._is_selected(root, "Galleries"), "Galleries did not become selected.")
        self._check(not self._is_selected(root, "Toolbars"), "Toolbars remained selected.")
        self._unique(root, "Without Grouping")
        self._unique(root, "With Grouping")
        self._assert_absent(
            root,
            "Clipboard",
            "Spinners",
            "Format Painter",
            "Bold",
            "Right margin",
            context="hidden Toolbars descendants",
        )
        self._assert_absent(
            root,
            "Text 1",
            "Text 2",
            "Grouped ToggleButton",
            "Toggle 2",
            context="hidden Tests descendants after tab change",
        )
        self._assert_absent(
            root,
            "Confidential",
            "Do Not Copy",
            "Draft",
            context="closed popup descendants after tab change",
        )
        self._unique(root, "Fluent Ribbon Showcase")
        self._unique(root, "Galleries")

    def _wait_for_tree(
        self,
        name: str,
        predicate: Callable[[ET.Element], bool],
    ) -> ET.Element:
        deadline = time.monotonic() + self.timeout
        last_error = "no dump attempted"
        while time.monotonic() < deadline:
            self._adb("shell", "rm", "-f", REMOTE_DUMP, check=False)
            dump = self._adb(
                "shell",
                "uiautomator",
                "dump",
                REMOTE_DUMP,
                check=False,
                timeout=min(self.timeout, 30),
            )
            if dump.returncode == 0:
                xml_result = self._adb(
                    "exec-out",
                    "cat",
                    REMOTE_DUMP,
                    check=False,
                    text=False,
                )
                if xml_result.returncode == 0 and xml_result.stdout:
                    try:
                        root = ET.fromstring(xml_result.stdout)
                        self.dump_count += 1
                        self.latest_xml = xml_result.stdout
                        self.latest_root = root
                        self._save_xml(name, xml_result.stdout)
                        if predicate(root) and self._is_foreground():
                            return root
                        last_error = (
                            "predicate or foreground-window check not satisfied; "
                            f"nodes: {self._summary(root)}"
                        )
                    except ET.ParseError as error:
                        last_error = f"invalid hierarchy XML: {error}"
                else:
                    last_error = self._decode(xml_result.stderr or b"empty hierarchy")
            else:
                last_error = (dump.stderr or dump.stdout).strip()
            self._bounded_wait(0.25)
        raise TestFailure(f"Timed out waiting for {name} hierarchy: {last_error}")

    def _wait_until(self, description: str, predicate: Callable[[], bool]) -> None:
        deadline = time.monotonic() + self.timeout
        while time.monotonic() < deadline:
            if predicate():
                return
            self._bounded_wait(0.25)
        raise TestFailure(f"Timed out waiting for {description}.")

    def _tap(self, node: ET.Element, description: str) -> None:
        self._wait_until(f"{description} foreground window", self._is_foreground)
        bounds = node.get("bounds", "")
        match = re.fullmatch(r"\[(\d+),(\d+)]\[(\d+),(\d+)]", bounds)
        self._check(match is not None, f"{description} had invalid bounds {bounds!r}.")
        left, top, right, bottom = (int(value) for value in match.groups())
        self._check(right > left and bottom > top, f"{description} had empty bounds {bounds}.")
        x = (left + right) // 2
        y = (top + bottom) // 2
        print(f"Tapping {description} at ({x}, {y}) from {bounds}")
        self._adb("shell", "input", "tap", str(x), str(y))
        self.action_count += 1

    def _is_foreground(self) -> bool:
        windows = self._adb(
            "shell",
            "dumpsys",
            "window",
            check=False,
            timeout=min(self.timeout, 30),
        ).stdout
        return any(
            "mCurrentFocus=" in line and self.package in line
            for line in windows.splitlines()
        )

    def _unique(self, root: ET.Element, name: str) -> ET.Element:
        nodes = self._nodes_named(root, name)
        self._check(
            len(nodes) == 1,
            f"Expected exactly one logical node named {name!r}, found {len(nodes)}.",
        )
        return nodes[0]

    def _assert_absent(self, root: ET.Element, *names: str, context: str) -> None:
        present = {name: self._count(root, name) for name in names if self._count(root, name)}
        self._check(not present, f"Found {context}: {present}.")

    def _is_descendant(
        self,
        root: ET.Element,
        ancestor: ET.Element,
        descendant: ET.Element,
    ) -> bool:
        parents = {
            child: parent
            for parent in root.iter()
            for child in parent
        }
        current = parents.get(descendant)
        while current is not None:
            if current is ancestor:
                return True
            current = parents.get(current)
        return False

    def _is_selected(self, root: ET.Element, name: str) -> bool:
        nodes = self._nodes_named(root, name)
        return len(nodes) == 1 and nodes[0].get("selected") == "true"

    def _nodes_named(self, root: ET.Element, name: str) -> list[ET.Element]:
        return [node for node in root.iter("node") if self._name(node) == name]

    def _count(self, root: ET.Element, name: str) -> int:
        return len(self._nodes_named(root, name))

    @staticmethod
    def _name(node: ET.Element) -> str:
        return node.get("content-desc") or node.get("text") or ""

    def _check(self, condition: bool, message: str) -> None:
        self.assertion_count += 1
        if not condition:
            raise TestFailure(message)

    def _save_xml(self, name: str, content: bytes) -> None:
        (self.artifact_directory / f"{name}.xml").write_bytes(content)

    def _write_failure_diagnostics(self) -> None:
        print("Android accessibility failure diagnostics:", file=sys.stderr)
        if self.latest_root is not None:
            print(self._summary(self.latest_root), file=sys.stderr)
        if self.latest_xml is not None:
            (self.artifact_directory / "failure-latest.xml").write_bytes(self.latest_xml)

        screenshot = self._adb("exec-out", "screencap", "-p", check=False, text=False)
        if screenshot.returncode == 0 and screenshot.stdout:
            (self.artifact_directory / "failure.png").write_bytes(screenshot.stdout)

        logcat = self._adb("logcat", "-d", "-t", "500", check=False)
        (self.artifact_directory / "failure-logcat.txt").write_text(
            logcat.stdout + logcat.stderr,
            encoding="utf-8",
        )

    def _clean_app_state(self) -> None:
        self._adb("shell", "am", "force-stop", self.package, check=False)
        self._adb("shell", "pm", "clear", self.package, check=False)
        self._adb("shell", "rm", "-f", REMOTE_DUMP, check=False)

    def _summary(self, root: ET.Element) -> str:
        interesting = []
        for node in root.iter("node"):
            name = self._name(node)
            if not name:
                continue
            states = ",".join(
                f"{key}={node.get(key)}"
                for key in ("enabled", "clickable", "checkable", "checked", "selected", "focusable")
                if node.get(key) == "true" or key in ("enabled", "checked", "selected")
            )
            interesting.append(f"{name!r} [{states}] {node.get('bounds', '')}")
        return "\n".join(interesting[:150])

    def _adb(
        self,
        *arguments: str,
        check: bool = True,
        timeout: float | None = None,
        text: bool = True,
    ) -> subprocess.CompletedProcess:
        command = [self.adb]
        if self.serial:
            command.extend(("-s", self.serial))
        command.extend(arguments)
        result = subprocess.run(
            command,
            capture_output=True,
            text=text,
            timeout=timeout or self.timeout,
            check=False,
        )
        if check and result.returncode != 0:
            stdout = result.stdout if text else self._decode(result.stdout)
            stderr = result.stderr if text else self._decode(result.stderr)
            raise TestFailure(
                f"Command failed ({result.returncode}): {' '.join(command)}\n"
                f"stdout: {stdout}\nstderr: {stderr}"
            )
        return result

    @staticmethod
    def _bounded_wait(seconds: float) -> None:
        threading.Event().wait(seconds)

    @staticmethod
    def _decode(value: bytes) -> str:
        return value.decode("utf-8", errors="replace")


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Install the native-rendered Fluent Ribbon Showcase APK and validate "
            "Android's out-of-process accessibility hierarchy."
        )
    )
    parser.add_argument("apk", type=Path, help="Path to the built Showcase APK.")
    parser.add_argument("--package", default=DEFAULT_PACKAGE, help="Android application package.")
    parser.add_argument("--serial", help="adb device serial; required when multiple devices are online.")
    parser.add_argument("--adb", help="Path to adb. Defaults to adb from PATH or ANDROID_SDK_ROOT.")
    parser.add_argument(
        "--timeout",
        type=float,
        default=120,
        help="Bounded wait timeout in seconds for each process/UI state.",
    )
    parser.add_argument(
        "--artifact-directory",
        type=Path,
        default=Path("TestResults") / "AndroidAccessibility",
        help="Directory for hierarchy and failure diagnostics.",
    )
    return parser.parse_args()


def find_adb(explicit: str | None) -> str:
    if explicit:
        return explicit
    from_path = shutil.which("adb")
    if from_path:
        return from_path
    for variable in ("ANDROID_SDK_ROOT", "ANDROID_HOME"):
        root = os.environ.get(variable)
        if root:
            candidate = Path(root) / "platform-tools" / ("adb.exe" if os.name == "nt" else "adb")
            if candidate.is_file():
                return str(candidate)
    raise TestFailure("adb was not found. Pass --adb or set ANDROID_SDK_ROOT.")


def main() -> int:
    arguments = parse_arguments()
    try:
        test = AndroidAccessibilityTest(
            adb=find_adb(arguments.adb),
            apk=arguments.apk.resolve(),
            package=arguments.package,
            artifact_directory=arguments.artifact_directory.resolve(),
            timeout=arguments.timeout,
            serial=arguments.serial,
        )
        test.run()
        return 0
    except (TestFailure, subprocess.TimeoutExpired, OSError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
