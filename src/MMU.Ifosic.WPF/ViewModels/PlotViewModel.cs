using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechApps.ViewModels;
using TechApps;
using System.IO;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class PlotViewModel : ViewModelBase
{
    [ObservableProperty] PlotModel _model = new();

    public PlotViewModel()
    {
        LoadFile();
    }

    private void LoadFile()
    {
        var file = WeakReferenceMessenger.Default.Send(new FileDialogMessage()).Response.FirstOrDefault();
        if (string.IsNullOrEmpty(file))
            return;
        var fdd = FrequencyShiftDistance.Load(file);
        if (fdd is null)
            return;
        if (Path.GetExtension(file) == ".zip")
            fdd?.Save(@$"C:\Projects\MMU\projects\{Path.GetFileNameWithoutExtension(file)}.bin");
        Model = fdd.PlotHeatmap(max: 20);
    }

    private void LoadFolder()
    {
        var path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
        if (string.IsNullOrEmpty(path))
            return;
    }

}
