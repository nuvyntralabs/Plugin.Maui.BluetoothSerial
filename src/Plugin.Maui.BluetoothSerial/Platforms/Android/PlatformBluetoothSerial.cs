#if ANDROID
using Android.Bluetooth;
using Java.Util;

namespace Plugin.Maui.BluetoothSerial;

sealed class PlatformBluetoothSerial : IBluetoothSerialPlatform
{
    public static IBluetoothSerialPlatform Create() => new PlatformBluetoothSerial();
    public bool IsSupported => BluetoothAdapter.DefaultAdapter?.IsEnabled == true;

    public Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions options, CancellationToken cancellationToken)
    {
        var adapter = BluetoothAdapter.DefaultAdapter;
        if (adapter is null)
            return Task.FromResult<IReadOnlyList<SerialDevice>>([]);
        var devices = adapter.BondedDevices?
            .Select(d => new SerialDevice { Id = d.Address ?? "", Name = d.Name ?? "", Address = d.Address ?? "" })
            .Where(d => string.IsNullOrWhiteSpace(options.NameContains) || d.Name.Contains(options.NameContains, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
        return Task.FromResult<IReadOnlyList<SerialDevice>>(devices);
    }

    public async Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, BluetoothSerialOptions options, CancellationToken cancellationToken)
    {
        var adapter = BluetoothAdapter.DefaultAdapter ?? throw new InvalidOperationException("Bluetooth adapter is missing.");
        var remote = adapter.GetRemoteDevice(device.Address);
        var uuid = UUID.FromString(options.DefaultUuid)!;
        var socket = remote.CreateRfcommSocketToServiceRecord(uuid);
        await Task.Run(() => socket.Connect(), cancellationToken).ConfigureAwait(false);
        return new AndroidSerialSession(socket);
    }
}

sealed class AndroidSerialSession : IBluetoothSerialSession
{
    readonly BluetoothSocket socket;
    public AndroidSerialSession(BluetoothSocket socket) => this.socket = socket;
    public bool IsConnected => socket.IsConnected;
    public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default) =>
        await socket.OutputStream!.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    public async Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken = default) =>
        await socket.InputStream!.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        socket.Close();
        return Task.CompletedTask;
    }
    public ValueTask DisposeAsync() => new(DisconnectAsync());
}
#endif
