#nullable enable

namespace FluentUno.Windows.UITests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using NUnit.Framework;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class ShowcaseExternalUiaTests
{
    private static readonly TimeSpan ActionTimeout = TimeSpan.FromSeconds(15);

    [Test]
    [Category("WindowsExternalUIA")]
    public void WinUiShowcaseExposesAndOperatesTheRibbonThroughWindowsUia()
    {
        using var showcase = ShowcaseSession.Start();

        try
        {
            Assert.That(showcase.Window.Current.ProcessId, Is.EqualTo(showcase.Process.Id));

            var ribbon = showcase.WaitForElementById("MainRibbon");
            Assert.Multiple(() =>
            {
                Assert.That(ribbon.Current.ControlType, Is.EqualTo(ControlType.Group));
                Assert.That(ribbon.Current.ClassName, Is.EqualTo("Ribbon"));
                Assert.That(ribbon.Current.Name, Is.EqualTo("Fluent Ribbon Showcase"));
                Assert.That(ribbon.Current.IsControlElement, Is.True);
            });

            var toolbarsTab = showcase.WaitForElementById("TabToolbars");
            var toolbarsSelection = GetPattern<SelectionItemPattern>(
                toolbarsTab,
                SelectionItemPattern.Pattern,
                "Toolbars tab SelectionItem");
            Assert.That(toolbarsSelection.Current.IsSelected, Is.True);
            Assert.That(IsRawDescendantOf(ribbon, toolbarsTab), Is.True, "Ribbon tree did not contain its visible tab.");
            Assert.That(
                showcase.FindWindowElementById("TglToggle2"),
                Is.Null,
                "A descendant of the hidden Tests tab leaked into the process UIA tree.");

            ExerciseDropDownSplitFocusAndDisabledProviders(showcase);
            ExerciseSpinner(showcase);
            ExerciseRibbonButtonWithObservableResult(showcase);
            ExerciseToggleSplitAndTextValue(showcase);
            ExerciseGallerySelection(showcase);
            ExerciseSearchValue(showcase);
        }
        catch
        {
            TestContext.Progress.WriteLine(showcase.DumpControlTree());
            throw;
        }
    }

