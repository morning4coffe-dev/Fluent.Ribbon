namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using MenuItem = Fluent.MenuItem;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
#if WINDOWS
using DropDownPresentation = Microsoft.UI.Xaml.Controls.Primitives.Popup;
#else
using DropDownPresentation = Microsoft.UI.Xaml.Controls.Flyout;
#endif

internal static class PortPopupContractTests
{
    public static async Task VerifyHeightIntentRepairAsync(Panel host, Func<Task> settle)
    {
        var owner = new Fluent.DropDownButton
        {
            Header = "Height request regression",
            ResizeMode = ContextMenuResizeMode.Both,
            MaxDropDownHeight = 320,
            DropDownHeight = 180,
        };
        owner.Items.Add(new Microsoft.UI.Xaml.Controls.TextBox { Text = "Actual popup content", MinWidth = 180 });
        host.Children.Add(owner);
        try
        {
            await settle();
            owner.IsDropDownOpen = true;
            await settle();
            var resize = RequireDropDownContent(Presentation(owner));
            await VerifyBothResize(resize, settle);
            owner.MaxDropDownHeight = 140;
            await settle();
            Near(resize.ActualHeight, 140, "The current popup did not clamp its height.");
            owner.MaxDropDownHeight = 320;
            owner.DropDownHeight = 180;
            await settle();
            Near(resize.ActualHeight, 180, "An unchanged DropDownHeight assignment did not restore its requested height.");
            await VerifyHeightIntentRestoration(owner, resize, settle);
        }
        finally
        {
            owner.IsDropDownOpen = false;
            host.Children.Remove(owner);
            await settle();
        }
    }

    public static async Task VerifySubmenuReopenRepairAsync(Panel host, Func<Task> settle)
    {
        var command = new PopupCommand { Allowed = false };
        var owner = new MenuItem { Header = "Split submenu regression", IsSplit = true, Command = command };
        var child = new MenuItem { Header = "Real keyboard target" };
        owner.Items.Add(child);
        var ancestor = new Fluent.DropDownButton { Header = "Reopen ancestor" };
        ancestor.Items.Add(owner);
        host.Children.Add(ancestor);
        try
        {
            await settle();
            ancestor.IsDropDownOpen = true;
            await settle();
            RequireDropDownContent(Presentation(ancestor));
            await VerifyRapidSubmenuReopen(ancestor, owner, child, command, settle);
            Require(command.Executions == 0, "A disabled split primary executed during submenu reopening.");
        }
        finally
        {
            owner.IsDropDownOpen = false;
            ancestor.IsDropDownOpen = false;
            host.Children.Remove(ancestor);
            await settle();
        }
    }

