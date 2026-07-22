namespace FluentUno.Tests.Architecture;

using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class AssemblyArchitectureTests
{
    [Test]
    public void UnoAssemblyShouldNotReferenceWpfFrameworkAssemblies()
    {
        var forbiddenAssemblies = new[]
        {
            "PresentationCore",
            "PresentationFramework",
            "WindowsBase",
        };

        var references = typeof(Fluent.Ribbon).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.That(references, Has.None.Matches<string>(name =>
            forbiddenAssemblies.Contains(name, StringComparer.Ordinal)));
    }

    [Test]
    public void ModernPublicTypesShouldCarryModernExtensionMarker()
    {
        var markerType = typeof(Fluent.Modern.ModernExtensionAttribute);
        var missingMarkers = markerType.Assembly
            .GetTypes()
            .Where(type => type.IsPublic
                           && type.Namespace is not null
                           && (type.Namespace.Equals("Fluent.Modern", StringComparison.Ordinal)
                               || type.Namespace.StartsWith("Fluent.Modern.", StringComparison.Ordinal))
                           && type.GetCustomAttributes(markerType, inherit: false).Length == 0)
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(missingMarkers, Is.Empty);
    }

    [Test]
    public void RibbonCommandsShouldExposeTheWpfCommandName()
    {
        var field = typeof(Fluent.RibbonCommands).GetField(nameof(Fluent.RibbonCommands.OpenBackstage));

        Assert.That(field, Is.Not.Null);
    }

    [Test]
    public void CoreInterfacesShouldRetainWpfMembers()
    {
        var keyTipMethod = typeof(Fluent.IKeyTipedControl).GetMethod(
            nameof(Fluent.IKeyTipedControl.OnKeyTipPressed));
        var scalable = typeof(Fluent.IScalableRibbonControl);
        var stateStorage = typeof(Fluent.IRibbonStateStorage);

        Assert.Multiple(() =>
        {
            Assert.That(keyTipMethod?.ReturnType, Is.EqualTo(typeof(Fluent.KeyTipPressedResult)));
            Assert.That(scalable.GetMethod(nameof(Fluent.IScalableRibbonControl.Enlarge)), Is.Not.Null);
            Assert.That(scalable.GetMethod(nameof(Fluent.IScalableRibbonControl.Reduce)), Is.Not.Null);
            Assert.That(scalable.GetMethod(nameof(Fluent.IScalableRibbonControl.ResetScale)), Is.Not.Null);
            Assert.That(scalable.GetEvent(nameof(Fluent.IScalableRibbonControl.Scaled)), Is.Not.Null);
            Assert.That(stateStorage.GetProperty(nameof(Fluent.IRibbonStateStorage.IsLoading)), Is.Not.Null);
            Assert.That(stateStorage.GetProperty(nameof(Fluent.IRibbonStateStorage.IsLoaded)), Is.Not.Null);
            Assert.That(typeof(IDisposable).IsAssignableFrom(stateStorage), Is.True);
            Assert.That(
                typeof(Fluent.ILargeIconProvider)
                    .GetProperty(nameof(Fluent.ILargeIconProvider.LargeIcon))
                    ?.PropertyType,
                Is.EqualTo(typeof(object)));
            Assert.That(
                typeof(Fluent.IMediumIconProvider)
                    .GetProperty(nameof(Fluent.IMediumIconProvider.MediumIcon))
                    ?.PropertyType,
                Is.EqualTo(typeof(object)));
            Assert.That(
                typeof(Fluent.IHeaderedControl)
                    .GetProperty(nameof(Fluent.IHeaderedControl.HeaderTemplate))
                    ?.PropertyType,
                Is.EqualTo(typeof(Microsoft.UI.Xaml.DataTemplate)));
            Assert.That(
                typeof(Fluent.IHeaderedControl)
                    .GetProperty(nameof(Fluent.IHeaderedControl.HeaderTemplateSelector))
                    ?.PropertyType,
                Is.EqualTo(typeof(Microsoft.UI.Xaml.Controls.DataTemplateSelector)));
            Assert.That(
                typeof(Fluent.IDropDownControl)
                    .GetProperty(nameof(Fluent.IDropDownControl.DropDownPopup))
                    ?.PropertyType,
                Is.EqualTo(typeof(Microsoft.UI.Xaml.Controls.Primitives.Popup)));
            Assert.That(
                typeof(Fluent.IDropDownControl)
                    .GetProperty(nameof(Fluent.IDropDownControl.IsContextMenuOpened))
                    ?.PropertyType,
                Is.EqualTo(typeof(bool)));
            Assert.That(
                typeof(Fluent.IconPresenter)
                    .GetProperty(nameof(Fluent.IconPresenter.SmallSize))
                    ?.PropertyType,
                Is.EqualTo(typeof(Windows.Foundation.Size)));
            Assert.That(
                typeof(Fluent.IconPresenter)
                    .GetProperty(nameof(Fluent.IconPresenter.CurrentIconSizeSize))
                    ?.CanWrite,
                Is.True);
            Assert.That(
                typeof(Fluent.ToggleButtonHelper).GetMethod(
                    nameof(Fluent.ToggleButtonHelper.UpdateButtonGroup),
                    new[] { typeof(Fluent.IToggleButton) }),
                Is.Not.Null);
        });
    }
}
