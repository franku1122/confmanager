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
    private Dictionary<string, string> _editedConfig = new();
    private List<string>? _loadedAnnotations = null; // not a list because this isnt supposed to change
    private List<string> _editedAnnotations = new();
    private HashSet<string> _pendingRemovalConfig = new();
    private HashSet<string> _pendingRemovalAnnotations = new();

    /// <summary>
    /// Replaces the logger with <paramref name="newLogger"/>
    /// </summary>
    public void SetLogger(ILogger newLogger)
    {
        _logger = newLogger;
    }

    /// <summary>
    /// Returns all annotations found in the config file
    /// </summary>
    public List<string>? GetAnnotations()
    {
        return _loadedAnnotations;
    }

    /// <summary>
    /// Returns all edited annotations
    /// </summary>
    public List<string> GetEditedAnnotations()
    {
        return _editedAnnotations;
    }

    /// <summary>
    /// Returns an annotation from the loaded annotations or null if not found
    /// </summary>
    public string? GetAnnotation(string name)
    {
        if (_loadedAnnotations != null)
        {
            foreach (string annotation in _loadedAnnotations)
            {
                if (annotation == "name") { return annotation; }
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the loaded config ( or null if it doesn't exist )
    /// </summary>
    public Dictionary<string, string>? GetLoadedConfig()
    {
        return _loadedConfig;
    }

    /// <summary>
    /// Returns the edited config. If nothing was edited, it's null. If a config is saved, the edited config is merged with the loaded one,
    /// and the edited config is set to null.
    /// </summary>
    public Dictionary<string, string> GetEditedConfig()
    {
        return _editedConfig;
    }

    /// <summary>
    /// Adds a value to the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value for the key</param>
    /// <param name="terminateRemoval">If true, will remove a value that matches this one from pending removal on next config apply</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult AddModifiedValue(string key, string value, bool terminateRemoval = true)
    {
        if (!_editedConfig.ContainsKey(key))
        {
            _editedConfig.TryAdd(key, value);

            if (terminateRemoval)
            {
                if (_pendingRemovalConfig.Contains(key))
                {
                    _pendingRemovalConfig.Remove(key);
                }
            }

            return OperationResult.Ok;
        }
        
        return OperationResult.Error;
    }

    /// <summary>
    /// Adds an annotation to the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="annotation">Annotation to be added</param>
    /// <param name="terminateRemoval">If true, will remove an annotation that matches this one from pending removal on next config apply</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult AddModifiedAnnotation(string annotation, bool terminateRemoval = true)
    {
        if (!_editedAnnotations.Contains(annotation))
        {
            _editedAnnotations.Add(annotation);

            if (terminateRemoval)
            {
                if (_pendingRemovalAnnotations.Contains(annotation))
                {
                    _pendingRemovalAnnotations.Remove(annotation);
                }
            }

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Removes a value from the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="pendRemoval">If true, this value will be removed from the config if changes are applied</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult RemoveModifiedValue(string key, bool pendRemoval = true)
    {
        if (_editedConfig.ContainsKey(key))
        {
            _editedConfig.Remove(key);

            if (pendRemoval)
            {
                _pendingRemovalConfig.Add(key);
            }

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Removes an annotation from the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="annotation">The annotation</param>
    /// <param name="pendRemoval">If true, this value will be removed from the config if changes are applied</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult RemoveModifiedAnnotation(string annotation, bool pendRemoval = true)
    {
        if (_editedAnnotations.Contains(annotation))
        {
            _editedAnnotations.Remove(annotation);

            if (pendRemoval)
            {
                _pendingRemovalAnnotations.Add(annotation);
            }

            return OperationResult.Ok;
        }

        return OperationResult.Error;
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
                List<string>? readAnnotations = null;
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
                                readAnnotations = annotations.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
#pragma warning restore IDE0300

                                for (int i = 0; i < readAnnotations.Count; i++)
	                            {
	                            	readAnnotations[i] = readAnnotations[i].Trim();
	                            }

                                if (readAnnotations.Count > 0)
                                {
                                    _loadedAnnotations = readAnnotations;
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
    /// Saves the config file at <paramref name="path"/>
    /// </summary>
    /// <param name="path">Path where the file will be created</param>
    /// <param name="overwriteIfExists">Overwrite an existing file if there is one</param>
    /// <param name="applyBeforeSave">Runs <c>ApplyModified</c> before saving</param>
    /// <returns>Result of the operation as <see cref="OperationResult"/>. Returns Ok always</returns>
    public OperationResult SaveFile(string path, bool applyBeforeSave = false, bool overwriteIfExists = true)
    {
        bool overwrite = !overwriteIfExists;

        if (applyBeforeSave)
        {
            ApplyModified();
        }
        
        try
        {
            using (StreamWriter writer = new(path, overwrite))
            {
                // first add annotations
                if (_loadedAnnotations != null && _loadedAnnotations.Count > 0)
                {
                    string annotations = $"@annotation {string.Join(", ", _loadedAnnotations)}";
                    writer.WriteLine(annotations);
                }

                // add some space
                writer.WriteLine();

                if (_loadedConfig != null)
                {
                    foreach (KeyValuePair<string, string> kvp in _loadedConfig)
                    {
                        if (CfgCustomizer.KeyValueSeparator != ' ')
                        {
                            writer.WriteLine($"{kvp.Key} {CfgCustomizer.KeyValueSeparator} {kvp.Value}");
                        }
                        else // spaces are handled differently than other stuff so no extra spaces are added around the kv separator
                        {
                            writer.WriteLine($"{kvp.Key}{CfgCustomizer.KeyValueSeparator}{kvp.Value}");
                        }
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Put(LogType.Error, $"Cannot access {path}");
            return OperationResult.NoPermission;
        }
        catch (PathTooLongException)
        {
            _logger.Put(LogType.Error, "Path is too long");
            return OperationResult.Error;
        }
        catch (IOException)
        {
            _logger.Put(LogType.Error, "IOException has been caught; file is probably in use");
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
        _editedConfig.Clear();
        _loadedAnnotations = null;
        _editedAnnotations.Clear();
        _pendingRemovalConfig.Clear();
        _pendingRemovalAnnotations.Clear();
    }

    /// <summary>
    /// Applies the changes made to the loaded config ( eg. merges _editedAnnotations into _loadedAnnotations )
    /// <para>Note: the edited values have priority over their loaded counterparts</para>
    /// </summary>
    public void ApplyModified()
    {
        Dictionary<string, string>? newConfig = null;
        List<string>? newAnnotations = null;
        
        // merge loaded and edited configs
        if (_loadedConfig != null)
        {
            newConfig = new(_loadedConfig);

            foreach (KeyValuePair<string, string> kvp in _editedConfig)
            {
                newConfig[kvp.Key] = kvp.Value;
            }
        }
        
        if (_loadedAnnotations != null)
        {
            newAnnotations = new(_loadedAnnotations);
            HashSet<string> seen = new(_loadedAnnotations);

            foreach (string item in _editedAnnotations)
            {
                if (seen.Add(item))
                {
                    newAnnotations.Add(item);
                }
            }
        }

        // and now we remove items that are in the pending removal lists
        foreach (string toRemove in _pendingRemovalConfig)
        {
            newConfig?.Remove(toRemove);
        }

        foreach (string toRemove in _pendingRemovalAnnotations)
        {
            newAnnotations?.Remove(toRemove);
        }

        // now we apply and reset the pending removals
        if (newConfig != null)
        {
            _loadedConfig = newConfig;
        }

        if (newAnnotations != null)
        {
            _loadedAnnotations = newAnnotations;
        }

        _pendingRemovalConfig?.Clear();
        _pendingRemovalAnnotations?.Clear();
        _editedConfig?.Clear();
        _editedAnnotations?.Clear();
    }
}