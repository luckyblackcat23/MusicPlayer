using AvaloniaApplication1.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;

//didn't wanna use some overcomplicated json so instead i just made my own overcomplicated save system 
public static class SaveManager
{
    //list containing the name of the save files
    private static List<string> saveFiles()
    {
        List<string> temp = new();

        foreach (SaveFile file in saveFileData)
        {
            temp.Add(file.SaveFileName);
        }

        return temp;
    }

    public static SaveFile FindSaveOfName(string saveFileName)
    {
        foreach (SaveFile file in saveFileData)
        {
            if(file.SaveFileName == saveFileName)
                return file;
        }

        return null;
    }

    // required data of each save file of name
    public static List<SaveFile> saveFileData = new();

    // Reccomended that SaveFiles is stored in some Globals class.

    /// <summary>
    /// Generic data
    /// </summary>
    public static SaveFile MainSave = new("Main.txt");
    public static SaveFile Settings = new("Settings.txt");

    public static List<SaveVariable> Variables = new();

    /// <summary> Saves all variables, assigned to this SaveFile, to the associated file  </summary>
    public static void SaveAll()
    {
        foreach(SaveFile saveFile in saveFileData)
        {
            using (StreamWriter outputFile = new StreamWriter(saveFile.SaveFilePath()))
            {
                foreach (SaveVariable variable in Variables)
                {
                    if (saveFile.groups.Length > 0)
                    {
                        for (int i = 0; i < saveFile.groups.Length; i++)
                        {
                            outputFile.WriteLine(saveFile.groups[i]);
                            outputFile.WriteLine();

                            if (variable.saveFile.Equals(saveFile.SaveFileName) && variable.group == i)
                            {
                                outputFile.WriteLine(variable.SavedString());
                            }

                            outputFile.WriteLine();
                        }
                    }
                    else
                    {
                        if (variable.saveFile.Equals(saveFile.SaveFileName))
                        {
                            outputFile.WriteLine(variable.SavedString());
                        }
                    }

                }

                outputFile.Close();
            }
        }
    }

    public static List<string> ReadAll()
    {
        List<string> list = new List<string>();

        foreach (SaveFile saveFile in saveFileData)
        {
            using (StreamReader outputFile = File.OpenText(saveFile.SaveFilePath()))
            {
                string line = outputFile.ReadLine();

                while (line != null)
                {
                    list.Add(line);

                    line = outputFile.ReadLine();
                }

                outputFile.Close();
            }
        }

        return list;
    }

    public delegate void OnInitializeDelegate();
    public static OnInitializeDelegate OnInitialize;
}

public class SaveFile
{
    public List<SaveVariable> Variables;

    private static SaveFile main;
    public static SaveFile Main()
    {
        if (main == null)
            return new("Main.txt");
        else
            return main;
    }

    /// <summary>
    /// Include .txt at the end unless you plan to use another filetype
    /// </summary> 
    public string SaveFileName = "Main.txt";
    public string SaveFilePath()
    {
        //change later
        return Path.Combine(Globals.SavePath, SaveFileName);
    }

    public static implicit operator string(SaveFile obj)
    {
        return obj.SaveFileName;
    }

    public string[] groups = new string[] { "General" };

    public List<string> ReadSaveFile()
    {
        List<string> list = new List<string>();

        using (StreamReader outputFile = File.OpenText(SaveFilePath()))
        {
            string line = outputFile.ReadLine();

            while (line != null)
            {
                list.Add(line);

                line = outputFile.ReadLine();
            }

            outputFile.Close();
        }
        return list;
    }

    /// <summary> 
    /// Writes string to line
    /// <para>
    /// Warning: this will overwrite a line. if you dont know exactly what will be on that line do not use this function
    /// </para> 
    /// </summary>
    public void lineChanger(string newText, int lineToEdit)
    {
        string[] arrLine = ReadSaveFile().ToArray();
        arrLine[lineToEdit - 1] = newText;
        using (StreamWriter outputFile = new StreamWriter(SaveFilePath()))
        {
            foreach (string line in arrLine)
            {
                outputFile.WriteLine(line);
            }
            outputFile.Close();
        }
    }

    public string FindVariable(string SavedName)
    {
        foreach (string line in ReadSaveFile())
        {
            if (line.StartsWith(SavedName + " : "))
            {
                return line;
            }
        }

        Debug.WriteLine("saved variable was not found.");

        return null;
    }

