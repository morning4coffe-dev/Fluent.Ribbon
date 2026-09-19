import copy
import json
from pathlib import Path
import shutil
import subprocess
import sys
import unittest
import uuid
import xml.etree.ElementTree as ET
import zipfile


HERE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(HERE))
import backport


class BackportContracts(unittest.TestCase):
    def setUp(self):
        self.pin = backport.load_manifest()
        self.root = backport.REPO / "artifacts" / "UnoBackport" / "contract-tests" / uuid.uuid4().hex
        self.root.mkdir(parents=True)

    def tearDown(self):
        shutil.rmtree(self.root)

    def make_package(self, package_id, *, platform="browserwasm", missing_runtime=False, extra=None,
                     namespace=backport.NUSPEC_NS):
        feed = self.root / "feed" / platform
        feed.mkdir(parents=True, exist_ok=True)
        package_version = backport.version(self.pin, platform)
        path = feed / f"{package_id}.{package_version}.nupkg"
        metadata = (
            f'<package xmlns="{namespace}"><metadata>'
            f"<id>{package_id}</id><version>{package_version}</version>"
            f'<repository type="git" commit="{self.pin["commit"]}" />'
            "</metadata></package>"
        )
        framework = self.pin["platforms"][platform]["packageFramework"]
        entries = {f"{package_id}.nuspec": metadata.encode()}
        if package_id == "Uno.WinUI":
            for assembly in ("Uno.UI", "Uno.UI.Composition", "Uno.UI.Toolkit", "Uno.Xaml",
                             "Uno.UI.FluentTheme", "Uno.UI.FluentTheme.v1", "Uno.UI.FluentTheme.v2"):
                entries[f"lib/{framework}/{assembly}.dll"] = f"contract fixture {assembly}".encode()
            entries["backport/backport.json"] = (HERE / "backport.json").read_bytes()
            runtime = ("uno-runtime/net10.0/skia/Uno.UI.dll" if platform in ("desktop", "browserwasm")
                       else f"lib/{framework}/Uno.UI.dll")
        else:
            runtime = "lib/net10.0/Uno.UI.Runtime.Skia.WebAssembly.Browser.dll"
        entries[runtime] = b"contract fixture; not an executable runtime"
        if missing_runtime:
            entries.pop(runtime)
        entries.update(extra or {})
        with zipfile.ZipFile(path, "w") as archive:
            for name, content in entries.items():
                archive.writestr(name, content)
        return path

    def make_feed_and_assets(self):
        packages = []
        libraries = {}
        cache = self.root / "consumer-packages" / "browserwasm"
        wanted = backport.version(self.pin, "browserwasm")
        for package_id in self.pin["platforms"]["browserwasm"]["packages"]:
            path = self.make_package(package_id)
            packages.append(backport.verify_package(path, package_id, "browserwasm", self.pin))
            libraries[f"{package_id}/{wanted}"] = {"type": "package"}
            folder = cache / package_id.lower() / wanted
            folder.mkdir(parents=True)
            shutil.copy2(path, folder / f"{package_id.lower()}.{wanted}.nupkg")
            with zipfile.ZipFile(path) as archive:
                archive.extractall(folder)
        receipt = {
            "commit": self.pin["commit"], "manifestSha256": backport.file_hash(HERE / "backport.json"),
            "platform": "browserwasm", "version": wanted, "packages": packages, "runtimeQualified": False,
        }
        (self.root / "feed" / "browserwasm" / "build-receipt.json").write_text(json.dumps(receipt), encoding="utf-8")
        assets = {"packageFolders": {str(cache): {}}, "libraries": libraries}
        assets_path = self.root / "project.assets.json"
        assets_path.write_text(json.dumps(assets), encoding="utf-8")
        return assets, assets_path

    def make_sdk(self, platform="browserwasm", *, weaken_guard=False):
        wanted = backport.version(self.pin, platform)
        folder = self.root / "sdk-feed" / platform
        folder.mkdir(parents=True, exist_ok=True)
        path = folder / f"Uno.Sdk.Private.{wanted}.nupkg"
        props = ET.Element("Project", {"xmlns": "http://schemas.microsoft.com/developer/msbuild/2003"})
        group = ET.SubElement(props, "PropertyGroup")
        ET.SubElement(group, "UnoVersion").text = wanted
        ET.SubElement(group, "UnoSdkVersion").text = f"{wanted}-Private"
        targets = ET.Element("Project")
        guard = ET.SubElement(targets, "Target", {"Name": "UnoSdkVersionCheck"})
        ET.SubElement(guard, "Error", {"Code": "UNOB0004", "Condition": f"'$(UnoVersion)' != '{wanted}'"})
        ET.SubElement(guard, "Error", {
            "Code": "UNOB0005",
            "Condition": "false" if weaken_guard else f"$(_UnoSdkVersionCheck_Resolved_UnoWinUI) != '{wanted}'",
        })
        entries = {
            "Uno.Sdk.Private.nuspec": (
                f'<package xmlns="{backport.NUSPEC_NS}"><metadata><id>Uno.Sdk.Private</id>'
                f'<version>{wanted}</version><repository commit="{self.pin["commit"]}" /></metadata></package>'
            ).encode(),
            "Sdk/Sdk.props": ET.tostring(props),
            "targets/Uno.Build.targets": ET.tostring(targets),
            "targets/netstandard2.0/packages.json": json.dumps(backport.sdk_dependency_manifest(self.pin, platform, built=True)).encode(),
            "targets/netstandard2.0/Uno.Sdk_v0.dll": b"metadata fixture; never loaded as a task assembly",
        }
        with zipfile.ZipFile(path, "w") as archive:
            for name, value in entries.items():
                archive.writestr(name, value)
        if weaken_guard:
            return path
        files = backport.verify_sdk_package(path, platform, self.pin)
        destination = backport.sdk_path(self.root, platform, self.pin)
        with zipfile.ZipFile(path) as archive:
            archive.extractall(destination)
        receipt = {
            "commit": self.pin["commit"], "version": wanted,
            "manifestSha256": backport.file_hash(HERE / "backport.json"),
            "sdkPinSha256": backport.file_hash(HERE / "sdk-manifest.json"),
            "packageSha256": backport.file_hash(path), "files": files, "runtimeQualified": False,
        }
        (destination / "build-receipt.json").write_text(json.dumps(receipt), encoding="utf-8")
        return path

    def test_patch_hashes_and_explicit_unqualified_status(self):
        backport.verify_patches(self.pin)
        self.assertFalse(self.pin["defaultEnabled"])
        self.assertFalse(self.pin["runtimeQualified"])
        self.assertEqual("6.6.42", self.pin["unoSdk"])
        self.assertEqual("c29204c9b2cb5798dbdc023b23a25b3f6d87c600", self.pin["commit"])

    def test_realization_and_factory_share_transformed_semantic_bounds(self):
        patch = (HERE / "patches" / "browser-accessibility.patch").read_text(encoding="utf-8")
        additions = "\n".join(line[1:] for line in patch.splitlines() if line.startswith("+") and not line.startswith("+++"))
        self.assertIn("GetSemanticBounds(UIElement element, UIElement? semanticParent)", additions)
        self.assertIn("UIElement.GetTransform(from: element, to: semanticParent).Transform(localRect)", additions)
        self.assertIn("GetSemanticBounds(item, subscription.Container)", additions)
        self.assertIn("GetSemanticBounds(child, FindUIElementByHandle(child, parentHandle))", additions)
        self.assertIn("(float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height", additions)
        self.assertNotIn("offset.X, offset.Y, item.Visual.Size.X, item.Visual.Size.Y", additions)

    def test_changed_patch_hash_is_rejected(self):
        changed = copy.deepcopy(self.pin)
        changed["patches"][0]["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            backport.verify_patches(changed)

    def test_unqualified_backport_cannot_be_marked_default(self):
        changed = copy.deepcopy(self.pin)
        changed["defaultEnabled"] = True
        with self.assertRaisesRegex(ValueError, "must not be the default"):
            backport.verify_patches(changed)

    def test_platform_package_versions_are_distinct_and_not_release_identities(self):
        versions = {backport.version(self.pin, platform) for platform in self.pin["platforms"]}
        self.assertEqual(4, len(versions))
        self.assertNotIn(self.pin["release"], versions)
        props = ET.parse(HERE / "Version.props")
        self.assertEqual(self.pin["packageVersionPrefix"], props.find(".//_FluentUnoBackportVersionPrefix").text)

    def test_private_prereleases_cannot_downgrade_the_released_runtime(self):
        changed = copy.deepcopy(self.pin)
        changed["packageVersionPrefix"] = f"{self.pin['release']}-fluent-a11y.1"
        with self.assertRaisesRegex(ValueError, "sort above"):
            backport.verify_patches(changed)

    def test_output_paths_cannot_escape_owned_artifacts(self):
        with self.assertRaisesRegex(ValueError, "must be below"):
            backport.owned_path(backport.REPO / "bin")
        with self.assertRaisesRegex(ValueError, "must be below"):
            backport.owned_path(self.root / ".." / ".." / ".." / ".." / "Shared")

    def test_browser_nuspec_keeps_reference_runtime_and_framework_build_assets(self):
        for name in ("lib\\net10.0", "uno-runtime\\net10.0\\skia", "buildTransitive\\net9.0",
                     "buildTransitive\\Uno.UI.Tasks", "analyzers/dotnet/cs"):
            self.assertTrue(backport.keep_package_file(name, "browserwasm", "net10.0"), name)
        for name in ("lib\\net9.0", "lib\\net10.0-ios26.0", "uno-runtime\\net10.0\\wasm",
                     "buildTransitive\\net10.0-windows", "buildTransitive\\Uno.UI.SourceGenerators.WinAppSDK"):
            self.assertFalse(backport.keep_package_file(name, "browserwasm", "net10.0"), name)

    def test_native_nuspec_never_carries_skia_or_another_platform(self):
        for platform in ("android", "ios"):
            tfm = self.pin["platforms"][platform]["packageFramework"]
            self.assertTrue(backport.keep_package_file(f"lib\\{tfm}", platform, tfm))
            self.assertFalse(backport.keep_package_file("lib\\net10.0", platform, tfm))
            self.assertFalse(backport.keep_package_file("uno-runtime\\net10.0\\skia", platform, tfm))

    def test_desktop_has_a_real_skia_implementation_without_browser_or_native_winui_assets(self):
        path = self.make_package("Uno.WinUI", platform="desktop")
        result = backport.verify_package(path, "Uno.WinUI", "desktop", self.pin)
        self.assertEqual("uno-runtime/net10.0/skia/Uno.UI.dll", result["runtimeEntry"])
        self.assertEqual(["Uno.WinUI"], self.pin["platforms"]["desktop"]["packages"])
        self.assertTrue(backport.keep_package_file("uno-runtime\\net10.0\\skia", "desktop", "net10.0"))
        self.assertFalse(backport.keep_package_file("uno-runtime\\net10.0\\wasm", "desktop", "net10.0"))
        self.assertFalse(backport.keep_package_file("lib\\net10.0-windows", "desktop", "net10.0"))

    def test_desktop_reference_only_package_is_rejected(self):
        path = self.make_package("Uno.WinUI", platform="desktop", missing_runtime=True)
        with self.assertRaisesRegex(ValueError, "actual patched runtime"):
            backport.verify_package(path, "Uno.WinUI", "desktop", self.pin)

    def test_upstream_nuspec_casing_is_resolved_without_changing_layout(self):
        (self.root / "bin" / "Release").mkdir(parents=True)
        (self.root / "bin" / "Release" / "Uno.UI.dll").write_bytes(b"fixture")
        actual = backport.upstream_file_pattern(self.root, "Bin\\Release\\Uno.UI.dll")
        self.assertEqual(self.root / "bin" / "Release" / "Uno.UI.dll", Path(actual))

    def test_a_reference_only_package_cannot_satisfy_browser_runtime_gate(self):
        path = self.make_package("Uno.WinUI", missing_runtime=True)
        with self.assertRaisesRegex(ValueError, "actual patched runtime"):
            backport.verify_package(path, "Uno.WinUI", "browserwasm", self.pin)

    def test_nuget_normalized_nuspec_namespace_retains_strict_identity_checks(self):
        path = self.make_package("Uno.WinUI", namespace="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")
        result = backport.verify_package(path, "Uno.WinUI", "browserwasm", self.pin)
        self.assertEqual("Uno.WinUI", result["id"])
        with self.assertRaisesRegex(ValueError, "Wrong package id"):
            backport.verify_package(path, "Uno.WinUI.Runtime.Skia.WebAssembly.Browser", "browserwasm", self.pin)

    def test_wrong_platform_assets_are_rejected(self):
        path = self.make_package("Uno.WinUI", extra={"lib/net10.0-ios26.0/Uno.UI.dll": b"wrong platform"})
        with self.assertRaisesRegex(ValueError, "Unexpected platform asset"):
            backport.verify_package(path, "Uno.WinUI", "browserwasm", self.pin)

    def test_provenance_and_cache_hashes_are_required(self):
        _, path = self.make_feed_and_assets()
        backport.verify_assets(path, self.root, "browserwasm", self.pin, True)
        cached = next((self.root / "consumer-packages").rglob("*.nupkg"))
        cached.write_bytes(b"replaced")
        with self.assertRaisesRegex(ValueError, "cache contains another"):
            backport.verify_assets(path, self.root, "browserwasm", self.pin, True)

    def test_cache_assembly_replacement_is_rejected_even_with_an_unchanged_nupkg(self):
        _, path = self.make_feed_and_assets()
        runtime = next((self.root / "consumer-packages" / "browserwasm").glob("uno.winui/*/uno-runtime/net10.0/skia/Uno.UI.dll"))
        runtime.write_bytes(b"replaced extracted runtime")
        with self.assertRaisesRegex(ValueError, "extracted assembly differs"):
            backport.verify_assets(path, self.root, "browserwasm", self.pin, True)

    def test_released_runtime_cannot_silently_satisfy_backport_restore(self):
        assets, path = self.make_feed_and_assets()
        wanted = backport.version(self.pin, "browserwasm")
        assets["libraries"].pop(f"Uno.WinUI/{wanted}")
        assets["libraries"][f"Uno.WinUI/{self.pin['release']}"] = {"type": "package"}
        path.write_text(json.dumps(assets), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "did not resolve the pinned"):
            backport.verify_assets(path, self.root, "browserwasm", self.pin, True)

    def test_showcase_requires_the_patched_browser_exporter_too(self):
        assets, path = self.make_feed_and_assets()
        wanted = backport.version(self.pin, "browserwasm")
        assets["libraries"].pop(f"Uno.WinUI.Runtime.Skia.WebAssembly.Browser/{wanted}")
        path.write_text(json.dumps(assets), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "did not resolve the pinned"):
            backport.verify_assets(path, self.root, "browserwasm", self.pin, True)

    def test_shared_package_cache_is_not_an_implicit_fallback(self):
        assets, path = self.make_feed_and_assets()
        assets["packageFolders"][str(self.root / "shared-cache")] = {}
        path.write_text(json.dumps(assets), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "escaped its isolated package cache"):
            backport.verify_assets(path, self.root, "browserwasm", self.pin, True)

    def test_feed_is_rejected_after_the_patch_pin_changes(self):
        self.make_feed_and_assets()
        path = self.root / "feed" / "browserwasm" / "build-receipt.json"
        receipt = json.loads(path.read_text(encoding="utf-8"))
        receipt["manifestSha256"] = "0" * 64
        path.write_text(json.dumps(receipt), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Stale feed"):
            backport.verify_feed(self.root, "browserwasm", self.pin)

    def test_private_sdk_changes_only_the_backported_dependency_versions(self):
        baseline = json.loads((HERE / "released-sdk-packages.json").read_text(encoding="utf-8-sig"))
        def versions(groups):
            return {package: (group["version"], group.get("versionOverride")) for group in groups for package in group["packages"]}
        original = versions(baseline)
        for platform in self.pin["platforms"]:
            actual = versions(backport.sdk_dependency_manifest(self.pin, platform, built=True))
            self.assertEqual(original.keys(), actual.keys())
            for package, value in actual.items():
                if package in self.pin["platforms"][platform]["packages"]:
                    self.assertEqual((backport.version(self.pin, platform), None), value)
                else:
                    self.assertEqual(original[package], value, package)

    def test_private_sdk_version_checks_cannot_be_disabled(self):
        package = self.make_sdk(weaken_guard=True)
        with self.assertRaisesRegex(ValueError, "version guard was weakened"):
            backport.verify_sdk_package(package, "browserwasm", self.pin)

    def test_materialized_sdk_files_are_hash_verified(self):
        self.make_sdk()
        backport.verify_sdk(self.root, "browserwasm", self.pin)
        (backport.sdk_path(self.root, "browserwasm", self.pin) / "Sdk" / "Sdk.props").write_text("<Project />", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Materialized SDK file changed"):
            backport.verify_sdk(self.root, "browserwasm", self.pin)

    def msbuild_probe(self, platform, framework, target="UnoImplicitPackages", extra=()):
        # Real MSBuild target-ordering probe. This is not an Uno runtime test.
        if platform:
            self.make_sdk(platform)
        project = ET.Element("Project")
        properties = ET.SubElement(project, "PropertyGroup")
        for name, value in {
            "FluentUnoBackport": platform, "FluentUnoBackportRoot": str(self.root),
            "TargetFramework": framework, "Configuration": "Release",
            "UnoSdkVersion": f"{backport.version(self.pin, platform)}-Private" if platform else self.pin["unoSdk"],
        }.items():
            ET.SubElement(properties, name).text = value
        ET.SubElement(project, "Import", {"Project": str(HERE / "UnoBackport.props")})
        implicit = ET.SubElement(project, "Target", {"Name": "UnoImplicitPackages"})
        group = ET.SubElement(implicit, "ItemGroup")
        for name in ("Uno.WinUI", "Uno.WinUI.Runtime.Skia.WebAssembly.Browser", "Uno.Foundation.Logging"):
            ET.SubElement(group, "PackageReference", {"Include": name, "Version": self.pin["release"]})
        generate = ET.SubElement(project, "Target", {"Name": "GenerateNuspec"})
        ET.SubElement(generate, "WriteLinesToFile", {"File": str(self.root / "unqualified.nupkg"), "Lines": "must not be emitted"})
        ET.SubElement(project, "Target", {"Name": "Pack", "DependsOnTargets": "GenerateNuspec"})
        ET.SubElement(project, "Import", {"Project": str(HERE / "UnoBackport.targets")})
        path = self.root / "probe.proj"
        ET.ElementTree(project).write(path, encoding="utf-8")
        return subprocess.run(
            ["dotnet", "msbuild", str(path), "-nologo", "-v:quiet", f"-t:{target}", "-getItem:PackageReference", *extra],
            cwd=HERE, text=True, capture_output=True, timeout=60,
        )

    def test_msbuild_overrides_after_implicit_resolution_and_keeps_unmodified_packages(self):
        result = self.msbuild_probe("browserwasm", "net10.0-browserwasm")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        packages = {item["Identity"]: item for item in json.loads(result.stdout)["Items"]["PackageReference"]}
        wanted = f"[{backport.version(self.pin, 'browserwasm')}]"
        self.assertEqual(wanted, packages["Uno.WinUI"]["Version"])
        self.assertEqual(wanted, packages["Uno.WinUI"]["VersionOverride"])
        self.assertEqual(wanted, packages["Uno.WinUI.Runtime.Skia.WebAssembly.Browser"]["Version"])
        self.assertEqual(self.pin["release"], packages["Uno.Foundation.Logging"]["Version"])

    def test_msbuild_default_is_the_unmodified_release(self):
        result = self.msbuild_probe("", "net10.0-browserwasm")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        packages = json.loads(result.stdout)["Items"]["PackageReference"]
        self.assertTrue(all(item["Version"] == self.pin["release"] for item in packages))

    def test_msbuild_desktop_uses_only_the_desktop_core_identity(self):
        result = self.msbuild_probe("desktop", "net10.0-desktop")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        packages = {item["Identity"]: item for item in json.loads(result.stdout)["Items"]["PackageReference"]}
        self.assertEqual(f"[{backport.version(self.pin, 'desktop')}]", packages["Uno.WinUI"]["Version"])
        self.assertEqual(self.pin["release"], packages["Uno.WinUI.Runtime.Skia.WebAssembly.Browser"]["Version"])

    def test_msbuild_rejects_a_mixed_platform_or_override_identity(self):
        for platform, framework, extra in (
            ("browserwasm", "net10.0-desktop", ()),
            ("android", "net10.0-ios", ()),
            ("browserwasm", "net10.0-browserwasm", ("-p:FluentUnoBackportVersion=6.6.184",)),
        ):
            result = self.msbuild_probe(platform, framework, extra=extra)
            self.assertNotEqual(0, result.returncode)

    def test_msbuild_fails_closed_when_the_feed_has_not_been_built(self):
        result = self.msbuild_probe("browserwasm", "net10.0-browserwasm", "VerifyFluentUnoBackportFeed")
        self.assertNotEqual(0, result.returncode)
        self.assertIn("released packages are not a fallback", result.stdout + result.stderr)

    def test_msbuild_blocks_shipping_fluent_packages_with_unqualified_dependencies(self):
        result = self.msbuild_probe("browserwasm", "net10.0-browserwasm", "Pack")
        self.assertNotEqual(0, result.returncode)
        self.assertIn("Do not ship Fluent.Ribbon packages", result.stdout + result.stderr)
        self.assertFalse((self.root / "unqualified.nupkg").exists())

    def test_both_project_build_boundaries_import_the_opt_in_hooks_only_for_uno(self):
        boundaries = (backport.REPO, backport.REPO / "Fluent.Ribbon.Uno" / "Fluent.Ribbon.Uno.Showcase")
        for boundary in boundaries:
            for kind in ("props", "targets"):
                imports = ET.parse(boundary / f"Directory.Build.{kind}").findall("Import")
                hook = next(item for item in imports if item.get("Project", "").endswith(f"UnoBackport.{kind}"))
                self.assertIn("'$(UsingUnoSdk)' == 'true'", hook.get("Condition"))
                self.assertIn("'$(FluentUnoBackport)' != ''", hook.get("Condition"))

    def test_sdk_wrapper_keeps_default_resolution_and_removes_only_old_local_outputs(self):
        props = ET.parse(HERE / "Sdk.props")
        original = props.find("Import[@Sdk='Uno.Sdk']")
        self.assertEqual("'$(FluentUnoBackport)' == ''", original.get("Condition"))
        targets = ET.parse(HERE / "Sdk.targets")
        cleanup = targets.find("ItemGroup")
        self.assertEqual("'$(FluentUnoBackport)' != ''", cleanup.get("Condition"))
        self.assertEqual("obj\\**;bin\\**", cleanup.find("Compile").get("Remove"))


if __name__ == "__main__":
    unittest.main()
