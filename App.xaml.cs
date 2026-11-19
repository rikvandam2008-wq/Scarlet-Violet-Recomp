using System.Windows;

namespace Tandem;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, ev) =>
        {
            MessageBox.Show(ev.Exception.Message, "Tandem Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            ev.Handled = true;
        };
        base.OnStartup(e);
    }
}
