using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MMU.Ifosic.Models;
using NPOI.HPSF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TechApps;
using TechApps.ViewModels;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private static readonly ViewModelBase _home = new FrontPageViewModel { Name = App.NAME, Description = App.DESCRIPTION };
    [ObservableProperty] private ViewModelBase _content = _home;
    [ObservableProperty] private AssetItem _assets = new();
    public Workspace Workspace => Workspace.Instance;

    public MainViewModel()
    {
        IsActive = true;
    }

    protected override void OnActivated()
    {
        Messenger.Register<MainViewModel, ValueChangedMessage<ViewModelBase>>(this, static (r, m) => r.Content = m.Value);
        Messenger.Register<MainViewModel, ValueChangedMessage<AppPage>>(this, static (r, m) => r.AppAction(m.Value));
        Messenger.Register<MainViewModel, ValueChangedMessage<(AppPage Page, object Data)>>(this, static (r, m) => r.AppAction(m.Value.Page, m.Value.Data));
        Messenger.Register<MainViewModel, ValueChangedMessage<(string Key, AssetItem Value)>>(this, static (r, m) => r.AddAsset(m.Value.Key, m.Value.Value));
    }

    private void AppAction(AppPage page, object? data = null)
    {
        ViewModelBase? vm = null;
        switch (page)
        {
            case AppPage.ProjectNew:
                Workspace.Instance.Project = new();
                vm = new ProjectViewModel();
                break;
            case AppPage.ProjectOpen:
                ProjectOpen();
                break;
            case AppPage.ProjectClose:
                ProjectClose();
                break;
            case AppPage.ProjectSave:
                ProjectSave();
                break;
            case AppPage.ProjectSaveAs:
                break;
            case AppPage.Setting:
                vm = new SettingViewModel();
                break;
            default:
                vm = new FrontPageViewModel();
                break;
        }
        if (vm is not null && !vm.IsCanceled)
            Content = vm;
    }

    private void ProjectSave()
    {
        var fileName = Messenger.Send(new FileDialogMessage { IsOpenDialog = false }).Response.FirstOrDefault();
        if (fileName is null)
            return;
        Workspace.Instance.Project?.Save(fileName);
    }

    private void ProjectClose()
    {
        Content = new FrontPageViewModel();
        Assets = new();
        Workspace.Instance.Project = null;
    }

    private void ProjectOpen(string? fileName = null)
    {
        fileName ??= Messenger.Send(new FileDialogMessage()).Response.FirstOrDefault();
        if (string.IsNullOrEmpty(fileName))
            return;
        ProgressViewModel.Init(() => Workspace.Instance.Project = Project.Load(fileName), () => Content = new ProjectViewModel());
    }


    void AddAsset(string key, AssetItem value)
    {
        if (Assets.Children.Count == 0)
            InitAssets();
        if (Assets.Children.FirstOrDefault(w => w.Name == key) is not AssetItem a)
            return;
        a.Children.Add(value);
    }

    void InitAssets()
    {
        if (Assets.Children.Count > 0)
            return;

        Assets.Children.Add(new AssetItem
        {
            Name = "Item",
            Glyph = IAssetItem.GetGlyph("folder.png"),
            ActionComands = new() {
                (AssetActions.Load, static () => Debug.WriteLine("Item")),
            },
        });

    }
}
