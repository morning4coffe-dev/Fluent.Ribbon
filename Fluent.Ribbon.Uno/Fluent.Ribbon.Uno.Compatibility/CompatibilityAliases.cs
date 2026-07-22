namespace Fluent;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonCheckBox"/>.
/// </summary>
public partial class CheckBox : RibbonCheckBox, IQuickAccessItemProvider, IRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonComboBox"/>.
/// </summary>
public partial class ComboBox : RibbonComboBox, IQuickAccessItemProvider, IRibbonControl, IDropDownControl, IMediumIconProvider, ISimplifiedRibbonControl;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonGallery"/>.
/// </summary>
public partial class Gallery : RibbonGallery;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonGalleryItem"/>.
/// </summary>
public partial class GalleryItem : RibbonGalleryItem, IKeyTipedControl, ICommandSource;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonRadioButton"/>.
/// </summary>
public partial class RadioButton : RibbonRadioButton, IQuickAccessItemProvider, IRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonSpinner"/>.
/// </summary>
public partial class Spinner : RibbonSpinner, IQuickAccessItemProvider, IRibbonControl, IMediumIconProvider, ISimplifiedRibbonControl;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonSplitButton"/>.
/// </summary>
public partial class SplitButton : RibbonSplitButton, ICommandSource, IQuickAccessItemProvider, IRibbonControl, IDropDownControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl, IToggleButton, Extensibility.IKeyTipInformationProvider;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonStatusBar"/>.
/// </summary>
public partial class StatusBar : RibbonStatusBar;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonTextBox"/>.
/// </summary>
public partial class TextBox : RibbonTextBox, IQuickAccessItemProvider, IRibbonControl, IMediumIconProvider, ISimplifiedRibbonControl;

/// <summary>
/// Provides the WPF-compatible name for <see cref="RibbonToggleButton"/>.
/// </summary>
public partial class ToggleButton : RibbonToggleButton, IToggleButton, IQuickAccessItemProvider, IRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl;