    private static void ExerciseDropDownSplitFocusAndDisabledProviders(ShowcaseSession showcase)
    {
        TestContext.Progress.WriteLine("UIA step: opening RibbonDropDownButton");
        using var dropDownLease = showcase.GetVisibleElement(
            "GrpClipboard",
            "DdbDropDownButton");
        var dropDown = dropDownLease.Element;
        var expand = GetPattern<ExpandCollapsePattern>(
            dropDown,
            ExpandCollapsePattern.Pattern,
            "RibbonDropDownButton ExpandCollapse");

        Assert.That(
            showcase.FindProcessElementById("MniConfidential"),
            Is.Null,
            "A closed drop-down exposed popup descendants.");

        using var expandedEvent = new PropertyEventProbe(
            dropDown,
            ExpandCollapsePattern.ExpandCollapseStateProperty,
            _ => true);
        expand.Expand();
        Assert.That(
            expandedEvent.Wait(ActionTimeout),
            Is.True,
            "No UIA expand property event was raised.");
        WaitUntil(
            () => expand.Current.ExpandCollapseState == ExpandCollapseState.Expanded,
            "RibbonDropDownButton did not expand.");
        Assert.That(
            expandedEvent.HasDuplicateWithin(TimeSpan.FromMilliseconds(500)),
            Is.False,
            "The drop-down raised duplicate UIA expand property events.");

        var popupItem = showcase.WaitForElementById("MniConfidential");
        Assert.That(popupItem.Current.ControlType, Is.EqualTo(ControlType.MenuItem));

        TestContext.Progress.WriteLine("UIA step: focusing popup item");
        using var popupFocus = new FocusEventProbe(
            element => IsSameOrRawDescendant(popupItem, element));
        popupItem.SetFocus();
        TestContext.Progress.WriteLine("UIA step: popup item focus returned");
        Assert.That(
            popupFocus.Wait(ActionTimeout),
            Is.True,
            "UIA focus did not enter the open popup.");

        // Windows UIA has no keyboard-injection pattern, and CI cannot reliably grant the
        // testhost foreground input. Exercise deterministic dismissal through ExpandCollapse.
        TestContext.Progress.WriteLine("UIA step: collapsing RibbonDropDownButton");
        expand.Collapse();
        TestContext.Progress.WriteLine("UIA step: collapse returned");
        WaitUntil(
            () => expand.Current.ExpandCollapseState == ExpandCollapseState.Collapsed,
            "RibbonDropDownButton did not collapse.");
        WaitUntil(
            () => showcase.FindProcessElementById("MniConfidential") is null,
            "Closed drop-down descendants remained in the process UIA tree.");

        AssertImplementationPartsAreRaw(dropDown, ControlType.Button);

        var split = showcase.WaitForElementById("SpbPaste");
        var splitInvoke = GetPattern<InvokePattern>(
            split,
            InvokePattern.Pattern,
            "RibbonSplitButton Invoke");
        var splitExpand = GetPattern<ExpandCollapsePattern>(
            split,
            ExpandCollapsePattern.Pattern,
            "RibbonSplitButton ExpandCollapse");
        splitInvoke.Invoke();
        splitExpand.Expand();
        WaitUntil(
            () => splitExpand.Current.ExpandCollapseState == ExpandCollapseState.Expanded,
            "RibbonSplitButton did not expand.");
        Assert.That(showcase.WaitForElementById("MniKeepSourceFormatting"), Is.Not.Null);
        splitExpand.Collapse();
        WaitUntil(
            () => splitExpand.Current.ExpandCollapseState == ExpandCollapseState.Collapsed,
            "RibbonSplitButton did not collapse.");
        WaitUntil(
            () => showcase.FindProcessElementById("MniKeepSourceFormatting") is null,
            "Closed split-button descendants remained in the process UIA tree.");
        AssertImplementationPartsAreRaw(split, ControlType.Button);

        var disabledButton = showcase.WaitForElementById("BtnCut");
        Assert.That(disabledButton.Current.IsEnabled, Is.False);
        var disabledInvoke = GetPattern<InvokePattern>(
            disabledButton,
            InvokePattern.Pattern,
            "disabled RibbonButton Invoke");
        Assert.That(
            () => disabledInvoke.Invoke(),
            Throws.TypeOf<ElementNotEnabledException>(),
            "A disabled provider did not reject Invoke.");
    }

    private static void ExerciseSpinner(ShowcaseSession showcase)
    {
        using var spinnerLease = showcase.GetVisibleElement("GrpSpinners", "SpnRight");
        var spinner = spinnerLease.Element;
        var range = GetPattern<RangeValuePattern>(
            spinner,
            RangeValuePattern.Pattern,
            "RibbonSpinner RangeValue");
        Assert.Multiple(() =>
        {
            Assert.That(spinner.Current.ControlType, Is.EqualTo(ControlType.Spinner));
            Assert.That(range.Current.Minimum, Is.EqualTo(0));
            Assert.That(range.Current.Maximum, Is.EqualTo(1000));
            Assert.That(range.Current.IsReadOnly, Is.False);
        });

        range.SetValue(7);
        WaitUntil(() => range.Current.Value == 7, "RibbonSpinner RangeValue did not change.");
        AssertImplementationPartsAreRaw(spinner, ControlType.Edit, ControlType.Button);

        var disabledSpinner = showcase.WaitForElementById("SpnLeft");
        Assert.That(disabledSpinner.Current.IsEnabled, Is.False);
        var disabledRange = GetPattern<RangeValuePattern>(
            disabledSpinner,
            RangeValuePattern.Pattern,
            "disabled RibbonSpinner RangeValue");
        Assert.That(
            () => disabledRange.SetValue(2),
            Throws.TypeOf<ElementNotEnabledException>(),
            "A disabled RangeValue provider accepted SetValue.");
    }

