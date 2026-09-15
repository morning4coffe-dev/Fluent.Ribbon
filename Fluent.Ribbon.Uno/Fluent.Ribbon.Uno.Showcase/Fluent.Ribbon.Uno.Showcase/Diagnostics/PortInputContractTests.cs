namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.System;

internal static partial class PortInputContractTests
{
    internal static async Task VerifyGalleryEnterAsync(Panel host, Func<Task> settle)
    {
        AssertPublicControlMetadata();
        var command = new ObservedCommand { Allowed = true };
        var item = new GalleryItem { Content = "Gallery input", Command = command };
        var gallery = new Gallery { Width = 320, Height = 100 };
        gallery.Items.Add(item);
        var dropDown = new Fluent.DropDownButton { Header = "Gallery popup" };
        dropDown.Items.Add(gallery);
        var clicks = 0;
        var clickHookDismissals = 0;
        item.Click += (_, _) => clicks++;
        EventHandler<DismissPopupEventArgs> onDismiss = (sender, args) =>
        {
            if (ReferenceEquals(sender, item))
            {
                Require(args.DismissMode == DismissPopupMode.Always,
                    "The real facade click hook requested the wrong dismissal mode.");
                clickHookDismissals++;
            }
        };
        PopupService.DismissPopup += onDismiss;
        host.Children.Add(dropDown);
        try
        {
            await settle();
            dropDown.IsDropDownOpen = true;
            await settle();
            Require(dropDown.IsDropDownOpen, "The gallery input fixture did not open its real popup.");
            Require(GalleryDown(item, VirtualKey.Enter), "Enter down was not consumed.");
            Require(clicks == 0 && command.Executions == 0, "Gallery Enter activated before its matching key release.");
            Require(GalleryUp(item, VirtualKey.Enter), "Enter up was not consumed.");
            await settle();
            Require(clicks == 1 && command.Executions == 1 && clickHookDismissals == 1,
                "One Enter pair must produce exactly one Click, command execution, and facade click hook.");
            Require(ReferenceEquals(gallery.SelectedItem, item) && item.IsSelected,
                "Gallery Enter did not select the authored item.");
            Require(!dropDown.IsDropDownOpen, "Definitive gallery activation did not dismiss its popup.");

            dropDown.IsDropDownOpen = true;
            await settle();
            Require(!GalleryUp(item, VirtualKey.Enter), "A stray key-up was treated as an activation.");
            Require(!GalleryDown(item, VirtualKey.Enter, handled: true) && !GalleryUp(item, VirtualKey.Enter),
                "An already-handled key-down armed activation.");
            Require(!GalleryDown(item, VirtualKey.Enter, original: false),
                "A descendant's key event armed the gallery container.");
            GalleryDown(item, VirtualKey.Enter);
            Require(!GalleryUp(item, VirtualKey.Enter, handled: true), "An already-handled key-up activated the gallery.");
            Require(clicks == 1 && command.Executions == 1, "Suppressed keyboard paths invoked the item.");
            GalleryDown(item, VirtualKey.Enter);
            GalleryDown(item, VirtualKey.Enter);
            GalleryUp(item, VirtualKey.Enter);
            Require(clicks == 2 && command.Executions == 2, "Repeated key-down generated extra activations.");
            await Reopen();
            GalleryDown(item, VirtualKey.Space);
            GalleryUp(item, VirtualKey.Space);
            Require(clicks == 3 && command.Executions == 3, "Space down/up did not invoke exactly once.");
            await Reopen();
            GalleryPointerPress(item);
            InvokeInput(GalleryPointerReleasedMethod, item);
            Require(clicks == 4 && command.Executions == 4, "Pointer press/release did not invoke exactly once.");
            await Reopen();
            Require(!GalleryPointerPress(item, primary: false), "A secondary pointer button activated the item.");
            item.OnKeyTipPressed();
            await Reopen();
            new GalleryItemWrapperAutomationPeer(item).Invoke();
            Require(clicks == 6 && command.Executions == 6 && clickHookDismissals == 6,
                "Key-tip or UIA activation double-invoked or failed to invoke the facade.");

            await Reopen();
            var core = new RibbonGalleryItem { Content = "Core gallery input", Command = command };
            gallery.Items.Add(core);
            var coreClicks = 0;
            core.Click += (_, _) => coreClicks++;
            await settle();
            GalleryDown(core, VirtualKey.Enter);
            GalleryUp(core, VirtualKey.Enter);
            Require(coreClicks == 1 && command.Executions == 7,
                "Core RibbonGalleryItem does not share the single keyboard activation path.");
        }
        finally
        {
            PopupService.DismissPopup -= onDismiss;
            dropDown.IsDropDownOpen = false;
            host.Children.Remove(dropDown);
            host.Children.Remove(gallery);
            await settle();
        }

        async Task Reopen()
        {
            await settle();
            dropDown.IsDropDownOpen = true;
            await settle();
            Require(dropDown.IsDropDownOpen && item.IsLoaded,
                "The registered gallery item did not return to its mounted popup input surface.");
        }
    }

