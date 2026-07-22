using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fluent;
using Fluent.Modern;
using Fluent.Modern.Automation;
using Fluent.Modern.Commands;
using Fluent.Modern.Controls;
using Fluent.Modern.Helpers;
using Fluent.Modern.Media;
using Fluent.Modern.Model;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using RibbonInputModeHelper = Fluent.Modern.Helpers.RibbonInputMode;
using RibbonInputDensity = Fluent.Modern.RibbonInputMode;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage
{
    private async Task RunModernAutoTestAsync()
    {
        AutoLog("MODERN-GUARD BEGIN");
        await Task.Yield();

        var assembly = typeof(ModernExtensionAttribute).Assembly;
        var failures = assembly.GetTypes()
            .Where(type => type.IsPublic
                           && type.Namespace is not null
                           && (type.Namespace.Equals("Fluent.Modern", StringComparison.Ordinal)
                               || type.Namespace.StartsWith("Fluent.Modern.", StringComparison.Ordinal))
                           && type.GetCustomAttribute<ModernExtensionAttribute>(false) is null)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (failures.Length == 0)
        {
            AutoLog("MODERN-GUARD PASS");
        }
        else
        {
            AutoLog($"MODERN-GUARD FAIL: {string.Join(", ", failures)}");
        }

        AutoLog("MODERN-ICONS BEGIN");
        Popup? popup = null;
        try
        {
            var host = new StackPanel { Spacing = 8, Width = 240, Height = 180 };
            host.Children.Add(new ModernIconPresenter
            {
                Source = new FontIconSource { Glyph = "\uE734" },
                IconSize = IconSize.Large,
            });
            host.Children.Add(new ModernRibbonButton
            {
                Header = "Path",
                Size = RibbonControlSize.Large,
                LargeIconSource = new PathIconSource { Data = CreateModernStarGeometry() },
                SmallIconSource = new PathIconSource { Data = CreateModernStarGeometry() },
            });
            host.Children.Add(new ModernRibbonButton
            {
                Header = "SVG",
                Size = RibbonControlSize.Large,
                LargeIconSource = new ImageIconSource
                {
                    ImageSource = new SvgImageSource(new Uri("ms-appx:///Fluent.Ribbon.Uno.Showcase/Assets/Modern/star.svg")),
                },
                SmallIconSource = new ImageIconSource
                {
                    ImageSource = new SvgImageSource(new Uri("ms-appx:///Fluent.Ribbon.Uno.Showcase/Assets/Modern/star.svg")),
                },
            });

            popup = new Popup { Child = host };
            if (Content is Panel rootPanel)
            {
                rootPanel.Children.Add(popup);
            }

            popup.IsOpen = true;
            await SettleAsync(4, 150);
            AutoLog("MODERN-ICONS PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-ICONS THREW: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                if (popup is not null)
                {
                    popup.IsOpen = false;
                    if (Content is Panel rootPanel)
                    {
                        rootPanel.Children.Remove(popup);
                    }
                }
            }
            catch
            {
                // ignore teardown errors
            }
        }
        AutoLog("MODERN-SEARCH BEGIN");
        try
        {
            using var catalog = new RibbonCommandCatalog(MainRibbon);
            if (catalog.Commands.Count == 0)
            {
                AutoLog("MODERN-SEARCH FAIL: empty catalog");
                return;
            }

            var previousTab = MainRibbon.SelectedTab;
            var descriptor = catalog.Commands.FirstOrDefault(command => command.OwningTab != previousTab) ?? catalog.Commands[0];
            var result = catalog.Search(descriptor.DisplayName, 8).FirstOrDefault();
            if (!ReferenceEquals(result, descriptor))
            {
                AutoLog($"MODERN-SEARCH FAIL: search did not return '{descriptor.DisplayName}' first");
                return;
            }

            descriptor.Navigate();
            if (MainRibbon.SelectedTab != descriptor.OwningTab)
            {
                AutoLog($"MODERN-SEARCH FAIL: selected tab did not switch to '{descriptor.TabHeader}'");
                return;
            }

            descriptor.Invoke();
            AutoLog("MODERN-SEARCH PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-SEARCH THREW: {ex.GetType().Name}: {ex.Message}");
        }

        RunModernCatalogMutationAutoTest();

        AutoLog("MODERN-ACCEL BEGIN");
        try
        {
            var invoked = false;
            var button = new RibbonButton
            {
                Header = "Save",
                ScreenTipTitle = "Save",
                ScreenTipText = "Saves the current document.",
                Command = new ModernShowcaseCommand(() =>
                {
                    invoked = true;
                }),
            };

            RibbonAccelerator.SetGesture(button, "Ctrl+S");

            if (button.KeyboardAccelerators.Count != 1)
            {
                AutoLog($"MODERN-ACCEL FAIL: expected one accelerator, found {button.KeyboardAccelerators.Count}");
                return;
            }

            var accelerator = button.KeyboardAccelerators[0];
            if (accelerator.Key != VirtualKey.S || accelerator.Modifiers != VirtualKeyModifiers.Control)
            {
                AutoLog($"MODERN-ACCEL FAIL: expected Ctrl+S, found {accelerator.Modifiers}+{accelerator.Key}");
                return;
            }

            if (RibbonAccelerator.GetAcceleratorText(button) != "Ctrl+S")
            {
                AutoLog($"MODERN-ACCEL FAIL: text was '{RibbonAccelerator.GetAcceleratorText(button)}'");
                return;
            }

            var toolTip = ToolTipService.GetToolTip(button);
            var toolTipText = toolTip is ScreenTip screenTip
                ? screenTip.Text?.ToString()
                : toolTip?.ToString();
            if (toolTipText?.Contains("Ctrl+S", StringComparison.Ordinal) != true)
            {
                AutoLog("MODERN-ACCEL FAIL: ScreenTip/ToolTip did not contain Ctrl+S");
                return;
            }

            RibbonInvoker.Invoke(button);
            if (!invoked)
            {
                AutoLog("MODERN-ACCEL FAIL: RibbonInvoker did not execute command");
                return;
            }

            RibbonAccelerator.SetGesture(button, "Ctrl+Shift+P");
            if (button.KeyboardAccelerators.Count != 1)
            {
                AutoLog($"MODERN-ACCEL FAIL: reset stacked accelerators ({button.KeyboardAccelerators.Count})");
                return;
            }

            accelerator = button.KeyboardAccelerators[0];
            if (accelerator.Key != VirtualKey.P || accelerator.Modifiers != (VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift))
            {
                AutoLog($"MODERN-ACCEL FAIL: expected Ctrl+Shift+P, found {accelerator.Modifiers}+{accelerator.Key}");
                return;
            }

            AutoLog("MODERN-ACCEL PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-ACCEL THREW: {ex.GetType().Name}: {ex.Message}");
        }

        RunModernAdaptiveAutoTest();
        RunModernBuilderAutoTest();
        await RunModernCustomizationAutoTestAsync();
        RunModernVisualsAutoTest();
        RunModernOnboardingAutoTest();
        RunModernRtlAccentAutoTest();
        RunModernA11yAutoTest();
    }

    private void RunModernCatalogMutationAutoTest()
    {
        AutoLog("MODERN-SEARCH-DYNAMIC BEGIN");

        RibbonCommandCatalog? catalog = null;
        try
        {
            var ribbon = new Ribbon();
            var firstTab = new RibbonTab { Header = "First" };
            var firstGroup = new RibbonGroupBox { Header = "First group" };
            firstGroup.Items.Add(new RibbonButton { Header = "Initial command" });
            firstTab.Groups.Add(firstGroup);
            ribbon.Tabs.Add(firstTab);

            catalog = new RibbonCommandCatalog(ribbon);

            var addedToItems = new RibbonButton { Header = "Dynamic item command" };
            firstGroup.Items.Add(addedToItems);
            if (catalog.Search("Dynamic item command", 1).Count != 1)
            {
                AutoLog("MODERN-SEARCH-DYNAMIC FAIL: group item mutation was not indexed");
                return;
            }

            var addedGroup = new RibbonGroupBox { Header = "Dynamic group" };
            addedGroup.Items.Add(new RibbonButton { Header = "Dynamic group command" });
            firstTab.Groups.Add(addedGroup);
            if (catalog.Search("Dynamic group command", 1).Count != 1)
            {
                AutoLog("MODERN-SEARCH-DYNAMIC FAIL: tab group mutation was not indexed");
                return;
            }

            var addedTab = new RibbonTab { Header = "Dynamic tab" };
            var addedTabGroup = new RibbonGroupBox { Header = "Dynamic tab group" };
            addedTabGroup.Items.Add(new RibbonButton { Header = "Dynamic tab command" });
            addedTab.Groups.Add(addedTabGroup);
            ribbon.Tabs.Add(addedTab);
            if (catalog.Search("Dynamic tab command", 1).Count != 1)
            {
                AutoLog("MODERN-SEARCH-DYNAMIC FAIL: ribbon tab mutation was not indexed");
                return;
            }

            var dropDown = new RibbonDropDownButton { Header = "Dynamic menu" };
            firstGroup.Items.Add(dropDown);
            var nested = new RibbonMenuItem { Header = "Dynamic nested command" };
            dropDown.Items.Add(nested);
            if (catalog.Search("Dynamic nested command", 1).Count != 1)
            {
                AutoLog("MODERN-SEARCH-DYNAMIC FAIL: nested item mutation was not indexed");
                return;
            }

            dropDown.Items.Remove(nested);
            if (catalog.Search("Dynamic nested command", 1).Count != 0)
            {
                AutoLog("MODERN-SEARCH-DYNAMIC FAIL: removed nested item remained indexed");
                return;
            }

            var countBeforeDispose = catalog.Commands.Count;
            catalog.Dispose();
            firstGroup.Items.Add(new RibbonButton { Header = "After dispose" });
            if (catalog.Commands.Count != countBeforeDispose)
            {
                AutoLog("MODERN-SEARCH-DYNAMIC FAIL: disposed catalog still rebuilt");
                return;
            }

            AutoLog("MODERN-SEARCH-DYNAMIC PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-SEARCH-DYNAMIC THREW: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            catalog?.Dispose();
        }
    }

    private void RunModernA11yAutoTest()
    {
        AutoLog("MODERN-A11Y BEGIN");

        try
        {
            var panel = new StackPanel();
            RibbonFocus.SetEnableXYFocus(panel, true);
            if (panel.XYFocusKeyboardNavigation != XYFocusKeyboardNavigationMode.Enabled)
            {
                AutoLog($"MODERN-A11Y FAIL: expected XYFocus Enabled, found {panel.XYFocusKeyboardNavigation}");
                return;
            }

            RibbonFocus.SetEnableXYFocus(panel, false);
            if (panel.XYFocusKeyboardNavigation != XYFocusKeyboardNavigationMode.Auto)
            {
                AutoLog($"MODERN-A11Y FAIL: expected XYFocus Auto, found {panel.XYFocusKeyboardNavigation}");
                return;
            }

            var searchBox = new RibbonSearchBox
            {
                PlaceholderText = "Find commands",
                Text = "Initial value",
            };
            AutomationProperties.SetName(searchBox, "Command search");
            var searchPeer = FrameworkElementAutomationPeer.CreatePeerForElement(searchBox);
            if (searchPeer is not RibbonSearchBoxAutomationPeer
                || searchPeer.GetClassName() != nameof(RibbonSearchBox)
                || searchPeer.GetName() != "Command search"
                || searchPeer.GetAutomationControlType() != AutomationControlType.Edit
                || searchPeer.GetPattern(PatternInterface.Value) is not IValueProvider valueProvider
                || valueProvider.IsReadOnly
                || valueProvider.Value != "Initial value")
            {
                AutoLog($"MODERN-A11Y FAIL: search peer metadata/value pattern was invalid ({searchPeer?.GetType().Name ?? "null"} / {searchPeer?.GetAutomationControlType()} / '{searchPeer?.GetName()}')");
                return;
            }

            valueProvider.SetValue("Updated by automation");
            if (searchBox.Text != "Updated by automation" || valueProvider.Value != "Updated by automation")
            {
                AutoLog("MODERN-A11Y FAIL: Value provider did not update search text");
                return;
            }

            searchBox.IsEnabled = false;
            if (!valueProvider.IsReadOnly)
            {
                AutoLog("MODERN-A11Y FAIL: disabled search box was not exposed as read-only");
                return;
            }

            var button = new ModernRibbonButton { Header = "A11y button" };
            var buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
            if (buttonPeer is not ModernRibbonButtonAutomationPeer
                || buttonPeer.GetClassName() != nameof(ModernRibbonButton)
                || string.IsNullOrWhiteSpace(buttonPeer.GetName()))
            {
                AutoLog($"MODERN-A11Y FAIL: button peer was {buttonPeer?.GetType().Name ?? "null"} / {buttonPeer?.GetClassName()} / '{buttonPeer?.GetName()}'");
                return;
            }

            var infoBarHost = new RibbonInfoBarHost { Title = "A11y notification" };
            var infoBarPeer = FrameworkElementAutomationPeer.CreatePeerForElement(infoBarHost);
            if (infoBarPeer is not RibbonInfoBarHostAutomationPeer
                || infoBarPeer.GetClassName() != nameof(RibbonInfoBarHost)
                || string.IsNullOrWhiteSpace(infoBarPeer.GetName()))
            {
                AutoLog($"MODERN-A11Y FAIL: info peer was {infoBarPeer?.GetType().Name ?? "null"} / {infoBarPeer?.GetClassName()} / '{infoBarPeer?.GetName()}'");
                return;
            }

            AutoLog("MODERN-A11Y PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-A11Y THREW: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void RunModernBuilderAutoTest()
    {
        AutoLog("MODERN-BUILDER BEGIN");
        try
        {
            var invoked = false;
            var model = new RibbonModel();
            var tabModel = new RibbonTabModel { Header = "Generated" };
            var groupModel = new RibbonGroupModel { Header = "Primary" };
            var commandButtonModel = new RibbonButtonModel
            {
                Header = "Generate",
                Size = RibbonControlSize.Medium,
                Gesture = "Ctrl+G",
                IconSource = new FontIconSource { Glyph = "\uE8A5" },
                Command = new ModernShowcaseCommand(() => invoked = true),
            };
            groupModel.Items.Add(commandButtonModel);
            groupModel.Items.Add(new RibbonToggleButtonModel
            {
                Header = "Toggle",
                Size = RibbonControlSize.Small,
                IsChecked = true,
            });
            groupModel.Items.Add(new RibbonSeparatorModel());

            var secondaryGroupModel = new RibbonGroupModel { Header = "Secondary" };
            secondaryGroupModel.Items.Add(new RibbonButtonModel
            {
                Header = "Plain",
                Size = RibbonControlSize.Large,
                IconGlyph = "\uE10F",
            });
            secondaryGroupModel.Items.Add(new RibbonButtonModel
            {
                Header = "More",
                Size = RibbonControlSize.Medium,
            });

            tabModel.Groups.Add(groupModel);
            tabModel.Groups.Add(secondaryGroupModel);
            model.Tabs.Add(tabModel);

            var ribbon = Fluent.Modern.RibbonBuilder.Build(model);
            if (ribbon.Tabs.Count != 1)
            {
                AutoLog($"MODERN-BUILDER FAIL: expected 1 tab, found {ribbon.Tabs.Count}");
                return;
            }

            var builtTab = ribbon.Tabs[0];
            if (builtTab.Groups.Count != 2)
            {
                AutoLog($"MODERN-BUILDER FAIL: expected 2 groups, found {builtTab.Groups.Count}");
                return;
            }

            var builtGroup = builtTab.Groups[0];
            if (builtGroup.Items.Count != groupModel.Items.Count)
            {
                AutoLog($"MODERN-BUILDER FAIL: expected {groupModel.Items.Count} first-group items, found {builtGroup.Items.Count}");
                return;
            }

            if (builtGroup.Items[0] is not ModernRibbonButton button)
            {
                AutoLog($"MODERN-BUILDER FAIL: IconSource button type was {builtGroup.Items[0].GetType().Name}");
                return;
            }

            if (!string.Equals(button.Header?.ToString(), commandButtonModel.Header, StringComparison.Ordinal)
                || button.Size != commandButtonModel.Size)
            {
                AutoLog($"MODERN-BUILDER FAIL: button header/size mismatch ({button.Header}, {button.Size})");
                return;
            }

            if (button.KeyboardAccelerators.Count != 1
                || button.KeyboardAccelerators[0].Key != VirtualKey.G
                || button.KeyboardAccelerators[0].Modifiers != VirtualKeyModifiers.Control)
            {
                AutoLog("MODERN-BUILDER FAIL: Ctrl+G accelerator was not created");
                return;
            }

            RibbonInvoker.Invoke(button);
            if (!invoked)
            {
                AutoLog("MODERN-BUILDER FAIL: RibbonInvoker did not execute built command");
                return;
            }

            AutoLog("MODERN-BUILDER PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-BUILDER THREW: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void RunModernAdaptiveAutoTest()
    {
        AutoLog("MODERN-ADAPTIVE BEGIN");
        try
        {
            RibbonAdaptiveBehavior.Evaluate(MainRibbon, 400);
            if (!MainRibbon.IsMinimized)
            {
                AutoLog("MODERN-ADAPTIVE FAIL: width 400 did not minimize");
                return;
            }

            RibbonAdaptiveBehavior.Evaluate(MainRibbon, 700);
            if (!MainRibbon.IsSimplified || MainRibbon.IsMinimized)
            {
                AutoLog("MODERN-ADAPTIVE FAIL: width 700 did not simplify without minimizing");
                return;
            }

            RibbonAdaptiveBehavior.Evaluate(MainRibbon, 1200);
            if (MainRibbon.IsSimplified || MainRibbon.IsMinimized)
            {
                AutoLog("MODERN-ADAPTIVE FAIL: width 1200 did not restore full ribbon state");
                return;
            }

            MainRibbon.IsSimplified = false;
            MainRibbon.IsMinimized = false;

            var host = new Border();
            RibbonInputModeHelper.SetInputMode(host, RibbonInputDensity.Touch);
            if (!RibbonInputModeHelper.IsTouchDensityApplied(host))
            {
                AutoLog("MODERN-ADAPTIVE FAIL: touch density dictionary was not applied");
                return;
            }

            if (!TryFindTouchResource(host, RibbonInputModeHelper.TouchTargetMinHeightKey, out var touchTarget)
                || touchTarget is not double touchTargetMinHeight
                || touchTargetMinHeight < 44)
            {
                AutoLog("MODERN-ADAPTIVE FAIL: touch density resource key did not resolve");
                return;
            }

            RibbonInputModeHelper.SetInputMode(host, RibbonInputDensity.Mouse);
            if (RibbonInputModeHelper.IsTouchDensityApplied(host))
            {
                AutoLog("MODERN-ADAPTIVE FAIL: touch density dictionary was not removed");
                return;
            }

            AutoLog("MODERN-ADAPTIVE PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-ADAPTIVE THREW: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            MainRibbon.IsSimplified = false;
            MainRibbon.IsMinimized = false;
        }
    }

    private async Task RunModernCustomizationAutoTestAsync()
    {
        AutoLog("MODERN-CUSTOMIZE BEGIN");
        RibbonLayout? originalLayout = null;

        try
        {
            var originalCapture = RibbonCustomizationService.CaptureResult(MainRibbon);
            originalLayout = originalCapture.Value;
            if (!originalCapture.Succeeded || originalLayout is null || originalLayout.Tabs.Count == 0)
            {
                AutoLog($"MODERN-CUSTOMIZE FAIL: capture failed ({string.Join(", ", originalCapture.Issues.Select(issue => issue.Code))})");
                return;
            }

            var structuralTab = new RibbonTab { Header = "Localized header" };
            structuralTab.Groups.Add(new RibbonGroupBox
            {
                Header = "Localized group",
                Items =
                {
                    new RibbonButton { Header = "Localized command", IconGlyph = "\uE8A5" },
                },
            });
            structuralTab.Groups.Add(new RibbonGroupBox
            {
                Header = "Second group",
                Items =
                {
                    new RibbonButton { Header = "Second command", IconGlyph = "\uE74E" },
                },
            });
            var firstStableKey = RibbonCustomizationService.ResolveStableKey(structuralTab);
            structuralTab.Header = "En-tête localisé";
            structuralTab.Groups[0].Header = "Groupe localisé";
            structuralTab.Groups[0].Items.OfType<RibbonButton>().Single().Header = "Commande localisée";
            var reorderedGroup = structuralTab.Groups[0];
            structuralTab.Groups.RemoveAt(0);
            structuralTab.Groups.Add(reorderedGroup);
            var localizedStableKey = RibbonCustomizationService.ResolveStableKey(structuralTab);
            if (!firstStableKey.Succeeded
                || !localizedStableKey.Succeeded
                || firstStableKey.Value != localizedStableKey.Value)
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: derived structural key changed with localized headers");
                return;
            }

            var duplicateRibbon = new Ribbon();
            var duplicateTabA = new RibbonTab();
            var duplicateTabB = new RibbonTab();
            RibbonCustomizationService.SetItemKey(duplicateTabA, "duplicate");
            RibbonCustomizationService.SetItemKey(duplicateTabB, "duplicate");
            duplicateRibbon.Tabs.Add(duplicateTabA);
            duplicateRibbon.Tabs.Add(duplicateTabB);
            var duplicateCapture = RibbonCustomizationService.CaptureResult(duplicateRibbon);
            if (duplicateCapture.Succeeded
                || duplicateCapture.Issues.All(issue => issue.Code != "DuplicateKey" || !issue.IsError))
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: duplicate live keys were not rejected");
                return;
            }

            var serialized = RibbonCustomizationService.SerializeResult(originalLayout);
            var json = serialized.Value;
            if (!serialized.Succeeded || string.IsNullOrWhiteSpace(json))
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: observable serialization failed");
                return;
            }

            var deserialized = RibbonCustomizationService.DeserializeResult(json);
            var round = deserialized.Value;
            if (!deserialized.Succeeded
                || round is null
                || round.Tabs.Count != originalLayout.Tabs.Count
                || !round.Tabs.Select(tab => tab.Key).SequenceEqual(originalLayout.Tabs.Select(tab => tab.Key)))
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: layout did not round-trip");
                return;
            }

            if (MainRibbon.Tabs.Count < 2)
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: need at least two tabs to verify reordering");
                return;
            }

            var invalidJson = RibbonCustomizationService.DeserializeResult("{invalid");
            if (invalidJson.Succeeded || invalidJson.Issues.All(issue => issue.Code != "DeserializationFailed"))
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: invalid JSON failure was not observable");
                return;
            }

            var modifiedResult = RibbonCustomizationService.DeserializeResult(json);
            var modified = modifiedResult.Value;
            if (!modifiedResult.Succeeded || modified is null)
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: could not clone layout");
                return;
            }

            var moved = modified.Tabs.OrderBy(tab => tab.Order).First();
            moved.IsVisible = false;
            moved.Order = modified.Tabs.Count + 10;

            var applied = RibbonCustomizationService.ApplyResult(MainRibbon, modified);
            if (!applied.Succeeded)
            {
                AutoLog($"MODERN-CUSTOMIZE FAIL: apply failed ({string.Join(", ", applied.Issues.Select(issue => issue.Code))})");
                return;
            }

            var movedRibbonTab = MainRibbon.Tabs.FirstOrDefault(tab => string.Equals(GetModernCustomizationKey(tab), moved.Key, StringComparison.Ordinal));
            if (movedRibbonTab is null)
            {
                AutoLog($"MODERN-CUSTOMIZE FAIL: moved tab '{moved.Key}' was not found");
                return;
            }

            if (movedRibbonTab.Visibility != Visibility.Collapsed)
            {
                AutoLog($"MODERN-CUSTOMIZE FAIL: tab '{moved.Key}' was not hidden");
                return;
            }

            if (MainRibbon.Tabs.IndexOf(movedRibbonTab) != MainRibbon.Tabs.Count - 1)
            {
                AutoLog($"MODERN-CUSTOMIZE FAIL: tab '{moved.Key}' was not moved to the end");
                return;
            }

            var duplicateLayout = RibbonCustomizationService.DeserializeResult(json).Value;
            if (duplicateLayout is null)
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: could not create duplicate layout");
                return;
            }

            duplicateLayout.Tabs.Add(new RibbonTabLayout
            {
                Key = duplicateLayout.Tabs[0].Key,
                IsVisible = true,
                Order = duplicateLayout.Tabs.Count,
            });
            var duplicateApply = RibbonCustomizationService.ApplyResult(MainRibbon, duplicateLayout);
            if (duplicateApply.Succeeded
                || duplicateApply.Issues.All(issue => issue.Code != "DuplicateKey" || !issue.IsError))
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: duplicate persisted keys were not rejected");
                return;
            }

            var restored = RibbonCustomizationService.ApplyResult(MainRibbon, originalLayout);
            if (!restored.Succeeded)
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: original layout restore reported an error");
                return;
            }

            if (!MainRibbon.Tabs.Select(GetModernCustomizationKey).SequenceEqual(originalLayout.Tabs.Select(tab => tab.Key))
                || MainRibbon.Tabs.Any(tab => originalLayout.Tabs.FirstOrDefault(item => item.Key == GetModernCustomizationKey(tab))?.IsVisible != (tab.Visibility == Visibility.Visible)))
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: original tab order/visibility was not restored");
                return;
            }

            var saved = await RibbonCustomizationService.SaveResultAsync(originalLayout, "modern-autotest-layout");
            var loadResult = await RibbonCustomizationService.LoadResultAsync("modern-autotest-layout");
            var loaded = loadResult.Value;
            if (!saved.Succeeded
                || !loadResult.Succeeded
                || loaded is null
                || loaded.Tabs.Count != originalLayout.Tabs.Count
                || !loaded.Tabs.Select(tab => tab.Key).SequenceEqual(originalLayout.Tabs.Select(tab => tab.Key)))
            {
                AutoLog($"MODERN-CUSTOMIZE FAIL: observable persistence failed (save={saved.Succeeded}, load={loadResult.Succeeded})");
                return;
            }

            var cleared = await RibbonCustomizationService.ClearResultAsync("modern-autotest-layout");
            if (!cleared.Succeeded)
            {
                AutoLog("MODERN-CUSTOMIZE FAIL: observable clear failed");
                return;
            }

            AutoLog("MODERN-CUSTOMIZE PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-CUSTOMIZE THREW: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (originalLayout is not null)
            {
                RibbonCustomizationService.Apply(MainRibbon, originalLayout);
            }
        }
    }

    private void RunModernVisualsAutoTest()
    {
        AutoLog("MODERN-VISUALS BEGIN");
        try
        {
            var window = GetShowcaseMainWindow();
            var backdropApplied = window is not null && RibbonBackdrop.TryApply(window, RibbonBackdropKind.Mica);
            AutoLog($"MODERN-VISUALS backdrop Mica applied={backdropApplied}");
            if (window is not null)
            {
                RibbonBackdrop.TryApply(window, RibbonBackdropKind.None);
            }

            var elevated = new Border();
            RibbonElevation.SetDepth(elevated, 16);
            if (elevated.Shadow is null && Math.Abs(elevated.Translation.Z - 16f) > 0.01f)
            {
                AutoLog("MODERN-VISUALS FAIL: elevation did not apply shadow or z translation");
                return;
            }

            RibbonElevation.SetDepth(elevated, 0);
            if (elevated.Shadow is not null || Math.Abs(elevated.Translation.Z) > 0.01f)
            {
                AutoLog("MODERN-VISUALS FAIL: elevation did not clear");
                return;
            }

            var animationHost = new StackPanel();
            RibbonAnimations.SetEnableImplicitTransitions(animationHost, true);
            RibbonAnimations.SetEnableImplicitTransitions(animationHost, false);

            var source = new Border { Width = 24, Height = 24 };
            var target = new Border { Width = 24, Height = 24 };
            _ = RibbonAnimations.PrepareConnected(source, "modern-visuals-connected");
            _ = RibbonAnimations.TryStartConnected(target, "modern-visuals-connected");

            AutoLog("MODERN-VISUALS PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-VISUALS THREW: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            var window = GetShowcaseMainWindow();
            if (window is not null)
            {
                RibbonBackdrop.TryApply(window, RibbonBackdropKind.None);
            }
        }
    }

    private void RunModernOnboardingAutoTest()
    {
        AutoLog("MODERN-ONBOARDING BEGIN");

        RibbonInfoBarHost? infoBarHost = null;
        TeachingTip? coachMark = null;
        Panel? rootPanel = null;

        try
        {
            rootPanel = Content as Panel;
            if (rootPanel is null)
            {
                AutoLog("MODERN-ONBOARDING FAIL: page content is not a Panel");
                return;
            }

            infoBarHost = new RibbonInfoBarHost();
            rootPanel.Children.Add(infoBarHost);
            infoBarHost.Show(InfoBarSeverity.Warning, "T", "M");

            if (!infoBarHost.IsOpen
                || infoBarHost.Title != "T"
                || infoBarHost.Message != "M"
                || infoBarHost.Severity != InfoBarSeverity.Warning)
            {
                AutoLog("MODERN-ONBOARDING FAIL: InfoBar host did not retain shown state");
                return;
            }

            infoBarHost.IsOpen = false;

            coachMark = RibbonCoachMark.Show(MainRibbon, "t", "m");
            if (coachMark is null)
            {
                AutoLog("MODERN-ONBOARDING FAIL: RibbonCoachMark.Show returned null");
                return;
            }

            RibbonCoachMark.Close(coachMark);
            coachMark = null;

            AutoLog("MODERN-ONBOARDING PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-ONBOARDING THREW: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                if (coachMark is not null)
                {
                    RibbonCoachMark.Close(coachMark);
                }

                if (infoBarHost is not null)
                {
                    infoBarHost.IsOpen = false;
                    rootPanel?.Children.Remove(infoBarHost);
                }
            }
            catch
            {
                // ignore teardown errors
            }
        }
    }

    private void RunModernRtlAccentAutoTest()
    {
        AutoLog("MODERN-RTL-ACCENT BEGIN");

        try
        {
            var container = new Grid();
            container.Children.Add(new ModernRibbonButton
            {
                Header = "RTL",
                Size = RibbonControlSize.Medium,
                SmallIconSource = new FontIconSource { Glyph = "\uE8AB" },
            });

            RibbonFlow.SetIsRightToLeft(container, true);
            if (container.FlowDirection != FlowDirection.RightToLeft)
            {
                AutoLog($"MODERN-RTL-ACCENT FAIL: expected RightToLeft, found {container.FlowDirection}");
                return;
            }

            RibbonFlow.SetIsRightToLeft(container, false);
            if (container.FlowDirection != FlowDirection.LeftToRight)
            {
                AutoLog($"MODERN-RTL-ACCENT FAIL: expected LeftToRight, found {container.FlowDirection}");
                return;
            }

            if (!TryResolveModernBrush("ModernRibbonAccentBrush", out var brush) || brush is null)
            {
                AutoLog("MODERN-RTL-ACCENT FAIL: ModernRibbonAccentBrush did not resolve to a Brush");
                return;
            }

            AutoLog("MODERN-RTL-ACCENT PASS");
        }
        catch (Exception ex)
        {
            AutoLog($"MODERN-RTL-ACCENT THREW: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool TryFindTouchResource(FrameworkElement element, string key, out object? value)
    {
        if (element.Resources.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var dictionary in element.Resources.MergedDictionaries)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryResolveModernBrush(string key, out Brush? brush)
    {
        brush = null;

        try
        {
            if (TryFindResource(Application.Current.Resources, key, out var value) && value is Brush appBrush)
            {
                brush = appBrush;
                return true;
            }

            var dictionary = new ResourceDictionary
            {
                Source = new Uri("ms-appx:///Fluent.Ribbon.Uno/Themes/Modern/ModernAccentBrushes.xaml"),
            };

            if (TryFindResource(dictionary, key, out value) && value is Brush dictionaryBrush)
            {
                brush = dictionaryBrush;
                return true;
            }
        }
        catch
        {
            // Modern auto-tests report failure instead of throwing for unsupported resource lookup paths.
        }

        return false;
    }

    private static bool TryFindResource(ResourceDictionary dictionary, string key, out object? value)
    {
        if (dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var mergedDictionary in dictionary.MergedDictionaries)
        {
            if (TryFindResource(mergedDictionary, key, out value))
            {
                return true;
            }
        }

        foreach (var themeValue in dictionary.ThemeDictionaries.Values)
        {
            if (themeValue is ResourceDictionary themeDictionary
                && TryFindResource(themeDictionary, key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string GetModernCustomizationKey(DependencyObject element)
    {
        return RibbonCustomizationService.ResolveStableKey(element).Value ?? string.Empty;
    }
}
