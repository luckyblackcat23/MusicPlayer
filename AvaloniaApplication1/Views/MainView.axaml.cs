using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication1.ViewModels;
using System;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
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

    private void SliderValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        Slider slider = sender as Slider;
        if(slider.Value != MainViewModel.Instance.PlaybackTime)
        {
            MusicPlayer.playbackTime = TimeSpan.FromSeconds(slider.Value);
            MusicPlayer.Play();
        }
    }
}
