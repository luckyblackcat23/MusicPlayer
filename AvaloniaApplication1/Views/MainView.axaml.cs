using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using AvaloniaApplication1.ViewModels;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    public static MainView instance;

    public MainView()
    {
        instance = this;
       
        InitializeComponent();
        PlaybackSlider.AddHandler(PointerPressedEvent, Slider_PointerPressed, RoutingStrategies.Tunnel);
        PlaybackSlider.AddHandler(PointerReleasedEvent, Slider_PointerReleased, RoutingStrategies.Tunnel);
    }

    private void PreviousButton(object? sender, RoutedEventArgs e)
    {
        MusicPlayer.PlayPrevious();
    }

    private void PlayButton(object? sender, RoutedEventArgs e)
    {
        MusicPlayer.TogglePause();
    }

    private void NextButton(object? sender, RoutedEventArgs e)
    {
        MusicPlayer.PlayNext();
    }

    private void OpenFileLocationButton(object? sender, RoutedEventArgs e)
    {
        OpenFileLocation(MusicPlayer.musicQueue[MusicPlayer.currentSongIndex]);
    }

    void OpenFileLocation(string path)
    {
        if (File.Exists(path))
        {
            Process.Start("explorer.exe", "/select, " + path);
        }
    }

    private void SliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if(MainViewModel.Instance.SliderHeld)
        {
            MusicPlayer.playbackTime = TimeSpan.FromSeconds(PlaybackSlider.Value);
        }
    }

    private void Slider_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        MainViewModel.Instance.SliderHeld = true;
    }

    private void Slider_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        MainViewModel.Instance.SliderHeld = false;
        MusicPlayer.Play();
    }

    private void OnVolumeChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        MusicPlayer.volume = (float)e.NewValue;
    }

    private void DataGrid_LoadingRow(object? sender, Avalonia.Controls.DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is Song song)
        {
            TagLib.File tfile = TagLib.File.Create(song.SongPath);

            song.Title = tfile.Tag.Title;

            song.Artist = tfile.Tag.FirstPerformer;

            song.Album = tfile.Tag.Album;

            song.Year = tfile.Tag.Year.ToString();

            //maybe change to the list of genres later or something
            song.Genre = tfile.Tag.JoinedGenres;

            //i should find a way to do this with tfile so i can avoid having to create a FileReader
            var songFile = new NAudio.Wave.AudioFileReader(song.SongPath);
            song.Duration = songFile.TotalTime.TotalSeconds;
            songFile.Dispose();

            //get album image
            if (tfile.Tag.Pictures.Length > 0)
            {
                MemoryStream ms;
                ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                try
                {
                    song.AlbumCover = new Bitmap(ms).CreateScaledBitmap(new Avalonia.PixelSize(124, 124)); ;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    song.AlbumCover = MainViewModel.Instance.LoadDefaultAlbumImage();
                }

                ms.Dispose();
            }
            else
            {
                //Debug.WriteLine("file does not contain album art. album replaced with default image");
                song.AlbumCover = MainViewModel.Instance.LoadDefaultAlbumImage();
            }

            tfile.Dispose();
        }
    }

    private void DataGrid_UnloadingRow(object? sender, Avalonia.Controls.DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is Song song)
        {
            song.AlbumCover?.Dispose();
            song.AlbumCover = null;
        }
    }
    private void Image_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Avalonia.Controls.Control control)
        {
            var rowDataContext = control.DataContext;
            // rowDataContext is the item bound to the row
            // Use it as needed, for example cast to your item type:
            var song = rowDataContext as Song;
            if (song != null)
            {
                MusicPlayer.PlaySongFromQueue(song.SongPath);
            }
        }
    }
}