    private static void ExerciseRibbonButtonWithObservableResult(ShowcaseSession showcase)
    {
        SelectTab(showcase, "TabInsert");
        using var comboLease = showcase.GetVisibleElement("GrpInsertFonts", "CmbInsertFonts");
        var combo = comboLease.Element;
        const string addedItemName = "Added item 9";
        var hasItemContainer = combo.TryGetCurrentPattern(
            ItemContainerPattern.Pattern,
            out var itemContainerProvider);
        var items = itemContainerProvider as ItemContainerPattern;
        int[]? existingItemRuntimeId = null;
        if (hasItemContainer)
        {
            var existingItem = items!.FindItemByProperty(
                null,
                AutomationElement.NameProperty,
                "Arial");
            Assert.That(existingItem, Is.Not.Null, "The existing ComboBox item was missing.");
            existingItemRuntimeId = existingItem!.GetRuntimeId();
            Assert.That(
                items.FindItemByProperty(
                    null,
                    AutomationElement.NameProperty,
                    addedItemName),
                Is.Null,
                "The observable button result was already present.");
        }

        var button = showcase.WaitForElementById("BtnAddItemToFonts");
        Assert.That(button.Current.ClassName, Is.EqualTo("RibbonButton"));
        var invoke = GetPattern<InvokePattern>(
            button,
            InvokePattern.Pattern,
            "RibbonButton Invoke");
        invoke.Invoke();
        if (hasItemContainer)
        {
            WaitUntil(
                () => items!.FindItemByProperty(
                    null,
                    AutomationElement.NameProperty,
                    addedItemName) is not null,
                "RibbonButton Invoke did not update the OS-visible ComboBox items.");
            var existingItemAfterMutation = items!.FindItemByProperty(
                null,
                AutomationElement.NameProperty,
                "Arial");
            Assert.That(
                existingItemAfterMutation?.GetRuntimeId(),
                Is.EqualTo(existingItemRuntimeId),
                "An unchanged ComboBox item lost its UIA provider identity after insertion.");
            return;
        }

        // Skia's native bridge does not expose ItemContainer. Invoke itself is still exercised;
        // opening its ComboBox solely to inspect the option tree can invalidate later providers.
    }

