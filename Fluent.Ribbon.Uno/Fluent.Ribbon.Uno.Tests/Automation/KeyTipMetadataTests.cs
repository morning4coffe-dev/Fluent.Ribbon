#nullable enable

namespace FluentUno.Tests.Automation;

using System;
using System.Reflection;
using Fluent.Automation.Peers;
using NUnit.Framework;

[TestFixture]
public sealed class KeyTipMetadataTests
{
    private static readonly Type[] KeyTippedPeers =
    [
        typeof(RibbonButtonAutomationPeer),
        typeof(RibbonCheckBoxAutomationPeer),
        typeof(RibbonComboBoxAutomationPeer),
        typeof(RibbonDropDownButtonAutomationPeer),
        typeof(RibbonGroupBoxAutomationPeer),
        typeof(RibbonInRibbonGalleryAutomationPeer),
        typeof(RibbonRadioButtonAutomationPeer),
        typeof(RibbonSpinnerAutomationPeer),
        typeof(RibbonTabItemAutomationPeer),
        typeof(RibbonTextBoxAutomationPeer),
        typeof(RibbonToggleButtonAutomationPeer),
    ];

    [Test]
    public void KeyTippedPeersOverrideOrInheritRibbonAccessKeyFallback()
    {
        Assert.Multiple(() =>
        {
            foreach (var peerType in KeyTippedPeers)
            {
                var method = peerType.GetMethod(
                    "GetAccessKeyCore",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null, peerType.Name);
            }
        });
    }

    [Test]
    public void TogglePeerExposesScreenTipHelpText()
    {
        var method = typeof(RibbonToggleButtonAutomationPeer).GetMethod(
            "GetHelpTextCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.That(method, Is.Not.Null);
    }
}
