#nullable enable

namespace FluentUno.Tests.Automation;

using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;
using System.Reflection;

[TestFixture]
public class AutomationPeerTests
{
    private static readonly string[] ExpectedPeerTypeNames =
    [
        nameof(GalleryItemAutomationPeer),
        nameof(GalleryItemWrapperAutomationPeer),
        nameof(RibbonAutomationPeer),
        nameof(RibbonBackstageAutomationPeer),
        nameof(RibbonBackstageTabControlAutomationPeer),
        nameof(RibbonBackstageTabItemAutomationPeer),
        nameof(RibbonButtonAutomationPeer),
        nameof(RibbonCheckBoxAutomationPeer),
        nameof(RibbonComboBoxAutomationPeer),
        nameof(RibbonControlAutomationPeer),
        nameof(RibbonControlDataAutomationPeer),
        nameof(RibbonDropDownButtonAutomationPeer),
        nameof(RibbonGroupBoxAutomationPeer),
        nameof(RibbonGroupHeaderAutomationPeer),
        nameof(RibbonHeaderedControlAutomationPeer),
        nameof(RibbonGalleryAutomationPeer),
        nameof(RibbonInRibbonGalleryAutomationPeer),
        nameof(RibbonQuickAccessToolBarAutomationPeer),
        nameof(RibbonRadioButtonAutomationPeer),
        nameof(RibbonScreenTipAutomationPeer),
        nameof(RibbonMenuAutomationPeer),
        nameof(RibbonSplitButtonAutomationPeer),
        nameof(RibbonSpinnerAutomationPeer),
        nameof(RibbonStartScreenAutomationPeer),
        nameof(RibbonStartScreenTabControlAutomationPeer),
        nameof(RibbonTabControlAutomationPeer),
        nameof(RibbonTabItemAutomationPeer),
        nameof(RibbonTabItemDataAutomationPeer),
        nameof(RibbonTextBoxAutomationPeer),
        nameof(RibbonTitleBarAutomationPeer),
        nameof(RibbonToolBarAutomationPeer),
        nameof(RibbonToggleButtonAutomationPeer),
        nameof(TwoLineLabelAutomationPeer),
    ];

    [Test]
    public void PublicPeerTypesArePresent()
    {
        var assembly = typeof(RibbonAutomationPeer).Assembly;

        foreach (var typeName in ExpectedPeerTypeNames)
        {
            var type = assembly.GetType($"Fluent.Automation.Peers.{typeName}");
            Assert.That(type, Is.Not.Null, typeName);
            Assert.That(type!.IsPublic || type.IsNestedPublic, Is.True, typeName);
        }
    }

