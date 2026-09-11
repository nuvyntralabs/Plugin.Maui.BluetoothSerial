#if IOS
using ExternalAccessory;

namespace Plugin.Maui.BluetoothSerial;

sealed class PlatformBluetoothSerial : IBluetoothSerialPlatform
{
    public static IBluetoothSerialPlatform Create() => new PlatformBluetoothSerial();
    public bool IsSupported => EAAccessoryManager.SharedAccessoryManager.ConnectedAccessories.Length > 0;

    public Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions options, CancellationToken cancellationToken)
    {
        var devices = EAAccessoryManager.SharedAccessoryManager.ConnectedAccessories
            .Select(a => new SerialDevice { Id = a.SerialNumber ?? "", Name = a.Name ?? "", Address = a.SerialNumber ?? "" })
            .Where(d => string.IsNullOrWhiteSpace(options.NameContains) || d.Name.Contains(options.NameContains, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<SerialDevice>>(devices);
    }

    public Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, BluetoothSerialOptions options, CancellationToken cancellationToken)
    {
        var accessory = EAAccessoryManager.SharedAccessoryManager.ConnectedAccessories
            .FirstOrDefault(a => a.SerialNumber == device.Id || a.Name == device.Name);
        if (accessory is null || accessory.ProtocolStrings.Length == 0)
            throw new NotSupportedException("iOS classic SPP is not available. Only MFi External Accessory protocols registered in Info.plist are supported.");
        var session = new EASession(accessory, accessory.ProtocolStrings[0]);
        return Task.FromResult<IBluetoothSerialSession>(new MfiSession(session));
    }
}

sealed class MfiSession : IBluetoothSerialSession
{
    readonly EASession session;
    public MfiSession(EASession session) => this.session = session;
    public bool IsConnected => session.OutputStream is not null;
    public Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        session.OutputStream?.Write(data);
        return Task.CompletedTask;
    }
    public Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        var n = session.InputStream?.Read(buffer, 0, buffer.Length) ?? 0;
        return Task.FromResult(n);
    }
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
#endif
