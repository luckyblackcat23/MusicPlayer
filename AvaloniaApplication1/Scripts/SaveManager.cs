using AvaloniaApplication1.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

//didn't wanna use some overcomplicated json so instead i just made my own overcomplicated save system 

//couldn't think of a way to reference the save files without just referencing the file name as a string.
//possible improvement for later could be to find a way to have intellisense pickup the SaveFile names somehow.
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

    public static SaveFile StringToSave(string saveFileName)
    {
        foreach (SaveFile file in saveFileData)
        {
            if(file.SaveFileName == saveFileName)
                return file;
        }

        return null;
    }

    //required data of each save file of name
    public static List<SaveFile> saveFileData = new();
    public static SaveFile MainSave = new("Main.txt");

    public class SaveFile
    {
        /// <summary>
        /// Include .txt at the end unless you plan to use another filetype
        /// </summary>
        public string SaveFileName = "Main.txt";
        public string SaveFilePath()
        { 
            //change later
            return Path.Combine(Globals.SavePath, SaveFileName); 
        }

        public string[] groups = new string[] { "General", "Audio" };

        public SaveFile(string name)
        {
            saveFileData.Add(this);

            SaveFileName = name;
        }
    }

    public static List<SaveVariable> Variables = new();

    /// <summary> 
    /// Writes string to line
    /// <para>
    /// Warning: this will overwrite a line. if you dont know exactly what will be on that line do not use this function
    /// </para> 
    /// </summary>
    public static void lineChanger(string newText, int lineToEdit, SaveFile save)
    {
        string[] arrLine = ReadAll().ToArray();
        arrLine[lineToEdit - 1] = newText;
        using (StreamWriter outputFile = new StreamWriter(save.SaveFilePath()))
        {
            foreach (string line in arrLine)
            {
                outputFile.WriteLine(line);
            }
            outputFile.Close();
        }
    }

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

    public static string FindVariable(string SavedName)
    {
        foreach (string line in ReadAll())
        {
            if (line.StartsWith(SavedName + " : "))
            {
                return line;
            }
        }

        Debug.WriteLine("saved variable was not found.");

        return null;
    }

    public static string FindVariable(string SavedName, out int lineNumber)
    {
        string[] lines = ReadAll().ToArray();

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

    public static string FindVariableValue(string SavedName)
    {
        foreach (string line in ReadAll())
        {
            if (line.StartsWith(SavedName + " : "))
            {
                return line.Remove(0, (SavedName + " : ").Length);
            }
        }

        Debug.WriteLine("saved variable was not found.");

        return "";
    }

    public static void UpdateVariable(SaveVariable variable)
    {
        FindVariable(variable.savedName, out int line);

        lineChanger(variable.SavedString(), line, StringToSave(variable.saveFile));
    }

    public delegate void OnInitializeDelegate();
    public static OnInitializeDelegate OnInitialize;

    public static void Initialize()
    {
        if (OnInitialize != null)
            OnInitialize();

        if (!Directory.Exists(Globals.SavePath))
        {
            Directory.CreateDirectory(Globals.SavePath);

            foreach (SaveFile saveFile in saveFileData)
            {
                using (StreamWriter outputFile = File.CreateText(saveFile.SaveFilePath()))
                {
                    if(saveFile.groups.Length > 0)
                    {
                        for (int i = 0; i < saveFile.groups.Length; i++)
                        {
                            outputFile.WriteLine(saveFile.groups[i]);
                            outputFile.WriteLine();

                            foreach (SaveVariable variable in Variables)
                            {
                                if (variable.saveFile.Equals(saveFile.SaveFileName) && variable.group == i)
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
        else
        {
            foreach (SaveFile saveFile in saveFileData)
            {
                if (!File.Exists(saveFile.SaveFilePath()))
                    File.CreateText(saveFile.SaveFilePath());

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

                    using (StreamWriter outputFile = new StreamWriter(saveFile.SaveFilePath()))
                    {
                        if (saveFile.groups.Length > 0)
                        {
                            for (int i = 0; i < saveFile.groups.Length; i++)
                            {
                                outputFile.WriteLine(saveFile.groups[i]);
                                outputFile.WriteLine();

                                foreach (SaveVariable variable in Variables)
                                {
                                    if (variable.saveFile.Equals(saveFile.SaveFileName) && variable.group == i)
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
                                if (variable.saveFile.Equals(saveFile.SaveFileName))
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
}