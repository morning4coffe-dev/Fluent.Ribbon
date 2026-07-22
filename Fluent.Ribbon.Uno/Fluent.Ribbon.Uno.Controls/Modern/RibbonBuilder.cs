namespace Fluent.Modern;

using System.Diagnostics;
using Fluent.Modern.Commands;
using Fluent.Modern.Controls;
using Fluent.Modern.Model;

/// <summary>
/// <para><b>Modern extension</b> — materializes lightweight ribbon models into Fluent.Ribbon controls.</para>
/// </summary>
[ModernExtension]
public static class RibbonBuilder
{
    #region Methods

    /// <summary>
    /// Builds a new ribbon from the supplied model.
    /// </summary>
    /// <param name="model">The ribbon model.</param>
    /// <returns>A populated ribbon.</returns>
    public static Fluent.Ribbon Build(RibbonModel model)
    {
        var ribbon = new Fluent.Ribbon();
        Populate(ribbon, model);
        return ribbon;
    }

    /// <summary>
    /// Populates an existing ribbon from the supplied model.
    /// </summary>
    /// <param name="ribbon">The ribbon to populate.</param>
    /// <param name="model">The ribbon model.</param>
    public static void Populate(Fluent.Ribbon ribbon, RibbonModel model)
    {
        if (ribbon is null)
        {
            return;
        }

        ribbon.Tabs.Clear();
        if (model is null)
        {
            return;
        }

        foreach (var tabModel in model.Tabs)
        {
            var tab = TryBuildTab(tabModel);
            if (tab is not null)
            {
                ribbon.Tabs.Add(tab);
            }
        }
    }

    private static Fluent.RibbonTab? TryBuildTab(RibbonTabModel? model)
    {
        if (model is null)
        {
            return null;
        }

        try
        {
            var tab = new Fluent.RibbonTab
            {
                Header = model.Header ?? string.Empty,
            };

            foreach (var groupModel in model.Groups)
            {
                var group = TryBuildGroup(groupModel);
                if (group is not null)
                {
                    tab.Groups.Add(group);
                }
            }

            return tab;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RibbonBuilder skipped tab '{model.Header}': {ex}");
            return null;
        }
    }

    private static Fluent.RibbonGroupBox? TryBuildGroup(RibbonGroupModel? model)
    {
        if (model is null)
        {
            return null;
        }

        try
        {
            var group = new Fluent.RibbonGroupBox
            {
                Header = model.Header ?? string.Empty,
            };

            foreach (var itemModel in model.Items)
            {
                var item = TryBuildItem(itemModel);
                if (item is not null)
                {
                    group.Items.Add(item);
                }
            }

            return group;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RibbonBuilder skipped group '{model.Header}': {ex}");
            return null;
        }
    }

    private static UIElement? TryBuildItem(RibbonItemModel? model)
    {
        if (model is null)
        {
            return null;
        }

        try
        {
            return model switch
            {
                RibbonButtonModel buttonModel => BuildButton(buttonModel),
                RibbonToggleButtonModel toggleModel => BuildToggleButton(toggleModel),
                RibbonSeparatorModel => new Fluent.RibbonSeparator(),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RibbonBuilder skipped item '{model.Header}': {ex}");
            return null;
        }
    }

    private static Fluent.RibbonButton BuildButton(RibbonButtonModel model)
    {
        var button = model.IconSource is not null
            ? new ModernRibbonButton
            {
                LargeIconSource = model.IconSource,
                SmallIconSource = model.IconSource,
            }
            : new Fluent.RibbonButton();

        ApplyCommon(button, model);
        return button;
    }

    private static Fluent.RibbonToggleButton BuildToggleButton(RibbonToggleButtonModel model)
    {
        var toggleButton = new Fluent.RibbonToggleButton
        {
            IsChecked = model.IsChecked,
        };

        ApplyCommon(toggleButton, model);
        return toggleButton;
    }

    private static void ApplyCommon(Fluent.RibbonButton button, RibbonItemModel model)
    {
        button.Header = model.Header ?? string.Empty;
        button.Size = model.Size;
        button.Command = model.Command;
        button.CommandParameter = model.CommandParameter;
        button.IconGlyph = model.IconGlyph ?? string.Empty;
        button.ScreenTipTitle = model.ScreenTipTitle ?? string.Empty;
        button.ScreenTipText = model.ScreenTipText ?? string.Empty;
        ApplyGesture(button, model.Gesture);
    }

    private static void ApplyCommon(Fluent.RibbonToggleButton button, RibbonItemModel model)
    {
        button.Header = model.Header ?? string.Empty;
        button.Size = model.Size;
        button.Command = model.Command;
        button.CommandParameter = model.CommandParameter;
        button.IconGlyph = model.IconGlyph ?? string.Empty;
        button.ScreenTipTitle = model.ScreenTipTitle ?? string.Empty;
        button.ScreenTipText = model.ScreenTipText ?? string.Empty;
        ApplyGesture(button, model.Gesture);
    }

    private static void ApplyGesture(DependencyObject element, string? gesture)
    {
        if (!string.IsNullOrWhiteSpace(gesture))
        {
            RibbonAccelerator.SetGesture(element, gesture);
        }
    }

    #endregion
}
