using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MMU.Ifosic.Models;
using OxyPlot;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using TechApps;
using TechApps.ViewModels;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class PlotViewModel : ViewModelBase
{
    [ObservableProperty] PlotModel _model = new();
    [ObservableProperty] PlotModel _model2 = new();
    [ObservableProperty] int _index;
    [ObservableProperty] private ObservableCollection<string> _indexes = new();

    public Project Project => Workspace.Instance.Project ?? new();
    private FrequencyShiftDistance? Item;

    public PlotViewModel()
    {
        if (Project.Runner.Sequences.Count == 0 || Project.Items.Count == Project.Runner.Sequences.Count)
            return;
        foreach (var sequence in Project.Runner.Sequences)
        {
            var fdd = FrequencyShiftDistance.LoadFolder(sequence.Path);
            if (fdd is not null)
                Project.Items.Add(fdd);
        }
        if (Project.Items.Count > 0)
            Item = Project.Items[Index];
        Plot();
    }

    partial void OnIndexChanged(int value)
    {
        Item = Project.Items[value];
        Plot();
    }

    [RelayCommand]
    private void LoadFile()
    {
        var file = WeakReferenceMessenger.Default.Send(new FileDialogMessage()).Response.FirstOrDefault();
        if (string.IsNullOrEmpty(file))
            return;
        var fdd = FrequencyShiftDistance.Load(file);
        if (fdd is null)
            return;
        // convert old data
        if (Path.GetExtension(file) == ".zip")
            fdd.Save(@$"C:\Projects\MMU\projects\{Path.GetFileNameWithoutExtension(file)}.bin");
        Item = fdd;
        Plot();
    }

    [RelayCommand]
    private void LoadFolder()
    {
        var path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
        if (string.IsNullOrEmpty(path))
            return;
        var fdd = FrequencyShiftDistance.LoadFolder(path);
        if (fdd is null)
            return;
        Item = fdd;
        Plot();
    }

    private void Plot()
    {
        if (Item is null)
            return;
        Model = Item.PlotHeatmap(max: 20);
    }
}
