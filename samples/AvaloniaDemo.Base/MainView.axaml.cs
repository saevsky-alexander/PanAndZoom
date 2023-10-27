using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaDemo;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        ZoomBorder1 = this.Find<ZoomBorder>("ZoomBorder1");
        if (ZoomBorder1 != null)
        {
            ZoomBorder1.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder1.ZoomChanged += ZoomBorder_ZoomChanged;
        }

        ZoomBorder2 = this.Find<ZoomBorder>("ZoomBorder2");
        if (ZoomBorder2 != null)
        {
            ZoomBorder2.KeyDown += ZoomBorder_KeyDown;
            ZoomBorder2.ZoomChanged += ZoomBorder_ZoomChanged;
        }
        ShowR1.Click += ShowR1_Click;
        ShowT1.Click += ShowT1_Click;
        ShowT2.Click += ShowT2_Click;

        DataContext = ZoomBorder1;
    }

    private void ShowT1_Click(object sender, RoutedEventArgs e)
    {
        T1.BringIntoView();
    }
    private void ShowT2_Click(object sender, RoutedEventArgs e)
    {
        T2.BringIntoView();
    }

    private void ShowR1_Click(object sender, RoutedEventArgs e)
    {
        R1.BringIntoView();
    }

    private void ZoomBorder_KeyDown(object? sender, KeyEventArgs e)
    {
        var zoomBorder = this.DataContext as ZoomBorder;
            
        switch (e.Key)
        {
            case Key.F:
                zoomBorder?.Fill();
                break;
            case Key.U:
                zoomBorder?.Uniform();
                break;
            case Key.R:
                zoomBorder?.ResetMatrix();
                break;
            case Key.T:
                zoomBorder?.ToggleStretchMode();
                zoomBorder?.AutoFit();
                break;
        }
    }

    private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
    {
        Debug.WriteLine($"[ZoomChanged] {e.ZoomX} {e.ZoomY} {e.OffsetX} {e.OffsetY}");
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl)
        {
            if (tabControl.SelectedItem is TabItem tabItem)
            {
                if (tabItem.Tag is string tag)
                {
                    if (tag == "1")
                    {
                        DataContext = ZoomBorder1;
                    }
                    else if (tag == "2")
                    {
                        DataContext = ZoomBorder2;
                    }
                }
            }
        }
    }
}

