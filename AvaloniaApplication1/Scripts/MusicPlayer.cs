using Avalonia.Media.Imaging;
using AvaloniaApplication1.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;

namespace AvaloniaApplication1.ViewModels
{
    using System.Collections.Generic;
    using NAudio.Wave;
    using NAudio.Vorbis;
    using System.IO;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Threading;
    using DynamicData;

    internal static class MusicPlayer
    {
        //update/improve later
        public static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "GameNameHereReplaceLater", "Songs");

        public static bool shuffle;
        public static bool loop = true;
        public static bool loopSingle;

        public static TimeSpan playbackTime;
        public static TimeSpan clipLength;

        public static bool paused;

        public static string[] musicQueue = new string[0];
        public static int currentSongIndex = 0;

        static WaveOutEvent? outputDevice;
        static WaveStream? audioFile;

        //store paths
        public static List<string> musicInitial = new();

        private static float _volume;
        public static float volume
        {
            get
            {
                return _volume;
            }
            set 
            {
                _volume = value;

                if (outputDevice != null)
                    outputDevice.Volume = value;
            }
        }

        private static void OnPlaybackStopped(object? sender, StoppedEventArgs args)
        {

        }

        //still needs to run
        public static async void Initialize()
        {
            DirectoryInfo info = new DirectoryInfo(path);

            FileInfo[] fileInfo = info.GetFiles();

            List<string> temp = new List<string>();

            foreach (FileInfo file in fileInfo)
            {
                //file.exists might be unnecessary. might remove later if im really strapped on things to optimise
                if (File.Exists(file.FullName))
                {
                    //there is definitely a better way to add supported file type
                    if (file.Extension.Equals(".mp3"))
                    {
                        temp.Add(file.FullName);
                    }
                    else if (file.Extension.Equals(".ogg"))
                    {
                        temp.Add(file.FullName);
                    }
                    else if (file.Extension.Equals(".flac"))
                    {
                        temp.Add(file.FullName);
                    }
                    else if (file.Extension.Equals(".wav"))
                    {
                        temp.Add(file.FullName);
                    }
                    else if (file.Extension.Equals(".aiff"))
                    {
                        temp.Add(file.FullName);
                    }
                }
            }

            musicInitial = temp;

            Requeue(shuffle);

            await Play();
            Pause();

            MainViewModel.Instance._timer.Tick += (s, e) =>
            {
                if (!paused && !MainViewModel.Instance.SliderHeld)
                    playbackTime = audioFile?.CurrentTime ?? TimeSpan.Zero;

                Update();
            };
        }

        // Update is called once per frame
        public static void Update()
        {
            //once the song has ended
            if (outputDevice?.PlaybackState == PlaybackState.Stopped && !paused)
                PlayNext();
        }

        public static void Pause()
        {
            if (!paused && !MainViewModel.Instance.SliderHeld)
            {
                playbackTime = audioFile?.CurrentTime ?? playbackTime;
                paused = true;
                outputDevice?.Pause();
            }
        }

        public static void Stop()
        {
            outputDevice?.Stop();
            playbackTime = TimeSpan.Zero;
            paused = true;
        }

        public static Task LoadAsync(string Song)
        {
            return Task.Run(() =>
            {
                if (Song.EndsWith(".mp3") || Song.EndsWith(".flac") || Song.EndsWith(".wav") || Song.EndsWith(".aiff"))
                {
                    WaveStream newReader;

                    newReader = new AudioFileReader(Song);

                    WaveStream oldReader = Interlocked.Exchange(ref audioFile, newReader);

                    if (oldReader != null)
                    {
                        oldReader.Dispose();
                    }
                }
                else if (Song.EndsWith(".ogg"))
                {
                    WaveStream newReader;

                    newReader = new VorbisWaveReader(Song);

                    WaveStream oldReader = Interlocked.Exchange(ref audioFile, newReader);

                    if (oldReader != null)
                    {
                        oldReader.Dispose();
                    }
                }
            });
        }

        public static async Task Play()
        {
            paused = false;

            if (currentSongIndex >= musicQueue.Length)
                return;

            string currentPath = musicQueue[currentSongIndex];

            //clean up existing resources
            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;

            var pbT = new TimeSpan(playbackTime.Ticks);

            await LoadAsync(currentPath);
            
            audioFile.CurrentTime = pbT;

            clipLength = audioFile.TotalTime;

            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);

            outputDevice.PlaybackStopped += OnPlaybackStopped;

            outputDevice?.Play();

