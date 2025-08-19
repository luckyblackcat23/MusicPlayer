﻿using System;
using System.IO;

public static class Globals
{
    //public const Int32 BUFFER_SIZE = 512; // Unmodifiable
    //public static String FILE_NAME = "Output.txt"; // Modifiable
    //public static readonly String CODE_PREFIX = "US-"; // Unmodifiable

    public const string SaveFolderName = "Music Player";
    public static readonly string SaveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SaveFolderName);
    public static readonly string PlaylistsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SaveFolderName, "Playlists");
}