    internal static async Task VerifyCommandAvailabilityAsync(Panel host, Func<Task> settle)
    {
        AssertPublicControlMetadata();
        var menu = new Fluent.MenuItem { Header = "Command menu", IsCheckable = true };
        var coreItem = new RibbonGalleryItem { Content = "Core command item" };
        var facadeItem = new GalleryItem { Content = "Facade command item" };
        var coreSplit = new RibbonSplitButton { Header = "Core command split", IsCheckable = true };
        var facadeSplit = new Fluent.SplitButton { Header = "Facade command split", IsCheckable = true };
        var surfaces = new[]
        {
            new CommandSurface(menu, Fluent.MenuItem.CommandProperty, Fluent.MenuItem.CommandParameterProperty,
                () => InvokeInput(MenuClickMethod, menu), () => new RibbonMenuItemAutomationPeer(menu).Invoke(),
                () => menu.IsEnabled, () => menu.IsChecked == true, () => menu.IsChecked = false,
                handler => menu.Click += handler),
            new CommandSurface(coreItem, RibbonGalleryItem.CommandProperty, RibbonGalleryItem.CommandParameterProperty,
                () => coreItem.OnKeyTipPressed(), () => new GalleryItemWrapperAutomationPeer(coreItem).Invoke(),
                () => coreItem.IsEnabled, () => coreItem.IsSelected, () => coreItem.IsSelected = false,
                handler => coreItem.Click += handler),
            new CommandSurface(facadeItem, RibbonGalleryItem.CommandProperty, RibbonGalleryItem.CommandParameterProperty,
                facadeItem.RaiseClick, () => new GalleryItemWrapperAutomationPeer(facadeItem).Invoke(),
                () => facadeItem.IsEnabled, () => facadeItem.IsSelected, () => facadeItem.IsSelected = false,
                handler => facadeItem.Click += handler),
            new CommandSurface(coreSplit, RibbonSplitButton.CommandProperty, RibbonSplitButton.CommandParameterProperty,
                () => InvokeInput(SplitPrimaryMethod, coreSplit), () => new RibbonSplitButtonAutomationPeer(coreSplit).Invoke(),
                () => coreSplit.IsPrimaryActionEnabled, () => coreSplit.IsChecked == true, () => coreSplit.IsChecked = false,
                handler => coreSplit.Click += handler),
            new CommandSurface(facadeSplit, RibbonSplitButton.CommandProperty, RibbonSplitButton.CommandParameterProperty,
                () => InvokeInput(SplitPrimaryMethod, facadeSplit), () => new RibbonSplitButtonAutomationPeer(facadeSplit).Invoke(),
                () => facadeSplit.IsPrimaryActionEnabled, () => facadeSplit.IsChecked == true, () => facadeSplit.IsChecked = false,
                handler => facadeSplit.Click += handler),
        };

        foreach (var surface in surfaces)
        {
            await VerifyCommandSurface(host, surface, settle);
        }

        await VerifyLoadedSplitClone(host, settle, useFacade: false);
        await VerifyLoadedSplitClone(host, settle, useFacade: true);

        var parent = new Fluent.MenuItem { Header = "Group" };
        var first = new Fluent.MenuItem { Header = "A", GroupName = "commands", IsCheckable = true, IsChecked = true };
        var blocked = new Fluent.MenuItem
        {
            Header = "B", GroupName = "commands", IsCheckable = true,
            Command = new ObservedCommand { Allowed = false },
        };
        parent.Items.Add(first);
        parent.Items.Add(blocked);
        host.Children.Add(parent);
        try
        {
            await settle();
            InvokeInput(MenuClickMethod, blocked);
            Require(first.IsChecked == true && blocked.IsChecked != true,
                "A command-disabled grouped menu item changed group selection.");
            ExpectDisabled(() => new RibbonMenuItemAutomationPeer(blocked).Select());
            Require(first.IsChecked == true && blocked.IsChecked != true,
                "UIA bypassed command gating for a grouped menu item.");
        }
        finally
        {
            host.Children.Remove(parent);
            await settle();
        }
    }