    private static void ExerciseToggleSplitAndTextValue(ShowcaseSession showcase)
    {
        SelectTab(showcase, "TabTests");
        var previousTab = showcase.WaitForElementById("TabToolbars");
        Assert.That(
            GetPattern<SelectionItemPattern>(
                previousTab,
                SelectionItemPattern.Pattern,
                "previous tab SelectionItem").Current.IsSelected,
            Is.False,
            "Selecting Tests did not deselect Toolbars.");
        Assert.That(
            showcase.FindWindowElementById("SpnRight"),
            Is.Null,
            "A descendant of the hidden Toolbars tab leaked into the process UIA tree.");

        using (var toggleLease = showcase.GetVisibleElement("GrpGroupedToggle", "TglToggle2"))
        {
            var toggle = toggleLease.Element;
            var togglePattern = GetPattern<TogglePattern>(
                toggle,
                TogglePattern.Pattern,
                "RibbonToggleButton Toggle");
            Assert.That(togglePattern.Current.ToggleState, Is.EqualTo(ToggleState.Off));
            togglePattern.Toggle();
            WaitUntil(
                () => togglePattern.Current.ToggleState == ToggleState.On,
                "RibbonToggleButton did not toggle.");
        }

        using (var splitLease = showcase.GetVisibleElement("GrpGroupedSplit", "SpbSplit1"))
        {
            var split = splitLease.Element;
            var invoke = GetPattern<InvokePattern>(
                split,
                InvokePattern.Pattern,
                "RibbonSplitButton Invoke");
            var expand = GetPattern<ExpandCollapsePattern>(
                split,
                ExpandCollapsePattern.Pattern,
                "RibbonSplitButton ExpandCollapse");
            invoke.Invoke();
            expand.Expand();
            WaitUntil(
                () => expand.Current.ExpandCollapseState == ExpandCollapseState.Expanded,
                "Tests split button did not expand.");
            Assert.That(showcase.WaitForElementById("MniFirst"), Is.Not.Null);
            expand.Collapse();
            WaitUntil(
                () => showcase.FindProcessElementById("MniFirst") is null,
                "Tests split-button popup remained in the UIA tree after collapse.");
        }

        using var textLease = showcase.GetVisibleElement("GrpSharedSize", "TxtMyShortHeader");
        var textBox = textLease.Element;
        var value = GetPattern<ValuePattern>(
            textBox,
            ValuePattern.Pattern,
            "RibbonTextBox Value");
        const string expectedText = "External UIA text";
        value.SetValue(expectedText);
        WaitUntil(
            () => string.Equals(value.Current.Value, expectedText, StringComparison.Ordinal),
            "RibbonTextBox Value did not change.");

        using var focusEvent = new FocusEventProbe(
            element => IsSameOrRawDescendant(textBox, element));
        textBox.SetFocus();
        Assert.That(focusEvent.Wait(ActionTimeout), Is.True, "RibbonTextBox UIA focus was not delegated.");
        AssertImplementationPartsAreRaw(textBox, ControlType.Edit);
    }

