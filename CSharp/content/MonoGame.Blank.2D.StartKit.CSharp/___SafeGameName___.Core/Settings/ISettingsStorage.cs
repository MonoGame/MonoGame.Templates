namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Defines a platform-agnostic interface for managing the storage and retrieval of game settings.
/// </summary>
public interface ISettingsStorage
{
    /// <summary>
    /// Gets or sets the name of the file used to store settings.
    /// </summary>
    string SettingsFileName { get; set; }

    /// <summary>
    /// Saves the specified settings to the storage medium.
    /// </summary>
    /// <typeparam name="T">The type of the settings object.</typeparam>
    /// <param name="settings">The settings object to save.</param>
    void SaveSettings<T>(T settings) where T : new();

    /// <summary>
    /// Loads settings from the storage medium.
    /// </summary>
    /// <typeparam name="T">The type of the settings object.</typeparam>
    /// <returns>The settings object loaded from storage.</returns>
    T LoadSettings<T>() where T : new();
}