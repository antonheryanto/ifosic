using CommunityToolkit.Mvvm.ComponentModel;
using MMU.Ifosic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMU.Ifosic.WPF;

public partial class Workspace : ObservableObject
{
    // http://csharpindepth.com/Articles/General/Singleton.aspx
    private static readonly Lazy<Workspace> _lazy = new(() => new());
    public static Workspace Instance => _lazy.Value;

    [NotifyPropertyChangedFor(nameof(HasProject))]
    [ObservableProperty] private Project? _project;

    public bool HasProject => Project is not null;

    private Workspace() { }

    public static readonly string TYPES = string.Join("|", new Dictionary<string, string> {
            {"DFOS Project File", ".dfos"},
            {"All Files", "*"},
        }.Select(s => $"{s.Key} (*.{s.Value})|*.{s.Value}"));
}