    [Test]
    public void FrameworkSpecificPeersUseWinUiEquivalents()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(RibbonButtonAutomationPeer).BaseType, Is.EqualTo(typeof(ButtonAutomationPeer)));
            Assert.That(typeof(RibbonCheckBoxAutomationPeer).BaseType, Is.EqualTo(typeof(CheckBoxAutomationPeer)));
            Assert.That(typeof(RibbonComboBoxAutomationPeer).BaseType, Is.EqualTo(typeof(ComboBoxAutomationPeer)));
            Assert.That(
                typeof(RibbonComboBoxAccessibleAutomationPeer).BaseType,
                Is.EqualTo(typeof(RibbonComboBoxAutomationPeer)));
            Assert.That(
                typeof(RibbonBackstageButtonAutomationPeer).BaseType,
                Is.EqualTo(typeof(FrameworkElementAutomationPeer)));
            Assert.That(typeof(RibbonRadioButtonAutomationPeer).BaseType, Is.EqualTo(typeof(RadioButtonAutomationPeer)));
            Assert.That(typeof(RibbonSpinnerAutomationPeer).BaseType, Is.EqualTo(typeof(FrameworkElementAutomationPeer)));
            Assert.That(typeof(RibbonTextBoxAutomationPeer).BaseType, Is.EqualTo(typeof(TextBoxAutomationPeer)));
            Assert.That(typeof(RibbonToggleButtonAutomationPeer).BaseType, Is.EqualTo(typeof(ToggleButtonAutomationPeer)));
            Assert.That(typeof(RibbonTabControlAutomationPeer).BaseType, Is.EqualTo(typeof(TabViewAutomationPeer)));
            Assert.That(typeof(RibbonTabItemAutomationPeer).BaseType, Is.EqualTo(typeof(FrameworkElementAutomationPeer)));
            Assert.That(typeof(RibbonSplitButtonAutomationPeer).BaseType, Is.EqualTo(typeof(RibbonDropDownButtonAutomationPeer)));
            Assert.That(typeof(RibbonHeaderedControlAutomationPeer).IsAbstract, Is.True);
        });
    }

    [Test]
    public void PeersExposeOwnerConstructors()
    {
        AssertOwnerConstructor<RibbonAutomationPeer, Ribbon>();
        AssertOwnerConstructor<RibbonBackstageAutomationPeer, Backstage>();
        AssertOwnerConstructor<RibbonBackstageTabControlAutomationPeer, BackstageTabControl>();
        AssertOwnerConstructor<RibbonBackstageTabItemAutomationPeer, BackstageTabItem>();
        AssertOwnerConstructor<RibbonButtonAutomationPeer, RibbonButton>();
        AssertOwnerConstructor<RibbonCheckBoxAutomationPeer, RibbonCheckBox>();
        AssertOwnerConstructor<RibbonComboBoxAutomationPeer, RibbonComboBox>();
        AssertOwnerConstructor<RibbonControlAutomationPeer, Control>();
        AssertOwnerConstructor<RibbonDropDownButtonAutomationPeer, RibbonDropDownButton>();
        AssertOwnerConstructor<RibbonGroupBoxAutomationPeer, RibbonGroupBox>();
        AssertOwnerConstructor<RibbonGroupHeaderAutomationPeer, FrameworkElement>();
        AssertOwnerConstructor<RibbonGalleryAutomationPeer, RibbonGallery>();
        AssertOwnerConstructor<RibbonInRibbonGalleryAutomationPeer, InRibbonGallery>();
        AssertOwnerConstructor<RibbonQuickAccessToolBarAutomationPeer, QuickAccessToolBar>();
        AssertOwnerConstructor<RibbonRadioButtonAutomationPeer, RibbonRadioButton>();
        AssertOwnerConstructor<RibbonScreenTipAutomationPeer, ScreenTip>();
        AssertOwnerConstructor<RibbonMenuAutomationPeer, RibbonMenu>();
        AssertOwnerConstructor<RibbonSplitButtonAutomationPeer, RibbonSplitButton>();
        AssertOwnerConstructor<RibbonSpinnerAutomationPeer, RibbonSpinner>();
        AssertOwnerConstructor<RibbonStartScreenAutomationPeer, StartScreen>();
        AssertOwnerConstructor<RibbonStartScreenTabControlAutomationPeer, StartScreenTabControl>();
        AssertOwnerConstructor<RibbonTabControlAutomationPeer, RibbonTabControl>();
        AssertOwnerConstructor<RibbonTabItemAutomationPeer, RibbonTab>();
        AssertOwnerConstructor<RibbonTextBoxAutomationPeer, RibbonTextBox>();
        AssertOwnerConstructor<RibbonTitleBarAutomationPeer, RibbonTitleBar>();
        AssertOwnerConstructor<RibbonToolBarAutomationPeer, RibbonToolBar>();
        AssertOwnerConstructor<RibbonToggleButtonAutomationPeer, RibbonToggleButton>();
        AssertOwnerConstructor<TwoLineLabelAutomationPeer, TwoLineLabel>();

        Assert.That(
            typeof(GalleryItemAutomationPeer).GetConstructor([typeof(object), typeof(SelectorAutomationPeer)]),
            Is.Not.Null);
        Assert.That(
            typeof(RibbonControlDataAutomationPeer).GetConstructor([typeof(object), typeof(ItemsControlAutomationPeer)]),
            Is.Not.Null);
        Assert.That(
            typeof(RibbonTabItemDataAutomationPeer).GetConstructor([typeof(object), typeof(RibbonTabControlAutomationPeer)]),
            Is.Not.Null);
        Assert.That(
            typeof(RibbonComboBoxAccessibleAutomationPeer).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(RibbonComboBox)],
                modifiers: null),
            Is.Not.Null);
        Assert.That(
            typeof(RibbonBackstageButtonAutomationPeer).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(BackstageButton)],
                modifiers: null),
            Is.Not.Null);
    }

    [Test]
    public void RibbonSpinnerCreatesItsSpecificPeer()
    {
        var method = typeof(RibbonSpinner).GetMethod(
            "OnCreateAutomationPeer",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(AutomationPeer)));
        });
    }

    [Test]
    public void RepresentativePatternsAreDeclared()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonBackstageAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonDropDownButtonAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonComboBoxAccessibleAutomationPeer)), Is.True);
            Assert.That(typeof(IItemContainerProvider).IsAssignableFrom(typeof(RibbonComboBoxAccessibleAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonComboBoxAccessibleAutomationPeer)), Is.True);
            Assert.That(typeof(IValueProvider).IsAssignableFrom(typeof(RibbonComboBoxAccessibleAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionItemProvider).IsAssignableFrom(typeof(RibbonComboBoxItemDataAutomationPeer)), Is.True);
            Assert.That(typeof(IInvokeProvider).IsAssignableFrom(typeof(RibbonBackstageButtonAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonGroupBoxAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonStartScreenAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonStartScreenTabControlAutomationPeer)), Is.True);
            Assert.That(typeof(IInvokeProvider).IsAssignableFrom(typeof(RibbonSplitButtonAutomationPeer)), Is.True);
            Assert.That(typeof(IRangeValueProvider).IsAssignableFrom(typeof(RibbonSpinnerAutomationPeer)), Is.True);
            Assert.That(typeof(IInvokeProvider).IsAssignableFrom(typeof(GalleryItemWrapperAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonGalleryAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonInRibbonGalleryAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionItemProvider).IsAssignableFrom(typeof(GalleryItemAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionItemProvider).IsAssignableFrom(typeof(GalleryItemWrapperAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonTabControlAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionItemProvider).IsAssignableFrom(typeof(RibbonTabItemDataAutomationPeer)), Is.True);
            Assert.That(typeof(IScrollItemProvider).IsAssignableFrom(typeof(GalleryItemAutomationPeer)), Is.True);
            Assert.That(typeof(IScrollItemProvider).IsAssignableFrom(typeof(RibbonGroupBoxAutomationPeer)), Is.True);
        });

        AssertPatternOverride<RibbonDropDownButtonAutomationPeer>();
        AssertPatternOverride<RibbonComboBoxAccessibleAutomationPeer>();
        AssertPatternOverride<RibbonBackstageButtonAutomationPeer>();
        AssertPatternOverride<RibbonSplitButtonAutomationPeer>();
        AssertPatternOverride<RibbonSpinnerAutomationPeer>();
        AssertPatternOverride<RibbonTabControlAutomationPeer>();
        AssertPatternOverride<RibbonGalleryAutomationPeer>();
        AssertPatternOverride<GalleryItemWrapperAutomationPeer>();
    }

    [Test]
    public void RepresentativePeersOverrideAccessibleMetadata()
    {
        Assert.Multiple(() =>
        {
            AssertCoreOverride<RibbonButtonAutomationPeer>("GetNameCore");
            AssertCoreOverride<RibbonScreenTipAutomationPeer>("GetNameCore");
            AssertCoreOverride<RibbonScreenTipAutomationPeer>("GetHelpTextCore");
            AssertCoreOverride<RibbonScreenTipAutomationPeer>("GetAutomationControlTypeCore");
            AssertCoreOverride<RibbonScreenTipAutomationPeer>("GetAcceleratorKeyCore");
            AssertCoreOverride<RibbonSpinnerAutomationPeer>("GetAutomationControlTypeCore");
            AssertCoreOverride<RibbonSpinnerAutomationPeer>("GetNameCore");
            AssertCoreOverride<RibbonSpinnerAutomationPeer>("GetAccessKeyCore");
            AssertCoreOverride<RibbonSpinnerAutomationPeer>("SetFocusCore");
            AssertCoreOverride<RibbonTabItemAutomationPeer>("GetAccessKeyCore");
            AssertCoreOverride<RibbonGalleryAutomationPeer>("GetChildrenCore");
            AssertCoreOverride<RibbonInRibbonGalleryAutomationPeer>("GetChildrenCore");
            AssertCoreOverride<TwoLineLabelAutomationPeer>("GetNameCore");
        });
    }

    private static void AssertOwnerConstructor<TPeer, TOwner>()
    {
        Assert.That(typeof(TPeer).GetConstructor([typeof(TOwner)]), Is.Not.Null, typeof(TPeer).Name);
    }

    private static void AssertPatternOverride<TPeer>()
    {
        AssertCoreOverride<TPeer>("GetPatternCore");
    }

    private static void AssertCoreOverride<TPeer>(string methodName)
    {
        var method = typeof(TPeer).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Not.Null, $"{typeof(TPeer).Name}.{methodName}");
    }
}
