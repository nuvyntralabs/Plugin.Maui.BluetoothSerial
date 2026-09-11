using Plugin.Maui.BluetoothSerial;

namespace Plugin.Maui.BluetoothSerial.Tests;

public sealed class BluetoothSerialTests
{
    [Fact]
    public async Task Loopback_write_read_and_filter()
    {
        var api = BluetoothSerial.Create();
        Assert.True(api.IsSupported);
        var none = await api.ScanAsync(new SerialScanOptions { NameContains = "Printer" });
        Assert.Empty(none);
        var devices = await api.ScanAsync(new SerialScanOptions { NameContains = "Loop" });
        Assert.Single(devices);
        await using var session = await api.ConnectAsync(devices[0]);
        Assert.True(session.IsConnected);
        await session.WriteAsync("hello"u8.ToArray());
        var buffer = new byte[8];
        var n = await session.ReadAsync(buffer);
        Assert.Equal(5, n);
        await session.DisconnectAsync();
        Assert.False(session.IsConnected);
    }

    [Fact]
    public async Task Connect_timeout_uses_options()
    {
        var platform = new HangPlatform();
        var api = new BluetoothSerialImplementation(new BluetoothSerialOptions { ConnectTimeout = TimeSpan.FromMilliseconds(50) }, platform);
        await Assert.ThrowsAnyAsync<Exception>(() => api.ConnectAsync(new SerialDevice { Id = "x", Name = "x" }));
    }
}

sealed class HangPlatform : IBluetoothSerialPlatform
{
    public bool IsSupported => true;
    public Task<IReadOnlyList<SerialDevice>> ScanAsync(SerialScanOptions options, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SerialDevice>>([]);
    public async Task<IBluetoothSerialSession> ConnectAsync(SerialDevice device, BluetoothSerialOptions options, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        return new LoopbackSession();
    }
}