    private static async Task VerifyCommandSurface(Panel host, CommandSurface surface, Func<Task> settle)
    {
        var command = new ObservedCommand { ExpectedParameter = "go" };
        var owner = surface.Owner;
        owner.SetValue(surface.CommandProperty, command);
        owner.SetValue(surface.ParameterProperty, "go");
        var clicks = 0;
        surface.SubscribeClick((_, _) => clicks++);
        host.Children.Add(owner);
        try
        {
            await settle();
            Require(command.Subscribers == 1, $"{owner.GetType().Name} did not observe its command exactly once.");
            AssertBlocked();
            if (owner is RibbonSplitButton split)
            {
                Require(split.IsEnabled && split.IsButtonEnabled && !split.IsPrimaryActionEnabled,
                    "An unavailable split command disabled the dropdown or rewrote the user's primary permission.");
                split.Items.Add(new Fluent.MenuItem { Header = "Dropdown stays available" });
                split.OnKeyTipPressed();
                await settle();
                Require(split.IsDropDownOpen, "Command unavailability disabled the split dropdown.");
                split.IsDropDownOpen = false;
                split.IsButtonEnabled = false;
                command.Allowed = true;
                command.Notify();
                await settle();
                Require(split.IsEnabled && !split.IsButtonEnabled && !split.IsPrimaryActionEnabled,
                    "Command re-enabling overwrote an explicit primary-action disable.");
                split.IsButtonEnabled = true;
                command.Allowed = false;
                command.Notify();
                await settle();
            }

            command.Allowed = true;
            command.Notify();
            await settle();
            Require(surface.ActionEnabled(), "CanExecuteChanged did not enable the command action.");
            surface.Invoke();
            Require(clicks == 1 && command.Executions == 1 && Equals(command.LastParameter, "go"),
                "Enabled input did not invoke exactly once with the current parameter.");
            surface.ResetState();

            owner.SetValue(surface.ParameterProperty, "blocked parameter");
            await settle();
            AssertBlocked(expectedClicks: 1, expectedExecutions: 1);
            owner.SetValue(surface.ParameterProperty, "go");
            await settle();
            Require(surface.ActionEnabled(), "A parameter change did not restore command availability.");

            command.Allowed = false;
            command.Notify();
            await settle();
            owner.IsEnabled = false;
            command.Allowed = true;
            command.Notify();
            await settle();
            Require(!owner.IsEnabled && !surface.ActionEnabled(),
                "Command re-enabling overwrote an explicit user IsEnabled=false.");
            owner.IsEnabled = true;

            var enabledModel = new EnabledModel();
            owner.SetBinding(Control.IsEnabledProperty, new Binding
            {
                Source = enabledModel, Path = new PropertyPath(nameof(EnabledModel.Enabled)), Mode = BindingMode.TwoWay,
            });
            var enabledBinding = owner.GetBindingExpression(Control.IsEnabledProperty)!.ParentBinding;
            command.Allowed = false;
            command.Notify();
            await settle();
            Require(enabledModel.Enabled, "Command coercion wrote false into the user's two-way enabled model.");
            enabledModel.Enabled = false;
            command.Allowed = true;
            command.Notify();
            await settle();
            Require(!owner.IsEnabled && !surface.ActionEnabled()
                    && ReferenceEquals(owner.GetBindingExpression(Control.IsEnabledProperty)?.ParentBinding, enabledBinding)
                    && enabledBinding.UpdateSourceTrigger == UpdateSourceTrigger.Default,
                "Command updates replaced the enabled binding or lost its false input.");
            enabledModel.Enabled = true;
            await settle();
            Require(surface.ActionEnabled(), "The enabled binding did not remain live.");
            command.Allowed = false;
            command.Notify();
            await settle();
            Require(enabledModel.Enabled, "A repeated constraint fed an effective disabled value back to its source.");
            owner.IsEnabled = false;
            command.Allowed = true;
            command.Notify();
            await settle();
            Require(!owner.IsEnabled && !surface.ActionEnabled()
                    && ReferenceEquals(owner.GetBindingExpression(Control.IsEnabledProperty)?.ParentBinding, enabledBinding),
                "Releasing a constraint lost an explicit desired disable underneath a live binding.");
            enabledModel.Enabled = true;
            await settle();
            Require(surface.ActionEnabled(), "The original binding stopped updating after an explicit constrained write.");

            var replacement = new ObservedCommand { ExpectedParameter = "go" };
            owner.SetValue(surface.CommandProperty, replacement);
            await settle();
            Require(command.Subscribers == 0 && replacement.Subscribers == 1,
                "Command replacement retained the old subscription or missed the new one.");
            command.Notify();
            Require(!surface.ActionEnabled(), "A retired command changed current availability.");
            host.Children.Remove(owner);
            await settle();
            Require(replacement.Subscribers == 0, "Unloading retained a command subscription.");
            replacement.Allowed = true;
            replacement.Notify();
            host.Children.Add(owner);
            await settle();
            Require(replacement.Subscribers == 1 && surface.ActionEnabled(),
                "Reload did not re-observe and recompute command availability.");

            if (owner is RibbonSplitButton qatSource)
            {
                var clone = (RibbonSplitButton)qatSource.CreateQuickAccessItem()!;
                host.Children.Add(clone);
                try
                {
                    await settle();
                    replacement.Allowed = false;
                    replacement.Notify();
                    await settle();
                    var before = clicks;
                    clone.IsChecked = false;
                    ExpectDisabled(() => new RibbonSplitButtonAutomationPeer(clone).Invoke());
                    Require(clicks == before && clone.IsChecked != true && !clone.IsPrimaryActionEnabled,
                        "Quick-access forwarding bypassed primary command availability.");
                }
                finally
                {
                    host.Children.Remove(clone);
                    await settle();
                }
            }

            owner.SetValue(surface.CommandProperty, null);
            await settle();
            Require(replacement.Subscribers == 0 && surface.ActionEnabled(),
                "Clearing Command did not release observation and restore the uncommanded action.");
        }
        finally
        {
            if (owner is RibbonSplitButton split)
            {
                split.IsDropDownOpen = false;
            }
            owner.SetValue(surface.CommandProperty, null);
            host.Children.Remove(owner);
            await settle();
        }

        void AssertBlocked(int expectedClicks = 0, int expectedExecutions = 0)
        {
            Require(!surface.ActionEnabled(), "CanExecute=false did not drive effective enabled state.");
            surface.Invoke();
            ExpectDisabled(surface.InvokeUia);
            Require(clicks == expectedClicks && command.Executions == expectedExecutions && !surface.StateChanged(),
                "A command-disabled input changed Click, command, check, or selection state.");
        }
    }

