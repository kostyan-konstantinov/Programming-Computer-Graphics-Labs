using System.Windows;

namespace Lab5.App;
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show(e.Exception.Message,
                "lab5", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
