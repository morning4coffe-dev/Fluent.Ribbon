namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NativeComboBox = Microsoft.UI.Xaml.Controls.ComboBox;
using NativeTextBox = Microsoft.UI.Xaml.Controls.TextBox;

internal static class PortEditorHeaderContractTests
{
    internal static async Task VerifyAsync(Panel host, Func<Task> settle)
    {
        App.LogAutoTestStartup("EDITOR HEADERS metadata");
        AssertMetadata();
        App.LogAutoTestStartup("EDITOR HEADERS constructing fixture");
        var fixture = new PortEditorHeaderFixture();
        var editors = fixture.Editors;
        var state = fixture.State;
        state.Header = "Ordinary editor header";
        host.Children.Add(fixture);
        App.LogAutoTestStartup("EDITOR HEADERS fixture mounted");
        try
        {
            await settle();
            var bindings = CaptureBindings(editors);
            AssertFacadePresentationBindings(editors);
            App.LogAutoTestStartup("EDITOR HEADERS initial layout");
            foreach (var editor in editors)
            {
                Require(editor.IsLoaded && editor.XamlRoot is not null, editor, "was not rooted.");
                AssertPlainHeader(editor, (string)state.Header);
                AssertLayout(editor);
                AssertAutomation(editor, (string)state.Header);
            }
            await VerifyTextHeaderProjection(host, fixture, settle);
            await VerifyComboHeaderAutomation(fixture, editors.OfType<RibbonComboBox>(), settle);
            await VerifyQuickAccessHeaders(host, settle);

            var first = new PortEditorHeaderModel { Title = "First templated header" };
            state.Header = first;
            state.HeaderTemplate = fixture.ExplicitTemplate;
            state.HeaderTemplateSelector = fixture.Selector;
            await settle();
            AssertAllTemplates(editors, first, "PortEditorHeaderExplicit");
            foreach (var editor in editors)
            {
                AssertAutomation(editor, first.Title);
            }
            Require(fixture.Selector.Calls.Count == 0,
                "An editor invoked its selector while an explicit HeaderTemplate was present.");

            first.Title = "Updated model title";
            await settle();
            AssertAllTemplates(editors, first, "PortEditorHeaderExplicit");
            foreach (var combo in editors.OfType<RibbonComboBox>())
            {
                AssertAutomation(combo, first.Title);
            }
            state.HeaderTemplate = fixture.AlternateTemplate;
            await settle();
            AssertAllTemplates(editors, first, "PortEditorHeaderAlternate");
            Require(fixture.Selector.Calls.Count == 0,
                "A live explicit-template change lost priority over HeaderTemplateSelector.");

            state.HeaderTemplate = null;
            await settle();
            AssertFacadePresentationBindings(editors);
            AssertSelectedTemplates(editors, first, "PortEditorHeaderPrimary");
            Require(fixture.Selector.Calls.Count > 0
                    && fixture.Selector.Calls.All(call => ReferenceEquals(call.Item, first)),
                "Header selectors did not receive the actual header model.");
            Require(fixture.Selector.Calls.Where(call => call.Container is not null)
                    .All(call => editors.Any(editor => ReferenceEquals(call.Container, editor)
                                                       || ReferenceEquals(call.Container, HeaderPresenter(editor)))),
                "A header selector received an unrelated editor/container.");
            first.Title = "Live selected header";
            await settle();
            foreach (var editor in editors.Where(HasSelector))
            {
                AssertTemplate(editor, first, "PortEditorHeaderPrimary");
            }

            var second = new PortEditorHeaderModel { Title = "Alternate model", UseAlternate = true };
            state.Header = second;
            await settle();
            AssertSelectedTemplates(editors, second, "PortEditorHeaderAlternate");
            state.HeaderTemplateSelector = fixture.ReplacementSelector;
            await settle();
            AssertSelectedTemplates(editors, second, "PortEditorHeaderExplicit");
            Require(fixture.ReplacementSelector.Calls.Count > 0
                    && fixture.ReplacementSelector.Calls.All(call => ReferenceEquals(call.Item, second)),
                "Replacing HeaderTemplateSelector did not reselect the current model.");

            state.HeaderTemplateSelector = new PortEditorHeaderTemplateSelector { ReturnNull = true };
            await settle();
            foreach (var editor in editors)
            {
                AssertPlainHeader(editor, second.Title);
            }
            state.HeaderTemplateSelector = null;
            await settle();
            foreach (var editor in editors)
            {
                AssertPlainHeader(editor, second.Title);
            }

            state.Header = "Restored ordinary header";
            await settle();
            foreach (var editor in editors)
            {
                AssertPlainHeader(editor, (string)state.Header);
                AssertAutomation(editor, (string)state.Header);
                ((IScalableRibbonControl)editor).ScaleTo(RibbonControlSize.Small);
            }
            await settle();
            foreach (var editor in editors)
            {
                Require(HeaderPresenter(editor).Visibility == Visibility.Collapsed, editor,
                    "did not hide its header in Small size.");
                Require(Input(editor).IsEnabled, editor, "disabled its input when hiding the header.");
                ((IScalableRibbonControl)editor).ScaleTo(RibbonControlSize.Large);
            }
            await settle();
            foreach (var editor in editors)
            {
                AssertPlainHeader(editor, (string)state.Header);
            }

            state.Header = null;
            await settle();
            foreach (var editor in editors)
            {
                var presenter = HeaderPresenter(editor);
                var header = editor switch
                {
                    RibbonComboBox combo => combo.Header,
                    RibbonTextBox text => text.Header,
                    RibbonSpinner spinner => spinner.Header,
                    _ => throw new InvalidOperationException("Unexpected editor header owner."),
                };
                Require(header is null
                        && !Descendants<TextBlock>(presenter).Any(text =>
                            !string.IsNullOrEmpty(text.Text) && IsVisible(text)),
                    editor, "retained visible text for a null header. "
                    + $"content='{presenter.Content}', contentType={presenter.Content?.GetType().Name}, visibility={presenter.Visibility}, "
                    + $"texts={string.Join("|", Descendants<TextBlock>(presenter).Select(text => $"{text.Text}:{IsVisible(text)}"))}.");
                if (editor is RibbonComboBox or RibbonTextBox)
                {
                    Require(presenter.Visibility == Visibility.Collapsed, editor,
                        "overrode its null-header visibility binding in Large size.");
                }
            }

            state.Header = first;
            state.HeaderTemplateSelector = fixture.Selector;
            await settle();
            await VerifyRetemplateAndReload(host, fixture, editors, settle);
            await VerifyNativeTemplateBindings(fixture, editors, settle);
            await VerifyConsumerTemplate(fixture, editors, settle);
            AssertBindings(bindings);
            AssertFacadePresentationBindings(editors);

            state.Header = new PortEditorHeaderModel { Title = "Editable templated header" };
            state.HeaderTemplate = fixture.ExplicitTemplate;
            state.HeaderTemplateSelector = fixture.Selector;
            await settle();
            AssertAllTemplates(editors, (PortEditorHeaderModel)state.Header, "PortEditorHeaderExplicit");
            await VerifyInputs(fixture, editors, settle);
            AssertBindings(bindings);

            state.HeaderTemplate = null;
            state.HeaderTemplateSelector = null;
            state.Header = "Final plain header";
            await settle();
            foreach (var editor in editors)
            {
                AssertPlainHeader(editor, (string)state.Header);
                AssertAutomation(editor, (string)state.Header);
            }
        }
        finally
        {
            foreach (var combo in editors.OfType<RibbonComboBox>())
            {
                combo.IsDropDownOpen = false;
            }
            host.Children.Remove(fixture);
            await settle();
        }
    }

