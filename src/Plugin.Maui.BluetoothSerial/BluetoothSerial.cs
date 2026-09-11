namespace Plugin.Maui.BluetoothSerial;

public static class BluetoothSerialUuids
{
    public const string SerialPort = "00001101-0000-1000-8000-00805F9B34FB";
}

public sealed class BluetoothSerialOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public string DefaultUuid { get; set; } = BluetoothSerialUuids.SerialPort;
    public int MaxReconnectAttempts { get; set; }
}

public sealed class SerialDevice
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Address { get; init; } = "";
}

public sealed class SerialScanOptions
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);
    public string? NameContains { get; set; }
}

public enum SerialConnectionState { Disconnected, Connecting, Connected, Failed }

public sealed class SerialConnectionEventArgs : EventArgs
{
    public SerialConnectionState State { get; init; }
    public string? Message { get; init; }
}

public interface IBluetoothSerialSession : IAsyncDisposable
{
    bool IsConnected { get; }
    Task WriteAsync(byte[] data, CancellationToken cancellationToken = default);
    Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

public interface IBluetoothSerialPlatform
{
    bool IsSupported { get; }
    Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions options, CancellationToken cancellationToken);
    Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, BluetoothSerialOptions options, CancellationToken cancellationToken);
}

public interface IBluetoothSerial
{
    bool IsSupported { get; }
    bool IsConnected { get; }
    event EventHandler<SerialConnectionEventArgs>? ConnectionStateChanged;
    Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions? options = null, CancellationToken cancellationToken = default);
    Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, CancellationToken cancellationToken = default);
}

public static class BluetoothSerial
{
    static IBluetoothSerial? current;
    public static IBluetoothSerial Current =>
        current ?? throw new InvalidOperationException("BluetoothSerial is not initialized. Call builder.UseBluetoothSerial().");
    public static void SetDefault(IBluetoothSerial implementation) =>
        current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    public static IBluetoothSerial Create(BluetoothSerialOptions? options = null, IBluetoothSerialPlatform? platform = null)
    {
        var instance = new BluetoothSerialImplementation(options ?? new BluetoothSerialOptions(), platform ?? PlatformBluetoothSerial.Create());
        SetDefault(instance);
        return instance;
    }
}

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseBluetoothSerial(this MauiAppBuilder builder, Action<BluetoothSerialOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var options = new BluetoothSerialOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(BluetoothSerial.Create(options));
        return builder;
    }
}

sealed class BluetoothSerialImplementation : IBluetoothSerial
{
    readonly BluetoothSerialOptions options;
    readonly IBluetoothSerialPlatform platform;
    IBluetoothSerialSession? session;

    public BluetoothSerialImplementation(BluetoothSerialOptions options, IBluetoothSerialPlatform platform)
    {
        this.options = options;
        this.platform = platform;
    }

    public bool IsSupported => platform.IsSupported;
    public bool IsConnected => session?.IsConnected == true;
    public event EventHandler<SerialConnectionEventArgs>? ConnectionStateChanged;

    public Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions? options = null, CancellationToken cancellationToken = default) =>
        platform.ScanAsync(options ?? new SerialScanOptions(), cancellationToken);

    public async Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, CancellationToken cancellationToken = default)
    {
        ConnectionStateChanged?.Invoke(this, new SerialConnectionEventArgs { State = SerialConnectionState.Connecting });
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.ConnectTimeout);
            session = await platform.ConnectAsync(device, options, cts.Token).ConfigureAwait(false);
            ConnectionStateChanged?.Invoke(this, new SerialConnectionEventArgs { State = SerialConnectionState.Connected });
            return session;
        }
        catch (Exception ex)
        {
            ConnectionStateChanged?.Invoke(this, new SerialConnectionEventArgs { State = SerialConnectionState.Failed, Message = ex.Message });
            throw;
        }
    }
}

sealed class LoopbackSession : IBluetoothSerialSession
{
    readonly Queue<byte> buffer = new();
    public bool IsConnected { get; private set; } = true;
    public Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        foreach (var b in data)
            buffer.Enqueue(b);
        return Task.CompletedTask;
    }
    public Task<int> ReadAsync(byte[] destination, CancellationToken cancellationToken = default)
    {
        var n = 0;
        while (n < destination.Length && buffer.Count > 0)
            destination[n++] = buffer.Dequeue();
        return Task.FromResult(n);
    }
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }
    public ValueTask DisposeAsync() => new(DisconnectAsync());
}

#if !ANDROID && !IOS
sealed class PlatformBluetoothSerial : IBluetoothSerialPlatform
{
    public static IBluetoothSerialPlatform Create() => new PlatformBluetoothSerial();
    public bool IsSupported => true;
    public Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions options, CancellationToken cancellationToken)
    {
        IReadOnlyList<SerialDevice> devices = [new SerialDevice { Id = "loopback", Name = "Loopback", Address = "00:00:00:00:00:00" }];
        if (!string.IsNullOrWhiteSpace(options.NameContains) &&
            !devices[0].Name.Contains(options.NameContains, StringComparison.OrdinalIgnoreCase))
            devices = [];
        return Task.FromResult(devices);
    }
    public Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, BluetoothSerialOptions options, CancellationToken cancellationToken) =>
        Task.FromResult<IBluetoothSerialSession>(new LoopbackSession());
}
#endif
