// Usage example:

// public SaveFloat MasterVolume = new SaveFloat("Master Volume", Main.txt, true, 1, 0);
// or just
// public SaveFloat MasterVolume = new SaveFloat("Master Volume" SaveOnChange: false);

// public SaveFloat TutorialComplete = new SaveBool("Completed Tutorial");
// ...
// if(TutorialComplete)
// {
//     //code here...
// }

using System;
using System.Diagnostics;

public abstract class SaveVariable
{
    protected bool initialized;

    public string savedName;
    public bool saveOnChange = true;

    public int group;
    
    /// <summary> The variable written exactly as it would be in the saveFile </summary>
    public abstract string SavedString();

    /// <summary> Updates the Value to match the save file </summary>
    public abstract void UpdateValue();

    public string saveFile = "Main.txt";

    public SaveVariable(string SavedName, string _SaveFile = "Main.txt", bool SaveOnChange = true, int Group = default)
    {
        savedName = SavedName;
        saveOnChange = SaveOnChange;
        group = Group;
        saveFile = _SaveFile;

        if (saveFile != null)
        {
            SaveManager.OnInitialize += () =>
            {
                initialized = true;

                bool contained = false;

                foreach (SaveVariable variable in SaveManager.Variables)
                {
                    if (variable.savedName == SavedName)
                    {
                        contained = true;
                    }
                }

                if (!contained)
                {
                    SaveManager.Variables.Add(this);
                }
            };
        }
        else
            Debug.WriteLine("Unable to initialize variable " + savedName + " as the associated save file (" + saveFile + ") was not found");
    }

    /// <summary> 
    /// Reads the value of the SaveVariable as it is written in the save file
    /// <para>
    /// This is just here for ease of use. This function is entirely redundant unless you dont have access to the save manager 
    /// </para> 
    /// </summary>
    public string ReadSavedStringValue()
    {
        return SaveManager.FindVariableValue(savedName);
    }

    /// <summary> 
    /// Reads the value of the SaveVariable as it is written in the save file
    /// <para>
    /// This is just here for ease of use. This function is entirely redundant unless you dont have access to the save manager 
    /// </para> 
    /// </summary>
    public string ReadSavedFull()
    {
        return SaveManager.FindVariable(savedName);
    }

    /// <summary> Read the variable as it is currently written in the save file </summary>
    public void Save()
    {
        SaveManager.UpdateVariable(this);
    }
}

public class SaveInt : SaveVariable
{
    //honestly i dont fully understand why i need some of this although the code doesn't work without it.
    public SaveInt(string SavedName, string _SaveFile = "Main.txt", int DefaultValue = 0, bool SaveOnChange = true, int Group = default) : base(SavedName, _SaveFile, SaveOnChange, Group) 
    {
        defaultValue = DefaultValue;
    }

    int defaultValue;

    private int _value;
    public int Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;

            if (saveOnChange)
                Save();
        }
    }

    public static implicit operator int (SaveInt obj)
    {
        return obj.Value;
    }

    public override string SavedString() => savedName + " : " + Value.ToString();

    public override void UpdateValue()
    {
        if(int.TryParse(ReadSavedStringValue(), out int val))
        {
            Value = val;
        }
        else
        {
            Value = defaultValue;
            Debug.WriteLine("Variable  `" + savedName + "` was not found when attempting to update value. value set to " + defaultValue);
        }
    }

    public int ReadSavedValue()
    {
        if (int.TryParse(ReadSavedStringValue(), out int val))
        {
            return val;
        }
        else
        {

            return defaultValue;
        }
    }
}

public class SaveFloat : SaveVariable
{
    public SaveFloat(string SavedName, string _SaveFile = "Main.txt", float DefaultValue = 0f, bool SaveOnChange = true, int Group = default) : base(SavedName, _SaveFile, SaveOnChange, Group)
    { 
        defaultValue = DefaultValue; 
    }
    
    float defaultValue;

    private float _value;
    public float Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;

            if (saveOnChange)
                Save();
        }
    }

    public static implicit operator float (SaveFloat obj)
    {
        return obj.Value;
    }

    public override string SavedString() => savedName + " : " + Value.ToString();

    public override void UpdateValue()
    {
        if (float.TryParse(ReadSavedStringValue(), out float val))
        {
            Value = val;
        }
        else
        {
            Value = defaultValue;
            Debug.WriteLine("Variable  `" + savedName + "` was not found when attempting to update value. value set to " + defaultValue);
        }
    }

    public float ReadSavedValue()
    {
        if (float.TryParse(ReadSavedStringValue(), out float val))
        {
            return val;
        }
        else
        {
            return defaultValue;
        }
    }
}

