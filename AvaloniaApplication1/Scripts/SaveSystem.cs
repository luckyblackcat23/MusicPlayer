using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

//didn't wanna use some overcomplicated json so instead i just made my own overcomplicated save system 
public static class SaveManager
{
    //list containing the name of the save files
    private static List<string> saveFiles()
    {
        List<string> temp = new();

        foreach (SaveFile file in SaveFiles)
            temp.Add(file.SaveName);

        return temp;
    }

    //save data for each file
    private static List<SaveFile> SaveFiles = new();

    //main (default) save file
    private static SaveFile? mainFile;

    public static SaveFile Main()
    {
        // create default file if it doesn't exist
        if (mainFile == null)
        {
            mainFile = new SaveFile("MainSave.txt");
        }
        return mainFile;
    }

    //lookup a savefile by name
    public static SaveFile? GetFile(string fileName)
    {
        return SaveFiles.Find(f => f.SaveName == fileName);
    }

    //register a new savefile
    internal static void RegisterFile(SaveFile file)
    {
        if (!SaveFiles.Contains(file))
            SaveFiles.Add(file);
    }
}

/// <summary>
/// Represents a single save file (text-based).
/// </summary>
public class SaveFile
{
    public string SaveName { get; }
    public string SavePath { get; }
    public List<SaveVariable> Variables { get; } = new();

    /// <summary>
    /// The file as it was written in the last read pass.
    /// </summary>
    public List<string> cachedText = new();

    public SaveFile(string fileName, string savePath = null)
    {
        SaveName = fileName ?? throw new ArgumentNullException(nameof(fileName));

        SavePath = savePath ?? Path.Combine(Globals.SaveFolderPath, fileName);
     

        if (!File.Exists(SavePath))
        {
            File.CreateText(SavePath);

            UpdateVariables();
        }

        SaveManager.RegisterFile(this);
    }

    /// <summary>
    /// Updates the file to match the SaveVariables
    /// </summary>
    public void UpdateVariables()
    {
        try
        {
            using StreamWriter sw = new StreamWriter(SavePath, false);
            foreach (var variable in Variables)
            {
                sw.WriteLine(variable.SavedString());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SaveFile] Failed to save {SaveName}: {ex}");
        }
    }

    /// <summary>
    /// Updates the SaveVariables to match the file
    /// </summary>
    public List<string> ReadAndSetVariables()
    {
        try
        {
            using(StreamReader sw = new StreamReader(SavePath, false))
            {
                cachedText.Clear();

                string line = sw.ReadLine();

                while (line != null)
                {
                    cachedText.Add(line);

                    line = sw.ReadLine();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SaveFile] Failed to read {SaveName}: {ex}");
        }

        foreach (SaveVariable variable in Variables)
        {
            foreach (string line in cachedText)
            {
                if (line.Contains(variable.SavedString()))
                {
                    variable.SetFromString(variable.SavedString().Remove(0, $"{variable.SavedName}=".Length));
                }
            }
        }

        return cachedText;
    }
}

/// <summary>
/// Base class for any variable saved in a SaveFile.
/// </summary>
public abstract class SaveVariable
{
    public string SavedName { get; }
    public SaveFile SaveFile { get; }
    private string _value = "";
    internal string Value 
    {
        get => _value;
        set
        {
            _value = value;

            if (UpdateOnChange)
            {
                SaveFile.UpdateVariables();
            }
        }
    }

    // enforce that all derived classes must implement Set and Get
    public abstract object GetAsObject();
    public abstract void SetFromObject(object v);

    public abstract string GetAsString();
    public abstract void SetFromString(string v);

    internal string SavedString() => $"{SavedName}={Value}";

    public abstract bool UpdateOnChange { get; set; }

    protected SaveVariable(string savedName, SaveFile? saveFile = null, bool updateOnChange = true)
    {
        UpdateOnChange = updateOnChange;

        SavedName = savedName ?? throw new ArgumentNullException(nameof(savedName));

        // decide which file this variable belongs to
        SaveFile = saveFile ?? SaveManager.Main();

        // register with the chosen file
        SaveFile.Variables.Add(this);
    }
}


/// <summary>
/// Stores and retrieves float values
/// </summary>
public class SaveFloat : SaveVariable
{
    public override bool UpdateOnChange { get; set; }

