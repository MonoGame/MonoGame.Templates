namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Provides a console-specific implementation of settings storage,
/// using the user's TBA folder as the base directory for storing settings.
/// </summary>
public class ConsoleSettingsStorage : BaseSettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleSettingsStorage"/> class.
    /// Sets the base directory for settings to the TBA folder on desktop platforms.
    /// </summary>
    public ConsoleSettingsStorage()
    {
        // TODO
    }
}