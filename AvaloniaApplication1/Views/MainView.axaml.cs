using AvaloniaApplication1.ViewModels;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using System.IO;
using System;
using System.Collections.Generic;
using Kawazu;
using Avalonia;

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

        SongSearchBar.TextChanged += TextBox_TextChanged;
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
        if (e.Pointer.IsPrimary)
        {
            MainViewModel.Instance.SliderHeld = true;
        }
    }

    private void Slider_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.IsPrimary)
        {
            MainViewModel.Instance.SliderHeld = false;
            MusicPlayer.Play();
        }
    }

    private void OnVolumeChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        MusicPlayer.volume = (float)e.NewValue;
    }

    private async void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is Song song)
        {
            using (TagLib.File tfile = TagLib.File.Create(song.SongPath))
            {
                //not actually sure if this is more efficient. check later.
                if (MainViewModel.Instance.songsFinishedCaching)
                {
                    foreach (Song CachedSong in MainViewModel.Instance.CachedSongs)
                    {
                        if (CachedSong.SongPath == song.SongPath)
                        {
                            song = CachedSong;
                        }
                    }
                }
                else
                {
                    KawazuConverter converter = new KawazuConverter();

                    song.Title = tfile.Tag.Title;

                    if (song.Title?.Length > 0)
                        song.RomanisedTitle = await converter.Convert(song.Title, To.Romaji);

                    song.Artist = tfile.Tag.FirstPerformer;

                    if (song.Artist?.Length > 0)
                        song.RomanisedArtist = await converter.Convert(song.Artist, To.Romaji);

                    song.Album = tfile.Tag.Album;

                    if (song.Album?.Length > 0)
                        song.RomanisedAlbum = await converter.Convert(song.Album, To.Romaji);

                    song.Year = tfile.Tag.Year;

                    //maybe change to the list of genres later or something
                    song.Genre = tfile.Tag.JoinedGenres;

                    song.Duration = tfile.Properties.Duration.TotalSeconds;

                    converter.Dispose();
                }

                //get album image
                if (tfile.Tag.Pictures.Length > 0)
                {
                    MemoryStream ms;
                    ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
                    ms.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        song.AlbumCover = new Bitmap(ms).CreateScaledBitmap(new PixelSize(256, 256));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        song.AlbumCover = MainViewModel.Instance.DefaultAlbumImage().CreateScaledBitmap(new PixelSize(256, 256));
                    }

                    ms.Dispose();
                }
                else
                {
                    //Debug.WriteLine("file does not contain album art. album replaced with default image");
                    song.AlbumCover = MainViewModel.Instance.DefaultAlbumImage().CreateScaledBitmap(new PixelSize(256, 256));
                }
            }           
        }
    }

    private void DataGrid_UnloadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is Song song)
        {
            song.AlbumCover?.Dispose();
            song.AlbumCover = null;
        }
    }
    private void Image_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control control)
        {
            if(e.Pointer.IsPrimary)
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

    private async void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if(SongSearchBar.Text?.Length > 0)
        {
            List<Song> temp = new List<Song>();

            KawazuConverter converter = new KawazuConverter();

            string romanisedText = await converter.Convert(SongSearchBar.Text, To.Romaji);

            foreach (Song song in MainViewModel.Instance.CurrentSongs)
            {
                bool songAdded;

                if (song.Title?.Length > 0)
                    if (song.Title.ToLower().Contains(romanisedText.ToLower()))
                    {
                        temp.Add(song);
                        continue;
                    }

                if (song.Artist?.Length > 0)
                    if (song.Artist.ToLower().Contains(romanisedText.ToLower()))
                    {
                        temp.Add(song);
                        continue;
                    }

                if (song.RomanisedTitle?.Length > 0)
                    if (song.RomanisedTitle.ToLower().Contains(romanisedText.ToLower()))
                    {
                        temp.Add(song);
                        continue;
                    }

                if (song.RomanisedArtist?.Length > 0)
                    if (song.RomanisedArtist.ToLower().Contains(romanisedText.ToLower()))
                    {
                        temp.Add(song);
                        continue;
                    }
            }

            converter.Dispose();

            MainViewModel.Instance.CurrentlyDisplayedSongs = new System.Collections.ObjectModel.ObservableCollection<Song>(temp);
        }
    }

    private void FullScreen(object? sender, RoutedEventArgs e)
    {
        //temp playlist creation code
        //var songs = MainViewModel.Instance.CachedSongs;

        SaveFile ed = new SaveFile("TestPlaylist.txt");
        new SaveFloat("testing", ed).Set(3.14f);
        new SaveFloat("sting", ed).Set(2.71f);
        new SaveFloat("tting", ed).Set(1.23f);
        new SaveFloat("teting", ed).Set(4.56f);

        //MainWindow.Instance.DeleteButton_Click(sender, e);
    }
}
