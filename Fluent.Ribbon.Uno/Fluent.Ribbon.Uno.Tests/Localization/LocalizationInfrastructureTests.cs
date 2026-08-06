#nullable enable

namespace FluentUno.Tests.Localization;

using System.Collections.Generic;
using Fluent;
using Fluent.Automation.Peers;
using Fluent.Localization.Languages;
using Microsoft.UI.Xaml;
using NUnit.Framework;
using Windows.UI;

[TestFixture]
public sealed class LocalizationInfrastructureTests
{
    [Test]
    public void ComplexHeadersYieldVisibleText()
    {
        object[] header = ["Font", new object[] { "\uE710", "settings" }];

        Assert.That(AutomationPeerHelpers.GetObjectName(header), Is.EqualTo("Font settings"));
    }

    [Test]
    public void MeaningfulCustomStringsAreAccepted()
    {
        Assert.That(
            AutomationPeerHelpers.GetObjectName(new MeaningfulHeader()),
            Is.EqualTo("Custom header"));
    }

    [Test]
    public void TypeNamesAndGlyphOnlyStringsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutomationPeerHelpers.GetObjectName(new DefaultHeader()), Is.Empty);
            Assert.That(AutomationPeerHelpers.GetObjectName(typeof(string)), Is.Empty);
            Assert.That(AutomationPeerHelpers.GetObjectName("\uE710"), Is.Empty);
            Assert.That(AutomationPeerHelpers.GetObjectName(" \uE710\uE711 "), Is.Empty);
        });
    }

    [Test]
    public void GeneratedValuesStopUpdatingAfterApplicationOwnership()
    {
        var ownership = new AutomationPeerHelpers.GeneratedValueOwnership();
        var first = new string("Framework one".ToCharArray());
        var second = new string("Framework two".ToCharArray());

        Assert.Multiple(() =>
        {
            Assert.That(
                ownership.TryGenerate(null, DependencyProperty.UnsetValue, first),
                Is.True);
            ownership.CaptureLocalValue(first);
            Assert.That(
                ownership.TryGenerate(first, first, second),
                Is.True);
            ownership.CaptureLocalValue(second);
            Assert.That(
                ownership.TryGenerate(
                    "Application text",
                    "Application text",
                    "Framework three"),
                Is.False);
            Assert.That(ownership.IsOwned, Is.False);
        });
    }

    [Test]
    public void GeneratedNamesDoNotReplaceBindings()
    {
        var ownership = new AutomationPeerHelpers.GeneratedValueOwnership();
        var generated = new string("Bound name".ToCharArray());
        Assert.That(
            ownership.TryGenerate(
                null,
                DependencyProperty.UnsetValue,
                generated),
            Is.True);
        ownership.CaptureLocalValue(generated);

        Assert.That(
            ownership.TryGenerate(
                "Bound name",
                new string("Bound name".ToCharArray()),
                "Updated framework name"),
            Is.False);
    }

    [Test]
    public void GeneratedValuesDoNotReplaceStyleValues()
    {
        var ownership = new AutomationPeerHelpers.GeneratedValueOwnership();

        Assert.That(
            ownership.TryGenerate(
                "Styled value",
                DependencyProperty.UnsetValue,
                "Framework value"),
            Is.False);
    }

    [Test]
    public void NewLocalizationPropertiesFallBackToEnglish()
    {
        var english = new English();
        var german = new German();

        Assert.Multiple(() =>
        {
            Assert.That(german.ThemeColors, Is.EqualTo(english.ThemeColors));
            Assert.That(german.GalleryFilter, Is.EqualTo(english.GalleryFilter));
            Assert.That(german.RibbonSearchName, Is.EqualTo(english.RibbonSearchName));
            Assert.That(german.ColorDescriptionFormat, Is.EqualTo(english.ColorDescriptionFormat));
        });
    }

    [Test]
    public void ColorDescriptionsAreReadableAndAlphaDistinct()
    {
        var localization = new English();
        var opaque = AutomationPeerHelpers.GetColorDescription(
            Color.FromArgb(255, 12, 34, 56),
            localization);
        var transparent = AutomationPeerHelpers.GetColorDescription(
            Color.FromArgb(128, 12, 34, 56),
            localization);

        Assert.Multiple(() =>
        {
            Assert.That(opaque, Is.EqualTo("Red 12, green 34, blue 56"));
            Assert.That(transparent, Does.Contain("Alpha 128"));
            Assert.That(transparent, Is.Not.EqualTo(opaque));
            Assert.That(opaque, Does.Not.StartWith("#"));
        });
    }

    [Test]
    public void LocalizationRefreshRecognizesCultureAndInstanceChanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                RibbonLocalizationUpdateHelper.IsLocalizationChange(
                    nameof(RibbonLocalization.Culture)),
                Is.True);
            Assert.That(
                RibbonLocalizationUpdateHelper.IsLocalizationChange(
                    nameof(RibbonLocalization.Localization)),
                Is.True);
            Assert.That(RibbonLocalizationUpdateHelper.IsLocalizationChange(null), Is.True);
            Assert.That(RibbonLocalizationUpdateHelper.IsLocalizationChange("Other"), Is.False);
        });

        var localization = new RibbonLocalization();
        var changes = new List<string?>();
        localization.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        localization.Localization = new English();

        Assert.That(changes, Does.Contain(nameof(RibbonLocalization.Localization)));
    }

    private sealed class MeaningfulHeader
    {
        public override string ToString() => "Custom header";
    }

    private sealed class DefaultHeader
    {
    }

}
