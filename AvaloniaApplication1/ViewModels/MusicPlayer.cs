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
    using TagLib.Mpeg;

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

        private static void OnPlaybackStopped(object? sender, StoppedEventArgs args)
        {
            outputDevice?.Dispose();
            outputDevice = null;
            audioFile?.Dispose();
            audioFile = null;
        }

        //still needs to run
        public static void Initialize()
        {
            DirectoryInfo info = new DirectoryInfo(path);

            FileInfo[] fileInfo = info.GetFiles();

            List<string> temp = new List<string>();

            foreach (FileInfo file in fileInfo)
            {
                temp.Add(file.FullName);
            }

            musicInitial = temp;

            Requeue(shuffle);

            MainViewModel.Instance._timer.Tick += (s, e) =>
            {
                if (!paused && !MainViewModel.Instance.SliderHeld)
                    playbackTime = outputDevice?.GetPositionTimeSpan() ?? TimeSpan.Zero;
            };
        }

        // Update is called once per frame
        public static void Update()
        {
            if (audioFile != null && !MainViewModel.Instance.SliderHeld)
                playbackTime = outputDevice.GetPositionTimeSpan();

            //once the song has ended
            if (playbackTime >= clipLength)
                PlayNext();
        }

        public static void Pause()
        {
            if (!paused && !MainViewModel.Instance.SliderHeld)
            {
                playbackTime = outputDevice?.GetPositionTimeSpan() ?? playbackTime;
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

            // Only recreate the audioFile if it's null or a different file
            if (audioFile == null || audioFile.FileName != currentPath)
            {
                outputDevice?.Stop(); // Clean up any existing resources

                await LoadAsync(currentPath);

                // Resume playback
                audioFile.Skip(playbackTime);

                clipLength = audioFile.TotalTime;

                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
            }
            else
            {
                // Resume playback
                audioFile.CurrentTime = playbackTime;
            }

            outputDevice.Play();

            MainViewModel.Instance?.OnSongUpdate();
        }

        public static void TogglePause()
        {
            if (paused)
                Play();
            else
                Pause();
        }

        public static async void PlayNext()
        {
            currentSongIndex++;

            playbackTime = TimeSpan.Zero;

            if (currentSongIndex < musicQueue.Length)
            {
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

            Debug.WriteLine("Now playing: " + audioFile.FileName);
        }

        public static async void PlayPrevious()
        {
            currentSongIndex--;

            if (currentSongIndex < 0)
            {
                Pause();
                currentSongIndex = 0;
            }

            await Play();

            Debug.WriteLine("Now playing: " + audioFile.FileName);
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
