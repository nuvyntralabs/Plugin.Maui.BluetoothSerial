using Plugin.Maui.BluetoothSerial;

namespace Plugin.Maui.BluetoothSerial.Sample;

public partial class MainPage : ContentPage
{
    readonly Label log = new();
    IBluetoothSerialSession? session;

    public MainPage()
    {
        InitializeComponent();
        BluetoothSerial.Current.ConnectionStateChanged += (_, e) =>
            MainThread.BeginInvokeOnMainThread(() => log.Text = $"{e.State} {e.Message}\n{log.Text}");
        Root.Children.Add(new Button { Text = "Scan paired / loopback", Command = new Command(async () => await Scan()) });
        Root.Children.Add(new Button { Text = "Write hello", Command = new Command(async () => await Write()) });
        Root.Children.Add(new Button { Text = "Disconnect", Command = new Command(async () => { if (session is not null) await session.DisconnectAsync(); log.Text = "disconnected"; }) });
        Root.Children.Add(new Label { Text = "Android: lists bonded SPP devices. iOS: MFi only; otherwise NotSupported." });
        Root.Children.Add(log);
    }

    async Task Scan()
    {
        var devices = await BluetoothSerial.Current.ScanAsync(new SerialScanOptions { Duration = TimeSpan.FromSeconds(8) });
        log.Text = devices.Count == 0 ? "No devices" : string.Join("\n", devices.Select(d => $"{d.Name} {d.Address}"));
        if (devices.Count > 0)
            session = await BluetoothSerial.Current.ConnectAsync(devices[0]);
    }

    async Task Write()
    {
        if (session is null)
        {
            log.Text = "Connect first";
            return;
        }
        var payload = "Hello SPP\n"u8.ToArray();
        await session.WriteAsync(payload);
        var buffer = new byte[64];
        var n = await session.ReadAsync(buffer);
        log.Text = $"wrote {payload.Length} read {n}";
    }
}
