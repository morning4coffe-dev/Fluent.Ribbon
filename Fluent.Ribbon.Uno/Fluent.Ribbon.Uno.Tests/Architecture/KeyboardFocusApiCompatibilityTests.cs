namespace FluentUno.Tests.Architecture;

using System.Reflection;
using Fluent;
using Microsoft.UI.Xaml;
using NUnit.Framework;
using Windows.System;

[TestFixture]
public sealed class KeyboardFocusApiCompatibilityTests
{
    [Test]
    public void KeyTipServiceShouldExposeLifecycleAndVisibilityContracts()
    {
        var type = typeof(KeyTipService);

        Assert.Multiple(() =>
        {
            Assert.That(type.GetProperty(nameof(KeyTipService.KeyTipKeys)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(KeyTipService.IsActive)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(KeyTipService.AreAnyKeyTipsVisible)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(KeyTipService.IsEnabled)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(KeyTipService.Attach)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(KeyTipService.Detach)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(KeyTipService.Show)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(KeyTipService.Hide)), Is.Not.Null);
        });
    }

    [Test]
    public void PopupScopesShouldParticipateInKeyTipBackNavigation()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(IKeyTipedControl).IsAssignableFrom(typeof(ApplicationMenu)), Is.True);
            Assert.That(typeof(IKeyTipedControl).IsAssignableFrom(typeof(Backstage)), Is.True);
            Assert.That(typeof(IKeyTipedControl).IsAssignableFrom(typeof(StartScreen)), Is.True);
            Assert.That(typeof(IKeyTipedControl).IsAssignableFrom(typeof(BackstageTabItem)), Is.True);
            Assert.That(typeof(ApplicationMenu).GetMethod(nameof(ApplicationMenu.Close)), Is.Not.Null);
            Assert.That(typeof(StartScreen).GetField(nameof(StartScreen.CloseOnEscProperty)), Is.Not.Null);
            Assert.That(typeof(StartScreen).GetField(nameof(StartScreen.ShownProperty)), Is.Not.Null);
            Assert.That(
                typeof(StartScreen).GetMethod(
                    "Show",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                typeof(StartScreen).GetMethod(
                    "Hide",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                typeof(Backstage).GetMethod(
                    "Show",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                typeof(Backstage).GetMethod(
                    "Hide",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
        });
    }

    [Test]
    public void ScreenTipShouldExposeWpfHelpSemantics()
    {
        var helpEvent = typeof(ScreenTip).GetEvent(
            nameof(ScreenTip.HelpPressed),
            BindingFlags.Public | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            Assert.That(helpEvent, Is.Not.Null);
            Assert.That(typeof(ScreenTip).GetField(nameof(ScreenTip.HelpTopicProperty)), Is.Not.Null);
            Assert.That(typeof(ScreenTip).GetField(nameof(ScreenTip.HelpLabelActualVisibilityProperty)), Is.Not.Null);
            Assert.That(typeof(ScreenTip).GetField(nameof(ScreenTip.TextTemplateProperty)), Is.Not.Null);
            Assert.That(typeof(ScreenTipHelpEventArgs).GetProperty(nameof(ScreenTipHelpEventArgs.HelpTopic)), Is.Not.Null);
        });
    }

    [Test]
    public void KeyTipInformationEnabledStateShouldRemainReadOnlyAndLive()
    {
        var property = typeof(KeyTipInformation).GetProperty(nameof(KeyTipInformation.IsEnabled));

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(property?.CanRead, Is.True);
            Assert.That(property?.CanWrite, Is.False);
        });
    }

    [Test]
    public void KeyTipShouldExposeAttachedPlacementContracts()
    {
        var type = typeof(KeyTip);

        Assert.Multiple(() =>
        {
            Assert.That(type.GetField(nameof(KeyTip.KeysProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(KeyTip.AutoPlacementProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(KeyTip.HorizontalAlignmentProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(KeyTip.VerticalAlignmentProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(KeyTip.MarginProperty)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(KeyTip.GetKeys)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(KeyTip.SetKeys)), Is.Not.Null);
        });
    }

    [Test]
    public void DefaultActivationKeysShouldMatchWpfBehavior()
    {
        var first = KeyTipService.DefaultKeyTipKeys;
        var second = KeyTipService.DefaultKeyTipKeys;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new[] { VirtualKey.Menu, VirtualKey.F10 }));
            Assert.That(second, Is.Not.SameAs(first));
        });
    }

    [Test]
    public void BackstageButtonShouldHandleStandardKeyboardActivation()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(BackstageButton).GetMethod(
                    "OnKeyDown",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
            Assert.That(
                typeof(BackstageButton).GetMethod(
                    "OnKeyUp",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
        });
    }

    [Test]
    public void DelegatedFocusStateShouldOverrideAStaleEditorFocusState()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                FocusRoutingHelper.ResolveDelegatedFocusState(
                    FocusState.Pointer,
                    FocusState.Keyboard),
                Is.EqualTo(FocusState.Pointer));
            Assert.That(
                FocusRoutingHelper.ResolveDelegatedFocusState(
                    FocusState.Keyboard,
                    FocusState.Pointer),
                Is.EqualTo(FocusState.Keyboard));
            Assert.That(
                FocusRoutingHelper.ResolveDelegatedFocusState(
                    FocusState.Programmatic,
                    FocusState.Keyboard),
                Is.EqualTo(FocusState.Keyboard));
        });
    }

    [Test]
    public void ScreenTipShouldRetainF1HandlingAndAccessibilityHooks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(ScreenTip).GetMethod(
                    "TryHandleHelpKey",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                typeof(ScreenTip).GetMethod(
                    "UpdateKeyboardSubscription",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                typeof(ScreenTip).GetMethod(
                    "OnCreateAutomationPeer",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
        });
    }

    [Test]
    public void KeyTipServiceShouldExposeDeterministicInputAndCleanupHooks()
    {
        var type = typeof(KeyTipService);
        var precedence = type.GetMethod(
            "ShouldProcessKeyEvent",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.Multiple(() =>
        {
            Assert.That(
                type.GetField("_rootKeyDownHandler", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                type.GetField("_rootKeyUpHandler", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                type.GetField("_rootPointerPressedHandler", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    "DismissForWindowDeactivation",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    "DismissForPointerInput",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                type.GetMethod("FocusCurrentScope", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(precedence, Is.Not.Null);
            Assert.That(precedence?.Invoke(null, new object[] { true, true }), Is.True);
            Assert.That(precedence?.Invoke(null, new object[] { false, true }), Is.False);
            Assert.That(precedence?.Invoke(null, new object[] { false, false }), Is.True);
        });
    }
}
