"""Pinned source and package provenance for the opt-in Uno accessibility build."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath, PureWindowsPath
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile


HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
NUSPEC_NS = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"
NS = {"n": NUSPEC_NS}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def file_hash(path, *, text=False):
    data = Path(path).read_bytes()
    return sha256(data.replace(b"\r\n", b"\n") if text else data)


def load_manifest():
    return json.loads((HERE / "backport.json").read_text(encoding="utf-8"))


def load_sdk_pin():
    return json.loads((HERE / "sdk-manifest.json").read_text(encoding="utf-8"))


def source_patches(manifest):
    return manifest["patches"] + [load_sdk_pin()["patch"]]


def version(manifest, platform):
    return f"{manifest['packageVersionPrefix']}.{platform}"


def owned_path(path):
    result = Path(path).resolve()
    require(result.is_relative_to(REPO / "artifacts" / "UnoBackport"),
            f"Backport outputs must be below {REPO / 'artifacts' / 'UnoBackport'}: {result}")
    return result


def source_path(root, path):
    return Path(root).joinpath(*PurePosixPath(path).parts)


def git(source, *arguments):
    return subprocess.check_output(
        ["git", "-C", str(source), *arguments], stderr=subprocess.STDOUT
    ).decode("utf-8").strip()


def verify_patches(manifest):
    require(manifest["defaultEnabled"] is False, "Unqualified backport must not be the default.")
    require(manifest["runtimeQualified"] is False, "Runtime qualification requires a separate reviewed change.")
    require(re.fullmatch(r"[0-9a-f]{40}", manifest["commit"]), "Expected a full source commit.")
    private_version = re.fullmatch(r"(\d+\.\d+\.\d+)-fluent-a11y\.\d+", manifest["packageVersionPrefix"])
    require(private_version is not None, "Expected a distinct fluent-a11y prerelease package identity.")
    require(tuple(map(int, private_version[1].split("."))) > tuple(map(int, manifest["release"].split("."))),
            "The private version must sort above the released dependency minimum.")
    sdk = load_sdk_pin()
    require(sdk["baselinePackage"]["version"] == manifest["unoSdk"], "SDK baseline does not match the runtime pin.")
    require(file_hash(HERE / sdk["baselinePackage"]["manifestPath"]) == sdk["baselinePackage"]["manifestSha256"],
            "Released SDK dependency manifest checksum mismatch.")
    for patch in source_patches(manifest):
        require(file_hash(source_path(HERE, patch["path"])) == patch["sha256"],
                f"Patch checksum mismatch: {patch['path']}")
        require(patch["files"], f"Missing source hashes: {patch['path']}")


def verify_source(source, manifest):
    verify_patches(manifest)
    require(git(source, "rev-parse", "HEAD") == manifest["commit"], "Source HEAD is not the pinned Uno commit.")
    require(git(source, "remote", "get-url", "origin") == manifest["repository"], "Unexpected Uno source origin.")
    expected = set()
    for patch in source_patches(manifest):
        for item in patch["files"]:
            expected.add(item["path"])
            require(file_hash(source_path(source, item["path"]), text=True) == item["sha256"],
                    f"Patched source checksum mismatch: {item['path']}")
    changed = set(git(source, "diff", "--name-only", "HEAD").splitlines())
    untracked = set(git(source, "ls-files", "--others", "--exclude-standard").splitlines())
    require((changed | untracked) <= expected, f"Unexpected source changes: {sorted((changed | untracked) - expected)}")
    git(source, "diff", "--check")


def prepare_source(source, manifest):
    verify_patches(manifest)
    source = owned_path(source)
    if not source.exists():
        source.mkdir(parents=True)
        subprocess.run(["git", "init", "--quiet", str(source)], check=True)
        git(source, "remote", "add", "origin", manifest["repository"])
        git(source, "config", "core.longpaths", "true")
        git(source, "config", "core.autocrlf", "false")
        git(source, "fetch", "--depth", "1", "--filter=blob:none", "origin", manifest["commit"])
        git(source, "sparse-checkout", "init", "--cone")
        git(source, "sparse-checkout", "set", "src", "build")
        git(source, "checkout", "--detach", manifest["commit"])
    require(git(source, "rev-parse", "HEAD") == manifest["commit"], "Refusing to reuse another source revision.")
    for patch in source_patches(manifest):
        patched = all(
            source_path(source, item["path"]).is_file()
            and file_hash(source_path(source, item["path"]), text=True) == item["sha256"]
            for item in patch["files"]
        )
        if not patched:
            paths = [item["path"] for item in patch["files"]]
            require(not git(source, "status", "--porcelain", "--", *paths),
                    f"Patch inputs are neither pristine nor pinned: {patch['path']}")
            path = str(source_path(HERE, patch["path"]))
            git(source, "apply", "--check", "--whitespace=error-all", path)
            git(source, "apply", "--whitespace=error-all", path)
    verify_source(source, manifest)


def refresh_hashes(source, manifest):
    """Explicit maintainer operation, never called by prepare/build/verification."""
    require(git(source, "rev-parse", "HEAD") == manifest["commit"], "Wrong base commit.")
    sdk = load_sdk_pin()
    for patch in manifest["patches"] + [sdk["patch"]]:
        path = source_path(HERE, patch["path"])
        patch["sha256"] = file_hash(path)
        paths = re.findall(r"^\+\+\+ b/(.+)$", path.read_text(encoding="utf-8"), re.MULTILINE)
        patch["files"] = [
            {"path": name, "sha256": file_hash(source_path(source, name), text=True)}
            for name in paths
        ]
    (HERE / "backport.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
    (HERE / "sdk-manifest.json").write_text(json.dumps(sdk, indent=2) + "\n", encoding="utf-8", newline="\n")


def sdk_dependency_manifest(manifest, platform, *, built=False):
    sdk = load_sdk_pin()
    groups = json.loads((HERE / sdk["baselinePackage"]["manifestPath"]).read_text(encoding="utf-8-sig"))
    core = next(group for group in groups if group["group"] == "Core")
    require(core["version"] == manifest["release"] and not core.get("versionOverride"), "Unexpected SDK Core baseline.")
    changed = set(manifest["platforms"][platform]["packages"])
    require(changed <= set(core["packages"]), "Backported packages are not SDK Core dependencies.")
    unchanged = dict(core, group="BackportBaselineCore", packages=[name for name in core["packages"] if name not in changed])
    core["packages"] = [name for name in core["packages"] if name in changed]
    core["version"] = version(manifest, platform) if built else "DefaultUnoVersion"
    groups.append(unchanged)
    return groups


def prepare_sdk(root, platform, manifest):
    verify_patches(manifest)
    directory = owned_path(root) / "sdk-input" / platform
    directory.mkdir(parents=True, exist_ok=True)
    (directory / "packages.json").write_text(json.dumps(sdk_dependency_manifest(manifest, platform), indent=2) + "\n",
                                            encoding="utf-8")


def sdk_path(root, platform, manifest):
    return owned_path(root) / "sdk" / platform / version(manifest, platform)


def verify_sdk_package(package, platform, manifest):
    with zipfile.ZipFile(package) as archive:
        names = archive.namelist()
        require(len(names) == len(set(names)), "Duplicate private SDK package entries.")
        specs = [name for name in names if name.endswith(".nuspec")]
        require(len(specs) == 1, "Invalid private SDK nuspec count.")
        metadata = ET.fromstring(archive.read(specs[0])).find("{*}metadata")
        require(metadata is not None and metadata.findtext("{*}id") == "Uno.Sdk.Private", "Wrong private SDK package id.")
        require(metadata.findtext("{*}version") == version(manifest, platform), "Wrong private SDK version.")
        repository = metadata.find("{*}repository")
        require(repository is not None and repository.get("commit") == manifest["commit"], "Wrong private SDK source commit.")
        actual = json.loads(archive.read("targets/netstandard2.0/packages.json"))
        require(actual == sdk_dependency_manifest(manifest, platform, built=True), "Private SDK changed unrelated dependencies.")
        props = ET.fromstring(archive.read("Sdk/Sdk.props"))
        require(props.findtext(".//{*}UnoVersion") == version(manifest, platform), "Private SDK runtime identity differs.")
        require(props.findtext(".//{*}UnoSdkVersion") == f"{version(manifest, platform)}-Private", "Private SDK identity differs.")
        guard = ET.fromstring(archive.read("targets/Uno.Build.targets")).find("{*}Target[@Name='UnoSdkVersionCheck']")
        require(guard is not None, "The SDK version guard must not be removed.")
        errors = {item.get("Code"): item.get("Condition", "").strip() for item in guard.findall("{*}Error")}
        require(errors.get("UNOB0004") == f"'$(UnoVersion)' != '{version(manifest, platform)}'",
                "Private SDK property version guard was weakened.")
        require(errors.get("UNOB0005") == f"$(_UnoSdkVersionCheck_Resolved_UnoWinUI) != '{version(manifest, platform)}'",
                "Private SDK resolved package version guard was weakened.")
        return {name: sha256(archive.read(name)) for name in names if not name.endswith("/")}


def seal_sdk(source, root, platform, manifest):
    verify_source(source, manifest)
    package = owned_path(root) / "sdk-feed" / platform / f"Uno.Sdk.Private.{version(manifest, platform)}.nupkg"
    files = verify_sdk_package(package, platform, manifest)
    compiled = source / "src" / "Uno.Sdk" / "bin" / "Release" / "netstandard2.0" / "Uno.Sdk_v0.dll"
    require(file_hash(compiled) == files["targets/netstandard2.0/Uno.Sdk_v0.dll"], "Private SDK is not the source build.")
    destination = sdk_path(root, platform, manifest)
    require(not destination.exists(), "Refusing to overwrite an existing materialized SDK identity.")
    with zipfile.ZipFile(package) as archive:
        for name in files:
            parts = PurePosixPath(name)
            require(not parts.is_absolute() and ".." not in parts.parts and "\\" not in name, "Unsafe SDK package path.")
            target = destination.joinpath(*parts.parts)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.read(name))
    receipt = {"commit": manifest["commit"], "version": version(manifest, platform),
               "manifestSha256": file_hash(HERE / "backport.json"), "sdkPinSha256": file_hash(HERE / "sdk-manifest.json"),
               "packageSha256": file_hash(package), "files": files, "runtimeQualified": False}
    (destination / "build-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")


def verify_sdk(root, platform, manifest):
    verify_patches(manifest)
    destination = sdk_path(root, platform, manifest)
    receipt = json.loads((destination / "build-receipt.json").read_text(encoding="utf-8"))
    require(receipt["manifestSha256"] == file_hash(HERE / "backport.json")
            and receipt["sdkPinSha256"] == file_hash(HERE / "sdk-manifest.json"), "Stale private SDK inputs.")
    require(receipt["commit"] == manifest["commit"] and receipt["version"] == version(manifest, platform),
            "Wrong materialized private SDK identity.")
    package = owned_path(root) / "sdk-feed" / platform / f"Uno.Sdk.Private.{version(manifest, platform)}.nupkg"
    require(file_hash(package) == receipt["packageSha256"], "Private SDK package checksum mismatch.")
    require(verify_sdk_package(package, platform, manifest) == receipt["files"], "Private SDK receipt differs.")
    for name, digest in receipt["files"].items():
        require(file_hash(source_path(destination, name)) == digest, f"Materialized SDK file changed: {name}")


def keep_package_file(target, platform, package_framework):
    parts = PureWindowsPath(target.replace("/", "\\")).parts
    if not parts:
        return False
    if parts[0] in ("lib", "ref"):
        return len(parts) > 1 and parts[1] == package_framework
    if parts[0] == "uno-runtime":
        return platform in ("desktop", "browserwasm") and parts[1:3] == ("net10.0", "skia")
    if parts[0] == "buildTransitive" and len(parts) > 1:
        if parts[1] == "Uno.UI.SourceGenerators.WinAppSDK":
            return False
        if parts[1].startswith(("net", "uap")):
            # The upstream net10.0 package uses net9.0 build props/targets.
            return parts[1] == package_framework or (platform in ("desktop", "browserwasm") and parts[1] == "net9.0")
    return True


def upstream_file_pattern(base, value):
    """Resolve upstream nuspec casing (Bin/bin) without changing its file layout."""
    current = Path(base)
    parts = PureWindowsPath(value).parts
    for index, part in enumerate(parts):
        if part == "..":
            current = current.parent
        elif "*" in part or "?" in part:
            return str(current.joinpath(*parts[index:]))
        else:
            candidate = current / part
            if not candidate.exists() and current.is_dir():
                matches = [item for item in current.iterdir() if item.name.lower() == part.lower()]
                require(len(matches) <= 1, f"Ambiguous upstream package path: {candidate}")
                if matches:
                    candidate = matches[0]
            current = candidate
    return str(current)


def prepare_pack(source, work, platform, manifest):
    verify_source(source, manifest)
    work = owned_path(work)
    official = work / "official" / "build"
    official.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source / "build" / "Uno.UI.Build.csproj", official)
    (official / "nuget").mkdir(exist_ok=True)
    for path in (source / "build" / "nuget").glob("*.nuspec"):
        shutil.copy2(path, official / "nuget" / path.name)
    # Use Uno's own dependency/identity preparation, on packaging-only copies.
    subprocess.run([
        "dotnet", "msbuild", str(official / "Uno.UI.Build.csproj"),
        "-t:PrepareNuGetPackage", "-p:CombinedConfiguration=Release|AnyCPU",
        f"-p:NBGV_SemVer2={manifest['release']}", "-p:UNO_UWP_BUILD=false",
        "-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false",
        "-p:ImportDirectoryPackagesProps=false", "-p:ManagePackageVersionsCentrally=false",
        "-v:minimal", "-nologo",
    ], check=True)
    tree = ET.parse(official / "nuget" / "Uno.WinUI.nuspec")
    metadata = tree.find("n:metadata", NS)
    metadata.find("n:version", NS).text = version(manifest, platform)
    metadata.find("n:repository", NS).set("branch", "fluent-accessibility-backport")
    metadata.find("n:repository", NS).set("commit", manifest["commit"])
    framework = manifest["platforms"][platform]["packageFramework"]
    for section in ("dependencies", "references"):
        element = metadata.find(f"n:{section}", NS)
        for group in list(element):
            if group.get("targetFramework") != framework:
                element.remove(group)
    files = tree.find("n:files", NS)
    for item in list(files):
        if not keep_package_file(item.get("target", ""), platform, framework):
            files.remove(item)
        else:
            item.set("src", upstream_file_pattern(source / "build" / "nuget", item.get("src")))
    for name in ("backport.json", "LICENSE.uno", "THIRD-PARTY-NOTICES.uno"):
        ET.SubElement(files, f"{{{NUSPEC_NS}}}file", {"src": str(HERE / name), "target": "backport"})
    ET.register_namespace("", NUSPEC_NS)
    tree.write(work / "Uno.WinUI.nuspec", encoding="utf-8", xml_declaration=True)


def verify_package(path, package_id, platform, manifest):
    expected_version = version(manifest, platform)
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        require(len(names) == len(set(names)), f"Duplicate package entries: {path}")
        nuspecs = [name for name in names if name.endswith(".nuspec")]
        require(len(nuspecs) == 1, f"Expected exactly one nuspec: {path}")
        tree = ET.fromstring(archive.read(nuspecs[0]))
        require(re.fullmatch(r"\{http://schemas\.microsoft\.com/packaging/\d{4}/\d{2}/nuspec\.xsd\}package", tree.tag),
                f"Unexpected package schema: {path}")
        metadata = tree.find("{*}metadata")
        require(metadata is not None, f"Missing package metadata: {path}")
        require(metadata.findtext("{*}id") == package_id, f"Wrong package id: {path}")
        require(metadata.findtext("{*}version") == expected_version, f"Wrong package version: {path}")
        repository = metadata.find("{*}repository")
        require(repository is not None and repository.get("commit") == manifest["commit"],
                f"Package does not identify the pinned commit: {path}")
        framework = manifest["platforms"][platform]["packageFramework"]
        if package_id == "Uno.WinUI":
            for assembly in ("Uno.UI", "Uno.UI.Composition", "Uno.UI.Toolkit", "Uno.Xaml",
                             "Uno.UI.FluentTheme", "Uno.UI.FluentTheme.v1", "Uno.UI.FluentTheme.v2"):
                require(f"lib/{framework}/{assembly}.dll" in names, f"Missing reference/native assembly {assembly}")
            runtime_entry = (f"uno-runtime/net10.0/skia/Uno.UI.dll" if platform in ("desktop", "browserwasm")
                             else f"lib/{framework}/Uno.UI.dll")
            require(runtime_entry in names, "Core package is missing the actual patched runtime.")
            require(archive.read("backport/backport.json") == (HERE / "backport.json").read_bytes(),
                    "Package patch provenance differs from the repository pin.")
            for name in names:
                if name.startswith(("lib/", "ref/")):
                    require(name.split("/")[1] == framework, f"Unexpected platform asset: {name}")
                if name.startswith("uno-runtime/"):
                    require(platform in ("desktop", "browserwasm") and name.startswith("uno-runtime/net10.0/skia/"),
                            f"Unexpected runtime asset: {name}")
        else:
            runtime_entry = "lib/net10.0/Uno.UI.Runtime.Skia.WebAssembly.Browser.dll"
            require(runtime_entry in names, "Browser package is missing its patched managed/JS runtime.")
        reference_entry = f"lib/{framework}/Uno.UI.dll" if package_id == "Uno.WinUI" else runtime_entry
        return {"id": package_id, "file": path.name, "sha256": file_hash(path),
                "runtimeEntry": runtime_entry, "runtimeSha256": sha256(archive.read(runtime_entry)),
                "referenceEntry": reference_entry, "referenceSha256": sha256(archive.read(reference_entry))}


def seal_feed(source, root, platform, manifest):
    verify_source(source, manifest)
    root = owned_path(root)
    feed = root / "feed" / platform
    expected_ids = manifest["platforms"][platform]["packages"]
    packages = []
    require(len(list(feed.glob("*.nupkg"))) == len(expected_ids), "Unexpected packages in the private feed.")
    for package_id in expected_ids:
        path = feed / f"{package_id}.{version(manifest, platform)}.nupkg"
        result = verify_package(path, package_id, platform, manifest)
        if package_id == "Uno.WinUI":
            variant = "Skia" if platform in ("desktop", "browserwasm") else "netcoremobile"
            tfm = "net10.0-ios26.0" if platform == "ios" else manifest["platforms"][platform]["sourceFramework"]
            compiled = source / "src" / "Uno.UI" / "bin" / f"Uno.UI.{variant}" / "Release" / tfm / "Uno.UI.dll"
        else:
            compiled = source / "src" / "Uno.UI.Runtime.Skia.WebAssembly.Browser" / "bin" / "Release" / "net10.0" / "Uno.UI.Runtime.Skia.WebAssembly.Browser.dll"
        require(file_hash(compiled) == result["runtimeSha256"], f"Packaged runtime is not the source build: {package_id}")
        packages.append(result)
    receipt = {"commit": manifest["commit"], "manifestSha256": file_hash(HERE / "backport.json"),
               "platform": platform, "version": version(manifest, platform), "packages": packages,
               "runtimeQualified": False}
    (feed / "build-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    configuration = ET.Element("configuration")
    sources = ET.SubElement(configuration, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", {"key": "uno-backport", "value": str(feed)})
    ET.SubElement(sources, "add", {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json", "protocolVersion": "3"})
    mapping = ET.SubElement(configuration, "packageSourceMapping")
    ET.SubElement(mapping, "clear")
    private = ET.SubElement(mapping, "packageSource", {"key": "uno-backport"})
    for package_id in expected_ids:
        ET.SubElement(private, "package", {"pattern": package_id})
    public = ET.SubElement(mapping, "packageSource", {"key": "nuget.org"})
    ET.SubElement(public, "package", {"pattern": "*"})
    ET.indent(configuration)
    ET.ElementTree(configuration).write(feed / "NuGet.Config", encoding="utf-8", xml_declaration=True)


def verify_feed(root, platform, manifest):
    verify_patches(manifest)
    feed = owned_path(root) / "feed" / platform
    receipt = json.loads((feed / "build-receipt.json").read_text(encoding="utf-8"))
    require(receipt["manifestSha256"] == file_hash(HERE / "backport.json"), "Stale feed: patch pin changed.")
    require(receipt["commit"] == manifest["commit"] and receipt["version"] == version(manifest, platform)
            and receipt["platform"] == platform, "Wrong backport feed identity.")
    require(len(receipt["packages"]) == len(manifest["platforms"][platform]["packages"])
            and {item["id"] for item in receipt["packages"]} == set(manifest["platforms"][platform]["packages"]),
            "Incomplete backport feed.")
    for item in receipt["packages"]:
        expected = f"{item['id']}.{version(manifest, platform)}.nupkg"
        require(item["file"] == expected, "Unexpected package filename.")
        require(file_hash(feed / expected) == item["sha256"], f"Package checksum mismatch: {expected}")
        require(verify_package(feed / expected, item["id"], platform, manifest) == item, "Package provenance mismatch.")
    return receipt


def verify_assets(assets_path, root, platform, manifest, require_browser):
    receipt = verify_feed(root, platform, manifest)
    assets = json.loads(Path(assets_path).read_text(encoding="utf-8"))
    expected_folder = owned_path(root) / "consumer-packages" / platform
    require({Path(folder).resolve() for folder in assets["packageFolders"]} == {expected_folder},
            "Consumer restore escaped its isolated package cache.")
    wanted = version(manifest, platform)
    expected_ids = manifest["platforms"][platform]["packages"]
    for package_id in expected_ids:
        keys = [key for key in assets["libraries"] if key.split("/")[0].lower() == package_id.lower()]
        if package_id != "Uno.WinUI" and not require_browser and not keys:
            continue
        require(keys == [f"{package_id}/{wanted}"], f"Consumer did not resolve the pinned {package_id}: {keys}")
        package = next(item for item in receipt["packages"] if item["id"] == package_id)
        cached = expected_folder / package_id.lower() / wanted / f"{package_id.lower()}.{wanted}.nupkg"
        require(file_hash(cached) == package["sha256"], f"Consumer cache contains another {package_id} build.")
        for entry, digest in (("runtimeEntry", "runtimeSha256"), ("referenceEntry", "referenceSha256")):
            extracted = source_path(cached.parent, package[entry])
            require(file_hash(extracted) == package[digest], f"Consumer extracted assembly differs from the pinned package: {extracted}")
    require(not any(log.get("level") == "Error" for log in assets.get("logs", [])), "Restore reported errors.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("prepare", "verify-source", "refresh-hashes", "prepare-pack",
                                              "seal-feed", "verify-feed", "verify-assets",
                                              "prepare-sdk", "seal-sdk", "verify-sdk"))
    parser.add_argument("--source", type=Path, default=REPO / "artifacts" / "UnoBackport" / "source")
    parser.add_argument("--root", type=Path, default=REPO / "artifacts" / "UnoBackport")
    parser.add_argument("--platform", choices=("desktop", "browserwasm", "android", "ios"), default="browserwasm")
    parser.add_argument("--assets", type=Path)
    parser.add_argument("--require-browser-runtime", action="store_true")
    arguments = parser.parse_args()
    manifest = load_manifest()
    source = owned_path(arguments.source)
    root = owned_path(arguments.root)
    operation = arguments.operation
    if operation == "prepare":
        prepare_source(source, manifest)
    elif operation == "verify-source":
        verify_source(source, manifest)
    elif operation == "refresh-hashes":
        refresh_hashes(source, manifest)
    elif operation == "prepare-pack":
        prepare_pack(source, root / "pack" / arguments.platform, arguments.platform, manifest)
    elif operation == "seal-feed":
        seal_feed(source, root, arguments.platform, manifest)
    elif operation == "verify-feed":
        verify_feed(root, arguments.platform, manifest)
    elif operation == "prepare-sdk":
        prepare_sdk(root, arguments.platform, manifest)
    elif operation == "seal-sdk":
        seal_sdk(source, root, arguments.platform, manifest)
    elif operation == "verify-sdk":
        verify_sdk(root, arguments.platform, manifest)
    else:
        require(arguments.assets is not None, "--assets is required")
        verify_assets(arguments.assets, root, arguments.platform, manifest, arguments.require_browser_runtime)
    print(f"{operation}: passed ({arguments.platform}); runtime qualification is not implied")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, subprocess.CalledProcessError, KeyError, ET.ParseError, zipfile.BadZipFile) as error:
        print(f"Uno backport: {error}", file=sys.stderr)
        sys.exit(1)
