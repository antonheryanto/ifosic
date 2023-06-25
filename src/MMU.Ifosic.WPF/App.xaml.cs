using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TechApps;

namespace MMU.Ifosic.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App() => DispatcherHelper.Initialize();
    public const string NAME = "DFOS";
    public const string DESCRIPTION = "Multi-Sensory Distributed Optical Fibre Sensor Cable for Exploration and Production Phase";

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Display the error message to developer
        MessageBox.Show(e.Exception.Message + "\n" + e.Exception.StackTrace);
        // Intercept the fatal error
        e.Handled = true;
        if (e.Exception is AggregateException errors)
        {
            var errorMessages = string.Join(Environment.NewLine, errors.InnerExceptions.Select(x => x.Message));
            MessageBox.Show(errorMessages, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            MessageBox.Show(e.Exception.StackTrace);
        }

    }
}
