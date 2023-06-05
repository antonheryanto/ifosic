using System.Windows;
using TechApps;

namespace MMU.Ifosic.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App() => DispatcherHelper.Initialize();
    public const string NAME = "MMU.Ifosic";
    public const string DESCRIPTION = "DFOS Fiber";
}
