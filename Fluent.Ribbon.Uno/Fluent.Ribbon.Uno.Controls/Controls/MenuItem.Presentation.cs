namespace Fluent;

using System.Text;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;
using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

public partial class MenuItem
{
    private readonly EnabledStateConstraint menuEnabledConstraint;
    private WinUIButton? primaryButton;
    private WinUIButton? submenuButton;
    private ButtonPointerClickFallback? primaryClickFallback;
    private ButtonPointerClickFallback? submenuClickFallback;
    private ContentPresenter? menuHeaderPresenter;
    private TextBlock? accessKeyText;
    private bool showAccessKey;
    private bool updatingPresentation;
    private bool presentationSuspended;
    private bool primaryPointerOver;
    private bool submenuPointerOver;
    private bool primaryPressed;
    private bool submenuPressed;
    private Binding? generatedAccessKeyBinding;
    private string generatedAccessKey = string.Empty;
    private bool updatingGeneratedAccessKey;

    private void InitializeMenuPresentation()
    {
        foreach (var property in new[]
                 {
                     IsSplitProperty, IsCheckableProperty, IsCheckedProperty, GroupNameProperty,
                     HasSubItemsProperty, HeaderProperty, HeaderTemplateProperty,
                     HeaderTemplateSelectorProperty, RecognizesAccessKeyProperty, FlowDirectionProperty,
                 })
        {
            RegisterPropertyChangedCallback(property, (_, _) => UpdateMenuPresentation());
        }

        RegisterPropertyChangedCallback(GroupNameProperty, (_, _) => ReconcileCheckedGroup());
        RegisterPropertyChangedCallback(IsCheckableProperty, (_, _) => ReconcileCheckedGroup());
        RegisterPropertyChangedCallback(AccessKeyProperty, (_, _) =>
        {
            if (!updatingGeneratedAccessKey
                && !string.Equals((string)GetValue(AccessKeyProperty), generatedAccessKey, StringComparison.Ordinal))
            {
                generatedAccessKeyBinding = null;
            }
        });
        IsEnabledChanged += (_, _) => UpdateMenuPresentation();
        AccessKeyInvoked += OnMenuAccessKeyInvoked;
        AccessKeyDisplayRequested += (_, _) =>
        {
            showAccessKey = true;
            UpdateAccessKeyPresentation();
        };
        AccessKeyDisplayDismissed += (_, _) =>
        {
            showAccessKey = false;
            UpdateAccessKeyPresentation();
        };
        Loaded += (_, _) =>
        {
            presentationSuspended = false;
            UpdateMenuPresentation();
            ReconcileCheckedGroup();
        };
        Unloaded += (_, _) =>
        {
#if WINDOWS
            if (IsLoaded)
            {
                return;
            }
#endif
            presentationSuspended = true;
            menuEnabledConstraint.Release();
            showAccessKey = false;
        };
    }

    private void ApplyMenuPresentationTemplate()
    {
        if (primaryButton is not null)
        {
            primaryButton.Click -= OnPrimaryButtonClick;
            ConnectActionPointerHandlers(primaryButton, attach: false);
        }

        if (submenuButton is not null)
        {
            submenuButton.Click -= OnSubmenuButtonClick;
            ConnectActionPointerHandlers(submenuButton, attach: false);
        }

        primaryClickFallback?.Dispose();
        submenuClickFallback?.Dispose();
        primaryPointerOver = submenuPointerOver = primaryPressed = submenuPressed = false;
        primaryButton = GetTemplateChild("PART_PrimaryButton") as WinUIButton;
        submenuButton = GetTemplateChild("PART_SubmenuButton") as WinUIButton;
        menuHeaderPresenter = GetTemplateChild("HeaderPresenter") as ContentPresenter;
        accessKeyText = GetTemplateChild("AccessKeyText") as TextBlock;
        if (primaryButton is not null)
        {
            var target = primaryButton;
            primaryButton.Click += OnPrimaryButtonClick;
            ConnectActionPointerHandlers(primaryButton, attach: true);
            primaryClickFallback = ButtonPointerClickFallback.Attach(primaryButton, () =>
            {
                if (IsLoaded && ReferenceEquals(primaryButton, target) && target.IsLoaded)
                {
                    InvokePrimaryAction();
                }
            });
        }

        if (submenuButton is not null)
        {
            var target = submenuButton;
            submenuButton.Click += OnSubmenuButtonClick;
            ConnectActionPointerHandlers(submenuButton, attach: true);
            submenuClickFallback = ButtonPointerClickFallback.Attach(submenuButton, () =>
            {
                if (IsLoaded && ReferenceEquals(submenuButton, target) && target.IsLoaded)
                {
                    InvokeSubmenuAction();
                }
            });
        }

        UpdateMenuPresentation();
    }