    private static async Task VerifyLoadedSplitClone(Panel host, Func<Task> settle, bool useFacade)
    {
        var command = new ObservedCommand { ExpectedParameter = "go" };
        RibbonSplitButton source = useFacade ? new Fluent.SplitButton() : new RibbonSplitButton();
        source.Header = useFacade ? "Facade QAT command source" : "Core QAT command source";
        source.Command = command;
        source.CommandParameter = "go";
        source.IsCheckable = true;
        source.IsDefinitive = false;
        source.Items.Add(new Fluent.MenuItem { Header = "QAT dropdown item" });
        var clone = (RibbonSplitButton)source.CreateQuickAccessItem()!;
        var sourceClicks = 0;
        var cloneClicks = 0;
        source.Click += (_, _) => sourceClicks++;
        clone.Click += (_, _) => cloneClicks++;
        ObservedCommand? replacement = null;
#if WINDOWS
        var canonicalPopupOwnerVerified = false;
#endif
        host.Children.Add(source);
        host.Children.Add(clone);
        try
        {
            await settle();
            var primary = FindPart(clone, "PART_Button");
            var dropdown = FindPart(clone, "PART_DropDownButton");
            Require(command.Subscribers == 2 && !primary.IsEnabled && dropdown.IsEnabled,
                "The source and loaded QAT clone did not independently observe the unavailable command.");
            host.Children.Remove(source);
            await settle();
            Require(!source.IsLoaded && clone.IsLoaded && command.Subscribers == 1,
                "Unloading the source removed the clone's observer or retained the source's observer.");

            command.Allowed = true;
            command.Notify();
            await settle();
            Require(primary.IsEnabled && dropdown.IsEnabled && clone.IsPrimaryActionEnabled,
                "A loaded QAT primary button did not re-enable while its original source was unloaded.");
            var peer = new RibbonSplitButtonAutomationPeer(clone);
            peer.Invoke();
            Require(command.Executions == 1 && sourceClicks == 1 && cloneClicks == 1
                    && source.IsChecked == true && clone.IsChecked == true,
                "QAT primary forwarding double-executed or lost click/check synchronization.");

            command.Allowed = false;
            command.Notify();
            await settle();
            Require(!primary.IsEnabled && dropdown.IsEnabled,
                "CanExecuteChanged did not disable only the loaded clone's primary action.");
            ExpectDisabled(peer.Invoke);
            Require(command.Executions == 1 && sourceClicks == 1 && cloneClicks == 1,
                "A disabled QAT primary still forwarded an invocation.");
            clone.OnKeyTipPressed();
            await settle();
            Require(clone.IsDropDownOpen, "The loaded QAT dropdown was disabled along with its primary command.");
#if WINDOWS
            canonicalPopupOwnerVerified = ReferenceEquals(source.ContainerFromItem(source.Items[0]), source.Items[0])
                                          && source.Items[0] is DependencyObject authoredItem
                                          && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(authoredItem), source);
#endif
            clone.IsDropDownOpen = false;
            await settle();

            command.Allowed = true;
            command.Notify();
            source.CommandParameter = "blocked";
            await settle();
            Require(!primary.IsEnabled && Equals(clone.CommandParameter, "blocked"),
                "A parameter change on the unloaded source was not observed by its loaded clone.");
            source.CommandParameter = "go";
            await settle();
            Require(primary.IsEnabled, "Restoring the source parameter left its clone disabled.");

            var primaryPermission = new EnabledModel();
            source.SetBinding(RibbonSplitButton.IsButtonEnabledProperty, new Binding
            {
                Source = primaryPermission, Path = new PropertyPath(nameof(EnabledModel.Enabled)),
                Mode = BindingMode.TwoWay,
            });
            primaryPermission.Enabled = false;
            command.Notify();
            await settle();
            Require(!primary.IsEnabled && !clone.IsButtonEnabled && dropdown.IsEnabled,
                "Command refresh overwrote the source's bound primary-action disable.");
            primaryPermission.Enabled = true;
            await settle();
            Require(primary.IsEnabled && primaryPermission.Enabled,
                "The source's bound primary permission stopped updating its clone.");

            var enabled = new EnabledModel();
            source.SetBinding(Control.IsEnabledProperty, new Binding
            {
                Source = enabled, Path = new PropertyPath(nameof(EnabledModel.Enabled)), Mode = BindingMode.TwoWay,
            });
            enabled.Enabled = false;
            command.Notify();
            await settle();
            Require(!clone.IsEnabled && !primary.IsEnabled && !dropdown.IsEnabled,
                "The loaded clone ignored a bound whole-control disable on its unloaded source.");
            enabled.Enabled = true;
            await settle();
            Require(clone.IsEnabled && primary.IsEnabled && dropdown.IsEnabled,
                "The clone lost the source's whole-control enabled binding.");

            replacement = new ObservedCommand { ExpectedParameter = "go" };
            source.Command = replacement;
            await settle();
            Require(command.Subscribers == 0 && replacement.Subscribers == 1
                    && ReferenceEquals(clone.Command, replacement) && !primary.IsEnabled,
                "Replacing a command on the unloaded source did not replace the clone's independent observer.");
            command.Notify();
            Require(!primary.IsEnabled, "A retired command re-enabled the QAT clone.");
            replacement.Allowed = true;
            replacement.Notify();
            await settle();
            Require(primary.IsEnabled, "The clone did not observe its replacement command.");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Require(clone.IsLoaded && ReferenceEquals(peer.Owner, clone),
                "Retiring another popup detached the retained split-clone peer from its live owner.");
            peer.Invoke();
            Require(replacement.Executions == 1 && command.Executions == 1
                    && sourceClicks == 2 && cloneClicks == 2,
                "The replacement command was executed more than once or forwarded to the retired command.");
#if WINDOWS
            Require(canonicalPopupOwnerVerified,
                "The retained split popup item is not owned by the source's canonical native generator.");
#endif

            host.Children.Remove(clone);
            await settle();
            Require(replacement.Subscribers == 0 && command.Subscribers == 0,
                "Removing the clone retained a command observer after its source was already unloaded.");
            source.Command = command;
            command.Allowed = false;
            command.Notify();
            Require(command.Subscribers == 0 && replacement.Subscribers == 0,
                "An unloaded clone subscribed when the unloaded source changed commands.");
            host.Children.Add(clone);
            await settle();
            primary = FindPart(clone, "PART_Button");
            Require(command.Subscribers == 1 && !primary.IsEnabled,
                "Reloading the clone did not observe the current source command independently.");
        }
        finally
        {
            clone.IsDropDownOpen = false;
            source.IsDropDownOpen = false;
            host.Children.Remove(clone);
            host.Children.Remove(source);
            source.Command = null;
            await settle();
        }

