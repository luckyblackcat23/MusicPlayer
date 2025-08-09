using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

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

    private Bitmap? _albumCover;
    public Bitmap? AlbumCover
    {
        get => _albumCover;
        set
        {
            if (_albumCover != value)
            {
                _albumCover = value;
                OnPropertyChanged(nameof(AlbumCover));
            }
        }
    }

    public void OnSongUpdate()
    {
        SongLength = MusicPlayer.clipLength.TotalSeconds;
        AlbumCover = LoadAlbumImage();
    }

    private Bitmap LoadAlbumImage()
    {
        string path;
        MemoryStream ms;

        if (MusicPlayer.musicQueue.Length > 0)
        {
            path = MusicPlayer.musicQueue[MusicPlayer.currentSongIndex];

            var tfile = TagLib.File.Create(path);

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

    private Bitmap LoadDefaultAlbumImage()
    {
        string path = @"C:\Users\Jordan\Music\GameNameHereReplaceLater\Songs\The Vampire - DECO_27.mp3";
        var tfile = TagLib.File.Create(path);
        var ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
