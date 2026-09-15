namespace Fluent;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;

/// <summary>Constrains an effective DP value while retaining the authored state for release.</summary>
internal sealed class EffectiveValueConstraint
{
    private readonly Control target;
    private readonly DependencyProperty property;
    private readonly string propertyName;
    private bool active;
    private bool applying;
#if WINDOWS
    private Storyboard? constraint;
    private Binding? originalBinding;
    private Binding? guardedBinding;
#endif
    private object? heldValue;

    internal EffectiveValueConstraint(Control target, DependencyProperty property, string propertyName)
    {
        this.target = target;
        this.property = property;
        this.propertyName = propertyName;
        target.RegisterPropertyChangedCallback(property, OnTargetChanged);
    }

    internal void Hold(object value)
    {
        if (active && Equals(heldValue, value) && (applying || Equals(target.GetValue(property), value)))
        {
            return;
        }

        active = true;
        heldValue = value;
        ApplyHeldValue();
    }

    private void OnTargetChanged(DependencyObject sender, DependencyProperty changedProperty)
    {
        if (active && !applying && !Equals(target.GetValue(property), heldValue))
        {
            ApplyHeldValue();
        }
    }

    private void ApplyHeldValue()
    {
#if WINDOWS
        WithSourceUpdatesSuppressed(() =>
        {
            constraint?.Stop();
            if (!active)
            {
                return;
            }
            var storyboard = CreateStoryboard(heldValue!);
            constraint = storyboard;
            storyboard.Begin();
            if (ReferenceEquals(constraint, storyboard))
            {
                // SkipToFill is deferred on WinUI; a locked DP must be coherent before SetValue returns.
                storyboard.SeekAlignedToLastTick(TimeSpan.Zero);
            }
        });
#else
        WithSourceUpdatesSuppressed(() =>
            DependencyObjectExtensions.SetValue(
                target, property, heldValue, DependencyPropertyValuePrecedences.Animations));
#endif
    }

#if WINDOWS
    private Storyboard CreateStoryboard(object value)
    {
        var animation = new ObjectAnimationUsingKeyFrames
        {
            Duration = new Duration(TimeSpan.Zero),
            FillBehavior = FillBehavior.HoldEnd,
            EnableDependentAnimation = true,
        };
        animation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = value });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyName);
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }
#endif

    internal void Release()
    {
        if (!active)
        {
            return;
        }

        active = false;
        heldValue = null;
#if WINDOWS
        var storyboard = constraint;
        constraint = null;
        var currentBinding = target.GetBindingExpression(property)?.ParentBinding;
        var desiredValue = target.GetAnimationBaseValue(property);
        var wasApplying = applying;
        applying = true;
        try
        {
            storyboard?.Stop();
            if (originalBinding is not null && ReferenceEquals(currentBinding, guardedBinding))
            {
                target.SetBinding(property, originalBinding);
                if (!Equals(target.GetAnimationBaseValue(property), desiredValue))
                {
                    target.SetValue(property, desiredValue);
                }
            }
        }
        finally
        {
            originalBinding = null;
            guardedBinding = null;
            applying = wasApplying;
        }
#else
        var desiredLocalValue = target.ReadLocalValue(property);
        var binding = target.GetBindingExpression(property)?.ParentBinding;
        var originalTrigger = binding?.UpdateSourceTrigger ?? UpdateSourceTrigger.Default;
        WithSourceUpdatesSuppressed(() =>
        {
            DependencyObjectExtensions.SetValue(
                target, property, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Animations);

            // Uno clears the binding when an animation value is removed. Restore the
            // original binding, not a replacement binding/default, and retain local intent.
            if (binding is not null && target.GetBindingExpression(property) is null)
            {
                target.SetBinding(property, binding);
                if (desiredLocalValue != DependencyProperty.UnsetValue
                    && !Equals(target.ReadLocalValue(property), desiredLocalValue))
                {
                    target.SetValue(property, desiredLocalValue);
                    if (binding.Mode == BindingMode.TwoWay
                        && originalTrigger is UpdateSourceTrigger.Default or UpdateSourceTrigger.PropertyChanged)
                    {
                        var restoredBinding = target.GetBindingExpression(property)
                                              ?? throw new InvalidOperationException("The constrained property's binding could not be restored.");
                        restoredBinding.UpdateSource(desiredLocalValue);
                    }
                }
            }
        });
#endif
    }

    private void WithSourceUpdatesSuppressed(Action action)
    {
#if WINDOWS
        var wasApplying = applying;
        applying = true;
        try
        {
            var binding = target.GetBindingExpression(property)?.ParentBinding;
            if (binding is { Mode: BindingMode.TwoWay }
                && binding.UpdateSourceTrigger != UpdateSourceTrigger.Explicit
                && !ReferenceEquals(binding, guardedBinding))
            {
                // WinUI freezes attached bindings, so guard updates with a temporary binding
                // and restore the original instance and latest authored value on release.
                originalBinding = binding;
                guardedBinding = CreateExplicitBinding(binding);
                target.SetBinding(property, guardedBinding);
            }

            action();
        }
        finally
        {
            applying = wasApplying;
        }
#else
        var binding = target.GetBindingExpression(property)?.ParentBinding;
        var trigger = binding?.UpdateSourceTrigger ?? UpdateSourceTrigger.Default;
        var wasApplying = applying;
        applying = true;
        var triggerChanged = false;
        try
        {
            if (binding is not null && trigger != UpdateSourceTrigger.Explicit)
            {
                binding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                triggerChanged = true;
            }

            action();
        }
        finally
        {
            if (triggerChanged)
            {
                binding!.UpdateSourceTrigger = trigger;
            }
            applying = wasApplying;
        }
#endif
    }

#if WINDOWS
    private static Binding CreateExplicitBinding(Binding source)
    {
        var binding = new Binding
        {
            Path = source.Path,
            Mode = source.Mode,
            UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
            Converter = source.Converter,
            ConverterParameter = source.ConverterParameter,
            ConverterLanguage = source.ConverterLanguage,
        };
        if (!string.IsNullOrEmpty(source.ElementName))
        {
            binding.ElementName = source.ElementName;
        }
        else if (source.RelativeSource is not null)
        {
            binding.RelativeSource = source.RelativeSource;
        }
        else if (source.Source is not null)
        {
            binding.Source = source.Source;
        }

        if (source.FallbackValue != DependencyProperty.UnsetValue)
        {
            binding.FallbackValue = source.FallbackValue;
        }

        if (source.TargetNullValue != DependencyProperty.UnsetValue)
        {
            binding.TargetNullValue = source.TargetNullValue;
        }

        return binding;
    }
#endif
}