    public static implicit operator float(SaveFloat obj) => obj.Get();

    public SaveFloat(string savedName, SaveFile? saveFile = null) : base(savedName, saveFile) { }

    public float Get() => float.TryParse(Value, out var f) ? f : 0f;

    public void Set(float v)
    {
        Value = v.ToString();

        if (UpdateOnChange)
        {
            SaveFile.UpdateVariables();
        }
    }

    public override object GetAsObject() => Get();
    public override void SetFromObject(object v) => Set((float)v);

    public override string GetAsString() => Value;
    public override void SetFromString(string v)
    {
        if (float.TryParse(v, out float result))
            Set(result);
        else
            Value = default(float).ToString();
    }
}

/// <summary>
/// Stores and retrieves int values
/// </summary>
public class SaveInt : SaveVariable
{
    public override bool UpdateOnChange { get; set; }

    public static implicit operator int(SaveInt obj) => obj.Get();

    public SaveInt(string savedName, SaveFile? saveFile = null) : base(savedName, saveFile) { }

    public int Get() => int.TryParse(Value, out var i) ? i : 0;

    public void Set(int v)
    {
        Value = v.ToString();

        if (UpdateOnChange)
        {
            SaveFile.UpdateVariables();
        }
    }

    public override object GetAsObject() => Get();
    public override void SetFromObject(object v) => Set((int)v);

    public override string GetAsString() => Value;
    public override void SetFromString(string v)
    {
        if (int.TryParse(v, out int result))
            Set(result);
        else
            Value = default(int).ToString();
    }
}

/// <summary>
/// Stores and retrieves bool values
/// </summary>
public class SaveBool : SaveVariable
{
    public override bool UpdateOnChange { get; set; }

    public static implicit operator bool(SaveBool obj) => obj.Get();

    public SaveBool(string savedName, SaveFile? saveFile = null) : base(savedName, saveFile) { }

    public bool Get() => bool.TryParse(Value, out var b) && b;

    public void Set(bool v)
    {
        Value = v.ToString();

        if (UpdateOnChange)
        {
            SaveFile.UpdateVariables();
        }
    }

    public override object GetAsObject() => Get();
    public override void SetFromObject(object v) => Set((bool)v);

    public override string GetAsString() => Value;
    public override void SetFromString(string v)
    {
        if (bool.TryParse(v, out bool result))
            Set(result);
        else
            Value = default(bool).ToString();
    }
}

/// <summary>
/// Stores and retrieves string values
/// </summary>
public class SaveString : SaveVariable
{
    public override bool UpdateOnChange { get; set; }

    public static implicit operator string(SaveString obj) => obj.Get();

    public SaveString(string savedName, SaveFile? saveFile = null) : base(savedName, saveFile) { }

    public string Get() => Value ?? string.Empty;

    public void Set(string v)
    {
        Value = v ?? "";


        if (UpdateOnChange)
        {
            SaveFile.UpdateVariables();
        }
    }

    public override object GetAsObject() => Get();
    public override void SetFromObject(object v) => Set((string)v);

    //huh, guess these are kind of pointless here
    public override string GetAsString() => Value;
    public override void SetFromString(string v) => Set(v);
}


/// <summary>
/// Stores and retrieves Enum values
/// </summary>
public class SaveEnum<T> : SaveVariable where T : struct, Enum
{
    public override bool UpdateOnChange { get; set; }

    public static implicit operator T(SaveEnum<T> obj) => obj.Get();

    public SaveEnum(string savedName, SaveFile? saveFile = null) : base(savedName, saveFile) { }

    public T Get()
    {
        if (Enum.TryParse(Value, out T result))
            return result;
        return default; // fallback to first enum value
    }

    public void Set(T v)
    {
        Value = v.ToString();

        if (UpdateOnChange)
        {
            SaveFile.UpdateVariables();
        }
    }

    public override object GetAsObject() => Get();
    public override void SetFromObject(object v) => Set((T)v);

    public override string GetAsString() => Value;
    public override void SetFromString(string v) 
    {
        if(Enum.TryParse(v, out T result))
            Set(result);
        else
            Value = default(T).ToString();
    }
}