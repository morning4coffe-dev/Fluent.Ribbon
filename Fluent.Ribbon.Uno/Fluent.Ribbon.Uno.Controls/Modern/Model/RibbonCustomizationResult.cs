namespace Fluent.Modern.Model;

/// <summary>
/// <para><b>Modern extension</b> — describes an observable ribbon customization warning or error.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonCustomizationIssue
{
    internal RibbonCustomizationIssue(string code, string message, bool isError, Exception? exception = null)
    {
        Code = code;
        Message = message;
        IsError = isError;
        Exception = exception;
    }

    /// <summary>
    /// Gets the stable machine-readable issue code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable issue message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets a value indicating whether the issue caused the operation to fail.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// Gets the underlying exception, when one was raised.
    /// </summary>
    public Exception? Exception { get; }
}

/// <summary>
/// <para><b>Modern extension</b> — returns a customization value together with observable warnings and errors.</para>
/// </summary>
/// <typeparam name="T">The operation value type.</typeparam>
[ModernExtension]
public sealed class RibbonCustomizationResult<T>
{
    internal RibbonCustomizationResult(T? value, IEnumerable<RibbonCustomizationIssue>? issues = null)
    {
        Value = value;
        Issues = issues?.ToArray() ?? Array.Empty<RibbonCustomizationIssue>();
    }

    /// <summary>
    /// Gets the operation value. A failed operation can expose a partial value for diagnostics.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets all warnings and errors reported by the operation.
    /// </summary>
    public IReadOnlyList<RibbonCustomizationIssue> Issues { get; }

    /// <summary>
    /// Gets a value indicating whether the operation completed without errors.
    /// </summary>
    public bool Succeeded => Issues.All(issue => !issue.IsError);
}