    public static async Task VerifyPopupOptionsAsync(Panel host, Func<Task> settle)
    {
        var surface = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
        var command = new PopupCommand();
        var leaf = new MenuItem { Header = "Popup command", Command = command };
        var submenu = new MenuItem
        {
            Header = "Resizable submenu", ResizeMode = ContextMenuResizeMode.Both, MaxDropDownHeight = 220,
        };
        submenu.Items.Add(leaf);
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Popup input", MinWidth = 180 };
        var items = new ObservableCollection<UIElement> { submenu, editor };
        var dropDown = new Fluent.DropDownButton
        {
            Header = "Popup options", ItemsSource = items,
            DismissOnClickOutside = false, ResizeMode = ContextMenuResizeMode.Both,
            DropDownHeight = 180, MaxDropDownHeight = 320,
        };
        surface.Children.Add(dropDown);
        host.Children.Add(surface);
#if WINDOWS
        NativePopupInputUnavailableException? unavailableInput = null;
#endif
        try
        {
            await settle();
            var opened = 0;
            dropDown.DropDownOpened += (_, _) => opened++;
            dropDown.IsDropDownOpen = true;
            dropDown.IsDropDownOpen = false;
            await settle();
            Require(opened == 0 && !dropDown.IsDropDownOpen,
                "A canceled deferred dropdown opening still opened its native flyout.");

            dropDown.IsDropDownOpen = true;
            await settle();
            var flyout = Presentation(dropDown);
            var resize = RequireDropDownContent(flyout);
            await VerifyBothResize(resize, settle);
            dropDown.MaxDropDownHeight = 140;
            await settle();
            Require(resize.ActualHeight <= 141 && resize.Height <= 140,
                "A live MaxDropDownHeight change did not constrain the rendered dropdown.");
            dropDown.MaxDropDownHeight = 320;
            dropDown.DropDownHeight = 180;
            await settle();
            Near(resize.ActualHeight, 180, "A live DropDownHeight change did not resize its popup.");
            await VerifyHeightIntentRestoration(dropDown, resize, settle);
            dropDown.ResizeMode = ContextMenuResizeMode.Vertical;
            await settle();
            await VerifyVerticalResize(resize, settle);
            dropDown.ResizeMode = ContextMenuResizeMode.None;
            await settle();
            Require(!Descendants<Control>(resize).Any(IsVisibleResizeHandle),
                "ResizeMode.None left an interactive resize handle.");
            dropDown.ResizeMode = ContextMenuResizeMode.Both;
            await settle();

#if WINDOWS
            var outside = new NativeButton
            {
                Content = "Native light-dismiss input target", Width = 200, Height = 40,
                Margin = new Thickness(500, 360, 0, 0),
            };
            AutomationProperties.SetAutomationId(outside, "NativePopupOutsideDismissTarget");
            surface.Children.Add(outside);
            try
            {
                var clicks = 0;
                outside.Click += (_, _) => clicks++;
                await settle();
                Require(!flyout.IsLightDismissEnabled && flyout.IsOpen,
                    "DismissOnClickOutside=false did not disable native Popup light dismissal.");
                await NativePopupInputContract.WaitForExternalClickAsync(outside);
                await settle();
                Require(clicks == 1 && dropDown.IsDropDownOpen && resize.IsLoaded,
                    "Real outside input did not pass through or incorrectly dismissed the persistent popup.");
                dropDown.DismissOnClickOutside = true;
                Require(flyout.IsLightDismissEnabled && flyout.IsOpen,
                    "The live dismissal option did not reach the actual native Popup.");
                await NativePopupInputContract.WaitForExternalClickAsync(outside, flyout);
                await settle();
                Require(!dropDown.IsDropDownOpen && !flyout.IsOpen && !resize.IsLoaded,
                    "Real outside pointer input did not close the native Popup and requested state.");
            }
            catch (NativePopupInputUnavailableException exception)
            {
                unavailableInput = exception;
                App.LogAutoTestStartup($"NATIVE OUTSIDE INPUT BLOCKED {exception.Message}");
                dropDown.IsDropDownOpen = false;
                await settle();
                Require(!flyout.IsOpen && !resize.IsLoaded,
                    "Explicit native closure failed while outside-input verification was unavailable.");
            }
            finally
            {
                surface.Children.Remove(outside);
            }
#else
            // Hide with the owner's requested state still true takes the real
            // Flyout.Closing route used by native light dismissal.
            flyout.Hide();
            await settle();
            Require(dropDown.IsDropDownOpen && resize.IsLoaded,
                "DismissOnClickOutside=false did not cancel native light dismissal.");
            dropDown.DismissOnClickOutside = true;
            flyout.Hide();
            await settle();
            Require(!dropDown.IsDropDownOpen && !resize.IsLoaded,
                "Changing DismissOnClickOutside to true did not restore native dismissal.");
#endif
            dropDown.DismissOnClickOutside = false;
            await OpenDropDown();
            Require(Invoke<bool>(dropDown, "HandleDropDownPopupKey", VirtualKey.Escape),
                "The popup's real Escape handler did not consume Escape.");
            await settle();
            Require(!dropDown.IsDropDownOpen && !resize.IsLoaded,
                "Outside-dismiss suppression incorrectly suppressed Escape.");

            await OpenDropDown();
            submenu.IsDropDownOpen = true;
            await settle();
            var submenuContent = RequirePopupContent(submenu.DropDownPopup);
            await VerifyBothResize(Find<ResizeableContentControl>(submenuContent), settle);
            AssertSubmenuPlacement(submenu);
            Require(dropDown.IsDropDownOpen && submenu.IsDropDownOpen,
                "Submenu resizing closed its real parent popup.");
            PopupService.RaiseDismissPopupEvent(submenu, DismissPopupMode.Always);
            Require(dropDown.IsDropDownOpen,
                "Nested opening dismissal did not preserve the dropdown's actual ancestor.");
            new RibbonMenuItemAutomationPeer(leaf).Invoke();
            await settle();
            Require(command.Executions == 1 && !submenu.IsDropDownOpen && !dropDown.IsDropDownOpen,
                "A definitive leaf command failed to close the chain through a persistent dropdown.");

            await OpenDropDown();
            dropDown.ClosePopupOnMouseDown = true;
            dropDown.ClosePopupOnMouseDownDelay = -10;
            Require(dropDown.ClosePopupOnMouseDownDelay == -10,
                "The delay setter coerced the stored WPF value instead of applying the minimum when scheduling.");
            var handle = ResizeHandle(resize, both: true);
            Invoke<object?>(dropDown, "HandleDropDownPopupMouseDown", handle);
            Require(Field<object?>(dropDown, "_mouseDownCloseTimer") is null && dropDown.IsDropDownOpen,
                "Pressing a real resize handle scheduled mouse-down dismissal.");
            var closed = new TaskCompletionSource<bool>();
            EventHandler onClosed = (_, _) => closed.TrySetResult(true);
            dropDown.DropDownClosed += onClosed;
            var clock = Stopwatch.StartNew();
            try
            {
                Invoke<object?>(dropDown, "HandleDropDownPopupMouseDown", editor);
                Require(dropDown.IsDropDownOpen, "Mouse-down dismissal ran before the input route completed.");
                Require(Field<DispatcherTimer>(dropDown, "_mouseDownCloseTimer").Interval >= TimeSpan.FromMilliseconds(100),
                    "The actual close timer did not apply WPF's 100 ms minimum.");
                await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Require(clock.Elapsed >= TimeSpan.FromMilliseconds(100),
                    "Mouse-down dismissal occurred before WPF's minimum delay.");
            }
            finally
            {
                dropDown.DropDownClosed -= onClosed;
            }

            Require(!dropDown.IsDropDownOpen && command.Executions == 1,
                "Mouse-down dismissal invoked a command or honored outside-dismiss suppression.");
            await OpenDropDown();
            dropDown.ClosePopupOnMouseDownDelay = 350;
            Invoke<object?>(dropDown, "HandleDropDownPopupMouseDown", editor);
            var canceledTimer = Field<DispatcherTimer>(dropDown, "_mouseDownCloseTimer");
            dropDown.IsDropDownOpen = false;
            await OpenDropDown();
            Require(!canceledTimer.IsEnabled && Field<object?>(dropDown, "_mouseDownCloseTimer") is null,
                "A stale mouse-down timer survived explicit close/reopen.");
            Invoke<object?>(dropDown, "HandleDropDownPopupMouseDown", editor);
            canceledTimer = Field<DispatcherTimer>(dropDown, "_mouseDownCloseTimer");
            dropDown.ClosePopupOnMouseDown = false;
            Require(!canceledTimer.IsEnabled && Field<object?>(dropDown, "_mouseDownCloseTimer") is null,
                "Disabling ClosePopupOnMouseDown left a pending close active.");

            var added = new MenuItem { Header = "Added while open" };
            items.Add(added);
            await settle();
            Require(added.IsLoaded && dropDown.IsDropDownOpen && ReferenceEquals(PresentationContent(flyout), resize),
                "A live collection edit recreated or closed the dropdown presentation.");
            var replacement = new MenuItem { Header = "Replacement source" };
            dropDown.ItemsSource = new ObservableCollection<UIElement> { replacement };
            await settle();
            Require(replacement.IsLoaded && !added.IsLoaded && dropDown.IsDropDownOpen,
                "ItemsSource replacement did not update the actual open dropdown.");
            host.Children.Remove(surface);
            await settle();
            Require(!dropDown.IsDropDownOpen && !resize.IsLoaded,
                "Unloading a persistent dropdown left its popup alive.");
            host.Children.Add(surface);
            await OpenDropDown();
            Require(replacement.IsLoaded, "Reloading the dropdown lost its live popup content.");
            dropDown.IsDropDownOpen = false;
            await settle();

            await VerifyComboPopup(new RibbonComboBox(), surface, settle);
            await VerifyComboPopup(new Fluent.ComboBox(), surface, settle);
            await VerifyContextMenu(surface, settle);
#if WINDOWS
            App.LogAutoTestStartup("NATIVE POPUP NONPOINTER CONTRACTS COMPLETE");
            if (unavailableInput is not null)
            {
                ExceptionDispatchInfo.Capture(unavailableInput).Throw();
            }
#endif

            async Task OpenDropDown()
            {
                dropDown.IsDropDownOpen = true;
                await settle();
                var currentFlyout = Presentation(dropDown);
                var currentRoot = PresentationContent(currentFlyout) as ResizeableContentControl;
                Require(dropDown.IsDropDownOpen && currentFlyout.IsOpen
                        && currentRoot is { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 },
                    "The dropdown did not open its real live popup content. "
                    + $"Requested={dropDown.IsDropDownOpen}, OwnerLoaded={dropDown.IsLoaded}, "
                    + $"NativeOpen={currentFlyout.IsOpen}, "
                    + $"Closing={Field<bool>(dropDown, "_flyoutIsClosing")}, "
                    + $"Opened={Field<bool>(dropDown, "_flyoutIsOpen")}, "
                    + $"RootLoaded={currentRoot?.IsLoaded}, "
                    + $"ActiveAnchor={Field<object?>(dropDown, "_activePopupAnchor") is not null}, "
                    + $"PendingAnchor={Field<object?>(dropDown, "_pendingPopupAnchor") is not null}");
            }
        }
        finally
        {
            submenu.IsDropDownOpen = false;
            dropDown.IsDropDownOpen = false;
            host.Children.Remove(surface);
            await settle();
        }
    }

