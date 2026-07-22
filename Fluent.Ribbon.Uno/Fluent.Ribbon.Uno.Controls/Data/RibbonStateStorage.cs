namespace Fluent;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;

/// <summary>
/// Handles loading and saving portable ribbon state.
/// </summary>
/// <remarks>
/// Persistent state uses <see cref="ApplicationData.LocalSettings"/>. The protected
/// isolated-storage members remain available for WPF API compatibility only.
/// </remarks>
public class RibbonStateStorage : IRibbonStateStorage
{
    private const string StorageContainerName = "FluentRibbon";
    private const string IsMinimizedKey = "IsMinimized";
    private const string ShowQatBelowKey = "ShowQuickAccessToolBarBelowRibbon";
    private const string IsSimplifiedKey = "IsSimplified";

    private readonly Ribbon? _ribbon;
    private readonly Stream _memoryStream;
    private string? _isolatedStorageFileName;
    private bool _isMinimized;
    private bool _showQuickAccessToolBarBelowRibbon;
    private bool _isSimplified;

    /// <summary>Creates a standalone state container.</summary>
    public RibbonStateStorage()
    {
        _memoryStream = new MemoryStream();
    }

    /// <summary>Creates state storage for <paramref name="ribbon"/>.</summary>
    public RibbonStateStorage(Ribbon ribbon)
        : this()
    {
        _ribbon = ribbon ?? throw new ArgumentNullException(nameof(ribbon));
    }

    /// <summary>Finalizes the state storage.</summary>
    ~RibbonStateStorage()
    {
        Dispose(false);
    }

    /// <summary>Occurs when a platform storage capability prevents a void operation from completing.</summary>
    public event EventHandler<RibbonStateStorageErrorEventArgs>? StorageError;

    /// <summary>Gets whether this instance has been disposed.</summary>
    protected bool Disposed { get; private set; }

    /// <inheritdoc />
    public bool IsLoading { get; private set; }

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <summary>Gets or sets whether the ribbon is minimized.</summary>
    public bool IsMinimized
    {
        get => _ribbon?.IsMinimized ?? _isMinimized;
        set
        {
            _isMinimized = value;
            if (_ribbon is not null && (_ribbon.CanMinimize || !value))
            {
                _ribbon.IsMinimized = value;
            }
        }
    }

    /// <summary>Gets or sets whether the quick access toolbar is below the ribbon.</summary>
    public bool ShowQuickAccessToolBarBelowRibbon
    {
        get => _ribbon is not null
            ? !_ribbon.ShowQuickAccessToolBarAboveRibbon
            : _showQuickAccessToolBarBelowRibbon;
        set
        {
            _showQuickAccessToolBarBelowRibbon = value;
            if (_ribbon is not null)
            {
                _ribbon.ShowQuickAccessToolBarAboveRibbon = !value;
            }
        }
    }

    /// <summary>Gets or sets whether simplified mode is active.</summary>
    public bool IsSimplified
    {
        get => _ribbon?.IsSimplified ?? _isSimplified;
        set
        {
            _isSimplified = value;
            if (_ribbon is not null && (_ribbon.CanUseSimplified || !value))
            {
                _ribbon.IsSimplified = value;
            }
        }
    }

    /// <summary>Gets the WPF-compatible state file name.</summary>
    protected string IsolatedStorageFileName
    {
        get
        {
            if (_isolatedStorageFileName is not null)
            {
                return _isolatedStorageFileName;
            }

            var identity = _ribbon is null
                ? "Standalone"
                : $"{_ribbon.GetType().FullName}.{_ribbon.Name}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
            _isolatedStorageFileName =
                $"Fluent.Ribbon.State.{Convert.ToHexString(hash.AsSpan(0, 4))}";
            return _isolatedStorageFileName;
        }
    }

    /// <inheritdoc />
    public virtual void SaveTemporary()
    {
        ThrowIfDisposed();
        _memoryStream.Position = 0;
        _memoryStream.SetLength(0);
        Save(_memoryStream);
        _memoryStream.Position = 0;
    }

    /// <inheritdoc />
    public virtual void Save()
    {
        ThrowIfDisposed();

        if (_ribbon is { AutomaticStateManagement: false })
        {
            return;
        }

        if (!IsLoaded)
        {
            return;
        }

        ExecutePersistentOperation(
            RibbonStateStorageOperation.Save,
            () =>
            {
                var values = ApplicationData.Current.LocalSettings.Values;
                var serialized = CreateStateData().ToString();

                values[GetSettingsKey(IsolatedStorageFileName)] = serialized;

                // Preserve the original Uno keys for applications that already consumed them.
                values[GetSettingsKey(IsMinimizedKey)] = IsMinimized;
                values[GetSettingsKey(ShowQatBelowKey)] = ShowQuickAccessToolBarBelowRibbon;
                values[GetSettingsKey(IsSimplifiedKey)] = IsSimplified;
            });
    }