public class SaveString : SaveVariable
{
    public SaveString(string SavedName, string _SaveFile = "Main.txt", string DefaultValue = "", bool SaveOnChange = true, int Group = default) : base(SavedName, _SaveFile, SaveOnChange, Group) 
    { 
        defaultValue = DefaultValue; 
    }

    string defaultValue;

    private string _value;
    public string Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;
            
            if (saveOnChange)
                Save();
        }
    }

    public static implicit operator string (SaveString obj)
    {
        return obj.Value;
    }

    public override string SavedString() => savedName + " : " + Value.ToString();

    public override void UpdateValue()
    {
        string val = ReadSavedStringValue();

        if (val?.Length > 0)
        {
            Value = val;
        }
        else
        {
            Value = defaultValue;
            Debug.WriteLine("Variable  `" + savedName + "` was not found when attempting to update value. value set to " + defaultValue);
        }
    }

    public string ReadSavedValue()
    {
        string val = ReadSavedStringValue();

        if (val?.Length > 0)
        {
            return val;
        }
        else
        {
            return defaultValue;
        }
    }
}

public class SaveBool : SaveVariable
{
    public SaveBool(string SavedName, string _SaveFile = "Main.txt", bool DefaultValue = false, bool SaveOnChange = true, int Group = default) : base(SavedName, _SaveFile, SaveOnChange, Group) 
    { 
        defaultValue = DefaultValue; 
    }

    bool defaultValue;

    private bool _value;
    public bool Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;

            if (saveOnChange)
                Save();
        }
    }

    public static implicit operator bool(SaveBool obj)
    {
        return obj.ReadSavedValue();
    }

    public override string SavedString() => savedName + " : " + Value.ToString();

    public override void UpdateValue()
    {
        if (bool.TryParse(ReadSavedStringValue(), out bool val))
        {
            Value = val;
        }
        else
        {
            Value = defaultValue;
            Debug.WriteLine("Variable  `" + savedName + "` was not found when attempting to update value. value set to " + defaultValue);
        }
    }

    public bool ReadSavedValue()
    {
        if (bool.TryParse(ReadSavedStringValue(), out bool val))
        {
            return val;
        }
        else
        {
            return defaultValue;
        }
    }
}

public class SaveEnum<T> : SaveVariable where T : struct, Enum
{
    public SaveEnum(string SavedName, string _SaveFile = "Main.txt", T DefaultValue = default, bool SaveOnChange = true, int Group = default) : base(SavedName, _SaveFile, SaveOnChange, Group)
    { 
        defaultValue = DefaultValue; 
    }

    T defaultValue;

    private T _value;
    public T Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;

            if (saveOnChange)
                Save();
        }
    }

    public static implicit operator T (SaveEnum<T> obj)
    {
        return obj.Value;
    }

    public override string SavedString() => savedName + " : " + Value.ToString();

    public override void UpdateValue()
    {
        if (Enum.TryParse(ReadSavedStringValue(), out T result))
        {
            Value = result;
        }
        else
        {
            Value = defaultValue;
            Debug.WriteLine("Variable  `" + savedName + "` was not found when attempting to update value. value set to " + defaultValue);
        }
    }

    public T ReadSavedValue()
    {
        if (Enum.TryParse(ReadSavedStringValue(), out T result))
        {
            return result;
        }
        else
        {
            return defaultValue;
        }
    }
}

public class SaveAlbum : SaveVariable
{
    public SaveAlbum(string SavedName, string _SaveFile = "Main.txt", bool DefaultValue = false, bool SaveOnChange = true, int Group = default) : base(SavedName, _SaveFile, SaveOnChange, Group)
    {
        defaultValue = DefaultValue;
    }

    bool defaultValue;

    private bool _value;
    public bool Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;

            if (saveOnChange)
                Save();
        }
    }

    public override string SavedString() => savedName + " : " + Value.ToString();

    public override void UpdateValue()
    {
        if (bool.TryParse(ReadSavedStringValue(), out bool val))
        {
            Value = val;
        }
        else
        {
            Value = defaultValue;
            Debug.WriteLine("Variable  `" + savedName + "` was not found when attempting to update value. value set to " + defaultValue);
        }
    }

    public bool ReadSavedValue()
    {
        if (bool.TryParse(ReadSavedStringValue(), out bool val))
        {
            return val;
        }
        else
        {
            return defaultValue;
        }
    }
}