    public static async Task VerifyMenuPresentationAsync(Panel host, Func<Task> settle)
    {
        var command = new PopupCommand();
        var leafCommand = new PopupCommand();
        var checkCommand = new PopupCommand();
        var check = new MenuItem
        {
            Header = "_Check", IsCheckable = true, IsDefinitive = false, Command = checkCommand,
        };
        var firstRadio = new MenuItem
        {
            Header = "First option", IsCheckable = true, GroupName = "popup-options", IsChecked = true,
            IsDefinitive = false,
        };
        var secondRadio = new MenuItem
        {
            Header = "Second option", IsCheckable = true, GroupName = "popup-options", IsDefinitive = false,
        };
        var split = new MenuItem
        {
            Header = "_Run__ item", IsSplit = true, Command = command, IsDefinitive = false,
            ResizeMode = ContextMenuResizeMode.Vertical,
        };
        var leaf = new MenuItem { Header = "Nested command", Command = leafCommand };
        split.Items.Add(leaf);
        var data = new ObservableCollection<PopupLabel> { new("First bound label") };
        var dataMenu = new MenuItem
        {
            Header = "Data templates", ItemsSource = data, ItemTemplate = LabelTemplate("PortPopupLabel"),
            ResizeMode = ContextMenuResizeMode.Both,
        };
        var dropDown = new Fluent.DropDownButton { Header = "Menu presentation", DismissOnClickOutside = false };
        foreach (var item in new[] { check, firstRadio, secondRadio, split, dataMenu })
        {
            dropDown.Items.Add(item);
        }

        host.Children.Add(dropDown);
        Exception? verificationFailure = null;
        try
        {
            await Open();
            var checkMark = Part<FrameworkElement>(check, "CheckMark");
            Require(Part<FrameworkElement>(check, "CheckIndicator").Visibility == Visibility.Visible
                    && checkMark.Visibility == Visibility.Collapsed,
                "The default template does not present an unchecked checkable menu item.");
            InvokeButton(Part<NativeButton>(check, "PART_PrimaryButton"));
            await settle();
            Require(check.IsChecked == true && checkMark.Visibility == Visibility.Visible
                    && checkMark.ActualWidth > 0 && checkMark.ActualHeight > 0 && checkCommand.Executions == 1,
                "A primary menu activation did not toggle/render/execute exactly once. "
                + $"checked={check.IsChecked}, visibility={checkMark.Visibility}, "
                + $"size={checkMark.ActualWidth}x{checkMark.ActualHeight}, executions={checkCommand.Executions}, loaded={check.IsLoaded}.");
            var checkPeer = new RibbonMenuItemAutomationPeer(check);
            Require(checkPeer.GetAutomationControlType() == AutomationControlType.MenuItem
                    && checkPeer.ToggleState == ToggleState.On,
                "The checked item has incorrect menu/toggle automation semantics.");
            check.IsChecked = null;
            await settle();
            Require(Part<FrameworkElement>(check, "IndeterminateMark").Visibility == Visibility.Visible
                    && checkPeer.ToggleState == ToggleState.Indeterminate,
                "The default template or automation omitted the indeterminate state.");
            checkPeer.Toggle();
            await settle();
            Require(check.IsChecked == true && checkCommand.Executions == 1,
                "UIA Toggle did not update the visible state or incorrectly executed the command.");

            InvokeButton(Part<NativeButton>(secondRadio, "PART_PrimaryButton"));
            await settle();
            Require(firstRadio.IsChecked == false && secondRadio.IsChecked == true
                    && Part<FrameworkElement>(secondRadio, "RadioMark").Visibility == Visibility.Visible
                    && Part<FrameworkElement>(secondRadio, "CheckMark").Visibility == Visibility.Collapsed,
                "Grouped menu activation failed mutual exclusion or rendered a check instead of a radio marker.");
            InvokeButton(Part<NativeButton>(secondRadio, "PART_PrimaryButton"));
            Require(secondRadio.IsChecked == true, "Activating the selected radio item deselected the entire group.");
            var radioPeer = new RibbonMenuItemAutomationPeer(firstRadio);
            radioPeer.Select();
            await settle();
            Require(firstRadio.IsChecked == true && secondRadio.IsChecked == false
                    && radioPeer.GetPattern(PatternInterface.SelectionItem) is not null,
                "Radio UIA selection bypassed the mutually exclusive presentation.");
            secondRadio.GroupName = "other-group";
            secondRadio.IsChecked = true;
            secondRadio.GroupName = firstRadio.GroupName;
            Require(firstRadio.IsChecked == false && secondRadio.IsChecked == true,
                "A checked item's dynamic GroupName change did not reconcile the new group.");
            secondRadio.IsCheckable = false;
            await settle();
            Require(Part<FrameworkElement>(secondRadio, "CheckIndicator").Visibility == Visibility.Collapsed,
                "Disabling IsCheckable retained a visible check/radio affordance.");

            var primary = Part<NativeButton>(split, "PART_PrimaryButton");
            var arrow = Part<NativeButton>(split, "PART_SubmenuButton");
            Require(arrow.Visibility == Visibility.Visible && arrow.ActualWidth >= 24 && arrow.ActualHeight >= 24,
                "The split menu has no meaningful independent submenu hit target.");
            AssertDistinctTargets(primary, arrow);
            var splitPeer = new RibbonMenuItemAutomationPeer(split);
            Require(splitPeer.GetPattern(PatternInterface.Invoke) is not null
                    && splitPeer.GetPattern(PatternInterface.ExpandCollapse) is not null,
                "A split menu does not expose distinct primary and expansion automation patterns.");
            InvokeButton(primary);
            await settle();
            Require(command.Executions == 1 && !split.IsDropDownOpen && dropDown.IsDropDownOpen,
                "The split primary action opened its submenu, double-invoked, or ignored IsDefinitive=false.");
            InvokeButton(arrow);
            await settle();
            Require(command.Executions == 1 && split.IsDropDownOpen && split.DropDownPopup?.IsOpen == true,
                "The independent submenu action ran the primary command or failed to open its popup.");
            new RibbonMenuItemAutomationPeer(leaf).Invoke();
            await settle();
            Require(leafCommand.Executions == 1 && command.Executions == 1
                    && !split.IsDropDownOpen && !dropDown.IsDropDownOpen,
                "A split submenu leaf did not execute once and dismiss its ancestor chain.");

            await Open();
            command.Allowed = false;
            command.Notify();
            await settle();
            Require(split.IsEnabled && !primary.IsEnabled && arrow.IsEnabled,
                "An unavailable split primary command incorrectly disabled submenu access.");
            ExpectDisabled(splitPeer.Invoke);
            splitPeer.Expand();
            await settle();
            Require(split.IsDropDownOpen && leaf.IsEnabled, "UIA could not expand a command-disabled split primary.");
            splitPeer.Collapse();
            Require(command.Executions == 1, "Expanding/collapsing invoked a disabled split command.");
            split.OnKeyTipPressed();
            await settle();
            Require(split.IsDropDownOpen && command.Executions == 1,
                "The public menu KeyTip route disabled expansion with the split primary command.");
            await VerifyRapidSubmenuReopen(dropDown, split, leaf, command, settle);
            split.OnKeyTipBack();
            split.IsEnabled = false;
            command.Allowed = true;
            command.Notify();
            Require(!split.IsEnabled, "Command availability overwrote authored menu IsEnabled=false.");
            split.IsEnabled = true;

            Require(Part<TextBlock>(split, "AccessKeyText").Text == "Run_ item"
                    && split.AccessKey == "R" && splitPeer.GetName() == "Run_ item",
                "RecognizesAccessKey did not strip/escape markers or expose the meaningful automation name.");
            Require(Invoke<bool>(split, "InvokeMenuAccessKey"), "The real access-key route did not activate a split submenu.");
            await settle();
            Require(split.IsDropDownOpen && command.Executions == 1,
                "A split access key incorrectly invoked the primary action.");
            split.IsDropDownOpen = false;
            split.RecognizesAccessKey = false;
            await settle();
            Require(split.AccessKey == string.Empty
                    && Part<ContentPresenter>(split, "HeaderPresenter").Visibility == Visibility.Visible
                    && splitPeer.GetName() == "_Run__ item",
                "RecognizesAccessKey=false did not restore literal header presentation. "
                + $"AccessKey='{split.AccessKey}', Header='{split.Header}', Name='{splitPeer.GetName()}', "
                + $"Presenter={Part<ContentPresenter>(split, "HeaderPresenter").Visibility}.");
            split.AccessKey = "X";
            split.RecognizesAccessKey = true;
            split.Header = "_Launch";
            await settle();
            Require(split.AccessKey == "X" && Part<TextBlock>(split, "AccessKeyText").Text == "Launch",
                "Access-key presentation overwrote an explicitly authored native AccessKey.");
            await VerifyAccessKeyHeaderChanges(split, command, settle);

            split.FlowDirection = FlowDirection.RightToLeft;
            Require(Invoke<bool>(split, "HandleMenuNavigationKey", VirtualKey.Left),
                "RTL left-arrow navigation did not open the actual submenu.");
            await settle();
            AssertDistinctTargets(primary, arrow);
            AssertWithinViewport(RequirePopupContent(split.DropDownPopup));
            AssertSubmenuPlacement(split);
            Require(Invoke<bool>(leaf, "HandleMenuNavigationKey", VirtualKey.Escape),
                "Submenu Escape did not navigate to its owning split item.");
            await settle();
            Require(!split.IsDropDownOpen && dropDown.IsDropDownOpen
                    && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(split.XamlRoot), split),
                "Submenu Escape closed its outer popup or failed to restore owner focus.");

            dataMenu.IsDropDownOpen = true;
            await settle();
            var dataRoot = RequirePopupContent(dataMenu.DropDownPopup);
            Require(Descendants<TextBlock>(dataRoot).Any(text => text.Name == "PortPopupLabel"
                                                              && text.Text == data[0].Title),
                "Menu presentation bypassed the phase-one ItemTemplate source containers.");
            var retained = dataMenu.ContainerFromItem(data[0]);
            data.Add(new PopupLabel("Second bound label"));
            dataMenu.ItemTemplate = LabelTemplate("PortPopupAlternateLabel");
            await settle();
            Require(ReferenceEquals(retained, dataMenu.ContainerFromItem(data[0]))
                    && Descendants<TextBlock>(dataRoot).Count(text => text.Name == "PortPopupAlternateLabel") == 2,
                "Live submenu source/template changes lost retained containers or rendered stale templates.");
            dataMenu.MaxDropDownHeight = 120;
            await settle();
            Require(Find<ResizeableContentControl>(dataRoot).ActualHeight <= 121,
                "Source-backed submenu content ignored a live height constraint.");
            dataMenu.IsDropDownOpen = false;

            dropDown.RequestedTheme = ElementTheme.Light;
            await settle();
            var light = (Part<Border>(check, "CheckBoxOutline").BorderBrush as SolidColorBrush)?.Color;
            dropDown.RequestedTheme = ElementTheme.Dark;
            await settle();
            var dark = (Part<Border>(check, "CheckBoxOutline").BorderBrush as SolidColorBrush)?.Color;
            Require(light is not null && dark is not null && light != dark,
                "The checked menu presentation did not follow a live Light/Dark theme change.");

            async Task Open()
            {
                dropDown.IsDropDownOpen = true;
                await settle();
                RequireDropDownContent(Presentation(dropDown));
            }
        }
        catch (Exception exception)
        {
            verificationFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                split.IsDropDownOpen = false;
                dataMenu.IsDropDownOpen = false;
                dropDown.IsDropDownOpen = false;
                host.Children.Remove(dropDown);
                await settle();
                Require(command.Subscribers == 0 && leafCommand.Subscribers == 0 && checkCommand.Subscribers == 0,
                    "Unloaded menu presentation retained command subscriptions. "
                    + $"Primary={command.Subscribers}, leaf={leafCommand.Subscribers}, check={checkCommand.Subscribers}; "
                    + $"loaded={split.IsLoaded}/{leaf.IsLoaded}/{check.IsLoaded}.");
            }
            catch (Exception cleanupFailure) when (verificationFailure is not null)
            {
                throw new AggregateException("Menu verification and cleanup both failed.", verificationFailure, cleanupFailure);
            }
        }
    }

    private static async Task VerifyHeightIntentRestoration(
        Fluent.DropDownButton owner, ResizeableContentControl resize, Func<Task> settle)
    {
        var provider = ResizeProvider(ResizeHandle(resize, both: true));
        var scale = resize.XamlRoot.RasterizationScale;
        var width = resize.ActualWidth;
        provider.Resize(width * scale, 220 * scale);
        await settle();
        Near(resize.ActualHeight, 220, "The popup did not accept the user's resize intent.");
        owner.MaxDropDownHeight = 140;
        await settle();
        Near(resize.ActualHeight, 140, "The popup did not apply a temporary height constraint.");
        owner.MaxDropDownHeight = 320;
        await settle();
        Near(resize.ActualHeight, 220, "Relaxing the constraint discarded the user's requested height.");

        owner.MaxDropDownHeight = 140;
        await settle();
        provider.Resize(width * scale, 140 * scale);
        owner.MaxDropDownHeight = 320;
        await settle();
        Near(resize.ActualHeight, 140, "An explicit resize to the clamped size retained stale height intent.");
        owner.DropDownHeight = 180;
        await settle();
        Near(resize.ActualHeight, 180, "Reassigning the unchanged initial height did not reset the user's resize.");
    }

    private static async Task VerifyRapidSubmenuReopen(
        Fluent.DropDownButton ancestor, MenuItem owner, MenuItem firstChild,
        PopupCommand command, Func<Task> settle)
    {
        var peer = new RibbonMenuItemAutomationPeer(owner);
        var executions = command.Executions;
        var clicks = 0;
        RoutedEventHandler onClick = (_, _) => clicks++;
        owner.Click += onClick;
        try
        {
            for (var iteration = 0; iteration < 3; iteration++)
            {
                peer.Expand();
                await settle();
                RequirePopupContent(owner.DropDownPopup);
                var opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler onOpened = (_, _) => opened.TrySetResult(true);
                owner.DropDownOpened += onOpened;
                try
                {
                    peer.Collapse();
                    owner.OnKeyTipPressed();
                    await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    await settle();
                    var root = RequirePopupContent(owner.DropDownPopup);
                    Require(owner.IsDropDownOpen && ancestor.IsDropDownOpen
                            && firstChild.IsLoaded && firstChild.ActualWidth > 0 && firstChild.ActualHeight > 0
                            && IsDescendant(firstChild, root)
                            && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(owner.XamlRoot), firstChild),
                        "Rapid UIA collapse/KeyTip reopen lost the real popup, child geometry, ancestor, or child focus.");
                    Require(command.Executions == executions && clicks == 0,
                        "Rapid submenu reopening invoked the command-disabled primary action.");
                }
                finally
                {
                    owner.DropDownOpened -= onOpened;
                }
            }

            peer.Collapse();
            owner.OnKeyTipPressed();
            owner.OnKeyTipBack();
            await settle();
            Require(!owner.IsDropDownOpen && owner.DropDownPopup?.IsOpen == false && ancestor.IsDropDownOpen,
                "Canceling a rapid reopen left a native submenu open or closed its ancestor.");
        }
        finally
        {
            owner.Click -= onClick;
        }
    }

    private static async Task VerifyAccessKeyHeaderChanges(MenuItem owner, PopupCommand command, Func<Task> settle)
    {
        var model = new PopupHeaderModel { Header = "_First__ value", AccessKey = "Z" };
        var executions = command.Executions;
        var clicks = 0;
        RoutedEventHandler onClick = (_, _) => clicks++;
        owner.Click += onClick;
        try
        {
            owner.ClearValue(UIElement.AccessKeyProperty);
            owner.SetBinding(MenuItem.HeaderProperty, new Binding
            {
                Source = model, Path = new PropertyPath(nameof(PopupHeaderModel.Header)), Mode = BindingMode.OneWay,
            });
            await settle();
            Require(Equals(owner.Header, "_First__ value")
                    && Part<TextBlock>(owner, "AccessKeyText").Text == "First_ value" && owner.AccessKey == "F",
                "A bound header did not preserve its original text while presenting/registering its access key.");
            owner.RecognizesAccessKey = false;
            model.Header = "_Second__ value";
            await settle();
            Require(Equals(owner.Header, "_Second__ value")
                    && Equals(Part<ContentPresenter>(owner, "HeaderPresenter").Content, "_Second__ value")
                    && new RibbonMenuItemAutomationPeer(owner).GetName() == "_Second__ value"
                    && owner.AccessKey == string.Empty,
                "Disabling access-key recognition lost the bound literal header or retained a generated key.");
            owner.RecognizesAccessKey = true;
            await settle();
            Require(Part<TextBlock>(owner, "AccessKeyText").Text == "Second_ value" && owner.AccessKey == "S",
                "Re-enabling access-key recognition did not use the latest bound header.");
            owner.SetBinding(UIElement.AccessKeyProperty, new Binding
            {
                Source = model, Path = new PropertyPath(nameof(PopupHeaderModel.AccessKey)), Mode = BindingMode.OneWay,
            });
            model.Header = "_Third";
            await settle();
            Require(owner.AccessKey == "Z", "A header replacement overwrote the consumer's access-key binding.");
            owner.RecognizesAccessKey = false;
            model.AccessKey = "Y";
            model.Header = "_Fourth";
            await settle();
            Require(owner.AccessKey == "Y" && Equals(Part<ContentPresenter>(owner, "HeaderPresenter").Content, "_Fourth"),
                "Recognition changes detached the consumer's access-key binding or literal header binding.");
            Require(command.Executions == executions && clicks == 0,
                "Header/key replacement or recognition changes accidentally invoked a menu command.");
        }
        finally
        {
            owner.Click -= onClick;
            owner.ClearValue(MenuItem.HeaderProperty);
            owner.Header = "_Launch";
            owner.RecognizesAccessKey = true;
        }
    }

    private static async Task VerifyComboPopup(RibbonComboBox combo, Panel host, Func<Task> settle)
    {
        var top = new TextBlock { Text = "Native combo top content" };
        var menu = new MenuItem { Header = "Native combo footer" };
        combo.Header = "Resizable editable combo";
        combo.IsEditable = true;
        combo.InputWidth = 150;
        combo.ItemsSource = new ObservableCollection<string> { "First", "Second", "Third" };
        combo.SelectedIndex = 1;
        combo.TopPopupContent = top;
        combo.Menu = menu;
        combo.ResizeMode = ContextMenuResizeMode.Both;
        combo.DropDownHeight = 200;
        combo.MaxDropDownHeight = 320;
        host.Children.Add(combo);
        try
        {
            await settle();
            combo.IsDropDownOpen = true;
            await settle();
            var content = RequirePopupContent(((IDropDownControl)combo).DropDownPopup);
            var resize = Find<ResizeableContentControl>(content);
            Require(IsDescendant(top, resize) && IsDescendant(menu, resize),
                "The combo resize surface discarded native top content or the menu footer. "
                + $"Type={combo.GetType().Name}, TopLoaded={top.IsLoaded}, MenuLoaded={menu.IsLoaded}.");
            await VerifyBothResize(resize, settle);
            Require(content.ActualWidth + 1 >= resize.ActualWidth
                    && content.ActualHeight + 1 >= resize.ActualHeight,
                "The native combo popup clipped the resized content to its original dimensions. "
                + $"Outer={content.ActualWidth}x{content.ActualHeight} ({content.Width}x{content.Height}), "
                + $"Limits={content.MinWidth},{content.MaxWidth},{content.MinHeight},{content.MaxHeight}, "
                + $"Resize={resize.ActualWidth}x{resize.ActualHeight}, "
                + $"Popup={((IDropDownControl)combo).DropDownPopup!.Width}x{((IDropDownControl)combo).DropDownPopup!.Height}.");
            if (content is Border border)
            {
                Near(border.ActualHeight,
                    resize.ActualHeight + border.Padding.Top + border.Padding.Bottom
                    + border.BorderThickness.Top + border.BorderThickness.Bottom,
                    "The complete native combo popup did not follow the user's resized height.");
            }
            var replacementMenu = new MenuItem { Header = "Replacement combo footer" };
            combo.Menu = replacementMenu;
            await settle();
            Require(replacementMenu.IsLoaded && IsDescendant(replacementMenu, resize) && !menu.IsLoaded,
                "A live core/facade combo menu replacement did not update its actual popup footer.");
            combo.Menu = menu;
            await settle();
            combo.ResizeMode = ContextMenuResizeMode.Vertical;
            await settle();
            await VerifyVerticalResize(resize, settle);
            combo.MaxDropDownHeight = 180;
            await settle();
            Require(resize.ActualHeight <= 181, "The native combo ignored its live maximum popup height.");
            combo.SelectedIndex = 2;
            await settle();
            Require(Equals(combo.SelectedItem, "Third"),
                "Popup resizing replaced the platform-native combo selection path.");
            combo.Text = "Editable value";
            await settle();
            Require(Part<Microsoft.UI.Xaml.Controls.TextBox>(combo, "EditableText").Text == "Editable value",
                "Popup resizing broke the platform-native editable ComboBox text.");
            if (!combo.IsDropDownOpen)
            {
                combo.IsDropDownOpen = true;
                await settle();
            }

            Require(top.IsLoaded && menu.IsLoaded,
                "A selection/editable change lost the combo's supplemental popup content.");
            combo.IsDropDownOpen = false;
            await settle();
            combo.IsDropDownOpen = true;
            await settle();
            Require(RequirePopupContent(((IDropDownControl)combo).DropDownPopup).IsLoaded && top.IsLoaded,
                "The native combo lost popup ownership on reopen.");
        }
        finally
        {
            combo.IsDropDownOpen = false;
            host.Children.Remove(combo);
            await settle();
        }
    }

    private static async Task VerifyContextMenu(Panel host, Func<Task> settle)
    {
        var command = new PopupCommand();
        var nativeItem = new MenuFlyoutItem { Text = "Native context command", Command = command };
        var toggle = new ToggleMenuFlyoutItem { Text = "Native toggle", IsChecked = true };
        var context = new Fluent.ContextMenu { ResizeMode = ContextMenuResizeMode.Both };
        var presenterStyle = new Style(typeof(MenuFlyoutPresenter));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 180d));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 340d));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.MaxHeightProperty, 260d));
        context.MenuFlyoutPresenterStyle = presenterStyle;
        context.Items.Add(nativeItem);
        context.Items.Add(toggle);
        var anchor = new NativeButton { Content = "Context resize anchor", ContextFlyout = context };
        host.Children.Add(anchor);
        try
        {
            await settle();
            context.ShowAt(anchor);
            await settle();
            var presenter = Ancestor<MenuFlyoutPresenter>(nativeItem);
            var resize = Find<ResizeableContentControl>(presenter);
            Require(resize.IsLoaded && IsDescendant(nativeItem, presenter) && IsDescendant(toggle, presenter),
                "ContextMenu resizing replaced the native MenuFlyout presenter/items.");
            await VerifyBothResize(resize, settle);
            Require(presenter.ActualWidth <= 341 && presenter.ActualHeight <= 261 && toggle.IsChecked,
                "Native menu presenter constraints or toggle state were lost while resizing.");
            Require(presenter.ActualWidth + 1 >= resize.ActualWidth
                    && presenter.ActualHeight + 1 >= resize.ActualHeight,
                "The native menu presenter clipped its resized content.");
            context.ResizeMode = ContextMenuResizeMode.Vertical;
            await settle();
            await VerifyVerticalResize(resize, settle);
            Require(nativeItem.IsLoaded && resize.IsLoaded,
                "Resizing/focusing its handle prematurely dismissed the native context menu.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(nativeItem);
            var invoke = peer?.GetPattern(PatternInterface.Invoke) as IInvokeProvider
                         ?? throw new InvalidOperationException("The native context item lost its Invoke provider.");
            invoke.Invoke();
            await settle();
            Require(command.Executions == 1 && !nativeItem.IsLoaded,
                "Resizing broke native context-menu command invocation/dismissal.");
            context.ResizeMode = ContextMenuResizeMode.None;
            context.ShowAt(anchor);
            await settle();
            Require(!Descendants<Control>(presenter).Any(IsVisibleResizeHandle),
                "A reopened ContextMenu ignored ResizeMode.None.");
        }
        finally
        {
            context.Hide();
            anchor.ContextFlyout = null;
            host.Children.Remove(anchor);
            await settle();
        }
    }

    private static async Task VerifyBothResize(ResizeableContentControl resize, Func<Task> settle)
    {
        var provider = ResizeProvider(ResizeHandle(resize, both: true));
        Require(provider.CanResize, "The visible both-direction handle has no working Transform provider.");
        Require(
            await Task.Run(() => provider.CanResize).WaitAsync(TimeSpan.FromSeconds(6)),
            "The resize provider could not marshal a property query from a worker thread.");
        var width = Math.Min(resize.MaxWidth, resize.ActualWidth + 36);
        var height = Math.Min(resize.MaxHeight, resize.ActualHeight + 28);
        var scale = resize.XamlRoot.RasterizationScale;
        provider.Resize(0, 0);
        await settle();
        Require(resize.ActualWidth >= resize.MinWidth && resize.ActualHeight >= resize.MinHeight
                && ResizeHandle(resize, both: true).ActualHeight > 0,
            "Minimum resizing violated constraints or made the actual drag handle disappear.");
        await Task.Run(() => provider.Resize(width * scale, height * scale))
            .WaitAsync(TimeSpan.FromSeconds(6));
        await settle();
        Require(resize.IsLoaded, "The popup closed instead of accepting both-direction resizing.");
        Near(resize.ActualWidth, width, "The resize handle did not change rendered popup width.");
        Near(resize.ActualHeight, height, "The resize handle did not change rendered popup height.");
        provider.Resize(100000 * scale, 100000 * scale);
        await settle();
        Require(resize.ActualWidth <= resize.MaxWidth + 1 && resize.ActualHeight <= resize.MaxHeight + 1,
            "The real popup exceeded its maximum resize/viewport constraints.");
        provider.Resize(width * scale, height * scale);
        await settle();
    }

    private static async Task VerifyVerticalResize(ResizeableContentControl resize, Func<Task> settle)
    {
        var handle = ResizeHandle(resize, both: false);
        var provider = ResizeProvider(handle);
        var width = resize.ActualWidth;
        var height = Math.Min(resize.MaxHeight, resize.ActualHeight + 20);
        var scale = resize.XamlRoot.RasterizationScale;
        provider.Resize((width + 70) * scale, height * scale);
        await settle();
        Near(resize.ActualWidth, width, "Vertical mode resized the popup horizontally.");
        Near(resize.ActualHeight, height, "Vertical mode did not resize the actual popup.");
        Require(handle.Focus(FocusState.Keyboard)
                && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(handle.XamlRoot), handle),
            "The live popup resize handle cannot receive keyboard focus.");
        Require(!Invoke<bool>(handle, "TryHandleKey", VirtualKey.Right, false),
            "A vertical-only resize handle consumed horizontal keyboard resizing.");
        Require(Invoke<bool>(handle, "TryHandleKey", VirtualKey.Up, false),
            "The actual resize handle's shared keyboard handler is not connected.");
        await settle();
        Require(resize.IsLoaded, "The popup closed instead of accepting keyboard resizing.");
        Near(resize.ActualHeight, Math.Max(resize.MinHeight, height - 10),
            "Keyboard resize did not change rendered popup height.");
    }

    private static DropDownPresentation Presentation(Fluent.DropDownButton owner)
    {
#if WINDOWS
        var accessor = typeof(RibbonDropDownButton).GetProperty(
            "OpenNativePopup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The real native popup accessor is missing.");
        return accessor.GetValue(owner) as Popup
               ?? throw new InvalidOperationException("The active source-owned native Popup is missing.");
#else
        return Field<Flyout>(owner, "_flyout");
#endif
    }

    private static object? PresentationContent(DropDownPresentation presentation) =>
