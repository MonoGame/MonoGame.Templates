namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Manages the loading and saving of settings for the game, using a specified storage mechanism.
/// </summary>
/// <typeparam name="T">The type of the settings object. Must have a parameterless constructor.</typeparam>
internal class SettingsManager<T> where T : new()
{
    private readonly ISettingsStorage storage;

    /// <summary>
    /// Gets the storage mechanism used for saving and loading settings.
    /// </summary>
    public ISettingsStorage Storage => storage;

    private T settings;

    /// <summary>
    /// Gets the current settings object.
    /// </summary>
    public T Settings => settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsManager{T}"/> class,
    /// using the specified storage mechanism. Automatically loads the settings on creation.
    /// </summary>
    /// <param name="storage">The storage mechanism to use for saving and loading settings.</param>
    public SettingsManager(ISettingsStorage storage)
    {
        this.storage = storage;
        Load();
    }

    /// <summary>
    /// Saves the current settings to the storage.
    /// </summary>
    public void Save()
    {
        storage.SaveSettings<T>(settings);
    }

    /// <summary>
    /// Loads the settings from the storage. If no settings file exists, a new instance of
    /// the settings object is created.
    /// </summary>
    public void Load()
    {
        settings = storage.LoadSettings<T>();
    }
}