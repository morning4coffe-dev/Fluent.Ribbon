namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Input;
using Windows.System;

public partial class Spinner
{
    /// <inheritdoc />
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            return;
        }

        base.OnKeyUp(e);
    }

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren
    {
        get
        {
            foreach (var child in EnumerateBaseLogicalChildren())
            {
                yield return child;
            }

            if (MediumIcon is not null)
            {
                yield return MediumIcon;
            }
        }
    }

    private IEnumerable<object> EnumerateBaseLogicalChildren()
    {
        var enumerator = base.LogicalChildren;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is not null)
            {
                yield return enumerator.Current;
            }
        }
    }
}