#if WINDOWS
        presentation.Child;
#else
        presentation.Content;
#endif

    private static ResizeableContentControl RequireDropDownContent(DropDownPresentation flyout)
    {
        var content = PresentationContent(flyout) as ResizeableContentControl
                      ?? throw new InvalidOperationException("The actual flyout has no resizable content surface.");
        Require(content.IsLoaded && content.ActualWidth > 0 && content.ActualHeight > 0,
            "The actual flyout content is not mounted and measured. "
            + $"NativeOpen={flyout.IsOpen}, Loaded={content.IsLoaded}, Size={content.ActualWidth}x{content.ActualHeight}.");
        return content;
    }

    private static FrameworkElement RequirePopupContent(Popup? popup)
    {
        Require(popup?.IsOpen == true, "The real native Popup is not open.");
        var child = popup!.Child as FrameworkElement
                    ?? throw new InvalidOperationException("The real popup has no FrameworkElement content.");
        Require(child.IsLoaded && child.ActualWidth > 0 && child.ActualHeight > 0,
            "The real popup child is not mounted and measured.");
        return child;
    }

    private static void AssertWithinViewport(FrameworkElement child)
    {
        var bounds = child.TransformToVisual(null).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
        var viewport = child.XamlRoot.Size;
        Require(bounds.Left >= -1 && bounds.Top >= -1
                && bounds.Right <= viewport.Width + 1 && bounds.Bottom <= viewport.Height + 1,
            $"The actual submenu escaped its viewport: {bounds}, viewport={viewport}.");
    }

    private static void AssertSubmenuPlacement(MenuItem owner)
    {
        var child = RequirePopupContent(owner.DropDownPopup);
        var bounds = child.TransformToVisual(null).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
        var anchor = owner.TransformToVisual(null).TransformBounds(new Rect(0, 0, owner.ActualWidth, owner.ActualHeight));
        var viewport = owner.XamlRoot.Size;
        var rightToLeft = owner.FlowDirection == FlowDirection.RightToLeft;
        var x = rightToLeft ? anchor.Left - bounds.Width : anchor.Right;
        if (x < 0 || x + bounds.Width > viewport.Width)
        {
            x = rightToLeft ? anchor.Right : anchor.Left - bounds.Width;
        }

        Near(bounds.Left, Math.Clamp(x, 0, Math.Max(0, viewport.Width - bounds.Width)),
            "The real submenu did not honor its anchor, flow direction, and available side. "
            + $"Anchor={anchor}, Child={bounds}, Viewport={viewport}, Flow={owner.FlowDirection}, "
            + $"PopupOffset={owner.DropDownPopup!.HorizontalOffset},{owner.DropDownPopup.VerticalOffset}, "
            + $"PopupTransform={owner.DropDownPopup.TransformToVisual(null).TransformBounds(new Rect(0, 0, bounds.Width, bounds.Height))}");
        Near(bounds.Top, Math.Clamp(anchor.Top, 0, Math.Max(0, viewport.Height - bounds.Height)),
            "The real submenu did not honor its vertical anchor/viewport bounds.");
    }

    private static void AssertDistinctTargets(FrameworkElement primary, FrameworkElement arrow)
    {
        var first = primary.TransformToVisual(null).TransformBounds(new Rect(0, 0, primary.ActualWidth, primary.ActualHeight));
        var second = arrow.TransformToVisual(null).TransformBounds(new Rect(0, 0, arrow.ActualWidth, arrow.ActualHeight));
        Require(first.Width > 0 && second.Width > 0
                && (first.Right <= second.Left + 1 || second.Right <= first.Left + 1),
            "The primary and submenu hit targets overlap or have no geometry.");
    }

    private static Control ResizeHandle(DependencyObject root, bool both) =>
        Descendants<Control>(root).SingleOrDefault(control =>
            control.Name == (both ? "PART_ResizeBothThumb" : "PART_ResizeVerticalThumb")
            && control.Visibility == Visibility.Visible)
        ?? throw new InvalidOperationException("The requested live popup resize handle is missing.");

    private static bool IsVisibleResizeHandle(Control control) =>
        control.Name is "PART_ResizeBothThumb" or "PART_ResizeVerticalThumb"
        && control.Visibility == Visibility.Visible;

    private static ITransformProvider ResizeProvider(Control handle)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(handle)
                   ?? throw new InvalidOperationException("The actual resize handle has no automation peer.");
        Require(peer.GetAutomationControlType() == AutomationControlType.Thumb
                && !string.IsNullOrWhiteSpace(peer.GetName()),
            "The popup resize handle has no meaningful localized name or thumb automation role.");
        return peer.GetPattern(PatternInterface.Transform) as ITransformProvider
               ?? throw new InvalidOperationException("The actual resize handle has no Transform provider.");
    }

    private static void InvokeButton(NativeButton button)
    {
        var provider = FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetPattern(PatternInterface.Invoke)
                       as IInvokeProvider
                       ?? throw new InvalidOperationException("The actual template button has no native Invoke provider.");
        provider.Invoke();
    }

    private static T Part<T>(DependencyObject root, string name) where T : FrameworkElement =>
        Descendants<T>(root).FirstOrDefault(part => part.Name == name)
        ?? throw new InvalidOperationException($"Missing live template part '{name}'.");

    private static T Find<T>(DependencyObject root) where T : DependencyObject =>
        Descendants<T>(root).FirstOrDefault()
        ?? throw new InvalidOperationException($"Missing live {typeof(T).Name}. "
            + string.Join(" > ", Descendants<FrameworkElement>(root).Take(24).Select(item => $"{item.GetType().Name}#{item.Name}")));

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private static T Ancestor<T>(DependencyObject item) where T : DependencyObject
    {
        for (var current = VisualTreeHelper.GetParent(item); current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is T match)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"Missing live {typeof(T).Name} ancestor.");
    }

    private static bool IsDescendant(DependencyObject item, DependencyObject root)
    {
        for (DependencyObject? current = item; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, root))
            {
                return true;
            }
        }

        return false;
    }

    private static T Field<T>(object target, string name)
    {
        for (var type = target.GetType(); type is not null; type = type.BaseType)
        {
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } field)
            {
                return (T)field.GetValue(target)!;
            }
        }

        throw new MissingFieldException(target.GetType().FullName, name);
    }

    private static T Invoke<T>(object target, string name, params object?[] arguments)
    {
        for (var type = target.GetType(); type is not null; type = type.BaseType)
        {
            var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .SingleOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            if (method is null)
            {
                continue;
            }

            try
            {
                return (T)method.Invoke(target, arguments)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        throw new MissingMethodException(target.GetType().FullName, name);
    }

    private static DataTemplate LabelTemplate(string name) =>
        (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" "
            + "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><TextBlock x:Name=\""
            + name + "\" Text=\"{Binding Title}\" /></DataTemplate>");

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

        throw new InvalidOperationException("A command-disabled menu automation action did not reject invocation.");
    }

    private static void Near(double actual, double expected, string message) =>
        Require(Math.Abs(actual - expected) <= 1.1, $"{message} Expected={expected}, actual={actual}.");

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record PopupLabel(string Title);

    private sealed class PopupHeaderModel : System.ComponentModel.INotifyPropertyChanged
    {
        private string header = string.Empty;
        private string accessKey = string.Empty;
        public string Header
        {
            get => header;
            set { header = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Header))); }
        }
        public string AccessKey
        {
            get => accessKey;
            set { accessKey = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(AccessKey))); }
        }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class PopupCommand : ICommand
    {
        private EventHandler? changed;
        public bool Allowed { get; set; } = true;
        public int Executions { get; private set; }
        public int Subscribers { get; private set; }
        public event EventHandler? CanExecuteChanged
        {
            add { changed += value; Subscribers++; }
            remove { changed -= value; Subscribers--; }
        }

        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) => Executions++;
        public void Notify() => changed?.Invoke(this, EventArgs.Empty);
    }
}
