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
    private List<string>? _loadedAnnotations = null; // not a list because this isnt supposed to change
    private List<string>? _editedAnnotations = null;
    private HashSet<string>? _pendingRemovalConfig = null;
    private HashSet<string>? _pendingRemovalAnnotations = null;

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
    public List<string>? GetAnnotations()
    {
        return _loadedAnnotations;
    }

    /// <summary>
    /// Returns all edited annotations or null if nothing was edited
    /// </summary>
    public List<string>? GetEditedAnnotations()
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
    public Dictionary<string, string>? GetEditedConfig()
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
        if (_editedConfig != null)
        {
            if (!_editedConfig.ContainsKey(key))
            {
                _editedConfig?.TryAdd(key, value);

                if (terminateRemoval)
                {
                    if (_pendingRemovalConfig != null)
                    {
                        if (_pendingRemovalConfig.Contains(key))
                        {
                            _pendingRemovalConfig.Remove(key);
                        }
                    }
                }

                return OperationResult.Ok;
            }
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
        if (_editedAnnotations != null)
        {
            if (_editedAnnotations?.Contains(annotation) != null)
            {
                _editedAnnotations?.Add(annotation);

                if (terminateRemoval)
                {
                    if (_pendingRemovalAnnotations != null)
                    {
                        if (_pendingRemovalAnnotations.Contains(annotation))
                        {
                            _pendingRemovalAnnotations.Remove(annotation);
                        }
                    }
                }

                return OperationResult.Ok;
            }
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
        if (_editedConfig != null)
        {
            if (_editedConfig.ContainsKey(key))
            {
                _editedConfig.Remove(key);

                if (pendRemoval)
                {
                    _pendingRemovalConfig?.Add(key);
                }

                return OperationResult.Ok;
            }
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
        if (_editedAnnotations != null)
        {
            if (_editedAnnotations.Contains(annotation))
            {
                _editedAnnotations.Remove(annotation);

                if (pendRemoval)
                {
                    _pendingRemovalAnnotations?.Add(annotation);
                }

                return OperationResult.Ok;
            }
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

            _pendingRemovalConfig = new();
            _pendingRemovalAnnotations = new();

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
    /// Resets this CfgFile instance. Unsaved data will be lost.
    /// <para>Note: the logger will not be changed</para>
    /// </summary>
    public void Clear()
    {
        _loadedConfig = null;
        _editedConfig = null;
        _loadedAnnotations = null;
        _editedAnnotations = null;
        _pendingRemovalConfig = null;
        _pendingRemovalAnnotations = null;
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
        if (_loadedConfig != null && _editedConfig != null)
        {
            newConfig = new(_loadedConfig);

            foreach (KeyValuePair<string, string> kvp in _editedConfig)
            {
                newConfig[kvp.Key] = kvp.Value;
            }
        }
        
        if (_loadedAnnotations != null && _editedAnnotations != null)
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
        if (_pendingRemovalConfig != null)
        {
            foreach (string toRemove in _pendingRemovalConfig)
            {
                newConfig?.Remove(toRemove);
            }
        }

        if (_pendingRemovalAnnotations != null)
        {
            foreach (string toRemove in _pendingRemovalAnnotations)
            {
                newAnnotations?.Remove(toRemove);
            }
        }

        // now we apply and reset the pending removals
        _loadedConfig = newConfig;
        _loadedAnnotations = newAnnotations;

        _pendingRemovalConfig?.Clear();
        _pendingRemovalAnnotations?.Clear();
        _editedConfig?.Clear();
        _editedAnnotations?.Clear();
    }
}