    private static void ExerciseGallerySelection(ShowcaseSession showcase)
    {
        SelectTab(showcase, "TabGalleries");
        using var galleryLease = showcase.GetVisibleElement("GrpGalleries", "GalInRibbon");
        var gallery = galleryLease.Element;
        var selection = GetPattern<SelectionPattern>(
            gallery,
            SelectionPattern.Pattern,
            "InRibbonGallery Selection");
        Assert.Multiple(() =>
        {
            Assert.That(gallery.Current.ControlType, Is.EqualTo(ControlType.List));
            Assert.That(selection.Current.CanSelectMultiple, Is.False);
        });

        var item = WaitFor(
            () => gallery.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "2")),
            "Gallery item '2' was not exposed.");
        Assert.That(item.Current.ControlType, Is.EqualTo(ControlType.ListItem));
        var selectionItem = GetPattern<SelectionItemPattern>(
            item,
            SelectionItemPattern.Pattern,
            "gallery item SelectionItem");
        selectionItem.Select();
        WaitUntil(() => selectionItem.Current.IsSelected, "Gallery item was not selected.");
    }

    private static void ExerciseSearchValue(ShowcaseSession showcase)
    {
        SelectTab(showcase, "ModernTab");
        using var searchLease = showcase.GetVisibleElement(
            "ModernSearchGroup",
            "ModernRibbonSearchBox");
        var search = searchLease.Element;
        var value = GetPattern<ValuePattern>(
            search,
            ValuePattern.Pattern,
            "RibbonSearchBox Value");
        Assert.Multiple(() =>
        {
            Assert.That(search.Current.ControlType, Is.EqualTo(ControlType.Edit));
            Assert.That(search.Current.Name, Is.EqualTo("Search ribbon commands"));
            Assert.That(value.Current.IsReadOnly, Is.False);
        });

        const string query = "save";
        value.SetValue(query);
        WaitUntil(
            () => string.Equals(value.Current.Value, query, StringComparison.Ordinal),
            "RibbonSearchBox Value did not change.");
        AssertImplementationPartsAreRaw(search, ControlType.Edit);
        value.SetValue(string.Empty);
    }

    private static void SelectTab(ShowcaseSession showcase, string automationId)
    {
        var tab = showcase.WaitForElementById(automationId);
        var selection = GetPattern<SelectionItemPattern>(
            tab,
            SelectionItemPattern.Pattern,
            $"{automationId} SelectionItem");
        if (selection.Current.IsSelected)
        {
            return;
        }

        selection.Select();
        WaitUntil(() => selection.Current.IsSelected, $"{automationId} was not selected.");
    }

    private static TPattern GetPattern<TPattern>(
        AutomationElement element,
        AutomationPattern pattern,
        string description)
        where TPattern : class
    {
        Assert.That(
            element.TryGetCurrentPattern(pattern, out var provider),
            Is.True,
            $"{description} was not available on {Describe(element)}.");
        Assert.That(provider, Is.InstanceOf<TPattern>(), description);
        return (TPattern)provider;
    }

    private static void AssertImplementationPartsAreRaw(
        AutomationElement owner,
        params ControlType[] implementationTypes)
    {
        foreach (var controlType in implementationTypes)
        {
            var typeCondition = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                controlType);
            var controlViewCondition = new AndCondition(
                Automation.ControlViewCondition,
                typeCondition);
            Assert.That(
                owner.FindAll(TreeScope.Descendants, controlViewCondition).Count,
                Is.Zero,
                $"{Describe(owner)} exposed a duplicate {controlType.ProgrammaticName} in Control View.");

            // CompositeOwnershipTests separately proves that the template implementation parts
            // exist and are marked Raw. WinUI may prune those parts from its provider fragment
            // entirely; when it retains them, they must remain outside Control and Content views.
            var rawParts = owner.FindAll(TreeScope.Descendants, typeCondition);
            foreach (AutomationElement rawPart in rawParts)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(
                        rawPart.Current.IsControlElement,
                        Is.False,
                        $"{Describe(owner)} implementation part {Describe(rawPart)} was not raw.");
                    Assert.That(
                        rawPart.Current.IsContentElement,
                        Is.False,
                        $"{Describe(owner)} implementation part {Describe(rawPart)} was content.");
                });
            }
        }
    }

    private static bool IsSameOrRawDescendant(
        AutomationElement ancestor,
        AutomationElement? candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        for (var current = candidate; current is not null;)
        {
            if (AreSameElement(ancestor, current))
            {
                return true;
            }

            try
            {
                current = TreeWalker.RawViewWalker.GetParent(current);
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool IsRawDescendantOf(
        AutomationElement ancestor,
        AutomationElement candidate)
        => !AreSameElement(ancestor, candidate)
           && IsSameOrRawDescendant(ancestor, candidate);

    private static bool AreSameElement(AutomationElement left, AutomationElement right)
    {
        try
        {
            return left.GetRuntimeId().SequenceEqual(right.GetRuntimeId());
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static T WaitFor<T>(Func<T?> probe, string failureMessage)
        where T : class
    {
        var deadline = Stopwatch.StartNew();
        using var retry = new ManualResetEventSlim();
        Exception? lastException = null;
        while (deadline.Elapsed < ActionTimeout)
        {
            try
            {
                if (probe() is { } result)
                {
                    return result;
                }
            }
            catch (Exception exception) when (
                exception is ElementNotAvailableException
                or InvalidOperationException
                or COMException)
            {
                lastException = exception;
            }

            retry.Wait(TimeSpan.FromMilliseconds(50));
        }

        Assert.Fail(
            lastException is null
                ? failureMessage
                : $"{failureMessage}{Environment.NewLine}{lastException}");
        throw new AssertionException(failureMessage);
    }

    private static void WaitUntil(Func<bool> condition, string failureMessage)
        => WaitFor(
            () => condition() ? new object() : null,
            failureMessage);

    private static string Describe(AutomationElement element)
    {
        try
        {
            return $"'{element.Current.Name}' ({element.Current.AutomationId}, {element.Current.ClassName})";
        }
        catch (ElementNotAvailableException)
        {
            return "<unavailable element>";
        }
    }

    private sealed class ShowcaseSession : IDisposable
    {
        private ShowcaseSession(Process process, AutomationElement window)
        {
            Process = process;
            Window = window;
        }

        internal Process Process { get; }

        internal AutomationElement Window { get; }

        internal static ShowcaseSession Start()
        {
            var application = ResolveShowcaseApplication();
            var windowOpened = new ManualResetEventSlim();
            var processExited = new ManualResetEventSlim();
            var processId = 0;
            AutomationEventHandler windowHandler = (sender, _) =>
            {
                if (sender is not AutomationElement element)
                {
                    return;
                }

                try
                {
                    if (processId != 0 && element.Current.ProcessId == processId)
                    {
                        windowOpened.Set();
                    }
                }
                catch (ElementNotAvailableException)
                {
                }
            };

            Automation.AddAutomationEventHandler(
                WindowPattern.WindowOpenedEvent,
                AutomationElement.RootElement,
                TreeScope.Children,
                windowHandler);

            Process? process = null;
            try
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = application.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                            ? "dotnet"
                            : application,
                        Arguments = application.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                            ? $"\"{application}\""
                            : string.Empty,
                        WorkingDirectory = Path.GetDirectoryName(application)!,
                        UseShellExecute = false,
                    },
                    EnableRaisingEvents = true,
                };
                process.StartInfo.Environment["SHOWCASE_WIDTH"] = "1600";
                process.StartInfo.Environment["SHOWCASE_HEIGHT"] = "900";
                process.Exited += (_, _) => processExited.Set();

                Assert.That(process.Start(), Is.True, $"Could not start '{application}'.");
                processId = process.Id;

                var deadline = Stopwatch.StartNew();
                while (deadline.Elapsed < TimeSpan.FromSeconds(45))
                {
                    if (FindTopLevelWindow(processId) is { } window)
                    {
                        return new ShowcaseSession(process, window);
                    }

                    var remaining = TimeSpan.FromSeconds(45) - deadline.Elapsed;
                    var wait = remaining < TimeSpan.FromMilliseconds(250)
                        ? remaining
                        : TimeSpan.FromMilliseconds(250);
                    var signaled = WaitHandle.WaitAny(
                        [windowOpened.WaitHandle, processExited.WaitHandle],
                        wait);
                    if (signaled == 1 || process.HasExited)
                    {
                        Assert.Fail(
                            $"Showcase exited before exposing a top-level UIA window. Exit code: {process.ExitCode}.");
                    }

                    windowOpened.Reset();
                }

                Assert.Fail(
                    $"Showcase process {processId} did not expose a top-level UIA window within 45 seconds.");
                throw new AssertionException("Showcase UIA window was not found.");
            }
            catch
            {
                if (process is { HasExited: false })
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(10000);
                }

                process?.Dispose();
                throw;
            }
            finally
            {
                Automation.RemoveAutomationEventHandler(
                    WindowPattern.WindowOpenedEvent,
                    AutomationElement.RootElement,
                    windowHandler);
                windowOpened.Dispose();
                processExited.Dispose();
            }
        }

        internal AutomationElement? FindElementById(string automationId)
            => FindWindowElementById(automationId)
               ?? FindProcessElementById(automationId);

        internal AutomationElement? FindWindowElementById(string automationId)
        {
            var condition = new AndCondition(
                Automation.ControlViewCondition,
                new PropertyCondition(
                    AutomationElement.AutomationIdProperty,
                    automationId));
            return Window.FindFirst(TreeScope.Descendants, condition);
        }

        internal AutomationElement? FindProcessElementById(string automationId)
        {
            var condition = new AndCondition(
                Automation.ControlViewCondition,
                new PropertyCondition(
                    AutomationElement.ProcessIdProperty,
                    Process.Id),
                new PropertyCondition(
                    AutomationElement.AutomationIdProperty,
                    automationId));
            return AutomationElement.RootElement.FindFirst(
                TreeScope.Descendants,
                condition);
        }

        internal AutomationElement WaitForElementById(string automationId)
            => WaitFor(
                () => FindElementById(automationId),
                $"UIA element '{automationId}' was not found in Showcase process {Process.Id}.");

        internal AutomationElement? FindElementByName(string name)
        {
            var condition = new AndCondition(
                Automation.ControlViewCondition,
                new PropertyCondition(
                    AutomationElement.ProcessIdProperty,
                    Process.Id),
                new PropertyCondition(
                    AutomationElement.NameProperty,
                    name));
            return AutomationElement.RootElement.FindFirst(
                TreeScope.Descendants,
                condition);
        }

        internal VisibleElementLease GetVisibleElement(
            string groupAutomationId,
            string elementAutomationId)
        {
            if (FindElementById(elementAutomationId) is { } visible)
            {
                return new VisibleElementLease(visible, null);
            }

            var group = WaitForElementById(groupAutomationId);
            var expand = GetPattern<ExpandCollapsePattern>(
                group,
                ExpandCollapsePattern.Pattern,
                $"{groupAutomationId} ExpandCollapse");
            expand.Expand();
            WaitUntil(
                () => expand.Current.ExpandCollapseState == ExpandCollapseState.Expanded,
                $"{groupAutomationId} did not expand.");
            return new VisibleElementLease(
                WaitForElementById(elementAutomationId),
                expand);
        }

        internal string DumpControlTree()
        {
            var lines = new List<string>
            {
                $"Showcase PID={Process.Id}, exited={Process.HasExited}",
            };
            Dump(Window, 0, lines);
            return string.Join(Environment.NewLine, lines);

            static void Dump(
                AutomationElement element,
                int depth,
                ICollection<string> lines)
            {
                if (depth > 8 || lines.Count >= 250)
                {
                    return;
                }

                try
                {
                    lines.Add(
                        $"{new string(' ', depth * 2)}"
                        + $"{element.Current.ControlType.ProgrammaticName} "
                        + $"Name='{element.Current.Name}' "
                        + $"Id='{element.Current.AutomationId}' "
                        + $"Class='{element.Current.ClassName}' "
                        + $"Enabled={element.Current.IsEnabled}");
                    for (var child = TreeWalker.ControlViewWalker.GetFirstChild(element);
                         child is not null;
                         child = TreeWalker.ControlViewWalker.GetNextSibling(child))
                    {
                        Dump(child, depth + 1, lines);
                    }
                }
                catch (ElementNotAvailableException)
                {
                    lines.Add($"{new string(' ', depth * 2)}<unavailable>");
                }
                catch (COMException exception)
                {
                    lines.Add(
                        $"{new string(' ', depth * 2)}<provider error 0x{exception.HResult:X8}>");
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (!Process.HasExited)
                {
                    Process.Kill(entireProcessTree: true);
                    if (!Process.WaitForExit(10000))
                    {
                        throw new TimeoutException(
                            $"Showcase process {Process.Id} did not terminate.");
                    }
                }
            }
            finally
            {
                Process.Dispose();
            }
        }

        private static AutomationElement? FindTopLevelWindow(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return AutomationElement.FromHandle(process.MainWindowHandle);
                }
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            var processCondition = new PropertyCondition(
                AutomationElement.ProcessIdProperty,
                processId);
            var candidates = AutomationElement.RootElement.FindAll(
                TreeScope.Children,
                processCondition);
            foreach (AutomationElement candidate in candidates)
            {
                try
                {
                    if (candidate.Current.ControlType == ControlType.Window
                        && candidate.Current.NativeWindowHandle != 0)
                    {
                        return candidate;
                    }
                }
                catch (ElementNotAvailableException)
                {
                }
            }

            return null;
        }

        private static string ResolveShowcaseApplication()
        {
            var explicitPath =
                Environment.GetEnvironmentVariable("SHOWCASE_UIA_APP")
                ?? Environment.GetEnvironmentVariable("SHOWCASE_WINUI_EXE");
            var path = string.IsNullOrWhiteSpace(explicitPath)
                ? Path.Combine(
                    FindRepositoryRoot(TestContext.CurrentContext.TestDirectory),
                    "Fluent.Ribbon.Uno",
                    "Fluent.Ribbon.Uno.Showcase",
                    "Fluent.Ribbon.Uno.Showcase",
                    "bin",
                    "Release",
                    "net10.0-windows10.0.26100",
                    "Fluent.Ribbon.Uno.Showcase.exe")
                : Path.GetFullPath(explicitPath);
            Assert.That(
                File.Exists(path),
                Is.True,
                $"The Showcase application was not found at '{path}'. "
                + "Build it with ShowcaseExternalUia=true or set SHOWCASE_UIA_APP.");
            return path;
        }

        private static string FindRepositoryRoot(string startingDirectory)
        {
            for (var directory = new DirectoryInfo(startingDirectory);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluent.Ribbon.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException(
                $"Could not locate Fluent.Ribbon.sln above '{startingDirectory}'.");
        }
    }

    private sealed class VisibleElementLease(
        AutomationElement element,
        ExpandCollapsePattern? openedGroup) : IDisposable
    {
        internal AutomationElement Element { get; } = element;

        public void Dispose()
        {
            if (openedGroup is null)
            {
                return;
            }

            try
            {
                openedGroup.Collapse();
            }
            catch (ElementNotAvailableException)
            {
            }
        }
    }

    private sealed class PropertyEventProbe : IDisposable
    {
        private readonly AutomationElement _element;
        private readonly AutomationPropertyChangedEventHandler _handler;
        private readonly ManualResetEventSlim _matched = new();
        private readonly ManualResetEventSlim _duplicate = new();
        private readonly Predicate<object?> _matches;
        private int _matchCount;

        internal PropertyEventProbe(
            AutomationElement element,
            AutomationProperty property,
            Predicate<object?> matches)
        {
            _element = element;
            _matches = matches;
            _handler = OnPropertyChanged;
            Automation.AddAutomationPropertyChangedEventHandler(
                element,
                TreeScope.Element,
                _handler,
                property);
        }

        internal bool Wait(TimeSpan timeout) => _matched.Wait(timeout);

        internal bool HasDuplicateWithin(TimeSpan timeout) => _duplicate.Wait(timeout);

        public void Dispose()
        {
            Automation.RemoveAutomationPropertyChangedEventHandler(
                _element,
                _handler);
            _matched.Dispose();
            _duplicate.Dispose();
        }

        private void OnPropertyChanged(
            object sender,
            AutomationPropertyChangedEventArgs args)
        {
            try
            {
                if (_matches(args.NewValue))
                {
                    if (Interlocked.Increment(ref _matchCount) == 1)
                    {
                        _matched.Set();
                    }
                    else
                    {
                        _duplicate.Set();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private sealed class FocusEventProbe : IDisposable
    {
        private readonly AutomationFocusChangedEventHandler _handler;
        private readonly ManualResetEventSlim _matched = new();
        private readonly Predicate<AutomationElement> _matches;

        internal FocusEventProbe(Predicate<AutomationElement> matches)
        {
            _matches = matches;
            _handler = OnFocusChanged;
            Automation.AddAutomationFocusChangedEventHandler(_handler);
        }

        internal bool Wait(TimeSpan timeout) => _matched.Wait(timeout);

        public void Dispose()
        {
            Automation.RemoveAutomationFocusChangedEventHandler(_handler);
            _matched.Dispose();
        }

        private void OnFocusChanged(object sender, AutomationFocusChangedEventArgs args)
        {
            try
            {
                if (sender is AutomationElement element && _matches(element))
                {
                    _matched.Set();
                }
            }
            catch (ElementNotAvailableException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

}