            MainViewModel.Instance?.OnSongUpdate();
        }

        public static void TogglePause()
        {
            if (paused)
                _ = Play();
            else
                Pause();
        }

        public static async void PlayNext()
        {
            currentSongIndex++;

            playbackTime = TimeSpan.Zero;

            if (currentSongIndex < musicQueue.Length)
            {
                Stop();
                await Play();
            }
            else
            {
                Requeue(shuffle);

                if (loop)
                    PlayNext();
                else
                    Pause();
            }

            Debug.WriteLine("Now playing: " + musicQueue[currentSongIndex]);
        }

        public static async void PlayPrevious()
        {
            currentSongIndex--;

            if (currentSongIndex < 0)
            {
                Pause();
                currentSongIndex = 0;
            }

            Stop();
            await Play();

            Debug.WriteLine("Now playing: " + musicQueue[currentSongIndex]);
        }

        public static void Requeue(bool shuffle = false)
        {
            currentSongIndex = 0;

            if (shuffle)
            {
                //use another array to be shuffled (not shuffling the original array, in order to preserve user initialized order)
                string[] temp = new string[musicInitial.Count];
                musicInitial.CopyTo(temp, 0);

                Random rand = new();

                //shuffle the temporary array
                rand.Shuffle(temp);

                //set the musicQueue to the temporary shuffled queue
                musicQueue = temp;
            }
            else
            {
                musicQueue = musicInitial.ToArray();
            }

            MainViewModel.Instance?.OnSongUpdate();
        }

        public static void PlaySongFromQueue(string Song)
        {
            currentSongIndex = musicQueue.IndexOf(Song);
            Stop();
            Play();
        }
    }

    //probably an easier way to do this rather than importing a new function
    /* moved to a different script
    static class RandomExtensions
    {
        public static void Shuffle<T>(this System.Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }
    */
}


public class Song : INotifyPropertyChanged
{
    public string SongPath;

    private Bitmap? _albumCover;
    public Bitmap? AlbumCover 
    {
        get
        {
            if (_albumCover != null)
                return _albumCover;
            else
                return MainViewModel.Instance.LoadDefaultAlbumImage();
        }
        set
        {
            _albumCover = value;
            OnPropertyChanged(nameof(AlbumCover));
        }
    }

    private string _title;
    public string Title 
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    private string _romanisedTitle;
    public string RomanisedTitle
    {
        get => _romanisedTitle;
        set
        {
            _romanisedTitle = value;
            OnPropertyChanged(nameof(RomanisedTitle));
        }
    }

    private string _artist;
    public string Artist 
    { 
        get => _artist;
        set
        {
            _artist = value;
            OnPropertyChanged(nameof(Artist));
        }
    }

    private string _romanisedArtist;
    public string RomanisedArtist
    {
        get => _romanisedArtist;
        set
        {
            _romanisedArtist = value;
            OnPropertyChanged(nameof(RomanisedArtist));
        }
    }

    private string _album;
    public string Album 
    { 
        get => _album;
        set
        {
            _album = value;
            OnPropertyChanged(nameof(Album));
        }
    }

    private string _romanisedAlbum;
    public string RomanisedAlbum
    {
        get => _romanisedAlbum;
        set
        {
            _romanisedAlbum = value;
            OnPropertyChanged(nameof(RomanisedAlbum));
        }
    }

    private uint _year;
    public uint Year 
    { 
        get => _year;
        set
        {
            _year = value;
            OnPropertyChanged(nameof(Year));
        }
    }

    private string _genre;
    public string Genre 
    { 
        get => _genre;
        set
        {
            _genre = value;
            OnPropertyChanged(nameof(Genre));
        }
    }

    private double _duration;
    public double Duration 
    { 
        get => _duration;
        set
        {
            _duration = value;
            OnPropertyChanged(nameof(Duration));
        }
    }

    public Song(string songPath, Bitmap? albumCover = null, string title = "Unknown", string romanisedTitle = "Unknown", string artist = "Unknown", string romanisedArtist = "Unkown", string album = "Unknown", string romanisedAlbum = "Unkown", uint year = 0, string genre = "Unknown genre", double duration = 0)
    {
        SongPath = songPath;
        AlbumCover = albumCover;
        Title = title;
        RomanisedTitle = romanisedTitle;
        Artist = artist;
        RomanisedArtist = artist;
        Album = album;
        RomanisedAlbum = album;
        Year = year;
        Genre = genre;
        Duration = duration;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class Playlist : INotifyPropertyChanged
{
    public List<Song> Songs;
    public string PlaylistCoverPath;

    public Bitmap GetAlbumCover()
    {
        return new Bitmap(PlaylistCoverPath);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}