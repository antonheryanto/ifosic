using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechApps;

namespace MMU.Ifosic.WPF;

public enum AppPage
{
    Measurement,
    Calculate,
    ProjectNew,
    ProjectOpen,
    ProjectClose,
    ProjectSave,
    ProjectSaveAs,
    Home,
    Setting
}

public class AppCommand : AppRelayCommand<AppPage> { }