    /// <summary>Saves state to <paramref name="stream"/>.</summary>
    protected virtual void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
        writer.Write(CreateStateData().ToString());
        writer.Flush();
    }

    /// <summary>Creates the serialized ribbon state.</summary>
    protected virtual StringBuilder CreateStateData()
    {
        var builder = new StringBuilder();
        builder.Append(IsMinimized.ToString(CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append((!ShowQuickAccessToolBarBelowRibbon).ToString(CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(IsSimplified.ToString(CultureInfo.InvariantCulture));
        return builder;
    }

    /// <inheritdoc />
    public virtual void LoadTemporary()
    {
        ThrowIfDisposed();
        _memoryStream.Position = 0;
        Load(_memoryStream);
    }

    /// <inheritdoc />
    public virtual void Load()
    {
        ThrowIfDisposed();
        IsLoading = true;

        try
        {
            if (_ribbon is { AutomaticStateManagement: false })
            {
                return;
            }

            ExecutePersistentOperation(
                RibbonStateStorageOperation.Load,
                () =>
                {
                    var values = ApplicationData.Current.LocalSettings.Values;

                    if (values.TryGetValue(GetSettingsKey(IsolatedStorageFileName), out var serialized)
                        && serialized is string state)
                    {
                        LoadState(state);
                        CopyStateToTemporaryStorage(state);
                        return;
                    }

                    LoadLegacyValues(values);
                    SaveTemporary();
                });
        }
        finally
        {
            IsLoaded = true;
            IsLoading = false;
        }
    }

    /// <summary>Loads state from <paramref name="stream"/>.</summary>
    protected virtual void Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        IsLoading = true;
        try
        {
            LoadStateCore(stream);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Loads serialized state from <paramref name="stream"/>.</summary>
    protected virtual void LoadStateCore(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        LoadState(reader.ReadToEnd());
    }

    /// <summary>Loads serialized state from <paramref name="data"/>.</summary>
    protected virtual void LoadState(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return;
        }

        var values = data.Split(',');
        if (values.Length > 0 && bool.TryParse(values[0], out var isMinimized))
        {
            IsMinimized = isMinimized;
        }

        if (values.Length > 1 && bool.TryParse(values[1], out var showAbove))
        {
            ShowQuickAccessToolBarBelowRibbon = !showAbove;
        }

        if (values.Length > 2 && bool.TryParse(values[2], out var isSimplified))
        {
            IsSimplified = isSimplified;
        }
    }

    /// <summary>Determines whether a WPF-compatible isolated-storage file exists.</summary>
    protected static bool IsolatedStorageFileExists(
        IsolatedStorageFile storage,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        return storage.GetFileNames(fileName).Length != 0;
    }

    /// <summary>Gets WPF-compatible isolated storage when the platform supports it.</summary>
    protected static IsolatedStorageFile GetIsolatedStorageFile()
    {
        try
        {
            return IsolatedStorageFile.GetUserStoreForDomain();
        }
        catch (IsolatedStorageException)
        {
            return IsolatedStorageFile.GetUserStoreForAssembly();
        }
        catch (PlatformNotSupportedException)
        {
            return IsolatedStorageFile.GetUserStoreForAssembly();
        }
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        ThrowIfDisposed();

        IsMinimized = false;
        ShowQuickAccessToolBarBelowRibbon = false;
        IsSimplified = false;

        ExecutePersistentOperation(
            RibbonStateStorageOperation.Reset,
            () =>
            {
                var values = ApplicationData.Current.LocalSettings.Values;
                values.Remove(GetSettingsKey(IsolatedStorageFileName));
                values.Remove(GetSettingsKey(IsMinimizedKey));
                values.Remove(GetSettingsKey(ShowQatBelowKey));
                values.Remove(GetSettingsKey(IsSimplifiedKey));
            });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases resources owned by this instance.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing)
        {
            _memoryStream.Dispose();
        }

        Disposed = true;
    }

    private void LoadLegacyValues(IDictionary<string, object> values)
    {
        if (values.TryGetValue(GetSettingsKey(IsMinimizedKey), out var minimized)
            && minimized is bool isMinimized)
        {
            IsMinimized = isMinimized;
        }

        if (values.TryGetValue(GetSettingsKey(ShowQatBelowKey), out var qatBelow)
            && qatBelow is bool showBelow)
        {
            ShowQuickAccessToolBarBelowRibbon = showBelow;
        }

        if (values.TryGetValue(GetSettingsKey(IsSimplifiedKey), out var simplified)
            && simplified is bool isSimplified)
        {
            IsSimplified = isSimplified;
        }

    }

    private static string GetSettingsKey(string key) => $"{StorageContainerName}.{key}";

    private void CopyStateToTemporaryStorage(string state)
    {
        _memoryStream.Position = 0;
        _memoryStream.SetLength(0);
        using var writer = new StreamWriter(_memoryStream, Encoding.UTF8, 1024, leaveOpen: true);
        writer.Write(state);
        writer.Flush();
        _memoryStream.Position = 0;
    }

    private void ExecutePersistentOperation(
        RibbonStateStorageOperation operation,
        Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (IsStorageCapabilityException(exception))
        {
            Trace.WriteLine(
                $"Ribbon state {operation} is unavailable on this platform: {exception}");
            StorageError?.Invoke(
                this,
                new RibbonStateStorageErrorEventArgs(operation, exception));
        }
    }

    private static bool IsStorageCapabilityException(Exception exception) =>
        exception is InvalidOperationException
            or NotSupportedException
            or PlatformNotSupportedException
            or UnauthorizedAccessException
            or COMException;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
    }
}