    private void OnPrimaryButtonClick(object sender, RoutedEventArgs args) => InvokePrimaryAction();

    private void InvokePrimaryAction()
    {
        Focus(FocusState.Pointer);
        OnClick();
    }

    private void OnSubmenuButtonClick(object sender, RoutedEventArgs args) => InvokeSubmenuAction();

    private void InvokeSubmenuAction()
    {
        if (CanOpenSubmenu)
        {
            Focus(FocusState.Pointer);
            IsDropDownOpen = !IsDropDownOpen;
        }
    }

    private void UpdateMenuPresentation()
    {
        if (updatingPresentation)
        {
            return;
        }

        updatingPresentation = true;
        try
        {
            if (!presentationSuspended)
            {
                menuEnabledConstraint.SetAllowed(
                    (IsSplit && HasSubItems) || commandAvailability.CanExecute);
            }

#if WINDOWS
            if (!IsLoaded || presentationSuspended)
            {
                return;
            }
#endif

            if (primaryButton is not null)
            {
                primaryButton.IsEnabled = HasSubItems && !IsSplit ? CanOpenSubmenu : CanInvoke;
                AutomationProperties.SetAccessibilityView(primaryButton, AccessibilityView.Raw);
            }

            if (submenuButton is not null)
            {
                submenuButton.Visibility = IsSplit && HasSubItems ? Visibility.Visible : Visibility.Collapsed;
                submenuButton.IsEnabled = CanOpenSubmenu;
                AutomationProperties.SetAccessibilityView(submenuButton, AccessibilityView.Raw);
            }

            if (GetTemplateChild("SubMenuIndicator") is FrameworkElement indicator)
            {
                indicator.Visibility = HasSubItems && !IsSplit ? Visibility.Visible : Visibility.Collapsed;
            }

            var state = !IsCheckable ? "NotCheckable"
                : !string.IsNullOrEmpty(GroupName) ? (IsChecked is true ? "RadioChecked" : "RadioUnchecked")
                : IsChecked switch { true => "Checked", false => "Unchecked", _ => "Indeterminate" };
            VisualStateManager.GoToState(this, state, false);
            VisualStateManager.GoToState(this, IsSplit && HasSubItems ? "Split" : "NotSplit", false);
            UpdateActionVisualStates();
            UpdateAccessKeyPresentation();
        }
        finally
        {
            updatingPresentation = false;
        }
    }

