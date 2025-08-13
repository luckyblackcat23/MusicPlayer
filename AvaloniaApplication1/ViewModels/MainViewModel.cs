using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaloniaApplication1.Views;

namespace AvaloniaApplication1.ViewModels;

public class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    public static MainViewModel Instance { get; private set; }

    public const int MinimumUIUpdateTime = 100;

    private int _timeBetweenUIUpdates = MinimumUIUpdateTime;
    public int TimeBetweenUIUpdates
    {
        get
        {
            return _timeBetweenUIUpdates;
        }
        set
        {
            _timeBetweenUIUpdates = Math.Min(value, MinimumUIUpdateTime);
        }
    }

    public DispatcherTimer _timer;

    public bool SliderHeld;
    public ObservableCollection<Song> _songs;
    public ObservableCollection<Song> Songs 
    { 
        get => _songs;
        set 
        {
            _songs = value;
            OnPropertyChanged(nameof(Songs));
        } 
    }

    public MainViewModel()
    {
        Instance = this;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(MinimumUIUpdateTime) // every 0.2 seconds
        };
        _timer.Tick += (s, e) =>
        {
            if (!SliderHeld)
                PlaybackTime = MusicPlayer.playbackTime.TotalSeconds;
        };
        _timer.Start();

        MusicPlayer.Initialize();

        //change later
        LoadSongRows();
    }

    //i have no fuckin clue what to call this variable
    List<Song> tempsongs = new();
    
    public async Task<Task> LoadSongRows()
    {
        return Task.Run(() =>
        {
            //change later
            foreach (string songPath in MusicPlayer.musicInitial)
            {
                tempsongs.Add(new Song(songPath));
            }

            Songs = new ObservableCollection<Song>(tempsongs);
        });
    }

    private double _PlaybackTime;
    public double PlaybackTime
    {
        get => _PlaybackTime;
        set
        {
            if (_PlaybackTime != value)
            {
                _PlaybackTime = value;
                OnPropertyChanged(nameof(PlaybackTime));
            }
        }
    }

    private double _SongLength;
    public double SongLength
    {
        get => _SongLength;
        set
        {
            if (_SongLength != value)
            {
                _SongLength = value;
                OnPropertyChanged(nameof(SongLength));
            }
        }
    }

    private Bitmap? _currentlyPlayingAlbumCover;
    public Bitmap? CurrentlyPlayingAlbumCover
    {
        get => _currentlyPlayingAlbumCover;
        set
        {
            if (_currentlyPlayingAlbumCover != value)
            {
                _currentlyPlayingAlbumCover = value;
                OnPropertyChanged(nameof(CurrentlyPlayingAlbumCover));
            }
        }
    }

    public void OnSongUpdate()
    {
        SongLength = MusicPlayer.clipLength.TotalSeconds;
        CurrentlyPlayingAlbumCover = LoadAlbumImage(MusicPlayer.musicQueue[MusicPlayer.currentSongIndex]);
    }

    private Bitmap LoadAlbumImage(string songPath)
    {
        MemoryStream ms;

        if (MusicPlayer.musicQueue.Length > 0)
        {
            var tfile = TagLib.File.Create(songPath);

            if (tfile.Tag.Pictures.Length > 0)
            {
                ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
            }
            else
            {
                //Debug.WriteLine("file does not contain album art. album replaced with default image");
                return LoadDefaultAlbumImage();
            }
        }
        else
        {
            Debug.WriteLine("no song was loaded when loading album image");
            return LoadDefaultAlbumImage();
        }

        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }

    public Bitmap LoadDefaultAlbumImage()
    {
        //change later
        string path = @"C:\Users\Jordan\Music\GameNameHereReplaceLater\Songs\The Vampire - DECO_27.mp3";
        var tfile = TagLib.File.Create(path);
        var ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms).CreateScaledBitmap(new Avalonia.PixelSize(64, 64));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
