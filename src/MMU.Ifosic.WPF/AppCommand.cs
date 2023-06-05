using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechApps;

namespace MMU.Ifosic.WPF;

public enum AppPage
{
    ProjectNew,
    ProjectOpen,
    ProjectClose,
    ProjectSave,
    ProjectSaveAs,
    Calculate,
    Home,
    Seismic,
    Setting
}

public class AppCommand : AppRelayCommand<AppPage> { }
