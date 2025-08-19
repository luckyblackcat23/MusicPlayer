using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Threading;
using System.Reflection;
using Avalonia.Controls;
using System.Linq;
using System.IO;
using Kawazu;
using System;
using Avalonia.Interactivity;
using Avalonia.Controls.Templates;
using DynamicData;

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

    private ObservableCollection<Song> _currentlyDisplayedSongs;
    public ObservableCollection<Song> CurrentlyDisplayedSongs 
    { 
        get => _currentlyDisplayedSongs;
        set 
        {
            _currentlyDisplayedSongs = value;
            OnPropertyChanged(nameof(CurrentlyDisplayedSongs));
        }
    }

    private ObservableCollection<DirectoryNode> _nodes;
    public ObservableCollection<DirectoryNode> Nodes 
    {
        get => _nodes;
        set
        {
            _nodes = value;
            OnPropertyChanged(nameof(Nodes));
        }
    }

    private ObservableCollection<DirectoryNode> _selectedNodes;
    public ObservableCollection<DirectoryNode> SelectedNodes
    {
        get => _selectedNodes;
        set
        {
            _selectedNodes = value;
            OnPropertyChanged(nameof(SelectedNodes));
        }
    }

    public MainViewModel()
    {
        Instance = this;

        // Acts as an update loop. updates everything at a standard rate of x seconds
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(MinimumUIUpdateTime) // Changing this value could affect performance.
        };
        _timer.Tick += (s, e) =>
        {
            if (!SliderHeld)
                PlaybackTime = MusicPlayer.playbackTime.TotalSeconds;
        };
        _timer.Start();

        if (!Directory.Exists(Globals.PlaylistsPath))
            Directory.CreateDirectory(Globals.PlaylistsPath);

        SelectedNodes = new ObservableCollection<DirectoryNode>();

        Nodes = new ObservableCollection<DirectoryNode>() { new DirectoryNode(Globals.PlaylistsPath) };

        //i really need to stop making functions called initialize
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

        await CacheSongs(); // Code beyond here runs after songs have finished caching

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
                        song.AlbumCover = DefaultAlbumImage();
                    }
                }
                else
                {
                    song.AlbumCover = DefaultAlbumImage();
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
                return DefaultAlbumImage();
            }
        }
        else
        {
            Debug.WriteLine("no song was loaded when loading album image");
            return DefaultAlbumImage();
        }

        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }

    public string DefaultAlbumImagePath()
    {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"C:Assets\DefaultIcon.jpg");
    }

    public Bitmap DefaultAlbumImage()
    {
        if (Design.IsDesignMode)
        {
            //change later
            string path = @"C:\Users\Jordan\Music\GameNameHereReplaceLater\Songs\The Vampire - DECO_27.mp3";
            var tfile = TagLib.File.Create(path);
            var ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
            ms.Seek(0, SeekOrigin.Begin);
            return new Bitmap(ms);
        }
        else
            return new Bitmap(DefaultAlbumImagePath());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class DirectoryNode
{
    public string Name { get; }
    public string FullPath { get; }
    public ObservableCollection<DirectoryNode> SubNodes { get; }

    public NodeType Type { get; }

    private bool _isLoaded;

    public DirectoryNode(string path)
    {
        FullPath = path;
        Name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name))
            Name = path;

        SubNodes = new ObservableCollection<DirectoryNode>();

        if (Directory.Exists(path))
        {
            Type = NodeType.Directory;
            // Add a dummy child so the expander arrow appears
            SubNodes.Add(null!);
        }
        else
        {
            Type = NodeType.Playlists;
        }
        /*scrapped for now
        else if (Path.GetExtension(path) == ".myext") // in case i add any extra file types of my own in the future
        {
            Type = NodeType.SpecialFile;
        }
        */

        LoadChildren();
    }

    public void LoadChildren()
    {
        if (_isLoaded || Type != NodeType.Directory) return;
        _isLoaded = true;

        SubNodes.Clear();

        try
        {
            foreach (var dir in Directory.GetDirectories(FullPath))
            {
                SubNodes.Add(new DirectoryNode(dir));
            }
            foreach (var file in Directory.GetFiles(FullPath))
            {
                SubNodes.Add(new DirectoryNode(file));
            }
        }
        catch
        {
            // Skip directories we don't have access to
        }
    }
}

public enum NodeType
{
    Directory,
    Playlists,
}