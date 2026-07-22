using Microsoft.UI.Xaml;
using NUnit.Framework;
using Windows.Foundation;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed partial class HelperCompatibilityTests
{
    [Test]
    public void CommandSourceExtensions_UsePortableSourceContract()
    {
        var source = new TestCommandSource();

        Assert.Multiple(() =>
        {
            Assert.That(
                global::Fluent.Extensions.ICommandSourceExtensions.CanExecuteCommand(source),
                Is.True);
            Assert.That(
                () => global::Fluent.Extensions.ICommandSourceExtensions.ExecuteCommand(source),
                Throws.Nothing);
            Assert.That(source.ExecutionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DropDownHelper_PreservesExplicitHeight()
    {
        Assert.That(
            global::Fluent.Helpers.DropDownHelper.GetMaxDropDownHeight(
                null!,
                320),
            Is.EqualTo(320));
    }

    [Test]
    public void PopupHelper_ReturnsWpfCompatiblePlacementSet()
    {
        var placements = global::Fluent.Helpers.PopupHelper.GetSimplePlacement(
            new Size(100, 80),
            new Size(40, 20),
            new Point());

        Assert.Multiple(() =>
        {
            Assert.That(placements, Has.Length.EqualTo(5));
            Assert.That(
                global::Fluent.Helpers.PopupHelper.SimplePlacementCallback,
                Is.Not.Null);
        });
    }

    [Test]
    public void ScopeGuard_InvokesEntryAndDisposeOnce()
    {
        var entries = 0;
        var disposals = 0;
        var guard = new global::Fluent.Internal.ScopeGuard(
            () => entries++,
            () => disposals++);

        guard.Start().Start();
        guard.Dispose();
        guard.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Is.EqualTo(1));
            Assert.That(disposals, Is.EqualTo(1));
            Assert.That(guard.IsActive, Is.False);
        });
    }

    private sealed class TestCommandSource : global::Fluent.ICommandSource
    {
        public int ExecutionCount { get; private set; }

        public System.Windows.Input.ICommand Command =>
            new TestCommand(() => ExecutionCount++);

        public object? CommandParameter => null;

        public UIElement? CommandTarget => null;
    }

    private sealed class TestCommand(Action execute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add
            {
            }
            remove
            {
            }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