    private static async Task VerifyTextHeaderProjection(Panel host, PortEditorHeaderFixture fixture, Func<Task> settle)
    {
        Require(ReferenceEquals(RibbonTextBox.HeaderProperty, NativeTextBox.HeaderProperty)
                && ReferenceEquals(Fluent.TextBox.HeaderTemplateProperty, RibbonTextBox.HeaderTemplateProperty)
                && ReferenceEquals(Fluent.TextBox.HeaderTemplateSelectorProperty, RibbonTextBox.HeaderTemplateSelectorProperty),
            "Text editor header identity or the shared requested-template DPs diverged.");
        foreach (var useFacade in new[] { false, true })
        {
            RibbonTextBox textBox = useFacade ? new Fluent.TextBox() : new RibbonTextBox();
            textBox.Width = 420;
            var first = new PortEditorHeaderModel { Title = "Model assigned before its header template" };
            var source = new PortEditorHeaderState { Header = first };
            textBox.SetBinding(RibbonTextBox.HeaderProperty, new Binding
            {
                Source = source, Path = new PropertyPath(nameof(PortEditorHeaderState.Header)), Mode = BindingMode.TwoWay,
            });
            textBox.SetBinding(RibbonTextBox.HeaderTemplateProperty, new Binding
            {
                Source = source, Path = new PropertyPath(nameof(PortEditorHeaderState.HeaderTemplate)), Mode = BindingMode.TwoWay,
            });
            textBox.SetBinding(RibbonTextBox.HeaderTemplateSelectorProperty, new Binding
            {
                Source = source, Path = new PropertyPath(nameof(PortEditorHeaderState.HeaderTemplateSelector)), Mode = BindingMode.TwoWay,
            });
            var bindings = CaptureBindings(new[] { textBox });
            host.Children.Add(textBox);
            try
            {
                await settle();
                AssertTextProjection(textBox, source);
                AssertPlainHeader(textBox, first.Title);

                var fallback = ((NativeTextBox)textBox).HeaderTemplate
                               ?? throw new InvalidOperationException("The native model header has no protection template.");
                var fallbackContent = fallback.LoadContent() as FrameworkElement
                                      ?? throw new InvalidOperationException("The native header fallback has no real content.");
                fallbackContent.DataContext = first;
                host.Children.Add(fallbackContent);
                try
                {
                    await settle();
                    Require(Descendants<TextBlock>(fallbackContent).Any(label => label.Text == first.Title && IsVisible(label)),
                        textBox, "its native fallback template did not render ordinary model content.");
                }
                finally
                {
                    host.Children.Remove(fallbackContent);
                }

                var selector = new PortEditorHeaderTemplateSelector
                {
                    PrimaryTemplate = fixture.PrimaryTemplate, AlternateTemplate = fixture.AlternateTemplate,
                };
                source.HeaderTemplate = fixture.ExplicitTemplate;
                var second = new PortEditorHeaderModel { Title = "Template assigned before its model", UseAlternate = true };
                source.Header = second;
                source.HeaderTemplateSelector = selector;
                await settle();
                AssertTextProjection(textBox, source);
                AssertTemplate(textBox, second, "PortEditorHeaderExplicit");
                Require(selector.Calls.Count == 0, textBox, "selected a template despite an explicit requested template.");
                second.Title = "Live projected header model";
                await settle();
                AssertTemplate(textBox, second, "PortEditorHeaderExplicit");

                source.HeaderTemplate = null;
                await settle();
                AssertTextProjection(textBox, source);
                AssertTemplate(textBox, second, "PortEditorHeaderAlternate");
                source.HeaderTemplateSelector = new PortEditorHeaderTemplateSelector { ReturnNull = true };
                await settle();
                AssertPlainHeader(textBox, second.Title);
                source.HeaderTemplateSelector = null;
                source.Header = null;
                await settle();
                AssertTextProjection(textBox, source);
                Require(HeaderPresenter(textBox).Visibility == Visibility.Collapsed, textBox, "showed a null projected header.");

                var visualHeader = new TextBlock { Text = "An authored visual header" };
                source.Header = visualHeader;
                await settle();
                AssertTextProjection(textBox, source);
                Require(ReferenceEquals(HeaderPresenter(textBox).Content, visualHeader) && IsVisible(visualHeader),
                    textBox, "wrapped or replaced an authored UIElement header.");
                source.Header = first;
                var defaultTemplate = textBox.Template;
                textBox.Template = fixture.ConsumerCoreTextTemplate;
                await settle();
                Require(ReferenceEquals(HeaderPresenter(textBox).Content, first), textBox,
                    "an authored native TemplateBinding Header lost model identity.");
                source.HeaderTemplateSelector = selector;
                await settle();
                AssertTemplate(textBox, first, "PortEditorHeaderPrimary");
                source.HeaderTemplateSelector = null;
                await settle();
                AssertPlainHeader(textBox, first.Title);
                textBox.Template = defaultTemplate;
                await settle();
                AssertBindings(bindings);

                // The native nullable-template/CLR-model combination itself is
                // unsupported by WinUI. Non-null native bindings remain owned by
                // their caller; nullable requests use the Fluent DP tested above.
                var nativeSource = new PortEditorHeaderState { HeaderTemplate = fixture.AlternateTemplate };
                textBox.SetBinding(NativeTextBox.HeaderTemplateProperty, new Binding
                {
                    Source = nativeSource, Path = new PropertyPath(nameof(PortEditorHeaderState.HeaderTemplate)), Mode = BindingMode.TwoWay,
                });
                var nativeBinding = CaptureBinding(textBox, NativeTextBox.HeaderTemplateProperty);
                await settle();
                AssertTemplate(textBox, first, "PortEditorHeaderAlternate");
                source.HeaderTemplate = fixture.ExplicitTemplate;
                await settle();
                AssertTemplate(textBox, first, "PortEditorHeaderExplicit");
                Require(ReferenceEquals(nativeSource.HeaderTemplate, fixture.AlternateTemplate)
                        && ReferenceEquals(((NativeTextBox)textBox).HeaderTemplate, fixture.AlternateTemplate),
                    textBox, "overwrote an independent native template binding or its source.");
                nativeSource.HeaderTemplate = fixture.PrimaryTemplate;
                source.HeaderTemplate = null;
                await settle();
                AssertTemplate(textBox, first, "PortEditorHeaderPrimary");
                AssertBindings(bindings);
                AssertBindings(new[] { nativeBinding });
            }
            finally
            {
                host.Children.Remove(textBox);
                await settle();
            }
        }
    }

