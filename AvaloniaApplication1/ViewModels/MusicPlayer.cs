using Avalonia.Media.Imaging;
using AvaloniaApplication1.ViewModels;
using AvaloniaApplication1.Views;
using System.ComponentModel;
using TagLib;

namespace AvaloniaApplication1.ViewModels
{
    using System.Collections.Generic;
    using NAudio.Utils;
    using NAudio.Wave;
    using System.IO;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Numerics;
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
        static AudioFileReader? audioFile;

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
            outputDevice?.Dispose();
            outputDevice = null;
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
                if (File.Exists(file.FullName) && file.Extension.Equals(".mp3"))
                    temp.Add(file.FullName);
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
                var newReader = new AudioFileReader(Song);

                AudioFileReader oldReader = Interlocked.Exchange(ref audioFile, newReader);

                if (oldReader != null)
                {
                    oldReader.Dispose();
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

            Debug.WriteLine("Now playing: " + audioFile?.FileName);
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

            Debug.WriteLine("Now playing: " + audioFile?.FileName);
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

    public string Title { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public string Year { get; set; }
    public string Genre { get; set; }

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

    public Song(string songPath, Bitmap? albumCover = null, string title = "Unknown", string artist = "Unknown", string album = "Unknown", string year = "Unknown", string genre = "Unknown genre", double duration = 0)
    {
        SongPath = songPath;
        AlbumCover = albumCover;
        Title = title;
        Artist = artist;
        Album = album;
        Year = year;
        Genre = genre;
        Duration = duration;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}