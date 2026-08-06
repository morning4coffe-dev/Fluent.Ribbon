#!/bin/bash
set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project="${script_directory}/FluentRibbonMobileAccessibilityUITests.xcodeproj"
scheme="FluentRibbonMobileAccessibilityUITests"
results="${IOS_ACCESSIBILITY_RESULTS_DIR:-${script_directory}/TestResults}"
app_argument="${1:-${SHOWCASE_IOS_APP:-}}"

if [[ -z "${app_argument}" ]]; then
  echo "Usage: $0 /absolute/path/to/Fluent.Ribbon.Uno.Showcase.app" >&2
  exit 2
fi

app_path="$(cd "$(dirname "${app_argument}")" && pwd)/$(basename "${app_argument}")"
if [[ ! -d "${app_path}" || ! -f "${app_path}/Info.plist" ]]; then
  echo "The native Uno Showcase .app was not found at ${app_path}." >&2
  exit 1
fi

bundle_id=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "${app_path}/Info.plist")
if [[ "${bundle_id}" != "com.companyname.fluent-ribbon-uno-showcase" ]]; then
  echo "Unexpected Showcase bundle identifier ${bundle_id} in ${app_path}." >&2
  exit 1
fi

selection="$(xcrun simctl list devices available --json | python3 "${script_directory}/select_ios_simulator.py")"
IFS=$'\t' read -r simulator_id initial_state simulator_name simulator_runtime <<<"${selection}"
if [[ -z "${simulator_id}" ]]; then
  echo "Simulator selection returned no UDID." >&2
  exit 1
fi
echo "Selected ${simulator_name} (${simulator_runtime}, ${simulator_id}, ${initial_state})"

cleanup() {
  xcrun simctl terminate "${simulator_id}" "${bundle_id}" >/dev/null 2>&1 || true
  xcrun simctl uninstall "${simulator_id}" "${bundle_id}" >/dev/null 2>&1 || true
  if [[ "${initial_state}" != "Booted" ]]; then
    xcrun simctl shutdown "${simulator_id}" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

if [[ "${initial_state}" != "Booted" ]]; then
  xcrun simctl boot "${simulator_id}"
fi
xcrun simctl bootstatus "${simulator_id}" -b
xcrun simctl uninstall "${simulator_id}" "${bundle_id}" >/dev/null 2>&1 || true
xcrun simctl install "${simulator_id}" "${app_path}"

rm -rf "${results}"
mkdir -p "${results}"
log_path="${results}/xcodebuild.log"
result_bundle="${results}/MobileAccessibility.xcresult"
derived_data="${results}/DerivedData"

set +e
xcodebuild \
  -project "${project}" \
  -scheme "${scheme}" \
  -configuration Debug \
  -destination "platform=iOS Simulator,id=${simulator_id}" \
  -derivedDataPath "${derived_data}" \
  -resultBundlePath "${result_bundle}" \
  "SHOWCASE_IOS_APP=${app_path}" \
  test 2>&1 | tee "${log_path}"
xcode_status=${PIPESTATUS[0]}
set -e

executed="$(
  sed -nE 's/.*Executed ([0-9]+) tests?, with.*/\1/p' "${log_path}" \
    | tail -n 1
)"
if [[ ${xcode_status} -ne 0 ]]; then
  echo "xcodebuild failed with exit code ${xcode_status}; diagnostics remain in ${results}." >&2
  exit "${xcode_status}"
fi
if [[ -z "${executed}" || "${executed}" -lt 1 ]]; then
  echo "XCTest reported no executed mobile accessibility tests." >&2
  exit 1
fi

echo "PASS: iOS native accessibility (${executed} XCTest test executed)."
rm -rf "${results}"
