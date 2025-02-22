using System.Collections.Generic;
using System;
using System.IO;

namespace ConfManager;

/// <summary>
/// A class storing the data of a loaded .cfg file
/// </summary>
public sealed class CfgFile
{
    private ILogger _logger = new DefaultLogger();
    private Dictionary<string, string>? _loadedConfig = null;
    private Dictionary<string, string>? _editedConfig = null;
    private string[]? _annotations = null;

    /// <summary>
    /// Replaces the logger with <paramref name="newLogger"/>
    /// </summary>
    public void SetLogger(ILogger newLogger)
    {
        _logger = newLogger;
    }

    /// <summary>
    /// Returns all annotations found in the config file. May return null if nothing was found
    /// </summary>
    public string[]? GetAnnotations()
    {
        return _annotations;
    }

    /// <summary>
    /// Returns the loaded config
    /// </summary>
    public Dictionary<string, string>? GetLoadedConfig()
    {
        return _loadedConfig;
    }

    /// <summary>
    /// Returns the edited config. If nothing was edited, it's null. If a config is saved, the edited config is merged with the loaded one,
    /// and the edited config is set to null.
    /// </summary>
    public Dictionary<string, string>? GetEditedConfig()
    {
        return _editedConfig;
    }

    /// <summary>
    /// Opens a config file
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <returns><see cref="OperationResult"/></returns>
    public OperationResult OpenFile(string path)
    {
        // try open the file
        try
        {
            FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);

            using (StreamReader reader = new(stream))
            {
                int lineIdx = 0;
                string[] readAnnotations = Array.Empty<string>();
                Dictionary<string, string> loadedDict = new();
                
                while (!reader.EndOfStream)
                {
                    lineIdx++;
                    string? uglyLine = reader.ReadLine(); // ugly because it can contain extra spaces n stuff

                    if (uglyLine != null)
                    {
                        string line = uglyLine.Trim();

                        if (lineIdx == 1)
                        {
                            // we add annotations ( requirements are simple; line starts with '@annotation ' and we put everything
                            // into readAnnotations )
                            
                            if (line.StartsWith("@annotation "))
                            {
                                // now we copy everything after @annotation into a new string, and separate those into specific annotations
                                // which can be separated either by a semicolon or a comma
                                string annotations = line.Remove(0, 11).Trim();

#pragma warning disable IDE0300 // this is needed otherwise youll get CS0121; you need new char[]
                                readAnnotations = annotations.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
#pragma warning restore IDE0300

                                for (int i = 0; i < readAnnotations.Length; i++)
	                            {
	                            	readAnnotations[i] = readAnnotations[i].Trim();
	                            }

                                if (readAnnotations.Length > 0)
                                {
                                    _annotations = readAnnotations;
                                }
                            }
                        }
                        else
                        {
                            // remove everything after the comment character
                            int commentIndex = line.IndexOf(CfgCustomizer.CommentCharacter);
                            string lineNoComments = commentIndex >= 0 ? line.Substring(0, commentIndex).Trim() : line;

#pragma warning disable IDE0300 // this is needed otherwise youll get CS0121; you need new char[]
                            string[] keyValuePairs = lineNoComments.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
#pragma warning restore IDE0300

                            foreach (string pair in keyValuePairs)
                            {
                                // now we split the line by the key value separator
                                string[] parts = pair.Split(CfgCustomizer.KeyValueSeparator, 2, StringSplitOptions.RemoveEmptyEntries);

                                if (parts.Length == 2)
                                {
                                    // we get the values and add them in
                                    string key = parts[0].Trim();
                                    string value = parts[1].Trim();

                                    loadedDict.Add(key, value);
                                }
                                else
                                {
                                    _logger.Put(LogType.Warn, "Failed to parse a value; the value has not been added. Error encountered at:");
                                    _logger.Put(LogType.Warn, $"Line {lineIdx} : {line}");
                                }
                            }
                        }
                    }
                }

                _loadedConfig = loadedDict;
            }
        }
        catch (FileNotFoundException)
        {
            _logger.Put(LogType.Error, $"File {path} not found");
            return OperationResult.FileNotFound;
        }
        catch (DirectoryNotFoundException)
        {
            _logger.Put(LogType.Error, $"File {path} not found");
            return OperationResult.FileNotFound;
        }
        catch (Exception exception)
        {
            _logger.Put(LogType.Error, $"OpenFile has caught an undefined exception: {exception}");
            return OperationResult.Error;
        }
        
        return OperationResult.Ok;
    }

    /// <summary>
    /// Resets this CfgFile instance. Unsaved data will be lost.
    /// <para>Note: the logger will not be changed</para>
    /// </summary>
    public void Clear()
    {
        _loadedConfig = null;
        _editedConfig = null;
        _annotations = null;
    }
}