        Require(command.Subscribers == 0 && (replacement?.Subscribers ?? 0) == 0,
            "Split-clone cleanup retained a command observer.");
    }

    private static Microsoft.UI.Xaml.Controls.Button FindPart(DependencyObject owner, string name)
        => FindElement<Microsoft.UI.Xaml.Controls.Button>(owner, name);

    private static T FindElement<T>(DependencyObject owner, string name) where T : FrameworkElement
        => FindElementOrNull<T>(owner, name)
           ?? throw new InvalidOperationException($"The control template did not provide {name} as {typeof(T).FullName}.");

    private static T? FindElementOrNull<T>(DependencyObject owner, string name) where T : FrameworkElement
    {
        if (owner is T element && element.Name == name)
        {
            return element;
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
        {
            if (FindElementOrNull<T>(VisualTreeHelper.GetChild(owner, index), name) is { } nested)
            {
                return nested;
            }
        }
        return null;
    }

    internal static async Task VerifySpinnerConversionAsync(Panel host, Func<Task> settle)
    {
        AssertPublicControlMetadata();
        var converter = new WordConverter();
        var spinner = new Fluent.Spinner
        {
            Header = "Custom conversion", Minimum = 0, Maximum = 50, Value = 7, TextToValueConverter = converter,
        };
        var focusSink = new Microsoft.UI.Xaml.Controls.Button { Content = "Move focus" };
        var panel = new StackPanel();
        panel.Children.Add(spinner);
        panel.Children.Add(focusSink);
        host.Children.Add(panel);
        try
        {
            await settle();
            var editor = FindElement<Microsoft.UI.Xaml.Controls.TextBox>(spinner, "PART_TextBox");
            Require(FocusInput(editor), "The spinner editor could not receive native focus.");
            await settle();
            converter.Clear();
            editor.Text = "forty-two";
            Require(SpinnerKey(spinner, VirtualKey.Enter), "Spinner Enter was not handled.");
            await settle();
            Require(spinner.Value == 42 && converter.Inputs.SequenceEqual(new[] { "forty-two" })
                    && converter.Formats == 1 && editor.Text == converter.Format(42, spinner.Format)
                    && spinner.Text == editor.Text,
                "Enter did not commit original custom text once and format the final value once.");

            spinner.Value = 7;
            Require(FocusInput(editor), "The spinner could not refocus for the focus-loss commit.");
            await settle();
            converter.Clear();
            editor.Text = "forty-two";
            Require(focusSink.Focus(FocusState.Programmatic), "The native focus-loss target could not be focused.");
            await settle();
            Require(spinner.Value == 42 && converter.Inputs.SequenceEqual(new[] { "forty-two" }) && converter.Formats == 1,
                "Focus loss parsed reformatted text, skipped conversion, or committed twice.");

            Require(FocusInput(editor), "The spinner could not refocus for invalid input.");
            await settle();
            converter.Clear();
            editor.Text = "invalid input";
            SpinnerKey(spinner, VirtualKey.Enter);
            await settle();
            Require(spinner.Value == 42 && converter.Inputs.SequenceEqual(new[] { "invalid input" })
                    && converter.Formats == 1 && editor.Text == converter.Format(42, spinner.Format),
                "Invalid custom input did not revert through the authoritative formatter.");

            spinner.Value = 12;
            Require(FocusInput(editor), "The spinner could not refocus for Escape.");
            await settle();
            converter.Clear();
            editor.Text = "forty-two";
            Require(!SpinnerKey(spinner, VirtualKey.Enter, handled: true)
                    && converter.Inputs.Count == 0 && editor.Text == "forty-two",
                "An already-handled editor key committed or reformatted the user's text.");
            SpinnerKey(spinner, VirtualKey.Escape);
            await settle();
            Require(spinner.Value == 12 && converter.Inputs.Count == 0 && converter.Formats == 1
                    && editor.Text == converter.Format(12, spinner.Format),
                "Escape committed dirty text or bypassed custom formatting.");

            converter.Clear();
            spinner.Value = 9;
            Require(converter.Formats == 1 && editor.Text == converter.Format(9, spinner.Format),
                "Value changes did not use the custom formatter exactly once.");
            converter.Clear();
            spinner.Format = "N2";
            Require(converter.Formats == 1 && editor.Text == converter.Format(9, "N2"),
                "Format changes bypassed the custom formatter.");
            var replacement = new WordConverter { Prefix = "replacement" };
            spinner.TextToValueConverter = replacement;
            Require(replacement.Formats == 1 && editor.Text == replacement.Format(9, "N2"),
                "Converter replacement did not refresh the editor.");
            Require(FocusInput(editor), "The spinner could not refocus for range coercion.");
            await settle();
            replacement.Clear();
            editor.Text = "too big";
            SpinnerKey(spinner, VirtualKey.Enter);
            await settle();
            Require(spinner.Value == 50 && replacement.Inputs.SequenceEqual(new[] { "too big" })
                    && replacement.Formats == 1 && editor.Text == replacement.Format(50, spinner.Format),
                "Custom conversion bypassed range coercion or formatted an uncoerced value.");

            var range = (IRangeValueProvider)new RibbonSpinnerAutomationPeer(spinner);
            replacement.Clear();
            range.SetValue(13);
            Require(range.Value == 13 && range.Minimum == 0 && range.Maximum == 50
                    && replacement.Formats == 1 && editor.Text == replacement.Format(13, spinner.Format),
                "Native range-value automation diverged from custom text formatting.");
            spinner.TextToValueConverter = Fluent.Converters.SpinnerTextToValueConverter.DefaultInstance;
            spinner.Format = "F1";
            spinner.Value = 3;
            Require(editor.Text == 3d.ToString("F1", CultureInfo.CurrentCulture),
                "Restoring the default converter changed normal formatting.");
        }
        finally
        {
            host.Children.Remove(panel);
            await settle();
        }
    }

    private static void ExpectDisabled(Action action)
    {
        try
        {
            action();
        }
        catch (ElementNotEnabledException)
        {
            return;
        }
        throw new InvalidOperationException("A command-disabled UIA action did not report ElementNotEnabledException.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record CommandSurface(
        Control Owner, DependencyProperty CommandProperty, DependencyProperty ParameterProperty,
        Action Invoke, Action InvokeUia, Func<bool> ActionEnabled, Func<bool> StateChanged, Action ResetState,
        Action<RoutedEventHandler> SubscribeClick);

    private sealed class ObservedCommand : ICommand
    {
        private EventHandler? changed;
        internal bool Allowed { get; set; }
        internal object? ExpectedParameter { get; set; }
        internal object? LastParameter { get; private set; }
        internal int Executions { get; private set; }
        internal int Subscribers { get; private set; }
        public event EventHandler? CanExecuteChanged
        {
            add { changed += value; Subscribers++; }
            remove { changed -= value; Subscribers--; }
        }
        public bool CanExecute(object? parameter) => Allowed && Equals(parameter, ExpectedParameter);
        public void Execute(object? parameter) { Executions++; LastParameter = parameter; }
        internal void Notify() => changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class EnabledModel : INotifyPropertyChanged
    {
        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set { enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class WordConverter : IValueConverter
    {
        internal List<string> Inputs { get; } = [];
        internal int Formats { get; private set; }
        internal string Prefix { get; set; } = "custom";
        internal string Format(double value, string format) => $"{Prefix}:{value.ToString("0.##", CultureInfo.InvariantCulture)}:{format}";
        internal void Clear() { Inputs.Clear(); Formats = 0; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Require(value is string && targetType == typeof(double) && parameter is Tuple<string, double>,
                "The custom parser received the wrong input contract.");
            var text = (string)value;
            Inputs.Add(text);
            return text switch { "forty-two" => 42d, "too big" => 1000d, _ => DependencyProperty.UnsetValue };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Require(value is double && targetType == typeof(string) && parameter is string,
                "The custom formatter received the wrong input contract.");
            Formats++;
            return Format((double)value, (string)parameter);
        }
    }

    // Use registered public controls. Private managed Control subclasses have no
    // native XAML type registration and can be projected as ContentControl on WinUI.
    private static readonly MethodInfo GalleryDownMethod = GetInputMethod(
        typeof(RibbonGalleryItem), "HandleActivationKeyDown", typeof(bool), typeof(VirtualKey), typeof(bool), typeof(bool));
    private static readonly MethodInfo GalleryUpMethod = GetInputMethod(
        typeof(RibbonGalleryItem), "HandleActivationKeyUp", typeof(bool), typeof(VirtualKey), typeof(bool), typeof(bool));
    private static readonly MethodInfo GalleryPointerPressedMethod = GetInputMethod(
        typeof(RibbonGalleryItem), "HandleActivationPointerPressed", typeof(bool), typeof(bool), typeof(bool));
    private static readonly MethodInfo GalleryPointerReleasedMethod = GetInputMethod(
        typeof(RibbonGalleryItem), "HandleActivationPointerReleased", typeof(void));
    private static readonly MethodInfo MenuClickMethod = GetInputMethod(typeof(Fluent.MenuItem), "OnClick", typeof(void));
    private static readonly MethodInfo SplitPrimaryMethod = GetInputMethod(
        typeof(RibbonSplitButton), "InvokePrimaryAction", typeof(void));
    private static readonly MethodInfo SpinnerKeyMethod = GetInputMethod(
        typeof(RibbonSpinner), "HandleEditorKeyDown", typeof(bool), typeof(VirtualKey), typeof(bool));

    private static bool GalleryDown(RibbonGalleryItem item, VirtualKey key, bool handled = false, bool original = true)
        => InvokeBooleanInput(GalleryDownMethod, item, key, handled, original);

    private static bool GalleryUp(RibbonGalleryItem item, VirtualKey key, bool handled = false, bool original = true)
        => InvokeBooleanInput(GalleryUpMethod, item, key, handled, original);

    private static bool GalleryPointerPress(RibbonGalleryItem item, bool primary = true)
        => InvokeBooleanInput(GalleryPointerPressedMethod, item, false, primary);

    private static bool SpinnerKey(RibbonSpinner spinner, VirtualKey key, bool handled = false)
        => InvokeBooleanInput(SpinnerKeyMethod, spinner, key, handled);

    private static MethodInfo GetInputMethod(Type type, string name, Type returnType, params Type[] parameters)
    {
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            binder: null, parameters, modifiers: null)
                     ?? throw new MissingMethodException(type.FullName, name);
        Require(method.ReturnType == returnType && (method.IsFamily || method.IsFamilyOrAssembly),
            $"{type.FullName}.{name} no longer has the expected protected input contract.");
        return method;
    }

    private static bool InvokeBooleanInput(MethodInfo method, object target, params object[] arguments)
        => InvokeInput(method, target, arguments) is bool handled
            ? handled
            : throw new InvalidOperationException($"{method.DeclaringType?.FullName}.{method.Name} did not return a Boolean.");

    private static object? InvokeInput(MethodInfo method, object target, params object[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static bool FocusInput(Microsoft.UI.Xaml.Controls.TextBox editor)
    {
        var tabStop = editor.IsTabStop;
        try { editor.IsTabStop = true; return editor.Focus(FocusState.Programmatic); }
        finally { editor.IsTabStop = tabStop; }
    }

    private static void AssertPublicControlMetadata()
    {
#if WINDOWS
        var provider = Application.Current as Microsoft.UI.Xaml.Markup.IXamlMetadataProvider
                       ?? throw new InvalidOperationException("The native Showcase does not expose XAML type metadata.");
        foreach (var type in new[]
                 {
                     typeof(Fluent.MenuItem), typeof(RibbonGalleryItem), typeof(GalleryItem),
                     typeof(RibbonSplitButton), typeof(Fluent.SplitButton), typeof(RibbonSpinner), typeof(Fluent.Spinner),
                 })
        {
            var xamlType = provider.GetXamlType(type);
            Require(xamlType is not null && xamlType.UnderlyingType == type,
                $"The public control {type.FullName} is not registered with its native XAML type.");
        }
#endif
    }
}
