#nullable enable

namespace FluentUno.Tests.Automation;

using System;
using System.Reflection;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;

[TestFixture]
public sealed class MenuItemAutomationPeerTests
{
    [Test]
    public void PeerDeclaresMenuPatternsAndMetadataOverrides()
    {
        var type = typeof(RibbonMenuItemAutomationPeer);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(IInvokeProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IToggleProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(ISelectionItemProvider).IsAssignableFrom(type), Is.True);
            Assert.That(
                typeof(ISelectionProvider).IsAssignableFrom(
                    typeof(Fluent.Automation.Peers.RibbonDropDownButtonAutomationPeer)),
                Is.True);
            AssertOverride(type, "GetPatternCore");
            AssertOverride(type, "GetAccessKeyCore");
            AssertOverride(type, "GetChildrenCore");
        });
    }

    [TestCase(PatternInterface.Invoke, false, false, false, null, true)]
    [TestCase(PatternInterface.Invoke, true, false, false, null, false)]
    [TestCase(PatternInterface.Invoke, true, true, false, null, true)]
    [TestCase(PatternInterface.ExpandCollapse, true, false, false, null, true)]
    [TestCase(PatternInterface.ExpandCollapse, false, false, false, null, false)]
    [TestCase(PatternInterface.Toggle, false, false, true, null, true)]
    [TestCase(PatternInterface.Toggle, false, false, true, "group", false)]
    [TestCase(PatternInterface.SelectionItem, false, false, true, "group", true)]
    [TestCase(PatternInterface.SelectionItem, true, false, true, "group", false)]
    public void PatternApplicabilityMatchesMenuRole(
        PatternInterface pattern,
        bool hasSubItems,
        bool isSplit,
        bool isCheckable,
        string? groupName,
        bool expected)
    {
        Assert.That(
            RibbonMenuItemAutomationPeer.IsPatternApplicable(
                pattern,
                hasSubItems,
                isSplit,
                isCheckable,
                groupName),
            Is.EqualTo(expected));
    }

    [Test]
    public void ProviderValidationUsesUiaExceptions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => AutomationProviderGuard.Validate(
                    isEnabled: false,
                    isAvailable: true,
                    "Unavailable."),
                Throws.InstanceOf<ElementNotEnabledException>());
            Assert.That(
                () => AutomationProviderGuard.Validate(
                    isEnabled: true,
                    isAvailable: false,
                    "Unavailable."),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => AutomationProviderGuard.Validate(
                    isEnabled: true,
                    isAvailable: true,
                    "Unavailable."),
                Throws.Nothing);
        });
    }

    [TestCase(true, ToggleState.On)]
    [TestCase(false, ToggleState.Off)]
    [TestCase(null, ToggleState.Indeterminate)]
    public void NullableCheckedStateMapsToToggleState(bool? value, ToggleState expected)
    {
        Assert.That(RibbonMenuItemAutomationPeer.ToToggleState(value), Is.EqualTo(expected));
    }

    private static void AssertOverride(Type type, string methodName)
    {
        Assert.That(
            type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
            Is.Not.Null,
            methodName);
    }
}
