using System;

namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Provides a desktop-specific implementation of settings storage,
/// using the user's application data folder as the base directory for storing settings.
/// </summary>
public class DesktopSettingsStorage : BaseSettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopSettingsStorage"/> class.
    /// Sets the base directory for settings to the application data folder on desktop platforms.
    /// </summary>
    public DesktopSettingsStorage()
    {
        SpecialFolderPath = Environment.SpecialFolder.ApplicationData;
    }
}