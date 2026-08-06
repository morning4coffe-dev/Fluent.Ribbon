#nullable enable

namespace FluentUno.Tests.Automation;

using System;
using Fluent;
using Fluent.Automation.Peers;
using Fluent.Modern.Automation;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;

[TestFixture]
public sealed class ProviderContractTests
{
    private static readonly (Type Peer, Type Provider)[] CustomMutationProviders =
    [
        (typeof(RibbonButtonAutomationPeer), typeof(IInvokeProvider)),
        (typeof(RibbonCheckBoxAutomationPeer), typeof(IToggleProvider)),
        (typeof(RibbonRadioButtonAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(RibbonTextBoxAutomationPeer), typeof(IValueProvider)),
        (typeof(RibbonToggleButtonAutomationPeer), typeof(IToggleProvider)),
        (typeof(GalleryItemAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(GalleryItemAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(GalleryItemWrapperAutomationPeer), typeof(IInvokeProvider)),
        (typeof(GalleryItemWrapperAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(GalleryItemWrapperAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(RibbonAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonBackstageAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonStartScreenAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonBackstageButtonAutomationPeer), typeof(IInvokeProvider)),
        (typeof(RibbonBackstageTabItemAutomationPeer), typeof(IInvokeProvider)),
        (typeof(RibbonBackstageTabItemAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(RibbonGroupBoxAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonGroupBoxAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(RibbonInRibbonGalleryAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonComboBoxAccessibleAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonComboBoxAccessibleAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(RibbonComboBoxAccessibleAutomationPeer), typeof(IValueProvider)),
        (typeof(RibbonComboBoxItemDataAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(RibbonComboBoxItemDataAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(RibbonDropDownButtonAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonSplitButtonAutomationPeer), typeof(IInvokeProvider)),
        (typeof(RibbonSpinnerAutomationPeer), typeof(IRangeValueProvider)),
        (typeof(ResizeHandleAutomationPeer), typeof(ITransformProvider)),
        (typeof(RibbonTabItemAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(RibbonTabItemAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(RibbonTabItemDataAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonTabItemDataAutomationPeer), typeof(IScrollItemProvider)),
        (typeof(RibbonTabItemDataAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(StatusBarMenuItemAutomationPeer), typeof(IInvokeProvider)),
        (typeof(StatusBarMenuItemAutomationPeer), typeof(IToggleProvider)),
        (typeof(RibbonMenuItemAutomationPeer), typeof(IExpandCollapseProvider)),
        (typeof(RibbonMenuItemAutomationPeer), typeof(IInvokeProvider)),
        (typeof(RibbonMenuItemAutomationPeer), typeof(ISelectionItemProvider)),
        (typeof(RibbonMenuItemAutomationPeer), typeof(IToggleProvider)),
        (typeof(RibbonSearchBoxAutomationPeer), typeof(IValueProvider)),
    ];

    [Test]
    public void DisabledStateTakesPrecedenceOverCapabilityState()
    {
        Assert.That(
            () => AutomationProviderGuard.Validate(
                isEnabled: false,
                isAvailable: false,
                "Unavailable."),
            Throws.InstanceOf<ElementNotEnabledException>());
    }

    [Test]
    public void UnavailableEnabledActionThrowsInvalidOperation()
    {
        Assert.That(
            () => AutomationProviderGuard.Validate(
                isEnabled: true,
                isAvailable: false,
                "Unavailable."),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void AvailableEnabledActionPassesValidation()
    {
        Assert.That(
            () => AutomationProviderGuard.Validate(
                isEnabled: true,
                isAvailable: true,
                "Unavailable."),
            Throws.Nothing);
    }

    [Test]
    public void CustomMutationProvidersUseAssemblyOwnedImplementations()
    {
        var assembly = typeof(RibbonAutomationPeer).Assembly;

        Assert.Multiple(() =>
        {
            foreach (var (peer, provider) in CustomMutationProviders)
            {
                Assert.That(provider.IsAssignableFrom(peer), Is.True, $"{peer.Name}: {provider.Name}");

                var map = peer.GetInterfaceMap(provider);
                foreach (var targetMethod in map.TargetMethods)
                {
                    if (targetMethod.Name.StartsWith(
                            "get_",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Assert.That(
                        targetMethod.Module.Assembly,
                        Is.EqualTo(assembly),
                        $"{peer.Name}.{targetMethod.Name}");
                }
            }
        });
    }
}