    private void ConnectActionPointerHandlers(WinUIButton button, bool attach)
    {
        if (attach)
        {
            button.PointerEntered += OnActionPointerEntered;
            button.PointerExited += OnActionPointerExited;
            button.AddHandler(PointerPressedEvent, new PointerEventHandler(OnActionPointerPressed), true);
            button.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnActionPointerReleased), true);
            button.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnActionPointerReleased), true);
        }
        else
        {
            button.PointerEntered -= OnActionPointerEntered;
            button.PointerExited -= OnActionPointerExited;
            button.RemoveHandler(PointerPressedEvent, new PointerEventHandler(OnActionPointerPressed));
            button.RemoveHandler(PointerReleasedEvent, new PointerEventHandler(OnActionPointerReleased));
            button.RemoveHandler(PointerCaptureLostEvent, new PointerEventHandler(OnActionPointerReleased));
        }
    }

    private void OnActionPointerEntered(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, primaryButton)) primaryPointerOver = true;
        else submenuPointerOver = true;
        UpdateActionVisualStates();
    }

    private void OnActionPointerExited(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, primaryButton)) primaryPointerOver = primaryPressed = false;
        else submenuPointerOver = submenuPressed = false;
        UpdateActionVisualStates();
    }

    private void OnActionPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, primaryButton)) primaryPressed = true;
        else submenuPressed = true;
        UpdateActionVisualStates();
    }

    private void OnActionPointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, primaryButton)) primaryPressed = false;
        else submenuPressed = false;
        UpdateActionVisualStates();
    }

    private void UpdateActionVisualStates()
    {
#if WINDOWS
        if (!IsLoaded || presentationSuspended)
        {
            return;
        }
#endif

        VisualStateManager.GoToState(this,
            primaryButton?.IsEnabled == false ? "PrimaryDisabled"
                : primaryPressed ? "PrimaryPressed"
                : primaryPointerOver ? "PrimaryPointerOver" : "PrimaryEnabled", false);
        VisualStateManager.GoToState(this,
            submenuButton?.IsEnabled == false ? "SubmenuDisabled"
                : submenuPressed ? "SubmenuPressed"
                : submenuPointerOver ? "SubmenuPointerOver" : "SubmenuEnabled", false);
    }

    private void ReconcileCheckedGroup()
    {
        if (IsCheckable && IsChecked is true)
        {
            UncheckGroupPeers();
        }
    }

    internal bool CanOpenSubmenu => HasSubItems && IsEnabled && AreMenuAncestorsEnabled();

    private bool AreMenuAncestorsEnabled()
    {
        for (var owner = DropDownOwner; owner is not null;
             owner = owner is MenuItem menu ? menu.DropDownOwner : null)
        {
            if (owner is Control { IsEnabled: false })
            {
                return false;
            }
        }

        return true;
    }

    private void OnMenuAccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
    {
        if (InvokeMenuAccessKey())
        {
            args.Handled = true;
        }
    }

    private bool InvokeMenuAccessKey()
    {
        if (HasSubItems)
        {
            if (!CanOpenSubmenu)
            {
                return false;
            }

            OpenSubmenuAndFocusFirstItem();
        }
        else
        {
            if (!CanInvoke)
            {
                return false;
            }

            OnClick();
        }

        return true;
    }

    internal string GetMenuHeaderName() =>
        Header is string text && RecognizesAccessKey
            ? ParseAccessText(text).Text
            : AutomationPeerHelpers.GetObjectName(Header);

    private void UpdateAccessKeyPresentation()
    {
#if WINDOWS
        if (!IsLoaded || presentationSuspended)
        {
            return;
        }
#endif

        var useAccessText = RecognizesAccessKey && Header is string
                            && HeaderTemplate is null && HeaderTemplateSelector is null;
        var parsed = ParseAccessText(useAccessText ? (string)Header! : string.Empty);
        UpdateGeneratedAccessKey(parsed.KeyIndex < 0 ? string.Empty : parsed.Text[parsed.KeyIndex].ToString());
        if (menuHeaderPresenter is not null)
        {
            menuHeaderPresenter.Visibility = useAccessText ? Visibility.Collapsed : Visibility.Visible;
        }

        if (accessKeyText is null)
        {
            return;
        }

        accessKeyText.Visibility = useAccessText ? Visibility.Visible : Visibility.Collapsed;
        accessKeyText.Inlines.Clear();
        if (parsed.KeyIndex < 0 || !showAccessKey)
        {
            accessKeyText.Text = parsed.Text;
            return;
        }

        accessKeyText.Inlines.Add(new Run { Text = parsed.Text[..parsed.KeyIndex] });
        accessKeyText.Inlines.Add(new Run
        {
            Text = parsed.Text[parsed.KeyIndex].ToString(),
            TextDecorations = TextDecorations.Underline,
        });
        accessKeyText.Inlines.Add(new Run { Text = parsed.Text[(parsed.KeyIndex + 1)..] });
    }

    private void UpdateGeneratedAccessKey(string key)
    {
        var currentBinding = GetBindingExpression(AccessKeyProperty)?.ParentBinding;
        if ((generatedAccessKeyBinding is not null && ReferenceEquals(currentBinding, generatedAccessKeyBinding))
            || (currentBinding is null && ReadLocalValue(AccessKeyProperty) == DependencyProperty.UnsetValue
                && string.IsNullOrEmpty((string)GetValue(AccessKeyProperty))))
        {
            // A binding is an ownership token that survives WinRT string marshalling.
            // A consumer's local value or replacement binding takes precedence.
            updatingGeneratedAccessKey = true;
            try
            {
                generatedAccessKey = key;
                generatedAccessKeyBinding = new Binding { Source = key, Mode = BindingMode.OneWay };
                SetBinding(AccessKeyProperty, generatedAccessKeyBinding);
            }
            finally
            {
                updatingGeneratedAccessKey = false;
            }
        }
    }

    internal static (string Text, int KeyIndex) ParseAccessText(string text)
    {
        var result = new StringBuilder(text.Length);
        var keyIndex = -1;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '_' && index + 1 < text.Length)
            {
                if (text[index + 1] == '_')
                {
                    result.Append('_');
                    index++;
                    continue;
                }

                if (keyIndex < 0)
                {
                    keyIndex = result.Length;
                    continue;
                }
            }

            result.Append(text[index]);
        }

        return (result.ToString(), keyIndex);
    }
}
