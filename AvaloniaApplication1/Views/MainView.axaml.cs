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
        //to prevent this from being called when the MusicPlayer changed the slider value
        if(PlaybackSlider.Value != MainViewModel.Instance.PlaybackTime)
        {
            MusicPlayer.playbackTime = TimeSpan.FromSeconds(PlaybackSlider.Value);
            MusicPlayer.Play();
        }
    }

    private void Slider_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is Slider slider)
        {
            // Find the Track
            var track = slider.GetTemplateChildren().OfType<Track>().FirstOrDefault();

            if (track?.Thumb != null)
            {
                track.Thumb.DragStarted += (_, __) => MainViewModel.Instance.SliderHeld = true;
                track.Thumb.DragCompleted += (_, __) => MainViewModel.Instance.SliderHeld = false;
            }
        }
    }
}
