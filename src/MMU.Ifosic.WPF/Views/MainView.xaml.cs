﻿using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System.Linq;
using System;
using System.Windows;
using TechApps.Views;
using TechApps;
using Wpf.Ui.Controls;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.IO;

namespace MMU.Ifosic.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainView : UiWindow
{
    private readonly OpenFileDialog _dlgOpen = new();
    private readonly SaveFileDialog _dlgSave = new();
    private readonly System.Windows.Forms.FolderBrowserDialog _dlgFolder = new()
    {
        Description = "Select a folder",
        UseDescriptionForTitle = true,
        SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                    + Path.DirectorySeparatorChar,
        ShowNewFolderButton = true
    };

    public MainView()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<MainView, FileDialogMessage>(this, static (r, m) => r.ShowFileDialog(m));
        WeakReferenceMessenger.Default.Register<MainView, RequestMessage<string>>(this, static (r, m) => r.ShowFolderDialog(m));
        WeakReferenceMessenger.Default.Register<MainView, DialogMessage>(this, static (r, m) => r.ShowDialog(m));
    }

    private void ShowFolderDialog(RequestMessage<string> message)
    {
        var m = _dlgFolder.ShowDialog() != System.Windows.Forms.DialogResult.OK ? "" : _dlgFolder.SelectedPath;
        message.Reply(m);
    }

    private void ShowFileDialog(FileDialogMessage message)
    {
        FileDialog dlg = message.IsOpenDialog ? _dlgOpen : _dlgSave;
        dlg.Title = message.Title;
        dlg.Filter = message.Filter;
        if (message.IsOpenDialog)
            _dlgOpen.Multiselect = message.MultiSelect;
        var m = Array.Empty<string>();
        if (Application.Current.MainWindow is MainView actWin && dlg.ShowDialog(actWin) == true)
        {
            m = message.IsOpenDialog && message.MultiSelect ? _dlgOpen.FileNames : new string[] { dlg.FileName };
            message.FilterIndex = dlg.FilterIndex;
        }
        message.Reply(m);
    }

    private void ShowDialog(DialogMessage message)
    {
        var actWin = Application.Current.Windows
            .Cast<Window>()
            .Where(x => x is UiWindow)
            .FirstOrDefault(x => x.IsActive) ?? this;
        if (message.View is null)
        {
            var r = System.Windows.MessageBox.Show(actWin, message.Content, message.Caption, message.Button, message.Icon);
            message.Reply(r);
            return;
        }

        var dlg = new DialogWin
        {
            Owner = actWin,
            DataContext = message.View
        };
        if (message.IsNonBlocking)
        {
            dlg.Show();
            return;
        }

        try
        {
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(actWin, ex.Message);
        }
    }
}

