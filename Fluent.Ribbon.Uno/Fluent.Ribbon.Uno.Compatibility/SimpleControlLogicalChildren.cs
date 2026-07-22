namespace Fluent;

using System.Collections;

public partial class CheckBox
{
    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren => EnumerateLogicalChildren().GetEnumerator();

    private IEnumerable<object> EnumerateLogicalChildren()
    {
        if (Icon is not null)
        {
            yield return Icon;
        }

        if (Header is not null)
        {
            yield return Header;
        }
    }
}

public partial class RadioButton
{
    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren => EnumerateLogicalChildren().GetEnumerator();

    private IEnumerable<object> EnumerateLogicalChildren()
    {
        if (Icon is not null)
        {
            yield return Icon;
        }

        if (Header is not null)
        {
            yield return Header;
        }
    }
}

public partial class ToggleButton
{
    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren => EnumerateLogicalChildren().GetEnumerator();

    private IEnumerable<object> EnumerateLogicalChildren()
    {
        if (Icon is not null)
        {
            yield return Icon;
        }

        if (Header is not null)
        {
            yield return Header;
        }
    }
}
