using System;
using System.IO;
using System.Text.Json;

namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Provides a base implementation for storing and retrieving settings in a JSON file,
/// with platform-specific file paths and support for creating directories if needed.
/// </summary>
public abstract class BaseSettingsStorage : ISettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSettingsStorage"/> class.
    /// Sets a default file name for settings.
    /// </summary>
    protected BaseSettingsStorage()
    {
        SettingsFileName = "settings.json"; // Default file name for settings
    }

    /// <summary>
    /// Gets or sets the special folder path used as the base directory for settings storage.
    /// </summary>
    protected static Environment.SpecialFolder SpecialFolderPath { get; set; }

    private string settingsFileName;

    /// <summary>
    /// Gets or sets the name of the settings file.
    /// </summary>
    public string SettingsFileName
    {
        get => settingsFileName;
        set
        {
            if (settingsFileName != value)
            {
                settingsFileName = value;
            }
        }
    }

    /// <summary>
    /// Gets the full file path to the settings file, combining the special folder path,
    /// the application folder, and the file name.
    /// </summary>
    protected string SettingsFilePath => Path.Combine(Environment.GetFolderPath(SpecialFolderPath), "___SafeGameName___", SettingsFileName);

    /// <summary>
    /// Saves the specified settings object to a JSON file.
    /// Creates the target directory if it does not already exist.
    /// </summary>
    /// <typeparam name="T">The type of the settings object.</typeparam>
    /// <param name="settings">The settings object to save.</param>
    public virtual void SaveSettings<T>(T settings) where T : new()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(settings, options);

        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(SettingsFilePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(SettingsFilePath, jsonString);
    }

    /// <summary>
    /// Loads the settings object from a JSON file.
    /// If the file does not exist or deserialization fails, returns a new instance of the settings object.
    /// </summary>
    /// <typeparam name="T">The type of the settings object.</typeparam>
    /// <returns>The settings object loaded from the file, or a new instance if the file does not exist.</returns>
    public virtual T LoadSettings<T>() where T : new()
    {
        if (!File.Exists(SettingsFilePath))
            return new T();

        string jsonString = File.ReadAllText(SettingsFilePath);
        return JsonSerializer.Deserialize<T>(jsonString) ?? new T();
    }
}