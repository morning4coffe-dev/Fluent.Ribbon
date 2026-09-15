#if WINDOWS
namespace Fluent;

public partial class RibbonGallery
{
    private object? nativeContainerItem;
    private bool hasNativeContainerItem;
    private ItemsPresenter? nativeItemsPresenter;
    private UniformItemsPanel? nativeItemsPanel;
    private Canvas? nativeGroupHeaders;
    private ControlTemplate? nativeElementContainerTemplate;
    private bool creatingCanonicalGalleryContainer;
    private bool clearingCanonicalGalleryContainer;

    private void InitializeNativeGallery()
    {
        itemsBinding.UsesNativeGenerator = true;
        foreach (var property in new[]
                 {
                     ItemWidthProperty, ItemHeightProperty, MinItemsInRowProperty,
                     MaxItemsInRowProperty, OrientationProperty,
                 })
        {
            RegisterPropertyChangedCallback(property, (_, _) => SyncNativeGallery());
        }
    }

    /// <summary>
    /// Returns the canonical container requested by the Windows item generator, or null
    /// when an override should create a new data container for the gallery's items view.
    /// </summary>
    protected DependencyObject? GetNativeGalleryContainer()
    {
        if (!hasNativeContainerItem || creatingCanonicalGalleryContainer
            || itemsBinding is not { UsesNativeGenerator: true })
        {
            return null;
        }

        var item = nativeContainerItem;
        nativeContainerItem = null;
        hasNativeContainerItem = false;
        if (item is UIElement)
        {
            // Plain authored visuals keep their public identity/content, but a native ListBox
            // still needs its normal SelectorItem shell. This is not another items owner.
            nativeElementContainerTemplate ??= (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                 TargetType="ListBoxItem">
                    <ContentPresenter Content="{TemplateBinding Content}"
                                      HorizontalContentAlignment="Stretch"
                                      VerticalContentAlignment="Stretch" />
                </ControlTemplate>
                """);
            return new ListBoxItem
            {
                Template = nativeElementContainerTemplate,
                IsTabStop = false,
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
        }
        return itemsBinding.AcquireNativeContainer(item);
    }

    private bool IsCanonicalGalleryContainer(object item)
        => RunCanonicalGalleryFactory(() => IsItemItsOwnContainerOverride(item));

    private DependencyObject CreateCanonicalGalleryContainer()
        => RunCanonicalGalleryFactory(GetContainerForItemOverride);

    private T RunCanonicalGalleryFactory<T>(Func<T> factory)
    {
        // Native callbacks can run synchronously inside the adapter's own Items edits.
        // IsUpdating therefore cannot distinguish the view factory from the native generator.
        var wasCreating = creatingCanonicalGalleryContainer;
        creatingCanonicalGalleryContainer = true;
        try
        {
            return factory();
        }
        finally
        {
            creatingCanonicalGalleryContainer = wasCreating;
        }
    }

    private void ClearCanonicalGalleryContainer(DependencyObject container, object item)
    {
        var wasClearing = clearingCanonicalGalleryContainer;
        clearingCanonicalGalleryContainer = true;
        try
        {
            ClearContainerForItemOverride(container, item);
        }
        finally
        {
            clearingCanonicalGalleryContainer = wasClearing;
        }
    }

    private void ApplyNativeGalleryTemplate()
    {
        nativeItemsPresenter = GetTemplateChild("ItemsPresenter") as ItemsPresenter;
        nativeGroupHeaders = GetTemplateChild("PART_GroupHeaders") as Canvas;
        if (nativeItemsPresenter is not null)
        {
            nativeItemsPresenter.Loaded += OnNativeGalleryPresenterLoaded;
            nativeItemsPresenter.LayoutUpdated += OnNativeGalleryPresenterLayoutUpdated;
        }
    }

    private void DetachNativeGalleryTemplate()
    {
        if (nativeItemsPresenter is not null)
        {
            nativeItemsPresenter.Loaded -= OnNativeGalleryPresenterLoaded;
            nativeItemsPresenter.LayoutUpdated -= OnNativeGalleryPresenterLayoutUpdated;
        }

        nativeItemsPanel?.ConfigureNativeGallery(null, null);
        nativeItemsPanel = null;
        nativeItemsPresenter = null;
        nativeGroupHeaders = null;
    }

    private void OnNativeGalleryPresenterLoaded(object sender, RoutedEventArgs args) => SyncNativeGallery();

    private void OnNativeGalleryPresenterLayoutUpdated(object? sender, object args)
    {
        if (!ReferenceEquals(FindNativeItemsPanel(nativeItemsPresenter), nativeItemsPanel))
        {
            SyncNativeGallery();
        }
    }

    private void SyncNativeGallery()
    {
        if (itemsBinding is null || itemsBinding.IsUpdating || itemsBinding.IsSuspended)
        {
            return;
        }

        // Unload leaves the source template intact. Reconnect observation without moving any items.
        if (nativeItemsPresenter is null)
        {
            ApplyNativeGalleryTemplate();
        }

        ApplyFilter();
        var panel = FindNativeItemsPanel(nativeItemsPresenter);
        if (!ReferenceEquals(panel, nativeItemsPanel))
        {
            nativeItemsPanel?.ConfigureNativeGallery(null, null);
            nativeItemsPanel = panel;
        }

        if (nativeItemsPanel is not null)
        {
            nativeItemsPanel.ItemWidth = ItemWidth;
            nativeItemsPanel.ItemHeight = ItemHeight;
            nativeItemsPanel.MinColumns = MinItemsInRow;
            nativeItemsPanel.MaxColumns = MaxItemsInRow;
            nativeItemsPanel.Orientation = Orientation;
            nativeItemsPanel.ConfigureNativeGallery(this, nativeGroupHeaders);
        }
    }

    private static UniformItemsPanel? FindNativeItemsPanel(DependencyObject? root)
    {
        if (root is null)
        {
            return null;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is Panel panel)
            {
                return panel as UniformItemsPanel;
            }

            if (FindNativeItemsPanel(child) is { } result)
            {
                return result;
            }
        }

        return null;
    }
}
#endif