    private static async Task VerifyQuickAccessHeaders(Panel host, Func<Task> settle)
    {
        var fixture = new PortEditorHeaderFixture();
        var editors = fixture.Editors;
        host.Children.Add(fixture);
        var first = new PortEditorHeaderModel { Title = "Quick-access header model" };
        fixture.State.Header = first;
        fixture.State.HeaderTemplate = fixture.ExplicitTemplate;
        fixture.State.HeaderTemplateSelector = fixture.Selector;
        var copies = new List<Control>();
        try
        {
            foreach (var editor in editors)
            {
                var copy = ((IQuickAccessItemProvider)editor).CreateQuickAccessItem() as Control
                           ?? throw new InvalidOperationException("The editor has no quick-access control.");
                copies.Add(copy);
                ((IScalableRibbonControl)copy).ScaleTo(RibbonControlSize.Medium);
                host.Children.Add(copy);
            }
            await settle();
            AssertAllTemplates(copies.ToArray(), first, "PortEditorHeaderExplicit");
            fixture.State.HeaderTemplate = fixture.AlternateTemplate;
            await settle();
            AssertAllTemplates(copies.ToArray(), first, "PortEditorHeaderAlternate");
            fixture.State.HeaderTemplate = null;
            await settle();
            AssertSelectedTemplates(copies.ToArray(), first, "PortEditorHeaderPrimary");
            var second = new PortEditorHeaderModel { Title = "Updated quick-access header", UseAlternate = true };
            fixture.State.Header = second;
            await settle();
            AssertSelectedTemplates(copies.ToArray(), second, "PortEditorHeaderAlternate");
            fixture.State.HeaderTemplateSelector = null;
            await settle();
            foreach (var copy in copies)
            {
                AssertPlainHeader(copy, second.Title);
            }
            for (var index = 0; index < editors.Length; index++)
            {
                var editor = editors[index];
                var copy = copies[index];
                var property = editor switch
                {
                    RibbonComboBox => RibbonComboBox.HeaderProperty,
                    RibbonTextBox => RibbonTextBox.HeaderProperty,
                    RibbonSpinner => RibbonSpinner.HeaderProperty,
                    _ => throw new InvalidOperationException("Unexpected editor header owner."),
                };
                var originalBinding = editor.GetBindingExpression(property)!.ParentBinding;
                var headerState = new PortEditorHeaderState { Header = second };
                editor.SetBinding(property, new Binding
                {
                    Source = headerState, Path = new PropertyPath(nameof(PortEditorHeaderState.Header)), Mode = BindingMode.OneWay,
                });
                var headerBinding = editor.GetBindingExpression(property)!.ParentBinding;
                try
                {
                    headerState.Header = null;
                    await settle();
                    var visual = new TextBlock { Text = "A live visual editor header" };
                    headerState.Header = visual;
                    await settle();
                    var visualCopy = HeaderPresenter(copy).Content as TextBlock;
                    Require(ReferenceEquals(HeaderPresenter(editor).Content, visual)
                            && visualCopy is not null && !ReferenceEquals(visualCopy, visual)
                            && IsVisible(visual) && IsVisible(visualCopy)
                            && visual.ActualWidth > 0 && visualCopy.ActualWidth > 0,
                        editor, "a model/null-to-visual header update shared or stole its quick-access visual.");
                    visual.Text = "Updated live visual editor header";
                    await settle();
                    AssertPlainHeader(editor, visual.Text);
                    AssertPlainHeader(copy, visual.Text);
                    headerState.Header = first;
                    await settle();
                    AssertPlainHeader(editor, first.Title);
                    AssertPlainHeader(copy, first.Title);
                    Require(ReferenceEquals(editor.GetBindingExpression(property)?.ParentBinding, headerBinding)
                            && ReferenceEquals(headerState.Header, first),
                        editor, "visual-safe quick-access projection changed its source Header binding or model.");
                }
                finally
                {
                    editor.SetBinding(property, originalBinding);
                    await settle();
                }
            }
        }
        finally
        {
            foreach (var copy in copies)
            {
                host.Children.Remove(copy);
            }
            host.Children.Remove(fixture);
            await settle();
        }
    }

    private static void AssertTextProjection(RibbonTextBox textBox, PortEditorHeaderState source)
    {
        Require(ReferenceEquals(textBox.Header, source.Header)
                && ReferenceEquals(textBox.GetValue(NativeTextBox.HeaderProperty), source.Header),
            textBox, "changed the header model or its native dependency-property identity.");
        Require(ReferenceEquals(textBox.HeaderTemplate, source.HeaderTemplate)
                && ReferenceEquals(textBox.GetValue(RibbonTextBox.HeaderTemplateProperty), source.HeaderTemplate)
                && ReferenceEquals(textBox.HeaderTemplateSelector, source.HeaderTemplateSelector),
            textBox, "changed a requested template/selector value while protecting native presentation.");
        Require(((NativeTextBox)textBox).HeaderTemplate is not null
                && (source.HeaderTemplate is null
                    || ReferenceEquals(((NativeTextBox)textBox).HeaderTemplate, source.HeaderTemplate)),
            textBox, "allowed native visibility to inspect an untemplated CLR header model.");
    }

