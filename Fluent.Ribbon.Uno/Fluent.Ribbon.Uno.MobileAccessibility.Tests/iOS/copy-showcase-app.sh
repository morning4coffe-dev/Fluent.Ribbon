#!/bin/bash
set -euo pipefail

if [[ -z "${SHOWCASE_IOS_APP:-}" ]]; then
  echo "SHOWCASE_IOS_APP must point to the built Uno Showcase .app." >&2
  exit 1
fi

source_app="${SHOWCASE_IOS_APP}"
destination_app="${TARGET_BUILD_DIR}/${WRAPPER_NAME}"

if [[ ! -d "${source_app}" || ! -f "${source_app}/Info.plist" ]]; then
  echo "The built Uno Showcase app is missing or invalid: ${source_app}" >&2
  exit 1
fi

bundle_id=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "${source_app}/Info.plist")
executable=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "${source_app}/Info.plist")
if [[ "${bundle_id}" != "com.companyname.fluent-ribbon-uno-showcase" ]]; then
  echo "Unexpected Showcase bundle identifier: ${bundle_id}" >&2
  exit 1
fi
if [[ ! -f "${source_app}/${executable}" ]]; then
  echo "The Showcase executable ${executable} is missing from ${source_app}." >&2
  exit 1
fi
if [[ "${executable}" != "${EXECUTABLE_NAME}" ]]; then
  echo "The Xcode wrapper expects ${EXECUTABLE_NAME}, but the Uno app contains ${executable}." >&2
  exit 1
fi

rm -rf "${destination_app}"
/usr/bin/ditto "${source_app}" "${destination_app}"
echo "Copied native Uno app ${source_app} to Xcode test product ${destination_app}"
