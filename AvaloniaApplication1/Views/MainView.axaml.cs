using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaApplication1.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        PlaybackSlider.PointerEntered += Slider_PointerEntered;
        PlaybackSlider.PointerExited += Slider_PointerExited;
    }

    private void Previous(object? sender, RoutedEventArgs e)
    {
        MusicPlayer.PlayPrevious();
    }

    private void Play(object? sender, RoutedEventArgs e)
    {
        MusicPlayer.TogglePause();
    }

    private void Next(object? sender, RoutedEventArgs e)
    {
        MusicPlayer.PlayNext();
    }

    private void SliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if(MainViewModel.Instance.SliderHeld)
        {
            MusicPlayer.playbackTime = TimeSpan.FromSeconds(PlaybackSlider.Value);
        }
    }

    private void Slider_PointerExited(object? sender, PointerEventArgs e)
    {
        MainViewModel.Instance.SliderHeld = false;
        MusicPlayer.Play();
    }

    private void Slider_PointerEntered(object? sender, PointerEventArgs e)
    {
        MainViewModel.Instance.SliderHeld = true;
        MusicPlayer.Stop();
    }
}
