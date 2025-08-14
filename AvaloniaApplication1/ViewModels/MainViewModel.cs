using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Kawazu;

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
    public ObservableCollection<Song> _currentlyDisplayedSongs;
    public ObservableCollection<Song> CurrentlyDisplayedSongs 
    { 
        get => _currentlyDisplayedSongs;
        set 
        {
            _currentlyDisplayedSongs = value;
            OnPropertyChanged(nameof(CurrentlyDisplayedSongs));
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

        Initalization();
    }

    private async void Initalization()
    {
        //change later
        foreach (string songPath in MusicPlayer.musicInitial)
        {
            CachedSongs.Add(new Song(songPath));
        }

        CurrentSongs = new List<Song>(CachedSongs);
        CurrentlyDisplayedSongs = new ObservableCollection<Song>(CachedSongs);

        await CacheSongs();

        songsFinishedCaching = true;
    }

    /// <summary>
    /// All songs stored in memory
    /// </summary>
    public List<Song> CachedSongs = new();
    public List<Song> CurrentSongs = new();

    public bool songsFinishedCaching;

    private async Task CacheSongs()
    {
        // Create converter only once
        using var converter = new KawazuConverter();

        // Parallel processing (limit to a safe degree, e.g., 4 threads)
        await Parallel.ForEachAsync(CachedSongs, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (song, _) =>
        {
            try
            {
                using var tfile = TagLib.File.Create(song.SongPath);

                song.Title = tfile.Tag.Title;
                song.Artist = tfile.Tag.FirstPerformer;
                song.Album = tfile.Tag.Album;
                song.Year = tfile.Tag.Year;
                song.Genre = tfile.Tag.JoinedGenres;

                // Romanisation only if needed
                if (!string.IsNullOrWhiteSpace(song.Title))
                    song.RomanisedTitle = await converter.Convert(song.Title, To.Romaji);
                if (!string.IsNullOrWhiteSpace(song.Artist))
                    song.RomanisedArtist = await converter.Convert(song.Artist, To.Romaji);
                if (!string.IsNullOrWhiteSpace(song.Album))
                    song.RomanisedAlbum = await converter.Convert(song.Album, To.Romaji);

                // Use TagLib for duration (avoids NAudio extra read)
                var properties = tfile.Properties;
                song.Duration = properties.Duration.TotalSeconds;

                // Album art (optional: lazy-load later)
                if (tfile.Tag.Pictures.Length > 0)
                {
                    using var ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
                    ms.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        song.AlbumCover = new Bitmap(ms).CreateScaledBitmap(new Avalonia.PixelSize(124, 124));
                    }
                    catch
                    {
                        song.AlbumCover = LoadDefaultAlbumImage();
                    }
                }
                else
                {
                    song.AlbumCover = LoadDefaultAlbumImage();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing {song.SongPath}: {ex.Message}");
            }
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