    public string FindVariable(string SavedName, out int lineNumber)
    {
        string[] lines = ReadSaveFile().ToArray();

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith(SavedName + " : "))
            {
                //not sure if this is correct or if i messed something up. code works when i add 1 though so whatever.
                lineNumber = i + 1;
                return lines[i];
            }
        }

        Debug.WriteLine("saved variable was not found. line number set to 0");

        lineNumber = 0;
        return null;
    }

    public string FindVariableValue(string SavedName)
    {
        foreach (string line in ReadSaveFile())
        {
            if (line.StartsWith(SavedName + " : "))
            {
                return line.Remove(0, (SavedName + " : ").Length);
            }
        }

        Debug.WriteLine("saved variable was not found.");

        return "";
    }

    public void UpdateVariable(SaveVariable variable)
    {
        FindVariable(variable.savedName, out int line);

        lineChanger(variable.SavedString(), line);
    }


    public SaveFile(string name)
    {
        SaveManager.saveFileData.Add(this);

        SaveFileName = name;

        if (!Directory.Exists(Globals.SavePath))
        {
            Directory.CreateDirectory(Globals.SavePath);

            using (StreamWriter outputFile = File.CreateText(SaveFilePath()))
            {
                if (groups.Length > 0)
                {
                    for (int i = 0; i < groups.Length; i++)
                    {
                        outputFile.WriteLine(groups[i]);
                        outputFile.WriteLine();

                        foreach (SaveVariable variable in Variables)
                        {
                            if (variable.Equals(SaveFileName) && variable.group == i)
                            {
                                outputFile.WriteLine(variable.SavedString());
                            }
                        }

                        outputFile.WriteLine();
                    }
                }
                else
                {
                    foreach (SaveVariable variable in Variables)
                    {
                        if (variable.saveFile.Equals(SaveFileName))
                        {
                            outputFile.WriteLine(variable.SavedString());
                        }
                    }
                }

                outputFile.Close();
            }
        }
        else
        {
            if (!File.Exists(SaveFilePath()))
                File.CreateText(SaveFilePath());

            bool variableNotFound = false;

            //check if any variables are missing
            foreach (SaveVariable variable in Variables)
            {
                //should only ever occur after updates or if someone deleted a variable from the save file
                if (FindVariable(variable.savedName) == null)
                    variableNotFound = true;
            }

            //if a variable isn't found during initialisation
            if (variableNotFound)
            {
                Debug.WriteLine("Fixing save file");
                List<string> tempVariables = new List<string>();

                //each variables values are temporarily added to a temp file to be re-written in the main file.
                foreach (SaveVariable variable in Variables)
                {
                    string varString = FindVariable(variable.savedName);

                    if (varString != null)
                    {
                        //currently saved value
                        tempVariables.Add(varString);
                    }
                    //if we dont find the value then we instead write the default value
                    else
                    {
                        //default value
                        tempVariables.Add(variable.SavedString());
                    }
                }

                using (StreamWriter outputFile = new StreamWriter(SaveFilePath()))
                {
                    if (groups.Length > 0)
                    {
                        for (int i = 0; i < groups.Length; i++)
                        {
                            outputFile.WriteLine(groups[i]);
                            outputFile.WriteLine();

                            foreach (SaveVariable variable in Variables)
                            {
                                if (variable.saveFile.Equals(SaveFileName) && variable.group == i)
                                {
                                    for (int ii = 0; ii < tempVariables.Count; ii++)
                                    {
                                        //check each of our current temp variables to see if they contain the saved name
                                        if (tempVariables[ii].StartsWith(variable.savedName + " : "))
                                        {
                                            outputFile.WriteLine(tempVariables[ii]);
                                        }
                                    }
                                }
                            }

                            outputFile.WriteLine();
                        }
                    }
                    else
                    {
                        foreach (SaveVariable variable in Variables)
                        {
                            if (variable.saveFile.Equals(SaveFileName))
                            {
                                outputFile.WriteLine(variable.SavedString());
                            }
                        }
                    }

                    outputFile.Close();
                }
            }

            foreach (SaveVariable variable in Variables)
            {
                variable.UpdateValue();
            }
        }
    }
}