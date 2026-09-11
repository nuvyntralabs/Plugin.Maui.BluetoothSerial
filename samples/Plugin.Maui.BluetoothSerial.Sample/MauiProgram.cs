using Microsoft.Extensions.Logging;
using Plugin.Maui.BluetoothSerial;

namespace Plugin.Maui.BluetoothSerial.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();
        builder.UseMauiApp<App>()
            .UseBluetoothSerial(o => o.ConnectTimeout = TimeSpan.FromSeconds(10));
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
