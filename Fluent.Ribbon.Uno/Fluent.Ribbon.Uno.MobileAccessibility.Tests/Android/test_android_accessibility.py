import subprocess
import unittest
from pathlib import Path
from unittest.mock import patch

from run_android_accessibility import AndroidAccessibilityTest, DEFAULT_PACKAGE, TestFailure


class ProcessLifetimeTests(unittest.TestCase):
    def setUp(self):
        self.test = AndroidAccessibilityTest(
            "adb", Path("Showcase.apk"), DEFAULT_PACKAGE, Path("artifacts"), 20, "emulator-5554"
        )

    def test_does_not_probe_before_launch(self):
        with patch.object(self.test, "_adb") as adb:
            self.test._ensure_process_alive()
        adb.assert_not_called()

    def test_accepts_the_original_process(self):
        self.test.process_id = "1234"
        result = subprocess.CompletedProcess([], 0, "1234\n", "")
        with patch.object(self.test, "_adb", return_value=result):
            self.test._ensure_process_alive()

    def test_reports_process_exit_instead_of_waiting_for_launcher_nodes(self):
        self.test.process_id = "1234"
        result = subprocess.CompletedProcess([], 1, "", "")
        with patch.object(self.test, "_adb", return_value=result):
            with self.assertRaisesRegex(TestFailure, "process 1234 exited"):
                self.test._ensure_process_alive()

    def test_does_not_accept_a_restarted_application_as_the_original(self):
        self.test.process_id = "1234"
        result = subprocess.CompletedProcess([], 0, "5678\n", "")
        with patch.object(self.test, "_adb", return_value=result):
            with self.assertRaisesRegex(TestFailure, "process 1234 exited"):
                self.test._ensure_process_alive()


if __name__ == "__main__":
    unittest.main()
