#nullable enable

namespace FluentUno.Tests.Automation;

using System;
using System.Reflection;
using Fluent.Automation.Peers;
using Fluent.Localization.Languages;
using Fluent.Modern.Automation;
using NUnit.Framework;

[TestFixture]
public sealed class AccessibleNameContractTests
{
    [Test]
    public void SemanticOwnerPeersDeclareNameFallbackContracts()
    {
        Assert.Multiple(() =>
        {
            AssertNameOverride(typeof(RibbonHeaderedControlAutomationPeer));
            AssertNameOverride(typeof(RibbonComboBoxAccessibleAutomationPeer));
            AssertNameOverride(typeof(RibbonSpinnerAutomationPeer));
            AssertNameOverride(typeof(RibbonTextBoxAutomationPeer));
            AssertNameOverride(typeof(RibbonSearchBoxAutomationPeer));
            AssertHelpTextUsesFrameworkContract(typeof(RibbonHeaderedControlAutomationPeer));
            AssertHelpTextUsesFrameworkContract(typeof(RibbonComboBoxAccessibleAutomationPeer));
            AssertHelpTextUsesFrameworkContract(typeof(RibbonSpinnerAutomationPeer));
            AssertHelpTextUsesFrameworkContract(typeof(RibbonTextBoxAutomationPeer));
            AssertHelpTextUsesFrameworkContract(typeof(RibbonSearchBoxAutomationPeer));
            Assert.That(
                typeof(RibbonSplitButtonAutomationPeer).IsSubclassOf(
                    typeof(RibbonHeaderedControlAutomationPeer)),
                Is.True);
            Assert.That(
                typeof(AutomationPeerHelpers).GetMethod(
                    "GetHeaderOrPlaceholderName",
                    BindingFlags.Static | BindingFlags.NonPublic),
                Is.Not.Null);
        });
    }

    [Test]
    public void EnglishActionNamesAreDistinctAndHumanReadable()
    {
        var localization = new English();

        Assert.Multiple(() =>
        {
            Assert.That(localization.ScrollRibbonLeft, Is.Not.Empty);
            Assert.That(localization.ScrollRibbonRight, Is.Not.Empty);
            Assert.That(localization.ScrollRibbonLeft, Is.Not.EqualTo(localization.ScrollRibbonRight));
            Assert.That(localization.ScrollGalleryUp, Is.Not.EqualTo(localization.ScrollGalleryDown));
            Assert.That(localization.OpenGallery, Is.Not.EqualTo(localization.OpenGalleryOptions));
            Assert.That(
                string.Format(localization.OpenGroupDialogFormat, "Font"),
                Is.EqualTo("Open Font dialog"));
        });
    }

    private static void AssertNameOverride(Type peerType)
    {
        var method = peerType.GetMethod(
            "GetNameCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Not.Null, peerType.Name);
    }

    private static void AssertHelpTextUsesFrameworkContract(Type peerType)
    {
        var method = peerType.GetMethod(
            "GetHelpTextCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Null, peerType.Name);
    }
}