    private static async Task VerifyComboHeaderAutomation(
        PortEditorHeaderFixture fixture, IEnumerable<RibbonComboBox> combos, Func<Task> settle)
    {
        foreach (var combo in combos)
        {
            var input = Input(combo);
            var panel = VisualTreeHelper.GetParent(combo) as Panel
                        ?? throw new InvalidOperationException("The combo header fixture has no panel parent.");
            var consumerLabel = new TextBlock { Text = "Consumer combo label" };
            var nameModel = new PortEditorHeaderState { Header = "Consumer editable name" };
            var labelModel = new PortEditorHeaderState { Header = consumerLabel };
            panel.Children.Add(consumerLabel);
            Exception? verificationFailure = null;
            try
            {
                input.SetBinding(AutomationProperties.NameProperty, new Binding
                {
                    Source = nameModel, Path = new PropertyPath(nameof(PortEditorHeaderState.Header)), Mode = BindingMode.OneWay,
                });
                var nameBinding = input.GetBindingExpression(AutomationProperties.NameProperty)!.ParentBinding;
                fixture.State.Header = "Header changed behind a consumer name";
                await settle();
                Require(AutomationProperties.GetName(input) == (string)nameModel.Header
                        && ReferenceEquals(input.GetBindingExpression(AutomationProperties.NameProperty)?.ParentBinding, nameBinding),
                    combo, "overwrote a consumer's editable Name binding after a header change.");
                Require(FrameworkElementAutomationPeer.CreatePeerForElement(input)?.GetName() == (string)nameModel.Header,
                    combo, "did not expose the consumer's explicit editable Name through UIA.");
                nameModel.Header = "Updated consumer editable name";
                await settle();
                Require(AutomationProperties.GetName(input) == (string)nameModel.Header
                        && FrameworkElementAutomationPeer.CreatePeerForElement(input)?.GetName() == (string)nameModel.Header,
                    combo, "the consumer's editable Name binding stopped updating UIA.");

                input.ClearValue(AutomationProperties.NameProperty);
                var header = new PortEditorHeaderModel { Title = "Templated native automation header" };
                fixture.State.Header = header;
                fixture.State.HeaderTemplate = fixture.ExplicitTemplate;
                await settle();
                AssertAutomation(combo, header.Title);

                input.SetBinding(AutomationProperties.LabeledByProperty, new Binding
                {
                    Source = labelModel, Path = new PropertyPath(nameof(PortEditorHeaderState.Header)), Mode = BindingMode.OneWay,
                });
                var labelBinding = input.GetBindingExpression(AutomationProperties.LabeledByProperty)!.ParentBinding;
                fixture.State.HeaderTemplate = fixture.AlternateTemplate;
                header.Title = "Header changed behind a consumer label";
                await settle();
                Require(ReferenceEquals(AutomationProperties.GetLabeledBy(input), consumerLabel)
                        && ReferenceEquals(input.GetBindingExpression(AutomationProperties.LabeledByProperty)?.ParentBinding, labelBinding),
                    combo, "overwrote a consumer's LabeledBy binding during header template changes.");
                Require(FrameworkElementAutomationPeer.CreatePeerForElement(input)?.GetName() == consumerLabel.Text,
                    combo, "did not expose the consumer's real label through UIA.");
                consumerLabel.Text = "Updated consumer combo label";
                await settle();
                Require(FrameworkElementAutomationPeer.CreatePeerForElement(input)?.GetName() == consumerLabel.Text,
                    combo, "did not follow live text changes on the consumer's label.");

                input.ClearValue(AutomationProperties.LabeledByProperty);
                fixture.State.HeaderTemplate = null;
                fixture.State.Header = "Plain header after consumer automation";
                await settle();
                AssertAutomation(combo, (string)fixture.State.Header);
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
                    input.ClearValue(AutomationProperties.NameProperty);
                    input.ClearValue(AutomationProperties.LabeledByProperty);
                    panel.Children.Remove(consumerLabel);
                    fixture.State.HeaderTemplate = null;
                    fixture.State.HeaderTemplateSelector = null;
                    fixture.State.Header = "Ordinary editor header";
                    await settle();
                }
                catch (Exception cleanupFailure) when (verificationFailure is not null)
                {
                    throw new AggregateException("Header automation verification and cleanup both failed.",
                        verificationFailure, cleanupFailure);
                }
            }
        }
    }

    private static async Task VerifyRetemplateAndReload(
        Panel host, PortEditorHeaderFixture fixture, Control[] editors, Func<Task> settle)
    {
        var oldPresenters = editors.Select(HeaderPresenter).ToArray();
        var templates = editors.Select(editor => editor.Template).ToArray();
        Require(templates.All(template => template is not null), "An editor has no real default control template.");
        foreach (var editor in editors)
        {
            ((IScalableRibbonControl)editor).ScaleTo(RibbonControlSize.Small);
            editor.Template = null;
        }
        await settle();
        for (var index = 0; index < editors.Length; index++)
        {
            editors[index].Template = templates[index];
            editors[index].ApplyTemplate();
        }
        await settle();
        for (var index = 0; index < editors.Length; index++)
        {
            var editor = editors[index];
            Require(!ReferenceEquals(HeaderPresenter(editor), oldPresenters[index]), editor,
                "did not recreate the header presentation part.");
            Require(HeaderPresenter(editor).Visibility == Visibility.Collapsed, editor,
                "forgot Small size when applying a new template.");
            ((IScalableRibbonControl)editor).ScaleTo(RibbonControlSize.Large);
        }
        await settle();
        AssertSelectedTemplates(editors, (PortEditorHeaderModel)fixture.State.Header!, "PortEditorHeaderPrimary");

        host.Children.Remove(fixture);
        await settle();
        Require(!fixture.IsLoaded && editors.All(editor => !editor.IsLoaded),
            "The editor fixture did not unload.");
        var reloaded = new PortEditorHeaderModel { Title = "Changed while unloaded", UseAlternate = true };
        fixture.State.Header = reloaded;
        fixture.State.HeaderTemplate = fixture.ExplicitTemplate;
        fixture.State.HeaderTemplateSelector = fixture.ReplacementSelector;
        host.Children.Add(fixture);
        await settle();
        AssertAllTemplates(editors, reloaded, "PortEditorHeaderExplicit");
        fixture.State.HeaderTemplate = null;
        await settle();
        AssertSelectedTemplates(editors, reloaded, "PortEditorHeaderExplicit");
        fixture.State.HeaderTemplateSelector = null;
        fixture.State.Header = "Reloaded ordinary header";
        await settle();
        foreach (var editor in editors)
        {
            AssertPlainHeader(editor, (string)fixture.State.Header);
        }
    }

    private static async Task VerifyNativeTemplateBindings(
        PortEditorHeaderFixture fixture, Control[] editors, Func<Task> settle)
    {
        var facades = editors.OfType<Fluent.ComboBox>().ToArray();
        var nativeState = new PortEditorHeaderState { HeaderTemplate = fixture.AlternateTemplate };
        var bindings = new List<ObservedBinding>();
        foreach (var facade in facades)
        {
            var property = NativeComboBox.HeaderTemplateProperty;
            facade.SetBinding(property, new Binding
            {
                Source = nativeState,
                Path = new PropertyPath(nameof(PortEditorHeaderState.HeaderTemplate)),
                Mode = BindingMode.TwoWay,
            });
            bindings.Add(CaptureBinding(facade, property));
        }

        try
        {
            var model = new PortEditorHeaderModel { Title = "Native and facade header templates" };
            fixture.State.Header = model;
            fixture.State.HeaderTemplate = null;
            fixture.State.HeaderTemplateSelector = fixture.Selector;
            await settle();
            AssertAllTemplates(facades, model, "PortEditorHeaderAlternate");
            AssertBindings(bindings);
            Require(ReferenceEquals(nativeState.HeaderTemplate, fixture.AlternateTemplate),
                "The facade wrote a selected template into the native two-way template model.");

            nativeState.HeaderTemplate = null;
            await settle();
            AssertAllTemplates(facades, model, "PortEditorHeaderPrimary");
            AssertBindings(bindings);
            Require(nativeState.HeaderTemplate is null
                    && bindings.All(binding => binding.Owner.GetValue(binding.Property) is null),
                "A selector result replaced a consumer's null native HeaderTemplate value/binding.");

            nativeState.HeaderTemplate = fixture.ExplicitTemplate;
            fixture.State.HeaderTemplate = fixture.AlternateTemplate;
            await settle();
            AssertAllTemplates(facades, model, "PortEditorHeaderAlternate");
            Require(ReferenceEquals(nativeState.HeaderTemplate, fixture.ExplicitTemplate),
                "The facade overwrote an independent native HeaderTemplate binding.");
            fixture.State.HeaderTemplate = null;
            await settle();
            AssertAllTemplates(facades, model, "PortEditorHeaderExplicit");
            nativeState.HeaderTemplate = null;
            fixture.State.HeaderTemplateSelector = null;
            fixture.State.Header = "Native binding restored to text";
            await settle();
            foreach (var facade in facades)
            {
                AssertPlainHeader(facade, (string)fixture.State.Header);
            }
            AssertBindings(bindings);
        }
        finally
        {
            foreach (var binding in bindings)
            {
                binding.Owner.ClearValue(binding.Property);
            }
        }
    }

    private static async Task VerifyConsumerTemplate(
        PortEditorHeaderFixture fixture, Control[] editors, Func<Task> settle)
    {
        var editor = editors.OfType<Fluent.TextBox>().Single();
        var template = editor.Template;
        var model = new PortEditorHeaderModel { Title = "Consumer-authored header template" };
        fixture.State.Header = model;
        fixture.State.HeaderTemplate = fixture.ExplicitTemplate;
        fixture.State.HeaderTemplateSelector = fixture.Selector;
        editor.Template = fixture.ConsumerTextTemplate;
        try
        {
            await settle();
            var presenter = HeaderPresenter(editor);
            var templateBinding = presenter.GetBindingExpression(ContentPresenter.ContentTemplateProperty)?.ParentBinding;
            var selectorBinding = presenter.GetBindingExpression(ContentPresenter.ContentTemplateSelectorProperty)?.ParentBinding;
            Require(templateBinding is not null && selectorBinding is not null, editor,
                "replaced an authored template part's header bindings.");
            AssertTemplate(editor, model, "PortEditorHeaderExplicit");
            fixture.State.HeaderTemplate = null;
            await settle();
            AssertTemplate(editor, model, "PortEditorHeaderPrimary");
            fixture.State.HeaderTemplateSelector = fixture.ReplacementSelector;
            model.Title = "Live authored header";
            await settle();
            AssertTemplate(editor, model, "PortEditorHeaderExplicit");
            Require(ReferenceEquals(presenter.GetBindingExpression(ContentPresenter.ContentTemplateProperty)?.ParentBinding,
                        templateBinding)
                    && ReferenceEquals(presenter.GetBindingExpression(ContentPresenter.ContentTemplateSelectorProperty)?.ParentBinding,
                        selectorBinding),
                editor, "overwrote a consumer's presentation bindings during facade updates.");
            fixture.State.HeaderTemplateSelector = null;
            fixture.State.Header = "Authored template returned to text";
            await settle();
            AssertPlainHeader(editor, (string)fixture.State.Header);
        }
        finally
        {
            editor.Template = template;
            await settle();
        }
    }

    private static async Task VerifyInputs(PortEditorHeaderFixture fixture, Control[] editors, Func<Task> settle)
    {
        foreach (var editor in editors)
        {
            var input = Input(editor);
            editor.IsEnabled = false;
            await settle();
            Require(!input.IsEnabled, editor, "lost its disabled input state after header updates.");
            editor.IsEnabled = true;
            await settle();
            Require(input.IsEnabled && FocusInput(input), editor,
                "cannot focus its real input after header template changes.");
            await settle();
            Require(ReferenceEquals(FocusManager.GetFocusedElement(editor.XamlRoot), input), editor,
                "redirected input focus into the header.");
            AssertLayout(editor);

            switch (editor)
            {
                case RibbonComboBox combo:
                    input.Text = "Edited combo value";
                    await settle();
                    Require(combo.Text == input.Text, editor, "lost its editable text binding.");
                    combo.Items.Add("First header diagnostic item");
                    combo.Items.Add("Second header diagnostic item");
                    combo.IsDropDownOpen = true;
                    await settle();
                    Require(combo.IsDropDownOpen, editor, "cannot open its native dropdown.");
                    combo.SelectedIndex = 1;
                    combo.IsDropDownOpen = false;
                    await settle();
                    Require(Equals(combo.SelectedItem, "Second header diagnostic item"), editor,
                        "lost native item selection after a header change.");
                    break;
                case RibbonTextBox textBox:
                    input.Text = "Edited text value";
                    Require(fixture.FocusSink.Focus(FocusState.Programmatic), editor,
                        "cannot move focus out of its native text part.");
                    await settle();
                    Require(textBox.Text == input.Text, editor, "lost its editor-to-owner text binding.");
                    textBox.Select(0, 6);
                    Require(input.SelectionStart == 0 && input.SelectionLength == 6, editor,
                        "lost selection/focus delegation to its native text part.");
                    textBox.SelectAll();
                    Require(input.SelectionStart == 0 && input.SelectionLength == input.Text.Length
                            && textBox.SelectionLength == input.Text.Length, editor,
                        "SelectAll did not delegate to the rendered text part.");
                    textBox.SelectionStart = 1;
                    textBox.SelectionLength = 3;
                    Require(input.SelectionStart == 1 && input.SelectionLength == 3, editor,
                        "selection properties stopped updating the rendered text part.");
                    await VerifyTextSelectionOwnership(fixture, textBox, settle);
                    break;
                case RibbonSpinner spinner:
                    input.Text = 11.5d.ToString(CultureInfo.CurrentCulture);
                    Require(fixture.FocusSink.Focus(FocusState.Programmatic), editor,
                        "cannot move focus to commit spinner text.");
                    await settle();
                    Require(spinner.Value == 11.5
                            && input.Text == 11.5d.ToString(spinner.Format, CultureInfo.CurrentCulture)
                            && spinner.Text == input.Text,
                        editor, "changed spinner parsing or formatting while rendering a header template.");
                    break;
            }
        }
    }

    private static async Task VerifyTextSelectionOwnership(
        PortEditorHeaderFixture fixture, RibbonTextBox textBox, Func<Task> settle)
    {
        var panel = VisualTreeHelper.GetParent(textBox) as Panel
                    ?? throw new InvalidOperationException("The text selection fixture has no panel parent.");
        var index = panel.Children.IndexOf(textBox);
        var originalTemplate = textBox.Template;
        var originalText = textBox.Text;
        var originalBinding = textBox.GetBindingExpression(NativeTextBox.TextProperty)?.ParentBinding;
        var model = new PortEditorHeaderState { Header = "0123456789ABCDEFGHIJ" };
        var changes = new List<(int Start, int Length, string Text)>();
        RoutedEventHandler onSelectionChanged = (sender, _) =>
        {
            Require(ReferenceEquals(sender, textBox), textBox, "raised selection changes with the wrong public sender.");
            changes.Add((textBox.SelectionStart, textBox.SelectionLength, textBox.SelectedText));
        };
        textBox.SetBinding(NativeTextBox.TextProperty, new Binding
        {
            Source = model,
            Path = new PropertyPath(nameof(PortEditorHeaderState.Header)),
            Mode = BindingMode.TwoWay,
        });
        try
        {
            await settle();
            var binding = textBox.GetBindingExpression(NativeTextBox.TextProperty)!.ParentBinding;
            var input = Input(textBox);
            textBox.Select(0, 0);
            await settle();
            textBox.SelectionChanged += onSelectionChanged;

            textBox.Select(2, 5);
            await settle();
            Require(changes.SequenceEqual(new[] { (2, 5, "23456") }), textBox,
                "did not publish exactly one coherent selection change from Select.");
            textBox.Select(2, 5);
            await settle();
            Require(changes.Count == 1, textBox, "published a duplicate no-op selection change.");
            input.Select(4, 2);
            await settle();
            Require(changes.Count == 2 && changes[^1] == (4, 2, "45"), textBox,
                "did not relay the real editor's selection through its public event/getters.");

            var reentered = false;
            RoutedEventHandler reentrant = (_, _) =>
            {
                if (!reentered && textBox.SelectionStart == 1 && textBox.SelectionLength == 2)
                {
                    reentered = true;
                    textBox.Select(3, 2);
                }
            };
            textBox.SelectionChanged += reentrant;
            try
            {
                var before = changes.Count;
                textBox.Select(1, 2);
                await settle();
                Require(reentered && changes.Count == before + 2 && changes[^1] == (3, 2, "34")
                        && input.SelectionStart == 3 && input.SelectionLength == 2,
                    textBox, "lost or duplicated a reentrant public selection change.");
            }
            finally
            {
                textBox.SelectionChanged -= reentrant;
            }

            textBox.Select(int.MaxValue, int.MaxValue);
            Require(textBox.SelectionStart == textBox.Text.Length && textBox.SelectionLength == 0,
                textBox, "did not clamp an oversized selection to the current text.");
            var rejectedNegative = false;
            try { textBox.Select(-1, 1); }
            catch (ArgumentException) { rejectedNegative = true; }
            Require(rejectedNegative, textBox, "accepted a negative selection start.");

            Require(FocusInput(input), textBox, "could not focus its bound selection editor.");
            await settle();
            input.Text = "Live bound selection text";
            Require(fixture.FocusSink.Focus(FocusState.Programmatic), textBox, "could not commit bound selection text.");
            await settle();
            Require(Equals(model.Header, input.Text) && textBox.Text == input.Text
                    && ReferenceEquals(textBox.GetBindingExpression(NativeTextBox.TextProperty)?.ParentBinding, binding),
                textBox, "selection delegation replaced or broke its two-way Text binding.");
            model.Header = "0123456789ABCDEFGHIJ";
            await settle();
            Require(input.Text == (string)model.Header && textBox.Text == input.Text, textBox,
                "its Text binding stopped observing source changes after selection.");
            textBox.Select(3, 4);
            await settle();
            var previousInput = input;
            textBox.Template = null;
            await settle();
            Require(textBox.SelectionStart == 3 && textBox.SelectionLength == 4 && textBox.SelectedText == "3456",
                textBox, "lost the retained range while its editor template was removed.");
            textBox.Select(2, 5);
            var beforeRestore = changes.Count;
            textBox.Template = originalTemplate;
            textBox.ApplyTemplate();
            await settle();
            input = Input(textBox);
            Require(!ReferenceEquals(input, previousInput) && input.SelectionStart == 2 && input.SelectionLength == 5
                    && textBox.SelectedText == "23456" && changes.Count == beforeRestore,
                textBox, "did not restore the latest selection once on the replacement editor.");

            panel.Children.Remove(textBox);
            await settle();
            textBox.SelectionStart = 4;
            textBox.SelectionLength = 3;
            beforeRestore = changes.Count;
            panel.Children.Insert(index, textBox);
            await settle();
            input = Input(textBox);
            Require(input.SelectionStart == 4 && input.SelectionLength == 3 && textBox.SelectedText == "456"
                    && changes.Count == beforeRestore
                    && ReferenceEquals(textBox.GetBindingExpression(NativeTextBox.TextProperty)?.ParentBinding, binding),
                textBox, "reload lost a pending selection, duplicated its notification, or replaced the Text binding.");
            input.Select(1, 2);
            await settle();
            Require(changes.Count == beforeRestore + 1 && changes[^1] == (1, 2, "12"), textBox,
                "reload duplicated or lost the current editor's selection subscription.");

            var alternateParent = new StackPanel();
            panel.Children.Add(alternateParent);
            try
            {
                var unloadHandlers = new[] { "OnSelectionOwnerUnloaded", "OnSelectionEditorUnloaded" };
                for (var cycle = 0; cycle < unloadHandlers.Length; cycle++)
                {
                    panel.Children.Remove(textBox);
                    alternateParent.Children.Add(textBox);
                    await settle();
                    input = Input(textBox);
                    Require(textBox.IsLoaded && input.IsLoaded && IsVisible(input)
                            && input.ActualWidth > 0 && input.ActualHeight > 0, textBox,
                        "same-turn reparenting did not leave a live selection editor.");

                    // Replay an obsolete notification only after real re-entry
                    // and the restore queue settle; the selection comes from the
                    // actual inner editor, not the wrapper's public Select method.
                    var handler = typeof(RibbonTextBox).GetMethod(unloadHandlers[cycle],
                                      BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                  ?? throw new MissingMethodException(typeof(RibbonTextBox).FullName, unloadHandlers[cycle]);
                    handler.Invoke(textBox, new object[] { cycle == 0 ? textBox : input, new RoutedEventArgs() });
                    beforeRestore = changes.Count;
                    var start = 6 + cycle;
                    input.Select(start, 2);
                    await settle();
                    var selected = textBox.Text.Substring(start, 2);
                    Require(input.SelectionStart == start && input.SelectionLength == 2
                            && textBox.SelectionStart == start && textBox.SelectionLength == 2
                            && textBox.SelectedText == selected
                            && changes.Count == beforeRestore + 1 && changes[^1] == (start, 2, selected)
                            && ReferenceEquals(textBox.GetBindingExpression(NativeTextBox.TextProperty)?.ParentBinding, binding),
                        textBox, "an obsolete unload notification stranded live selection after same-turn reparenting.");
                    alternateParent.Children.Remove(textBox);
                    panel.Children.Insert(index, textBox);
                    await settle();
                }
            }
            finally
            {
                if (alternateParent.Children.Contains(textBox))
                {
                    alternateParent.Children.Remove(textBox);
                }
                if (!panel.Children.Contains(textBox))
                {
                    panel.Children.Insert(Math.Min(index, panel.Children.Count), textBox);
                }
                panel.Children.Remove(alternateParent);
                await settle();
            }

            textBox.Template = fixture.NativeTextTemplate;
            textBox.ApplyTemplate();
            await settle();
            textBox.Select(2, 4);
            await settle();
            Require(((NativeTextBox)textBox).SelectionStart == 2 && ((NativeTextBox)textBox).SelectionLength == 4
                    && textBox.SelectedText == "2345", textBox,
                "did not delegate to a real native-template editor when PART_TextBox was absent. "
                + $"Native={((NativeTextBox)textBox).SelectionStart},{((NativeTextBox)textBox).SelectionLength}; "
                + $"public={textBox.SelectionStart},{textBox.SelectionLength},'{textBox.SelectedText}'; loaded={textBox.IsLoaded}.");
            beforeRestore = changes.Count;
            ((NativeTextBox)textBox).Select(1, 3);
            await settle();
            Require(changes.Count == beforeRestore + 1 && changes[^1] == (1, 3, "123")
                    && ReferenceEquals(textBox.GetBindingExpression(NativeTextBox.TextProperty)?.ParentBinding, binding)
                    && Equals(model.Header, "0123456789ABCDEFGHIJ"),
                textBox, "lost native-template selection notifications or changed the bound text.");
        }
        finally
        {
            textBox.SelectionChanged -= onSelectionChanged;
            if (!panel.Children.Contains(textBox))
            {
                panel.Children.Insert(Math.Min(index, panel.Children.Count), textBox);
            }
            textBox.Template = originalTemplate;
            textBox.ClearValue(NativeTextBox.TextProperty);
            if (originalBinding is not null)
            {
                textBox.SetBinding(NativeTextBox.TextProperty, originalBinding);
            }
            else
            {
                textBox.Text = originalText;
            }
            await settle();
        }
    }

    private static List<ObservedBinding> CaptureBindings(IEnumerable<Control> editors)
    {
        var result = new List<ObservedBinding>();
        foreach (var editor in editors)
        {
            result.Add(CaptureBinding(editor, editor switch
            {
                RibbonComboBox => RibbonComboBox.HeaderProperty,
                RibbonTextBox => RibbonTextBox.HeaderProperty,
                RibbonSpinner => RibbonSpinner.HeaderProperty,
                _ => throw new InvalidOperationException("Unknown header editor."),
            }));
            result.Add(CaptureBinding(editor, editor switch
            {
                Fluent.ComboBox => Fluent.ComboBox.HeaderTemplateProperty,
                Fluent.TextBox => Fluent.TextBox.HeaderTemplateProperty,
                RibbonComboBox => NativeComboBox.HeaderTemplateProperty,
                RibbonTextBox => RibbonTextBox.HeaderTemplateProperty,
                RibbonSpinner => RibbonControl.HeaderTemplateProperty,
                _ => throw new InvalidOperationException("Unknown header editor."),
            }));
            var selectorProperty = editor switch
            {
                Fluent.ComboBox => Fluent.ComboBox.HeaderTemplateSelectorProperty,
                Fluent.TextBox => Fluent.TextBox.HeaderTemplateSelectorProperty,
                RibbonTextBox => RibbonTextBox.HeaderTemplateSelectorProperty,
                RibbonSpinner => RibbonControl.HeaderTemplateSelectorProperty,
                _ => null,
            };
            if (selectorProperty is not null)
            {
                result.Add(CaptureBinding(editor, selectorProperty));
            }
        }
        return result;
    }

    private static ObservedBinding CaptureBinding(Control owner, DependencyProperty property)
    {
        var binding = owner.GetBindingExpression(property)?.ParentBinding;
        Require(binding is not null, owner, "has no live authored header binding.");
        return new ObservedBinding(owner, property, binding!);
    }

    private static void AssertBindings(IEnumerable<ObservedBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            Require(ReferenceEquals(binding.Owner.GetBindingExpression(binding.Property)?.ParentBinding, binding.Binding),
                binding.Owner, "replaced or cleared a consumer header binding.");
            Require(binding.Binding.UpdateSourceTrigger == UpdateSourceTrigger.Default,
                binding.Owner, "rewrote the consumer binding's update behavior.");
        }
    }

    private static void AssertFacadePresentationBindings(IEnumerable<Control> editors)
    {
        foreach (var editor in editors.Where(editor => editor is Fluent.ComboBox or Fluent.TextBox))
        {
            var binding = HeaderPresenter(editor).GetBindingExpression(ContentPresenter.ContentTemplateProperty)?.ParentBinding;
            Require(binding is not null && binding.Mode == BindingMode.OneWay
                    && binding.RelativeSource is null && ReferenceEquals(binding.Source, editor),
                editor, "retained its default TemplatedParent binding instead of the facade's effective header presentation binding.");
        }
    }

    private static void AssertSelectedTemplates(Control[] editors, PortEditorHeaderModel model, string tag)
    {
        foreach (var editor in editors)
        {
            if (HasSelector(editor))
            {
                AssertTemplate(editor, model, tag);
            }
            else
            {
                AssertPlainHeader(editor, model.Title);
            }
        }
    }

    private static bool HasSelector(Control editor) => editor is Fluent.ComboBox or RibbonTextBox or RibbonSpinner;

    private static void AssertAllTemplates(IEnumerable<Control> editors, PortEditorHeaderModel model, string tag)
    {
        foreach (var editor in editors)
        {
            AssertTemplate(editor, model, tag);
        }
    }

    private static void AssertTemplate(Control editor, PortEditorHeaderModel model, string tag)
    {
        var presenter = HeaderPresenter(editor);
        var labels = Descendants<TextBlock>(presenter).Where(text => IsHeaderTemplateTag(text.Tag)).ToArray();
        Require(ReferenceEquals(presenter.Content, model), editor, "converted the header model to text.");
        Require(labels.Length == 1 && Equals(labels[0].Tag, tag)
                && ReferenceEquals(labels[0].DataContext, model) && labels[0].Text == model.Title
                && IsVisible(labels[0]),
            editor, $"did not visibly realize {tag} with the current header model/title. "
            + $"Labels={string.Join("|", labels.Select(label => $"{label.Tag}:{label.Text}:{label.IsLoaded}:{label.ActualWidth}x{label.ActualHeight}"))}; "
            + $"template={presenter.ContentTemplate?.GetHashCode()}, model={model.Title}.");
    }

    private static void AssertPlainHeader(Control editor, string text)
    {
        var presenter = HeaderPresenter(editor);
        Require(!Descendants<TextBlock>(presenter).Any(label => IsHeaderTemplateTag(label.Tag)), editor,
            "retained a selected or explicit header template after it was cleared.");
        Require(Descendants<TextBlock>(presenter).Any(label => label.Text == text && label.FontSize == 12 && IsVisible(label)), editor,
            $"did not restore the ordinary header text '{text}'.");
    }

    private static bool IsHeaderTemplateTag(object? tag)
        => tag is string text && text.StartsWith("PortEditorHeader", StringComparison.Ordinal);

    private static void AssertLayout(Control editor)
    {
        var header = HeaderPresenter(editor);
        var input = Input(editor);
        var inputRoot = Find<FrameworkElement>(editor, "InputRoot");
        var glyph = Find<FontIcon>(editor, "GlyphIcon");
        Require(header.FontSize == 12 && header.Foreground is not null
                && header.VerticalAlignment == VerticalAlignment.Center
                && header.Margin.Equals(new Thickness(0, 0, 6, 0)),
            editor, "changed default header typography, alignment, or spacing.");
        Require(IsVisible(glyph) && glyph.Glyph == "\uE8D2", editor, "lost its existing icon layout.");
        // ComboBox owns InputWidth on its root; its native editable child can
        // remain unmeasured until editing begins.
        var measuredInput = editor is RibbonComboBox ? inputRoot : input;
        Require(measuredInput.ActualWidth > 0 && measuredInput.ActualHeight > 0
                && (editor is RibbonSpinner ? input.MinWidth == 140 : inputRoot.MinWidth == 140),
            editor, "changed the input-width contract while presenting the header. "
            + $"input={input.ActualWidth}x{input.ActualHeight}, visibility={input.Visibility}, "
            + $"inputMin={input.MinWidth}, rootMin={inputRoot.MinWidth}, root={inputRoot.ActualWidth}x{inputRoot.ActualHeight}.");
    }

    private static void AssertAutomation(Control editor, string text)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
        Require(peer is not null && peer.GetName() == text, editor, "lost its meaningful header-derived UIA name.");
        if (editor is RibbonComboBox)
        {
            var input = Input(editor);
            var label = AutomationProperties.GetLabeledBy(input) as FrameworkElement;
            Require(label is not null && !ReferenceEquals(label, HeaderPresenter(editor))
                    && Descendants<FrameworkElement>(HeaderPresenter(editor)).Any(element => ReferenceEquals(element, label)),
                editor, "lost EditableText's relationship to the actual rendered HeaderText label.");
            var labelPeer = label is null ? null : FrameworkElementAutomationPeer.CreatePeerForElement(label);
            Require(labelPeer is not null && labelPeer.GetName() == text, editor,
                "the actual header label did not expose its meaningful native peer name.");
            var inputPeer = FrameworkElementAutomationPeer.CreatePeerForElement(input);
            Require(inputPeer is not null && inputPeer.GetName() == text, editor,
                "lost the editable text part's meaningful UIA name. "
                + $"expected='{text}', peer='{inputPeer?.GetName()}', property='{AutomationProperties.GetName(input)}', "
                + $"label='{AutomationProperties.GetName(HeaderPresenter(editor))}', actualLabelPeer='{labelPeer?.GetName()}'.");
        }
    }

    private static bool FocusInput(NativeTextBox input)
    {
        var isTabStop = input.IsTabStop;
        try
        {
            input.IsTabStop = true;
            return input.Focus(FocusState.Programmatic);
        }
        finally
        {
            input.IsTabStop = isTabStop;
        }
    }

    private static ContentPresenter HeaderPresenter(Control editor) => Find<ContentPresenter>(editor, "HeaderText");

    private static NativeTextBox Input(Control editor)
        => Find<NativeTextBox>(editor, editor is RibbonComboBox ? "EditableText" : "PART_TextBox");

    private static T Find<T>(DependencyObject root, string name) where T : FrameworkElement
        => Descendants<T>(root).SingleOrDefault(element => element.Name == name)
           ?? throw new InvalidOperationException($"{root.GetType().Name} has no {name} {typeof(T).Name} template part.");

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

    private static bool IsVisible(FrameworkElement element)
    {
        if (!element.IsLoaded || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: Visibility.Collapsed })
            {
                return false;
            }
        }
        return true;
    }

    private static void AssertMetadata()
    {
#if WINDOWS
        var provider = Application.Current as Microsoft.UI.Xaml.Markup.IXamlMetadataProvider
                       ?? throw new InvalidOperationException("The native Showcase has no XAML metadata provider.");
        foreach (var type in new[]
                 {
                     typeof(RibbonComboBox), typeof(Fluent.ComboBox), typeof(RibbonTextBox), typeof(Fluent.TextBox),
                     typeof(RibbonSpinner), typeof(Fluent.Spinner), typeof(PortEditorHeaderFixture),
                     typeof(PortEditorHeaderTemplateSelector), typeof(PortEditorHeaderModel), typeof(PortEditorHeaderState),
                 })
        {
            var xamlType = provider.GetXamlType(type);
            Require(xamlType is not null && xamlType.UnderlyingType == type,
                $"The editor-header type {type.FullName} is not registered with its native XAML type.");
        }
#endif
    }

    private static void Require(bool condition, Control editor, string message)
        => Require(condition, $"{editor.GetType().Name} {message}");

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record ObservedBinding(Control Owner, DependencyProperty Property, Binding Binding);
}
